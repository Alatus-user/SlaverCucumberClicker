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

        [Header("Offline")]
        [Range(0, 1)] public float offlineEfficiency = 0.5f;
        [Min(0)] public float offlineCapHours = 8;

        public static GameConfig CreateDefault() => CreateInstance<GameConfig>();
    }
}
