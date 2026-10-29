using System.Collections.Generic;

namespace CucumberClicker
{
    /// <summary>Sliding one-second window that caps taps across all fingers.</summary>
    public class TapLimiter
    {
        private readonly Queue<double> recent = new Queue<double>();
        private readonly int maxPerSecond;

        public TapLimiter(int maxPerSecond) => this.maxPerSecond = maxPerSecond;

        /// <param name="now">Any monotonic time in seconds (e.g. Time.unscaledTimeAsDouble).</param>
        public bool TryTap(double now)
        {
            while (recent.Count > 0 && now - recent.Peek() >= 1.0) recent.Dequeue();
            if (recent.Count >= maxPerSecond) return false;
            recent.Enqueue(now);
            return true;
        }
    }
}
