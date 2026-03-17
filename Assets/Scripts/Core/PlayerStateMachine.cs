using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerInputReader))]
public class PlayerStateMachine : MonoBehaviour
{
    public PlayerState CurrentState { get; private set; }

    private Stack<PlayerState> stateStack = new Stack<PlayerState>();

    public PlayerInputReader Input { get; private set; }

    private void Awake()
    {
        Input = GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        // стартовое состояние
        SwitchState(new PlayerMoveState(this));
    }

    private void Update()
    {
        CurrentState?.Update();
    }

    private void FixedUpdate()
    {
        CurrentState?.FixedUpdate();
    }

    // Полная замена
    public void SwitchState(PlayerState newState)
    {
        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    // Вложенное состояние (напр. Inspect) — можно Pop чтобы вернуться
    public void PushState(PlayerState newState)
    {
        if (CurrentState != null)
            stateStack.Push(CurrentState);

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    public void PopState()
    {
        CurrentState?.Exit();

        if (stateStack.Count > 0)
            CurrentState = stateStack.Pop();
        else
            CurrentState = null;

        CurrentState?.Enter();
    }
}