using UnityEngine;
using System;
using System.Collections;

public class VisitorActor : MonoBehaviour
{
    public VisitorData data;
    public Transform visualRoot;      // object that visually moves
    public float moveSpeed = 3f;

    private Coroutine moveCoroutine;

    private void Reset()
    {
        if (visualRoot == null) visualRoot = this.transform;
    }

    public void Initialize(VisitorData d)
    {
        data = d;
    }

    // New: two-stage movement: approach -> seat -> face lookAt -> callback
    public void MoveToSeatAndSit(Transform approachPoint, Transform seatPoint, Transform lookAtPoint, Action onArrived = null)
    {
        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(MoveSequence(approachPoint, seatPoint, lookAtPoint, onArrived));
    }

    private IEnumerator MoveSequence(Transform approach, Transform seat, Transform lookAt, Action onArrived)
    {
        if (visualRoot == null)
        {
            onArrived?.Invoke();
            yield break;
        }

        // Stage 1: approach (if provided and not too close)
        if (approach != null)
        {
            yield return StartCoroutine(MoveToPointRoutine(approach.position));
        }

        // Stage 2: move to exact seat point
        if (seat != null)
        {
            yield return StartCoroutine(MoveToPointRoutine(seat.position));
        }

        // Face lookAt point
        if (lookAt != null)
        {
            Vector3 dir = (lookAt.position - visualRoot.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
                float t = 0f;
                float dur = 0.25f;
                Quaternion from = visualRoot.rotation;
                while (t < 1f)
                {
                    t += Time.deltaTime / dur;
                    visualRoot.rotation = Quaternion.Slerp(from, target, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
            }
        }

        // Mark occupied (Seat logic handles occupied flag)
        onArrived?.Invoke();
    }

    private IEnumerator MoveToPointRoutine(Vector3 target)
    {
        Vector3 start = visualRoot.position;
        float dist = Vector3.Distance(start, target);
        float duration = Mathf.Max(0.05f, dist / moveSpeed);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            visualRoot.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
    }

    public void LeaveAndDestroy(float delay = 0.5f)
    {
        Destroy(gameObject, delay);
    }
}