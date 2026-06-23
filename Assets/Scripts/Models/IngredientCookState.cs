using System;

namespace CookingSimulator.Models
{
    /// <summary>
    /// 运行时单个食材的烹饪状态追踪
    /// </summary>
    [Serializable]
    public class IngredientCookState
    {
        /// <summary>食材名称</summary>
        public string ingredientName;

        /// <summary>当前累计烹饪点数</summary>
        public float cookProgress;

        /// <summary>当前熟度</summary>
        public DonenessLevel doneness;

        /// <summary>是否在锅中（false = 在盘子上或未使用）</summary>
        public bool isInPan;
    }
}
