using System;
using CucumberClicker;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CucumberClicker.Tests
{
    public class NumberFormatterTests
    {
        [TestCase(0, "0")]
        [TestCase(7.9, "7")]
        [TestCase(999, "999")]
        [TestCase(1000, "1.00K")]
        [TestCase(1234, "1.23K")]
        [TestCase(1239, "1.23K")] // truncates, never rounds up
        [TestCase(12500, "12.5K")]
        [TestCase(999999, "999K")]
        [TestCase(45_600_000, "45.6M")]
        [TestCase(7_890_000_000, "7.89B")]
        [TestCase(1e12, "1.00T")]
        [TestCase(1e15, "1.00aa")]
        [TestCase(2.5e18, "2.50ab")]
        public void Format(double value, string expected) => Assert.AreEqual(expected, NumberFormatter.Format(value));

        [TestCase(3.5, "+3.5/s")]
        [TestCase(20, "+20/s")]
        [TestCase(1500, "+1.50K/s")]
        public void FormatRate(double value, string expected) => Assert.AreEqual(expected, NumberFormatter.FormatRate(value));

        [TestCase(1, "1")]
        [TestCase(1.15, "1.15")]
        [TestCase(1.5209, "1.52")]
        [TestCase(28.6, "28.6")]
        [TestCase(12500, "12.5K")]
        public void FormatMultiplier(double value, string expected) =>
            Assert.AreEqual(expected, NumberFormatter.FormatMultiplier(value));

        [TestCase(45, "45s")]
        [TestCase(133, "2m 13s")]
        [TestCase(3 * 3600 + 5 * 60, "3h 05m")]
        [TestCase(26 * 3600, "1d 02h")]
        [TestCase(-5, "0s")]
        public void FormatDuration(double seconds, string expected) =>
            Assert.AreEqual(expected, NumberFormatter.FormatDuration(seconds));
    }

    public class CostFormulaTests
    {
        [Test]
        public void CostGrowsGeometrically()
        {
            Assert.AreEqual(15, CostFormula.Cost(15, 1.5, 0), 1e-9);
            Assert.AreEqual(22.5, CostFormula.Cost(15, 1.5, 1), 1e-9);
            Assert.AreEqual(33.75, CostFormula.Cost(15, 1.5, 2), 1e-9);
        }

        [Test]
        public void TapPowerFormula()
        {
            Assert.AreEqual(1, CostFormula.TapPower(1, 0, 0));
            Assert.AreEqual(4, CostFormula.TapPower(1, 3, 0));
            Assert.AreEqual(16, CostFormula.TapPower(1, 3, 2));
        }

        [Test]
        public void RecipeDurationAndValueFormulas()
        {
            Assert.AreEqual(180, CostFormula.RecipeDuration(180, 0, 0.05), 1e-9);
            Assert.AreEqual(90, CostFormula.RecipeDuration(180, 10, 0.05), 1e-9);
            Assert.AreEqual(10, CostFormula.RecipeValue(10, 0, 0.1), 1e-9);
            Assert.AreEqual(35, CostFormula.RecipeValue(10, 25, 0.1), 1e-9);
        }
    }

    public class EconomyTests
    {
        private GameState state;
        private Wallet wallet;
        private Market market;
        private GameConfig config;

        [SetUp]
        public void SetUp()
        {
            config = GameConfig.CreateDefault();
            state = new GameState();
            wallet = new Wallet(state);
            market = new Market(state, wallet, config);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(config);

        [Test]
        public void RawLotsDoubleWithEverySale()
        {
            wallet.AddCucumbers(7);

            Assert.AreEqual(1, market.SellRaw());
            Assert.AreEqual(2, market.SellRaw());
            Assert.AreEqual(4, market.NextRawSaleAmount);
            Assert.AreEqual(4, market.SellRaw());

            Assert.AreEqual(7, wallet.Coins);
            Assert.AreEqual(0, wallet.Cucumbers);
            Assert.AreEqual(7, state.stats.lifetimeCoins);
            Assert.AreEqual(8, market.NextRawSaleAmount);
        }

        [Test]
        public void CannotSellLessThanTheNextLot()
        {
            wallet.AddCucumbers(4);
            market.SellRaw(); // 1
            market.SellRaw(); // 2

            Assert.IsFalse(market.CanSellRaw);
            Assert.AreEqual(0, market.NextRawSaleValue);
            Assert.AreEqual(0, market.SellRaw());
            Assert.AreEqual(1, wallet.Cucumbers);
            Assert.AreEqual(4, market.NextRawSaleAmount, "a failed sale must not grow the next lot");
        }

        [Test]
        public void SaleOnlyTakesTheLotAndKeepsTheRest()
        {
            wallet.AddCucumbers(100);
            Assert.AreEqual(1, market.SellRaw());
            Assert.AreEqual(99, wallet.Cucumbers);
        }

        [Test]
        public void MarketLevelRisesOnlyOnASuccessfulSale()
        {
            Assert.AreEqual(1, market.MarketLevel);
            market.SellRaw();
            Assert.AreEqual(1, market.MarketLevel, "nothing was sold");

            wallet.AddCucumbers(3);
            market.SellRaw();
            Assert.AreEqual(2, market.MarketLevel);
            market.SellRaw();
            Assert.AreEqual(3, market.MarketLevel);
        }

        [Test]
        public void PickleValueGrowsWithEachMarketLevelAboveOne()
        {
            config.pickleValueGrowth = 2;
            state.pickles.Add(new PickleStack { recipeId = "quick_brine", count = 5, totalValue = 250 });
            Assert.AreEqual(1, market.PickleMultiplier);
            Assert.AreEqual(250, market.ValueOfAllPickles);

            wallet.AddCucumbers(3);
            market.SellRaw();
            Assert.AreEqual(2, market.PickleMultiplier);
            Assert.AreEqual(500, market.ValueOfAllPickles);

            market.SellRaw();
            Assert.AreEqual(4, market.PickleMultiplier);
            Assert.AreEqual(1000, market.SellPickles("quick_brine"));
        }

        [Test]
        public void DefaultPickleGrowthIsGentle()
        {
            state.rawSales = 9;
            Assert.AreEqual(Math.Pow(1.15, 9), market.PickleMultiplier, 1e-9);
            Assert.Less(market.PickleMultiplier, 4);
        }

        [Test]
        public void HagglingStacksWithTheMarketLevel()
        {
            market.SellPriceMultiplier = 1.5;
            Assert.AreEqual(15, market.PickleSellValue(10));
        }

        [Test]
        public void SellEverythingPaysExactlyWhatItShows()
        {
            state.pickles.Add(new PickleStack { recipeId = "quick_brine", count = 5, totalValue = 250 });
            wallet.AddCucumbers(1);

            double shown = market.ValueOfEverything;
            Assert.AreEqual(251, shown);
            Assert.AreEqual(shown, market.SellEverything());
            Assert.AreEqual(2, market.MarketLevel);
        }

        [Test]
        public void HagglingRaisesRawPriceButNotLotSize()
        {
            wallet.AddCucumbers(3);
            market.SellRaw();
            market.SellPriceMultiplier = 1.5;

            Assert.AreEqual(2, market.NextRawSaleAmount);
            Assert.AreEqual(3, market.SellRaw());
        }

        [Test]
        public void FractionalCucumbersAreNotSold()
        {
            wallet.AddCucumbers(1.7);
            Assert.AreEqual(1, market.SellRaw());
            Assert.AreEqual(0.7, wallet.Cucumbers, 1e-9);
            Assert.AreEqual(0, market.SellRaw());
        }

        [Test]
        public void SpendingFailsWithoutEnoughCoinsAndRaisesEvent()
        {
            bool raised = false;
            wallet.InsufficientCoins += () => raised = true;
            wallet.AddCoins(10);

            Assert.IsFalse(wallet.TrySpendCoins(11));
            Assert.IsTrue(raised);
            Assert.AreEqual(10, wallet.Coins);

            Assert.IsTrue(wallet.TrySpendCoins(10));
            Assert.AreEqual(0, wallet.Coins);
            Assert.AreEqual(10, state.stats.lifetimeCoins, "spending must not reduce lifetime coins");
        }
    }

    public class TapLimiterTests
    {
        [Test]
        public void CapsTapsPerSecond()
        {
            var limiter = new TapLimiter(20);
            int accepted = 0;
            for (int i = 0; i < 30; i++)
                if (limiter.TryTap(i * 0.01)) accepted++;
            Assert.AreEqual(20, accepted);

            // A second later the window has cleared.
            Assert.IsTrue(limiter.TryTap(1.5));
        }
    }
}
