using System;
using UnityEngine;

public static class InteractionSystem
{
    public static float interactDistance = 3f;
    public static LayerMask interactLayerMask = ~0;
    public static QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    public static void TryInteract(Transform player)
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (player == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        var hits = Physics.RaycastAll(ray, interactDistance, interactLayerMask, triggerInteraction);
        if (hits == null || hits.Length == 0) return;

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        var playerSM = player.GetComponent<PlayerStateMachine>();
        if (playerSM == null) return;
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(playerSM);
                return;
            }
        }

        // Useful in level debugging: ray hit something but there is no interactable component.
        Debug.Log("[InteractionSystem] Ray hit colliders, but no IInteractable was found on them.");
    }
}