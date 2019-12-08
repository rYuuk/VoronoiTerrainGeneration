using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Moisture
{
    // Calculate moisture. Freshwater sources spread moisture: rivers
    // and lakes (not oceans). Saltwater sources have moisture but do
    // not spread it (we set it at the end, after propagation).
    public static void AssignCorner(ref Map map)
    {
        float newMoisture;
        Queue<Graph.Corner> queue = new Queue<Graph.Corner>();
        // Fresh water
        foreach (var q in map.corners)
        {
            if ((q.water || q.river > 0) && !q.ocean)
            {
                q.moisture = q.river > 0 ? Mathf.Min(3.0f, (0.2f * q.river)) : 1.0f;
                queue.Enqueue(q);
            }
            else
                q.moisture = 0.0f;
        }
        while (queue.Count > 0)
        {
            var q = queue.Dequeue();

            foreach (var r in q.adjacent)
            {
                newMoisture = q.moisture * 0.9f;
                if (newMoisture > r.moisture)
                {
                    r.moisture = newMoisture;
                    queue.Enqueue(r);
                }
            }
        }
        // Salt water
        foreach (var q in map.corners)
        {
            if (q.ocean || q.coast)
                q.moisture = 1.0f;
        }
    }

    // Change the overall distribution of moisture to be evenly distributed.
    public static void Redistribute(ref List<Graph.Corner> locations)
    {
        locations.OrderBy(x => x.moisture);
        for (int i = 0; i < locations.Count; i++)
        {
            locations[i].moisture = (float)i / (locations.Count - 1);
        }
    }

    // Polygon moisture is the average of the moisture at corners
    public static void AssignPolygon(ref Map map)
    {
        float sumMoisture;
        foreach (var p in map.centers)
        {
            sumMoisture = 0.0f;
            foreach (var q in p.corners)
            {
                if (q.moisture > 1.0) q.moisture = 1.0f;
                sumMoisture += q.moisture;
            }
            p.moisture = sumMoisture / p.corners.Count;
        }
    }

}
