using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>Pooled "+N" texts that rise and fade. Place it on a full-screen layer.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class FloatingTextSpawner : MonoBehaviour
    {
        [Tooltip("Inactive child used as the template.")]
        [SerializeField] private TextMeshProUGUI template;
        [SerializeField] private float rise = 160;
        [SerializeField] private float duration = 0.8f;

        private class Item
        {
            public TextMeshProUGUI text;
            public Vector2 start;
            public float age;
            public Color color;
        }

        private readonly List<Item> active = new List<Item>();
        private readonly Stack<Item> pool = new Stack<Item>();
        private RectTransform rectTransform;
        private Canvas canvas;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>().rootCanvas;
            template.gameObject.SetActive(false);
        }

        public void SpawnAtScreen(Vector2 screenPosition, string text, Color color)
        {
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, cam, out Vector2 local);
            Spawn(local, text, color);
        }

        public void SpawnAtWorld(Vector3 worldPosition, string text, Color color) =>
            Spawn(rectTransform.InverseTransformPoint(worldPosition), text, color);

        private void Spawn(Vector2 localPosition, string text, Color color)
        {
            Item item = pool.Count > 0 ? pool.Pop() : new Item { text = Instantiate(template, rectTransform) };
            item.start = localPosition;
            item.age = 0;
            item.color = color;
            item.text.text = text;
            item.text.gameObject.SetActive(true);
            item.text.transform.SetAsLastSibling();
            active.Add(item);
            Animate(item);
        }

        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Item item = active[i];
                item.age += Time.unscaledDeltaTime;
                if (item.age >= duration)
                {
                    item.text.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    pool.Push(item);
                    continue;
                }
                Animate(item);
            }
        }

        private void Animate(Item item)
        {
            float p = item.age / duration;
            float eased = 1 - (1 - p) * (1 - p);
            item.text.rectTransform.anchoredPosition = item.start + Vector2.up * (rise * eased);
            item.text.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.1f, Mathf.Min(1, p * 4));
            Color c = item.color;
            c.a = 1 - p * p;
            item.text.color = c;
        }
    }
}
