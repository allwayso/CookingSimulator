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

        [Header("Free Roam")]
        [SerializeField] private 交互管理 interactionManager;
        [SerializeField] private 备菜选菜UI ingredientSelectUI;
        [SerializeField] private GameObject playerObject;
        [SerializeField] private GameObject fridgeObject;

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

        private bool ingredientsReady;
        private RecipeData selectedRecipe;

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
            ingredientsReady = false;
            selectedRecipe = null;
            EnterFreeRoam();
        }

        // ── Free Roam（餐厅自由走动）──────────────────────────

        private void EnterFreeRoam()
        {
            Hide(loginUI);
            Hide(modeSelectUI);
            Hide(recipeSelectUI);
            Hide(cookingUI);
            Hide(reviewUI);
            Hide(saveDishUI);
            Hide(menuUI);
            statusBarUI?.Show(currentUser);
            SetLoginBackgroundVisible(true);

            if (interactionManager != null)
            {
                interactionManager.gameObject.SetActive(true);
                interactionManager.OnInteract -= HandleInteraction;
                interactionManager.OnInteract += HandleInteraction;
            }

            // 恢复厨房物体显示
            if (fridgeObject != null) fridgeObject.SetActive(true);
            if (playerObject != null) playerObject.SetActive(true);
        }

        private void HandleInteraction(InteractionType type)
        {
            switch (type)
            {
                case InteractionType.Fridge:
                    OpenIngredientSelect();
                    break;
                case InteractionType.Stove:
                    if (ingredientsReady && selectedRecipe != null)
                        StartCookingWithRecipe(selectedRecipe);
                    break;
            }
        }

        private void OpenIngredientSelect()
        {
            if (interactionManager != null)
                interactionManager.gameObject.SetActive(false);

            var recipes = recipeManager.LoadRecipes();
            var allIngredients = GatherAllIngredients(recipes);

            ingredientSelectUI.Show(allIngredients, recipes, OnIngredientsConfirmed, OnIngredientCancelled);
            SetLoginBackgroundVisible(false);
        }

        private void OnIngredientsConfirmed(RecipeData recipe)
        {
            selectedRecipe = recipe;
            ingredientsReady = true;
            StartCoroutine(DelayedExitIngredientSelect());
        }

        private void OnIngredientCancelled()
        {
            ingredientSelectUI.Hide();
            EnterFreeRoam();
        }

        private void StartCookingWithRecipe(RecipeData recipe)
        {
            if (interactionManager != null)
                interactionManager.gameObject.SetActive(false);

            currentRecipe = recipe;
            currentDishId = Guid.NewGuid().ToString("N");
            currentState = DishState.Raw;
            currentFireLevel = 1; // 默认小火

            // 初始化食材熟度追踪（所有食材初始在盘子上/未使用）
            ingredientStates = new Dictionary<string, IngredientCookState>();
            currentCookConfigs = recipe.ingredientCookingConfigs ?? Array.Empty<IngredientCookingConfig>();

            Debug.Log($"[Cooking] 菜谱: {recipe.name}, 食材配置 {currentCookConfigs.Length} 个");
            foreach (var config in currentCookConfigs)
            {
                ingredientStates[config.ingredientName] = new IngredientCookState
                {
                    ingredientName = config.ingredientName,
                    cookProgress = 0f,
                    doneness = DonenessLevel.Raw,
                    isInPan = false
                };
                Debug.Log($"[Cooking] 食材: {config.ingredientName}, 全熟阈值: {config.fullCookThreshold}");
            }

            logManager.StartLog(currentUser, currentRecipe, currentDishId);
            cookingUI.Show(currentRecipe, currentState,
                HandleCookingAction, FinishCooking, HandleFireLevelChanged,
                HandleTransferToPlate, HandleTransferToPan);
            SetLoginBackgroundVisible(false);
            Hide(recipeSelectUI);

            // 做菜时隐藏厨房物体
            if (fridgeObject != null) fridgeObject.SetActive(false);
            if (playerObject != null) playerObject.SetActive(false);
        }

        // ── 动作处理 ──

        public void HandleCookingAction(string action, string target)
        {
            var before = currentState;
            if (!TryApplyAction(action, out var after))
            {
                cookingUI.ShowMessage("当前不能这样做");
                return;
            }

            currentState = after;

            // 放入食材：标记该食材在锅中，启动协程
            if (action == "put_in_pan" && ingredientStates != null &&
                ingredientStates.TryGetValue(target, out var putState))
            {
                putState.isInPan = true;
                putState.hasActivated = true;
                Debug.Log($"[Cooking] {target} 下锅, isInPan=true");
                EnsureCoroutineRunning();
            }

            // 进入 Cooking 状态时启动协程
            if (after == DishState.Cooking && before != DishState.Cooking)
            {
                EnsureCoroutineRunning();
            }
            // 离开 Cooking 状态时停止协程
            if (before == DishState.Cooking && after != DishState.Cooking)
            {
                StopCookingCoroutine();
            }

            logManager.AddAction(action, target, before, after);
            cookingUI.UpdateState(currentState);
            RefreshContainerDisplay();
        }

        public void HandleFireLevelChanged(int level)
        {
            currentFireLevel = level;
        }

        public void HandleTransferToPlate()
        {
            if (currentState != DishState.Cooking)
            {
                cookingUI.ShowMessage("现在不能盛出");
                return;
            }

            bool hasContent = false;
            foreach (var state in ingredientStates.Values)
            {
                if (state.isInPan)
                {
                    state.isInPan = false;
                    hasContent = true;
                }
            }

            if (!hasContent)
            {
                cookingUI.ShowMessage("锅里没有东西可以盛出");
                return;
            }

            Debug.Log("[Cooking] 锅→盘 转移");
            logManager.AddAction("transfer_to_plate", "锅→盘", currentState, currentState);
            RefreshContainerDisplay();
        }

        public void HandleTransferToPan()
        {
            if (currentState != DishState.Cooking)
            {
                cookingUI.ShowMessage("现在不能倒回");
                return;
            }

            bool hasContent = false;
            foreach (var state in ingredientStates.Values)
            {
                if (!state.isInPan)
                {
                    state.isInPan = true;
                    hasContent = true;
                }
            }

            if (!hasContent)
            {
                cookingUI.ShowMessage("盘子上没有东西可以倒回");
                return;
            }

            Debug.Log("[Cooking] 盘→锅 转移");
            EnsureCoroutineRunning();
            logManager.AddAction("transfer_to_pan", "盘→锅", currentState, currentState);
            RefreshContainerDisplay();
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
                        doneness = kvp.Value.doneness,
                        isInPan = kvp.Value.isInPan
                    });
                }
            }

            currentLogPath = saveManager.SaveLog(currentLog);
            currentReview = reviewManager.CreateLocalReview(currentDishId, currentRecipe, currentLog);
            saveManager.SaveReview(currentReview);

            // 直接进入保存界面（AI评价在保存时并行请求）
            saveDishUI.Show(currentReview, SaveDish);
            Hide(cookingUI);
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
            Hide(saveDishUI);

            // 后台异步请求 AI 评价
            var npcs = AIReviewService.LoadNPCs();
            if (npcs.Count > 0)
            {
                StartCoroutine(BackgroundAIReview(dish, npcs));
            }

            // 立即回到菜单
            ShowMenu();
        }

        private IEnumerator BackgroundAIReview(DishData dish, List<NPCData> npcs)
        {
            var recipe = currentRecipe;
            var log = LoadLog(dish.logPath);
            var baseReview = currentReview;

            yield return aiReviewService.CreateBatchReviews(dish, recipe, log, baseReview, npcs, reviews =>
            {
                var ids = new List<string>();
                foreach (var review in reviews)
                {
                    saveManager.SaveReview(review);
                    ids.Add(review.reviewId);
                }

                var avgScore = 0;
                foreach (var r in reviews) avgScore += r.score;
                avgScore /= reviews.Count;

                dish.score = avgScore;
                dish.reviewIds = ids;
                dish.reviewId = reviews[0].reviewId;
                dish.reviewText = reviews[0].summary;
                saveManager.SaveDish(dish);

                var totalDelta = 0;
                foreach (var r in reviews) totalDelta += r.reputationDelta;
                currentUser.reputation += totalDelta;
                saveManager.SaveUser(currentUser);
                statusBarUI?.Refresh(currentUser);

                Debug.Log($"[AI] 后台评价完成，{reviews.Count} 条已保存");
            });
        }

        public void ShowDishReviews(DishData dish)
        {
            // 加载该菜品所有保存的评价
            var reviews = new List<ReviewData>();

            if (dish.reviewIds != null)
            {
                foreach (var id in dish.reviewIds)
                {
                    var review = saveManager.LoadReview(id);
                    if (review != null) reviews.Add(review);
                }
            }

            if (reviews.Count == 0)
            {
                // 还没评价或只有本地评价，用菜品的本地评价兜底
                var localReview = saveManager.LoadReview(dish.reviewId);
                if (localReview != null) reviews.Add(localReview);
            }

            reviewUI.ShowMultiple(reviews, ShowMenu);
            Hide(menuUI);
        }

        public void ShowMenu()
        {
            var dishes = saveManager.LoadDishes(currentUser.userId);
            menuUI.ShowDishes(dishes, EnterChefMode, ShowDishReviews);
            Hide(saveDishUI);
            Hide(reviewUI);
        }

        public void ShowMenuFromRecipeSelect()
        {
            ShowMenu();
            Hide(recipeSelectUI);
        }

        // ── Free Roam helpers ──────────────────────────────

        private static string[] GatherAllIngredients(List<RecipeData> recipes)
        {
            var set = new HashSet<string>();
            if (recipes != null)
            {
                foreach (var r in recipes)
                {
                    if (r.ingredients != null)
                    {
                        foreach (var ing in r.ingredients)
                            set.Add(ing);
                    }
                }
            }
            var result = new string[set.Count];
            set.CopyTo(result);
            return result;
        }

        private IEnumerator DelayedExitIngredientSelect()
        {
            yield return new WaitForSeconds(1.5f);
            ingredientSelectUI.Hide();
            EnterFreeRoam();
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

        // ── 状态机 ──

        private bool TryApplyAction(string action, out DishState nextState)
        {
            nextState = currentState;

            if (action == "cut" && currentState == DishState.Raw)
            {
                nextState = DishState.Cut;
                return true;
            }

            // 下锅：Cut→Cooking（首次）或 Cooking 下再次下锅（不同食材）
            if (action == "put_in_pan" && (currentState == DishState.Cut || currentState == DishState.Cooking))
            {
                if (currentState == DishState.Cut)
                    nextState = DishState.Cooking;
                return true;
            }

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

        // ── 烹饪协程 ──

        private void EnsureCoroutineRunning()
        {
            if (cookingCoroutine == null && currentState == DishState.Cooking)
            {
                cookingCoroutine = StartCoroutine(CookingCoroutine());
            }
        }

        private void StopCookingCoroutine()
        {
            if (cookingCoroutine != null)
            {
                StopCoroutine(cookingCoroutine);
                cookingCoroutine = null;
            }
        }

        private IEnumerator CookingCoroutine()
        {
            cookingElapsed = 0f;
            Debug.Log($"[Cooking] 协程启动, 火力={currentFireLevel}, 食材数={ingredientStates?.Count ?? 0}");

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
                            continue;

                        // 只烹饪锅里的食材
                        if (!state.isInPan)
                            continue;

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

            cookingCoroutine = null;
            Debug.Log($"[Cooking] 协程结束, 总烹饪时间={cookingElapsed:F1}s");
        }

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

        // ── 容器显示 ──

        private void RefreshContainerDisplay()
        {
            bool hasPan = false, hasPlate = false;
            if (ingredientStates != null)
            {
                foreach (var state in ingredientStates.Values)
                {
                    if (state.isInPan) hasPan = true;
                    else if (state.hasActivated) hasPlate = true;
                    cookingUI.UpdateIngredientVisibility(
                        state.ingredientName, state.hasActivated, state.isInPan);
                }
            }
            cookingUI.UpdateContainerDisplay(hasPan, hasPlate);
        }

        // ── 辅助 ──

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
                return;

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
