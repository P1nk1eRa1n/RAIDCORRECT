using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConciergeObject : MonoBehaviour, IInteractable
{
    public DoorController doorController;
    public GameObject ConciergeUI; // scene canvas object

    private void Awake()
    {

        AutoAssignMissingReferences();
    }

    private void OnValidate()
    {
        AutoAssignMissingReferences();
    }

    private void AutoAssignMissingReferences()
    {
        if (doorController == null)
            doorController = UnityEngine.Object.FindFirstObjectByType<DoorController>();

        if (ConciergeUI == null)
        {
            var doorUI = FindDoorUIIncludingInactive();
            if (doorUI != null)
            {
                var canvas = doorUI.GetComponentInParent<Canvas>(true);
                ConciergeUI = canvas != null ? canvas.gameObject : doorUI.gameObject;
            }
        }
    }

    private static DoorUI FindDoorUIIncludingInactive()
    {
        var allDoorUIs = Resources.FindObjectsOfTypeAll<DoorUI>();
        foreach (var ui in allDoorUIs)
        {
            if (ui == null) continue;
            if (!ui.gameObject.scene.IsValid()) continue;
            return ui;
        }
        return null;
    }

    public void Interact(PlayerStateMachine player)
    {
        if (player == null) return;

        AutoAssignMissingReferences();

        if (doorController == null) { Debug.LogError("[ConciergeObject] No DoorController"); return; }
        if (ConciergeUI == null) { Debug.LogError("[ConciergeObject] ConciergeUI (scene) not assigned"); return; }

        Transform cameraPoint = doorController.peekCameraPoint != null ? doorController.peekCameraPoint : doorController.puzzleCameraPoint;

        Action opened = () =>
        {
            ConciergeUI.SetActive(true);
            var doorUI = ConciergeUI.GetComponentInChildren<DoorUI>(true);
            if (doorUI != null) doorUI.Refresh();
        };

        Action closed = () =>
        {
            ConciergeUI.SetActive(false);
        };

        player.PushState(new PlayerConciergeState(player, cameraPoint, doorController, 0.35f, opened, closed));
    }
}