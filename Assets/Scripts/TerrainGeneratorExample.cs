using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGeneratorExample : MonoBehaviour
{
    public Vector2 dimensions = new Vector2(100, 100);
    public bool autoUpdate = false;
    public float radius = 10;
    public int seed = 1;
    public int relaxationCount = 2;
    public PointSelector.FaceType cellType = PointSelector.FaceType.Random;
    public int regionCount = 40;
    public bool showGizmo = false;
    public IslandShape.FunctionType islandFunction;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Material baseMaterial;
    public AnimationCurve heightMap;
    public float heightFactor = 20f;

    private Map mapData;

    public void Generate()
    {
        Random.InitState(seed);
        MapGenerator map = new MapGenerator();
        mapData = map.Generate(dimensions, seed, cellType, islandFunction, heightFactor, heightMap, regionCount, relaxationCount, radius);
        MeshData meshData = map.GenerateMeshData();
        meshFilter.mesh = meshData.CreateMesh();

        meshRenderer.sharedMaterials = new Material[mapData.biomes.Count];
        Material[] materials = new Material[mapData.biomes.Count];
        for (int i = 0; i < mapData.biomes.Count; i++)
        {
            Material material = new Material(baseMaterial);
            ColorUtility.TryParseHtmlString(Biomes.displayColors[mapData.biomes[i]], out Color biome);
            material.color = biome;
            material.name = mapData.biomes[i];
            materials[i] = material;
        }

        meshRenderer.sharedMaterials = materials;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        if (mapData == null)
            return;

        foreach (var edge in mapData.edges)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(edge.v0.pos, edge.v1.pos);
            Gizmos.color = Color.black;
            Gizmos.DrawLine(edge.d0.pos, edge.d1.pos);
            ColorUtility.TryParseHtmlString(Biomes.displayColors[edge.d0.biome], out Color color);
            Gizmos.color = color;
            Gizmos.DrawSphere(edge.d0.pos, 1f);
            Gizmos.DrawSphere(edge.d1.pos, 1f);
        }


        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(0, dimensions.y));
        Gizmos.DrawLine(new Vector2(0, 0), new Vector2(dimensions.x, 0));
        Gizmos.DrawLine(new Vector2(dimensions.x, 0), dimensions);
        Gizmos.DrawLine(new Vector2(0, dimensions.y), dimensions);
    }

    public void OnValidate()
    {
        dimensions = new Vector2(Mathf.Max(15, dimensions.x), Mathf.Max(15, dimensions.y));
        seed = Mathf.Max(0, seed);
        regionCount = Mathf.Max(1, regionCount);
        radius = Mathf.Max(2, radius);
        relaxationCount = Mathf.Max(0, relaxationCount);
    }
}
