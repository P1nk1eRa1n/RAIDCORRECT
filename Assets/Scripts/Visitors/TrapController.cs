using UnityEngine;

// Простая заглушка для ловушки
public class TrapController : MonoBehaviour
{
    public void ActivateForVisitor(VisitorData visitor)
    {
        Debug.Log($"[Trap] Activated for {visitor?.displayName}");
        // Здесь можно играть эффект (particles, sound) и вызывать последствия
    }
}