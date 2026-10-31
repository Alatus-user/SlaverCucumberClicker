using System;
using System.Collections.Generic;
using System.Linq;
using CucumberClicker;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CucumberClicker.Tests
{
    public class UpgradeTests
    {
        private static readonly DateTime Start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private GameState state;
        private Wallet wallet;
        private GameConfig config;
        private UpgradeManager upgrades;
        private PicklingManager pickling;
        private Market market;
        private RecipeData brine;
        private RecipeData dill;
        private List<UpgradeData> upgradeAssets;
        private DateTime now;

        private static RecipeData MakeRecipe(string id, double seconds, double value, double visibleAt, double cost)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.id = id;
            recipe.displayName = id;
            recipe.baseDurationSeconds = seconds;
            recipe.baseValuePerCucumber = value;
            recipe.unlockCost = cost;
            recipe.unlockCondition = new UnlockCondition
            {
                type = visibleAt > 0 ? UnlockType.LifetimeCoinsAtLeast : UnlockType.None,
                amount = visibleAt,
            };
            return recipe;
        }

        private UpgradeData MakeUpgrade(string id, UpgradeEffect effect, double perLevel, double cost, double growth,
            int max, double visibleAt = 0)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.id = id;
            upgrade.displayName = id;
            upgrade.effect = effect;
            upgrade.effectPerLevel = perLevel;
            upgrade.baseCost = cost;
            upgrade.growth = growth;
            upgrade.maxLevel = max;
            upgrade.unlockCondition = new UnlockCondition
            {
                type = visibleAt > 0 ? UnlockType.LifetimeCoinsAtLeast : UnlockType.None,
                amount = visibleAt,
            };
            upgradeAssets.Add(upgrade);
            return upgrade;
        }

        [SetUp]
        public void SetUp()
        {
            now = Start;
            TimeService.Override = () => now;

            upgradeAssets = new List<UpgradeData>();
            brine = MakeRecipe("quick_brine", 180, 10, 0, 0);
            dill = MakeRecipe("garlic_dill", 600, 25, 1000, 500);
            config = GameConfig.CreateDefault();
            config.pickleValueGrowth = 1;
            config.recipes = new[] { brine, dill };
            config.upgrades = new[]
            {
                MakeUpgrade("tap_power", UpgradeEffect.TapPower, 1, 15, 1.5, 100),
                MakeUpgrade("jar_count", UpgradeEffect.JarCount, 1, 100, 4, 2),
                MakeUpgrade("jar_capacity", UpgradeEffect.JarCapacity, 5, 50, 1.8, 40),
                MakeUpgrade("market_haggle", UpgradeEffect.MarketHaggle, 0.05, 2000, 2.2, 20, 1000),
            };
            state = new GameState();
            wallet = new Wallet(state);
            upgrades = new UpgradeManager(state, wallet, config);
            pickling = new PicklingManager(state, wallet, config, upgrades);
            market = new Market(state, wallet, config);
        }

        [TearDown]
        public void TearDown()
        {
            TimeService.Reset();
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(brine);
            Object.DestroyImmediate(dill);
            foreach (UpgradeData upgrade in upgradeAssets) Object.DestroyImmediate(upgrade);
        }

        private ShopItem DillUnlock => upgrades.Find("recipe:garlic_dill");

        private void Earn(double coins) => wallet.AddCoins(coins);

        [Test]
        public void BuyingSpendsCoinsAndRaisesCost()
        {
            ShopItem item = upgrades.Find(UpgradeEffect.TapPower);
            Earn(15);

            Assert.IsTrue(upgrades.TryBuy(item));
            Assert.AreEqual(0, wallet.Coins);
            Assert.AreEqual(1, item.Level);
            Assert.AreEqual(22.5, item.Cost, 1e-9);
        }

        [Test]
        public void CannotBuyWithoutCoins()
        {
            ShopItem item = upgrades.Find(UpgradeEffect.TapPower);
            bool shook = false;
            wallet.InsufficientCoins += () => shook = true;
            Earn(14);

            Assert.IsFalse(upgrades.TryBuy(item));
            Assert.IsTrue(shook);
            Assert.AreEqual(14, wallet.Coins);
            Assert.AreEqual(0, item.Level);
        }

        [Test]
        public void CannotBuyPastMaxLevel()
        {
            ShopItem item = upgrades.Find(UpgradeEffect.JarCount);
            Earn(1000);

            Assert.IsTrue(upgrades.TryBuy(item));
            Assert.IsTrue(upgrades.TryBuy(item));
            Assert.AreEqual(ShopVisibility.Maxed, upgrades.Visibility(item));
            Assert.IsFalse(upgrades.TryBuy(item));
            Assert.AreEqual(500, wallet.Coins);
        }

        [Test]
        public void ConditionMovesThroughHiddenTeaserAvailable()
        {
            Assert.AreEqual(ShopVisibility.Hidden, upgrades.Visibility(DillUnlock));
            Earn(499);
            Assert.AreEqual(ShopVisibility.Hidden, upgrades.Visibility(DillUnlock));
            Earn(1);
            Assert.AreEqual(ShopVisibility.Teaser, upgrades.Visibility(DillUnlock));
            Earn(499);
            Assert.AreEqual(ShopVisibility.Teaser, upgrades.Visibility(DillUnlock));
            Earn(1);
            Assert.AreEqual(ShopVisibility.Available, upgrades.Visibility(DillUnlock));
        }

        [Test]
        public void SpendingDoesNotHideAnAvailableItem()
        {
            Earn(1000);
            wallet.TrySpendCoins(1000);
            Assert.AreEqual(ShopVisibility.Available, upgrades.Visibility(DillUnlock));
        }

        [Test]
        public void CannotBuyHiddenOrTeaserItems()
        {
            Earn(600);
            Assert.AreEqual(ShopVisibility.Teaser, upgrades.Visibility(DillUnlock));
            Assert.IsFalse(upgrades.TryBuy(DillUnlock));
            Assert.AreEqual(600, wallet.Coins);
        }

        [TestCase(UnlockType.TotalTapsAtLeast, 10, 0.3)]
        [TestCase(UnlockType.PicklesSoldAtLeast, 10, 0.2)]
        public void OtherConditionsReadTheirOwnStat(UnlockType type, double amount, double expected)
        {
            state.stats.totalTaps = 3;
            state.stats.picklesSold = 2;
            Assert.AreEqual(expected, upgrades.Progress(new UnlockCondition { type = type, amount = amount }), 1e-9);
        }

        [Test]
        public void RecipeUnlockedCondition()
        {
            var condition = new UnlockCondition { type = UnlockType.RecipeUnlocked, recipeId = "garlic_dill" };
            Assert.AreEqual(0, upgrades.Progress(condition));

            Earn(1000);
            upgrades.TryBuy(DillUnlock);

            Assert.AreEqual(1, upgrades.Progress(condition));
        }

        [Test]
        public void StartingRecipeIsOwnedAndHasNoUnlockItem()
        {
            Assert.IsTrue(upgrades.IsRecipeUnlocked(brine));
            Assert.IsNull(upgrades.Find("recipe:quick_brine"));
            CollectionAssert.AreEqual(new[] { brine }, pickling.Recipes.ToList());
        }

        [Test]
        public void UnlockingARecipeAddsItToThePicker()
        {
            Earn(1000);
            Assert.IsTrue(upgrades.TryBuy(DillUnlock));

            CollectionAssert.AreEqual(new[] { brine, dill }, pickling.Recipes.ToList());
            Assert.AreEqual(ShopVisibility.Maxed, upgrades.Visibility(DillUnlock));
            Assert.AreEqual(500, wallet.Coins);
        }

        [Test]
        public void LockedRecipeCannotBeUsed()
        {
            wallet.AddCucumbers(5);
            Assert.IsFalse(pickling.TryStartBatch(0, dill, 5));
            Assert.AreEqual(5, wallet.Cucumbers);
        }

        [Test]
        public void RecipeUpgradesAppearOnceTheRecipeIsOwned()
        {
            Assert.AreEqual(ShopVisibility.Available, upgrades.Visibility(upgrades.QualityItem(brine)));
            Assert.AreEqual(ShopVisibility.Hidden, upgrades.Visibility(upgrades.QualityItem(dill)));

            Earn(1000);
            upgrades.TryBuy(DillUnlock);

            Assert.AreEqual(ShopVisibility.Available, upgrades.Visibility(upgrades.QualityItem(dill)));
            Assert.AreEqual(ShopVisibility.Available, upgrades.Visibility(upgrades.SpeedItem(dill)));
        }

        [Test]
        public void RecipeUpgradeBaseCostFollowsUnlockCost()
        {
            Assert.AreEqual(50, upgrades.QualityItem(brine).BaseCost); // max(50, 0)
            Assert.AreEqual(250, upgrades.SpeedItem(dill).BaseCost); // 500 × 0.5
        }

        [Test]
        public void QualityAndSpeedLevelsChangeNewBatchValues()
        {
            Earn(1000);
            upgrades.TryBuy(upgrades.QualityItem(brine));
            upgrades.TryBuy(upgrades.SpeedItem(brine));

            Assert.AreEqual(11, pickling.ValuePerPickle(brine), 1e-9);
            Assert.AreEqual(171, pickling.DurationSeconds(brine), 1e-9);
        }

        [Test]
        public void FasterFermentationStopsAtHalfDuration()
        {
            ShopItem speed = upgrades.SpeedItem(brine);
            Earn(1e9);
            while (upgrades.TryBuy(speed)) { }

            Assert.AreEqual(10, speed.Level);
            Assert.AreEqual(90, pickling.DurationSeconds(brine), 1e-9);
        }

        [Test]
        public void UpgradesBoughtMidBatchDoNotChangeIt()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);

            Earn(1000);
            upgrades.TryBuy(upgrades.QualityItem(brine));
            upgrades.TryBuy(upgrades.SpeedItem(brine));

            now += TimeSpan.FromSeconds(179);
            Assert.AreEqual(0, pickling.Collect(0), "the shorter duration must not apply");
            now += TimeSpan.FromSeconds(1);
            pickling.Collect(0);
            Assert.AreEqual(50, market.ValueOfAllPickles);
        }

        [Test]
        public void ExtraJarAddsAnEmptyJar()
        {
            Earn(100);
            upgrades.TryBuy(upgrades.Find(UpgradeEffect.JarCount));
            pickling.SyncJarCount();

            Assert.AreEqual(2, pickling.Jars.Count);
            Assert.AreEqual(JarStatus.Empty, pickling.Jars[1].StatusAt(TimeService.UtcNowTicks));
        }

        [Test]
        public void BiggerJarsRaiseCapacity()
        {
            Earn(50);
            upgrades.TryBuy(upgrades.Find(UpgradeEffect.JarCapacity));
            wallet.AddCucumbers(20);

            Assert.AreEqual(10, pickling.JarCapacity);
            Assert.AreEqual(10, pickling.MaxBatch);
            Assert.IsTrue(pickling.TryStartBatch(0, brine, 10));
        }

        [Test]
        public void ItemsAreAnnouncedOnceWhenAvailable()
        {
            var announced = new List<string>();
            upgrades.ItemAvailable += item => announced.Add(item.Id);

            upgrades.CheckUnlocks();
            Assert.IsEmpty(announced, "nothing with a condition is available yet");

            Earn(1000);
            upgrades.CheckUnlocks();
            upgrades.CheckUnlocks();

            CollectionAssert.AreEquivalent(new[] { "market_haggle", "recipe:garlic_dill" }, announced);
            CollectionAssert.AreEquivalent(announced, state.announcedItems);
        }

        [Test]
        public void BuyingARecipeDoesNotAnnounceItsUpgrades()
        {
            var announced = new List<string>();
            Earn(1000);
            upgrades.CheckUnlocks();
            upgrades.ItemAvailable += item => announced.Add(item.Id);

            upgrades.TryBuy(DillUnlock);
            upgrades.CheckUnlocks();

            Assert.IsEmpty(announced);
        }

        [Test]
        public void AlreadyAnnouncedItemsStayQuietInANewSession()
        {
            Earn(1000);
            upgrades.CheckUnlocks();

            var next = new UpgradeManager(state, wallet, config);
            var announced = new List<string>();
            next.ItemAvailable += item => announced.Add(item.Id);
            next.CheckUnlocks();

            Assert.IsEmpty(announced);
        }

        [Test]
        public void LevelsCarryOverToANewManager()
        {
            Earn(15);
            upgrades.TryBuy(upgrades.Find(UpgradeEffect.TapPower));

            var next = new UpgradeManager(state, wallet, config);

            Assert.AreEqual(1, next.LevelOf(UpgradeEffect.TapPower));
        }

        [Test]
        public void HagglingBonusIsFivePercentPerLevel()
        {
            Earn(2000);
            upgrades.TryBuy(upgrades.Find(UpgradeEffect.MarketHaggle));
            Assert.AreEqual(0.05, upgrades.Bonus(UpgradeEffect.MarketHaggle), 1e-9);
        }

        [Test]
        public void MissingUpgradeCountsAsLevelZero()
        {
            Assert.AreEqual(0, upgrades.LevelOf(UpgradeEffect.TapMultiplier));
            Assert.AreEqual(0, upgrades.Bonus(UpgradeEffect.TapMultiplier));
        }
    }
}
