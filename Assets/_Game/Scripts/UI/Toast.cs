using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>Short message that slides in under the top bar. Messages queue up.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class Toast : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float fadeIn = 0.2f, hold = 1.8f, fadeOut = 0.3f;
        [SerializeField] private float slideDistance = 40;

        private readonly Queue<string> queue = new Queue<string>();
        private CanvasGroup group;
        private RectTransform rectTransform;
        private Vector2 home;
        private float time;
        private bool showing;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rectTransform = (RectTransform)transform;
            home = rectTransform.anchoredPosition;
            group.alpha = 0;
            group.blocksRaycasts = false;
        }

        public void Show(string message)
        {
            if (showing && label.text == message) return;
            queue.Enqueue(message);
        }

        private void Update()
        {
            if (!showing)
            {
                if (queue.Count == 0) return;
                label.text = queue.Dequeue();
                showing = true;
                time = 0;
            }

            time += Time.unscaledDeltaTime;
            float alpha;
            if (time < fadeIn) alpha = time / fadeIn;
            else if (time < fadeIn + hold) alpha = 1;
            else alpha = 1 - (time - fadeIn - hold) / fadeOut;

            alpha = Mathf.Clamp01(alpha);
            group.alpha = alpha;
            rectTransform.anchoredPosition = home + Vector2.up * (slideDistance * (1 - alpha));

            if (time >= fadeIn + hold + fadeOut) showing = false;
        }
    }
}
