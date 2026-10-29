using System;

namespace CucumberClicker
{
    public class Market
    {
        private readonly Wallet wallet;
        private readonly GameConfig config;

        public Market(Wallet wallet, GameConfig config)
        {
            this.wallet = wallet;
            this.config = config;
        }

        /// <summary>Multiplier from the Haggling upgrade (Phase 4). 1 = no bonus.</summary>
        public double SellPriceMultiplier { get; set; } = 1;

        public double RawPrice => config.rawCucumberSellPrice * SellPriceMultiplier;

        /// <summary>Sells up to <paramref name="amount"/> whole cucumbers. Returns coins earned.</summary>
        public double SellRaw(double amount)
        {
            double sold = Math.Min(Math.Floor(amount), wallet.WholeCucumbers);
            if (sold <= 0 || !wallet.TryRemoveCucumbers(sold)) return 0;

            double earned = sold * RawPrice;
            wallet.AddCoins(earned);
            return earned;
        }

        public double SellAllRaw() => SellRaw(wallet.WholeCucumbers);

        public double ValueOfAllRaw => wallet.WholeCucumbers * RawPrice;

        /// <summary>Everything the player can sell right now. Pickles join in Phase 2.</summary>
        public double ValueOfEverything => ValueOfAllRaw;

        public double SellEverything() => SellAllRaw();
    }
}
