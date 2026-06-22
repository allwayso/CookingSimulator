using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSimulator.UI
{
    public class RecipeSelectUI : MonoBehaviour
    {
        [SerializeField] private Transform recipeButtonRoot;
        [SerializeField] private Button recipeButtonTemplate;

        private List<RecipeData> recipes;
        private Action<RecipeData> onRecipeSelected;
        private Action onOpenMenu;

        public void Show(List<RecipeData> availableRecipes, Action<RecipeData> recipeSelectedAction, Action openMenuAction)
        {
            recipes = availableRecipes;
            onRecipeSelected = recipeSelectedAction;
            onOpenMenu = openMenuAction;
            gameObject.SetActive(true);

            ClearRecipeButtons();

            if (recipes == null || recipes.Count == 0)
                return;

            foreach (var recipe in recipes)
            {
                var button = Instantiate(recipeButtonTemplate, recipeButtonRoot);
                button.gameObject.SetActive(true);
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = recipe.name;
                button.onClick.AddListener(() => onRecipeSelected?.Invoke(recipe));
            }
        }

        public void OpenMenu()
        {
            onOpenMenu?.Invoke();
        }

        private void ClearRecipeButtons()
        {
            if (recipeButtonTemplate != null)
                recipeButtonTemplate.gameObject.SetActive(false);

            if (recipeButtonRoot == null)
                return;

            for (int i = recipeButtonRoot.childCount - 1; i >= 0; i--)
            {
                var child = recipeButtonRoot.GetChild(i);
                if (recipeButtonTemplate != null && child == recipeButtonTemplate.transform)
                    continue;
                Destroy(child.gameObject);
            }
        }
    }
}
