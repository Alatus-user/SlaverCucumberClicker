using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>List row with a tinted icon, a name, an info line and one button.</summary>
    public class ItemRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI infoLabel;
        [SerializeField] private TextMeshProUGUI buttonLabel;

        public event Action Clicked;

        private LongPressRepeat longPress;

        private void Awake()
        {
            longPress = button.GetComponent<LongPressRepeat>();
            if (longPress != null) longPress.Repeated += () => Clicked?.Invoke();

            button.onClick.AddListener(() =>
            {
                if (longPress == null || !longPress.ConsumeRepeated()) Clicked?.Invoke();
            });
        }

        public Transform ButtonTransform => button.transform;

        public void Show(string itemName, string info, Color tint)
        {
            if (nameLabel.text != itemName) nameLabel.text = itemName;
            if (infoLabel.text != info) infoLabel.text = info;
            icon.color = tint;
        }

        public void SetBackground(Color color) => background.color = color;

        public void SetIcon(Sprite sprite) => icon.sprite = sprite;

        public void CancelHold()
        {
            if (longPress != null) longPress.Cancel();
        }

        /// <summary>Stays clickable when unaffordable: the attempt shakes the coin counter.</summary>
        public void ShowPrice(string price, bool affordable) =>
            SetButton(price, true, affordable ? UIStyle.Affordable : UIStyle.Unaffordable);

        public void ShowMaxed(string label) => SetButton(label, false, UIStyle.Highlight);

        public void HideButton()
        {
            if (button.gameObject.activeSelf) button.gameObject.SetActive(false);
        }

        private void SetButton(string label, bool interactable, Color color)
        {
            if (!button.gameObject.activeSelf) button.gameObject.SetActive(true);
            if (button.interactable != interactable) button.interactable = interactable;
            button.targetGraphic.color = color;
            if (buttonLabel.text != label) buttonLabel.text = label;
        }
    }
}
