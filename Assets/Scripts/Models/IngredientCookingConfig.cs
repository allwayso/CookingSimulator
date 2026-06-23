using System;

namespace CookingSimulator.Models
{
    /// <summary>
    /// 菜谱中每个食材的烹饪参数配置
    /// </summary>
    [Serializable]
    public class IngredientCookingConfig
    {
        /// <summary>食材名称，如 "番茄"、"鸡蛋"</summary>
        public string ingredientName;

        /// <summary>达到全熟所需的总烹饪点数（猛火 Lv4 下鸡蛋≈10s、番茄≈30s）</summary>
        public float fullCookThreshold;
    }
}
