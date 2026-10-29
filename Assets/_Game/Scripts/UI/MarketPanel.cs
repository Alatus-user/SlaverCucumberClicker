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
        [SerializeField] private Button sell1Button;
        [SerializeField] private Button sell10Button;
        [SerializeField] private Button sellAllButton;
        [SerializeField] private GameObject noPicklesLabel;
        [SerializeField] private FloatingTextSpawner effects;

        private string shownInfo, shownValue;

        private void Start()
        {
            sell1Button.onClick.AddListener(() => Sold(GameManager.Instance.Market.SellRaw(1), sell1Button));
            sell10Button.onClick.AddListener(() => Sold(GameManager.Instance.Market.SellRaw(10), sell10Button));
            sellAllButton.onClick.AddListener(() => Sold(GameManager.Instance.Market.SellAllRaw(), sellAllButton));
            sellEverythingButton.onClick.AddListener(() =>
                Sold(GameManager.Instance.Market.SellEverything(), sellEverythingButton));
        }

        private void Sold(double coins, Button source)
        {
            if (coins <= 0) return;
            effects.SpawnAtWorld(source.transform.position, Strings.CoinGain(NumberFormatter.Format(coins)), UIStyle.CoinText);
            Haptics.Light();
        }

        private void Update()
        {
            Market market = GameManager.Instance.Market;
            double owned = GameManager.Instance.Wallet.WholeCucumbers;

            UIStyle.SetAvailable(sell1Button, owned >= 1);
            UIStyle.SetAvailable(sell10Button, owned >= 10);
            UIStyle.SetAvailable(sellAllButton, owned >= 1);

            double everything = market.ValueOfEverything;
            UIStyle.SetAvailable(sellEverythingButton, everything > 0, UIStyle.Highlight);

            string value = everything > 0
                ? Strings.SellEverythingValue(NumberFormatter.Format(everything))
                : Strings.NothingToSell;
            if (value != shownValue) sellEverythingValue.text = shownValue = value;

            string info = Strings.RawCucumbersInfo(NumberFormatter.Format(owned), NumberFormatter.Format(market.RawPrice));
            if (info != shownInfo) rawInfo.text = shownInfo = info;

            // Pickles join the market in Phase 2.
            if (!noPicklesLabel.activeSelf) noPicklesLabel.SetActive(true);
        }
    }
}
