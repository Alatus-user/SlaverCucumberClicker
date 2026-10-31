using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CucumberClicker.UI
{
    /// <summary>Holding this button repeats its action. Starting to scroll cancels the hold.</summary>
    public class LongPressRepeat : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private float holdDelay = 0.4f;
        [SerializeField] private float repeatInterval = 0.1f;

        public event Action Repeated;

        private PointerEventData pointer;
        private float nextRepeat;
        private bool repeated;

        public void OnPointerDown(PointerEventData eventData)
        {
            pointer = eventData;
            nextRepeat = Time.unscaledTime + holdDelay;
            repeated = false;
        }

        public void OnPointerUp(PointerEventData eventData) => Cancel();
        public void OnPointerExit(PointerEventData eventData) => Cancel();
        private void OnDisable() => Cancel();

        public void Cancel() => pointer = null;

        /// <summary>True once after a hold, so the click on release doesn't buy one extra.</summary>
        public bool ConsumeRepeated()
        {
            bool result = repeated;
            repeated = false;
            return result;
        }

        private void Update()
        {
            if (pointer == null) return;
            if (pointer.dragging)
            {
                Cancel();
                return;
            }
            if (Time.unscaledTime < nextRepeat) return;

            repeated = true;
            nextRepeat += repeatInterval;
            Repeated?.Invoke();
        }
    }
}
