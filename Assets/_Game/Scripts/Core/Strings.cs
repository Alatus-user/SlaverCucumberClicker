namespace CucumberClicker
{
    /// <summary>Every player-facing string lives here so the game can be translated later.</summary>
    public static class Strings
    {
        // Cucumber area
        public static string PerTap(string amount) => $"+{amount} per tap";
        public static string FloatingGain(string amount) => $"+{amount}";

        // Tabs
        public const string TabPickle = "Pickle";
        public const string TabMarket = "Market";
        public const string TabUpgrades = "Upgrades";
        public const string TabWorkers = "Workers";

        // Market
        public const string SellEverything = "Sell Everything";
        public static string SellEverythingValue(string coins) => $"+{coins} coins";
        public const string NothingToSell = "Nothing to sell";
        public const string RawCucumbersHeader = "Raw Cucumbers";
        public static string RawCucumbersInfo(string owned, string price) => $"You have {owned}  ·  {price} coin each";
        public const string Sell1 = "Sell 1";
        public const string Sell10 = "Sell 10";
        public const string SellAll = "Sell All";
        public const string PicklesHeader = "Pickles";
        public const string NoPicklesYet = "No pickles yet.\nFill a jar in the Pickle tab!";
        public static string CoinGain(string coins) => $"+{coins}";

        // Placeholders for later phases
        public const string ComingSoon = "Coming soon!";
        public const string SettingsComingSoon = "Settings are coming soon!";

        // Dialogs
        public const string QuitQuestion = "Quit Cucumber Clicker?";
        public const string Quit = "Quit";
        public const string Cancel = "Cancel";
    }
}
