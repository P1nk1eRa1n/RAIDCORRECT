using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DoorUI : MonoBehaviour
{
    [Header("References")]
    public DoorController doorController;
    public GameObject rootPanel; // core panel (shows basic buttons)
    public Button btnPeek;
    public Button btnCandle;
    public Button btnToggleChain;
    public Button btnOpen;
    public Button btnReject;
    public Button btnTrap;
    public Button btnDagger;

    [Header("Seating UI (popup after open)")]
    public GameObject seatingPanel; // root panel for seating

    [Header("Display")]
    public TextMeshProUGUI visitorNameText;
    public TextMeshProUGUI visitorRequestText;
    public Image bloodColorSwatch;

    private void Awake()
    {
        // —крываем панели по-умолчанию Ч корректно.
        if (rootPanel != null) rootPanel.SetActive(false);
        if (seatingPanel != null) seatingPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (doorController != null)
        {
            doorController.OnVisitorPeeked += HandlePeek;
            doorController.OnDoorOpened += HandleOpen;
            doorController.OnDoorClosed += HandleClose;
            doorController.OnStateChanged += HandleStateChanged;
        }

        if (btnPeek) btnPeek.onClick.AddListener(OnPeek);
        if (btnCandle) btnCandle.onClick.AddListener(OnCandle);
        if (btnToggleChain) btnToggleChain.onClick.AddListener(OnToggleChain);
        if (btnOpen) btnOpen.onClick.AddListener(OnOpen);
        if (btnReject) btnReject.onClick.AddListener(OnReject);
        if (btnTrap) btnTrap.onClick.AddListener(OnTrap);
        if (btnDagger) btnDagger.onClick.AddListener(OnDagger);

        // ¬ј∆Ќќ: синхронизируем UI с текущим состо€нием двери Ч чтобы не пропустить событи€, пришедшие до активации Canvas
        SyncWithDoorController();
    }

    private void OnDisable()
    {
        if (doorController != null)
        {
            doorController.OnVisitorPeeked -= HandlePeek;
            doorController.OnDoorOpened -= HandleOpen;
            doorController.OnDoorClosed -= HandleClose;
            doorController.OnStateChanged -= HandleStateChanged;
        }

        if (btnPeek) btnPeek.onClick.RemoveListener(OnPeek);
        if (btnCandle) btnCandle.onClick.RemoveListener(OnCandle);
        if (btnToggleChain) btnToggleChain.onClick.RemoveListener(OnToggleChain);
        if (btnOpen) btnOpen.onClick.RemoveListener(OnOpen);
        if (btnReject) btnReject.onClick.RemoveListener(OnReject);
        if (btnTrap) btnTrap.onClick.RemoveListener(OnTrap);
        if (btnDagger) btnDagger.onClick.RemoveListener(OnDagger);
    }

    private void HandleStateChanged(DoorController.State s)
    {
        if (btnToggleChain != null) btnToggleChain.interactable = (s == DoorController.State.ClosedIdle);
        if (btnDagger != null) btnDagger.interactable = (s == DoorController.State.Ajar);
        if (btnTrap != null) btnTrap.interactable = (s == DoorController.State.ClosedIdle);
        if (btnPeek != null) btnPeek.interactable = (s != DoorController.State.FullyOpen);
        if (btnOpen != null) btnOpen.interactable = (s != DoorController.State.FullyOpen && doorController != null && !doorController.IsChainLocked && doorController.GetCurrentVisitorData() != null);
    }

    private void HandlePeek(VisitorData data)
    {
        if (rootPanel != null) rootPanel.SetActive(true);
        if (visitorNameText != null) visitorNameText.text = data != null ? data.displayName : "Ч";
        if (visitorRequestText != null) visitorRequestText.text = data != null ? data.requestLines : "";
        if (bloodColorSwatch != null) bloodColorSwatch.color = Color.clear;
    }

    private void HandleOpen()
    {
        if (seatingPanel != null) seatingPanel.SetActive(true);
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void HandleClose()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (seatingPanel != null) seatingPanel.SetActive(false);
        CameraController.Instance?.ReturnToFollow(0.25f);
    }

    // Buttons
    private void OnPeek() => doorController?.TogglePeek();
    private void OnCandle()
    {
        if (doorController?.candleCameraPoint != null)
            CameraController.Instance?.MoveToPoint(doorController.candleCameraPoint, 0.25f);
    }
    private void OnToggleChain() => doorController?.ToggleChain();
    private void OnOpen() => doorController?.TryOpenDoor();
    private void OnReject() => doorController?.RejectAndClose();
    private void OnTrap()
    {
        var trap = Object.FindFirstObjectByType<TrapController>();
        if (trap != null) doorController?.ActivateTrap(trap);
    }
    private void OnDagger()
    {
        var color = doorController?.InspectWithDagger();
        if (bloodColorSwatch != null)
        {
            if (color.HasValue)
            {
                bloodColorSwatch.color = color.Value;
            }
            else
            {
                bloodColorSwatch.color = Color.clear;
            }
        }
    }

    // --- utility: синхронизаци€ при включении ---
    private void SyncWithDoorController()
    {
        if (doorController == null) return;

        // ќбновл€ем кнопки по текущему состо€нию
        HandleStateChanged(doorController.CurrentState);

        // ≈сли дверь сейчас приоткрыта (Ajar) Ч попытатьс€ показать панель и инфу о посетителе.
        if (doorController.CurrentState == DoorController.State.Ajar)
        {
            // GetCurrentVisitorData вернет pending data (если есть)
            var data = doorController.GetCurrentVisitorData();
            // если актЄр уже запущен, можно попытатьс€ вз€ть его данные (если поле доступно)
            if (data == null && doorController.CurrentVisitorActor != null)
                data = doorController.CurrentVisitorActor.data;
            if (data != null) HandlePeek(data);
        }

        // ≈сли дверь полностью открыта Ч убедитьс€, что seatingPanel показан
        if (doorController.CurrentState == DoorController.State.FullyOpen)
            HandleOpen();
    }

    // ѕубличный метод Ч можно вызвать извне, если хочешь форсированно обновить UI
    public void Refresh() => SyncWithDoorController();
}