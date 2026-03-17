using UnityEngine;

public class InspectBookObject : MonoBehaviour, IInteractable
{
    public Transform cameraPoint;
    public GameObject recipeUI; // assign
    public float transitionDuration = 0.6f;

    public void Interact(PlayerStateMachine player)
    {
        System.Action opened = () => { if (recipeUI) recipeUI.SetActive(true); };
        System.Action closed = () => { if (recipeUI) recipeUI.SetActive(false); };

        player.PushState(new PlayerInspectState(player, cameraPoint, transitionDuration));
        // чтобы UI включился после перехода — можно подписать на RitualManager? Но проще:
        // В PlayerInspectState после WaitForCameraArrive() можно вызывать opened()
        // Для этого надо изменить PlayerInspectState to accept actions (we showed pattern in ConciergeState)
    }
}