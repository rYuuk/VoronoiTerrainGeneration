using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Elevation
{
    // Determine elevations and water at Voronoi corners. By
    // construction, we have no local minima. This is important for
    // the downslope vectors later, which are used in the river
    // construction algorithm. Also by construction, inlets/bays
    // push low elevation areas inland, which means many rivers end
    // up flowing out through them. Also by construction, lakes
    // often end up on river paths because they don't raise the
    // elevation as much as other terrain does.
    public static void AssignCorner(ref Map map, IslandShape islandShape, bool needsMoreRandomness)
    {
        System.Random mapRandom = new System.Random(map.seed);
        Queue<Graph.Corner> queue = new Queue<Graph.Corner>();
        foreach (var q in map.corners)
            q.water = !islandShape.IsInside(q.pos);
        
        foreach (var q in map.corners)
        {
            // The edges of the map are elevation 0
            if (q.border)
            {
                q.elevation = 0.0f;
                queue.Enqueue(q);
            }
            else
            {
                q.elevation = Mathf.Infinity;
            }
        }
        // Traverse the graph and assign elevations to each point. As we
        // move away from the map border, increase the elevations. This
        // guarantees that rivers always have a way down to the coast by
        // going downhill (no local minima).
        while (queue.Count > 0)
        {
            var q = queue.Dequeue();

            foreach (var s in q.adjacent)
            {
                // Every step up is epsilon over water or 1 over land. The
                // number doesn't matter because we'll rescale the
                // elevations later.
                var newElevation = 0.01f + q.elevation;
                if (!q.water && !s.water)
                {
                    newElevation += 1;
                    if (needsMoreRandomness)
                    {
                        // HACK: the map looks nice because of randomness of
                        // points, randomness of rivers, and randomness of
                        // edges. Without random point selection, I needed to
                        // inject some more randomness to make maps look
                        // nicer. I'm doing it here, with elevations, but I
                        // think there must be a better way. This hack is only
                        // used with square/hexagon grids.
                        newElevation += (float)mapRandom.NextDouble();
                    }
                }
                // If this point changed, we'll add it to the queue so
                // that we can process its neighbors too.
                if (newElevation < s.elevation)
                {
                    s.elevation = newElevation;
                    queue.Enqueue(s);
                }
            }
        }
    }


    // Change the overall distribution of elevations so that lower
    // elevations are more common than higher
    // elevations. Specifically, we want elevation X to have frequency
    // (1-X).  To do this we will sort the corners, then set each
    // corner to its desired elevation.
    public static void Redistribute(ref List<Graph.Corner> locations)
    {
        // SCALE_FACTOR increases the mountain area. At 1.0 the maximum
        // elevation barely shows up on the map, so we set it to 1.1.
        float SCALE_FACTOR = 1.1f;
        float x, y;

        locations = locations.OrderBy(loc => loc.elevation).ToList();
        for (int i = 0; i < locations.Count; i++)
        {
            // Let y(x) be the total area that we want at elevation <= x.
            // We want the higher elevations to occur less than lower
            // ones, and set the area to be y(x) = 1 - (1-x)^2.
            y = (float)i / (locations.Count - 1);
            // Now we have to solve for x, given the known y.
            //  *  y = 1 - (1-x)^2
            //  *  y = 1 - (1 - 2x + x^2)
            //  *  y = 2x - x^2
            //  *  x^2 - 2x + y = 0

            // x =  1- Sqrt(1-y)
            // From this we can use the quadratic equation to get:
            x = Mathf.Sqrt(SCALE_FACTOR) - Mathf.Sqrt(SCALE_FACTOR * (1 - y));
            x = x > 1.0f ? 1.0f : x;// TODO: does this break downslopes?
            locations[i].elevation = x;
        }
    }
    // Polygon elevations are the average of the elevations of their corners.
    public static void AssignPolygon(ref Map map)
    {
        float sumElevation;
        foreach (var p in map.centers)
        {
            sumElevation = 0.0f;
            foreach (var q in p.corners)
                sumElevation += q.elevation;
            p.elevation = sumElevation / p.corners.Count;
        }
    }

}
