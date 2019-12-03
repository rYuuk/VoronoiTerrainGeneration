using UnityEngine;
using System.Collections;
using TriangleNet.Geometry;

public class IslandShape
{
    public enum FunctionType
    {
        Radial,
        Perlin
    }

    private float width;
    private float height;
    private int bumps;
    private float startAngle;
    private float dipAngle;
    private float dipWidth;
    
    private const float ISLAND_FACTOR = 1.07f;

    public IslandShape(int seed, float width, float height)
    {
        this.width = width;
        this.height = height;

        Random.InitState(seed);
        
        bumps = Random.Range(1, 6);
        startAngle = Random.Range(0f, 2 * Mathf.PI);
        dipAngle = Random.Range(0f, 2 * Mathf.PI);
        dipWidth = Random.Range(0.2f, 0.7f);
    }

    public bool IsInside(FunctionType type, Point point)
    {
        switch (type)
        {
            case FunctionType.Radial:
                return IsInsideRadial(point);
            case FunctionType.Perlin:
                return IsInsidePerlin(point);
            default:
                break;
        }
        return true;
    }


    /// <summary>
    /// Source: https://github.com/amitp/mapgen2/blob/4394df0e04101dbbdc36ee1e61ad7d62446bb3f1/Map.as#L797
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    private bool IsInsideRadial(Point point)
    {
        point = new Point(2 * (point.X / width - 0.5f), 2 * (point.Y / height - 0.5f));

        float angle = Mathf.Atan2((float)point.Y, (float)point.X);
        float length = 0.5f * (Mathf.Max(Mathf.Abs((float)point.X), Mathf.Abs((float)point.Y)) + point.Length());

        float r1 = 0.5f + 0.40f * Mathf.Sin(startAngle + bumps * angle + Mathf.Cos((bumps + 3) * angle));
        float r2 = 0.7f - 0.20f * Mathf.Sin(startAngle + bumps * angle - Mathf.Sin((bumps + 2) * angle));
        if (Mathf.Abs(angle - dipAngle) < dipWidth
            || Mathf.Abs(angle - dipAngle + 2 * Mathf.PI) < dipWidth
            || Mathf.Abs(angle - dipAngle - 2 * Mathf.PI) < dipWidth)
        {
            r1 = r2 = 0.2f;
        }
        return (length < r1 || (length > r1 * ISLAND_FACTOR && length < r2));
    }


    private bool IsInsidePerlin(Point point)
    {
        point = new Point(2 * (point.X / width - 0.5f), 2 * (point.Y / height - 0.5f));
        float noise = Mathf.PerlinNoise((int)point.X, (int)point.Y);
        return (noise > (0.3f * (1 + point.Length() * point.Length())));

    }

}
