using System;

namespace CucumberClicker
{
    public static class CostFormula
    {
        /// <summary>cost(level) = baseCost × growth^level, where level = times already bought.</summary>
        public static double Cost(double baseCost, double growth, int level) => baseCost * Math.Pow(growth, level);

        /// <summary>tapPower = (base + tap_power level) × 2^(tap_multiplier level)</summary>
        public static double TapPower(double basePower, int tapPowerLevel, int tapMultiplierLevel) =>
            (basePower + tapPowerLevel) * Math.Pow(2, tapMultiplierLevel);
    }
}
