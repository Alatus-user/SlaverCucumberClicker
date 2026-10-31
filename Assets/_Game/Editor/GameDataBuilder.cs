using System;
using UnityEditor;
using UnityEngine;

namespace CucumberClicker.EditorTools
{
    /// <summary>
    /// Creates the recipe and upgrade assets and lists them in the GameConfig. These tables only
    /// seed new assets, so tuning done in the Inspector survives scene rebuilds.
    /// </summary>
    public static class GameDataBuilder
    {
        private const string DataFolder = "Assets/_Game/Data";
        private const string RecipesFolder = DataFolder + "/Recipes";
        private const string UpgradesFolder = DataFolder + "/Upgrades";

        public static void Populate(GameConfig config)
        {
            EnsureFolder(RecipesFolder, "Recipes");
            EnsureFolder(UpgradesFolder, "Upgrades");

            config.recipes = new[]
            {
                Recipe("quick_brine", "Quick Brine", 180, 50, new Color(0.6f, 0.78f, 0.22f), 0, 0),
                Recipe("garlic_dill", "Garlic Dill", 600, 125, new Color(0.35f, 0.62f, 0.3f), 30_000, 15_000),
                Recipe("bread_butter", "Bread & Butter", 1800, 300, new Color(0.93f, 0.76f, 0.3f), 200_000, 100_000),
                Recipe("spicy_chili", "Spicy Chili", 3600, 500, new Color(0.87f, 0.25f, 0.18f), 1_000_000, 500_000),
                Recipe("kosher_sour", "Kosher Sour", 14400, 1500, new Color(0.45f, 0.55f, 0.2f), 10_000_000, 4_000_000),
                Recipe("cucumber_kimchi", "Cucumber Kimchi", 28800, 2750, new Color(0.9f, 0.42f, 0.2f), 50_000_000, 20_000_000),
                Recipe("golden_royal", "Golden Royal Pickle", 86400, 7500, new Color(0.95f, 0.72f, 0.1f), 500_000_000, 200_000_000),
            };

            config.upgrades = new[]
            {
                Upgrade("tap_power", "Stronger Hands", "+1 cucumber per tap", ShopSection.Tapping,
                    UpgradeEffect.TapPower, 1, 15, 1.5, 100, 0),
                Upgrade("tap_multiplier", "Golden Gloves", "x2 tap power", ShopSection.Tapping,
                    UpgradeEffect.TapMultiplier, 2, 100_000, 25, 10, 50_000),
                Upgrade("jar_count", "Extra Jar", "+1 jar", ShopSection.Jars,
                    UpgradeEffect.JarCount, 1, 100, 4, 7, 0),
                Upgrade("jar_capacity", "Bigger Jars", "+5 capacity for every jar", ShopSection.Jars,
                    UpgradeEffect.JarCapacity, 5, 50, 1.8, 40, 0),
                Upgrade("market_haggle", "Haggling", "+5% sell price on everything", ShopSection.Market,
                    UpgradeEffect.MarketHaggle, 0.05, 40_000, 2.2, 20, 20_000),
            };

            EditorUtility.SetDirty(config);
        }

        private static void EnsureFolder(string path, string name)
        {
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(DataFolder, name);
        }

        private static UnlockCondition LifetimeCoins(double coins) => new UnlockCondition
        {
            type = coins > 0 ? UnlockType.LifetimeCoinsAtLeast : UnlockType.None,
            amount = coins,
        };

        private static RecipeData Recipe(string id, string displayName, double seconds, double value, Color tint,
            double visibleAtCoins, double unlockCost)
        {
            return LoadOrCreate<RecipeData>($"{RecipesFolder}/{id}.asset", recipe =>
            {
                recipe.id = id;
                recipe.displayName = displayName;
                recipe.baseDurationSeconds = seconds;
                recipe.baseValuePerCucumber = value;
                recipe.jarTint = tint;
                recipe.unlockCondition = LifetimeCoins(visibleAtCoins);
                recipe.unlockCost = unlockCost;
            });
        }

        private static UpgradeData Upgrade(string id, string displayName, string description, ShopSection section,
            UpgradeEffect effect, double effectPerLevel, double baseCost, double growth, int maxLevel, double visibleAtCoins)
        {
            return LoadOrCreate<UpgradeData>($"{UpgradesFolder}/{id}.asset", upgrade =>
            {
                upgrade.id = id;
                upgrade.displayName = displayName;
                upgrade.description = description;
                upgrade.section = section;
                upgrade.effect = effect;
                upgrade.effectPerLevel = effectPerLevel;
                upgrade.baseCost = baseCost;
                upgrade.growth = growth;
                upgrade.maxLevel = maxLevel;
                upgrade.unlockCondition = LifetimeCoins(visibleAtCoins);
            });
        }

        private static T LoadOrCreate<T>(string path, Action<T> seed) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            seed(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
