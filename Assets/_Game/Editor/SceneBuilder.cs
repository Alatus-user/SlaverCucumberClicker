using System.Linq;
using CucumberClicker.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CucumberClicker.EditorTools
{
    /// <summary>
    /// Builds Assets/_Game/Scenes/Main.unity from code, following UI_LAYOUT.md.
    /// Menu: Cucumber Clicker ▸ Build Main Scene. Re-running it rebuilds the scene from scratch.
    /// </summary>
    public static class SceneBuilder
    {
        private const string GameFolder = "Assets/_Game";
        private const string ArtFolder = GameFolder + "/Art/Generated";
        private const string ScenePath = GameFolder + "/Scenes/Main.unity";
        private const string ConfigPath = GameFolder + "/Data/GameConfig.asset";

        private const float TopBarHeight = 160;
        private const float DrawerHeaderHeight = 220;
        private const float TabRowHeight = 140;
        private const float JarCellHeight = 380;
        private const float ItemRowHeight = 150;

        private static readonly Color TextDark = new Color(0.18f, 0.27f, 0.13f);
        private static readonly Color TextMuted = new Color(0.45f, 0.5f, 0.4f);
        private static readonly Color DrawerColor = new Color(1f, 0.97f, 0.9f);
        private static readonly Color HandleColor = new Color(0.78f, 0.75f, 0.66f);
        private static readonly Color BadgeGreen = new Color(0.2f, 0.68f, 0.25f);
        private static readonly Color BadgeOrange = new Color(0.95f, 0.55f, 0.1f);
        private static readonly Color DotRed = new Color(0.9f, 0.22f, 0.2f);

        private static PlaceholderArt.Set art;

        [MenuItem("Cucumber Clicker/Build Main Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            EditorUtility.DisplayDialog("Cucumber Clicker", $"Scene built at {ScenePath}.\nPress Play to try it.", "OK");
        }

        /// <summary>Entry point for -batchmode -executeMethod.</summary>
        public static void Build()
        {
            art = PlaceholderArt.Generate(ArtFolder);
            ConfigurePlayer();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load after NewScene: it unloads unused assets, which would destroy the config.
            GameConfig config = LoadOrCreateConfig();

            CreateCamera();
            CreateEventSystem();
            var manager = new GameObject("GameManager").AddComponent<GameManager>();
            Set(manager, "config", config);

            BuildCanvas();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Cucumber Clicker: built {ScenePath}");
        }

        // ---------------------------------------------------------------- setup

        private static GameConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            GameDataBuilder.Populate(config);
            return config;
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.productName = "Cucumber Clicker";
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void CreateCamera()
        {
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.91f, 0.62f);
            camera.transform.position = new Vector3(0, 0, -10);
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<EventSystem>().pixelDragThreshold = 12;
        }

        // ---------------------------------------------------------------- canvas

        private static void BuildCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasGo.transform;

            // Background sits outside the safe area so it also fills the notch.
            Image background = Img(Stretch(Node("Background", root)), art.gradient, Color.white);
            background.raycastTarget = false;

            RectTransform safeRoot = Stretch(Node("SafeAreaRoot", root));
            safeRoot.gameObject.AddComponent<SafeArea>();

            // Order matters (back to front): cucumber area, dim, drawer, top bar, popups.
            RectTransform cucumberArea = Stretch(Node("CucumberArea", safeRoot));
            RectTransform dim = Stretch(Node("DimOverlay", safeRoot));
            RectTransform drawerRect = Node("Drawer", safeRoot);
            RectTransform topBar = Node("TopBar", safeRoot);
            RectTransform popupLayer = Stretch(Node("PopupLayer", safeRoot));

            DrawerController drawer = drawerRect.gameObject.AddComponent<DrawerController>();

            // Popups first: other parts reference them. The picker goes first so effects draw above it.
            RecipePicker picker = BuildRecipePicker(popupLayer);
            FloatingTextSpawner effects = BuildFloatingLayer("EffectsLayer", popupLayer, 64, UIStyle.CoinText);
            Toast toast = BuildToast(popupLayer);
            ConfirmDialog dialog = BuildConfirmDialog(popupLayer);

            BuildCucumberArea(cucumberArea, drawer);
            BuildDimOverlay(dim, drawer);
            BuildDrawer(drawerRect, drawer, safeRoot, effects, picker, toast);
            BuildTopBar(topBar, toast);

            var back = canvasGo.AddComponent<BackButtonRouter>();
            Set(back, "dialog", dialog);
            Set(back, "recipePicker", picker);
            Set(back, "drawer", drawer);
        }

        // ---------------------------------------------------------------- zone B

        private static void BuildCucumberArea(RectTransform area, DrawerController drawer)
        {
            var layout = area.gameObject.AddComponent<CucumberAreaLayout>();

            RectTransform cucumberRect = Centered(Node("BigCucumber", area), new Vector2(600, 600));
            Image cucumberImage = Img(cucumberRect, art.cucumber, Color.white);
            cucumberImage.preserveAspect = true;
            cucumberImage.raycastTarget = true;
            var cucumber = cucumberRect.gameObject.AddComponent<CucumberButton>();

            TextMeshProUGUI tapLabel = Text(area, "TapPowerLabel", "+1 per tap", 46, TextDark, FontStyles.Bold);
            Centered(tapLabel.rectTransform, new Vector2(800, 90));

            RectTransform burstRect = Stretch(Node("BurstLayer", area));
            var burst = burstRect.gameObject.AddComponent<UIBurst>();
            Set(burst, "sprite", art.circle);

            FloatingTextSpawner floating = BuildFloatingLayer("FloatingTextLayer", area, 72, new Color(0.2f, 0.55f, 0.15f));

            Set(cucumber, "floatingText", floating);
            Set(cucumber, "burst", burst);
            Set(cucumber, "tapPowerLabel", tapLabel);

            Set(layout, "drawer", drawer);
            Set(layout, "cucumber", cucumber);
            Set(layout, "tapPowerLabel", tapLabel.rectTransform);
            SetFloat(layout, "topBarHeight", TopBarHeight);
        }

        private static void BuildDimOverlay(RectTransform dim, DrawerController drawer)
        {
            Img(dim, null, new Color(0, 0, 0, 0));
            var overlay = dim.gameObject.AddComponent<DimOverlay>();
            Set(overlay, "drawer", drawer);
        }

        // ---------------------------------------------------------------- zone C

        private static void BuildDrawer(RectTransform drawerRect, DrawerController drawer, RectTransform safeRoot,
            FloatingTextSpawner effects, RecipePicker picker, Toast toast)
        {
            drawerRect.anchorMin = new Vector2(0, 0);
            drawerRect.anchorMax = new Vector2(1, 0);
            drawerRect.pivot = new Vector2(0.5f, 0);
            drawerRect.sizeDelta = new Vector2(0, 1400);
            drawerRect.anchoredPosition = Vector2.zero;

            // Soft shadow above, then a solid background that runs past the bottom edge
            // so its rounded bottom corners are never visible.
            RectTransform shadow = Stretch(Node("Shadow", drawerRect), new Vector2(-24, -200), new Vector2(24, 36));
            Sliced(Img(shadow, art.shadow, new Color(0, 0, 0, 0.22f)), 1);

            RectTransform bg = Stretch(Node("Background", drawerRect), new Vector2(0, -200), Vector2.zero);
            Image bgImage = Sliced(Img(bg, art.rounded, DrawerColor), 1);
            bgImage.raycastTarget = true;

            // Header: the drag area (handle + tabs).
            RectTransform header = TopStretch(Node("Header", drawerRect), DrawerHeaderHeight);
            Img(header, null, new Color(0, 0, 0, 0)).raycastTarget = true;

            RectTransform handleRect = Node("GrabHandle", header);
            handleRect.anchorMin = handleRect.anchorMax = new Vector2(0.5f, 1);
            handleRect.pivot = new Vector2(0.5f, 1);
            handleRect.anchoredPosition = new Vector2(0, -30);
            handleRect.sizeDelta = new Vector2(120, 12);
            RectTransform glowRect = Centered(Node("Glow", handleRect), new Vector2(220, 44));
            Image glow = Sliced(Img(glowRect, art.rounded, new Color(1f, 0.85f, 0.2f, 0f)), 2.2f);
            RectTransform pill = Stretch(Node("Pill", handleRect));
            Sliced(Img(pill, art.rounded, HandleColor), 8);
            var grab = handleRect.gameObject.AddComponent<GrabHandle>();
            Set(grab, "glow", glow);

            RectTransform tabRow = Node("TabRow", header);
            tabRow.anchorMin = new Vector2(0, 0);
            tabRow.anchorMax = new Vector2(1, 0);
            tabRow.pivot = new Vector2(0.5f, 0);
            tabRow.sizeDelta = new Vector2(0, TabRowHeight);
            tabRow.anchoredPosition = Vector2.zero;
            var row = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(24, 24, 4, 12);
            row.spacing = 12;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            var tabs = tabRow.gameObject.AddComponent<DrawerTabs>();

            // Content: one scroll panel per tab.
            RectTransform content = Stretch(Node("Content", drawerRect), Vector2.zero, new Vector2(0, -DrawerHeaderHeight));

            GameObject picklePanel = BuildPicklePanel(content, drawer, picker, effects);
            GameObject marketPanel = BuildMarketPanel(content, drawer, effects);
            GameObject upgradesPanel = BuildUpgradesPanel(content, drawer);
            GameObject workersPanel = BuildPlaceholderPanel("WorkersPanel", content, drawer);

            var entries = new[]
            {
                BuildTab(tabRow, DrawerTab.Pickle, Strings.TabPickle, art.jar, picklePanel, BadgeGreen, true),
                BuildTab(tabRow, DrawerTab.Market, Strings.TabMarket, art.market, marketPanel, BadgeOrange, false),
                BuildTab(tabRow, DrawerTab.Upgrades, Strings.TabUpgrades, art.arrow, upgradesPanel, null, true),
                BuildTab(tabRow, DrawerTab.Workers, Strings.TabWorkers, art.worker, workersPanel, null, true),
            };
            // Workers stays hidden until producers are unlocked (Phase 5).
            entries[3].button.gameObject.SetActive(false);

            var so = new SerializedObject(tabs);
            so.FindProperty("drawer").objectReferenceValue = drawer;
            SerializedProperty list = so.FindProperty("tabs");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)entries[i].id;
                e.FindPropertyRelative("button").objectReferenceValue = entries[i].button;
                e.FindPropertyRelative("background").objectReferenceValue = entries[i].background;
                e.FindPropertyRelative("label").objectReferenceValue = entries[i].label;
                e.FindPropertyRelative("icon").objectReferenceValue = entries[i].icon;
                e.FindPropertyRelative("panel").objectReferenceValue = entries[i].panel;
                e.FindPropertyRelative("badge").objectReferenceValue = entries[i].badge;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            var badges = tabRow.gameObject.AddComponent<DrawerBadges>();
            Set(badges, "tabs", tabs);
            Set(badges, "drawer", drawer);
            Set(badges, "toast", toast);

            Set(drawer, "root", safeRoot);
            Set(drawer, "content", content);
            Set(drawer, "grabHandle", grab);
            SetFloat(drawer, "topBarHeight", TopBarHeight);
            SetFloat(drawer, "collapsedHeight", DrawerHeaderHeight);
        }

        private static DrawerTabs.Entry BuildTab(RectTransform row, DrawerTab id, string label, Sprite icon,
            GameObject panel, Color? countColor, bool hasDot)
        {
            RectTransform tab = Node("Tab_" + id, row);
            Image bg = Sliced(Img(tab, art.rounded, Color.clear), 1.6f);
            bg.raycastTarget = true;
            var button = tab.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bg;

            RectTransform iconRect = Node("Icon", tab);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1);
            iconRect.pivot = new Vector2(0.5f, 1);
            iconRect.anchoredPosition = new Vector2(0, -10);
            iconRect.sizeDelta = new Vector2(62, 62);
            Image iconImage = Img(iconRect, icon, TextDark);
            iconImage.preserveAspect = true;

            TextMeshProUGUI text = Text(tab, "Label", label, 30, TextDark);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.pivot = new Vector2(0.5f, 0);
            textRect.anchoredPosition = new Vector2(0, 8);
            textRect.sizeDelta = new Vector2(0, 44);
            text.enableAutoSizing = true;
            text.fontSizeMin = 20;
            text.fontSizeMax = 30;

            var badge = tab.gameObject.AddComponent<TabBadge>();
            if (countColor.HasValue)
            {
                RectTransform count = Node("CountBadge", tab);
                count.anchorMin = count.anchorMax = new Vector2(1, 1);
                count.anchoredPosition = new Vector2(-22, -18);
                count.sizeDelta = new Vector2(52, 52);
                Img(count, art.circle, countColor.Value);
                TextMeshProUGUI countText = Text(count, "Count", "0", 28, Color.white, FontStyles.Bold);
                Stretch(countText.rectTransform);
                count.gameObject.SetActive(false);
                Set(badge, "countBadge", count);
                Set(badge, "countLabel", countText);
            }
            if (hasDot)
            {
                RectTransform dot = Node("RedDot", tab);
                // Tabs with a count badge keep the dot in the opposite corner so neither hides the other.
                dot.anchorMin = dot.anchorMax = new Vector2(countColor.HasValue ? 0 : 1, 1);
                dot.anchoredPosition = new Vector2(countColor.HasValue ? 26 : -26, -22);
                dot.sizeDelta = new Vector2(28, 28);
                Img(dot, art.circle, DotRed);
                dot.gameObject.SetActive(false);
                Set(badge, "redDot", dot.gameObject);
            }

            return new DrawerTabs.Entry
            {
                id = id, button = button, background = bg, label = text, icon = iconImage, panel = panel, badge = badge,
            };
        }

        /// <summary>Scroll panel: ScrollRect → Viewport (mask) → Content (vertical layout).</summary>
        private static RectTransform BuildScrollPanel(string name, RectTransform parent, DrawerController drawer,
            out GameObject panel)
        {
            RectTransform panelRect = Stretch(Node(name, parent));
            panel = panelRect.gameObject;
            var scroll = panel.AddComponent<DrawerScrollRect>();

            RectTransform viewport = Stretch(Node("Viewport", panelRect));
            viewport.gameObject.AddComponent<RectMask2D>();
            Img(viewport, null, new Color(0, 0, 0, 0)).raycastTarget = true;

            RectTransform content = TopStretch(Node("Content", viewport), 0);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 16, 48);
            layout.spacing = 24;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40;
            Set(scroll, "drawer", drawer);
            return content;
        }

        private static GameObject BuildPlaceholderPanel(string name, RectTransform parent, DrawerController drawer)
        {
            RectTransform content = BuildScrollPanel(name, parent, drawer, out GameObject panel);
            TextMeshProUGUI text = Text(content, "ComingSoon", Strings.ComingSoon, 40, TextMuted, FontStyles.Italic);
            Height(text.gameObject, 200);
            panel.SetActive(false);
            return panel;
        }

        /// <summary>Item row with a button at its right edge.</summary>
        private static ItemRowView BuildButtonRow(RectTransform parent, string name, string label, out Button button)
        {
            ItemRowView row = BuildItemRow(parent, name, 270, out _);
            button = MakeButton(row.transform, "Button", label, 36, UIStyle.Affordable, out TextMeshProUGUI buttonLabel);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-136, 0);
            rect.sizeDelta = new Vector2(240, 120);
            Set(row, "button", button);
            Set(row, "buttonLabel", buttonLabel);
            return row;
        }

        private static GameObject BuildUpgradesPanel(RectTransform parent, DrawerController drawer)
        {
            RectTransform content = BuildScrollPanel("UpgradesPanel", parent, drawer, out GameObject panel);

            TextMeshProUGUI header = Text(content, "SectionHeader", "", 44, TextDark, FontStyles.Bold, TextAlignmentOptions.Left);
            Height(header.gameObject, 70);
            header.gameObject.SetActive(false);

            ItemRowView row = BuildButtonRow(content, "ShopRow", "", out Button buy);
            buy.gameObject.AddComponent<LongPressRepeat>();
            row.gameObject.SetActive(false);

            var upgrades = panel.AddComponent<UpgradesPanel>();
            Set(upgrades, "rowTemplate", row);
            Set(upgrades, "headerTemplate", header);
            Set(upgrades, "upgradeIcon", art.arrow);
            Set(upgrades, "recipeIcon", art.jar);

            panel.SetActive(false);
            return panel;
        }

        private static GameObject BuildPicklePanel(RectTransform parent, DrawerController drawer, RecipePicker picker,
            FloatingTextSpawner effects)
        {
            RectTransform content = BuildScrollPanel("PicklePanel", parent, drawer, out GameObject panel);

            Button collectAll = MakeButton(content, "CollectAll", Strings.CollectAll, 46, UIStyle.Highlight, out _);
            Height(collectAll.gameObject, 130);
            collectAll.gameObject.SetActive(false);

            RectTransform gridRect = Node("JarGrid", content);
            var grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.spacing = new Vector2(24, 24);
            grid.cellSize = new Vector2(488, JarCellHeight); // width is refitted at runtime

            // A grid's minimum width follows its cells, which would stop it ever getting narrower.
            var gridSize = gridRect.gameObject.AddComponent<LayoutElement>();
            gridSize.minWidth = 0;
            gridSize.preferredWidth = 0;
            gridSize.flexibleWidth = 1;

            var pickle = panel.AddComponent<PicklePanel>();
            Set(pickle, "collectAllButton", collectAll);
            Set(pickle, "grid", grid);
            Set(pickle, "jarTemplate", BuildJarTemplate(gridRect));
            Set(pickle, "recipePicker", picker);
            Set(pickle, "effects", effects);

            panel.SetActive(false);
            return panel;
        }

        private static JarView BuildJarTemplate(RectTransform parent)
        {
            RectTransform card = Node("Jar", parent);

            RectTransform glowRect = Stretch(Node("Glow", card), new Vector2(-10, -10), new Vector2(10, 10));
            Image glow = Sliced(Img(glowRect, art.rounded, new Color(1f, 0.85f, 0.2f, 0f)), 1.2f);

            RectTransform bgRect = Stretch(Node("Background", card));
            Image bg = Sliced(Img(bgRect, art.rounded, Color.white), 1.4f);
            bg.raycastTarget = true;
            var button = bgRect.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;

            RectTransform iconRect = Node("JarIcon", card);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1);
            iconRect.pivot = new Vector2(0.5f, 1);
            iconRect.anchoredPosition = new Vector2(0, -24);
            iconRect.sizeDelta = new Vector2(170, 170);
            Image icon = Img(iconRect, art.jar, Color.white);
            icon.preserveAspect = true;

            TextMeshProUGUI title = Text(card, "Title", Strings.JarEmpty, 42, TextDark, FontStyles.Bold);
            BottomStretch(title.rectTransform, 104, 56, 16);
            TextMeshProUGUI status = Text(card, "Status", Strings.JarTapToFill, 32, TextMuted);
            BottomStretch(status.rectTransform, 58, 46, 16);

            RectTransform track = BottomStretch(Node("ProgressBar", card), 26, 22, 40);
            Sliced(Img(track, art.rounded, new Color(0.9f, 0.88f, 0.8f)), 8);
            RectTransform fill = Stretch(Node("Fill", track));
            fill.anchorMax = new Vector2(0, 1);
            Image fillImage = Sliced(Img(fill, art.rounded, Color.white), 8);
            track.gameObject.SetActive(false);

            var view = card.gameObject.AddComponent<JarView>();
            Set(view, "button", button);
            Set(view, "glow", glow);
            Set(view, "jarIcon", icon);
            Set(view, "title", title);
            Set(view, "status", status);
            Set(view, "progressBar", track.gameObject);
            Set(view, "progressFill", fill);
            Set(view, "progressFillImage", fillImage);

            card.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Row with a jar, name and info. Text stops short of the right edge by <paramref name="rightInset"/>.</summary>
        private static ItemRowView BuildItemRow(RectTransform parent, string name, float rightInset, out Image background)
        {
            RectTransform row = Node(name, parent);
            Height(row.gameObject, ItemRowHeight);
            background = Sliced(Img(row, art.rounded, Color.white), 1.4f);

            RectTransform iconRect = Node("Icon", row);
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(24, 0);
            iconRect.sizeDelta = new Vector2(100, 100);
            Image icon = Img(iconRect, art.jar, Color.white);
            icon.preserveAspect = true;

            TextMeshProUGUI nameText = Text(row, "Name", "", 40, TextDark, FontStyles.Bold, TextAlignmentOptions.Left);
            Stretch(nameText.rectTransform, new Vector2(148, 75), new Vector2(-rightInset, -14));
            TextMeshProUGUI infoText = Text(row, "Info", "", 30, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Stretch(infoText.rectTransform, new Vector2(148, 18), new Vector2(-rightInset, -75));
            infoText.enableAutoSizing = true;
            infoText.fontSizeMin = 20;
            infoText.fontSizeMax = 30;

            var view = row.gameObject.AddComponent<ItemRowView>();
            Set(view, "background", background);
            Set(view, "icon", icon);
            Set(view, "nameLabel", nameText);
            Set(view, "infoLabel", infoText);
            return view;
        }

        private static GameObject BuildMarketPanel(RectTransform parent, DrawerController drawer, FloatingTextSpawner effects)
        {
            RectTransform content = BuildScrollPanel("MarketPanel", parent, drawer, out GameObject panel);

            // Big "Sell Everything" button with its value underneath the label.
            Button sellEverything = MakeButton(content, "SellEverything", Strings.SellEverything, 50, UIStyle.Highlight, out TextMeshProUGUI sellLabel);
            Height(sellEverything.gameObject, 170);
            RectTransform labelRect = sellLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0, 0.45f);
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI sellValue = Text(sellEverything.transform, "Value", Strings.NothingToSell, 34, Color.white);
            RectTransform valueRect = sellValue.rectTransform;
            valueRect.anchorMin = new Vector2(0, 0.08f);
            valueRect.anchorMax = new Vector2(1, 0.48f);
            valueRect.offsetMin = valueRect.offsetMax = Vector2.zero;

            Height(Text(content, "RawHeader", Strings.RawCucumbersHeader, 44, TextDark, FontStyles.Bold, TextAlignmentOptions.Left).gameObject, 70);
            TextMeshProUGUI rawInfo = Text(content, "RawInfo", "", 34, TextMuted, FontStyles.Normal, TextAlignmentOptions.Left);
            Height(rawInfo.gameObject, 50);

            Button sellRaw = MakeButton(content, "SellRaw", Strings.SellAmount("1"), 40, UIStyle.Affordable, out TextMeshProUGUI sellRawLabel);
            Height(sellRaw.gameObject, 140);

            Height(Text(content, "PicklesHeader", Strings.PicklesHeader, 44, TextDark, FontStyles.Bold, TextAlignmentOptions.Left).gameObject, 70);
            TextMeshProUGUI noPickles = Text(content, "NoPickles", Strings.NoPicklesYet, 34, TextMuted, FontStyles.Italic);
            Height(noPickles.gameObject, 120);

            RectTransform pickleRows = Node("PickleRows", content);
            VerticalList(pickleRows, 16);
            ItemRowView pickleRow = BuildButtonRow(pickleRows, "PickleRow", Strings.SellAll, out _);
            pickleRow.gameObject.SetActive(false);

            var market = panel.AddComponent<MarketPanel>();
            Set(market, "sellEverythingButton", sellEverything);
            Set(market, "sellEverythingValue", sellValue);
            Set(market, "rawInfo", rawInfo);
            Set(market, "sellRawButton", sellRaw);
            Set(market, "sellRawLabel", sellRawLabel);
            Set(market, "noPicklesLabel", noPickles.gameObject);
            Set(market, "pickleRowTemplate", pickleRow);
            Set(market, "effects", effects);

            panel.SetActive(false);
            return panel;
        }

        // ---------------------------------------------------------------- zone A

        private static void BuildTopBar(RectTransform bar, Toast toast)
        {
            TopStretch(bar, TopBarHeight);
            RectTransform bg = Stretch(Node("Background", bar), new Vector2(16, 10), new Vector2(-16, -10));
            Sliced(Img(bg, art.rounded, new Color(1, 1, 1, 0.78f)), 1.2f).raycastTarget = true;

            // Coins (left)
            RectTransform coinGroup = Node("Coins", bar);
            coinGroup.anchorMin = coinGroup.anchorMax = new Vector2(0, 0.5f);
            coinGroup.pivot = new Vector2(0, 0.5f);
            coinGroup.anchoredPosition = new Vector2(40, 0);
            coinGroup.sizeDelta = new Vector2(290, 110);
            CountUpLabel coins = BuildCounter(coinGroup, art.coin, UIStyle.CoinText);

            // Cucumbers (center) with production rate below
            RectTransform cucumberGroup = Node("Cucumbers", bar);
            cucumberGroup.anchorMin = cucumberGroup.anchorMax = new Vector2(0.5f, 0.5f);
            cucumberGroup.anchoredPosition = new Vector2(0, 14);
            cucumberGroup.sizeDelta = new Vector2(270, 90);
            CountUpLabel cucumbers = BuildCounter(cucumberGroup, art.cucumber, TextDark);

            TextMeshProUGUI rate = Text(bar, "Rate", "+0/s", 30, new Color(0.25f, 0.55f, 0.2f), FontStyles.Bold);
            RectTransform rateRect = Centered(rate.rectTransform, new Vector2(300, 40));
            rateRect.anchoredPosition = new Vector2(0, -44);
            rate.gameObject.SetActive(false);

            // Settings (right)
            RectTransform gear = Node("Settings", bar);
            gear.anchorMin = gear.anchorMax = new Vector2(1, 0.5f);
            gear.pivot = new Vector2(1, 0.5f);
            gear.anchoredPosition = new Vector2(-30, 0);
            gear.sizeDelta = new Vector2(120, 120);
            Image hit = Img(gear, null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            RectTransform gearIcon = Centered(Node("Icon", gear), new Vector2(76, 76));
            Img(gearIcon, art.gear, TextDark);
            var settings = gear.gameObject.AddComponent<Button>();
            settings.targetGraphic = hit;
            settings.transition = Selectable.Transition.None;

            var topBar = bar.gameObject.AddComponent<TopBar>();
            Set(topBar, "coins", coins);
            Set(topBar, "cucumbers", cucumbers);
            Set(topBar, "rateLabel", rate);
            Set(topBar, "coinGroup", coinGroup);
            Set(topBar, "settingsButton", settings);
            Set(topBar, "toast", toast);
        }

        private static CountUpLabel BuildCounter(RectTransform group, Sprite icon, Color textColor)
        {
            RectTransform iconRect = Node("Icon", group);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(38, 0);
            iconRect.sizeDelta = new Vector2(76, 76);
            Img(iconRect, icon, Color.white).preserveAspect = true;

            TextMeshProUGUI label = Text(group, "Value", "0", 52, textColor, FontStyles.Bold, TextAlignmentOptions.Left);
            Stretch(label.rectTransform, new Vector2(92, 0), Vector2.zero);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;

            var counter = group.gameObject.AddComponent<CountUpLabel>();
            Set(counter, "label", label);
            Set(counter, "icon", iconRect);
            return counter;
        }

        // ---------------------------------------------------------------- popups

        private static FloatingTextSpawner BuildFloatingLayer(string name, RectTransform parent, float fontSize, Color color)
        {
            RectTransform layer = Stretch(Node(name, parent));
            TextMeshProUGUI template = Text(layer, "Template", "+1", fontSize, color, FontStyles.Bold);
            Centered(template.rectTransform, new Vector2(600, 120));

            var spawner = layer.gameObject.AddComponent<FloatingTextSpawner>();
            Set(spawner, "template", template);
            template.gameObject.SetActive(false);
            return spawner;
        }

        private static Toast BuildToast(RectTransform parent)
        {
            RectTransform rect = Node("Toast", parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -(TopBarHeight + 20));
            rect.sizeDelta = new Vector2(860, 110);
            rect.gameObject.AddComponent<CanvasGroup>();
            Sliced(Img(rect, art.rounded, new Color(0.15f, 0.3f, 0.12f, 0.94f)), 1.2f);
            TextMeshProUGUI label = Text(rect, "Label", "", 38, Color.white, FontStyles.Bold);
            Stretch(label.rectTransform, new Vector2(30, 0), new Vector2(-30, 0));

            var toast = rect.gameObject.AddComponent<Toast>();
            Set(toast, "label", label);
            return toast;
        }

        private static RecipePicker BuildRecipePicker(RectTransform parent)
        {
            RectTransform dimmer = Stretch(Node("RecipePicker", parent));
            Img(dimmer, null, new Color(0, 0, 0, 0.55f)).raycastTarget = true;

            RectTransform panel = Centered(Node("Panel", dimmer), new Vector2(920, 0));
            Sliced(Img(panel, art.rounded, DrawerColor), 1);
            VerticalLayoutGroup list = VerticalList(panel, 24);
            list.padding = new RectOffset(44, 44, 40, 44);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Height(Text(panel, "Title", Strings.FillJarTitle, 54, TextDark, FontStyles.Bold).gameObject, 80);

            // The list scrolls on screens too short for every recipe. The picker sizes it.
            RectTransform scrollRect = Node("RecipeScroll", panel);
            var listSize = scrollRect.gameObject.AddComponent<LayoutElement>();
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            RectTransform viewport = Stretch(Node("Viewport", scrollRect));
            viewport.gameObject.AddComponent<RectMask2D>();
            Img(viewport, null, new Color(0, 0, 0, 0)).raycastTarget = true;
            RectTransform recipes = TopStretch(Node("Recipes", viewport), 0);
            VerticalList(recipes, 16);
            recipes.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = recipes;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;

            // The whole recipe row is the button.
            ItemRowView row = BuildItemRow(recipes, "RecipeRow", 24, out Image rowBackground);
            rowBackground.raycastTarget = true;
            var rowButton = row.gameObject.AddComponent<Button>();
            rowButton.targetGraphic = rowBackground;
            rowButton.transition = Selectable.Transition.None;
            Set(row, "button", rowButton);
            row.gameObject.SetActive(false);

            TextMeshProUGUI amount = Text(panel, "Amount", "", 42, TextDark, FontStyles.Bold);
            Height(amount.gameObject, 60);
            Slider slider = BuildAmountSlider(panel);
            TextMeshProUGUI preview = Text(panel, "Preview", "", 36, UIStyle.CoinText, FontStyles.Bold);
            Height(preview.gameObject, 60);

            RectTransform buttons = Node("Buttons", panel);
            Height(buttons.gameObject, 140);
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            Button cancel = MakeButton(buttons, "Cancel", Strings.Cancel, 44, UIStyle.Unaffordable, out _);
            Button start = MakeButton(buttons, "Start", Strings.StartBatch, 44, UIStyle.Affordable, out _);

            var picker = dimmer.gameObject.AddComponent<RecipePicker>();
            Set(picker, "rowTemplate", row);
            Set(picker, "recipeScroll", scroll);
            Set(picker, "recipeListSize", listSize);
            Set(picker, "amountLabel", amount);
            Set(picker, "amountSlider", slider);
            Set(picker, "previewLabel", preview);
            Set(picker, "startButton", start);
            Set(picker, "cancelButton", cancel);
            dimmer.gameObject.SetActive(false);
            return picker;
        }

        private static Slider BuildAmountSlider(RectTransform parent)
        {
            const float HandleSize = 90;

            RectTransform root = Node("AmountSlider", parent);
            Height(root.gameObject, HandleSize);

            RectTransform track = Stretch(Node("Track", root));
            track.anchorMin = new Vector2(0, 0.35f);
            track.anchorMax = new Vector2(1, 0.65f);
            Sliced(Img(track, art.rounded, HandleColor), 4);

            // The fill and handle travel inside these areas so the handle's centre stays on the track.
            RectTransform fillArea = Stretch(Node("FillArea", root), new Vector2(HandleSize / 2, 0), new Vector2(-HandleSize / 2, 0));
            fillArea.anchorMin = track.anchorMin;
            fillArea.anchorMax = track.anchorMax;
            RectTransform fill = Node("Fill", fillArea);
            fill.sizeDelta = new Vector2(HandleSize, 0);
            Sliced(Img(fill, art.rounded, UIStyle.Affordable), 4);

            RectTransform handleArea = Stretch(Node("HandleArea", root), new Vector2(HandleSize / 2, 0), new Vector2(-HandleSize / 2, 0));
            RectTransform handle = Node("Handle", handleArea);
            handle.sizeDelta = new Vector2(HandleSize, 0);
            Image handleImage = Img(handle, art.circle, UIStyle.Affordable);
            handleImage.raycastTarget = true;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.transition = Selectable.Transition.None;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            slider.wholeNumbers = true;
            return slider;
        }

        private static ConfirmDialog BuildConfirmDialog(RectTransform parent)
        {
            RectTransform dimmer = Stretch(Node("ConfirmDialog", parent));
            Img(dimmer, null, new Color(0, 0, 0, 0.55f)).raycastTarget = true;

            RectTransform panel = Centered(Node("Panel", dimmer), new Vector2(860, 460));
            Sliced(Img(panel, art.rounded, DrawerColor), 1);

            TextMeshProUGUI message = Text(panel, "Message", "", 48, TextDark, FontStyles.Bold);
            Stretch(message.rectTransform, new Vector2(40, 200), new Vector2(-40, -40));

            RectTransform buttons = Node("Buttons", panel);
            buttons.anchorMin = new Vector2(0, 0);
            buttons.anchorMax = new Vector2(1, 0);
            buttons.pivot = new Vector2(0.5f, 0);
            buttons.sizeDelta = new Vector2(-80, 140);
            buttons.anchoredPosition = new Vector2(0, 40);
            var h = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 30;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;

            Button cancel = MakeButton(buttons, "Cancel", Strings.Cancel, 40, UIStyle.Unaffordable, out TextMeshProUGUI cancelLabel);
            Button confirm = MakeButton(buttons, "Confirm", Strings.Quit, 40, DotRed, out TextMeshProUGUI confirmLabel);

            var dialog = dimmer.gameObject.AddComponent<ConfirmDialog>();
            Set(dialog, "message", message);
            Set(dialog, "confirmButton", confirm);
            Set(dialog, "confirmLabel", confirmLabel);
            Set(dialog, "cancelButton", cancel);
            Set(dialog, "cancelLabel", cancelLabel);
            dimmer.gameObject.SetActive(false);
            return dialog;
        }

        // ---------------------------------------------------------------- helpers

        private static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static RectTransform Stretch(RectTransform rt) => Stretch(rt, Vector2.zero, Vector2.zero);

        private static RectTransform Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static RectTransform TopStretch(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static RectTransform BottomStretch(RectTransform rt, float bottom, float height, float sideInset)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(-2 * sideInset, height);
            rt.anchoredPosition = new Vector2(0, bottom);
            return rt;
        }

        private static RectTransform Centered(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        private static Image Img(RectTransform rt, Sprite sprite, Color color)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>9-sliced rounded sprite. Higher multiplier = smaller corner radius.</summary>
        private static Image Sliced(Image image, float pixelsPerUnitMultiplier)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            return image;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
            FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform rt = Node(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button MakeButton(Transform parent, string name, string label, float fontSize, Color color,
            out TextMeshProUGUI text)
        {
            RectTransform rt = Node(name, parent);
            Image bg = Sliced(Img(rt, art.rounded, color), 1.6f);
            bg.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;

            text = Text(rt, "Label", label, fontSize, Color.white, FontStyles.Bold);
            Stretch(text.rectTransform, new Vector2(16, 0), new Vector2(-16, 0));
            return button;
        }

        private static VerticalLayoutGroup VerticalList(RectTransform rt, float spacing)
        {
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static void Height(GameObject go, float height)
        {
            // No ?? here: Unity's fake-null objects break the null-coalescing operator.
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"SceneBuilder: {target.GetType().Name} has no serialized field '{field}'");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
