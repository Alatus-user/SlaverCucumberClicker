using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    public class GrabHandle : MonoBehaviour
    {
        [SerializeField] private Image glow;
        [SerializeField] private float glowDuration = 1.2f;

        private float glowTime = float.MaxValue;

        public void Glow() => glowTime = 0;

        private void Update()
        {
            if (glow == null) return;
            glowTime += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(glowTime / glowDuration);
            // Two soft pulses, then fade out.
            float alpha = p >= 1 ? 0 : Mathf.Abs(Mathf.Sin(p * Mathf.PI * 2)) * (1 - p);
            Color c = glow.color;
            c.a = alpha;
            glow.color = c;
        }
    }
}
