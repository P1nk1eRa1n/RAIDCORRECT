using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerInputActions inputActions;

    public Vector2 MoveValue { get; private set; }
    public Vector2 LookValue { get; private set; }
    public Vector2 ScrollValue { get; private set; }

    public event Action InteractPressed;
    public event Action DragPressed;
    public event Action DragReleased;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();

        inputActions.Player.Move.performed += ctx => MoveValue = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => MoveValue = Vector2.zero;

        inputActions.Player.Look.performed += ctx => LookValue = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => LookValue = Vector2.zero;

        // Interact
        inputActions.Player.Interact.performed += ctx => InteractPressed?.Invoke();

        inputActions.Player.Drag.performed += ctx => DragPressed?.Invoke();
        inputActions.Player.Drag.canceled += ctx => DragReleased?.Invoke();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        // Scroll reading: use new input system Mouse.current if available (works without specific binding)
        if (Mouse.current != null)
        {
            var val = Mouse.current.scroll.ReadValue();
            ScrollValue = val;
        }
        else
        {
            // fallback to old Input API if necessary (very small compatibility)
            float s = Input.GetAxis("Mouse ScrollWheel");
            ScrollValue = new Vector2(0f, s);
        }
    }
}