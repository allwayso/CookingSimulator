using System;

namespace CookingSimulator.Models
{
    [Serializable]
    public class RecipeData
    {
        public string recipeId;
        public string name;
        public string description;
        public string[] ingredients;
        public string[] seasonings;
        public float[] timedPopupDelays;
        public RecipeStep[] steps;
        public IngredientCookingConfig[] ingredientCookingConfigs;
    }

    [Serializable]
    public class RecipeStep
    {
        public int order;
        public string action;
        public string target;
        public string hint;
    }
}
