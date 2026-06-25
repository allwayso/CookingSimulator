using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CookingSimulator.Core;
using CookingSimulator.Models;
using CookingSimulator.Services;
using UnityEditor;
using UnityEngine;

namespace CookingSimulator.Editor
{
    /// <summary>
    /// 纯逻辑层测试运行器。通过 -executeMethod CookingSimulator.Editor.LogicTestRunner.RunAll 调用。
    /// 输出结果到 test-logic-results.json。
    /// </summary>
    public static class LogicTestRunner
    {
        private static int _passed, _failed;
        private static readonly List<TestResult> Results = new List<TestResult>();

        private struct TestResult
        {
            public string id;
            public string name;
            public bool passed;
            public string expected;
            public string actual;
            public string error;
        }

        [MenuItem("Cooking Simulator/Run Logic Tests")]
        public static void RunFromMenu() => RunAll();

        public static void RunAll()
        {
            _passed = 0; _failed = 0;
            Results.Clear();

            Debug.Log("===== 纯逻辑层自动测试开始 =====\n");

            TestStateMachine();
            TestDonenessCalculation();
            TestReviewScoring();
            TestModelSerialization();

            // ── 写入结果文件 ──
            var sb = new StringBuilder();
            sb.AppendLine("{ \"tests\": [");
            for (var i = 0; i < Results.Count; i++)
            {
                var r = Results[i];
                sb.Append("  {");
                sb.Append($"\"id\":\"{r.id}\",");
                sb.Append($"\"name\":\"{EscapeJson(r.name)}\",");
                sb.Append($"\"passed\":{r.passed.ToString().ToLower()},");
                sb.Append($"\"expected\":\"{EscapeJson(r.expected)}\",");
                sb.Append($"\"actual\":\"{EscapeJson(r.actual)}\",");
                sb.Append($"\"error\":\"{EscapeJson(r.error)}\"");
                sb.Append(i < Results.Count - 1 ? "}," : "}");
                sb.AppendLine();
            }
            sb.AppendLine("]}");

            var path = Path.Combine(Application.dataPath, "..", "test-logic-results.json");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"结果已写入: {path}");

            Debug.Log($"\n===== 测试完成: {_passed} 通过, {_failed} 失败, {Results.Count} 总计 =====\n");

            if (_failed > 0)
                EditorApplication.Exit(1);
        }

        private static void Assert(string id, string name, bool condition, string expected, string actual)
        {
            var result = new TestResult
            {
                id = id, name = name, passed = condition, expected = expected, actual = actual, error = condition ? "" : $"期望: {expected}, 实际: {actual}"
            };
            Results.Add(result);

            if (condition)
            {
                _passed++;
                Debug.Log($"  [PASS] {id}: {name}");
            }
            else
            {
                _failed++;
                Debug.LogError($"  [FAIL] {id}: {name} — 期望: {expected}, 实际: {actual}");
            }
        }

