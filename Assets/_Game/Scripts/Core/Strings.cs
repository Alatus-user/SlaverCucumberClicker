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
        public static string RawCucumbersInfo(string level, string pickleMultiplier, string owned) =>
            $"Market Lv {level}  ·  Pickles x{pickleMultiplier}  ·  You have {owned}";
        public static string SellAmount(string amount) => $"Sell {amount}";
        public const string SellAll = "Sell All";
        public const string PicklesHeader = "Pickles";
        public const string NoPicklesYet = "No pickles yet.\nFill a jar in the Pickle tab!";
        public static string CoinGain(string coins) => $"+{coins}";
        public static string PickleRowInfo(string owned, string price) => $"You have {owned}  ·  {price} coins each";

        // Pickling
        public const string CollectAll = "Collect All";
        public const string JarEmpty = "Empty Jar";
        public const string JarTapToFill = "Tap to fill";
        public const string JarReady = "Ready!";
        public const string JarTapToCollect = "Tap to collect";
        public static string JarRunning(string amount, string remaining) => $"{amount} in jar  ·  {remaining}";
        public static string PickleGain(string amount) => $"+{amount} pickled!";
        public const string FillJarTitle = "Fill the Jar";
        public static string RecipeInfo(string value, string duration) => $"{value} coins each  ·  {duration}";
        public static string BatchAmount(string amount, string max) => $"Cucumbers: {amount} / {max}";
        public static string BatchPreview(string coins, string duration) => $"Worth {coins} coins in {duration}";
        public const string NoCucumbersToPickle = "Tap the big cucumber to grow some first!";
        public const string StartBatch = "Start";

        // Upgrades
        public static string SectionName(ShopSection section) => section switch
        {
            ShopSection.Tapping => "Tapping",
            ShopSection.Jars => "Jars",
            ShopSection.Recipes => "Recipes",
            _ => "Market",
        };
        public const string BetterIngredients = "Better Ingredients";
        public const string FasterFermentation = "Faster Fermentation";
        public static string QualityEffect(string recipe, string percent) => $"{recipe}: +{percent}% value";
        public static string SpeedEffect(string recipe, string percent) => $"{recipe}: -{percent}% time";
        public static string ShopLevelInfo(string level, string max, string effect) => $"Lv {level}/{max}  ·  {effect}";
        public const string TeaserName = "???";
        public const string TeaserInfo = "Keep earning coins to reveal this";
        public const string Maxed = "MAX";
        public static string NewRecipe(string name) => $"New recipe: {name}!";
        public static string NewUpgrade(string name) => $"New upgrade: {name}!";
        public static string JarBuyCost(string cost) => $"{cost} coins";

        // Placeholders for later phases
        public const string ComingSoon = "Coming soon!";
        public const string SettingsComingSoon = "Settings are coming soon!";

        // Dialogs
        public const string QuitQuestion = "Quit Cucumber Clicker?";
        public const string Quit = "Quit";
        public const string Cancel = "Cancel";
    }
}
