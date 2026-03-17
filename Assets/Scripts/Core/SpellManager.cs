using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpellManager : MonoBehaviour
{
    public static SpellManager Instance { get; private set; }

    [Header("Player collection (edit in inspector or add at runtime)")]
    [SerializeField] private List<SpellCardData> inspectorStartingCollection = new List<SpellCardData>();

    private List<SpellCardData> collection = new List<SpellCardData>();

    [Header("Active pedestal (optional)")]
    public SpellPedestal activePedestal;

    public event System.Action OnCollectionChanged;
    public event System.Action OnPreparedChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        Instance = this;
        collection = new List<SpellCardData>(inspectorStartingCollection);

        if (activePedestal != null)
            activePedestal.OnPreparedChanged += NotifyPreparedChanged;
    }

    private void OnDestroy()
    {
        if (activePedestal != null)
            activePedestal.OnPreparedChanged -= NotifyPreparedChanged;
    }

    // Collection API
    public IReadOnlyList<SpellCardData> GetCollection() => collection.AsReadOnly();

    public bool AddToCollection(SpellCardData d)
    {
        if (d == null) return false;
        if (collection.Any(x => x.cardId == d.cardId)) return false;
        collection.Add(d);
        NotifyCollectionChanged();
        return true;
    }

    public bool RemoveFromCollection(SpellCardData d)
    {
        if (d == null) return false;
        bool removed = collection.RemoveAll(x => x.cardId == d.cardId) > 0;
        if (removed) NotifyCollectionChanged();
        return removed;
    }

    // Prepared API (via pedestal)
    public IReadOnlyList<CardItem> GetPreparedCards()
    {
        if (activePedestal == null) return new List<CardItem>().AsReadOnly();
        return activePedestal.GetPreparedCards();
    }

    // ---- FIXED: TryPrepareCard ----
    // Теперь корректно обрабатываем both branches: slotIndex >= 0 (bool) и fallback (int index)
    public bool TryPrepareCard(CardItem card, int slotIndex = -1)
    {
        if (card == null) return false;
        if (activePedestal == null) return false;

        if (slotIndex >= 0)
        {
            // direct place into explicit slot -> returns bool
            bool ok = activePedestal.TryPlaceCardIntoSlotIndex(card, slotIndex);
            if (ok) NotifyPreparedChanged();
            return ok;
        }
        else
        {
            // try first available -> returns int index (-1 == failed)
            int idx = activePedestal.TryPlaceCardIntoFirstAvailable(card);
            bool ok = (idx >= 0);
            if (ok) NotifyPreparedChanged();
            return ok;
        }
    }

    public bool TryUnprepareCard(int slotIndex)
    {
        if (activePedestal == null) return false;
        bool ok = activePedestal.RemoveCardFromSlotIndex(slotIndex);
        if (ok) NotifyPreparedChanged();
        return ok;
    }

    public SpellCardData GetCardById(string id) => collection.FirstOrDefault(c => c.cardId == id);

    // ------- Notification wrappers (safe invocation from external classes) -------
    public void NotifyPreparedChanged()
    {
        try { OnPreparedChanged?.Invoke(); }
        catch (System.Exception ex) { Debug.LogWarning("[SpellManager] Exception in NotifyPreparedChanged: " + ex.Message); }
    }

    public void NotifyCollectionChanged()
    {
        try { OnCollectionChanged?.Invoke(); }
        catch (System.Exception ex) { Debug.LogWarning("[SpellManager] Exception in NotifyCollectionChanged: " + ex.Message); }
    }
}