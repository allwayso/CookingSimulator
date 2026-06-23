using System;
using System.Collections.Generic;
using System.Text;
using CookingSimulator.Models;
using UnityEngine;

namespace CookingSimulator.Services
{
    public class ReviewManager : MonoBehaviour
    {
        public ReviewData CreateLocalReview(string dishId, RecipeData recipe, CookingLog log)
        {
            var required = new HashSet<string>();
            foreach (var step in recipe.steps)
            {
                required.Add(step.action);
            }

            var completed = new HashSet<string>();
            foreach (var record in log.records)
            {
                completed.Add(record.action);
            }

            var missingCount = 0;
            foreach (var action in required)
            {
                if (!completed.Contains(action))
                {
                    missingCount++;
                }
            }

            var stepScore = Mathf.Clamp(100 - missingCount * 15, 0, 100);

            // 熟度评分
            int donenessScore = 0;
            var donenessDetail = new StringBuilder();

            if (log.ingredientResults != null && log.ingredientResults.Count > 0)
            {
                donenessDetail.Append(" | 熟度: ");
                var parts = new List<string>();

                foreach (var result in log.ingredientResults)
                {
                    int s = GetDonenessScore(result.doneness);
                    donenessScore += s;
                    parts.Add($"{result.ingredientName}{GetDonenessChinese(result.doneness)}({s:+0;-#})");
                }

                donenessDetail.Append(string.Join(", ", parts));
            }

            var score = Mathf.Clamp(stepScore + donenessScore, 0, 100);
            var reputationDelta = score >= 80 ? 3 : score >= 60 ? 1 : -2;
            var suggestion = missingCount == 0
                ? "流程完整，可以尝试提高速度和表现。"
                : "有关键步骤缺失，先保证按菜谱完成基础动作。";

            return new ReviewData
            {
                reviewId = Guid.NewGuid().ToString("N"),
                dishId = dishId,
                score = score,
                summary = $"本地评价：完成 {completed.Count}/{required.Count} 个关键动作，得分 {score}。{donenessDetail}",
                suggestion = suggestion,
                reputationDelta = reputationDelta,
                createdAt = DateTime.UtcNow.ToString("O")
            };
        }

        public ReviewData CreateLocalLaobaReview(DishData dish)
        {
            var score = Mathf.Clamp(dish.score - 5, 0, 100);
            var reputationDelta = score >= 80 ? 2 : score >= 60 ? 0 : -2;
            return new ReviewData
            {
                reviewId = Guid.NewGuid().ToString("N"),
                dishId = dish.dishId,
                score = score,
                summary = $"本地老八评价：{dish.name} 看着能吃，价格 {dish.price}，基础评分 {score}。",
                suggestion = "先保证流程完整，再让 AI 老八给更细的口味反馈。",
                reputationDelta = reputationDelta,
                createdAt = DateTime.UtcNow.ToString("O")
            };
        }

        /// <summary>
        /// 熟度评分：全熟 +10，半生 +5，全生 0，过头 -5
        /// </summary>
        private static int GetDonenessScore(DonenessLevel level)
        {
            switch (level)
            {
                case DonenessLevel.FullyCooked:
                    return 10;
                case DonenessLevel.HalfCooked:
                    return 5;
                case DonenessLevel.Raw:
                    return 0;
                case DonenessLevel.Overcooked:
                    return -5;
                default:
                    return 0;
            }
        }

        private static string GetDonenessChinese(DonenessLevel level)
        {
            switch (level)
            {
                case DonenessLevel.Raw:
                    return "全生";
                case DonenessLevel.HalfCooked:
                    return "半生";
                case DonenessLevel.FullyCooked:
                    return "全熟";
                case DonenessLevel.Overcooked:
                    return "过头";
                default:
                    return "未知";
            }
        }
    }
}
