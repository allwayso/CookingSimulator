using System;
using CookingSimulator.Models;
using UnityEngine;

namespace CookingSimulator.Services
{
    public class LogManager : MonoBehaviour
    {
        private CookingLog currentLog;
        private float startedAt;

        public CookingLog CurrentLog => currentLog;

        public void StartLog(UserData user, RecipeData recipe, string dishId)
        {
            startedAt = Time.time;
            currentLog = new CookingLog
            {
                logId = Guid.NewGuid().ToString("N"),
                userId = user.userId,
                dishId = dishId,
                recipeId = recipe.recipeId,
                startedAt = DateTime.UtcNow.ToString("O")
            };
        }

        public void AddAction(string action, string target, DishState before, DishState after)
        {
            if (currentLog == null)
            {
                return;
            }

            currentLog.records.Add(new ActionRecord
            {
                action = action,
                target = target,
                elapsedSeconds = Time.time - startedAt,
                stage = "cook",
                stateBefore = before,
                stateAfter = after
            });
        }

        public CookingLog Finish(DishState finalState)
        {
            currentLog.finishedAt = DateTime.UtcNow.ToString("O");
            currentLog.finalState = finalState;
            return currentLog;
        }
    }
}
