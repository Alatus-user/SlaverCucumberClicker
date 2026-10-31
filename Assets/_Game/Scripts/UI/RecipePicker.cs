using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CucumberClicker.UI
{
    /// <summary>Popup that fills an empty jar: choose a recipe, choose an amount, press Start.</summary>
    public class RecipePicker : MonoBehaviour
    {
        [Tooltip("Inactive child of the recipe list used as the template.")]
        [SerializeField] private ItemRowView rowTemplate;
        [SerializeField] private TextMeshProUGUI amountLabel;
        [SerializeField] private Slider amountSlider;
        [SerializeField] private TextMeshProUGUI previewLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private ScrollRect recipeScroll;
        [SerializeField] private LayoutElement recipeListSize;
        [Tooltip("One recipe row plus the gap below it.")]
        [SerializeField] private float rowStep = 166;
        [Tooltip("Height of everything in the popup except the recipe list, plus a margin.")]
        [SerializeField] private float reservedHeight = 750;
        [SerializeField] private Color rowColor = Color.white;
        [SerializeField] private Color selectedRowColor = new Color(0.76f, 0.91f, 0.65f);

        private readonly List<ItemRowView> rows = new List<ItemRowView>();
        private int jarIndex;
        private RecipeData selected;

        public bool IsOpen => gameObject.activeSelf;

        private void Awake()
        {
            amountSlider.onValueChanged.AddListener(_ => Refresh());
            startButton.onClick.AddListener(StartBatch);
            cancelButton.onClick.AddListener(Close);
        }

        public void Open(int jar)
        {
            gameObject.SetActive(true);
            jarIndex = jar;

            PicklingManager pickling = GameManager.Instance.Pickling;
            Market market = GameManager.Instance.Market;
            if (selected == null && pickling.Recipes.Count > 0) selected = pickling.Recipes[0];

            while (rows.Count < pickling.Recipes.Count) AddRow();
            for (int i = 0; i < rows.Count; i++)
            {
                RecipeData recipe = pickling.Recipes[i];
                rows[i].Show(recipe.displayName, Strings.RecipeInfo(
                    NumberFormatter.Format(market.PickleSellValue(pickling.ValuePerPickle(recipe))),
                    NumberFormatter.FormatDuration(pickling.DurationSeconds(recipe))), recipe.jarTint);
            }

            // Short lists fit exactly; long ones scroll on short screens such as tablets.
            float available = ((RectTransform)transform).rect.height - reservedHeight;
            float height = Mathf.Min(pickling.Recipes.Count * rowStep, available);
            recipeListSize.minHeight = height;
            recipeListSize.preferredHeight = height;
            recipeScroll.verticalNormalizedPosition = 1;

            // Default to a full jar. The slider stays at 1 when there's nothing to choose.
            float max = Mathf.Max(1, (float)pickling.MaxBatch);
            amountSlider.minValue = 1;
            amountSlider.maxValue = max;
            amountSlider.SetValueWithoutNotify(max);
            amountSlider.interactable = max > 1;
            Refresh();
        }

        public void Close() => gameObject.SetActive(false);

        private void AddRow()
        {
            int index = rows.Count;
            ItemRowView row = Instantiate(rowTemplate, rowTemplate.transform.parent);
            row.gameObject.SetActive(true);
            row.Clicked += () =>
            {
                selected = GameManager.Instance.Pickling.Recipes[index];
                Refresh();
            };
            rows.Add(row);
        }

        private void Refresh()
        {
            PicklingManager pickling = GameManager.Instance.Pickling;
            Market market = GameManager.Instance.Market;
            double max = pickling.MaxBatch;
            double amount = amountSlider.value;
            bool canStart = selected != null && max >= 1;

            for (int i = 0; i < rows.Count; i++)
                rows[i].SetBackground(pickling.Recipes[i] == selected ? selectedRowColor : rowColor);

            amountLabel.text = Strings.BatchAmount(NumberFormatter.Format(canStart ? amount : 0), NumberFormatter.Format(max));
            previewLabel.text = canStart
                ? Strings.BatchPreview(NumberFormatter.Format(market.PickleSellValue(pickling.BatchValue(selected, amount))),
                    NumberFormatter.FormatDuration(pickling.DurationSeconds(selected)))
                : Strings.NoCucumbersToPickle;
            UIStyle.SetAvailable(startButton, canStart);
        }

        private void StartBatch()
        {
            if (!GameManager.Instance.Pickling.TryStartBatch(jarIndex, selected, amountSlider.value)) return;
            Haptics.Light();
            Close();
        }
    }
}
