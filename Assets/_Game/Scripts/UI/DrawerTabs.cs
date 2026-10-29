using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    public enum DrawerTab { Pickle, Market, Upgrades, Workers }

    /// <summary>Tab row in the drawer header. Only the selected tab's panel is active.</summary>
    public class DrawerTabs : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public DrawerTab id;
            public Button button;
            public Image background;
            public TextMeshProUGUI label;
            public Image icon;
            public GameObject panel;
            public TabBadge badge;
        }

        [SerializeField] private DrawerController drawer;
        [SerializeField] private List<Entry> tabs = new List<Entry>();
        [SerializeField] private DrawerTab initialTab = DrawerTab.Pickle;
        [SerializeField] private Color selectedColor = new Color(0.36f, 0.66f, 0.27f);
        [SerializeField] private Color normalColor = new Color(0, 0, 0, 0);
        [SerializeField] private Color selectedLabelColor = Color.white;
        [SerializeField] private Color normalLabelColor = new Color(0.25f, 0.3f, 0.22f);

        public DrawerTab Selected { get; private set; }
        public event Action<DrawerTab> OnTabSelected;

        private void Start()
        {
            foreach (Entry entry in tabs)
            {
                DrawerTab id = entry.id;
                entry.button.onClick.AddListener(() => OnTabClicked(id));
            }
            Select(initialTab);
        }

        public void OnTabClicked(DrawerTab id)
        {
            if (drawer.State == DrawerState.Collapsed)
            {
                Select(id);
                drawer.SnapTo(DrawerState.Half);
            }
            else if (id == Selected)
            {
                drawer.SnapTo(DrawerState.Collapsed);
            }
            else
            {
                Select(id);
            }
        }

        public void Select(DrawerTab id)
        {
            Selected = id;
            foreach (Entry entry in tabs)
            {
                bool on = entry.id == id;
                if (entry.panel != null) entry.panel.SetActive(on);
                entry.background.color = on ? selectedColor : normalColor;
                entry.label.color = on ? selectedLabelColor : normalLabelColor;
                if (entry.icon != null) entry.icon.color = entry.label.color;
                entry.label.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
                if (on && entry.badge != null) entry.badge.SetDot(false);
            }
            OnTabSelected?.Invoke(id);
        }

        public TabBadge GetBadge(DrawerTab id) => Find(id)?.badge;

        public void SetTabVisible(DrawerTab id, bool visible)
        {
            Entry entry = Find(id);
            if (entry != null) entry.button.gameObject.SetActive(visible);
        }

        private Entry Find(DrawerTab id) => tabs.Find(e => e.id == id);
    }
}
