using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Biomes
{
    public static Dictionary<string, string> displayColors = new Dictionary<string, string>()
    {
        // Features
        { "OCEAN", "#44447a" },
        { "COAST", "#33335a" },
        { "LAKESHORE", "#225588" },
        { "LAKE", "#336699" },
        { "RIVER", "#225588" },
        { "MARSH", "#2f6666"},
        { "ICE", "#99ffff" },
        { "BEACH", "#a09077"},
        { "ROAD1", "#442211"},
        { "ROAD2", "#553322"},
        { "ROAD3", "#664433"},
        { "BRIDGE", "#686860"},
        { "LAVA", "#cc3333"},

        // Terrain
        { "SNOW", "#ffffff"},
        { "TUNDRA", "#bbbbaa"},
        { "BARE", "#888888"},
        { "SCORCHED", "#555555"},
        { "TAIGA", "#99aa77"},
        { "SHRUBLAND", "#889977"},
        { "TEMPERATE_DESERT", "#c9d29b"},
        { "TEMPERATE_RAIN_FOREST", "#448855"},
        { "TEMPERATE_DECIDUOUS_FOREST", "#679459"},
        { "GRASSLAND", "#88aa55"},
        { "SUBTROPICAL_DESERT", "#d2b98b"},
        { "TROPICAL_RAIN_FOREST", "#337755"},
        { "TROPICAL_SEASONAL_FOREST", "#559944"}
    };

    private const float LAKE_THRESHOLD = 0.3f;

    // Determine polygon and corner types: ocean, coast, land.
    public static void AssignOceanCoastAndLand(ref Map data)
    {
        // Compute polygon attributes 'ocean' and 'water' based on the
        // corner attributes. Count the water corners per
        // polygon. Oceans are all polygons connected to the edge of the
        // map. In the first pass, mark the edges of the map as ocean;
        // in the second pass, mark any water-containing polygon
        // connected an ocean as ocean.
        Queue<Graph.Center> queue = new Queue<Graph.Center>();
        int numWater;

        foreach (var p in data.centers)
        {
            numWater = 0;
            foreach (var q in p.corners)
            {
                if (q.border)
                {
                    p.border = true;
                    p.ocean = true;
                    q.water = true;
                    queue.Enqueue(p);
                }
                if (q.water)
                    numWater += 1;
            }
            p.water = (p.ocean || numWater >= p.corners.Count * LAKE_THRESHOLD);
        }

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();

            foreach (var r in p.neighbours)
            {
                if (r.water && !r.ocean)
                {
                    r.ocean = true;
                    queue.Enqueue(r);
                }
            }
        }

        int numOcean = 0;
        int numLand = 0;

        // Set the polygon attribute 'coast' based on its neighbors. If
        // it has at least one ocean and at least one land neighbor,
        // then this is a coastal polygon.
        foreach (var p in data.centers)
        {
            numOcean = 0;
            numLand = 0;
            foreach (var r in p.neighbours)
            {
                numOcean += r.ocean ? 1 : 0;
                numLand += r.water ? 0 : 1;
            }
            p.coast = (numOcean > 0) && (numLand > 0);
        }


        // Set the corner attributes based on the computed polygon
        // attributes. If all polygons connected to this corner are
        // ocean, then it's ocean; if all are land, then it's land;
        // otherwise it's coast.
        foreach (var q in data.corners)
        {
            numOcean = 0;
            numLand = 0;
            foreach (var p in q.touches)
            {
                numOcean += p.ocean ? 1 : 0;
                numLand += p.water ? 0 : 1;
            }
            q.ocean = (numOcean == q.touches.Count);
            q.coast = (numOcean > 0) && (numLand > 0);
            q.water = q.border || ((numLand != q.touches.Count) && !q.coast);
        }
    }


    public static void AssignBiomes(ref Map map)
    {
        //biomes.Clear();
        foreach (var p in map.centers)
        {
            string biomeName = GetBiome(p);
            if (!map.biomes.Contains(biomeName))
                map.biomes.Add(biomeName);
            p.biome = biomeName;
        }
    }

    // Assign a biome type to each polygon. If it has
    // ocean/coast/water, then that's the biome; otherwise it depends
    // on low/high elevation and low/medium/high moisture. This is
    // roughly based on the Whittaker diagram but adapted to fit the
    // needs of the island map generator.
    private static string GetBiome(Graph.Center p)
    {
        if (p.ocean)
        {
            return "OCEAN";
        }
        else if (p.water)
        {
            if (p.elevation < 0.1f)
                return "MARSH";
            if (p.elevation > 0.8f)
                return "ICE";
            return "LAKE";
        }
        else if (p.coast)
        {
            return "BEACH";
        }
        else if (p.elevation > 0.8f)
        {
            if (p.moisture > 0.50f) return "SNOW";
            else if (p.moisture > 0.33f) return "TUNDRA";
            else if (p.moisture > 0.16f) return "BARE";
            else return "SCORCHED";
        }
        else if (p.elevation > 0.6f)
        {
            if (p.moisture > 0.66f) return "TAIGA";
            else if (p.moisture > 0.33f) return "SHRUBLAND";
            else return "TEMPERATE_DESERT";
        }
        else if (p.elevation > 0.3f)
        {
            if (p.moisture > 0.83f) return "TEMPERATE_RAIN_FOREST";
            else if (p.moisture > 0.50f) return "TEMPERATE_DECIDUOUS_FOREST";
            else if (p.moisture > 0.16f) return "GRASSLAND";
            else return "TEMPERATE_DESERT";
        }
        else
        {
            if (p.moisture > 0.66f) return "TROPICAL_RAIN_FOREST";
            else if (p.moisture > 0.33f) return "TROPICAL_SEASONAL_FOREST";
            else if (p.moisture > 0.16f) return "GRASSLAND";
            else return "SUBTROPICAL_DESERT";
        }
    }
}
