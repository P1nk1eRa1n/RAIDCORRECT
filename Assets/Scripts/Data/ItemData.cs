using UnityEngine;

[CreateAssetMenu(menuName = "Ritual/ItemData", fileName = "NewItemData")]
public class ItemData : ScriptableObject
{
    public string id; // уникальная строка, например "emerald_01"
    public string displayName;
    public Sprite icon;
    [Tooltip("Список тегов, которые участвуют в рецептах")]
    public string[] tags;
    public GameObject prefab;
}