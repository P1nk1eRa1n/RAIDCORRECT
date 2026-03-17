using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class SpellPedestal : MonoBehaviour
{
    [Header("Slots (child CardSlot transforms)")]
    [Tooltip("Если оставить пустым — заполнится автоматически дочерними CardSlot'ами")]
    public CardSlot[] preparedSlots;

    [Header("Settings")]
    [Tooltip("Сколько слотов активно сейчас (можно менять в рантайме как апгрейд)")]
    public int maxSlots = 3;

    public event System.Action OnPreparedChanged;

    private CardSlot[] allSlots;

    private void Awake()
    {
        // get all child slots (full list)
        allSlots = GetComponentsInChildren<CardSlot>(true);

        if (preparedSlots == null || preparedSlots.Length == 0)
            preparedSlots = allSlots;
    }

    private void Start()
    {
        ApplyActiveCount();
    }

    // Применить maxSlots: включаем первые maxSlots из allSlots, остальные выключаем
    public void ApplyActiveCount()
    {
        if (allSlots == null || allSlots.Length == 0) return;

        int clamped = Mathf.Clamp(maxSlots, 0, allSlots.Length);
        for (int i = 0; i < allSlots.Length; i++)
        {
            bool enable = i < clamped;
            allSlots[i].SetEnabled(enable);
        }

        // обновим preparedSlots референс (первые maxSlots)
        preparedSlots = allSlots.Take(clamped).ToArray();

        Debug.Log($"[SpellPedestal] Applied active slots = {clamped} / total {allSlots.Length}");
    }

    // Runtime: изменить количество активных слотов (например, при апгрейде)
    public void SetActiveSlotsCount(int newCount)
    {
        maxSlots = Mathf.Clamp(newCount, 0, allSlots.Length);
        ApplyActiveCount();
        OnPreparedChanged?.Invoke();
    }

    public IReadOnlyList<CardItem> GetPreparedCards()
    {
        return preparedSlots.Select(s => s?.placedCard).ToList().AsReadOnly();
    }

    public int TryPlaceCardIntoFirstAvailable(CardItem card)
    {
        for (int i = 0; i < preparedSlots.Length; i++)
        {
            var slot = preparedSlots[i];
            if (slot == null) continue;
            if (slot.placedCard == null)
            {
                bool ok = slot.PlaceCard(card);
                if (ok)
                {
                    OnPreparedChanged?.Invoke();
                    return i;
                }
            }
        }
        return -1;
    }

    public bool TryPlaceCardIntoSlotIndex(CardItem card, int index)
    {
        if (index < 0 || index >= preparedSlots.Length) return false;
        var slot = preparedSlots[index];
        if (slot == null) return false;
        bool ok = slot.PlaceCard(card);
        if (ok) OnPreparedChanged?.Invoke();
        return ok;
    }

    public bool RemoveCardFromSlotIndex(int index)
    {
        if (index < 0 || index >= preparedSlots.Length) return false;
        var slot = preparedSlots[index];
        if (slot == null) return false;
        bool ok = slot.RemoveCard();
        if (ok) OnPreparedChanged?.Invoke();
        return ok;
    }
}