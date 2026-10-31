#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.DebugTools
{
    /// <summary>
    /// Cheat button and panel. Built at runtime rather than in the scene, so a release build
    /// has no component left to go missing. Its text isn't player-facing, so it stays here.
    /// </summary>
    public class DebugPanel : MonoBehaviour
    {
        private const float RowHeight = 110;
        private const float FontSize = 36;
        private const string Title = "Cheats";

        private static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
        private static readonly Color Ink = new Color(0.17f, 0.25f, 0.12f);
        private static readonly Color Cheat = new Color(0.85f, 0.2f, 0.2f);

        private readonly List<Action> refreshers = new List<Action>();
        private GameObject panel;
        private TextMeshProUGUI titleLabel;
        private Cheats cheats;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInGame()
        {
            if (GameManager.Instance != null) Create();
        }

        public static DebugPanel Create()
        {
            var go = new GameObject("DebugCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var debugPanel = go.AddComponent<DebugPanel>();
            debugPanel.Build();
            return debugPanel;
        }

        public void Open()
        {
            foreach (Action refresh in refreshers) refresh();
            titleLabel.text = Title;
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

        private void Build()
        {
            GameManager game = GameManager.Instance;
            cheats = new Cheats(game.State, game.Upgrades);

            RectTransform root = Stretch(Node("SafeRoot", transform));
            root.gameObject.AddComponent<UI.SafeArea>();

            // Between the top bar and the drawer tabs, so it covers nothing the player taps.
            RectTransform open = MakeButton(root, "CheatButton", "CHEAT", Cheat, Open);
            open.anchorMin = open.anchorMax = open.pivot = new Vector2(0, 1);
            open.anchoredPosition = new Vector2(16, -170);
            open.sizeDelta = new Vector2(170, 84);

            RectTransform dim = Stretch(Node("Panel", root));
            Image(dim, new Color(0, 0, 0, 0.6f));
            panel = dim.gameObject;

            RectTransform card = Stretch(Node("Card", dim), new Vector2(30, 30), new Vector2(-30, -30));
            Image(card, Cream);

            titleLabel = Label(card, Title, 50, TextAlignmentOptions.Center);
            titleLabel.fontStyle = FontStyles.Bold;
            Stretch(titleLabel.rectTransform, new Vector2(20, 0), new Vector2(-20, 0));
            Dock(titleLabel.rectTransform, 1, -110, -10);

            BuildRows(BuildScrollContent(card), game);

            RectTransform finish = MakeButton(card, "FinishButton", "Finish All Fermentation", UI.UIStyle.Affordable, FinishJars);
            Stretch(finish, new Vector2(30, 0), new Vector2(-30, 0));
            Dock(finish, 0, 170, 300);

            RectTransform close = MakeButton(card, "CloseButton", "Close", UI.UIStyle.Unaffordable, Close);
            Stretch(close, new Vector2(30, 0), new Vector2(-30, 0));
            Dock(close, 0, 20, 150);

            panel.SetActive(false);
        }

        private void FinishJars()
        {
            int finished = cheats.FinishAllJars();
            titleLabel.text = $"Finished {finished} jar(s)";
        }

        private static RectTransform BuildScrollContent(RectTransform card)
        {
            RectTransform viewport = Stretch(Node("Scroll", card), new Vector2(20, 320), new Vector2(-20, -120));
            Image(viewport, Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = Node("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;

            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.spacing = 10;
            list.childControlWidth = list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            return content;
        }

        private void BuildRows(RectTransform content, GameManager game)
        {
            LifetimeStats stats = game.State.stats;
            GameState state = game.State;

            AddNumber(content, "Coins", () => state.coins, v => state.coins = v);
            AddNumber(content, "Cucumbers", () => state.cucumbers, v => state.cucumbers = v);
            AddNumber(content, "Lifetime Coins", () => stats.lifetimeCoins, v => stats.lifetimeCoins = v);
            AddLevel(content, "Market Level", () => cheats.MarketLevel, v => cheats.MarketLevel = v);
            AddNumber(content, "Total Taps", () => stats.totalTaps, v => stats.totalTaps = (long)v);
            AddNumber(content, "Pickles Sold", () => stats.picklesSold, v => stats.picklesSold = v);

            foreach (ShopItem item in game.Upgrades.Items)
            {
                ShopItem current = item;
                AddLevel(content, $"{ItemName(item)} (max {item.MaxLevel})", () => current.Level,
                    v => cheats.SetItemLevel(current, v));
            }
        }

        private static string ItemName(ShopItem item)
        {
            switch (item.Kind)
            {
                case ShopItemKind.RecipeUnlock: return $"Unlock {item.DisplayName}";
                case ShopItemKind.Upgrade: return item.DisplayName;
                default: return $"{item.Recipe.displayName}: {item.DisplayName}";
            }
        }

        private void AddNumber(RectTransform content, string label, Func<double> get, Action<double> set) =>
            AddRow(content, label, TMP_InputField.ContentType.DecimalNumber, get, v => set(Math.Max(0, v)));

        private void AddLevel(RectTransform content, string label, Func<int> get, Action<int> set) =>
            AddRow(content, label, TMP_InputField.ContentType.IntegerNumber, () => get(),
                v => set((int)Math.Max(int.MinValue, Math.Min(int.MaxValue, v))));

        private void AddRow(RectTransform content, string label, TMP_InputField.ContentType type,
            Func<double> get, Action<double> set)
        {
            RectTransform row = Node(label, content);
            var height = row.gameObject.AddComponent<LayoutElement>();
            height.minHeight = height.preferredHeight = RowHeight;

            TextMeshProUGUI name = Label(row, label, FontSize - 4, TextAlignmentOptions.Left);
            name.rectTransform.anchorMin = Vector2.zero;
            name.rectTransform.anchorMax = new Vector2(0.62f, 1);
            name.rectTransform.offsetMin = name.rectTransform.offsetMax = Vector2.zero;

            GameObject field = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            field.transform.SetParent(row, false);
            var fieldRect = (RectTransform)field.transform;
            fieldRect.anchorMin = new Vector2(0.64f, 0.1f);
            fieldRect.anchorMax = new Vector2(1, 0.9f);
            fieldRect.offsetMin = fieldRect.offsetMax = Vector2.zero;

            var input = field.GetComponent<TMP_InputField>();
            input.contentType = type;
            input.pointSize = FontSize;
            input.textComponent.alignment = TextAlignmentOptions.MidlineRight;

            Action refresh = () => input.SetTextWithoutNotify(Show(get()));
            refreshers.Add(refresh);
            input.onEndEdit.AddListener(text =>
            {
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                    && !double.IsInfinity(value))
                    set(value);
                refresh();
            });
        }

        private static string Show(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static RectTransform Node(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform Stretch(RectTransform rect) => Stretch(rect, Vector2.zero, Vector2.zero);

        private static RectTransform Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        /// <summary>Pins a stretched rect to the top (edge 1) or bottom (edge 0) of its parent.</summary>
        private static void Dock(RectTransform rect, float edge, float bottom, float top)
        {
            rect.anchorMin = new Vector2(0, edge);
            rect.anchorMax = new Vector2(1, edge);
            rect.offsetMin = new Vector2(rect.offsetMin.x, bottom);
            rect.offsetMax = new Vector2(rect.offsetMax.x, top);
        }

        private static Image Image(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI Label(RectTransform parent, string text, float size, TextAlignmentOptions alignment)
        {
            RectTransform rect = Stretch(Node("Label", parent));
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Ink;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform MakeButton(RectTransform parent, string name, string text, Color color, Action onClick)
        {
            RectTransform rect = Node(name, parent);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = Image(rect, color);
            button.onClick.AddListener(() => onClick());

            TextMeshProUGUI label = Label(rect, text, FontSize, TextAlignmentOptions.Center);
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            return rect;
        }
    }
}
#endif
