using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[DefaultExecutionOrder(100)]
public class SeatingManager : MonoBehaviour
{
    public static SeatingManager Instance { get; private set; }

    [Header("Slots")]
    public SeatSlot[] frontRowSlots = new SeatSlot[6];
    public SeatSlot[] backRowSlots = new SeatSlot[6];

    [Header("State (serializable)")]
    public List<SeatAssignment> assignments = new List<SeatAssignment>(); // public / editable

    // событие: слот обновлЄн Ч передаЄм глобальный индекс слота
    public event Action<int> OnSlotUpdatedEvent;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        Instance = this;
    }

    private void Start()
    {
        // «ащитно: если массивы null Ч замен€ем на пустые
        if (frontRowSlots == null) frontRowSlots = new SeatSlot[0];
        if (backRowSlots == null) backRowSlots = new SeatSlot[0];

        EnsureAssignmentsSize();

        // »нициализируем assignments по текущему состо€нию слотов (если на сцене уже есть размещЄнные стуль€)
        for (int i = 0; i < frontRowSlots.Length; i++)
            if (frontRowSlots[i] != null) NotifySlotUpdated(frontRowSlots[i]);

        for (int i = 0; i < backRowSlots.Length; i++)
            if (backRowSlots[i] != null) NotifySlotUpdated(backRowSlots[i]);
    }

    private void EnsureAssignmentsSize()
    {
        int total = (frontRowSlots?.Length ?? 0) + (backRowSlots?.Length ?? 0);
        while (assignments.Count < total)
            assignments.Add(new SeatAssignment { slotIndex = assignments.Count, occupied = false, occupantId = null, seatPrefabId = null });
        while (assignments.Count > total)
            assignments.RemoveAt(assignments.Count - 1);
    }

    //  онвертаци€ Slot -> глобальный индекс (0..N-1)
    public int SlotToIndex(SeatSlot slot)
    {
        if (slot == null) return -1;
        for (int i = 0; i < frontRowSlots.Length; i++) if (frontRowSlots[i] == slot) return i;
        for (int i = 0; i < backRowSlots.Length; i++) if (backRowSlots[i] == slot) return frontRowSlots.Length + i;
        return -1;
    }


    // ћетод, который вызывают внешние объекты (SeatSlot) чтобы сообщить об изменении
    public void NotifySlotUpdated(SeatSlot slot)
    {
        if (slot == null) return;
        int idx = SlotToIndex(slot);
        if (idx < 0) return;
        if (idx >= assignments.Count) EnsureAssignmentsSize();
        var ass = assignments[idx];
        if (slot.placedSeat == null)
        {
            ass.occupied = false;
            ass.occupantId = null;
            ass.seatPrefabId = null;
        }
        else
        {
            // если сиденье есть Ч считаем слот зан€тым в момент посадки
            ass.occupied = false; // при инициализации Ч false
            ass.seatPrefabId = slot.placedSeat.name;
        }
        assignments[idx] = ass;
        OnSlotUpdatedEvent?.Invoke(idx);
    }

    // ”добство: получить слот по глобальному индексу
    public SeatSlot GetSlotByIndex(int idx)
    {
        if (idx < 0) return null;
        if (idx < frontRowSlots.Length) return frontRowSlots[idx];
        int backIdx = idx - frontRowSlots.Length;
        if (backIdx < 0) return null;
        if (backIdx < backRowSlots.Length) return backRowSlots[backIdx];
        return null;
    }

    // ѕопытатьс€ посадить посетител€ в слот (NPC system)
    public bool TrySeatVisitorAt(int slotIndex, string visitorId)
    {
        var slot = GetSlotByIndex(slotIndex);
        if (slot == null) return false;
        if (slot.placedSeat == null) return false;

        if (slot.placedSeat.IsOccupied) return false;

        bool ok = slot.placedSeat.TrySetOccupied(visitorId);
        if (ok)
        {
            // обновим assignments аккуратно
            if (slotIndex >= 0 && slotIndex < assignments.Count)
            {
                SeatAssignment ass = assignments[slotIndex];
                ass.occupied = true;
                ass.occupantId = visitorId;
                assignments[slotIndex] = ass;
            }
            OnSlotUpdatedEvent?.Invoke(slotIndex);
        }
        return ok;
    }

    public bool TryClearVisitorAt(int slotIndex)
    {
        var slot = GetSlotByIndex(slotIndex);
        if (slot == null) return false;
        if (slot.placedSeat == null) return false;
        if (!slot.placedSeat.IsOccupied) return false;

        bool ok = slot.placedSeat.TryClearOccupied();
        if (ok)
        {
            if (slotIndex >= 0 && slotIndex < assignments.Count)
            {
                SeatAssignment ass = assignments[slotIndex];
                ass.occupied = false;
                ass.occupantId = null;
                assignments[slotIndex] = ass;
            }
            OnSlotUpdatedEvent?.Invoke(slotIndex);
        }
        return ok;
    }
}

[System.Serializable]
public struct SeatAssignment
{
    public int slotIndex;
    public bool occupied;
    public string occupantId; // null or identifier for NPC
    public string seatPrefabId; // what seat type is placed here (SeatData.seatId)
}