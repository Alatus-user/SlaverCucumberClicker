using System;
using CucumberClicker;
using CucumberClicker.DebugTools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CucumberClicker.Tests
{
    public class CheatsTests
    {
        private static readonly DateTime Start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private GameState state;
        private Wallet wallet;
        private UpgradeManager upgrades;
        private PicklingManager pickling;
        private Cheats cheats;
        private GameConfig config;
        private RecipeData brine;
        private RecipeData dill;
        private UpgradeData jars;
        private UpgradeData hands;
        private DateTime now;

        private static RecipeData MakeRecipe(string id, double seconds, double cost)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.id = id;
            recipe.displayName = id;
            recipe.baseDurationSeconds = seconds;
            recipe.baseValuePerCucumber = 10;
            recipe.unlockCost = cost;
            return recipe;
        }

        private static UpgradeData MakeUpgrade(string id, UpgradeEffect effect, int maxLevel)
        {
            var upgrade = ScriptableObject.CreateInstance<UpgradeData>();
            upgrade.id = id;
            upgrade.effect = effect;
            upgrade.effectPerLevel = 1;
            upgrade.baseCost = 10;
            upgrade.growth = 2;
            upgrade.maxLevel = maxLevel;
            return upgrade;
        }

        [SetUp]
        public void SetUp()
        {
            now = Start;
            TimeService.Override = () => now;

            brine = MakeRecipe("quick_brine", 180, 0);
            dill = MakeRecipe("garlic_dill", 600, 500);
            jars = MakeUpgrade("jar_count", UpgradeEffect.JarCount, 7);
            hands = MakeUpgrade("tap_power", UpgradeEffect.TapPower, 100);
            config = GameConfig.CreateDefault();
            config.startingJars = 3;
            config.recipes = new[] { brine, dill };
            config.upgrades = new[] { jars, hands };

            state = new GameState();
            wallet = new Wallet(state);
            upgrades = new UpgradeManager(state, wallet, config);
            pickling = new PicklingManager(state, wallet, config, upgrades);
            upgrades.Purchased += _ => pickling.SyncJarCount();
            cheats = new Cheats(state, upgrades);
        }

        [TearDown]
        public void TearDown()
        {
            TimeService.Reset();
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(brine);
            Object.DestroyImmediate(dill);
            Object.DestroyImmediate(jars);
            Object.DestroyImmediate(hands);
        }

        private JarStatus Status(int jar) => pickling.Jars[jar].StatusAt(TimeService.UtcNowTicks);

        [Test]
        public void FinishAllJarsMakesRunningJarsReadyAndKeepsTheirBatch()
        {
            wallet.AddCucumbers(10);
            pickling.TryStartBatch(0, brine, 5);
            pickling.TryStartBatch(1, brine, 3);

            Assert.AreEqual(2, cheats.FinishAllJars());

            Assert.AreEqual(JarStatus.Ready, Status(0));
            Assert.AreEqual(JarStatus.Ready, Status(1));
            Assert.AreEqual(JarStatus.Empty, Status(2), "empty jars stay empty");
            Assert.AreEqual(8, pickling.CollectAll());
            Assert.AreEqual(80, state.pickles[0].totalValue);
        }

        [Test]
        public void FinishAllJarsSkipsJarsThatAreAlreadyReady()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);
            now += TimeSpan.FromMinutes(3);

            Assert.AreEqual(0, cheats.FinishAllJars());
        }

        [TestCase(1, 0)]
        [TestCase(5, 4)]
        [TestCase(0, 0)]
        [TestCase(-3, 0)]
        [TestCase(9999, 999)]
        public void MarketLevelIsClamped(int level, int expectedSales)
        {
            cheats.MarketLevel = level;
            Assert.AreEqual(expectedSales, state.rawSales);
        }

        [Test]
        public void ItemLevelsAreClampedToTheirRange()
        {
            ShopItem item = upgrades.Find(UpgradeEffect.TapPower);

            cheats.SetItemLevel(item, 500);
            Assert.AreEqual(100, item.Level);

            cheats.SetItemLevel(item, -1);
            Assert.AreEqual(0, item.Level);
        }

        [Test]
        public void ExtraJarLevelsAddJarsButNeverRemoveThem()
        {
            ShopItem item = upgrades.Find(UpgradeEffect.JarCount);

            cheats.SetItemLevel(item, 2);
            Assert.AreEqual(5, pickling.Jars.Count);

            cheats.SetItemLevel(item, 0);
            Assert.AreEqual(2, item.Level);
            Assert.AreEqual(5, pickling.Jars.Count);
        }

        [Test]
        public void RecipeUnlockCanBeSetButNotTakenBack()
        {
            ShopItem item = upgrades.Find("recipe:garlic_dill");

            cheats.SetItemLevel(item, 1);
            Assert.IsTrue(upgrades.IsRecipeUnlocked(dill));
            Assert.AreEqual(2, pickling.Recipes.Count);

            cheats.SetItemLevel(item, 0);
            Assert.IsTrue(upgrades.IsRecipeUnlocked(dill));
        }
    }
}
