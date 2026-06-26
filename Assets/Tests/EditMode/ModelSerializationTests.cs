using System.Collections.Generic;
using CookingSimulator.Models;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class ModelSerializationTests
    {
        // ── UserData ──

        [Test]
        public void UserData_RoundTrip_PreservesAllFields()
        {
            var original = new UserData
            {
                userId = "test_user_001",
                username = "测试厨师",
                passwordHash = string.Empty,
                reputation = 42,
                createdAt = "2026-06-25T10:00:00.0000000Z",
                lastLoginAt = "2026-06-25T12:00:00.0000000Z"
            };

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<UserData>(json);

            Assert.AreEqual(original.userId, restored.userId);
            Assert.AreEqual(original.username, restored.username);
            Assert.AreEqual(original.reputation, restored.reputation);
            Assert.AreEqual(original.createdAt, restored.createdAt);
            Assert.AreEqual(original.lastLoginAt, restored.lastLoginAt);
        }

        // ── CookingLog ──

        [Test]
        public void CookingLog_RoundTrip_PreservesRecords()
        {
            var original = new CookingLog
            {
                logId = "log_abc123",
                userId = "user_001",
                dishId = "dish_001",
                recipeId = "tomato_egg",
                startedAt = "2026-06-25T10:00:00Z",
                finishedAt = "2026-06-25T10:05:00Z",
                finalState = DishState.Done,
                records = new List<ActionRecord>
                {
                    new ActionRecord
                    {
                        action = "cut", target = "番茄",
                        elapsedSeconds = 2.5f, stage = "cook",
                        stateBefore = DishState.Raw, stateAfter = DishState.Cut
                    },
                    new ActionRecord
                    {
                        action = "put_in_pan", target = "鸡蛋",
                        elapsedSeconds = 5.0f, stage = "cook",
                        stateBefore = DishState.Cut, stateAfter = DishState.Cooking
                    },
                    new ActionRecord
                    {
                        action = "finish", target = "菜品",
                        elapsedSeconds = 45.0f, stage = "cook",
                        stateBefore = DishState.Seasoned, stateAfter = DishState.Done
                    }
                },
                ingredientResults = new List<IngredientCookState>
                {
                    new IngredientCookState
                    {
                        ingredientName = "番茄", cookProgress = 100f,
                        doneness = DonenessLevel.FullyCooked, isInPan = false
                    },
                    new IngredientCookState
                    {
                        ingredientName = "鸡蛋", cookProgress = 50f,
                        doneness = DonenessLevel.Overcooked, isInPan = false
                    }
                }
            };

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<CookingLog>(json);

            Assert.AreEqual(original.logId, restored.logId);
            Assert.AreEqual(original.userId, restored.userId);
            Assert.AreEqual(original.dishId, restored.dishId);
            Assert.AreEqual(original.recipeId, restored.recipeId);
            Assert.AreEqual(original.finalState, restored.finalState);
            Assert.AreEqual(3, restored.records.Count);

            // 验证第一条记录
            Assert.AreEqual("cut", restored.records[0].action);
            Assert.AreEqual("番茄", restored.records[0].target);
            Assert.AreEqual(2.5f, restored.records[0].elapsedSeconds);
            Assert.AreEqual(DishState.Raw, restored.records[0].stateBefore);
            Assert.AreEqual(DishState.Cut, restored.records[0].stateAfter);

            // 验证熟度结果
            Assert.AreEqual(2, restored.ingredientResults.Count);
            Assert.AreEqual("番茄", restored.ingredientResults[0].ingredientName);
            Assert.AreEqual(DonenessLevel.FullyCooked, restored.ingredientResults[0].doneness);
            Assert.AreEqual(100f, restored.ingredientResults[0].cookProgress);
        }

        // ── DishData ──

        [Test]
        public void DishData_RoundTrip_PreservesAllFields()
        {
            var original = new DishData
            {
                dishId = "dish_abc",
                userId = "user_001",
                name = "我的番茄炒蛋",
                price = 38.5f,
                score = 92,
                finalState = DishState.Done,
                logPath = "/Saves/Logs/log_abc.json",
                reviewId = "review_001",
                reviewText = "色香味俱全",
                reviewIds = new List<string> { "review_001", "review_002", "review_003" },
                createdAt = "2026-06-25T10:10:00.0000000Z"
            };

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<DishData>(json);

            Assert.AreEqual(original.dishId, restored.dishId);
            Assert.AreEqual(original.userId, restored.userId);
            Assert.AreEqual(original.name, restored.name);
            Assert.AreEqual(38.5f, restored.price);
            Assert.AreEqual(92, restored.score);
            Assert.AreEqual(DishState.Done, restored.finalState);
            Assert.AreEqual(original.logPath, restored.logPath);
            Assert.AreEqual(original.reviewId, restored.reviewId);
            Assert.AreEqual(original.reviewText, restored.reviewText);
            Assert.AreEqual(3, restored.reviewIds.Count);
            Assert.AreEqual("review_003", restored.reviewIds[2]);
        }

        // ── ReviewData ──

        [Test]
        public void ReviewData_RoundTrip_PreservesAllFields()
        {
            var original = new ReviewData
            {
                reviewId = "rev_xyz789",
                dishId = "dish_abc",
                reviewerName = "老八",
                reviewerId = "laoba",
                score = 88,
                summary = "这道菜整体不错，番茄炒得恰到好处，鸡蛋略老。",
                suggestion = "下次可以注意火候，鸡蛋不要炒太久。",
                reputationDelta = 3,
                createdAt = "2026-06-25T10:08:00.0000000Z"
            };

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<ReviewData>(json);

            Assert.AreEqual(original.reviewId, restored.reviewId);
            Assert.AreEqual(original.dishId, restored.dishId);
            Assert.AreEqual(original.reviewerName, restored.reviewerName);
            Assert.AreEqual(original.reviewerId, restored.reviewerId);
            Assert.AreEqual(88, restored.score);
            Assert.AreEqual(original.summary, restored.summary);
            Assert.AreEqual(original.suggestion, restored.suggestion);
            Assert.AreEqual(3, restored.reputationDelta);
            Assert.AreEqual(original.createdAt, restored.createdAt);
        }

        // ── 空集合边界 ──

        [Test]
        public void CookingLog_EmptyRecords_RoundTrips()
        {
            var original = new CookingLog
            {
                logId = "empty_log",
                records = new List<ActionRecord>(),
                ingredientResults = new List<IngredientCookState>()
            };

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<CookingLog>(json);

            Assert.AreEqual("empty_log", restored.logId);
            Assert.AreEqual(0, restored.records.Count);
            Assert.IsNull(restored.ingredientResults);
            // JsonUtility doesn't serialize empty List<T> → null on deserialize
        }

        [Test]
        public void DishData_NullReviewIds_RoundTrips()
        {
            var original = new DishData
            {
                dishId = "simple_dish",
                name = "简单菜",
                reviewIds = null
            };

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<DishData>(json);

            Assert.AreEqual("simple_dish", restored.dishId);
            Assert.IsNull(restored.reviewIds);
        }
    }
}
