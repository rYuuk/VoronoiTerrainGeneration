using UnityEngine;
using System.Collections;
using TriangleNet.Geometry;
using TriangleNet.Topology.DCEL;
using System.Collections.Generic;
using TriangleNet.Voronoi;
using Vertex = TriangleNet.Geometry.Vertex;

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

public enum CellType
{
    Random,
    Square,
    Hexagon,
    PoisonDisc
}

public class CellGenerator
{
    private Vector2 dimensions;
    private int seed;
    private int regionCount;
    private float radius;
    private int relaxationCount;
    private Rectangle rectangle;

    public CellGenerator(Vector2 dimensions, int seed, int relaxationCount = 0, int regionCount = 0, float radius = 0)
    {
        this.dimensions = dimensions;
        this.seed = seed;
        this.regionCount = regionCount;
        this.radius = radius;
        this.relaxationCount = relaxationCount;

        rectangle = new Rectangle(0, 0, dimensions.x, dimensions.y);
    }

    private Polygon GenerateRandom()
    {
        Polygon polygon = new Polygon();
        UnityEngine.Random.InitState(seed);
        for (int i = 0; i < regionCount; i++)
            polygon.Add(new Vertex(UnityEngine.Random.Range(0, dimensions.x), UnityEngine.Random.Range(0, dimensions.y)));
        return polygon;

    }

    private Polygon GeneratePoisonDisc()
    {
        Polygon polygon = new Polygon();
        UnityEngine.Random.InitState(seed);
        PoissonDiscSampler poissonDiscSampler = new PoissonDiscSampler(dimensions.x, dimensions.y, radius);
        foreach (var sample in poissonDiscSampler.Samples())
            polygon.Add(sample.ToVertex());
        return polygon;
    }

    private Polygon GenerateHexagon()
    {
        Polygon centroids = new Polygon();
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

    private Polygon GenerateSquare()
    {
        Polygon centroids = new Polygon();

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

    private CellData GenerateCellData(Polygon polygon, int relaxationCount = 0)
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
            List<int> neighbours = new List<int>();

            HalfEdge edge = face.Edge;
            int first = edge.Origin.ID;

            do
            {
                int id = edge.Twin.Face.ID;
                if (id != face.ID && !neighbours.Contains(id))
                    neighbours.Add(id);

                edge = edge.Next;
            } while (edge != null && edge.Origin.ID != first);

            cellData.cells.Add(new Cell(face, neighbours));
        }
        return cellData;
    }

    /// <summary>
    /// Generate cells with random points
    /// </summary>
    private CellData Random()
    {
        Polygon polygon = GenerateRandom();
        return GenerateCellData(polygon, relaxationCount);
    }

    /// <summary>
    /// Generate cell data using poison disc sampling
    /// </summary>
    private CellData PoisonDisc()
    {
        Polygon polygon = GeneratePoisonDisc();
        return GenerateCellData(polygon, relaxationCount);
    }

    /// <summary>
    /// Generate hexagon cells
    /// </summary>
    private CellData Hexagon()
    {
        Polygon polygon = GenerateHexagon();
        return GenerateCellData(polygon);

    }

    /// <summary>
    /// Generate square cells
    /// </summary>
    private CellData Square()
    {
        Polygon polygon = GenerateSquare();
        return GenerateCellData(polygon);

    }

    public CellData Generate(CellType cellType)
    {
        switch (cellType)
        {
            case CellType.Random:
                return Random();
            case CellType.Square:
                return Square();
            case CellType.Hexagon:
                return Hexagon();
            case CellType.PoisonDisc:
                return PoisonDisc();
            default:
                return null;
        }
    }
}
