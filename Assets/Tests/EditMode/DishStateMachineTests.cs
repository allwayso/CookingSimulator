using CookingSimulator.Core;
using CookingSimulator.Models;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DishStateMachineTests
    {
        [Test]
        public void Cut_FromRaw_TransitionsToCut()
        {
            var ok = GameManager.TryApplyAction(DishState.Raw, "cut", out var next);
            Assert.IsTrue(ok, "Raw 状态应允许 cut 操作");
            Assert.AreEqual(DishState.Cut, next);
        }

        [Test]
        public void PutInPan_FromCut_TransitionsToCooking()
        {
            var ok = GameManager.TryApplyAction(DishState.Cut, "put_in_pan", out var next);
            Assert.IsTrue(ok, "Cut 状态应允许 put_in_pan 操作");
            Assert.AreEqual(DishState.Cooking, next);
        }

        [Test]
        public void PutInPan_FromCooking_StaysCooking()
        {
            // Second ingredient going in
            var ok = GameManager.TryApplyAction(DishState.Cooking, "put_in_pan", out var next);
            Assert.IsTrue(ok, "Cooking 状态应允许再次 put_in_pan（第二食材）");
            Assert.AreEqual(DishState.Cooking, next, "第二食材下锅状态应保持 Cooking");
        }

        [Test]
        public void Season_FromCooking_TransitionsToSeasoned()
        {
            var ok = GameManager.TryApplyAction(DishState.Cooking, "season", out var next);
            Assert.IsTrue(ok, "Cooking 状态应允许 season 操作");
            Assert.AreEqual(DishState.Seasoned, next);
        }

        [Test]
        public void Stir_FromSeasoned_StaysSeasoned()
        {
            var ok = GameManager.TryApplyAction(DishState.Seasoned, "stir", out var next);
            Assert.IsTrue(ok, "Seasoned 状态应允许 stir 操作");
            Assert.AreEqual(DishState.Seasoned, next, "stir 后状态应保持 Seasoned");
        }

        [Test]
        public void Finish_FromSeasoned_TransitionsToDone()
        {
            var ok = GameManager.TryApplyAction(DishState.Seasoned, "finish", out var next);
            Assert.IsTrue(ok, "Seasoned 状态应允许 finish 操作");
            Assert.AreEqual(DishState.Done, next);
        }

        // ── 非法操作 ──

        [Test]
        public void InvalidAction_FromRaw_ReturnsFalse(
            [Values("put_in_pan", "season", "stir", "finish")] string action)
        {
            var ok = GameManager.TryApplyAction(DishState.Raw, action, out var next);
            Assert.IsFalse(ok, $"Raw 状态不应允许 {action}");
            Assert.AreEqual(DishState.Raw, next, "失败时 nextState 应保持原状态");
        }

        [Test]
        public void InvalidAction_FromCut_ReturnsFalse(
            [Values("cut", "season", "stir", "finish")] string action)
        {
            var ok = GameManager.TryApplyAction(DishState.Cut, action, out var next);
            Assert.IsFalse(ok, $"Cut 状态不应允许 {action}");
            Assert.AreEqual(DishState.Cut, next);
        }

        [Test]
        public void InvalidAction_FromCooking_ReturnsFalse(
            [Values("cut", "stir", "finish")] string action)
        {
            // stir/finish from Cooking is invalid — must be Seasoned first
            var ok = GameManager.TryApplyAction(DishState.Cooking, action, out var next);
            Assert.IsFalse(ok, $"Cooking 状态不应允许 {action}（需先调味）");
            Assert.AreEqual(DishState.Cooking, next);
        }

        [Test]
        public void InvalidAction_FromSeasoned_ReturnsFalse(
            [Values("cut", "put_in_pan", "season")] string action)
        {
            var ok = GameManager.TryApplyAction(DishState.Seasoned, action, out var next);
            Assert.IsFalse(ok, $"Seasoned 状态不应允许 {action}");
            Assert.AreEqual(DishState.Seasoned, next);
        }

        [Test]
        public void AnyAction_FromDone_ReturnsFalse(
            [Values("cut", "put_in_pan", "season", "stir", "finish")] string action)
        {
            var ok = GameManager.TryApplyAction(DishState.Done, action, out var next);
            Assert.IsFalse(ok, $"Done 状态不应允许任何操作（{action}）");
            Assert.AreEqual(DishState.Done, next);
        }

        [Test]
        public void UnknownAction_ReturnsFalse()
        {
            var ok = GameManager.TryApplyAction(DishState.Raw, "unknown_action", out var next);
            Assert.IsFalse(ok, "未知操作名应返回 false");
            Assert.AreEqual(DishState.Raw, next);
        }

        // ── 完整流程验证 ──

        [Test]
        public void FullRecipeFlow_AllValidTransitions()
        {
            // 模拟番茄炒蛋完整流程
            Assert.IsTrue(GameManager.TryApplyAction(DishState.Raw, "cut", out var s1));
            Assert.AreEqual(DishState.Cut, s1);

            Assert.IsTrue(GameManager.TryApplyAction(s1, "put_in_pan", out var s2));
            Assert.AreEqual(DishState.Cooking, s2);

            Assert.IsTrue(GameManager.TryApplyAction(s2, "put_in_pan", out var s3));
            Assert.AreEqual(DishState.Cooking, s3);

            Assert.IsTrue(GameManager.TryApplyAction(s3, "season", out var s4));
            Assert.AreEqual(DishState.Seasoned, s4);

            Assert.IsTrue(GameManager.TryApplyAction(s4, "stir", out var s5));
            Assert.AreEqual(DishState.Seasoned, s5);

            Assert.IsTrue(GameManager.TryApplyAction(s5, "finish", out var s6));
            Assert.AreEqual(DishState.Done, s6);
        }
    }
}
