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

    private Polygon centroids;
    private Rectangle rectangle;
    private TriangleNet.Mesh mesh;
    private List<TriangleNet.Topology.DCEL.Face> regions;
    private IslandShape islandShape;

    public void Generate()
    {
        Random.InitState(seed);
        rectangle = new Rectangle(0, 0, dimensions.x, dimensions.y);
        centroids = new Polygon();

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

        if (centroids.Count < 3)
            return;

        for (int i = 0; i < relaxationCount + 1; i++)
        {
            mesh = (TriangleNet.Mesh)centroids.Triangulate();
            StandardVoronoi voronoi = new StandardVoronoi(mesh, rectangle);
            regions = voronoi.Faces;

            if (relaxationCount != 0)
                centroids = voronoi.LloydRelaxation(rectangle);
        }
    }

    private void OnDrawGizmos()
    {
        string label = "Info: ";

        Gizmos.color = Color.red;
        if (centroids != null)
        {
            for (int i = 0; i < centroids.Points.Count; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(centroids.Points[i].ToVector(), 1f);
            }

            label += "\nRegions: " + centroids.Count;
        }

        if (regions != null)
        {
            Gizmos.color = Color.white;

            label += "\nFaces: " + regions.Count;

            foreach (var face in regions)
            {
                var edge = face.Edge;
                var first = edge.Origin.ID;

                face.LoopEdges(rectangle, clipEdges, (v1, v2) =>
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(v1, v2);

                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(v1, 0.6f);
                    Gizmos.DrawSphere(v2, 0.6f);
                });
                }

        }

        if (mesh != null && showDelaunayTriangles)
        {
            List<Vertex> vertices = new List<Vertex>();
            foreach (Vertex vertex in mesh.Vertices)
                vertices.Add(vertex);
            Gizmos.color = Color.black;

            foreach (Edge edge in mesh.Edges)
            {
                Vertex v0 = vertices[edge.P0];
                Vertex v1 = vertices[edge.P1];
                Gizmos.DrawLine(v0.ToVector(), v1.ToVector());
            }

            label += "\nMesh vertices: " + mesh.Vertices.Count;
            label += "\nMesh triangles: " + mesh.Triangles.Count;
            label += "\nMesh edges: " + mesh.NumberOfEdges;

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

        if (radius < 5)
            radius = 5;

        if (seed < 0)
            seed = 0;

        if (relaxationCount < 0)
            relaxationCount = 0;
    }
}
