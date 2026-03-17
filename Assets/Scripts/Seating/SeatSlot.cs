using UnityEngine;

[DisallowMultipleComponent]
public class SeatSlot : MonoBehaviour, IInteractable
{
    [Header("Slot")]
    public Transform slotTransform; // where seat snaps; if null, will fallback to this.transform
    public int slotIndex = 0; // optional
    public bool isFrontRow = true;
    public string slotId;

    [Header("Snap")]
    public float snapSmooth = 20f;

    [Header("Approach / Sit points")]
    public Transform approachPoint; // куда идти сначала
    public Transform seatPoint;     // точка сиденья (сидящий визуально)
    public Transform seatLookAt;    // куда смотреть, когда сел

    [Header("Eject settings (when pressing E)")]
    public float ejectDistance = 0.6f;
    public float ejectUp = 0.35f;
    public float ejectSpeed = 3f;
    public bool ejectTowardsPlayer = true;

    public Seat placedSeat { get; private set; } = null;

    private void OnValidate()
    {
        if (slotTransform == null) slotTransform = this.transform;
    }

    private void Awake()
    {
        if (slotTransform == null) slotTransform = this.transform;
    }

    private void Update()
    {
        if (slotTransform == null) slotTransform = this.transform;

        if (placedSeat != null)
        {
            var t = placedSeat.transform;
            t.position = Vector3.Lerp(t.position, slotTransform.position, Time.deltaTime * snapSmooth);
            t.rotation = Quaternion.Slerp(t.rotation, slotTransform.rotation, Time.deltaTime * snapSmooth);
        }
    }

    // Try to place a Seat into this slot. Returns true if success.
    public bool PlaceSeat(Seat seat)
    {
        if (seat == null) return false;
        if (placedSeat != null) return false;
        if (seat.IsOccupied) return false;

        if (slotTransform == null) slotTransform = this.transform;

        var dr = seat.GetComponent<DraggableObject>();
        if (dr != null) dr.IsDragging = false;

        if (dr != null)
        {
            dr.PlaceInSlot(slotTransform, null);
        }
        else
        {
            seat.transform.SetParent(null, true);
            seat.transform.position = slotTransform.position;
            seat.transform.rotation = slotTransform.rotation;
        }

        placedSeat = seat;
        seat.PlaceInSlot(this);
        SeatingManager.Instance?.NotifySlotUpdated(this);

        Debug.Log($"[SeatSlot] Placed seat {(seat.data != null ? seat.data.displayName : seat.name)} into slot {slotId}");
        return true;
    }

    // Remove seat
    public bool RemoveSeat()
    {
        if (placedSeat == null) return false;
        if (placedSeat.IsOccupied) return false;

        var s = placedSeat;
        placedSeat = null;

        var dr = s.GetComponent<DraggableObject>();
        if (dr != null) dr.RemoveFromSlot();
        else
        {
            s.RemoveFromSlot();
            s.transform.SetParent(null, true);
        }

        Vector3 ejectDir = slotTransform != null ? slotTransform.right : transform.right;
        Vector3 worldPos = (slotTransform != null ? slotTransform.position : transform.position) + ejectDir * ejectDistance + Vector3.up * 0.15f;
        s.transform.position = worldPos;
        s.transform.rotation = slotTransform != null ? slotTransform.rotation : transform.rotation;

        Rigidbody rb = s.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
            Vector3 impulse = (ejectDir.normalized * ejectSpeed) + (Vector3.up * ejectUp * ejectSpeed);
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = impulse;
#else
            rb.velocity = impulse;
#endif
        }

        SeatingManager.Instance?.NotifySlotUpdated(this);
        Debug.Log($"[SeatSlot] Removed seat from slot {slotId}");
        return true;
    }

    public void Interact(PlayerStateMachine player)
    {
        if (placedSeat == null)
        {
            Debug.Log($"SeatSlot {slotId} empty.");
            return;
        }

        if (placedSeat.IsOccupied)
        {
            Debug.Log($"SeatSlot {slotId} is occupied; cannot remove.");
            return;
        }

        bool removed = RemoveSeat();
        if (removed) Debug.Log($"Seat removed from slot {slotId}");
    }
}