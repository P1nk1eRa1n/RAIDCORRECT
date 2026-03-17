using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BookDropArea : MonoBehaviour
{
    [Tooltip("Референс на UI книги (опционально)")]
    public RecipeBookUI bookUI;

    public bool AcceptPage(GameObject page)
    {
        if (page == null) return false;

        var recipePage = page.GetComponent<RecipePage>();
        if (recipePage == null || recipePage.recipe == null) return false;

        // Проверяем RitualManager
        if (RitualManager.Instance == null)
        {
            Debug.LogWarning("[BookDropArea] RitualManager.Instance is null");
            return false;
        }

        // Учим рецепт (persist: true по умолчанию в новом API)
        bool learned = RitualManager.Instance.LearnRecipe(recipePage.recipe);

        if (learned)
        {
            Debug.Log($"[BookDropArea] Learned recipe: {recipePage.recipe.recipeName}");

            // Обновляем UI книги если привязана
            if (bookUI != null)
            {
                // Refresh автоматически вызывается через OnKnownRecipesChanged
                // Показываем последнюю страницу
                int lastPageIndex = Mathf.Max(0, bookUI.PageCount - 1);
                bookUI.ShowPage(lastPageIndex);
            }

            // Удаляем страницу если нужно
            if (recipePage.consumeOnUse)
            {
                Destroy(page);
            }
            return true;
        }
        else
        {
            Debug.Log($"[BookDropArea] Recipe already known: {recipePage.recipe.recipeName}");

            // Можно оставить страницу или тоже уничтожить
            if (recipePage.consumeOnUse)
            {
                Destroy(page);
            }
            return false;
        }
    }

    // Перегруженный метод для удобства
    public bool AcceptPage(RecipePage recipePage)
    {
        return AcceptPage(recipePage != null ? recipePage.gameObject : null);
    }

    // Для OnTriggerEnter/OnCollisionEnter (если используешь)
    private void OnTriggerEnter(Collider other)
    {
        AcceptPage(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        AcceptPage(collision.gameObject);
    }
}
