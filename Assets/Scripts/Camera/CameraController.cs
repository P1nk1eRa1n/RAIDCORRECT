using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Target")]
    public Transform targetToFollow; // CameraPivot (child of player)

    [Header("Rotation (FPS style)")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    private float yaw;
    private float pitch;

    // transition
    private Coroutine transitionCoroutine;
    private bool lockedToPoint = false;

    private PlayerInputReader inputReader;
    private Transform playerTransform;

    // locked-look (for candle)
    private bool lockedLookEnabled = false;
    private float lockedLookYawOffset = 0f;
    private float lockedLookPitchOffset = 0f;
    private float lockedLookLimit = 12f; // degrees
    private float lockedLookBaseYaw = 0f;
    private float lockedLookBasePitch = 0f;
    private Vector3 lockedPointPosition;
    private Quaternion lockedPointRotation;

    public bool IsTransitioning => transitionCoroutine != null;
    public bool IsLockedToPoint => lockedToPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        Instance = this;
    }

    private void Start()
    {
        if (targetToFollow != null)
        {
            playerTransform = targetToFollow.root;
            if (playerTransform != null)
                inputReader = playerTransform.GetComponent<PlayerInputReader>();
        }

        Vector3 euler = transform.eulerAngles;
        yaw = (playerTransform != null) ? playerTransform.eulerAngles.y : euler.y;
        pitch = euler.x;
    }

    private void LateUpdate()
    {
        // ≈сли идет transition Ч пока ничего не делаем (TransitionTo сам держит позицию/ротацию)
        if (transitionCoroutine != null) return;

        // ќбновим inputReader, если потер€ли
        if (inputReader == null && playerTransform != null)
            inputReader = playerTransform.GetComponent<PlayerInputReader>();

        // ≈сли камера Ђзаблокированаї на точке и без локального look Ч ничего не делаем
        if (lockedToPoint && !lockedLookEnabled)
        {
            // держим позицию/ротацию, не реагируем на мышь
            transform.position = lockedPointPosition;
            transform.rotation = lockedPointRotation;
            return;
        }

        // ≈сли камера заблокирована и включЄн локальный look Ч даЄм небольшой контроль игроку
        if (lockedToPoint && lockedLookEnabled)
        {
            Vector2 lookDelta = inputReader != null ? inputReader.LookValue : Vector2.zero;
            // немного снизим чувствительность в режиме осмотра
            float yawDelta = lookDelta.x * mouseSensitivity * 0.6f;
            float pitchDelta = lookDelta.y * mouseSensitivity * 0.6f;

            lockedLookYawOffset += yawDelta;
            lockedLookPitchOffset -= pitchDelta;

            lockedLookYawOffset = Mathf.Clamp(lockedLookYawOffset, -lockedLookLimit, lockedLookLimit);
            lockedLookPitchOffset = Mathf.Clamp(lockedLookPitchOffset, -lockedLookLimit, lockedLookLimit);

            float appliedYaw = lockedLookBaseYaw + lockedLookYawOffset;
            float appliedPitch = lockedLookBasePitch + lockedLookPitchOffset;
            appliedPitch = Mathf.Clamp(appliedPitch, minPitch, maxPitch);

            transform.position = lockedPointPosition;
            transform.rotation = Quaternion.Euler(appliedPitch, appliedYaw, 0f);
            return;
        }

        // обычный follow режим
        if (targetToFollow == null) return;

        Vector2 look = inputReader != null ? inputReader.LookValue : Vector2.zero;
        float dx = look.x * mouseSensitivity;
        float dy = look.y * mouseSensitivity;

        yaw += dx;
        pitch -= dy;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (playerTransform != null)
            playerTransform.rotation = Quaternion.Euler(0f, yaw, 0f);

        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.position = targetToFollow.position;
    }

    // ѕлавный переход к точке Ч теперь с параметром allowLook
    public void MoveToPoint(Transform cameraPoint, float duration, bool allowLook = false, float lookLimitDegrees = 12f)
    {
        if (cameraPoint == null) return;

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        // prepare locked look variables Ч but wait transition end to actually enable lockedToPoint
        lockedLookEnabled = allowLook;
        lockedLookYawOffset = 0f;
        lockedLookPitchOffset = 0f;
        lockedLookLimit = Mathf.Abs(lookLimitDegrees);

        lockedPointPosition = cameraPoint.position;
        lockedPointRotation = cameraPoint.rotation;

        // store base yaw/pitch from cameraPoint rotation for locked look
        Vector3 e = cameraPoint.rotation.eulerAngles;
        lockedLookBasePitch = e.x;
        lockedLookBaseYaw = e.y;

        // start transition; when complete, TransitionTo will set lockedToPoint = true
        transitionCoroutine = StartCoroutine(TransitionTo(lockedPointPosition, lockedPointRotation, duration, unlockOnComplete: false));
    }

    // ѕлавное возвращение к follow; после завершени€ Ч камера разблокируетс€
    public void ReturnToFollow(float duration)
    {
        if (targetToFollow == null)
        {
            // если нет цели Ч просто разблокируем
            lockedToPoint = false;
            lockedLookEnabled = false;
            return;
        }

        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);

        Vector3 desiredPos = targetToFollow.position;
        Quaternion desiredRot = Quaternion.Euler(pitch, yaw, 0f);

        // запускаем переход и разлочим при завершении (unlockOnComplete = true)
        transitionCoroutine = StartCoroutine(TransitionTo(desiredPos, desiredRot, duration, unlockOnComplete: true));
    }

    // ѕринудительное немедленное возвращение (останавливает переход и снимает lock)
    public void ForceReturnToFollow()
    {
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = null;
        lockedToPoint = false;
        lockedLookEnabled = false;
        lockedLookYawOffset = lockedLookPitchOffset = 0f;

        // синхронизируем yaw/pitch с текущ player rotation if possible
        if (playerTransform != null)
        {
            yaw = playerTransform.eulerAngles.y;
        }
        pitch = transform.eulerAngles.x;

        // вернЄм позицию и ротацию к follow сразу
        if (targetToFollow != null)
        {
            transform.position = targetToFollow.position;
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    private IEnumerator TransitionTo(Vector3 toPos, Quaternion toRot, float duration, bool unlockOnComplete)
    {
        float t = 0f;
        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;

        // avoid division by zero
        float safeDur = Mathf.Max(0.0001f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / safeDur;
            transform.position = Vector3.Lerp(fromPos, toPos, Mathf.SmoothStep(0f, 1f, t));
            transform.rotation = Quaternion.Slerp(fromRot, toRot, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = toPos;
        transform.rotation = toRot;

        transitionCoroutine = null;

        if (unlockOnComplete)
        {
            lockedToPoint = false;
            lockedLookEnabled = false;
            lockedLookYawOffset = lockedLookPitchOffset = 0f;
        }
        else
        {
            // успешно перешли к точке Ч теперь зафиксируем еЄ и включим локальный look если задано
            lockedToPoint = true;

            // base yaw/pitch already подготовлены в MoveToPoint; but in case called directly, set them here:
            Vector3 e = toRot.eulerAngles;
            lockedLookBasePitch = e.x;
            lockedLookBaseYaw = e.y;

            lockedPointPosition = toPos;
            lockedPointRotation = toRot;

            // keep lockedLookEnabled as previously set by MoveToPoint
        }
    }
}