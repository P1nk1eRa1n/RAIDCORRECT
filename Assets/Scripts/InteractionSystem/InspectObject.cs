// InspectObject.cs
using UnityEngine;

public class InspectObject : MonoBehaviour, IInteractable
{
    [Header("Camera point to inspect this object")]
    public Transform cameraPoint;

    [Header("Transition duration")]
    public float transitionDuration = 0.6f;

    public void Interact(PlayerStateMachine player)
    {
        // Push — потому что потом нужно вернуться обратно
        player.PushState(new PlayerInspectState(player, cameraPoint, transitionDuration));
    }
}