using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    private CharacterController cc;
    [Header("Movement")]
    public float moveSpeed = 4.5f;
    public float sprintMultiplier = 1.5f; // если захотим
    public float rotationSmoothTime = 0.12f;
    private float rotationVelocity;

    [Header("Gravity")]
    public float gravity = -9.81f;
    private float verticalVelocity;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    // ¬ызываетс€ из состо€ни€ движени€ каждый кадр
    // PlayerMotor.cs (часть)
    public void Move(Vector2 input, Transform cameraTransform)
    {
        Vector3 direction = Vector3.zero;

        if (cameraTransform != null && input.sqrMagnitude > 0.001f)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = cameraTransform.right;
            right.y = 0;
            right.Normalize();

            direction = forward * input.y + right * input.x;
            direction.Normalize();
        }

        Vector3 velocity = direction * moveSpeed;

        // vertical
        if (cc.isGrounded)
        {
            if (verticalVelocity < 0) verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;
        velocity.y = verticalVelocity;

        cc.Move(velocity * Time.deltaTime);
    }

    // ѕолностью остановить двигатель
    public void Stop()
    {
        // ничего не делаем, CharacterController сам остановитс€
    }
}