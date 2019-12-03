using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Voronoi;
using TriangleNet.Tools;



public class VoronoiDiagram : MonoBehaviour
{
    public Vector2 dimensions = new Vector2(100, 100);
    public bool autoUpdate = false;
    public float radius = 10;
    public int seed = 1;
    public int relaxationCount = 2;
    public bool usePoisonDiscSampler = true;
    public int regionCount = 40;
    public bool clipEdges = false;
    public bool showDelaunayTriangles;
    public IslandShape.FunctionType islandFunction;
    private Rectangle rectangle;
    private IslandShape islandShape;

    private CellData cellData;

    public void Generate()
    {
        islandShape = new IslandShape(seed, dimensions.x, dimensions.y);
        Random.InitState(seed);
        rectangle = new Rectangle(0, 0, dimensions.x, dimensions.y);

        if (usePoisonDiscSampler)
            cellData = CellGenerator.Generate(seed, dimensions, radius, relaxationCount);
        else
            cellData = CellGenerator.Generate(seed, dimensions, regionCount, relaxationCount);
    }

    private void OnDrawGizmos()
    {
        string label = "Info: ";

        Gizmos.color = Color.red;

        if (cellData == null)
            return;

        if (cellData.cells != null && cellData.cells.Count != 0)
        {
            Gizmos.color = Color.white;

            label += "\nFaces: " + cellData.cells.Count;

            foreach (var cell in cellData.cells)
            {
                if (islandShape.IsInside(islandFunction, cellData.polygon.Points[cell.face.ID]))
                {
                    Gizmos.color = Color.green;
                }
                else
                    Gizmos.color = Color.blue;

                Gizmos.DrawSphere(cellData.polygon.Points[cell.face.ID].ToVector(), 1f);

                var edge = cell.face.Edge;
                var first = edge.Origin.ID;

                cell.face.LoopEdges(rectangle, clipEdges, (v1, v2) =>
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(v1, v2);

                    //Gizmos.color = Color.black;
                    //Gizmos.DrawSphere(v1, 0.6f);
                    //Gizmos.DrawSphere(v2, 0.6f);
                });

            }

        }

        if (cellData.mesh != null && showDelaunayTriangles)
        {
            List<Vertex> vertices = new List<Vertex>();
            foreach (Vertex vertex in cellData.mesh.Vertices)
                vertices.Add(vertex);
            Gizmos.color = Color.black;

            foreach (Edge edge in cellData.mesh.Edges)
            {
                Vertex v0 = vertices[edge.P0];
                Vertex v1 = vertices[edge.P1];
                Gizmos.DrawLine(v0.ToVector(), v1.ToVector());
            }

            label += "\nMesh vertices: " + cellData.mesh.Vertices.Count;
            label += "\nMesh triangles: " + cellData.mesh.Triangles.Count;
            label += "\nMesh edges: " + cellData.mesh.NumberOfEdges;

        }

        UnityEditor.Handles.Label(transform.position, label);

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
        regionCount = Mathf.Max(0, regionCount);
        radius = Mathf.Max(5, radius);
        relaxationCount = Mathf.Max(0, relaxationCount);
    }
}
