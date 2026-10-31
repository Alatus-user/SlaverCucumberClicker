using System;
using System.Collections.Generic;

namespace CucumberClicker
{
    /// <summary>The shop: what exists, what's visible, what it costs, and what the levels add up to.</summary>
    public class UpgradeManager
    {
        private const string RecipeUnlockPrefix = "recipe:";
        private const string QualityPrefix = "quality:";
        private const string SpeedPrefix = "speed:";

        private readonly GameState state;
        private readonly Wallet wallet;
        private readonly GameConfig config;

        private readonly List<ShopItem> items = new List<ShopItem>();
        private readonly Dictionary<string, ShopItem> itemsById = new Dictionary<string, ShopItem>();
        private readonly Dictionary<UpgradeEffect, ShopItem> itemsByEffect = new Dictionary<UpgradeEffect, ShopItem>();
        private readonly List<ShopItem> waitingToAnnounce = new List<ShopItem>();
        private readonly List<RecipeData> unlockedRecipes = new List<RecipeData>();

        /// <summary>Raised once per item when its unlock condition is first met.</summary>
        public event Action<ShopItem> ItemAvailable;
        public event Action<ShopItem> Purchased;

        public UpgradeManager(GameState state, Wallet wallet, GameConfig config)
        {
            this.state = state;
            this.wallet = wallet;
            this.config = config;

            foreach (UpgradeData upgrade in config.upgrades) AddUpgrade(upgrade);
            foreach (RecipeData recipe in config.recipes) AddRecipeItems(recipe);

            foreach (ShopItem item in items)
            {
                bool announces = item.Kind == ShopItemKind.Upgrade || item.Kind == ShopItemKind.RecipeUnlock;
                if (announces && item.Condition.type != UnlockType.None && !state.announcedItems.Contains(item.Id))
                    waitingToAnnounce.Add(item);
            }

            RefreshUnlockedRecipes();
        }

        public IReadOnlyList<ShopItem> Items => items;
        public IReadOnlyList<RecipeData> UnlockedRecipes => unlockedRecipes;

        /// <summary>Null if the config has no upgrade with this effect.</summary>
        public ShopItem Find(UpgradeEffect effect) => itemsByEffect.TryGetValue(effect, out ShopItem item) ? item : null;

        public ShopItem Find(string id) => itemsById.TryGetValue(id, out ShopItem item) ? item : null;

        public ShopItem QualityItem(RecipeData recipe) => Find(QualityPrefix + recipe.id);
        public ShopItem SpeedItem(RecipeData recipe) => Find(SpeedPrefix + recipe.id);

        public int LevelOf(UpgradeEffect effect) => Find(effect)?.Level ?? 0;

        /// <summary>Total bonus from an upgrade: level × effectPerLevel.</summary>
        public double Bonus(UpgradeEffect effect)
        {
            ShopItem item = Find(effect);
            return item == null ? 0 : item.Level * item.Upgrade.effectPerLevel;
        }

        public double RecipeDuration(RecipeData recipe) =>
            CostFormula.RecipeDuration(recipe.baseDurationSeconds, SpeedItem(recipe).Level, config.speedPerLevel);

        public double RecipeValue(RecipeData recipe) =>
            CostFormula.RecipeValue(recipe.baseValuePerCucumber, QualityItem(recipe).Level, config.qualityPerLevel);

        public bool IsRecipeUnlocked(RecipeData recipe) =>
            recipe != null && (recipe.unlockCost <= 0 || Find(RecipeUnlockPrefix + recipe.id).IsMaxed);

        /// <summary>How far along a condition is, from 0 to 1.</summary>
        public double Progress(UnlockCondition condition)
        {
            switch (condition.type)
            {
                case UnlockType.LifetimeCoinsAtLeast: return Ratio(state.stats.lifetimeCoins, condition.amount);
                case UnlockType.TotalTapsAtLeast: return Ratio(state.stats.totalTaps, condition.amount);
                case UnlockType.PicklesSoldAtLeast: return Ratio(state.stats.picklesSold, condition.amount);
                case UnlockType.RecipeUnlocked:
                    return IsRecipeUnlocked(Array.Find(config.recipes, r => r.id == condition.recipeId)) ? 1 : 0;
                default: return 1;
            }
        }

