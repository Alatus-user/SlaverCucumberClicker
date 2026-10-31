using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>Drawer tab with the two-column jar grid and the Collect All button.</summary>
    public class PicklePanel : MonoBehaviour
    {
        private const int CollectAllMinimum = 2;

        [SerializeField] private Button collectAllButton;
        [SerializeField] private GridLayoutGroup grid;
        [Tooltip("Inactive child of the grid used as the template.")]
        [SerializeField] private JarView jarTemplate;
        [SerializeField] private RecipePicker recipePicker;
        [SerializeField] private FloatingTextSpawner effects;

        private readonly List<JarView> views = new List<JarView>();
        private JarView lockedSlot;

        private void Start()
        {
            lockedSlot = Instantiate(jarTemplate, grid.transform);
            lockedSlot.Clicked += BuyExtraJar;

            collectAllButton.onClick.AddListener(() =>
                Collected(GameManager.Instance.Pickling.CollectAll(), collectAllButton.transform));
        }

        private void Update()
        {
            PicklingManager pickling = GameManager.Instance.Pickling;

            while (views.Count < pickling.Jars.Count) AddView(views.Count);

            long now = TimeService.UtcNowTicks;
            for (int i = 0; i < views.Count; i++)
            {
                JarState jar = pickling.Jars[i];
                views[i].Render(jar, pickling.FindRecipe(jar.recipeId), now);
            }

            RefreshLockedSlot();

            bool showCollectAll = pickling.ReadyCount >= CollectAllMinimum;
            if (collectAllButton.gameObject.activeSelf != showCollectAll)
                collectAllButton.gameObject.SetActive(showCollectAll);

            FitCellsToWidth();
        }

        private void AddView(int index)
        {
            JarView view = Instantiate(jarTemplate, grid.transform);
            view.gameObject.SetActive(true);
            view.Clicked += () => OnJarClicked(index, view);
            views.Add(view);
            lockedSlot.transform.SetAsLastSibling();
        }

        private void RefreshLockedSlot()
        {
            UpgradeManager upgrades = GameManager.Instance.Upgrades;
            ShopItem extraJar = upgrades.Find(UpgradeEffect.JarCount);
            bool show = extraJar != null && upgrades.Visibility(extraJar) == ShopVisibility.Available;

            if (lockedSlot.gameObject.activeSelf != show) lockedSlot.gameObject.SetActive(show);
            if (show) lockedSlot.RenderLocked(extraJar.DisplayName, Strings.JarBuyCost(NumberFormatter.Format(extraJar.Cost)));
        }

        private void BuyExtraJar()
        {
            UpgradeManager upgrades = GameManager.Instance.Upgrades;
            if (upgrades.TryBuy(upgrades.Find(UpgradeEffect.JarCount))) Haptics.Light();
        }

        private void OnJarClicked(int index, JarView view)
        {
            PicklingManager pickling = GameManager.Instance.Pickling;
            switch (pickling.Jars[index].StatusAt(TimeService.UtcNowTicks))
            {
                case JarStatus.Empty:
                    recipePicker.Open(index);
                    break;
                case JarStatus.Ready:
                    Collected(pickling.Collect(index), view.transform);
                    break;
            }
        }

        private void Collected(double pickles, Transform source)
        {
            if (pickles <= 0) return;
            effects.SpawnAtWorld(source.position, Strings.PickleGain(NumberFormatter.Format(pickles)), UIStyle.Affordable);
            Haptics.Light();
        }

        /// <summary>The drawer's width depends on the screen's aspect ratio, so cells can't be a fixed size.</summary>
        private void FitCellsToWidth()
        {
            float width = (((RectTransform)grid.transform).rect.width - grid.spacing.x) / grid.constraintCount;
            if (width > 0 && !Mathf.Approximately(width, grid.cellSize.x))
                grid.cellSize = new Vector2(width, grid.cellSize.y);
        }
    }
}
