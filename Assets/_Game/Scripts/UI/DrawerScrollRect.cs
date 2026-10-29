using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>
    /// ScrollRect for drawer panels. When the list is already at the top and the player
    /// drags down, the drag is handed to the drawer so it closes (nested-scroll handoff).
    /// </summary>
    public class DrawerScrollRect : ScrollRect
    {
        [SerializeField] private DrawerController drawer;

        private bool routedToDrawer;

        public void SetDrawer(DrawerController value) => drawer = value;

        private bool IsAtTop =>
            content == null || viewport == null
            || content.rect.height <= viewport.rect.height + 0.5f
            || verticalNormalizedPosition >= 0.999f;

        public override void OnBeginDrag(PointerEventData eventData)
        {
            Vector2 move = eventData.position - eventData.pressPosition;
            bool draggingDown = move.y < 0 && Mathf.Abs(move.y) >= Mathf.Abs(move.x);

            routedToDrawer = drawer != null && draggingDown && IsAtTop;
            if (routedToDrawer) drawer.OnBeginDrag(eventData);
            else base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (routedToDrawer) drawer.OnDrag(eventData);
            else base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (routedToDrawer)
            {
                routedToDrawer = false;
                drawer.OnEndDrag(eventData);
            }
            else base.OnEndDrag(eventData);
        }
    }
}
