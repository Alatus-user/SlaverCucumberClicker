using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>Shared colors so every screen looks like one game.</summary>
    public static class UIStyle
    {
        public static readonly Color Affordable = new Color(0.33f, 0.69f, 0.31f);
        public static readonly Color Unaffordable = new Color(0.66f, 0.66f, 0.66f);
        public static readonly Color Highlight = new Color(1f, 0.66f, 0.15f);
        public static readonly Color CoinText = new Color(0.85f, 0.6f, 0.05f);

        /// <summary>Green and clickable, or grey and disabled. Buttons use no color transition.</summary>
        public static void SetAvailable(Button button, bool available, Color? availableColor = null)
        {
            if (button.interactable != available) button.interactable = available;
            if (button.targetGraphic != null)
                button.targetGraphic.color = available ? availableColor ?? Affordable : Unaffordable;
        }
    }
}
