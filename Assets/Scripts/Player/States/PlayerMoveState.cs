using UnityEngine;

public class PlayerMoveState : PlayerState
{
    private PlayerMotor motor;
    private CameraController camController;
    private PlayerInputReader input;

    public PlayerMoveState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
        motor = stateMachine.GetComponent<PlayerMotor>();
        camController = CameraController.Instance;
        input = stateMachine.Input;
    }

    public override void Enter()
    {
        // скрыть курсор и забрать управление у UI
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        stateMachine.Input.InteractPressed += TryInteract;
    }

    public override void Exit()
    {
        stateMachine.Input.InteractPressed -= TryInteract;
    }

    public override void Update()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        motor.Move(input.MoveValue, cam);
    }

    private void TryInteract()
    {
        InteractionSystem.TryInteract(stateMachine.transform);
    }
}