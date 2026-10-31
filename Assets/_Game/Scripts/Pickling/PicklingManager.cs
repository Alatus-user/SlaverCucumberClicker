using System;
using System.Collections.Generic;

namespace CucumberClicker
{
    /// <summary>Starts batches and collects finished jars into the pickle inventory.</summary>
    public class PicklingManager
    {
        private readonly GameState state;
        private readonly Wallet wallet;
        private readonly GameConfig config;
        private readonly UpgradeManager upgrades;

        public PicklingManager(GameState state, Wallet wallet, GameConfig config, UpgradeManager upgrades)
        {
            this.state = state;
            this.wallet = wallet;
            this.config = config;
            this.upgrades = upgrades;

            SyncJarCount();
        }

        public IReadOnlyList<JarState> Jars => state.jars;

        /// <summary>Recipes the player can use.</summary>
        public IReadOnlyList<RecipeData> Recipes => upgrades.UnlockedRecipes;

        /// <summary>Cucumbers per jar.</summary>
        public double JarCapacity => config.baseJarCapacity + upgrades.Bonus(UpgradeEffect.JarCapacity);

        /// <summary>The most cucumbers the player can put in one jar right now.</summary>
        public double MaxBatch => Math.Min(JarCapacity, wallet.WholeCucumbers);

        public double DurationSeconds(RecipeData recipe) => upgrades.RecipeDuration(recipe);

        public double ValuePerPickle(RecipeData recipe) => upgrades.RecipeValue(recipe);

        public double BatchValue(RecipeData recipe, double amount) => amount * ValuePerPickle(recipe);

        public int ReadyCount
        {
            get
            {
                long now = TimeService.UtcNowTicks;
                int ready = 0;
                foreach (JarState jar in state.jars)
                    if (jar.StatusAt(now) == JarStatus.Ready) ready++;
                return ready;
            }
        }

        /// <summary>Null if the id isn't in the config, e.g. a recipe removed from the game.</summary>
        public RecipeData FindRecipe(string id) => Array.Find(config.recipes, r => r.id == id);

        /// <summary>Adds any jars the player owns but doesn't have yet.</summary>
        public void SyncJarCount()
        {
            int owned = config.startingJars + (int)upgrades.Bonus(UpgradeEffect.JarCount);
            while (state.jars.Count < owned) state.jars.Add(new JarState());
        }

        public bool TryStartBatch(int jarIndex, RecipeData recipe, double amount)
        {
            if (jarIndex < 0 || jarIndex >= state.jars.Count || !upgrades.IsRecipeUnlocked(recipe)) return false;

            JarState jar = state.jars[jarIndex];
            long now = TimeService.UtcNowTicks;
            if (jar.StatusAt(now) != JarStatus.Empty) return false;
            if (amount != Math.Floor(amount) || amount < 1 || amount > JarCapacity) return false;
            if (!wallet.TryRemoveCucumbers(amount)) return false;

            jar.recipeId = recipe.id;
            jar.amount = amount;
            jar.startUtcTicks = now;
            jar.durationSeconds = DurationSeconds(recipe);
            jar.valuePerPickle = ValuePerPickle(recipe);
            return true;
        }

        /// <summary>Moves a Ready jar's pickles to the inventory. Returns pickles collected.</summary>
        public double Collect(int jarIndex)
        {
            if (jarIndex < 0 || jarIndex >= state.jars.Count) return 0;

            JarState jar = state.jars[jarIndex];
            if (jar.StatusAt(TimeService.UtcNowTicks) != JarStatus.Ready) return 0;

            double collected = jar.amount;
            PickleStack stack = state.pickles.Find(p => p.recipeId == jar.recipeId);
            if (stack == null)
            {
                stack = new PickleStack { recipeId = jar.recipeId };
                state.pickles.Add(stack);
            }
            stack.count += collected;
            stack.totalValue += collected * jar.valuePerPickle;
            state.stats.picklesMade += collected;

            jar.Clear();
            return collected;
        }

        public double CollectAll()
        {
            double collected = 0;
            for (int i = 0; i < state.jars.Count; i++) collected += Collect(i);
            return collected;
        }
    }
}
