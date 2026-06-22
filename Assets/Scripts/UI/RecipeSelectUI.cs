using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class RecipeSelectUI : MonoBehaviour
    {
        [SerializeField] private Text recipeText;

        private List<RecipeData> recipes;
        private Action<RecipeData> onRecipeSelected;
        private Action onOpenMenu;

        public void Show(List<RecipeData> availableRecipes, Action<RecipeData> recipeSelectedAction, Action openMenuAction)
        {
            recipes = availableRecipes;
            onRecipeSelected = recipeSelectedAction;
            onOpenMenu = openMenuAction;
            gameObject.SetActive(true);
            recipeText.text = recipes.Count == 0 ? "没有可用菜谱" : $"{recipes[0].name}\n{recipes[0].description}";
        }

        public void SelectFirstRecipe()
        {
            if (recipes == null || recipes.Count == 0)
            {
                return;
            }

            onRecipeSelected?.Invoke(recipes[0]);
        }

        public void OpenMenu()
        {
            onOpenMenu?.Invoke();
        }
    }
}
