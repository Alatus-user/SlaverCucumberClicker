using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>Modal yes/no popup. The root object is the full-screen dimmer.</summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI cancelLabel;

        public bool IsOpen => gameObject.activeSelf;

        public void Show(string text, string confirmText, string cancelText, Action onConfirm)
        {
            message.text = text;
            confirmLabel.text = confirmText;
            cancelLabel.text = cancelText;

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(() =>
            {
                Close();
                onConfirm?.Invoke();
            });
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(Close);

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close() => gameObject.SetActive(false);
    }
}
