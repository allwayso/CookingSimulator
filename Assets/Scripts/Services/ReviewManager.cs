using System;
using System.Collections.Generic;
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

            var score = Mathf.Clamp(100 - missingCount * 15, 0, 100);
            var reputationDelta = score >= 80 ? 3 : score >= 60 ? 1 : -2;
            var suggestion = missingCount == 0 ? "流程完整，可以尝试提高速度和表现。" : "有关键步骤缺失，先保证按菜谱完成基础动作。";

            return new ReviewData
            {
                reviewId = Guid.NewGuid().ToString("N"),
                dishId = dishId,
                score = score,
                summary = $"本地评价：完成 {completed.Count}/{required.Count} 个关键动作，得分 {score}。",
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
    }
}
