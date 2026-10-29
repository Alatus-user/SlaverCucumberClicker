using System;

namespace CucumberClicker
{
    /// <summary>All mutable game data. Plain C# so it can be saved and unit-tested.</summary>
    [Serializable]
    public class GameState
    {
        public double coins;
        public double cucumbers; // fractional because producers add rate × deltaTime
        public LifetimeStats stats = new LifetimeStats();
    }

    [Serializable]
    public class LifetimeStats
    {
        public double lifetimeCoins;
        public double lifetimeCucumbers;
        public long totalTaps;
        public double picklesMade;
        public double picklesSold;
        public long firstPlayUtcTicks;
        public double totalPlayTimeSeconds;
    }
}
