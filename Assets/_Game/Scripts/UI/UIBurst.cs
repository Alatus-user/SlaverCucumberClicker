using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>
    /// Small particle burst drawn with pooled UI Images, so it renders correctly on an
    /// overlay canvas without a ParticleSystem.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIBurst : MonoBehaviour
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color[] colors = { new Color(0.45f, 0.8f, 0.3f), new Color(0.7f, 0.92f, 0.45f) };
        [SerializeField] private int count = 7;
        [SerializeField] private float speed = 700;
        [SerializeField] private float gravity = 1800;
        [SerializeField] private float lifetime = 0.45f;
        [SerializeField] private Vector2 sizeRange = new Vector2(18, 34);

        private class Particle
        {
            public Image image;
            public Vector2 position, velocity;
            public float age;
        }

        private readonly List<Particle> active = new List<Particle>();
        private readonly Stack<Particle> pool = new Stack<Particle>();
        private RectTransform rectTransform;
        private Canvas canvas;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        public void PlayAtScreen(Vector2 screenPosition)
        {
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, cam, out Vector2 local);

            for (int i = 0; i < count; i++)
            {
                Particle p = pool.Count > 0 ? pool.Pop() : CreateParticle();
                float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                p.position = local;
                p.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed * Random.Range(0.5f, 1f);
                p.age = 0;
                float size = Random.Range(sizeRange.x, sizeRange.y);
                p.image.rectTransform.sizeDelta = new Vector2(size, size);
                p.image.color = colors[Random.Range(0, colors.Length)];
                p.image.gameObject.SetActive(true);
                active.Add(p);
            }
        }

        private Particle CreateParticle()
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(rectTransform, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return new Particle { image = image };
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Particle p = active[i];
                p.age += dt;
                if (p.age >= lifetime)
                {
                    p.image.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    pool.Push(p);
                    continue;
                }

                p.velocity.y -= gravity * dt;
                p.position += p.velocity * dt;
                p.image.rectTransform.anchoredPosition = p.position;

                float life = 1 - p.age / lifetime;
                p.image.transform.localScale = Vector3.one * life;
            }
        }
    }
}
