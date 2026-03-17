using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MissingRefsFinder : EditorWindow
{
    [MenuItem("Tools/Find Missing Scene References")]
    public static void Open() => GetWindow<MissingRefsFinder>("FindMissingRefs");

    private void OnGUI()
    {
        if (GUILayout.Button("Scan Scene for missing serialized UnityEngine.Object refs"))
        {
            ScanScene();
        }
    }

    private static void ScanScene()
    {
        var results = new List<string>();
        var gos = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in gos)
            ScanGameObject(go, results);

        if (results.Count == 0) Debug.Log("No missing serialized UnityEngine.Object references found.");
        else
        {
            Debug.LogWarning("Found missing references:");
            foreach (var r in results) Debug.Log(r);
        }
    }

    private static void ScanGameObject(GameObject go, List<string> results)
    {
        var comps = go.GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null)
            {
                results.Add($"GameObject '{go.name}' has a missing (destroyed) component in its hierarchy.");
                continue;
            }
            var so = new SerializedObject(c);
            var prop = so.GetIterator();
            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null && prop.stringValue != null && prop.stringValue != "")
                    {
                        results.Add($"Component {c.GetType().Name} on '{c.gameObject.name}' has missing reference in field '{prop.displayName}'");
                    }
                }
            }
        }
    }
}