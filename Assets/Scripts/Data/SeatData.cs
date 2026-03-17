using UnityEngine;

[CreateAssetMenu(menuName = "Seating/SeatData", fileName = "NewSeatData")]
public class SeatData : ScriptableObject
{
    public string seatId; // unique id e.g. "stool_wood_01"
    public string displayName;
    public Sprite icon;
    public SeatCategory category = SeatCategory.Chair;
    [Range(0f, 1f)] public float comfort = 0.5f; // 0..1 affects revenue / satisfaction
    [TextArea] public string description;
}

public enum SeatCategory
{
    Stool,
    Chair,
    Armchair,
    Throne
}