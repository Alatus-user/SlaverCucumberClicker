using System;
using CucumberClicker;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CucumberClicker.Tests
{
    public class PicklingTests
    {
        private static readonly DateTime Start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private GameState state;
        private Wallet wallet;
        private Market market;
        private PicklingManager pickling;
        private GameConfig config;
        private RecipeData brine;
        private RecipeData dill;
        private DateTime now;

        private static RecipeData MakeRecipe(string id, double seconds, double value)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.id = id;
            recipe.displayName = id;
            recipe.baseDurationSeconds = seconds;
            recipe.baseValuePerCucumber = value;
            return recipe;
        }

        [SetUp]
        public void SetUp()
        {
            now = Start;
            TimeService.Override = () => now;

            brine = MakeRecipe("quick_brine", 180, 10);
            dill = MakeRecipe("garlic_dill", 600, 25);
            config = GameConfig.CreateDefault();
            config.startingJars = 2;
            config.pickleValueGrowth = 1;
            config.recipes = new[] { brine, dill };

            state = new GameState();
            wallet = new Wallet(state);
            market = new Market(state, wallet, config);
            pickling = new PicklingManager(state, wallet, config, new UpgradeManager(state, wallet, config));
        }

        [TearDown]
        public void TearDown()
        {
            TimeService.Reset();
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(brine);
            Object.DestroyImmediate(dill);
        }

        private JarStatus Status(int jar) => pickling.Jars[jar].StatusAt(TimeService.UtcNowTicks);

        [Test]
        public void StartsWithConfiguredEmptyJars()
        {
            Assert.AreEqual(2, pickling.Jars.Count);
            Assert.AreEqual(JarStatus.Empty, Status(0));
        }

        [Test]
        public void FiveCucumbersInQuickBrineSellForFiftyCoins()
        {
            wallet.AddCucumbers(5);

            Assert.IsTrue(pickling.TryStartBatch(0, brine, 5));
            now += TimeSpan.FromMinutes(3);
            Assert.AreEqual(5, pickling.Collect(0));

            Assert.AreEqual(50, market.SellPickles("quick_brine"));
            Assert.AreEqual(50, wallet.Coins);
            Assert.AreEqual(5, state.stats.picklesMade);
            Assert.AreEqual(5, state.stats.picklesSold);
            Assert.AreEqual(50, state.stats.lifetimeCoins);
        }

        [Test]
        public void StartingRemovesCucumbersImmediately()
        {
            wallet.AddCucumbers(7);
            pickling.TryStartBatch(0, brine, 5);
            Assert.AreEqual(2, wallet.Cucumbers);
        }

        [Test]
        public void JarIsReadyExactlyAtTheEndTime()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);
            Assert.AreEqual(JarStatus.Running, Status(0));

            now += TimeSpan.FromSeconds(179);
            Assert.AreEqual(JarStatus.Running, Status(0));
            Assert.AreEqual(1, pickling.Jars[0].RemainingSecondsAt(TimeService.UtcNowTicks), 1e-6);
            Assert.AreEqual(0, pickling.Collect(0), "a running jar can't be collected");

            now += TimeSpan.FromSeconds(1);
            Assert.AreEqual(JarStatus.Ready, Status(0));
            Assert.AreEqual(1, pickling.ReadyCount);
        }

        [Test]
        public void JarCompletesAcrossATimeJump()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);

            now += TimeSpan.FromDays(3);

            Assert.AreEqual(JarStatus.Ready, Status(0));
            Assert.AreEqual(1, pickling.Jars[0].ProgressAt(TimeService.UtcNowTicks));
            Assert.AreEqual(0, pickling.Jars[0].RemainingSecondsAt(TimeService.UtcNowTicks));
        }

        [Test]
        public void ClockRollbackDoesNotFinishAJar()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);

            now -= TimeSpan.FromDays(1);

            Assert.AreEqual(JarStatus.Running, Status(0));
            Assert.AreEqual(0, pickling.Jars[0].ProgressAt(TimeService.UtcNowTicks));
            Assert.AreEqual(0, pickling.Collect(0));
        }

        [Test]
        public void BatchIsASnapshotOfTheRecipe()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);

            brine.baseDurationSeconds = 9999;
            brine.baseValuePerCucumber = 1000;

            now += TimeSpan.FromSeconds(180);
            Assert.AreEqual(JarStatus.Ready, Status(0));
            pickling.Collect(0);
            Assert.AreEqual(50, market.ValueOfAllPickles);
        }

        [TestCase(0)]
        [TestCase(6)] // over the jar's capacity
        [TestCase(1.5)]
        public void InvalidAmountsAreRejected(double amount)
        {
            wallet.AddCucumbers(10);
            Assert.IsFalse(pickling.TryStartBatch(0, brine, amount));
            Assert.AreEqual(10, wallet.Cucumbers);
            Assert.AreEqual(JarStatus.Empty, Status(0));
        }

        [Test]
        public void CannotPickleMoreThanYouOwn()
        {
            wallet.AddCucumbers(3);
            Assert.AreEqual(3, pickling.MaxBatch);
            Assert.IsFalse(pickling.TryStartBatch(0, brine, 4));
            Assert.AreEqual(3, wallet.Cucumbers);
        }

        [Test]
        public void MaxBatchIsLimitedByCapacity()
        {
            wallet.AddCucumbers(100);
            Assert.AreEqual(5, pickling.MaxBatch);
        }

        [Test]
        public void CannotFillABusyJar()
        {
            wallet.AddCucumbers(10);
            pickling.TryStartBatch(0, brine, 5);

            Assert.IsFalse(pickling.TryStartBatch(0, brine, 5));
            Assert.AreEqual(5, wallet.Cucumbers);
        }

        [Test]
        public void BadJarIndexOrRecipeIsRejected()
        {
            wallet.AddCucumbers(5);
            Assert.IsFalse(pickling.TryStartBatch(-1, brine, 5));
            Assert.IsFalse(pickling.TryStartBatch(2, brine, 5));
            Assert.IsFalse(pickling.TryStartBatch(0, null, 5));
            Assert.AreEqual(0, pickling.Collect(2));
            Assert.AreEqual(5, wallet.Cucumbers);
        }

        [Test]
        public void CollectingEmptiesTheJarAndOnlyPaysOnce()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);
            now += TimeSpan.FromMinutes(3);

            Assert.AreEqual(5, pickling.Collect(0));
            Assert.AreEqual(0, pickling.Collect(0));
            Assert.AreEqual(JarStatus.Empty, Status(0));
            Assert.AreEqual(0, pickling.ReadyCount);
        }

        [Test]
        public void CollectAllTakesOnlyReadyJars()
        {
            wallet.AddCucumbers(10);
            pickling.TryStartBatch(0, brine, 5);
            pickling.TryStartBatch(1, dill, 3);
            now += TimeSpan.FromMinutes(3);

            Assert.AreEqual(5, pickling.CollectAll());
            Assert.AreEqual(JarStatus.Running, Status(1));
        }

        [Test]
        public void BatchesOfDifferentValueShareOneStack()
        {
            wallet.AddCucumbers(10);
            pickling.TryStartBatch(0, brine, 2);
            brine.baseValuePerCucumber = 20;
            pickling.TryStartBatch(1, brine, 3);
            now += TimeSpan.FromMinutes(3);
            pickling.CollectAll();

            Assert.AreEqual(1, state.pickles.Count);
            PickleStack stack = state.pickles[0];
            Assert.AreEqual(5, stack.count);
            Assert.AreEqual(80, stack.totalValue);
            Assert.AreEqual(16, market.PicklePrice(stack));
        }

        [Test]
        public void SellingRemovesTheStack()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);
            now += TimeSpan.FromMinutes(3);
            pickling.Collect(0);

            market.SellPickles("quick_brine");

            Assert.AreEqual(0, state.pickles.Count);
            Assert.AreEqual(0, market.SellPickles("quick_brine"));
            Assert.AreEqual(50, wallet.Coins);
        }

        [Test]
        public void SellPriceMultiplierAppliesToPickles()
        {
            wallet.AddCucumbers(5);
            pickling.TryStartBatch(0, brine, 5);
            now += TimeSpan.FromMinutes(3);
            pickling.Collect(0);

            market.SellPriceMultiplier = 1.5;

            Assert.AreEqual(75, market.ValueOfAllPickles);
            Assert.AreEqual(75, market.SellAllPickles());
        }

        [Test]
        public void SellEverythingTakesOneRawLotAndAllPickleTypes()
        {
            wallet.AddCucumbers(8);
            pickling.TryStartBatch(0, brine, 1);
            pickling.TryStartBatch(1, dill, 2);
            now += TimeSpan.FromMinutes(10);
            pickling.CollectAll();

            Assert.AreEqual(61, market.ValueOfEverything); // 1 raw lot + 10 + 50
            Assert.AreEqual(61, market.SellEverything());
            Assert.AreEqual(2, market.ValueOfEverything, "next lot is 2 and 4 cucumbers remain");
            Assert.AreEqual(4, wallet.Cucumbers);
            Assert.AreEqual(3, state.stats.picklesSold);
        }

        [Test]
        public void FindRecipeReturnsNullForUnknownIds()
        {
            Assert.AreEqual(dill, pickling.FindRecipe("garlic_dill"));
            Assert.IsNull(pickling.FindRecipe("removed_recipe"));
            Assert.IsNull(pickling.FindRecipe(null));
        }
    }
}
