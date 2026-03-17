using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RitualManager : MonoBehaviour
{
    public static RitualManager Instance { get; private set; }

    [Header("Recipe DB")]
    public RecipeData[] allRecipes;

    [Header("Player Progress")]
    [SerializeField] private List<RecipeData> inspectorKnownRecipes = new List<RecipeData>();

    [Header("Game Objects")]
    public GameObject dummyPrefab;
    public Transform spawnPoint;

    private List<BowlArea> bowls = new List<BowlArea>();
    private List<RecipeData> knownRecipes = new List<RecipeData>();

    public IReadOnlyList<RecipeData> KnownRecipes => knownRecipes.AsReadOnly();

    [Header("Matching")]
    public bool requireExactMatch = true;
    public bool allowSupersetMatch = false;
    public bool autoPerformOnFull = false;

    public event System.Action OnKnownRecipesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeBowls();
        InitializeKnownRecipes();
        OnKnownRecipesChanged?.Invoke();
    }

    private void InitializeBowls()
    {

        bowls = UnityEngine.Object.FindObjectsByType<BowlArea>(UnityEngine.FindObjectsSortMode.None).ToList();

    }

    private void InitializeKnownRecipes()
    {
        knownRecipes.Clear();
        knownRecipes.AddRange(inspectorKnownRecipes);

        Debug.Log($"[Ritual] Initialized with {knownRecipes.Count} inspector recipes");
    }

    public void OnBowlUpdated(BowlArea bowl)
    {
        if (!autoPerformOnFull || bowls.Count == 0) return;

        if (bowls.All(b => b.Items?.Count >= 1))
            TryPerformRitual();
    }

    public void TryPerformRitual()
    {
        var tags = bowls.SelectMany(b => b.GetAllTags()).ToList();
        var matched = FindMatchingRecipe(tags);

        if (matched != null)
        {
            Debug.Log($"[Ritual] Success: {matched.recipeName}  {matched.resultName}");

            LearnRecipe(matched); // всегда учим, даже если уже знали
            SpawnResult(matched.resultPrefab);
        }
        else
        {
            Debug.Log("[Ritual] No match - dummy spawned");
            SpawnResult(dummyPrefab);
        }

        ClearAllBowls();
    }

    private void SpawnResult(GameObject prefab)
    {
        if (prefab != null && spawnPoint != null)
            Instantiate(prefab, spawnPoint.position, Quaternion.identity);
    }

    private RecipeData FindMatchingRecipe(List<string> tags)
    {
        var targetCounts = tags.ToTagCounts();
        Debug.Log($"[Ritual] Looking for: {string.Join(", ", tags)}  {StringifyCounts(targetCounts)}");

        if (allRecipes == null || allRecipes.Length == 0)
        {
            Debug.LogError("[Ritual]  allRecipes EMPTY!");
            return null;
        }

        foreach (var recipe in allRecipes)
        {
            if (recipe == null || recipe.ingredientTags == null) continue;

            Debug.Log($"[Recipe] '{recipe.recipeName}': {string.Join(", ", recipe.ingredientTags)}");

            if (MatchesRecipe(recipe.ingredientTags, targetCounts))
            {
                Debug.Log($"[Ritual]  MATCHED: {recipe.recipeName}");
                return recipe;
            }
        }

        Debug.LogError("[Ritual]  NO MATCHES FOUND");
        return null;
    }

    private bool MatchesRecipe(string[] requiredTags, Dictionary<string, int> availableCounts)
    {
        var requiredCounts = requiredTags.ToTagCounts();

        Debug.Log($"[Match] Required:  {StringifyCounts(requiredCounts)}");
        Debug.Log($"[Match] Available: {StringifyCounts(availableCounts)}");

        if (requireExactMatch)
        {
            // Проверяем количество элементов
            if (requiredCounts.Count != availableCounts.Count)
            {
                Debug.Log("[Match]  Different count");
                return false;
            }

            // Проверяем каждый ключ и значение
            foreach (var kv in requiredCounts)
            {
                if (!availableCounts.TryGetValue(kv.Key, out int count) || count != kv.Value)
                {
                    Debug.Log($"[Match]  {kv.Key}: {kv.Value} != {count}");
                    return false;
                }
            }

            Debug.Log("[Match]  EXACT MATCH!");
            return true;
        }

        if (allowSupersetMatch)
        {
            bool isSuperset = requiredCounts.All(kv =>
                availableCounts.TryGetValue(kv.Key, out int count) && count >= kv.Value);
            Debug.Log($"[Match] Superset: {isSuperset}");
            return isSuperset;
        }

        Debug.Log("[Match] No match mode");
        return false;
    }

    private string StringifyCounts(Dictionary<string, int> counts)
    {
        return string.Join(", ", counts.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private void ClearAllBowls()
    {
        foreach (var bowl in bowls.Where(b => b != null))
            bowl.ForceClear(destroyItems: true);

        InitializeBowls();
    }

    public bool LearnRecipe(RecipeData recipe)
    {
        if (recipe == null || HasRecipe(recipe))
            return false;

        knownRecipes.Add(recipe);
        OnKnownRecipesChanged?.Invoke();
        Debug.Log($"[Ritual] Learned: {recipe.recipeName}");
        return true;
    }

    public bool HasRecipe(RecipeData recipe) =>
        recipe != null && knownRecipes.Any(r => r.recipeId == recipe.recipeId);
}

// Extension methods
public static class TagExtensions
{
    public static Dictionary<string, int> ToTagCounts(this IEnumerable<string> tags)
    {
        return tags?.Where(t => !string.IsNullOrEmpty(t))
                    .GroupBy(t => t.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Count()) ?? new();
    }
}
