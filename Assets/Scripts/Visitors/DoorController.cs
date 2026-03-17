// Put at Assets/Scripts/Door/DoorController.cs
using UnityEngine;
using System;
using System.Collections;

public class DoorController : MonoBehaviour
{
    public enum State { ClosedIdle, Interacting, Ajar, FullyOpen }

    [Header("Camera points")]
    public Transform puzzleCameraPoint;
    public Transform peekCameraPoint;
    public Transform candleCameraPoint;

    [Header("Visitor")]
    public GameObject silhouetteRoot;
    public GameObject visitorActorPrefab;

    [Header("Door visuals")]
    public Transform doorVisual;
    public float closedAngle = 0f;
    public float peekAngle = 15f;
    public float openAngle = 75f;
    public float doorAnimDuration = 0.25f;

    [Header("Settings")]
    [SerializeField] private bool chainLocked = true;

    private VisitorData currentVisitorData;
    private GameObject currentSilhouetteInstance;
    [NonSerialized] public VisitorActor CurrentVisitorActor;
    private Coroutine doorAnimCoroutine;
    private State state = State.ClosedIdle;

    public event Action<VisitorData> OnVisitorPeeked;
    public event Action OnDoorOpened;
    public event Action OnDoorClosed;
    public event Action<State> OnStateChanged;

    void Start()
    {
        Debug.Log("DoorController Start");
        if (VisitorManager.Instance == null) return;

        VisitorData pending;
        if (VisitorManager.Instance.TryGetCurrentPending(out pending) && pending != null)
        {
            ReceiveVisitor(pending);
        }
    }
    private void OnEnable()
    {
        if (VisitorManager.Instance != null)
        {
            VisitorManager.Instance.OnVisitorArrived += HandleVisitorArrived;
            VisitorData pending;
            if (VisitorManager.Instance.TryGetCurrentPending(out pending) && pending != null)
                ReceiveVisitor(pending);
        }
    }

    private void OnDisable()
    {
        if (VisitorManager.Instance != null)
            VisitorManager.Instance.OnVisitorArrived -= HandleVisitorArrived;
    }

    private void HandleVisitorArrived(VisitorData data) => ReceiveVisitor(data);

    public void ReceiveVisitor(VisitorData data)
    {
        if (data == null) return;
        currentVisitorData = data;
        SpawnSilhouette();
        SetState(State.ClosedIdle);
        Debug.Log($"[Door] Received visitor {data.displayName}");
    }

    public bool HasPendingOrActor() => currentVisitorData != null || CurrentVisitorActor != null;

    public void TogglePeek()
    {
        if (!HasPendingOrActor()) { Debug.Log("[Door] Nothing to peek at"); return; }

        if (state == State.Ajar)
        {
            // close peek -> Interacting (transient)
            AnimateDoorTo(closedAngle, onComplete: () =>
            {
                SetState(State.Interacting);
                if (currentSilhouetteInstance != null) currentSilhouetteInstance.SetActive(false);
                // after closing, go to ClosedIdle
                SetState(State.ClosedIdle);
                OnDoorClosed?.Invoke();
            });
        }
        else
        {
            // open to ajar
            AnimateDoorTo(peekAngle, onComplete: () =>
            {
                SetState(State.Ajar);
                if (currentSilhouetteInstance == null && currentVisitorData != null) SpawnSilhouette();
                if (currentSilhouetteInstance != null) currentSilhouetteInstance.SetActive(true);
                OnVisitorPeeked?.Invoke(currentVisitorData);
                if (!string.IsNullOrEmpty(currentVisitorData?.greetingLine))
                    Debug.Log($"[Visitor] {currentVisitorData.displayName}: \"{currentVisitorData.greetingLine}\"");
            });
        }
    }

    public void ToggleChain()
    {
        if (state != State.ClosedIdle) { Debug.Log("[Door] ToggleChain only in ClosedIdle"); return; }
        chainLocked = !chainLocked;
        Debug.Log($"[Door] chainLocked = {chainLocked}");
        OnStateChanged?.Invoke(state);
    }

    public void TryOpenDoor()
    {
        if (chainLocked) { Debug.Log("[Door] Chain locked"); return; }
        if (currentVisitorData == null && CurrentVisitorActor == null) { Debug.Log("[Door] No visitor to open for"); return; }
        AnimateDoorTo(openAngle, onComplete: () =>
        {
            SetState(State.FullyOpen);
            if (currentSilhouetteInstance != null) currentSilhouetteInstance.SetActive(false);
            SpawnVisitorActorClose();
            OnDoorOpened?.Invoke();
        });
    }

