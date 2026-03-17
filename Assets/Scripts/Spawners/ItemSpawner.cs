// ItemSpawner.cs
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn")]
    public ItemData[] pool; // заполнить ассетами
    public GameObject draggablePrefab; // общий префаб, у которого должен быть DraggableObject component
    public int initialCount = 5;
    public Vector3 spawnArea = new Vector3(2f, 0.5f, 2f);

    public void Start()
    {
        for (int i = 0; i < initialCount; i++)
            SpawnRandom();
    }

    public GameObject SpawnRandom()
    {
        if (pool == null || pool.Length == 0 || draggablePrefab == null) return null;
        var itemData = pool[Random.Range(0, pool.Length)];
        return Spawn(itemData);
    }

    public GameObject Spawn(ItemData data)
    {
        Vector3 pos = transform.position + new Vector3(Random.Range(-spawnArea.x, spawnArea.x), Random.Range(0.1f, spawnArea.y), Random.Range(-spawnArea.z, spawnArea.z));
        GameObject go = Instantiate(draggablePrefab, pos, Quaternion.Euler(0, Random.Range(0, 360f), 0));
        var dr = go.GetComponent<DraggableObject>();
        if (dr != null) dr.itemData = data;
        // optionally set icon/mesh based on data.prefab (or instantiate child)
        return go;
    }
}