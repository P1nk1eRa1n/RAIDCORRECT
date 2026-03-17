using UnityEngine;
using System;
using System.Collections;

public class PlayerConciergeState : PlayerState
{
    private Transform cameraPoint;
    private float transitionDuration;
    private DoorController doorController;
    private PlayerInputReader input;
    private Action onOpenedUI;
    private Action onClosedUI;
    private bool canExit = false;

    public PlayerConciergeState(PlayerStateMachine sm, Transform cameraPoint, DoorController doorController, float duration, Action opened, Action closed) : base(sm)
    {
        this.cameraPoint = cameraPoint;
        this.transitionDuration = duration;
        this.doorController = doorController;
        this.input = sm.Input;
        this.onOpenedUI = opened;
        this.onClosedUI = closed;
    }

    public override void Enter()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        input.InteractPressed += TryExit;

        if (doorController != null)
            doorController.OnDoorClosed += OnDoorClosedFromController;

        // move camera to given point; allowLook = false normally
        if (CameraController.Instance != null && cameraPoint != null)
            CameraController.Instance.MoveToPoint(cameraPoint, transitionDuration, allowLook: false);

        stateMachine.StartCoroutine(WaitForCameraAndOpenUI());
    }

    private IEnumerator WaitForCameraAndOpenUI()
    {
        // ∆дЄм конца перехода; если Instance отсутствует Ч сразу открываем UI
        while (CameraController.Instance != null && CameraController.Instance.IsTransitioning)
            yield return null;

        // один кадр на стабилизацию
        yield return null;

        // включаем UI (ConciergeObject передал callable)
        onOpenedUI?.Invoke();

        canExit = true;
    }

    private void TryExit()
    {
        if (!canExit) return;

        onClosedUI?.Invoke();

        try { input.InteractPressed -= TryExit; } catch { }

        CameraController.Instance?.ReturnToFollow(0.25f);
        stateMachine.PopState();
    }

    private void OnDoorClosedFromController()
    {
        // закрыли дверь извне Ч закрыть UI и выйти
        onClosedUI?.Invoke();

        try { input.InteractPressed -= TryExit; } catch { }
        if (doorController != null) doorController.OnDoorClosed -= OnDoorClosedFromController;

        CameraController.Instance?.ReturnToFollow(0.25f);
        stateMachine.PopState();
    }

    public override void Exit()
    {
        try { input.InteractPressed -= TryExit; } catch { }
        if (doorController != null) doorController.OnDoorClosed -= OnDoorClosedFromController;
        onClosedUI?.Invoke();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}