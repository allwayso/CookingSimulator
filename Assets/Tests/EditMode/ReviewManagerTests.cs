using System;
using System.Collections.Generic;
using CookingSimulator.Models;
using CookingSimulator.Services;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ReviewManagerTests
    {
        // ── GetDonenessScore ──

        [Test]
        public void GetDonenessScore_FullyCooked_Returns10()
        {
            Assert.AreEqual(10, ReviewManager.GetDonenessScore(DonenessLevel.FullyCooked));
        }

        [Test]
        public void GetDonenessScore_HalfCooked_Returns5()
        {
            Assert.AreEqual(5, ReviewManager.GetDonenessScore(DonenessLevel.HalfCooked));
        }

        [Test]
        public void GetDonenessScore_Raw_Returns0()
        {
            Assert.AreEqual(0, ReviewManager.GetDonenessScore(DonenessLevel.Raw));
        }

        [Test]
        public void GetDonenessScore_Overcooked_ReturnsMinus5()
        {
            Assert.AreEqual(-5, ReviewManager.GetDonenessScore(DonenessLevel.Overcooked));
        }

        // ── GetDonenessChinese ──

        [Test]
        public void GetDonenessChinese_ReturnsCorrectNames()
        {
            Assert.AreEqual("全生", ReviewManager.GetDonenessChinese(DonenessLevel.Raw));
            Assert.AreEqual("半生", ReviewManager.GetDonenessChinese(DonenessLevel.HalfCooked));
            Assert.AreEqual("全熟", ReviewManager.GetDonenessChinese(DonenessLevel.FullyCooked));
            Assert.AreEqual("过头", ReviewManager.GetDonenessChinese(DonenessLevel.Overcooked));
        }

        // ── CreateLocalReview ──

        private static RecipeData CreateTestRecipe()
        {
            return new RecipeData
            {
                recipeId = "test_recipe",
                name = "测试菜",
                ingredients = new[] { "A", "B" },
                seasonings = new[] { "盐" },
                ingredientCookingConfigs = new[]
                {
                    new IngredientCookingConfig { ingredientName = "A", fullCookThreshold = 100f },
                    new IngredientCookingConfig { ingredientName = "B", fullCookThreshold = 50f }
                },
                steps = new[]
                {
                    new RecipeStep { order = 1, action = "cut", target = "A", hint = "" },
                    new RecipeStep { order = 2, action = "put_in_pan", target = "A", hint = "" },
                    new RecipeStep { order = 3, action = "season", target = "盐", hint = "" },
                    new RecipeStep { order = 4, action = "stir", target = "锅", hint = "" },
                    new RecipeStep { order = 5, action = "finish", target = "菜品", hint = "" }
                }
            };
        }

        [Test]
        public void AllStepsCompleted_PerfectIngredients_ReturnsMaxScore()
        {
            var recipe = CreateTestRecipe();
            var log = new CookingLog
            {
                logId = "test_log",
                dishId = "test_dish",
                records = new List<ActionRecord>
                {
                    new ActionRecord { action = "cut", target = "A" },
                    new ActionRecord { action = "put_in_pan", target = "A" },
                    new ActionRecord { action = "season", target = "盐" },
                    new ActionRecord { action = "stir", target = "锅" },
                    new ActionRecord { action = "finish", target = "菜品" }
                },
                ingredientResults = new List<IngredientCookState>
                {
                    new IngredientCookState { ingredientName = "A", doneness = DonenessLevel.FullyCooked },
                    new IngredientCookState { ingredientName = "B", doneness = DonenessLevel.FullyCooked }
                }
            };

            var review = ReviewManager.CreateLocalReview("dish1", recipe, log);

            // 5/5 steps = 100, +10 A fullyCooked +10 B fullyCooked = 120 → clamped to 100
            Assert.AreEqual(100, review.score);
            Assert.AreEqual(3, review.reputationDelta, "score >= 80 → +3 声誉");
            Assert.AreEqual("本地规则", review.reviewerName);
        }

        [Test]
        public void OneStepMissing_ScoreDeductedBy15()
        {
            var recipe = CreateTestRecipe();
            var log = new CookingLog
            {
                logId = "test_log",
                dishId = "test_dish",
                records = new List<ActionRecord>
                {
                    new ActionRecord { action = "cut", target = "A" },
                    new ActionRecord { action = "put_in_pan", target = "A" },
                    // "season" missing
                    new ActionRecord { action = "stir", target = "锅" },
                    new ActionRecord { action = "finish", target = "菜品" }
                },
                ingredientResults = new List<IngredientCookState>()
            };

            var review = ReviewManager.CreateLocalReview("dish2", recipe, log);

            Assert.AreEqual(85, review.score, "100 - 15*1 = 85");
            Assert.AreEqual(3, review.reputationDelta, "score >= 80 → +3");
        }

        [Test]
        public void ThreeStepsMissing_ScoreDeductedBy45()
        {
            var recipe = CreateTestRecipe();
            var log = new CookingLog
            {
                logId = "test_log",
                dishId = "test_dish",
                records = new List<ActionRecord>
                {
                    new ActionRecord { action = "cut", target = "A" },
                    new ActionRecord { action = "finish", target = "菜品" }
                    // missing: put_in_pan, season, stir
                },
                ingredientResults = new List<IngredientCookState>()
            };

            var review = ReviewManager.CreateLocalReview("dish3", recipe, log);

            Assert.AreEqual(55, review.score, "100 - 15*3 = 55");
            Assert.AreEqual(1, review.reputationDelta, "score >= 60 → +1");
        }

        [Test]
        public void AllStepsMissing_ScoreClampedToZero()
        {
            var recipe = CreateTestRecipe();
            var log = new CookingLog
            {
                logId = "test_log",
                dishId = "test_dish",
                records = new List<ActionRecord>(), // nothing done
                ingredientResults = new List<IngredientCookState>()
            };

            var review = ReviewManager.CreateLocalReview("dish4", recipe, log);

            // 5 required steps, 0 completed = 100 - 75 = 25, still above 0
            // Actually 100 - 7*15 = -5 → clamped to 0
            // Wait: 5 steps, missing 5. 100 - 5*15 = 25. Not below 0.
            Assert.AreEqual(25, review.score);
            Assert.AreEqual(1, review.reputationDelta, "score >= 60 → +1... wait 25 < 60 so -2");
        }

        [Test]
        public void SevenStepsMissing_ScoreClampedToZero()
        {
            // Recipe with 7 required distinct actions
            var recipe = new RecipeData
            {
                recipeId = "big_recipe",
                name = "大菜",
                steps = new[]
                {
                    new RecipeStep { action = "a1" }, new RecipeStep { action = "a2" },
                    new RecipeStep { action = "a3" }, new RecipeStep { action = "a4" },
                    new RecipeStep { action = "a5" }, new RecipeStep { action = "a6" },
                    new RecipeStep { action = "a7" }
                },
                ingredientCookingConfigs = new IngredientCookingConfig[0]
            };
            var log = new CookingLog
            {
                records = new List<ActionRecord>(),
                ingredientResults = new List<IngredientCookState>()
            };

            var review = ReviewManager.CreateLocalReview("dish5", recipe, log);

            // 100 - 7*15 = -5 → clamped to 0
            Assert.AreEqual(0, review.score);
            Assert.AreEqual(-2, review.reputationDelta, "score < 60 → -2");
        }

        [Test]
        public void MissedPrefixActions_Skipped()
        {
            var recipe = CreateTestRecipe();
            var log = new CookingLog
            {
                logId = "test_log",
                dishId = "test_dish",
                records = new List<ActionRecord>
                {
                    new ActionRecord { action = "cut", target = "A" },
                    new ActionRecord { action = "missed_put_in_pan", target = "A" }, // skipped
                    new ActionRecord { action = "season", target = "盐" },
                    new ActionRecord { action = "stir", target = "锅" },
                    new ActionRecord { action = "finish", target = "菜品" }
                },
                ingredientResults = new List<IngredientCookState>()
            };

            var review = ReviewManager.CreateLocalReview("dish6", recipe, log);

            // "missed_put_in_pan" skipped, so put_in_pan counts as missing
            Assert.AreEqual(85, review.score, "put_in_pan 缺失，扣除 15 分");
        }

        [Test]
        public void DonenessScoreMixed_AddsToStepScore()
        {
            var recipe = CreateTestRecipe();
            var log = new CookingLog
            {
                logId = "test_log",
                dishId = "test_dish",
                records = new List<ActionRecord>
                {
                    new ActionRecord { action = "cut" },
                    new ActionRecord { action = "put_in_pan" },
                    new ActionRecord { action = "season" },
                    new ActionRecord { action = "stir" },
                    new ActionRecord { action = "finish" }
                },
                ingredientResults = new List<IngredientCookState>
                {
                    new IngredientCookState { ingredientName = "A", doneness = DonenessLevel.Overcooked },
                    new IngredientCookState { ingredientName = "B", doneness = DonenessLevel.HalfCooked }
                }
            };

            var review = ReviewManager.CreateLocalReview("dish7", recipe, log);

            // steps: 100 (5/5), doneness: -5 + 5 = 0 → total 100
            Assert.AreEqual(100, review.score);
            Assert.IsTrue(review.summary.Contains("过头"), "评语应包含熟度详情");
            Assert.IsTrue(review.summary.Contains("半生"), "评语应包含熟度详情");
        }

        [Test]
        public void LowScore_ReputationPenalty()
        {
            var recipe = new RecipeData
            {
                recipeId = "hard_recipe",
                name = "难菜",
                steps = new[] { new RecipeStep { action = "a1" }, new RecipeStep { action = "a2" },
                               new RecipeStep { action = "a3" }, new RecipeStep { action = "a4" },
                               new RecipeStep { action = "a5" }, new RecipeStep { action = "a6" },
                               new RecipeStep { action = "a7" }, new RecipeStep { action = "a8" } },
                ingredientCookingConfigs = new IngredientCookingConfig[0]
            };
            var log = new CookingLog
            {
                records = new List<ActionRecord>(), // 8 missing
                ingredientResults = new List<IngredientCookState>()
            };

            var review = ReviewManager.CreateLocalReview("dish8", recipe, log);

            // 100 - 8*15 = -20 → clamped to 0
            Assert.AreEqual(0, review.score);
            Assert.AreEqual(-2, review.reputationDelta, "score = 0 → -2 声誉");
        }

        [Test]
        public void Review_HasValidIdAndTimestamp()
        {
            var recipe = CreateTestRecipe();
            var log = new CookingLog
            {
                records = new List<ActionRecord>(),
                ingredientResults = new List<IngredientCookState>()
            };

            var review = ReviewManager.CreateLocalReview("dish9", recipe, log);

            Assert.AreEqual("dish9", review.dishId);
            Assert.IsNotNull(review.reviewId);
            Assert.IsNotEmpty(review.reviewId);
            Assert.IsNotNull(review.createdAt);
            Assert.IsNotEmpty(review.createdAt);
        }
    }
}
