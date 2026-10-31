using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>
    /// Keeps the tab badges current. Lives on the tab row because only the selected tab's
    /// panel is active, and finished jars must be noticeable with the drawer collapsed.
    /// </summary>
    public class DrawerBadges : MonoBehaviour
    {
        [SerializeField] private DrawerTabs tabs;
        [SerializeField] private DrawerController drawer;
        [SerializeField] private Toast toast;

        private TabBadge pickleBadge, marketBadge;
        private int lastReady;

        private void Start()
        {
            pickleBadge = tabs.GetBadge(DrawerTab.Pickle);
            marketBadge = tabs.GetBadge(DrawerTab.Market);

            UpgradeManager upgrades = GameManager.Instance.Upgrades;
            upgrades.ItemAvailable += OnItemAvailable;
            upgrades.Purchased += OnPurchased;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Upgrades.ItemAvailable -= OnItemAvailable;
            GameManager.Instance.Upgrades.Purchased -= OnPurchased;
        }

        private void OnItemAvailable(ShopItem item)
        {
            toast.Show(item.Kind == ShopItemKind.RecipeUnlock
                ? Strings.NewRecipe(item.DisplayName)
                : Strings.NewUpgrade(item.DisplayName));
            MarkNew(DrawerTab.Upgrades);
        }

        private void OnPurchased(ShopItem item)
        {
            if (item.Kind == ShopItemKind.RecipeUnlock) MarkNew(DrawerTab.Pickle);
        }

        private void MarkNew(DrawerTab tab)
        {
            bool open = tabs.Selected == tab && drawer.State != DrawerState.Collapsed;
            if (!open) tabs.GetBadge(tab).SetDot(true);
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            int ready = game.Pickling.ReadyCount;

            pickleBadge.SetCount(ready);
            marketBadge.SetCount(game.State.pickles.Count);

            if (ready > lastReady && drawer.State == DrawerState.Collapsed) drawer.GlowHandle();
            lastReady = ready;
        }
    }
}
