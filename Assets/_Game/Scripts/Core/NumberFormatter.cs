using System;
using System.Globalization;

namespace CucumberClicker
{
    /// <summary>
    /// Shared number display: 999, 1.23K, 45.6M, 7.89B, 1.00T, then aa, ab, ...
    /// Values are truncated (never rounded up) so the player never sees more than they have.
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] NamedSuffixes = { "", "K", "M", "B", "T" };
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "∞";
            if (value < 0) return "-" + Format(-value);
            if (value < 1000) return Math.Floor(value).ToString("0", Invariant);

            int tier = (int)Math.Floor(Math.Log10(value) / 3);
            double scaled = value / Math.Pow(1000, tier);

            // Guard against floating point putting 1000.0 into the lower tier.
            if (scaled >= 1000)
            {
                tier++;
                scaled /= 1000;
            }

            return Truncate3(scaled) + Suffix(tier);
        }

        /// <summary>Rates like "+3.5/s" keep one decimal below 1000.</summary>
        public static string FormatRate(double perSecond)
        {
            string number = perSecond < 1000
                ? (Math.Floor(perSecond * 10 + Epsilon) / 10).ToString("0.#", Invariant)
                : Format(perSecond);
            return "+" + number + "/s";
        }

        /// <summary>Remaining time: "45s", "2m 13s", "3h 05m", "1d 02h".</summary>
        public static string FormatDuration(double seconds)
        {
            if (seconds < 0) seconds = 0;
            long total = (long)Math.Ceiling(seconds);
            long d = total / 86400, h = total / 3600 % 24, m = total / 60 % 60, s = total % 60;

            if (d > 0) return $"{d}d {h:00}h";
            if (h > 0) return $"{h}h {m:00}m";
            if (m > 0) return $"{m}m {s:00}s";
            return $"{s}s";
        }

        // Tiny epsilon so 7.89 (stored as 7.8899999...) doesn't truncate to 7.88.
        private const double Epsilon = 1e-9;

        private static string Truncate3(double scaled)
        {
            if (scaled < 10) return (Math.Floor(scaled * 100 + Epsilon) / 100).ToString("0.00", Invariant);
            if (scaled < 100) return (Math.Floor(scaled * 10 + Epsilon) / 10).ToString("0.0", Invariant);
            return Math.Floor(scaled + Epsilon).ToString("0", Invariant);
        }

        private static string Suffix(int tier)
        {
            if (tier < NamedSuffixes.Length) return NamedSuffixes[tier];

            int index = tier - NamedSuffixes.Length; // 0 => "aa"
            char first = (char)('a' + index / 26 % 26);
            char second = (char)('a' + index % 26);
            return new string(new[] { first, second });
        }
    }
}
