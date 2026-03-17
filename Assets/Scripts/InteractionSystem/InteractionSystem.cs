using UnityEngine;

public static class InteractionSystem
{
    public static float interactDistance = 3f;

    public static void TryInteract(Transform player)
    {
        var cam = Camera.main;
        if (cam == null) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                var playerSM = player.GetComponent<PlayerStateMachine>();
                interactable.Interact(playerSM);
            }
        }
    }
}