        private static string EscapeJson(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");

        // ═══════════════════════════════════════════════════════════════
        //  T-COOK — 状态机测试
        // ═══════════════════════════════════════════════════════════════

        private static void TestStateMachine()
        {
            Debug.Log("── 状态机测试 ──");

            // Valid transitions
            Check("T-COOK-01a", "Raw + cut → Cut",
                GameManager.TryApplyAction(DishState.Raw, "cut", out var s1) && s1 == DishState.Cut);

            Check("T-COOK-01b", "Cut + put_in_pan → Cooking",
                GameManager.TryApplyAction(DishState.Cut, "put_in_pan", out var s2) && s2 == DishState.Cooking);

            Check("T-COOK-01c", "Cooking + put_in_pan → Cooking (stays)",
                GameManager.TryApplyAction(DishState.Cooking, "put_in_pan", out var s3) && s3 == DishState.Cooking);

            Check("T-COOK-01d", "Cooking + season → Seasoned",
                GameManager.TryApplyAction(DishState.Cooking, "season", out var s4) && s4 == DishState.Seasoned);

            Check("T-COOK-01e", "Seasoned + stir → Seasoned (stays)",
                GameManager.TryApplyAction(DishState.Seasoned, "stir", out var s5) && s5 == DishState.Seasoned);

            Check("T-COOK-01f", "Seasoned + finish → Done",
                GameManager.TryApplyAction(DishState.Seasoned, "finish", out var s6) && s6 == DishState.Done);

            // Invalid transitions — each returns false and state unchanged
            var invalidTests = new (string id, string name, DishState state, string action)[]
            {
                ("T-COOK-02a", "Raw + put_in_pan → blocked", DishState.Raw, "put_in_pan"),
                ("T-COOK-02b", "Raw + season → blocked", DishState.Raw, "season"),
                ("T-COOK-02c", "Raw + stir → blocked", DishState.Raw, "stir"),
                ("T-COOK-02d", "Raw + finish → blocked", DishState.Raw, "finish"),
                ("T-COOK-02e", "Cut + season → blocked", DishState.Cut, "season"),
                ("T-COOK-02f", "Cut + stir → blocked", DishState.Cut, "stir"),
                ("T-COOK-02g", "Cut + finish → blocked", DishState.Cut, "finish"),
                ("T-COOK-02h", "Cooking + stir → blocked", DishState.Cooking, "stir"),
                ("T-COOK-02i", "Cooking + finish → blocked", DishState.Cooking, "finish"),
                ("T-COOK-02j", "Done + cut → blocked", DishState.Done, "cut"),
                ("T-COOK-02k", "Done + finish → blocked", DishState.Done, "finish"),
            };

            foreach (var t in invalidTests)
            {
                var ok = GameManager.TryApplyAction(t.state, t.action, out var next);
                Check(t.id, t.name, !ok && next == t.state);
            }

            // Full recipe flow
            var fullOk = GameManager.TryApplyAction(DishState.Raw, "cut", out var fs1) && fs1 == DishState.Cut
                && GameManager.TryApplyAction(fs1, "put_in_pan", out var fs2) && fs2 == DishState.Cooking
                && GameManager.TryApplyAction(fs2, "put_in_pan", out var fs3) && fs3 == DishState.Cooking
                && GameManager.TryApplyAction(fs3, "season", out var fs4) && fs4 == DishState.Seasoned
                && GameManager.TryApplyAction(fs4, "stir", out var fs5) && fs5 == DishState.Seasoned
                && GameManager.TryApplyAction(fs5, "finish", out var fs6) && fs6 == DishState.Done;
            Check("T-COOK-01g", "完整流程 Raw → Done", fullOk);
        }

        // ═══════════════════════════════════════════════════════════════
        //  T-DONENESS — 熟度计算测试
        // ═══════════════════════════════════════════════════════════════

        private static void TestDonenessCalculation()
        {
            Debug.Log("── 熟度计算测试 ──");

            Check("T-DONENESS-01a", "progress=0 → Raw",
                GameManager.CalculateDoneness(0f, 120f) == DonenessLevel.Raw);

            Check("T-DONENESS-01b", "ratio=0.249 (边界) → Raw",
                GameManager.CalculateDoneness(29.99f, 120f) == DonenessLevel.Raw);

            Check("T-DONENESS-01c", "ratio=0.25 (边界) → HalfCooked",
                GameManager.CalculateDoneness(30f, 120f) == DonenessLevel.HalfCooked);

            Check("T-DONENESS-01d", "ratio=0.624 → HalfCooked",
                GameManager.CalculateDoneness(74.99f, 120f) == DonenessLevel.HalfCooked);

            Check("T-DONENESS-01e", "ratio=0.625 (边界) → FullyCooked",
                GameManager.CalculateDoneness(75f, 120f) == DonenessLevel.FullyCooked);

            Check("T-DONENESS-01f", "ratio=1.0 (边界) → FullyCooked",
                GameManager.CalculateDoneness(120f, 120f) == DonenessLevel.FullyCooked);

            Check("T-DONENESS-01g", "ratio=1.008 → Overcooked",
                GameManager.CalculateDoneness(121f, 120f) == DonenessLevel.Overcooked);

            Check("T-DONENESS-01h", "threshold=0 → FullyCooked",
                GameManager.CalculateDoneness(0f, 0f) == DonenessLevel.FullyCooked);

            // 鸡蛋 (threshold=40)
            Check("T-DONENESS-01i", "鸡蛋 10s (0.25) → HalfCooked",
                GameManager.CalculateDoneness(10f, 40f) == DonenessLevel.HalfCooked);

            Check("T-DONENESS-01j", "鸡蛋 25s (0.625) → FullyCooked",
                GameManager.CalculateDoneness(25f, 40f) == DonenessLevel.FullyCooked);

            Check("T-DONENESS-01k", "鸡蛋 41s → Overcooked",
                GameManager.CalculateDoneness(41f, 40f) == DonenessLevel.Overcooked);
        }

        // ═══════════════════════════════════════════════════════════════
        //  T-REVIEW — 评价评分测试
        // ═══════════════════════════════════════════════════════════════

        private static void TestReviewScoring()
        {
            Debug.Log("── 评价评分测试 ──");

            // Doneness score mapping
            Check("T-REVIEW-01a", "FullyCooked → +10",
                ReviewManager.GetDonenessScore(DonenessLevel.FullyCooked) == 10);
            Check("T-REVIEW-01b", "HalfCooked → +5",
                ReviewManager.GetDonenessScore(DonenessLevel.HalfCooked) == 5);
            Check("T-REVIEW-01c", "Raw → 0",
                ReviewManager.GetDonenessScore(DonenessLevel.Raw) == 0);
            Check("T-REVIEW-01d", "Overcooked → -5",
                ReviewManager.GetDonenessScore(DonenessLevel.Overcooked) == -5);

            // Doneness Chinese names
            Check("T-REVIEW-01e", "熟度中文名",
                ReviewManager.GetDonenessChinese(DonenessLevel.FullyCooked) == "全熟" &&
                ReviewManager.GetDonenessChinese(DonenessLevel.HalfCooked) == "半生" &&
                ReviewManager.GetDonenessChinese(DonenessLevel.Raw) == "全生" &&
                ReviewManager.GetDonenessChinese(DonenessLevel.Overcooked) == "过头");

            // Perfect review: all steps + perfect doneness
            var perfectRecipe = MakeRecipe(5);
            var perfectLog = new CookingLog
            {
                records = new List<ActionRecord>
                {
                    R("a1"), R("a2"), R("a3"), R("a4"), R("a5")
                },
                ingredientResults = new List<IngredientCookState>
                {
                    S("鸡蛋", DonenessLevel.FullyCooked),
                    S("番茄", DonenessLevel.FullyCooked)
                }
            };
            var review1 = ReviewManager.CreateLocalReview("d1", perfectRecipe, perfectLog);
            Check("T-REVIEW-02a", "全完成+全熟 → score=100",
                review1.score == 100 && review1.reputationDelta == 3);

            // Missing 1 step
            var log2 = new CookingLog
            {
                records = new List<ActionRecord> { R("a1"), R("a2"), R("a3"), R("a4") }, // a5 missing
                ingredientResults = new List<IngredientCookState>()
            };
            var review2 = ReviewManager.CreateLocalReview("d2", MakeRecipe(5), log2);
            Check("T-REVIEW-02b", "缺1步 → score=85",
                review2.score == 85 && review2.reputationDelta == 3);

            // Missing 3 steps
            var log3 = new CookingLog
            {
                records = new List<ActionRecord> { R("a1"), R("a2") }, // 3 missing
                ingredientResults = new List<IngredientCookState>()
            };
            var review3 = ReviewManager.CreateLocalReview("d3", MakeRecipe(5), log3);
            Check("T-REVIEW-02c", "缺3步 → score=55",
                review3.score == 55 && review3.reputationDelta == -2); // 55 < 60 → -2

            // Very low score
            var r8 = new RecipeData
            {
                recipeId = "r8", name = "r8",
                steps = new[] { S("a1"), S("a2"), S("a3"), S("a4"), S("a5"), S("a6"), S("a7"), S("a8") },
                ingredientCookingConfigs = new IngredientCookingConfig[0]
            };
            var log8 = new CookingLog
            {
                records = new List<ActionRecord>(), // 8 missing → -120, clamped to 0
                ingredientResults = new List<IngredientCookState>()
            };
            var review8 = ReviewManager.CreateLocalReview("d8", r8, log8);
            Check("T-REVIEW-02d", "全缺(8步) → score clamped to 0",
                review8.score == 0 && review8.reputationDelta == -2);

            // "missed_" prefix skipped
            var logMissed = new CookingLog
            {
                records = new List<ActionRecord>
                {
                    R("a1"), R("missed_a2"), R("a3"), R("a4"), R("a5")
                    // "missed_a2" skipped, so a2 counts as missing
                },
                ingredientResults = new List<IngredientCookState>()
            };
            var reviewMissed = ReviewManager.CreateLocalReview("dm", MakeRecipe(5), logMissed);
            Check("T-REVIEW-02e", "missed_ 前缀被跳过 → 缺1步 score=85",
                reviewMissed.score == 85);

            // Mixed doneness
            var logMixed = new CookingLog
            {
                records = new List<ActionRecord> { R("a1"), R("a2"), R("a3"), R("a4"), R("a5") },
                ingredientResults = new List<IngredientCookState>
                {
                    S("鸡蛋", DonenessLevel.Overcooked),  // -5
                    S("番茄", DonenessLevel.HalfCooked)   // +5
                }
            };
            var reviewMixed = ReviewManager.CreateLocalReview("dmx", MakeRecipe(5), logMixed);
            Check("T-REVIEW-02f", "熟度混合(Overcooked+HalfCooked) → score=100",
                reviewMixed.score == 100); // 100 steps + 0 doneness = 100
        }

        // ═══════════════════════════════════════════════════════════════
        //  T-PERSIST — JSON 序列化测试
        // ═══════════════════════════════════════════════════════════════

        private static void TestModelSerialization()
        {
            Debug.Log("── JSON 序列化测试 ──");

            // UserData
            var user = new UserData
            {
                userId = "u1", username = "测试厨师", reputation = 42,
                createdAt = "2026-06-25T10:00:00Z", lastLoginAt = "2026-06-25T12:00:00Z"
            };
            var userBack = JsonUtility.FromJson<UserData>(JsonUtility.ToJson(user));
            Check("T-PERSIST-02a", "UserData 序列化往返",
                userBack.userId == "u1" && userBack.username == "测试厨师" && userBack.reputation == 42);

            // CookingLog with records
            var log = new CookingLog
            {
                logId = "log1", dishId = "d1", finalState = DishState.Done,
                records = new List<ActionRecord>
                {
                    new ActionRecord { action = "cut", target = "番茄", elapsedSeconds = 2.5f,
                        stage = "cook", stateBefore = DishState.Raw, stateAfter = DishState.Cut },
                    new ActionRecord { action = "finish", target = "菜品", elapsedSeconds = 45f,
                        stage = "cook", stateBefore = DishState.Seasoned, stateAfter = DishState.Done }
                },
                ingredientResults = new List<IngredientCookState>
                {
                    new IngredientCookState { ingredientName = "番茄", cookProgress = 100f,
                        doneness = DonenessLevel.FullyCooked }
                }
            };
            var logBack = JsonUtility.FromJson<CookingLog>(JsonUtility.ToJson(log));
            Check("T-PERSIST-02b", "CookingLog 序列化往返",
                logBack.logId == "log1" && logBack.records.Count == 2 &&
                logBack.records[0].action == "cut" && logBack.records[1].action == "finish");

            Check("T-PERSIST-02c", "CookingLog 熟度结果",
                logBack.ingredientResults.Count == 1 &&
                logBack.ingredientResults[0].doneness == DonenessLevel.FullyCooked);

            // DishData
            var dish = new DishData
            {
                dishId = "d1", userId = "u1", name = "番茄炒蛋", price = 38.5f,
                score = 92, finalState = DishState.Done, reviewIds = new List<string> { "r1", "r2" }
            };
            var dishBack = JsonUtility.FromJson<DishData>(JsonUtility.ToJson(dish));
            Check("T-PERSIST-02d", "DishData 序列化往返",
                dishBack.name == "番茄炒蛋" && dishBack.price == 38.5f &&
                dishBack.score == 92 && dishBack.reviewIds.Count == 2);

            // ReviewData
            var review = new ReviewData
            {
                reviewId = "r1", dishId = "d1", reviewerName = "老八", score = 88,
                summary = "不错", suggestion = "火候注意", reputationDelta = 3
            };
            var reviewBack = JsonUtility.FromJson<ReviewData>(JsonUtility.ToJson(review));
            Check("T-PERSIST-02e", "ReviewData 序列化往返",
                reviewBack.reviewerName == "老八" && reviewBack.score == 88 && reviewBack.reputationDelta == 3);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════════

        private static void Check(string id, string name, bool condition)
        {
            Assert(id, name, condition, "true", condition ? "true" : "false");
        }

        private static RecipeData MakeRecipe(int stepCount)
        {
            var steps = new RecipeStep[stepCount];
            for (var i = 0; i < stepCount; i++)
                steps[i] = new RecipeStep { order = i + 1, action = $"a{i + 1}", target = "", hint = "" };
            return new RecipeData
            {
                recipeId = "test", name = "testRecipe",
                steps = steps,
                ingredientCookingConfigs = new IngredientCookingConfig[0]
            };
        }

        private static ActionRecord R(string action) => new ActionRecord { action = action, target = "" };
        private static RecipeStep S(string action) => new RecipeStep { action = action };
        private static IngredientCookState S(string name, DonenessLevel l) => new IngredientCookState { ingredientName = name, doneness = l };
    }
}
