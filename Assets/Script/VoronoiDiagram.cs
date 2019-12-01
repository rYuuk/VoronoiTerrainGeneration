using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Voronoi;
using TriangleNet.Tools;


public class VoronoiDiagram : MonoBehaviour
{
    public Vector2 dimensions = new Vector2(100, 100);
    public int regionCount = 10;
    public bool autoUpdate = false;
    public int seed = 1;
    public int relaxationCount = 2;
    public bool clipEdges = false;

    private Polygon centroids;
    private Rectangle area;
    private List<TriangleNet.Topology.DCEL.Face> regions;
    private StandardVoronoi voronoi;

    public void Generate()
    {
        Random.InitState(seed);
        centroids = new Polygon();

        for (int i = 0; i < regionCount; i++)
        {
            Vertex vertex = new Vertex(Random.Range(0, dimensions.x), Random.Range(0, dimensions.y));
            centroids.Add(vertex);
        }

        area = new Rectangle(0, 0, dimensions.x, dimensions.y);

        TriangleNet.Meshing.ConstraintOptions constraintOptions = new TriangleNet.Meshing.ConstraintOptions
        {
            //ConformingDelaunay = true,
            Convex = true
        };

        TriangleNet.Mesh mesh = (TriangleNet.Mesh)centroids.Triangulate(constraintOptions);
        voronoi = new StandardVoronoi(mesh, area);
        regions = voronoi.Faces;

        for (int i = 0; i < relaxationCount; i++)
        {
            centroids = LloydRelaxation(voronoi);
            Debug.Log("i: " + i + centroids.Count);
            mesh = (TriangleNet.Mesh)centroids.Triangulate(constraintOptions);
            voronoi = new StandardVoronoi(mesh, area);
            regions = voronoi.Faces;
        }
    }

    /// <summary>
    /// Returns polygon with points at centroid
    /// Source: https://penetcedric.wordpress.com/2017/06/13/polygon-maps/
    /// </summary>
    /// <param name="voronoi">VoronoiBase such as StandardVoronoi or Bounded Voronoi</param>
    private Polygon LloydRelaxation(VoronoiBase voronoi)
    {
        //create the new polygon
        Polygon res = new Polygon(voronoi.Faces.Count);

        //loop through the regions
        for (int i = 0; i < regions.Count; ++i)
        {
            Vector2 sumVector = new Vector2(0, 0);

            //create the hash set of vertices -- this is neat as it will only contain 1 instance of each
            HashSet<Vector2> verts = new HashSet<Vector2>();

            var edge = regions[i].Edge;
            var first = edge.Origin.ID;

            IterateEdgesOfFace(regions[i], true, (v1, v2) => {
                verts.Add(v1);
                verts.Add(v2);
            });

            //compute the centroid
            var vertsEnum = verts.GetEnumerator();
            while (vertsEnum.MoveNext())
            {
                sumVector += vertsEnum.Current;
            }
            sumVector /= verts.Count;

            //insert back into the result polygon
            res.Add(new Vertex(sumVector.x, sumVector.y));
        }

        return res;
    }

    private Vector2 VectorFromPoint(Point point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }

    private void IterateEdgesOfFace(TriangleNet.Topology.DCEL.Face face, bool clipEdges, System.Action<Vector2, Vector2> OnEdge)
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
                    IntersectionHelper.BoxRayIntersection(area, p1, p2, ref p2); //case 2
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
        Gizmos.color = Color.red;
        if (centroids != null)
        {
            for (int i = 0; i < centroids.Points.Count; i++)
                Gizmos.DrawSphere(new Vector2((float)centroids.Points[i].X, (float)centroids.Points[i].Y), 1f);
        }

        if (regions != null)
        {
            Gizmos.color = Color.white;

            foreach (var face in regions)
            {
                var edge = face.Edge;
                var first = edge.Origin.ID;

                IterateEdgesOfFace(face, clipEdges, (v1,v2) => Gizmos.DrawLine(v1, v2));
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector2(0, 0), new Vector2(0, dimensions.y));
            Gizmos.DrawLine(new Vector2(0, 0), new Vector2(dimensions.x, 0));
            Gizmos.DrawLine(new Vector2(dimensions.x, 0), dimensions);
            Gizmos.DrawLine(new Vector2(0, dimensions.y), dimensions);

        }
    }

    public void OnValidate()
    {
        if (regionCount <= 2)
            regionCount = 3;

        if (relaxationCount < 0)
            relaxationCount = 0;
    }
}
