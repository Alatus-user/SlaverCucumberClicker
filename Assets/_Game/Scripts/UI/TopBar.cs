using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>Coins, cucumbers, production rate and the settings button. Always on top.</summary>
    public class TopBar : MonoBehaviour
    {
        [SerializeField] private CountUpLabel coins;
        [SerializeField] private CountUpLabel cucumbers;
        [SerializeField] private TextMeshProUGUI rateLabel;
        [SerializeField] private RectTransform coinGroup;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Toast toast;

        [Header("Can't afford feedback")]
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeDistance = 18;
        [SerializeField] private Color shakeColor = new Color(0.9f, 0.2f, 0.2f);

        private float shakeTime = float.MaxValue;
        private Vector2 coinGroupHome;
        private string shownRate;

        private void Start()
        {
            coinGroupHome = coinGroup.anchoredPosition;
            GameManager.Instance.Wallet.InsufficientCoins += Shake;
            settingsButton.onClick.AddListener(() => toast.Show(Strings.SettingsComingSoon));
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.Wallet.InsufficientCoins -= Shake;
        }

        public void Shake()
        {
            shakeTime = 0;
            coins.Flash(shakeColor, shakeDuration);
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            coins.SetTarget(game.Wallet.Coins);
            cucumbers.SetTarget(game.Wallet.WholeCucumbers);

            bool showRate = game.HasProducers;
            if (rateLabel.gameObject.activeSelf != showRate) rateLabel.gameObject.SetActive(showRate);
            if (showRate)
            {
                string rate = NumberFormatter.FormatRate(game.CucumbersPerSecond);
                if (rate != shownRate) rateLabel.text = shownRate = rate;
            }

            shakeTime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(shakeTime / shakeDuration);
            float offset = p >= 1 ? 0 : Mathf.Sin(p * Mathf.PI * 6) * shakeDistance * (1 - p);
            coinGroup.anchoredPosition = coinGroupHome + new Vector2(offset, 0);
        }
    }
}
