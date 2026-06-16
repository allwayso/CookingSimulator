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
                reviewText = currentReview.summary,
                createdAt = DateTime.UtcNow.ToString("O")
            };
            saveManager.SaveDish(dish);
            ShowMenu();
        }

        public void ShowMenu()
        {
            var dishes = saveManager.LoadDishes(currentUser.userId);
            menuUI.Show(dishes, EnterChefMode);
            Hide(saveDishUI);
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

        private static void Hide(MonoBehaviour view)
        {
            view.gameObject.SetActive(false);
        }
    }
}
