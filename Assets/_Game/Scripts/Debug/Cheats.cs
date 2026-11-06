#if DEVELOPMENT_BUILD || UNITY_EDITOR || CUCUMBER_CHEATS
using System;

namespace CucumberClicker.DebugTools
{
    /// <summary>Debug-only changes to the game state. Compiled out of release builds.</summary>
    public class Cheats
    {
        private const int MaxMarketLevel = 1000; // Lots stay inside the range of a double.

        private readonly GameState state;
        private readonly UpgradeManager upgrades;

        public Cheats(GameState state, UpgradeManager upgrades)
        {
            this.state = state;
            this.upgrades = upgrades;
        }

        public int MarketLevel
        {
            get => state.rawSales + 1;
            set => state.rawSales = Math.Max(1, Math.Min(value, MaxMarketLevel)) - 1;
        }

        /// <summary>Extra Jar and recipe unlocks only go up: lowering them would remove running jars and picker rows.</summary>
        public void SetItemLevel(ShopItem item, int level)
        {
            bool oneWay = item.Kind == ShopItemKind.RecipeUnlock || item.Upgrade?.effect == UpgradeEffect.JarCount;
            upgrades.SetLevel(item, oneWay ? Math.Max(level, item.Level) : level);
        }

        /// <summary>Makes every running jar Ready now. Returns how many jars finished.</summary>
        public int FinishAllJars()
        {
            long now = TimeService.UtcNowTicks;
            int finished = 0;
            foreach (JarState jar in state.jars)
            {
                if (jar.StatusAt(now) != JarStatus.Running) continue;
                jar.startUtcTicks = now - (long)(jar.durationSeconds * TimeSpan.TicksPerSecond);
                finished++;
            }
            return finished;
        }
    }
}
#endif
