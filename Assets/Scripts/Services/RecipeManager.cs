using System.Collections.Generic;
using System.IO;
using CookingSimulator.Models;
using UnityEngine;

namespace CookingSimulator.Services
{
    public class RecipeManager : MonoBehaviour
    {
        public List<RecipeData> LoadRecipes()
        {
            var recipes = new List<RecipeData>();
            var root = Path.Combine(Application.streamingAssetsPath, "Recipes");
            if (!Directory.Exists(root))
            {
                return recipes;
            }

            foreach (var path in Directory.GetFiles(root, "*.json"))
            {
                var recipe = JsonUtility.FromJson<RecipeData>(File.ReadAllText(path));
                if (!string.IsNullOrWhiteSpace(recipe.recipeId))
                {
                    recipes.Add(recipe);
                }
            }

            return recipes;
        }
    }
}
