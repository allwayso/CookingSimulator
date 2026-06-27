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
        [SerializeField] private ReviewWriteUI reviewWriteUI;
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
        private DishData currentFoodieDish;

        // 火力 & 熟度系统
        private int currentFireLevel;
        private float cookingElapsed;
        private Dictionary<string, IngredientCookState> ingredientStates;
        private IngredientCookingConfig[] currentCookConfigs;
        private Coroutine cookingCoroutine;

        private bool ingredientsReady;
        private RecipeData selectedRecipe;
        private int currentStepIndex;

        private void Start()
        {
            ShowLogin();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        public void Login(string username)
        {
            currentUser = saveManager.LoadOrCreateUser(username);
            statusBarUI?.Show(currentUser);
            modeSelectUI.Show(currentUser, EnterChefMode, EnterFoodieMode);
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
            currentStepIndex = 0;
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

            AdvanceStep(); // 显示第一步提示 + 自动选中食材
        }

        // ── 动作处理 ──

        public void HandleCookingAction(string action, string target)
        {
            // ── 步骤拦截：必须按菜谱 steps[] 顺序执行 ──
            if (currentRecipe == null || currentRecipe.steps == null || currentStepIndex >= currentRecipe.steps.Length)
            {
                cookingUI.ShowMessage("没有可执行的步骤");
                return;
            }
            var step = currentRecipe.steps[currentStepIndex];
            if (action != step.action)
            {
                cookingUI.ShowMessage($"当前应该：{step.hint}");
                return;
            }
            if (action == "put_in_pan" && target != step.target)
            {
                cookingUI.ShowMessage($"当前应该：{step.hint}");
                return;
            }

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
                cookingUI.StartTimedHints(currentRecipe.timedHints, ingredientStates);
            }
            // 离开 Cooking 状态时停止协程
            if (before == DishState.Cooking && after != DishState.Cooking)
            {
                StopCookingCoroutine();
                cookingUI.StopTimedHints();
            }

            logManager.AddAction(action, target, before, after);
            cookingUI.UpdateState(currentState);
            RefreshContainerDisplay();

            currentStepIndex++;
            AdvanceStep();
        }

        public void HandleFireLevelChanged(int level)
        {
            currentFireLevel = level;
        }

        /// <summary>步骤推进：显示下一步提示，put_in_pan 步骤自动选中食材。</summary>
        private void AdvanceStep()
        {
            if (currentRecipe == null || currentRecipe.steps == null || currentStepIndex >= currentRecipe.steps.Length)
                return;

            var next = currentRecipe.steps[currentStepIndex];
            cookingUI.SetStepHint(next.hint);

            if (next.action == "put_in_pan")
                cookingUI.SelectIngredient(next.target);
        }

        public void HandleTransferToPlate()
        {
            // ── 步骤拦截 ──
            if (currentRecipe == null || currentRecipe.steps == null || currentStepIndex >= currentRecipe.steps.Length)
            {
                cookingUI.ShowMessage("没有可执行的步骤");
                return;
            }
            var step = currentRecipe.steps[currentStepIndex];
            if (step.action != "transfer_to_plate")
            {
                cookingUI.ShowMessage($"当前应该：{step.hint}");
                return;
            }

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

            currentStepIndex++;
            AdvanceStep();
        }

        public void HandleTransferToPan()
        {
            // ── 步骤拦截 ──
            if (currentRecipe == null || currentRecipe.steps == null || currentStepIndex >= currentRecipe.steps.Length)
            {
                cookingUI.ShowMessage("没有可执行的步骤");
                return;
            }
            var step = currentRecipe.steps[currentStepIndex];
            if (step.action != "transfer_to_pan")
            {
                cookingUI.ShowMessage($"当前应该：{step.hint}");
                return;
            }

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

            currentStepIndex++;
            AdvanceStep();
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
            currentReview = ReviewManager.CreateLocalReview(currentDishId, currentRecipe, currentLog);
            saveManager.SaveReview(currentReview);

            // 直接进入保存界面（AI评价在保存时并行请求）
            saveDishUI.Show(currentReview, SaveDish, DiscardDish);
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
                objectiveScore = currentReview.score,
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

        public void DiscardDish()
        {
            Hide(saveDishUI);
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
            var reviews = LoadReviewsForDish(dish);
            reviewUI.ShowMultiple(reviews, ShowMenu);
            Hide(menuUI);
        }

        private List<ReviewData> LoadReviewsForDish(DishData dish)
        {
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
                var localReview = saveManager.LoadReview(dish.reviewId);
                if (localReview != null) reviews.Add(localReview);
            }

            return reviews;
        }

        // ── Foodie Mode ─────────────────────────────────────

        public static float GetCombinedScore(DishData dish, int foodieScore)
        {
            int objScore = dish.objectiveScore != 0 ? dish.objectiveScore : 80;
            return objScore * 0.8f + foodieScore * 0.2f;
        }

        public static int CalculateLifeDelta(float combinedScore)
        {
            int raw = Mathf.FloorToInt((combinedScore - 80) / 5f);
            return raw + (raw >= 0 ? 1 : -1);
        }

        public void EnterFoodieMode()
        {
            var allDishes = saveManager.LoadAllDishes();
            menuUI.ShowDishes(allDishes, null, ShowFoodieDishReviews, "食单");
            statusBarUI?.ShowFoodie(currentUser);
            Hide(modeSelectUI);
            SetLoginBackgroundVisible(false);
        }

        public void ShowFoodieDishReviews(DishData dish)
        {
            currentFoodieDish = dish;
            var reviews = LoadReviewsForDish(dish);
            reviewUI.ShowMultiple(reviews, EnterReviewWrite, "写评价");
            Hide(menuUI);
        }

        public void EnterReviewWrite()
        {
            Hide(reviewUI);
            reviewWriteUI.Show(currentFoodieDish, SubmitFoodieReview, ReturnToFoodieMenu);
        }

        public void SubmitFoodieReview(int foodieScore, string comment)
        {
            var review = new ReviewData
            {
                reviewId = Guid.NewGuid().ToString("N"),
                dishId = currentFoodieDish.dishId,
                reviewerName = currentUser.username,
                reviewerId = currentUser.userId,
                score = foodieScore,
                summary = comment,
                suggestion = $"来自美食家 {currentUser.username} 的评价",
                reputationDelta = 0,
                createdAt = DateTime.UtcNow.ToString("O")
            };
            saveManager.SaveReview(review);

            if (currentFoodieDish.reviewIds == null)
                currentFoodieDish.reviewIds = new List<string>();
            currentFoodieDish.reviewIds.Add(review.reviewId);

            // 重算口碑分（所有评价的平均）
            var allReviews = LoadReviewsForDish(currentFoodieDish);
            int totalScore = 0;
            foreach (var r in allReviews) totalScore += r.score;
            currentFoodieDish.score = allReviews.Count > 0 ? totalScore / allReviews.Count : foodieScore;
            currentFoodieDish.reviewText = comment;
            saveManager.SaveDish(currentFoodieDish);

            // 生命值变化
            float combined = GetCombinedScore(currentFoodieDish, foodieScore);
            int lifeDelta = CalculateLifeDelta(combined);
            currentUser.lifeValue += lifeDelta;
            saveManager.SaveUser(currentUser);
            statusBarUI?.RefreshFoodie(currentUser);

            Debug.Log($"[Foodie] 评价菜品: {currentFoodieDish.name}, 客观分={currentFoodieDish.objectiveScore}, "
                + $"主观分={foodieScore}, combined={combined:F1}, lifeDelta={lifeDelta:+0;-#}, lifeValue={currentUser.lifeValue}");

            Hide(reviewWriteUI);
            ReturnToFoodieMenu();
        }

        public void ReturnToFoodieMenu()
        {
            Hide(reviewUI);
            var allDishes = saveManager.LoadAllDishes();
            menuUI.ShowDishes(allDishes, null, ShowFoodieDishReviews, "食单");
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

        /// <summary>纯逻辑状态转换检查，可直接用于单元测试。</summary>
        public static bool TryApplyAction(DishState currentState, string action, out DishState nextState)
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

        private bool TryApplyAction(string action, out DishState nextState)
        {
            return TryApplyAction(currentState, action, out nextState);
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

        public static DonenessLevel CalculateDoneness(float progress, float fullThreshold)
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
