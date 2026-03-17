using UnityEngine;

public class RitualStand : MonoBehaviour, IInteractable
{
    [Header("Optional UI")]
    public string promptText = "Start ritual";

    public void Interact(PlayerStateMachine player)
    {
        // Проверим, заполнены ли чаши
        if (RitualManager.Instance == null) { Debug.LogWarning("No RitualManager"); return; }

        // вместо автоматического запуска мы запустим TryPerformRitual только при нажатии на тумбу
        RitualManager.Instance.TryPerformRitual();
    }
}