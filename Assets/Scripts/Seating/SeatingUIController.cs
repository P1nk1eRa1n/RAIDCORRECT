using UnityEngine;
using UnityEngine.UI;

public class SeatingUIController : MonoBehaviour
{
    public GameObject rootPanel;
    public Transform contentParent;
    public Button slotButtonPrefab;
    public Button cancelButton;

    private void OnEnable()
    {
        if (SeatingManager.Instance != null)
            SeatingManager.Instance.OnSlotUpdatedEvent += OnSlotUpdated;

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSelection);

        Invoke(nameof(DelayedRefresh), 0.01f);
        SetPanelsActive(true);
    }

    private void OnDisable()
    {
        if (SeatingManager.Instance != null)
            SeatingManager.Instance.OnSlotUpdatedEvent -= OnSlotUpdated;

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelSelection);

        CancelInvoke(nameof(DelayedRefresh));
    }

    private void OnSlotUpdated(int idx) => Refresh();
    private void DelayedRefresh() => Refresh();

    public void Refresh()
    {
        if (SeatingManager.Instance == null) return;
        if (SeatingManager.Instance.assignments == null) return;
        if (contentParent == null || slotButtonPrefab == null) return;

        if (!contentParent.gameObject.activeSelf)
            contentParent.gameObject.SetActive(true);

        foreach (Transform c in contentParent) Destroy(c.gameObject);

        var assignments = SeatingManager.Instance.assignments;
        for (int i = 0; i < assignments.Count; i++)
        {
            var ass = assignments[i];
            var btn = Instantiate(slotButtonPrefab, contentParent);
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null) txt.text = $"Slot {i} — {(ass.occupied ? "occupied" : "free")}";
            int idx = i;
            btn.onClick.AddListener(() => OnSlotClicked(idx));
        }
    }

    private void OnSlotClicked(int idx)
    {
        if (SeatingManager.Instance == null) return;
        var door = Object.FindFirstObjectByType<DoorController>();
        if (door == null)
        {
            Debug.LogWarning("[SeatingUI] No DoorController found in scene.");
            return;
        }
        if (door.CurrentVisitorActor == null)
        {
            Debug.LogWarning("[SeatingUI] No current visitor actor to seat.");
            return;
        }

        bool ok = SeatingManager.Instance.TrySeatVisitorAt(idx, door.CurrentVisitorActor.data?.visitorId);
        if (!ok)
        {
            Debug.Log("[SeatingUI] Seat not available.");
            return;
        }

        var slot = SeatingManager.Instance.GetSlotByIndex(idx);
        if (slot != null)
        {
            Transform approach = slot.approachPoint != null ? slot.approachPoint : slot.transform;
            Transform seatPoint = slot.seatPoint != null ? slot.seatPoint : slot.transform;
            Transform lookAt = slot.seatLookAt != null ? slot.seatLookAt : slot.transform;

            Debug.Log($"[SeatingUI] Seating visitor -> approach {approach.position}, seat {seatPoint.position}");

            // Двигаем актёра и оставляем его в сцене
            door.CurrentVisitorActor.MoveToSeatAndSit(approach, seatPoint, lookAt, () =>
            {
                Debug.Log($"[SeatingUI] Visitor seated at slot {idx}");
                // После посадки: закрываем дверь и вернуть камеру игроку
                door.CloseDoorAfterInteraction(removeActor: false);
                CameraController.Instance?.ReturnToFollow(0.25f);
            });
        }
        else
        {
            Debug.LogWarning("[SeatingUI] Slot missing for index " + idx);
        }

        SetPanelsActive(false);
    }

    public void CancelSelection()
    {
        var door = Object.FindFirstObjectByType<DoorController>();
        if (door == null)
        {
            SetPanelsActive(false);
            return;
        }

        // Cancel treated as reject
        door.RejectAndClose();
        SetPanelsActive(false);
    }

    private void SetPanelsActive(bool active)
    {
        if (rootPanel != null) rootPanel.SetActive(active);
        if (contentParent != null) contentParent.gameObject.SetActive(active);
    }
}