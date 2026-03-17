using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeBookUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI ingredientsText;
    public Image icon;
    public Button nextBtn;
    public Button prevBtn;

    public int PageCount => pages?.Count ?? 0;

    private List<RecipeData> pages = new();
    private int currentIndex = 0;

    private void Awake()
    {
        if (nextBtn != null) nextBtn.onClick.AddListener(NextPage);
        if (prevBtn != null) prevBtn.onClick.AddListener(PrevPage);
    }

    private void OnEnable() => StartCoroutine(Initialize());

    private void OnDisable()
    {
        if (RitualManager.Instance != null)
            RitualManager.Instance.OnKnownRecipesChanged -= Refresh;
    }

    private IEnumerator Initialize()
    {
        // ∆дем RitualManager
        while (RitualManager.Instance == null)
            yield return null;

        RitualManager.Instance.OnKnownRecipesChanged += Refresh;
        Refresh();
        ShowPage(0);
    }

    public void Refresh()
    {
        pages = RitualManager.Instance?.KnownRecipes?.ToList() ?? new List<RecipeData>();
        currentIndex = Mathf.Clamp(currentIndex, 0, pages.Count - 1);
        Debug.Log($"[BookUI] Refreshed: {pages.Count} recipes");
    }

    public void ShowPage(int index)
    {
        if (pages == null || pages.Count == 0)
        {
            if (titleText != null) titleText.text = "No known recipes";
            if (ingredientsText != null) ingredientsText.text = "";
            if (icon != null) icon.enabled = false;
            currentIndex = -1;
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, pages.Count - 1);
        var recipe = pages[currentIndex];

        if (titleText != null)
            titleText.text = $"{recipe.recipeName} ({currentIndex + 1}/{pages.Count})";
        if (ingredientsText != null)
            ingredientsText.text = string.Join(", ", recipe.ingredientTags);
        if (icon != null) icon.enabled = false;
    }

    private void NextPage() => ShowPage((currentIndex + 1) % Mathf.Max(1, pages.Count));
    private void PrevPage() => ShowPage((currentIndex + 1 + pages.Count) % pages.Count);
}
