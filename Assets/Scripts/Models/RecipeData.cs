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
        /// <summary>旧版定时弹窗延迟，已被 timedHints 替代，保留用于反序列化兼容。</summary>
        public float[] timedPopupDelays;
        public RecipeStep[] steps;
        public IngredientCookingConfig[] ingredientCookingConfigs;
        /// <summary>新版定时弹窗提示，从 t=0（烹饪开始）计时。</summary>
        public TimedHint[] timedHints;
    }

    [Serializable]
    public class RecipeStep
    {
        public int order;
        public string action;
        public string target;
        public string hint;
    }

    /// <summary>
    /// 做菜过程中的定时弹窗提示。triggerTime 从 t=0（鸡蛋下锅/Cooking 状态开始）起算。
    /// condition 用于条件门控：为空则始终显示；"eggInPan"/"tomatoInPan"/"eggOnPlate" 等仅在条件满足时弹出。
    /// </summary>
    [Serializable]
    public class TimedHint
    {
        /// <summary>弹窗触发时间，从 t=0 起算（秒）</summary>
        public float triggerTime;
        /// <summary>弹窗持续显示时长（秒）</summary>
        public float duration;
        /// <summary>弹窗标题</summary>
        public string title;
        /// <summary>弹窗正文（详细操作说明）</summary>
        public string body;
        /// <summary>条件门控：""=始终, "eggInPan", "tomatoInPan", "eggOnPlate"</summary>
        public string condition;
    }
}
