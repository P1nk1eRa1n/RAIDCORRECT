using UnityEngine;

// Создай Asset: Assets -> Create -> Lich -> VisitorData
[CreateAssetMenu(menuName = "Lich/VisitorData")]
public class VisitorData : ScriptableObject
{
    [Header("Идентификация")]
    public string visitorId; // уникальный id, например "villager_01"
    public string displayName;

    [Header("Визуал (billboard)")]
    public GameObject silhouettePrefab; // префаб биллборда (2D плейн или спрайт)
    public bool canFly = false;

    [Header("Кровь / подозрительность")]
    public Color bloodColor = Color.red;
    public bool suspicious = false;

    [Header("Фразы / просьбы")]
    [TextArea] public string greetingLine;    // фраза при приоткрытии
    [TextArea] public string requestLines;    // основная просьба / список
}