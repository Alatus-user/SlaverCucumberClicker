using UnityEngine;

namespace CucumberClicker
{
    public enum ShopSection { Tapping, Jars, Recipes, Market }

    public enum UpgradeEffect { TapPower, TapMultiplier, JarCount, JarCapacity, MarketHaggle }

    /// <summary>A repeatable upgrade: cost(level) = baseCost × growth^level.</summary>
    [CreateAssetMenu(menuName = "Cucumber Clicker/Upgrade", fileName = "Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string id;
        public string displayName;
        [Tooltip("Shown on the shop row, e.g. \"+5 capacity for every jar\".")]
        public string description;
        public ShopSection section;
        public UpgradeEffect effect;
        [Tooltip("Jars, capacity or sell price bonus per level. Tap upgrades use CostFormula.TapPower instead.")]
        public double effectPerLevel = 1;
        [Min(0)] public double baseCost = 100;
        [Min(1)] public double growth = 1.5;
        [Min(1)] public int maxLevel = 10;
        public UnlockCondition unlockCondition = new UnlockCondition();
    }
}
