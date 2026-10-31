using UnityEngine;

namespace CucumberClicker
{
    /// <summary>One pickling method. A recipe with no unlock cost is owned from the start.</summary>
    [CreateAssetMenu(menuName = "Cucumber Clicker/Recipe", fileName = "Recipe")]
    public class RecipeData : ScriptableObject
    {
        public string id;
        public string displayName;
        [Min(1)] public double baseDurationSeconds = 180;
        [Tooltip("Coins per cucumber put in the jar.")]
        [Min(0)] public double baseValuePerCucumber = 10;
        public Color jarTint = Color.green;
        public UnlockCondition unlockCondition = new UnlockCondition();
        [Min(0)] public double unlockCost;
    }
}
