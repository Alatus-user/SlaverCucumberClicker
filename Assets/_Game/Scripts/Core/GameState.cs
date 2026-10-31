using System;
using System.Collections.Generic;

namespace CucumberClicker
{
    /// <summary>All mutable game data. Plain C# so it can be saved and unit-tested.</summary>
    [Serializable]
    public class GameState
    {
        public double coins;
        public double cucumbers; // fractional because producers add rate × deltaTime
        public int rawSales; // raw market sales so far; each one raises the market level
        public List<JarState> jars = new List<JarState>();
        public List<PickleStack> pickles = new List<PickleStack>();
        public List<UpgradeLevel> upgradeLevels = new List<UpgradeLevel>();
        public List<string> announcedItems = new List<string>(); // shop items already shown in a toast
        public LifetimeStats stats = new LifetimeStats();
    }

    [Serializable]
    public class UpgradeLevel
    {
        public string id;
        public int level;
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
