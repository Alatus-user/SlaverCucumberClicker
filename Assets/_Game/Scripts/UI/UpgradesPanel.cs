using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>Drawer tab listing every shop item, grouped into sections.</summary>
    public class UpgradesPanel : MonoBehaviour
    {
        [Tooltip("Inactive child of the content used as the template.")]
        [SerializeField] private ItemRowView rowTemplate;
        [SerializeField] private TextMeshProUGUI headerTemplate;
        [SerializeField] private Sprite upgradeIcon;
        [SerializeField] private Sprite recipeIcon;

        private class Row
        {
            public ShopItem item;
            public ItemRowView view;
        }

        private class Section
        {
            public GameObject header;
            public readonly List<Row> rows = new List<Row>();
        }

        private readonly List<Section> sections = new List<Section>();

        private void Start()
        {
            UpgradeManager upgrades = GameManager.Instance.Upgrades;
            foreach (ShopSection id in Enum.GetValues(typeof(ShopSection)))
            {
                var section = new Section();
                TextMeshProUGUI header = Instantiate(headerTemplate, headerTemplate.transform.parent);
                header.text = Strings.SectionName(id);
                section.header = header.gameObject;

                foreach (ShopItem item in upgrades.Items)
                    if (item.Section == id) section.rows.Add(AddRow(item));
                sections.Add(section);
            }
        }

        private Row AddRow(ShopItem item)
        {
            ItemRowView view = Instantiate(rowTemplate, rowTemplate.transform.parent);
            view.SetIcon(item.Recipe != null ? recipeIcon : upgradeIcon);
            view.Clicked += () =>
            {
                if (GameManager.Instance.Upgrades.TryBuy(item)) Haptics.Light();
                else view.CancelHold();
            };
            return new Row { item = item, view = view };
        }

        private void Update()
        {
            foreach (Section section in sections)
            {
                bool anyVisible = false;
                foreach (Row row in section.rows) anyVisible |= Refresh(row);
                if (section.header.activeSelf != anyVisible) section.header.SetActive(anyVisible);
            }
        }

        /// <summary>Returns whether the row is showing.</summary>
        private static bool Refresh(Row row)
        {
            UpgradeManager upgrades = GameManager.Instance.Upgrades;
            ShopItem item = row.item;
            ShopVisibility visibility = upgrades.Visibility(item);

            // An owned recipe is replaced by its own upgrade rows.
            bool visible = visibility != ShopVisibility.Hidden &&
                           !(item.Kind == ShopItemKind.RecipeUnlock && visibility == ShopVisibility.Maxed);
            if (row.view.gameObject.activeSelf != visible) row.view.gameObject.SetActive(visible);
            if (!visible) return false;

            if (visibility == ShopVisibility.Teaser)
            {
                row.view.Show(Strings.TeaserName, Strings.TeaserInfo, UIStyle.Silhouette);
                row.view.HideButton();
                return true;
            }

            row.view.Show(item.DisplayName, Info(item), item.Recipe != null ? item.Recipe.jarTint : UIStyle.Affordable);
            if (visibility == ShopVisibility.Maxed) row.view.ShowMaxed(Strings.Maxed);
            else row.view.ShowPrice(NumberFormatter.Format(item.Cost), upgrades.CanAfford(item));
            return true;
        }

        private static string Info(ShopItem item)
        {
            GameManager game = GameManager.Instance;
            switch (item.Kind)
            {
                case ShopItemKind.RecipeUnlock:
                    return Strings.RecipeInfo(NumberFormatter.Format(game.Market.PickleSellValue(game.Pickling.ValuePerPickle(item.Recipe))),
                        NumberFormatter.FormatDuration(game.Pickling.DurationSeconds(item.Recipe)));
                case ShopItemKind.RecipeQuality:
                    return LevelInfo(item, Strings.QualityEffect(item.Recipe.displayName, Percent(game.Config.qualityPerLevel)));
                case ShopItemKind.RecipeSpeed:
                    return LevelInfo(item, Strings.SpeedEffect(item.Recipe.displayName, Percent(game.Config.speedPerLevel)));
                default:
                    return LevelInfo(item, item.Upgrade.description);
            }
        }

        private static string LevelInfo(ShopItem item, string effect) =>
            Strings.ShopLevelInfo(item.Level.ToString(), item.MaxLevel.ToString(), effect);

        private static string Percent(double fraction) => Mathf.RoundToInt((float)(fraction * 100)).ToString();
    }
}
