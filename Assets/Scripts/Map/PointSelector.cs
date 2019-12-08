using UnityEngine;
using TriangleNet.Geometry;

public class PointSelector
{
    public enum FaceType
    {
        Random,
        Square,
        Hexagon,
        PoisonDisc
    }

    private static Vector2 dimensions;
    private static int seed;
    private static int regionCount;
    private static float radius;

    public static Polygon Generate(Vector2 dimensions, int seed, FaceType faceType, int regionCount = 0, float radius = 0)
    {
        PointSelector.dimensions = dimensions;
        PointSelector.seed = seed;
        PointSelector.regionCount = regionCount;
        PointSelector.radius = radius;

        switch (faceType)
        {
            case FaceType.Random:
                return GenerateRandom();
            case FaceType.Square:
                return GenerateSquare();
            case FaceType.Hexagon:
                return GenerateHexagon();
            case FaceType.PoisonDisc:
                return GeneratePoisonDisc();
            default:
                return null;
        }
    }

    private static Polygon GenerateRandom()
    {
        Polygon polygon = new Polygon();
        Random.InitState(seed);
        for (int i = 0; i < regionCount; i++)
            polygon.Add(new Vertex(Random.Range(0, dimensions.x), Random.Range(0, dimensions.y)));
        return polygon;

    }

    private static Polygon GeneratePoisonDisc()
    {
        Polygon polygon = new Polygon();
        Random.InitState(seed);
        PoissonDiscSampler poissonDiscSampler = new PoissonDiscSampler(dimensions.x, dimensions.y, radius);
        foreach (var sample in poissonDiscSampler.Samples())
            polygon.Add(sample.ToVertex());
        return polygon;
    }

    private static Polygon GenerateHexagon()
    {
        Polygon centroids = new Polygon();
        regionCount = (int)Mathf.Sqrt(regionCount);

        for (int x = 0; x < regionCount; x++)
        {
            for (int y = 0; y < regionCount; y++)
            {
                Vertex vertex = new Vertex(((0.5f + x) / regionCount) * dimensions.x, ((0.25f + (0.5f * (x % 2)) + y) / regionCount) * dimensions.y);
                centroids.Add(vertex);
            }
        }

        return centroids;
    }

    private static Polygon GenerateSquare()
    {
        Polygon centroids = new Polygon();
        regionCount = (int)Mathf.Sqrt(regionCount);

        for (int x = 0; x < regionCount; x++)
        {
            for (int y = 0; y < regionCount; y++)
            {
                Vertex vertex = new Vertex(((0.5f + x) / regionCount) * dimensions.x, ((0.5f + y) / regionCount) * dimensions.y);
                centroids.Add(vertex);
            }
        }

        return centroids;
    }
}
