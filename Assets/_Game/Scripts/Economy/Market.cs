using System;

namespace CucumberClicker
{
    public class Market
    {
        private readonly GameState state;
        private readonly Wallet wallet;
        private readonly GameConfig config;

        public Market(GameState state, Wallet wallet, GameConfig config)
        {
            this.state = state;
            this.wallet = wallet;
            this.config = config;
        }

        /// <summary>Multiplier from the Haggling upgrade (Phase 4). 1 = no bonus.</summary>
        public double SellPriceMultiplier { get; set; } = 1;

        public double RawPrice => config.rawCucumberSellPrice * SellPriceMultiplier;

        /// <summary>Starts at 1 and rises with every raw sale.</summary>
        public int MarketLevel => state.rawSales + 1;

        /// <summary>Multiplier on pickle value: growth^(market level).</summary>
        public double PickleMultiplier => Math.Pow(config.pickleValueGrowth, MarketLevel);

        /// <summary>Size of the next raw sale: lot(level) = first × growth^(level − 1).</summary>
        public double NextRawSaleAmount =>
            Math.Ceiling(CostFormula.Cost(config.firstRawSaleAmount, config.rawSaleGrowth, MarketLevel - 1));

        public bool CanSellRaw => wallet.WholeCucumbers >= NextRawSaleAmount;

        public double NextRawSaleValue => CanSellRaw ? NextRawSaleAmount * RawPrice : 0;

        /// <summary>Sells exactly the next lot, or nothing if the player doesn't have it. Returns coins earned.</summary>
        public double SellRaw()
        {
            double amount = NextRawSaleAmount;
            if (!wallet.TryRemoveCucumbers(amount)) return 0;

            state.rawSales++;
            double earned = amount * RawPrice;
            wallet.AddCoins(earned);
            return earned;
        }

        /// <summary>What pickles with this batch value sell for right now.</summary>
        public double PickleSellValue(double batchValue) => batchValue * SellPriceMultiplier * PickleMultiplier;

        public double PickleValue(PickleStack stack) => PickleSellValue(stack.totalValue);

        /// <summary>Average price per pickle; a stack can mix batches of different quality.</summary>
        public double PicklePrice(PickleStack stack) => PickleValue(stack) / stack.count;

        /// <summary>Sells one recipe's whole stack. Returns coins earned.</summary>
        public double SellPickles(string recipeId)
        {
            int index = state.pickles.FindIndex(p => p.recipeId == recipeId);
            return index < 0 ? 0 : SellStackAt(index);
        }

        public double SellAllPickles()
        {
            double earned = 0;
            for (int i = state.pickles.Count - 1; i >= 0; i--) earned += SellStackAt(i);
            return earned;
        }

        private double SellStackAt(int index)
        {
            PickleStack stack = state.pickles[index];
            double earned = PickleValue(stack);
            state.pickles.RemoveAt(index);
            state.stats.picklesSold += stack.count;
            wallet.AddCoins(earned);
            return earned;
        }

        public double ValueOfAllPickles
        {
            get
            {
                double value = 0;
                foreach (PickleStack stack in state.pickles) value += PickleValue(stack);
                return value;
            }
        }

        /// <summary>Everything the player can sell right now: all pickles, plus the next raw lot if they have it.</summary>
        public double ValueOfEverything => NextRawSaleValue + ValueOfAllPickles;

        /// <summary>Pickles go first: the raw sale raises the market level, and that must not change the shown total.</summary>
        public double SellEverything() => SellAllPickles() + SellRaw();
    }
}
