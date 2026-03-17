using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class DragSystem : MonoBehaviour
{
    [Header("Raycast")]
    public LayerMask draggableLayer = ~0;
    public float maxHoverDistance = 4f;

    [Header("Dragging")]
    public float dragLerpSpeed = 12f;
    public float minDragDistance = 0.8f;
    public float maxDragDistance = 3.5f;
    public float objectRadiusPadding = 0.05f;

    [Header("Slowdown while dragging")]
    [Range(0.01f, 1f)] public float moveSpeedMultiplier = 0.5f;
    [Range(0.01f, 1f)] public float mouseSensitivityMultiplier = 0.6f;

    // internal refs
    PlayerInputReader inputReader;
    Camera cam;

    IDraggable hoveredDraggable = null;
    IHoverable hoveredHoverable = null;
    DraggableObject currentDragged = null;

    float currentDragDistance = 2f;
    float lastHoverHitDistance = 0f;

    PlayerMotor playerMotor;
    float originalMoveSpeed;
    bool originalMoveSpeedCaptured = false;

    CameraController cameraController;
    float originalMouseSensitivity;
    bool originalMouseSensitivityCaptured = false;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        cam = Camera.main;
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.DragPressed += OnDragPressed;
            inputReader.DragReleased += OnDragReleased;
        }

        playerMotor = GetComponent<PlayerMotor>();
        cameraController = CameraController.Instance;
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.DragPressed -= OnDragPressed;
            inputReader.DragReleased -= OnDragReleased;
        }
    }

    private void Update()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        if (currentDragged == null) HoverCheck();
        else UpdateDragging();
    }

    // --- hover detection ---
    private void HoverCheck()
    {
        if (cam == null) { ClearHover(); return; }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxHoverDistance, draggableLayer, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) { ClearHover(); return; }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        IDraggable foundDr = null;
        IHoverable foundHv = null;
        float foundDist = 0f;

        foreach (var h in hits)
        {
            if (h.collider == null) continue;
            var component = h.collider.GetComponentInParent<IDraggable>();
            if (component != null)
            {
                foundDr = component;
                foundHv = h.collider.GetComponentInParent<IHoverable>();
                foundDist = h.distance;
                break;
            }
        }

        if (foundDr != null)
        {
            lastHoverHitDistance = foundDist;
            if (hoveredDraggable == null || !ReferenceEquals(hoveredDraggable, foundDr))
            {
                ClearHover();
                hoveredDraggable = foundDr;
                hoveredHoverable = foundHv;
                try { hoveredHoverable?.SetHover(true); } catch { }
            }
        }
        else ClearHover();
    }

    private void ClearHover()
    {
        if (hoveredHoverable != null)
        {
            try { hoveredHoverable.SetHover(false); } catch { }
            hoveredHoverable = null;
        }
        hoveredDraggable = null;
    }

    // --- input handlers ---
    private void OnDragPressed()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[DragSystem] Camera.main is null on DragPressed."); return; }

        IDraggable drInterface = hoveredDraggable;
        if (drInterface == null)
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, maxHoverDistance, draggableLayer, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (h.collider == null) continue;
                    var d = h.collider.GetComponentInParent<IDraggable>();
                    if (d != null) { drInterface = d; lastHoverHitDistance = h.distance; break; }
                }
            }
        }

        if (drInterface == null) return;

        // get DraggableObject
        DraggableObject drObj = drInterface as DraggableObject;
        if (drObj == null)
        {
            var comp = drInterface as Component;
            if (comp != null) drObj = comp.GetComponent<DraggableObject>();
            if (drObj == null) { Debug.LogWarning("[DragSystem] Found IDraggable but couldn't get DraggableObject component."); return; }
        }

        // seat occupied check
        var seatComp = drObj.GetComponent<Seat>();
        if (seatComp != null && seatComp.IsOccupied) { Debug.Log("[DragSystem] Seat is occupied — cannot start drag."); return; }

        // remove from bowl/slot if placed there
        if (drObj.IsPlaced && drObj.PlacedInBowl != null)
        {
            try { drObj.PlacedInBowl.RemoveItem(drObj); }
            catch (System.Exception ex) { Debug.LogWarning($"[DragSystem] Exception removing from bowl: {ex.Message}"); }
        }

        // assign drag
        currentDragged = drObj;
        currentDragDistance = Mathf.Clamp(lastHoverHitDistance > 0 ? lastHoverHitDistance : (minDragDistance + maxDragDistance) * 0.5f, minDragDistance, maxDragDistance);

        try { currentDragged.StartDrag(); }
        catch (System.Exception ex) { Debug.LogWarning($"[DragSystem] Exception in StartDrag: {ex.Message}"); currentDragged = null; return; }

        // capture and modify player/camera sensitivity
        if (playerMotor == null) playerMotor = GetComponent<PlayerMotor>();
        if (playerMotor != null && !originalMoveSpeedCaptured) { originalMoveSpeed = playerMotor.moveSpeed; originalMoveSpeedCaptured = true; }
        if (playerMotor != null) playerMotor.moveSpeed = originalMoveSpeed * moveSpeedMultiplier;

        if (cameraController == null) cameraController = CameraController.Instance;
        if (cameraController != null && !originalMouseSensitivityCaptured) { originalMouseSensitivity = cameraController.mouseSensitivity; originalMouseSensitivityCaptured = true; }
        if (cameraController != null) cameraController.mouseSensitivity = Mathf.Max(0.01f, (originalMouseSensitivityCaptured ? originalMouseSensitivity : cameraController.mouseSensitivity) * mouseSensitivityMultiplier);

        ClearHover();
    }

    private void OnDragReleased()
    {
        if (currentDragged == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            // fallback: stop drag and clear
            TryStopAndApplyVelocity(currentDragged, Vector3.zero);
            currentDragged = null;
            RestorePlayerAndCameraControls();
            ClearHover();
            return;
        }

        Vector3 camPos = cam.transform.position;
        Vector3 camFwd = cam.transform.forward;

        float radius = ComputeObjectRadius(currentDragged);
        float desiredDist = currentDragDistance;

        if (Physics.SphereCast(camPos, radius + objectRadiusPadding, camFwd, out RaycastHit hit, maxDragDistance + 1f, ~0, QueryTriggerInteraction.Ignore))
            desiredDist = Mathf.Clamp(hit.distance - radius - objectRadiusPadding, minDragDistance, maxDragDistance);

        Vector3 releasePoint = camPos + camFwd * desiredDist;

        // find nearest receiver: priority SeatSlot -> BowlArea -> CardSlot -> SpellPedestal -> BookDropArea
        Collider[] overlaps = Physics.OverlapSphere(releasePoint, 1.2f);
        SeatSlot bestSeat = null;
        BowlArea bestBowl = null;
        CardSlot bestCardSlot = null;
        SpellPedestal bestPedestal = null;
        BookDropArea bestBook = null;
        float bestDist = float.MaxValue;

        if (overlaps != null && overlaps.Length > 0)
        {
            foreach (var c in overlaps)
            {
                if (c == null) continue;

                var seatSlot = c.GetComponentInParent<SeatSlot>();
                if (seatSlot != null)
                {
                    float d = Vector3.Distance(releasePoint, seatSlot.transform.position);
                    if (d < bestDist) { bestDist = d; bestSeat = seatSlot; bestBowl = null; bestCardSlot = null; bestPedestal = null; bestBook = null; }
                    continue;
                }

                var bowl = c.GetComponentInParent<BowlArea>();
                if (bowl != null)
                {
                    float d = Vector3.Distance(releasePoint, bowl.transform.position);
                    if (d < bestDist) { bestDist = d; bestBowl = bowl; bestSeat = null; bestCardSlot = null; bestPedestal = null; bestBook = null; }
                    continue;
                }

                var cardSlot = c.GetComponentInParent<CardSlot>();
                if (cardSlot != null)
                {
                    float d = Vector3.Distance(releasePoint, cardSlot.transform.position);
                    if (d < bestDist) { bestDist = d; bestCardSlot = cardSlot; bestSeat = null; bestBowl = null; bestPedestal = null; bestBook = null; }
                    continue;
                }

                var pedestal = c.GetComponentInParent<SpellPedestal>();
                if (pedestal != null)
                {
                    float d = Vector3.Distance(releasePoint, pedestal.transform.position);
                    if (d < bestDist) { bestDist = d; bestPedestal = pedestal; bestSeat = null; bestBowl = null; bestCardSlot = null; bestBook = null; }
                    continue;
                }

                var book = c.GetComponentInParent<BookDropArea>();
                if (book != null)
                {
                    float d = Vector3.Distance(releasePoint, book.transform.position);
                    if (d < bestDist) { bestDist = d; bestBook = book; bestSeat = null; bestBowl = null; bestCardSlot = null; bestPedestal = null; }
                    continue;
                }
            }
        }

        bool placed = false;

        // priority: seat
        if (bestSeat != null)
        {
            var seatComp = currentDragged.GetComponent<Seat>();
            if (seatComp != null)
            {
                placed = bestSeat.PlaceSeat(seatComp);
            }
        }

        // bowl
        if (!placed && bestBowl != null)
            placed = bestBowl.PlaceItem(currentDragged);

        // card slot
        if (!placed && bestCardSlot != null)
        {
            var cardComp = currentDragged.GetComponent<CardItem>();
            if (cardComp != null)
                placed = bestCardSlot.PlaceCard(cardComp);
        }

        // pedestal
        if (!placed && bestPedestal != null)
        {
            var cardComp = currentDragged.GetComponent<CardItem>();
            if (cardComp != null)
            {
                int idx = bestPedestal.TryPlaceCardIntoFirstAvailable(cardComp);
                placed = (idx >= 0);
            }
        }

        // book (recipe page)
        if (!placed && bestBook != null)
        {
            var rp = currentDragged.GetComponent<RecipePage>();
            if (rp != null)
            {
                bool accepted = bestBook.AcceptPage(currentDragged.gameObject);
                if (accepted)
                {
                    // page handled by book; page may be destroyed -> clear state
                    currentDragged = null;
                    RestorePlayerAndCameraControls();
                    ClearHover();
                    return;
                }
            }
        }

        // not placed: release / throw
        if (!placed)
        {
            Vector3 throwVel = camFwd * 1.2f;
            TryStopAndApplyVelocity(currentDragged, throwVel);
        }
        else
        {
            // set dragging flag off if exists
            try { currentDragged.IsDragging = false; } catch { }
        }

        RestorePlayerAndCameraControls();
        currentDragged = null;
        ClearHover();
    }

    // helper: apply release fallback (stop drag and apply velocity to rigidbody)
    private void TryStopAndApplyVelocity(DraggableObject obj, Vector3 vel)
    {
        if (obj == null) return;
        try { obj.StopDrag(); } catch { }
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = vel;
#else
            rb.velocity = vel;
#endif
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }

    // helper: restore player movement and camera sensitivity
    private void RestorePlayerAndCameraControls()
    {
        if (playerMotor != null && originalMoveSpeedCaptured) playerMotor.moveSpeed = originalMoveSpeed;
        if (cameraController != null && originalMouseSensitivityCaptured) cameraController.mouseSensitivity = originalMouseSensitivity;
        else if (cameraController != null && !originalMouseSensitivityCaptured) cameraController.mouseSensitivity = Mathf.Max(0.01f, cameraController.mouseSensitivity / mouseSensitivityMultiplier);
    }

    // update dragged object position smoothly in front of camera
    private void UpdateDragging()
    {
        if (currentDragged == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float radius = ComputeObjectRadius(currentDragged);
        Vector3 camPos = cam.transform.position;
        Vector3 camFwd = cam.transform.forward;

        float targetDist;
        if (Physics.SphereCast(camPos, radius + objectRadiusPadding, camFwd, out RaycastHit hit, maxDragDistance + 1f, ~0, QueryTriggerInteraction.Ignore))
            targetDist = Mathf.Clamp(hit.distance - radius - objectRadiusPadding, minDragDistance, maxDragDistance);
        else
            targetDist = Mathf.Clamp(currentDragDistance, minDragDistance, maxDragDistance);

        currentDragDistance = Mathf.Lerp(currentDragDistance, targetDist, Time.deltaTime * 8f);
        Vector3 targetPos = camPos + camFwd * currentDragDistance;
        currentDragged.transform.position = Vector3.Lerp(currentDragged.transform.position, targetPos, Time.deltaTime * dragLerpSpeed);
    }

    // compute approximate object radius for placement math
    private float ComputeObjectRadius(DraggableObject obj)
    {
        if (obj == null) return 0.3f;
        Collider[] cols = obj.GetComponentsInChildren<Collider>(true);
        if (cols != null && cols.Length > 0)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) if (cols[i] != null) b.Encapsulate(cols[i].bounds);
            return Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
        }

        Renderer[] rends = obj.GetComponentsInChildren<Renderer>(true);
        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) if (rends[i] != null) b.Encapsulate(rends[i].bounds);
            return Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
        }

        return 0.3f;
    }
}