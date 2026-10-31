using System;

namespace CucumberClicker
{
    public enum UnlockType { None, LifetimeCoinsAtLeast, TotalTapsAtLeast, RecipeUnlocked, PicklesSoldAtLeast }

    /// <summary>When a shop item appears. <see cref="UpgradeManager.Progress"/> evaluates it.</summary>
    [Serializable]
    public class UnlockCondition
    {
        public UnlockType type;
        public double amount;
        public string recipeId;
    }
}
