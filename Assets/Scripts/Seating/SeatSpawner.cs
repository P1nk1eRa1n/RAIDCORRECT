// SeatSpawner.cs
using UnityEngine;

public class SeatSpawner : MonoBehaviour
{
    public SeatData[] pool;
    public GameObject seatPrefab; // prefab must have DraggableObject + Seat component
    public int initialCount = 4;
    public Vector3 spawnArea = new Vector3(2f, 0.5f, 2f);

    private void Start()
    {
        for (int i = 0; i < initialCount; i++) SpawnRandom();
    }

    public GameObject SpawnRandom()
    {
        if (pool == null || pool.Length == 0 || seatPrefab == null) return null;
        var data = pool[Random.Range(0, pool.Length)];
        var go = Instantiate(seatPrefab, transform.position + new Vector3(Random.Range(-spawnArea.x, spawnArea.x), 0.1f, Random.Range(-spawnArea.z, spawnArea.z)), Quaternion.Euler(0, Random.Range(0, 360), 0));
        var seat = go.GetComponent<Seat>();
        if (seat != null) seat.data = data;
        var dr = go.GetComponent<DraggableObject>();
        if (dr != null)
        {
            // optional: set collider, rigidbody defaults
        }
        return go;
    }
}