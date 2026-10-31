using UnityEngine;

namespace CucumberClicker
{
    /// <summary>Global balance numbers. Gameplay code reads these; it never hard-codes them.</summary>
    [CreateAssetMenu(menuName = "Cucumber Clicker/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Tapping")]
        [Min(1)] public double baseTapPower = 1;
        [Tooltip("Anti auto-clicker cap, counted across all fingers.")]
        [Min(1)] public int maxTapsPerSecond = 20;

        [Header("Market")]
        [Min(0)] public double rawCucumberSellPrice = 1;
        [Tooltip("Cucumbers in the first raw sale. Each sale is one fixed lot.")]
        [Min(1)] public double firstRawSaleAmount = 1;
        [Tooltip("Each raw sale needs this many times more cucumbers than the last.")]
        [Min(1)] public double rawSaleGrowth = 2;
        [Tooltip("Pickles sell for recipe value × this ^ (market level − 1).")]
        [Min(1)] public double pickleValueGrowth = 1.15;

        [Header("Pickling")]
        [Min(1)] public int startingJars = 1;
        [Tooltip("Cucumbers per jar before upgrades.")]
        [Min(1)] public double baseJarCapacity = 5;
        public RecipeData[] recipes = new RecipeData[0];

        [Header("Shop")]
        public UpgradeData[] upgrades = new UpgradeData[0];
        [Tooltip("Share of an unlock condition at which an item appears as \"???\".")]
        [Range(0, 1)] public float teaserThreshold = 0.5f;

        [Header("Per-recipe upgrades")]
        [Tooltip("Base cost = max(this, recipe unlock cost × factor).")]
        [Min(0)] public double recipeUpgradeMinCost = 50;
        [Min(0)] public double recipeUpgradeCostFactor = 0.5;
        [Tooltip("Better Ingredients: value bonus per level.")]
        [Min(0)] public double qualityPerLevel = 0.1;
        [Min(1)] public double qualityGrowth = 1.6;
        [Min(1)] public int qualityMaxLevel = 25;
        [Tooltip("Faster Fermentation: duration reduction per level.")]
        [Min(0)] public double speedPerLevel = 0.05;
        [Min(1)] public double speedGrowth = 2;
        [Min(1)] public int speedMaxLevel = 10;

        [Header("Offline")]
        [Range(0, 1)] public float offlineEfficiency = 0.5f;
        [Min(0)] public float offlineCapHours = 8;

        public static GameConfig CreateDefault() => CreateInstance<GameConfig>();
    }
}
