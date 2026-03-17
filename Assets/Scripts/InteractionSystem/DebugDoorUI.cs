using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugDoorUI : MonoBehaviour
{
    public GameObject rootPanel;
    public GameObject seatingPanel;
    public TextMeshProUGUI visitorNameText;
    public Image bloodColorSwatch;

    private void Awake()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (seatingPanel != null) seatingPanel.SetActive(false);
    }

    [ContextMenu("ForceShow")]
    public void ForceShow()
    {
        Debug.Log("[DebugDoorUI] ForceShow. rootPanel null? " + (rootPanel == null));
        if (rootPanel != null) rootPanel.SetActive(true);
    }

    public void ShowConciergeSafe()
    {
        Debug.Log("[DebugDoorUI] ShowConciergeSafe called. rootPanel null? " + (rootPanel == null));
        if (rootPanel == null)
        {
            Debug.LogError("[DebugDoorUI] rootPanel is null! assign it in inspector.");
            return;
        }
        rootPanel.SetActive(true);
    }
}