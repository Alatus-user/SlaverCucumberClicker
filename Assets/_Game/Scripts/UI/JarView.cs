using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>One card in the jar grid: Empty, Running (progress + timer) or Ready (bouncing, glowing).</summary>
    public class JarView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image glow;
        [SerializeField] private Image jarIcon;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI status;
        [SerializeField] private GameObject progressBar;
        [SerializeField] private RectTransform progressFill;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Color emptyTint = new Color(0.78f, 0.75f, 0.66f);
        [SerializeField] private float bounceSpeed = 5;
        [SerializeField] private float bounceScale = 0.035f;

        public event Action Clicked;

        private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());

        public void Render(JarState jar, RecipeData recipe, long nowTicks)
        {
            JarStatus jarStatus = jar.StatusAt(nowTicks);
            bool ready = jarStatus == JarStatus.Ready;
            bool running = jarStatus == JarStatus.Running;
            Color tint = recipe != null ? recipe.jarTint : emptyTint;
            string recipeName = recipe != null ? recipe.displayName : jar.recipeId;

            jarIcon.color = jarStatus == JarStatus.Empty ? emptyTint : tint;
            SetText(title, jarStatus switch
            {
                JarStatus.Ready => Strings.JarReady,
                JarStatus.Running => recipeName,
                _ => Strings.JarEmpty,
            });
            SetText(status, jarStatus switch
            {
                JarStatus.Ready => Strings.JarTapToCollect,
                JarStatus.Running => Strings.JarRunning(NumberFormatter.Format(jar.amount),
                    NumberFormatter.FormatDuration(jar.RemainingSecondsAt(nowTicks))),
                _ => Strings.JarTapToFill,
            });

            if (progressBar.activeSelf != running) progressBar.SetActive(running);
            if (running)
            {
                progressFillImage.color = tint;
                progressFill.anchorMax = new Vector2((float)jar.ProgressAt(nowTicks), 1);
            }

            float pulse = ready ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * bounceSpeed)) : 0;
            transform.localScale = Vector3.one * (1 + bounceScale * pulse);
            Color glowColor = glow.color;
            glowColor.a = pulse;
            glow.color = glowColor;
        }

        /// <summary>A slot the player can buy, showing the next jar's price.</summary>
        public void RenderLocked(string jarName, string price)
        {
            jarIcon.color = emptyTint;
            SetText(title, jarName);
            SetText(status, price);
            if (progressBar.activeSelf) progressBar.SetActive(false);
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label.text != value) label.text = value;
        }
    }
}
