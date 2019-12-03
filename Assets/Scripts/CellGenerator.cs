using UnityEngine;
using System.Collections;
using TriangleNet.Geometry;
using TriangleNet.Topology.DCEL;
using System.Collections.Generic;
using TriangleNet.Voronoi;
using Vertex = TriangleNet.Geometry.Vertex;
using TriangleNet;

public class Cell
{
    public Face face;
    public List<int> neighbours;

    public Cell(Face face, List<int> neighbours)
    {
        this.face = face;
        this.neighbours = neighbours;
    }
}

public class CellData
{
    public Polygon polygon;
    public List<Cell> cells;
    public TriangleNet.Mesh mesh;

}

public static class CellGenerator
{
    private static Polygon GeneratePolygon(int seed, Vector2 dimensions, bool usePoisonDiscSampler, int regionCount, float radius)
    {
        Polygon centroids = new Polygon();
        Random.InitState(seed);

        if (usePoisonDiscSampler)
        {
            PoissonDiscSampler poissonDiscSampler = new PoissonDiscSampler(dimensions.x, dimensions.y, radius);
            foreach (var sample in poissonDiscSampler.Samples())
                centroids.Add(sample.ToVertex());
        }
        else
        {
            for (int i = 0; i < regionCount; i++)
                centroids.Add(new Vertex(Random.Range(0, dimensions.x), Random.Range(0, dimensions.y)));
        }

        return centroids;
    }

    private static CellData GenerateCellData(Rectangle rectangle, Polygon polygon, int relaxationCount)
    {
        CellData cellData = new CellData();

        if (polygon.Count < 3)
            return cellData;

        TriangleNet.Mesh mesh = null;
        List<Face> faces = new List<Face>();

        for (int i = 0; i < relaxationCount + 1; i++)
        {
            mesh = (TriangleNet.Mesh)polygon.Triangulate();
            StandardVoronoi voronoi = new StandardVoronoi(mesh, rectangle);
            faces = voronoi.Faces;

            if (relaxationCount != 0)
                polygon = voronoi.LloydRelaxation(rectangle);
        }

        cellData.mesh = mesh;
        cellData.polygon = polygon;
        cellData.cells = new List<Cell>();

        foreach (Face face in faces)
        {
            cellData.cells.Add(new Cell(face, new List<int>()));
        }
        return cellData;
    }


    /// <summary>
    /// Generate cell data using poison disc sampling
    /// </summary>
    public static CellData Generate(int seed, Vector2 dimensions, int regionCount, int relaxationCount)
    {
        Rectangle rectangle = new Rectangle(0, 0, dimensions.x, dimensions.y);
        Polygon polygon = GeneratePolygon(seed, dimensions, false,  regionCount, 0);
        return GenerateCellData(rectangle, polygon, relaxationCount);
    }

    /// <summary>
    /// Generate cell data using poison disc sampling
    /// </summary>
    public static CellData Generate(int seed, Vector2 dimensions, float radius, int relaxationCount)
    {
        Rectangle rectangle = new Rectangle(0, 0, dimensions.x, dimensions.y);
        Polygon polygon = GeneratePolygon(seed, dimensions, true,  0, radius);
        return GenerateCellData(rectangle, polygon, relaxationCount);

    }
}
