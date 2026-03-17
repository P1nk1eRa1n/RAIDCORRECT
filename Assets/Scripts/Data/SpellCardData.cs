using UnityEngine;

[CreateAssetMenu(menuName = "Spell/CardData", fileName = "NewSpellCard")]
public class SpellCardData : ScriptableObject
{
    public string cardId; // уникальный id, lowercase preferred
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public int manaCost = 0;
    // add whatever other data you need (damage, target type, tags, rarity etc.)

    // Optional: gameplay prefab/ability reference
    // public GameObject abilityPrefab;
}