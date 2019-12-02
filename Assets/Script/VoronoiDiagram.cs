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

    [Header("Sampling")]
    public bool usePoisonDiscSampler = true;

    public int regionCount = 40;
    
    public bool clipEdges = false;
    public bool showDelaunayTriangles;

    private Polygon centroids;
    private Rectangle area;
    private TriangleNet.Mesh mesh;
    private List<TriangleNet.Topology.DCEL.Face> regions;

    public void Generate()
    {
        Random.InitState(seed);
        area = new Rectangle(0, 0, dimensions.x, dimensions.y);
        Polygon polygons = new Polygon();

        if (usePoisonDiscSampler)
        {
            PoissonDiscSampler poissonDiscSampler = new PoissonDiscSampler(dimensions.x, dimensions.y, radius);
            foreach (var sample in poissonDiscSampler.Samples())
                polygons.Add(VertexFromVector(sample));
        }
        else
        {
            for (int i = 0; i < regionCount; i++)
                polygons.Add(new Vertex(Random.Range(0, dimensions.x), Random.Range(0, dimensions.y)
                    
                    
                    
                    
                    
                    
                    
                    
                           
                    ));
        }

        if (polygons.Count < 3)
            return;

        for (int i = 0; i < relaxationCount + 1; i++)
        {
            mesh = (TriangleNet.Mesh)polygons.Triangulate();
            StandardVoronoi voronoi = new StandardVoronoi(mesh, area);
            regions = voronoi.Faces;

            if (relaxationCount != 0)
                polygons = LloydRelaxation(voronoi);

        }
        
        centroids = polygons;
    }

    /// <summary>
    /// Returns polygon with points at centroid
    /// Source: https://penetcedric.wordpress.com/2017/06/13/polygon-maps/
    /// </summary>
    /// <param name="voronoi">VoronoiBase such as StandardVoronoi or Bounded Voronoi</param>
    private Polygon LloydRelaxation(VoronoiBase voronoi)
    {
        //create the new polygon
        Polygon centroid = new Polygon(voronoi.Faces.Count);

        //loop through the regions
        for (int i = 0; i < voronoi.Faces.Count; ++i)
        {
            Vector2 average = new Vector2(0, 0);

            //create the hash set of vertices -- this is neat as it will only contain 1 instance of each
            HashSet<Vector2> verts = new HashSet<Vector2>();

            var edge = voronoi.Faces[i].Edge;
            var first = edge.Origin.ID;

            EdgesOfFaceIterator(voronoi.Faces[i], true, (v1, v2) =>
            {
                verts.Add(v1);
                verts.Add(v2);
            });

            //compute the centroid
            var vertsEnum = verts.GetEnumerator();
            while (vertsEnum.MoveNext())
                average += vertsEnum.Current;
            average /= verts.Count;

            //insert back into the result polygon
            centroid.Add(VertexFromVector(average));
        }

        return centroid;
    }

    private Vector2 VectorFromPoint(Point point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }

    private Vertex VertexFromVector(Vector2 vector)
    {
        return new Vertex(vector.x, vector.y);
    }


    private void EdgesOfFaceIterator(TriangleNet.Topology.DCEL.Face face, bool clipEdges, System.Action<Vector2, Vector2> OnEdge)
    {
        var edge = face.Edge;
        var first = edge.Origin.ID;
        do
        {
            //extract the vertices position
            Point p1 = new Point(edge.Origin.X, edge.Origin.Y);
            Point p2 = new Point(edge.Twin.Origin.X, edge.Twin.Origin.Y);

            if (clipEdges)
            {
                if ((area.Contains(p1) && !area.Contains(p2)))
                {
                    IntersectionHelper.BoxRayIntersection(area, p1, p2, ref p2); //case 1
                }
                else if (!area.Contains(p1) && area.Contains(p2))
                {
                    IntersectionHelper.BoxRayIntersection(area, p2, p1, ref p1); //case 2
                }
                else if (!area.Contains(p1) && !area.Contains(p2)) //case 3
                {
                    edge = edge.Next;
                    continue;
                }
            }

            Vector2 origin = VectorFromPoint(p1);
            Vector2 end = VectorFromPoint(p2);

            OnEdge?.Invoke(origin, end);
            edge = edge.Next;
        } while (edge != null && edge.Origin.ID != first);
    }

    private void OnDrawGizmos()
    {
        string label = "Info: ";

        Gizmos.color = Color.red;
        if (centroids != null)
        {
            for (int i = 0; i < centroids.Points.Count; i++)
                Gizmos.DrawSphere(VectorFromPoint(centroids.Points[i]), 1f);

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

                EdgesOfFaceIterator(face, clipEdges, (v1, v2) =>
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
                Gizmos.DrawLine(VectorFromPoint(v0), VectorFromPoint(v1));
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
       dimensions = new Vector2(Mathf.Max(0,dimensions.x), Mathf.Max(0,dimensions.y));

        if (radius < 5)
            radius = 5;

        if (seed < 0)
            seed = 0;

        if (relaxationCount < 0)
            relaxationCount = 0;
    }
}
