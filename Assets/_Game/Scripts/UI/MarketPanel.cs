using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    public class MarketPanel : MonoBehaviour
    {
        [SerializeField] private Button sellEverythingButton;
        [SerializeField] private TextMeshProUGUI sellEverythingValue;
        [SerializeField] private TextMeshProUGUI rawInfo;
        [SerializeField] private Button sellRawButton;
        [SerializeField] private TextMeshProUGUI sellRawLabel;
        [SerializeField] private GameObject noPicklesLabel;
        [Tooltip("Inactive child of the pickle list used as the template.")]
        [SerializeField] private ItemRowView pickleRowTemplate;
        [SerializeField] private FloatingTextSpawner effects;

        private readonly List<ItemRowView> pickleRows = new List<ItemRowView>();
        private string shownInfo, shownValue, shownSellRaw;

        private void Start()
        {
            sellRawButton.onClick.AddListener(() => Sold(GameManager.Instance.Market.SellRaw(), sellRawButton.transform));
            sellEverythingButton.onClick.AddListener(() =>
                Sold(GameManager.Instance.Market.SellEverything(), sellEverythingButton.transform));
        }

        private void Sold(double coins, Transform source)
        {
            if (coins <= 0) return;
            effects.SpawnAtWorld(source.position, Strings.CoinGain(NumberFormatter.Format(coins)), UIStyle.CoinText);
            Haptics.Light();
        }

        private void Update()
        {
            Market market = GameManager.Instance.Market;
            double owned = GameManager.Instance.Wallet.WholeCucumbers;

            UIStyle.SetAvailable(sellRawButton, market.CanSellRaw);
            string sellRaw = Strings.SellAmount(NumberFormatter.Format(market.NextRawSaleAmount));
            if (sellRaw != shownSellRaw) sellRawLabel.text = shownSellRaw = sellRaw;

            double everything = market.ValueOfEverything;
            UIStyle.SetAvailable(sellEverythingButton, everything > 0, UIStyle.Highlight);

            string value = everything > 0
                ? Strings.SellEverythingValue(NumberFormatter.Format(everything))
                : Strings.NothingToSell;
            if (value != shownValue) sellEverythingValue.text = shownValue = value;

            string info = Strings.RawCucumbersInfo(market.MarketLevel.ToString(),
                NumberFormatter.Format(market.PickleMultiplier), NumberFormatter.Format(owned));
            if (info != shownInfo) rawInfo.text = shownInfo = info;

            RefreshPickleRows();
        }

        private void RefreshPickleRows()
        {
            GameManager game = GameManager.Instance;
            List<PickleStack> stacks = game.State.pickles;

            bool none = stacks.Count == 0;
            if (noPicklesLabel.activeSelf != none) noPicklesLabel.SetActive(none);

            while (pickleRows.Count < stacks.Count) AddPickleRow();
            for (int i = 0; i < pickleRows.Count; i++)
            {
                bool used = i < stacks.Count;
                if (pickleRows[i].gameObject.activeSelf != used) pickleRows[i].gameObject.SetActive(used);
                if (!used) continue;

                PickleStack stack = stacks[i];
                RecipeData recipe = game.Pickling.FindRecipe(stack.recipeId);
                pickleRows[i].Show(recipe != null ? recipe.displayName : stack.recipeId,
                    Strings.PickleRowInfo(NumberFormatter.Format(stack.count), NumberFormatter.Format(game.Market.PicklePrice(stack))),
                    recipe != null ? recipe.jarTint : UIStyle.Unaffordable);
            }
        }

        private void AddPickleRow()
        {
            int index = pickleRows.Count;
            ItemRowView row = Instantiate(pickleRowTemplate, pickleRowTemplate.transform.parent);
            row.gameObject.SetActive(true);
            row.Clicked += () =>
            {
                GameManager game = GameManager.Instance;
                Sold(game.Market.SellPickles(game.State.pickles[index].recipeId), row.ButtonTransform);
            };
            pickleRows.Add(row);
        }
    }
}
