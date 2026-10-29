using System;

namespace CucumberClicker
{
    /// <summary>The only place coins and cucumbers change. Keeps lifetime stats in sync.</summary>
    public class Wallet
    {
        private readonly GameState state;

        /// <summary>Raised when a purchase fails, so the top bar can shake the coin counter.</summary>
        public event Action InsufficientCoins;

        public Wallet(GameState state) => this.state = state;

        public double Coins => state.coins;
        public double Cucumbers => state.cucumbers;
        /// <summary>Whole cucumbers the player can actually use.</summary>
        public double WholeCucumbers => Math.Floor(state.cucumbers);

        public void AddCoins(double amount)
        {
            if (amount <= 0) return;
            state.coins += amount;
            state.stats.lifetimeCoins += amount;
        }

        public bool CanAfford(double cost) => state.coins >= cost;

        public bool TrySpendCoins(double cost)
        {
            if (!CanAfford(cost))
            {
                InsufficientCoins?.Invoke();
                return false;
            }
            state.coins -= cost;
            return true;
        }

        public void AddCucumbers(double amount)
        {
            if (amount <= 0) return;
            state.cucumbers += amount;
            state.stats.lifetimeCucumbers += amount;
        }

        public bool TryRemoveCucumbers(double amount)
        {
            if (amount <= 0 || WholeCucumbers < amount) return false;
            state.cucumbers -= amount;
            return true;
        }
    }
}
