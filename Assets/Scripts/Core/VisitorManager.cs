using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisitorManager : MonoBehaviour
{
    public static VisitorManager Instance { get; private set; }

    [Header("Current day config")]
    public VisitorDay currentDay;

    private Queue<VisitorData> queue = new Queue<VisitorData>();

    public event Action<VisitorData> OnVisitorArrived; // fired when next visitor is ready

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // optionally start automatically if day assigned
        if (currentDay != null) StartDay();
    }

    public void StartDay()
    {
        queue.Clear();
        if (currentDay == null) { Debug.LogWarning("[VisitorManager] currentDay null"); return; }
        var list = currentDay.BuildQueue();
        foreach (var v in list) queue.Enqueue(v);

        // вместо TryNotifyNext(); Ч вызываем с задержкой на 1 кадр
        StartCoroutine(NotifyNextNextFrame());
    }

    private IEnumerator NotifyNextNextFrame()
    {
        // ждем один кадр, чтобы все OnEnable успели подписатьс€
        yield return null;
        TryNotifyNext();
    }

    public void TryNotifyNext()
    {
        if (queue.Count == 0) return;
        var next = queue.Peek();
        // "стук" Ч дл€ прототипа: лог в консоль
        Debug.Log($"[Visitor] Stuk! Visitor arrived: {next.displayName}");
        OnVisitorArrived?.Invoke(next);
    }

    // вызываетс€, когда DoorController конвертирует pending -> actor (после spawn)
    public void DequeueCurrent()
    {
        if (queue.Count == 0) return;
        queue.Dequeue();
        // notify next after small delay to simulate time between knocks (можно убрать)
        TryNotifyNext();
    }

    // утилита
    public bool TryGetCurrentPending(out VisitorData pending)
    {
        if (queue.Count == 0) { pending = null; return false; }
        pending = queue.Peek();
        return true;
    }
}