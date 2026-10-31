using System;

namespace CucumberClicker
{
    public enum JarStatus { Empty, Running, Ready }

    /// <summary>
    /// One jar. A batch is a snapshot taken when it starts, so later upgrades don't change it.
    /// Finishing depends only on wall-clock time, so jars keep running while the game is closed.
    /// </summary>
    [Serializable]
    public class JarState
    {
        public string recipeId;
        public double amount; // 0 = empty
        public long startUtcTicks;
        public double durationSeconds;
        public double valuePerPickle;

        private long EndUtcTicks => startUtcTicks + (long)(durationSeconds * TimeSpan.TicksPerSecond);

        public JarStatus StatusAt(long nowTicks)
        {
            if (amount <= 0) return JarStatus.Empty;
            return nowTicks >= EndUtcTicks ? JarStatus.Ready : JarStatus.Running;
        }

        /// <summary>0 to 1. Stays in range if the device clock moved before the batch started.</summary>
        public double ProgressAt(long nowTicks)
        {
            double elapsed = (double)(nowTicks - startUtcTicks) / TimeSpan.TicksPerSecond;
            return Math.Max(0, Math.Min(1, elapsed / durationSeconds));
        }

        public double RemainingSecondsAt(long nowTicks) =>
            Math.Max(0, (double)(EndUtcTicks - nowTicks) / TimeSpan.TicksPerSecond);

        public void Clear()
        {
            recipeId = null;
            amount = 0;
            startUtcTicks = 0;
            durationSeconds = 0;
            valuePerPickle = 0;
        }
    }

    /// <summary>Pickles of one recipe. totalValue handles batches made at different quality levels.</summary>
    [Serializable]
    public class PickleStack
    {
        public string recipeId;
        public double count;
        public double totalValue;
    }
}