        public ShopVisibility Visibility(ShopItem item)
        {
            if (item.IsMaxed) return ShopVisibility.Maxed;

            double progress = Progress(item.Condition);
            if (progress >= 1) return ShopVisibility.Available;
            return progress >= config.teaserThreshold ? ShopVisibility.Teaser : ShopVisibility.Hidden;
        }

        public bool CanAfford(ShopItem item) => wallet.CanAfford(item.Cost);

        /// <summary>Buys one level. A failure for lack of coins makes the wallet raise InsufficientCoins.</summary>
        public bool TryBuy(ShopItem item)
        {
            if (Visibility(item) != ShopVisibility.Available) return false;
            if (!wallet.TrySpendCoins(item.Cost)) return false;

            item.AddLevel();
            if (item.Kind == ShopItemKind.RecipeUnlock) RefreshUnlockedRecipes();
            Purchased?.Invoke(item);
            return true;
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Debug panel only: sets a level for free, within the item's range.</summary>
        public void SetLevel(ShopItem item, int level)
        {
            item.SetLevel(Math.Max(0, Math.Min(level, item.MaxLevel)));
            RefreshUnlockedRecipes();
            Purchased?.Invoke(item);
        }
#endif

        /// <summary>Announces items whose unlock condition has just been met.</summary>
        public void CheckUnlocks()
        {
            for (int i = waitingToAnnounce.Count - 1; i >= 0; i--)
            {
                ShopItem item = waitingToAnnounce[i];
                if (Progress(item.Condition) < 1) continue;

                waitingToAnnounce.RemoveAt(i);
                state.announcedItems.Add(item.Id);
                ItemAvailable?.Invoke(item);
            }
        }

        private static double Ratio(double value, double target) => target <= 0 ? 1 : Math.Min(1, value / target);

        private void AddUpgrade(UpgradeData upgrade)
        {
            ShopItem item = AddItem(upgrade.id, ShopItemKind.Upgrade, upgrade.section, upgrade.displayName,
                upgrade.baseCost, upgrade.growth, upgrade.maxLevel, upgrade.unlockCondition);
            item.Upgrade = upgrade;
            itemsByEffect[upgrade.effect] = item;
        }

        private void AddRecipeItems(RecipeData recipe)
        {
            bool free = recipe.unlockCost <= 0;
            if (!free)
            {
                AddItem(RecipeUnlockPrefix + recipe.id, ShopItemKind.RecipeUnlock, ShopSection.Recipes, recipe.displayName,
                    recipe.unlockCost, 1, 1, recipe.unlockCondition).Recipe = recipe;
            }

            // The recipe's own upgrades appear once the recipe is owned.
            var owned = new UnlockCondition { type = free ? UnlockType.None : UnlockType.RecipeUnlocked, recipeId = recipe.id };
            double baseCost = Math.Max(config.recipeUpgradeMinCost, recipe.unlockCost * config.recipeUpgradeCostFactor);

            AddItem(QualityPrefix + recipe.id, ShopItemKind.RecipeQuality, ShopSection.Recipes, Strings.BetterIngredients,
                baseCost, config.qualityGrowth, config.qualityMaxLevel, owned).Recipe = recipe;
            AddItem(SpeedPrefix + recipe.id, ShopItemKind.RecipeSpeed, ShopSection.Recipes, Strings.FasterFermentation,
                baseCost, config.speedGrowth, config.speedMaxLevel, owned).Recipe = recipe;
        }

        private ShopItem AddItem(string id, ShopItemKind kind, ShopSection section, string displayName, double baseCost,
            double growth, int maxLevel, UnlockCondition condition)
        {
            UpgradeLevel saved = state.upgradeLevels.Find(l => l.id == id);
            if (saved == null)
            {
                saved = new UpgradeLevel { id = id };
                state.upgradeLevels.Add(saved);
            }

            var item = new ShopItem(id, kind, section, displayName, baseCost, growth, maxLevel, condition, saved);
            items.Add(item);
            itemsById[id] = item;
            return item;
        }

        private void RefreshUnlockedRecipes()
        {
            unlockedRecipes.Clear();
            foreach (RecipeData recipe in config.recipes)
                if (IsRecipeUnlocked(recipe)) unlockedRecipes.Add(recipe);
        }
    }
}
