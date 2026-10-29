using System;

namespace CucumberClicker
{
    /// <summary>
    /// The only source of wall-clock time for game logic. Tests and the debug panel
    /// can shift it with <see cref="Offset"/> or replace it with <see cref="Override"/>.
    /// </summary>
    public static class TimeService
    {
        public static TimeSpan Offset { get; set; } = TimeSpan.Zero;
        public static Func<DateTime> Override { get; set; }

        public static DateTime UtcNow => (Override != null ? Override() : DateTime.UtcNow) + Offset;
        public static long UtcNowTicks => UtcNow.Ticks;

        public static void Reset()
        {
            Offset = TimeSpan.Zero;
            Override = null;
        }
    }
}
