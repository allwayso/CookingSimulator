using System;
using System.Collections;
using System.Collections.Generic;
using CookingSimulator.Models;
using CookingSimulator.Services;
using CookingSimulator.UI;
using UnityEngine;

namespace CookingSimulator.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private SaveManager saveManager;
        [SerializeField] private RecipeManager recipeManager;
        [SerializeField] private LogManager logManager;
        [SerializeField] private ReviewManager reviewManager;
        [SerializeField] private AIReviewService aiReviewService;

        [Header("Views")]
        [SerializeField] private LoginUI loginUI;
        [SerializeField] private ModeSelectUI modeSelectUI;
        [SerializeField] private RecipeSelectUI recipeSelectUI;
        [SerializeField] private CookingUI cookingUI;
        [SerializeField] private ReviewUI reviewUI;
        [SerializeField] private SaveDishUI saveDishUI;
        [SerializeField] private MenuUI menuUI;
        [SerializeField] private StatusBarUI statusBarUI;
        [SerializeField] private GameObject loginBackgroundRoot;

        private UserData currentUser;
        private RecipeData currentRecipe;
        private CookingLog currentLog;
        private ReviewData currentReview;
        private DishState currentState;
        private string currentDishId;
        private string currentLogPath;

        // 火力 & 熟度系统
        private int currentFireLevel;
        private float cookingElapsed;
        private Dictionary<string, IngredientCookState> ingredientStates;
        private IngredientCookingConfig[] currentCookConfigs;
        private Coroutine cookingCoroutine;

        private void Start()
        {
            ShowLogin();
        }

        public void Login(string username)
        {
            currentUser = saveManager.LoadOrCreateUser(username);
            statusBarUI?.Show(currentUser);
            modeSelectUI.Show(currentUser, EnterChefMode);
            Hide(loginUI);
            SetLoginBackgroundVisible(true);
        }

        public void EnterChefMode()
        {
            var recipes = recipeManager.LoadRecipes();
            recipeSelectUI.Show(recipes, StartCooking, ShowMenuFromRecipeSelect);
            Hide(modeSelectUI);
            SetLoginBackgroundVisible(false);
        }

        public void StartCooking(RecipeData recipe)
        {
            currentRecipe = recipe;
            currentDishId = Guid.NewGuid().ToString("N");
            currentState = DishState.Raw;
            currentFireLevel = 0;

            // 初始化食材熟度追踪
            ingredientStates = new Dictionary<string, IngredientCookState>();
            currentCookConfigs = recipe.ingredientCookingConfigs ?? Array.Empty<IngredientCookingConfig>();

            Debug.Log($"[Cooking] 菜谱: {recipe.name}, 食材配置 {currentCookConfigs.Length} 个");
            foreach (var config in currentCookConfigs)
            {
                ingredientStates[config.ingredientName] = new IngredientCookState
                {
                    ingredientName = config.ingredientName,
                    cookProgress = 0f,
                    doneness = DonenessLevel.Raw
                };
                Debug.Log($"[Cooking] 食材: {config.ingredientName}, 全熟阈值: {config.fullCookThreshold}");
            }

            logManager.StartLog(currentUser, currentRecipe, currentDishId);
            cookingUI.Show(currentRecipe, currentState,
                HandleCookingAction, FinishCooking, HandleFireLevelChanged);
            Hide(recipeSelectUI);
        }

        public void HandleCookingAction(string action, string target)
        {
            var before = currentState;
            if (!TryApplyAction(action, out var after))
            {
                cookingUI.ShowMessage("当前不能这样做");
                return;
            }

            currentState = after;

            // 进入 Cooking 状态时启动协程（必须在 currentState 更新之后）
            if (after == DishState.Cooking)
            {
                cookingCoroutine = StartCoroutine(CookingCoroutine());
            }
            // 离开 Cooking 状态时停止协程
            else if (before == DishState.Cooking && after != DishState.Cooking)
            {
                if (cookingCoroutine != null)
                {
                    StopCoroutine(cookingCoroutine);
                    cookingCoroutine = null;
                }
            }

            logManager.AddAction(action, target, before, after);
            cookingUI.UpdateState(currentState);
        }

        public void HandleFireLevelChanged(int level)
        {
            currentFireLevel = level;
        }

        public void FinishCooking()
        {
            HandleCookingAction("finish", "菜品");
            currentLog = logManager.Finish(currentState);

            // 记录最终食材熟度到日志
            if (ingredientStates != null && ingredientStates.Count > 0)
            {
                currentLog.ingredientResults = new List<IngredientCookState>();
                foreach (var kvp in ingredientStates)
                {
                    currentLog.ingredientResults.Add(new IngredientCookState
                    {
                        ingredientName = kvp.Value.ingredientName,
                        cookProgress = kvp.Value.cookProgress,
                        doneness = kvp.Value.doneness
                    });
                }
            }

            currentLogPath = saveManager.SaveLog(currentLog);
            currentReview = reviewManager.CreateLocalReview(currentDishId, currentRecipe, currentLog);
            saveManager.SaveReview(currentReview);

            currentUser.reputation += currentReview.reputationDelta;
            saveManager.SaveUser(currentUser);
            statusBarUI?.Refresh(currentUser);

            reviewUI.Show(currentReview, ShowSaveDish);
            Hide(cookingUI);
        }

        public void ShowSaveDish()
        {
            saveDishUI.Show(currentReview, SaveDish);
            Hide(reviewUI);
        }

        public void SaveDish(string dishName, float price)
        {
            var dish = new DishData
            {
                dishId = currentDishId,
                userId = currentUser.userId,
                name = dishName,
                price = price,
                score = currentReview.score,
                finalState = currentState,
                logPath = currentLogPath,
                reviewId = currentReview.reviewId,
                reviewText = currentReview.summary,
                createdAt = DateTime.UtcNow.ToString("O")
            };
            saveManager.SaveDish(dish);
            ShowMenu();
        }

        public void ShowMenu()
        {
            var dishes = saveManager.LoadDishes(currentUser.userId);
            menuUI.ShowDishes(dishes, EnterChefMode, ShowReviewerSelection);
            Hide(saveDishUI);
            Hide(reviewUI);
        }

        public void ShowMenuFromRecipeSelect()
        {
            ShowMenu();
            Hide(recipeSelectUI);
        }

        public void ShowReviewerSelection(DishData dish)
        {
            menuUI.ShowReviewers(dish, ShowMenu, reviewerName =>
            {
                if (reviewerName == "AI 老八")
                {
                    ShowLaobaReview(dish);
                }
            });
        }

        public void ShowLaobaReview(DishData dish)
        {
            var existingReview = saveManager.LoadReview(dish.reviewId);
            if (existingReview != null && existingReview.summary.StartsWith("AI 老八评价", StringComparison.Ordinal))
            {
                reviewUI.Show(existingReview, ShowMenu, "返回食单");
                Hide(menuUI);
                return;
            }

            var recipe = LoadRecipeForDish(dish);
            var log = LoadLog(dish.logPath);
            var baseReview = existingReview ?? reviewManager.CreateLocalLaobaReview(dish);
            StartCoroutine(aiReviewService.CreateLaobaReview(dish, recipe, log, baseReview, (review, usedAi, error) =>
            {
                saveManager.SaveReview(review);
                dish.reviewId = review.reviewId;
                dish.reviewText = review.summary;
                dish.score = review.score;
                saveManager.SaveDish(dish);

                currentUser.reputation += review.reputationDelta;
                saveManager.SaveUser(currentUser);
                statusBarUI?.Refresh(currentUser);

                reviewUI.Show(review, ShowMenu, "返回食单");
                Hide(menuUI);
            }));
        }

        private void ShowLogin()
        {
            loginUI.Show(Login);
            Hide(modeSelectUI);
            Hide(recipeSelectUI);
            Hide(cookingUI);
            Hide(reviewUI);
            Hide(saveDishUI);
            Hide(menuUI);
            Hide(statusBarUI);
            SetLoginBackgroundVisible(true);
        }

        private bool TryApplyAction(string action, out DishState nextState)
        {
            nextState = currentState;

            if (action == "cut" && currentState == DishState.Raw)
            {
                nextState = DishState.Cut;
                return true;
            }

            if (action == "put_in_pan" && currentState == DishState.Cut)
            {
                nextState = DishState.Cooking;
                return true;
            }

            // "heat" 动作已移除 —— 火力由滑杆实时控制，不再通过按钮触发
            // 保留状态检查以兼容旧日志/步骤，但不做任何操作

            if (action == "season" && currentState == DishState.Cooking)
            {
                nextState = DishState.Seasoned;
                return true;
            }

            if (action == "stir" && currentState == DishState.Seasoned)
            {
                return true;
            }

            if (action == "finish" && currentState == DishState.Seasoned)
            {
                nextState = DishState.Done;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 烹饪协程：每帧根据火力累积食材烹饪进度，检测熟度变化并更新 UI
        /// </summary>
        private IEnumerator CookingCoroutine()
        {
            cookingElapsed = 0f;
            Debug.Log($"[Cooking] 协程启动, 火力={currentFireLevel}, 食材数={ingredientStates?.Count ?? 0}, 配置数={currentCookConfigs?.Length ?? 0}");

            while (currentState == DishState.Cooking)
            {
                cookingElapsed += Time.deltaTime;
                cookingUI.UpdateTimer(cookingElapsed);

                if (currentFireLevel > 0 && ingredientStates != null)
                {
                    float delta = Time.deltaTime * currentFireLevel;

                    foreach (var config in currentCookConfigs)
                    {
                        if (!ingredientStates.TryGetValue(config.ingredientName, out var state))
                        {
                            Debug.LogWarning($"[Cooking] 找不到食材状态: {config.ingredientName}");
                            continue;
                        }

                        state.cookProgress += delta;
                        var newDoneness = CalculateDoneness(state.cookProgress, config.fullCookThreshold);

                        if (newDoneness != state.doneness)
                        {
                            state.doneness = newDoneness;
                            Debug.Log($"[Cooking] {config.ingredientName} 熟度变化: {newDoneness} (进度={state.cookProgress:F1}/{config.fullCookThreshold})");
                            cookingUI.UpdateIngredientDoneness(config.ingredientName, newDoneness);
                        }
                    }
                }

                yield return null;
            }

            Debug.Log($"[Cooking] 协程结束, 总烹饪时间={cookingElapsed:F1}s");
        }

        /// <summary>
        /// 根据烹饪进度和全熟阈值计算当前熟度等级
        /// 阈值划分：0-25% 全生 / 25%-62.5% 半生 / 62.5%-100% 全熟 / >100% 过头
        /// </summary>
        private static DonenessLevel CalculateDoneness(float progress, float fullThreshold)
        {
            if (fullThreshold <= 0f)
                return DonenessLevel.FullyCooked;

            float ratio = progress / fullThreshold;

            if (ratio < 0.25f)
                return DonenessLevel.Raw;
            if (ratio < 0.625f)
                return DonenessLevel.HalfCooked;
            if (ratio <= 1.0f)
                return DonenessLevel.FullyCooked;
            return DonenessLevel.Overcooked;
        }

        private RecipeData LoadRecipeForDish(DishData dish)
        {
            var recipes = recipeManager.LoadRecipes();
            foreach (var recipe in recipes)
            {
                if (!string.IsNullOrWhiteSpace(dish.logPath))
                {
                    var log = LoadLog(dish.logPath);
                    if (log != null && log.recipeId == recipe.recipeId)
                    {
                        return recipe;
                    }
                }
            }

            return recipes.Count > 0 ? recipes[0] : new RecipeData
            {
                recipeId = "unknown",
                name = "未知菜谱",
                description = string.Empty,
                ingredients = Array.Empty<string>(),
                seasonings = Array.Empty<string>(),
                steps = Array.Empty<RecipeStep>(),
                ingredientCookingConfigs = Array.Empty<IngredientCookingConfig>()
            };
        }

        private static CookingLog LoadLog(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                return new CookingLog();
            }

            return JsonUtility.FromJson<CookingLog>(System.IO.File.ReadAllText(path));
        }

        private static void Hide(MonoBehaviour view)
        {
            if (view == null)
            {
                return;
            }

            view.gameObject.SetActive(false);
        }

        private void SetLoginBackgroundVisible(bool visible)
        {
            if (loginBackgroundRoot != null)
            {
                loginBackgroundRoot.SetActive(visible);
            }
        }
    }
}
