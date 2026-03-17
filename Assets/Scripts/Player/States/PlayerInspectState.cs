// PlayerInspectState.cs
using UnityEngine;
using System.Collections;

public class PlayerInspectState : PlayerState
{
    private Transform cameraPoint;
    private float transitionDuration = 0.6f;
    private PlayerMotor motor;
    private PlayerInputReader input;
    private bool canExit = false;

    public PlayerInspectState(PlayerStateMachine sm, Transform cameraPoint, float duration = 0.6f) : base(sm)
    {
        this.cameraPoint = cameraPoint;
        this.transitionDuration = duration;
        motor = sm.GetComponent<PlayerMotor>();
        input = sm.Input;
    }

    public override void Enter()
    {
        // блокируем движение — просто не вызываем motor.Move в этом состоянии
        // запусти переход камеры и дождёмся его окончания
        CameraController.Instance.MoveToPoint(cameraPoint, transitionDuration);

        // пока камера едет — нельзя выйти
        canExit = false;

        // подписка на нажатие, но в обработчике проверяем canExit
        input.InteractPressed += OnExitInspect;

        // Запускаем корутину ожидания (stateMachine выполняет корутины)
        stateMachine.StartCoroutine(WaitForCameraArrive());

        // показать курсор (если это необходимо)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private IEnumerator WaitForCameraArrive()
    {
        // Ждем: либо пока идёт transition, либо пока lockedToPoint не установится
        // Ждём окончания transition и подтверждения блокировки
        while (CameraController.Instance == null || CameraController.Instance.IsTransitioning)
            yield return null;

        // Некоторое дополнительное небольшое ожидание — чтобы избежать "отскока" на очень коротких переходах
        yield return null;

        // Убедимся, что камера действительно заблокирована в точке
        if (CameraController.Instance.IsLockedToPoint)
            canExit = true;
        else
            canExit = true; // fallback — позволим выйти, но обычно не сработает
    }

    private void OnExitInspect()
    {
        if (!canExit) return;

        input.InteractPressed -= OnExitInspect;

        // Возвращаем камеру к следованию и выходим
        CameraController.Instance.ReturnToFollow(0.5f);

        stateMachine.PopState();
    }

    public override void Exit()
    {
        // при выходе — прятать курсор (MoveState снова покажет/скроет, если нужно)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void Update()
    {
        // игрока не двигаем
    }
}