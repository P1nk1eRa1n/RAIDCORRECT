using UnityEngine;

[CreateAssetMenu(menuName = "Ritual/RecipeData", fileName = "NewRecipe")]
public class RecipeData : ScriptableObject
{
    public string recipeId;
    public string recipeName;
    [Tooltip("ћультисписок тегов Ч пор€док не важен, элемент может повтор€тьс€")]
    public string[] ingredientTags;

    [Header("Result")]
    public string resultName; // текстовый итог (дл€ студии / консоли)
    public GameObject resultPrefab; // существо/эффект (можно null, тогда только лог)
}