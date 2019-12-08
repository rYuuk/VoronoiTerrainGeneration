using UnityEngine;
using System.Collections;

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
    private FunctionType functionType;
    
    private const float ISLAND_FACTOR = 1.07f;
    public IslandShape(int seed, float width, float height, FunctionType functionType)
    {
        this.width = width;
        this.height = height;
        this.functionType = functionType;

        Random.InitState(seed);
        
        bumps = Random.Range(1, 6);
        startAngle = Random.Range(0f, 2 * Mathf.PI);
        dipAngle = Random.Range(0f, 2 * Mathf.PI);
        dipWidth = Random.Range(0.2f, 0.7f);
    }

    public bool IsInside(Vector2 pos)
    {
        switch (functionType)
        {
            case FunctionType.Radial:
                return IsInsideRadial(pos);
            case FunctionType.Perlin:
                return IsInsidePerlin(pos);
            default:
                break;
        }
        return true;
    }

    private bool IsInsideRadial(Vector2 pos)
    {
        pos = new Vector2(2 * (pos.x / width - 0.5f), 2 * (pos.y / height - 0.5f));

        float angle = Mathf.Atan2(pos.y,pos.x);
        float length = 0.5f * (Mathf.Max(Mathf.Abs(pos.x), Mathf.Abs(pos.y)) + pos.magnitude);

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


    private bool IsInsidePerlin(Vector2 pos)
    {
        pos = new Vector2(2 * (pos.y / width - 0.5f), 2 * (pos.y / height - 0.5f));
        float noise = Mathf.PerlinNoise(pos.x, pos.y);
        return (noise > (0.3f * (1 + pos.magnitude* pos.magnitude)));

    }

}
