// Seat.cs
using UnityEngine;

[RequireComponent(typeof(DraggableObject))]
public class Seat : MonoBehaviour
{
    [Header("Seat data")]
    public SeatData data;

    // flag: сидит ли персонаж сейчас (если true Ч стул нельз€ двигать)
    public bool IsOccupied { get; private set; } = false;

    // если стул размещЄн в слоте Ч ссылка
    public SeatSlot PlacedInSlot { get; private set; } = null;

    // Optional: store occupant identifier (null if empty)
    public string occupantId = null;

    // Called by SeatSlot.PlaceSeat
    public void PlaceInSlot(SeatSlot slot)
    {
        PlacedInSlot = slot;
        // we don't mark IsOccupied here Ч that is done by NPC system when someone sits
    }

    public void RemoveFromSlot()
    {
        PlacedInSlot = null;
    }

    public bool TrySetOccupied(string occupantIdentifier)
    {
        if (IsOccupied) return false;
        IsOccupied = true;
        occupantId = occupantIdentifier;
        return true;
    }

    public bool TryClearOccupied()
    {
        if (!IsOccupied) return false;
        IsOccupied = false;
        occupantId = null;
        return true;
    }
}
