using System;
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

        private UserData currentUser;
        private RecipeData currentRecipe;
        private CookingLog currentLog;
        private ReviewData currentReview;
        private DishState currentState;
        private string currentDishId;
        private string currentLogPath;

        private void Start()
        {
            ShowLogin();
        }

        public void Login(string username)
        {
            currentUser = saveManager.LoadOrCreateUser(username);
            modeSelectUI.Show(currentUser, EnterChefMode);
            Hide(loginUI);
        }

        public void EnterChefMode()
        {
            var recipes = recipeManager.LoadRecipes();
            recipeSelectUI.Show(recipes, StartCooking);
            Hide(modeSelectUI);
        }

        public void StartCooking(RecipeData recipe)
        {
            currentRecipe = recipe;
            currentDishId = Guid.NewGuid().ToString("N");
            currentState = DishState.Raw;
            logManager.StartLog(currentUser, currentRecipe, currentDishId);
            cookingUI.Show(currentRecipe, currentState, HandleCookingAction, FinishCooking);
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
            logManager.AddAction(action, target, before, after);
            cookingUI.UpdateState(currentState);
        }

        public void FinishCooking()
        {
            HandleCookingAction("finish", "菜品");
            currentLog = logManager.Finish(currentState);
            currentLogPath = saveManager.SaveLog(currentLog);
            currentReview = reviewManager.CreateLocalReview(currentDishId, currentRecipe, currentLog);
            saveManager.SaveReview(currentReview);

            currentUser.reputation += currentReview.reputationDelta;
            saveManager.SaveUser(currentUser);

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
            menuUI.Show(dishes, EnterChefMode, ShowLaobaReview);
            Hide(saveDishUI);
            Hide(reviewUI);
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

            if (action == "heat" && currentState == DishState.Cooking)
            {
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
                steps = Array.Empty<RecipeStep>()
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
            view.gameObject.SetActive(false);
        }
    }
}
