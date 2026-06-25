using CookingSimulator.Core;
using CookingSimulator.Models;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DonenessCalculationTests
    {
        // ── 边界值（以番茄 fullCookThreshold=120 为参考） ──

        [Test]
        public void ZeroProgress_ReturnsRaw()
        {
            var result = GameManager.CalculateDoneness(0f, 120f);
            Assert.AreEqual(DonenessLevel.Raw, result);
        }

        [Test]
        public void BelowQuarterThreshold_ReturnsRaw()
        {
            // ratio = 29.99 / 120 = 0.2499 < 0.25
            var result = GameManager.CalculateDoneness(29.99f, 120f);
            Assert.AreEqual(DonenessLevel.Raw, result);
        }

        [Test]
        public void AtQuarterThreshold_ReturnsHalfCooked()
        {
            // ratio = 30 / 120 = 0.25
            var result = GameManager.CalculateDoneness(30f, 120f);
            Assert.AreEqual(DonenessLevel.HalfCooked, result);
        }

        [Test]
        public void BetweenQuarterAndPoint625_ReturnsHalfCooked()
        {
            // ratio = 74.99 / 120 = 0.6249
            var result = GameManager.CalculateDoneness(74.99f, 120f);
            Assert.AreEqual(DonenessLevel.HalfCooked, result);
        }

        [Test]
        public void AtPoint625Threshold_ReturnsFullyCooked()
        {
            // ratio = 75 / 120 = 0.625
            var result = GameManager.CalculateDoneness(75f, 120f);
            Assert.AreEqual(DonenessLevel.FullyCooked, result);
        }

        [Test]
        public void BetweenPoint625AndOne_ReturnsFullyCooked()
        {
            // ratio = 100 / 120 = 0.833
            var result = GameManager.CalculateDoneness(100f, 120f);
            Assert.AreEqual(DonenessLevel.FullyCooked, result);
        }

        [Test]
        public void ExactlyAtFullThreshold_ReturnsFullyCooked()
        {
            // ratio = 120 / 120 = 1.0
            var result = GameManager.CalculateDoneness(120f, 120f);
            Assert.AreEqual(DonenessLevel.FullyCooked, result,
                "ratio = 1.0 应判定为 FullyCooked（边界包含）");
        }

        [Test]
        public void SlightlyAboveFullThreshold_ReturnsOvercooked()
        {
            // ratio = 121 / 120 = 1.008 > 1.0
            var result = GameManager.CalculateDoneness(121f, 120f);
            Assert.AreEqual(DonenessLevel.Overcooked, result);
        }

        [Test]
        public void FarAboveFullThreshold_ReturnsOvercooked()
        {
            // ratio = 200 / 120 = 1.667
            var result = GameManager.CalculateDoneness(200f, 120f);
            Assert.AreEqual(DonenessLevel.Overcooked, result);
        }

        // ── 零或负阈值 ──

        [Test]
        public void ZeroThreshold_ReturnsFullyCooked()
        {
            var result = GameManager.CalculateDoneness(0f, 0f);
            Assert.AreEqual(DonenessLevel.FullyCooked, result,
                "fullThreshold <= 0 时应始终返回 FullyCooked");
        }

        [Test]
        public void NegativeThreshold_ReturnsFullyCooked()
        {
            var result = GameManager.CalculateDoneness(100f, -10f);
            Assert.AreEqual(DonenessLevel.FullyCooked, result);
        }

        // ── 鸡蛋熟度（fullCookThreshold=40） ──

        [Test]
        public void Egg_At10Seconds_ReturnsHalfCooked()
        {
            // 10 / 40 = 0.25
            var result = GameManager.CalculateDoneness(10f, 40f);
            Assert.AreEqual(DonenessLevel.HalfCooked, result);
        }

        [Test]
        public void Egg_At25Seconds_ReturnsFullyCooked()
        {
            // 25 / 40 = 0.625
            var result = GameManager.CalculateDoneness(25f, 40f);
            Assert.AreEqual(DonenessLevel.FullyCooked, result);
        }

        [Test]
        public void Egg_At40Seconds_ReturnsFullyCooked()
        {
            // 40 / 40 = 1.0
            var result = GameManager.CalculateDoneness(40f, 40f);
            Assert.AreEqual(DonenessLevel.FullyCooked, result);
        }

        [Test]
        public void Egg_At41Seconds_ReturnsOvercooked()
        {
            // 41 / 40 = 1.025
            var result = GameManager.CalculateDoneness(41f, 40f);
            Assert.AreEqual(DonenessLevel.Overcooked, result);
        }

        // ── 负进度 ──

        [Test]
        public void NegativeProgress_ReturnsRaw()
        {
            var result = GameManager.CalculateDoneness(-10f, 120f);
            Assert.AreEqual(DonenessLevel.Raw, result,
                "负进度应判定为 Raw（ratio < 0.25）");
        }
    }
}
