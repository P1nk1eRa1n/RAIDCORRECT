using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "VisitorDay", menuName = "Visitor/VisitorDay")]
public class VisitorDay : ScriptableObject
{
    [Header("Fixed visitors (will be enqueued in order)")]
    public List<VisitorData> fixedVisitors = new List<VisitorData>();

    [Header("Pool for random visitors (will fill remaining slots)")]
    public List<VisitorData> randomPool = new List<VisitorData>();

    [Header("Total visitors count for this day (including fixed ones)")]
    public int totalVisitors = 3;

    public List<VisitorData> BuildQueue()
    {
        var queue = new List<VisitorData>();
        // add fixed
        foreach (var v in fixedVisitors)
            if (v != null) queue.Add(v);

        // fill with random picks from pool
        var rnd = new System.Random();
        while (queue.Count < totalVisitors && randomPool.Count > 0)
        {
            var pick = randomPool[rnd.Next(randomPool.Count)];
            queue.Add(pick);
        }
        return queue;
    }
}