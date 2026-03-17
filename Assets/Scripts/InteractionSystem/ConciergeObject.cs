using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConciergeObject : MonoBehaviour, IInteractable
{
    public DoorController doorController;
    public GameObject ConciergeUI; // scene canvas object

    private void Reset()
    {
        // Ensure collider is trigger or something so player can interact
    }

    public void Interact(PlayerStateMachine player)
    {
        if (doorController == null) { Debug.LogError("[ConciergeObject] No DoorController"); return; }
        if (ConciergeUI == null) { Debug.LogError("[ConciergeObject] ConciergeUI (scene) not assigned"); return; }

        Transform cameraPoint = doorController.peekCameraPoint != null ? doorController.peekCameraPoint : doorController.puzzleCameraPoint;

        // create callbacks: opened = enable canvas and let DoorUI refresh; closed = hide canvas
        Action opened = () =>
        {
            ConciergeUI.SetActive(true);
            var doorUI = ConciergeUI.GetComponentInChildren<DoorUI>();
            if (doorUI != null) doorUI.Refresh(); // форсированно синхронизируем панели
        };

        Action closed = () =>
        {
            ConciergeUI.SetActive(false);
        };

        player.PushState(new PlayerConciergeState(player, cameraPoint, doorController, 0.35f, opened, closed));
    }
}