    public void RejectAndClose()
    {
        Debug.Log("[Door] Visitor rejected");
        if (CurrentVisitorActor != null)
        {
            CurrentVisitorActor.LeaveAndDestroy(0.2f);
            CurrentVisitorActor = null;
        }
        if (currentSilhouetteInstance) Destroy(currentSilhouetteInstance);
        currentVisitorData = null;

        AnimateDoorTo(closedAngle, onComplete: () =>
        {
            SetState(State.ClosedIdle);
            chainLocked = true; // reset chain after reject
            OnDoorClosed?.Invoke();
        });
    }

    public bool ActivateTrap(TrapController trap)
    {
        if (trap == null) return false;
        if (state != State.ClosedIdle) { Debug.Log("[Trap] Can only activate when closed"); return false; }
        var data = CurrentVisitorActor != null ? CurrentVisitorActor.data : currentVisitorData;
        if (data == null) { Debug.Log("[Trap] No visitor"); return false; }
        if (data.canFly) { Debug.Log("[Trap] Visitor can fly, trap failed"); return false; }
        trap.ActivateForVisitor(data);
        if (CurrentVisitorActor != null) CurrentVisitorActor.LeaveAndDestroy(0.2f);
        if (currentSilhouetteInstance) Destroy(currentSilhouetteInstance);
        currentVisitorData = null;
        CurrentVisitorActor = null;

        // door resets
        SetState(State.ClosedIdle);
        chainLocked = true;
        OnDoorClosed?.Invoke();
        return true;
    }

    public Color? InspectWithDagger()
    {
        if (state != State.Ajar) { Debug.Log("[Door] Dagger only when ajar"); return null; }
        var data = CurrentVisitorActor != null ? CurrentVisitorActor.data : currentVisitorData;
        if (data == null) return null;
        Debug.Log($"[Dagger] {data.displayName} bloodColor = {data.bloodColor}");
        return data.bloodColor;
    }

    public void CloseDoorAfterInteraction(bool removeActor = false)
    {
        AnimateDoorTo(closedAngle, onComplete: () =>
        {
            SetState(State.ClosedIdle);
            chainLocked = true;
            OnDoorClosed?.Invoke();
        });

        if (removeActor && CurrentVisitorActor != null)
        {
            CurrentVisitorActor.LeaveAndDestroy(0.2f);
            CurrentVisitorActor = null;
        }

        currentVisitorData = null;
    }

    private void SpawnSilhouette()
    {
        if (currentSilhouetteInstance != null) Destroy(currentSilhouetteInstance);
        if (currentVisitorData?.silhouettePrefab == null) return;
        currentSilhouetteInstance = Instantiate(currentVisitorData.silhouettePrefab, silhouetteRoot != null ? silhouetteRoot.transform : this.transform);
        currentSilhouetteInstance.transform.localPosition = Vector3.zero;
        currentSilhouetteInstance.transform.localRotation = Quaternion.identity;
        currentSilhouetteInstance.SetActive(false);
    }

    private void SpawnVisitorActorClose()
    {
        if (visitorActorPrefab == null) { Debug.LogError("[Door] visitorActorPrefab not set"); return; }
        Vector3 spawnPos = transform.position + transform.forward * 0.9f;
        var go = Instantiate(visitorActorPrefab, spawnPos, Quaternion.identity);
        var actor = go.GetComponent<VisitorActor>() ?? go.AddComponent<VisitorActor>();
        actor.Initialize(currentVisitorData);
        CurrentVisitorActor = actor;

        VisitorManager.Instance?.DequeueCurrent();
        currentVisitorData = null;
        Debug.Log("[Door] Spawned actor");
    }

    private void SetState(State s)
    {
        if (state == s) return;
        state = s;
        OnStateChanged?.Invoke(state);
    }

    private void AnimateDoorTo(float targetAngle, Action onComplete = null)
    {
        if (doorAnimCoroutine != null) StopCoroutine(doorAnimCoroutine);
        doorAnimCoroutine = StartCoroutine(DoDoorAnim(targetAngle, onComplete));
    }

    private IEnumerator DoDoorAnim(float targetAngle, Action onComplete)
    {
        if (doorVisual == null) { onComplete?.Invoke(); yield break; }
        float elapsed = 0f;
        float from = doorVisual.localEulerAngles.y;
        if (from > 180f) from -= 360f;
        float to = targetAngle;
        float dur = Mathf.Max(0.0001f, doorAnimDuration);
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            float ang = Mathf.Lerp(from, to, t);
            var e = doorVisual.localEulerAngles;
            doorVisual.localEulerAngles = new Vector3(e.x, ang, e.z);
            yield return null;
        }
        var ee = doorVisual.localEulerAngles;
        doorVisual.localEulerAngles = new Vector3(ee.x, targetAngle, ee.z);
        doorAnimCoroutine = null;
        onComplete?.Invoke();
    }

    public State CurrentState => state;
    public bool IsChainLocked => chainLocked;
    public VisitorData GetCurrentVisitorData() => currentVisitorData;
    
}