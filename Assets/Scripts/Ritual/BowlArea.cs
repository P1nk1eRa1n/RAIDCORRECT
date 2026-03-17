using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BowlArea : MonoBehaviour, IInteractable
{
    [Header("Slot")]
    public Transform slotTransform;
    public float snapSmooth = 18f;
    public int capacity = 1;
    public string bowlId;

    [Header("Eject (on Interact)")]
    public float ejectDistance = 0.6f;
    public float ejectUp = 0.35f;
    public float ejectSpeed = 3f;
    public bool ejectTowardsPlayer = true;

    private List<DraggableObject> items = new List<DraggableObject>();
    public IReadOnlyList<DraggableObject> Items => items;

    private void Update()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var obj = items[i];
            if (obj == null) continue;
            Vector3 target = slotTransform.position;
            Quaternion targetRot = slotTransform.rotation;
            obj.transform.position = Vector3.Lerp(obj.transform.position, target, Time.deltaTime * snapSmooth);
            obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, targetRot, Time.deltaTime * snapSmooth);
        }
    }

    public bool PlaceItem(DraggableObject obj)
    {
        if (obj == null) return false;
        if (items.Count >= capacity) return false;

        obj.IsDragging = false;
        obj.PlaceInSlot(slotTransform, this);

        items.Add(obj);

        Debug.Log($"[Bowl] Placed {obj.GetDisplayName()} in bowl {bowlId}");

        // уведомляем, но реальные автозапуски контролирует RitualManager.autoPerformOnFull
        RitualManager.Instance?.OnBowlUpdated(this);
        return true;
    }

    public bool RemoveItem(DraggableObject obj)
    {
        if (obj == null) return false;
        if (!items.Contains(obj)) return false;

        bool removed = items.Remove(obj);
        if (removed)
        {
            // возвращаем физику и состояние
            obj.RemoveFromSlot();
            Debug.Log($"[Bowl] Removed {obj.GetDisplayName()} from bowl {bowlId}");
            RitualManager.Instance?.OnBowlUpdated(this);
            return true;
        }
        return false;
    }

    public List<string> GetAllTags()
    {
        var tags = new List<string>();
        foreach (var it in items)
            tags.AddRange(it.GetTags());
        return tags;
    }

    // --- Interact: извлечь последний предмет (eject) ---
    public void Interact(PlayerStateMachine player)
    {
        if (items.Count == 0)
        {
            Debug.Log($"[Bowl] Bowl {bowlId} is empty.");
            return;
        }

        // берём последний добавленный
        DraggableObject obj = items[items.Count - 1];
        if (obj == null)
        {
            items.RemoveAt(items.Count - 1);
            RitualManager.Instance?.OnBowlUpdated(this);
            return;
        }

        items.RemoveAt(items.Count - 1);

        // снимаем из слота (восстановит физику)
        obj.RemoveFromSlot();

        // позиционируем чуть наружу
        Vector3 ejectDir;
        if (player != null && ejectTowardsPlayer)
        {
            var playerPos = player.transform.position;
            ejectDir = (playerPos - slotTransform.position);
            ejectDir.y = 0f;
            if (ejectDir.sqrMagnitude < 0.01f) ejectDir = slotTransform.forward;
            ejectDir.Normalize();
        }
        else
        {
            ejectDir = slotTransform.right;
        }

        Vector3 worldPos = slotTransform.position + ejectDir * ejectDistance + Vector3.up * 0.15f;
        obj.transform.position = worldPos;
        obj.transform.rotation = slotTransform.rotation;
        obj.transform.localScale = obj.transform.localScale;

        // даём небольшой импульс (если есть Rigidbody)
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;

            Vector3 impulse = (ejectDir * ejectSpeed) + (Vector3.up * ejectUp * ejectSpeed);
#if UNITY_2022_2_OR_NEWER
            rb.linearVelocity = impulse;
#else
            rb.velocity = impulse;
#endif
        }

        RitualManager.Instance?.OnBowlUpdated(this);
        Debug.Log($"[Bowl] Ejected {obj.GetDisplayName()} from bowl {bowlId}");
    }

    // --- New: принудительно очистить чашу (used by RitualManager) ---
    // destroyItems: если true — объекты будут уничтожены; иначе они будут просто извлечены и оставлены в мире
    public void ForceClear(bool destroyItems = true)
    {
        if (items == null || items.Count == 0) return;

        // сделаем копию, чтобы безопасно изменять список
        var copy = items.ToList();
        items.Clear();

        foreach (var it in copy)
        {
            if (it == null) continue;

            // снимаем с чаши (восстанавливает физику)
            it.RemoveFromSlot();

            if (destroyItems)
            {
                // уничтожаем игровой объект
                Destroy(it.gameObject);
            }
        }

        // уведомляем систему о смене состояния чаши
        RitualManager.Instance?.OnBowlUpdated(this);
    }
}