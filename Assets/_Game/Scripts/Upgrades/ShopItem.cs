namespace CucumberClicker
{
    public enum ShopItemKind { Upgrade, RecipeUnlock, RecipeQuality, RecipeSpeed }

    public enum ShopVisibility { Hidden, Teaser, Available, Maxed }

    /// <summary>One thing the player can buy. Upgrades and recipe purchases share these rules.</summary>
    public class ShopItem
    {
        private readonly UpgradeLevel saved;

        public ShopItem(string id, ShopItemKind kind, ShopSection section, string displayName, double baseCost,
            double growth, int maxLevel, UnlockCondition condition, UpgradeLevel saved)
        {
            Id = id;
            Kind = kind;
            Section = section;
            DisplayName = displayName;
            BaseCost = baseCost;
            Growth = growth;
            MaxLevel = maxLevel;
            Condition = condition;
            this.saved = saved;
        }

        public string Id { get; }
        public ShopItemKind Kind { get; }
        public ShopSection Section { get; }
        public string DisplayName { get; }
        public double BaseCost { get; }
        public double Growth { get; }
        public int MaxLevel { get; }
        public UnlockCondition Condition { get; }

        /// <summary>Set for <see cref="ShopItemKind.Upgrade"/>.</summary>
        public UpgradeData Upgrade { get; set; }

        /// <summary>Set for the recipe kinds.</summary>
        public RecipeData Recipe { get; set; }

        public int Level => saved.level;
        public bool IsMaxed => Level >= MaxLevel;
        public double Cost => CostFormula.Cost(BaseCost, Growth, Level);

        public void AddLevel() => saved.level++;
    }
}
