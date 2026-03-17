using UnityEngine;

// Не реализуем IDraggable тут — за перетаскивание отвечает DraggableObject, это просто meta-component
[RequireComponent(typeof(DraggableObject))]
public class RecipePage : MonoBehaviour
{
    [Tooltip("Ссылка на ScriptableObject с рецептом")]
    public RecipeData recipe;

    [Tooltip("Если true — страница расходуется (удаляется) после добавления в книгу")]
    public bool consumeOnUse = true;

    // Можно показывать мини-иконку/название при hover — по желанию
}
