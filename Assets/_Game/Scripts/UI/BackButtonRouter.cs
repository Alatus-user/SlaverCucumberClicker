using UnityEngine;
using UnityEngine.InputSystem;

namespace CucumberClicker.UI
{
    /// <summary>Android back button (Escape): close popup, then step the drawer down, then ask to quit.</summary>
    public class BackButtonRouter : MonoBehaviour
    {
        [SerializeField] private ConfirmDialog dialog;
        [SerializeField] private RecipePicker recipePicker;
        [SerializeField] private DrawerController drawer;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) HandleBack();
        }

        public void HandleBack()
        {
            if (dialog.IsOpen)
            {
                dialog.Close();
                return;
            }
            if (recipePicker.IsOpen)
            {
                recipePicker.Close();
                return;
            }
            if (drawer.HandleBack()) return;

            dialog.Show(Strings.QuitQuestion, Strings.Quit, Strings.Cancel, Application.Quit);
        }
    }
}
