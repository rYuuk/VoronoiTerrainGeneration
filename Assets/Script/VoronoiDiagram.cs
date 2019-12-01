using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Voronoi;
using TriangleNet.Tools;


public class VoronoiDiagram : MonoBehaviour
{
    public Vector2 dimensions;
    public int regionCount;
    public bool autoUpdate;
    public int seed;

    private Polygon centroids;
    private Rectangle area;
    private IEnumerable<IEdge> edges;
    private List<TriangleNet.Topology.DCEL.Vertex> vertices;
    private List<TriangleNet.Topology.DCEL.Face> regions;

    public void Generate()
    {
        Random.InitState(seed);
        centroids = new Polygon();

        for (int i = 0; i < regionCount; i++)
        {
            Vertex vertex = new Vertex(Random.Range(0, dimensions.x), Random.Range(0, dimensions.y));
            centroids.Add(vertex);
        }

        var mesh = (TriangleNet.Mesh)centroids.Triangulate();
        area = new Rectangle(0, 0, dimensions.x, dimensions.y);
        var voronoi = new StandardVoronoi(mesh, area);
        vertices = voronoi.Vertices;
        regions = voronoi.Faces;
        edges = voronoi.Edges;

        centroids = LloydRelaxation(voronoi);
    }
    /// <summary>
    /// Returns polygon with points at centroid
    /// Source: https://penetcedric.wordpress.com/2017/06/13/polygon-maps/
    /// </summary>
    /// <param name="voro">VoronoiBase such as StandardVoronoi or Bounded Voronoi</param>
    private Polygon LloydRelaxation(VoronoiBase voro)
    {
        //create the new polygon
        Polygon res = new Polygon(voro.Faces.Count);

        //loop through the regions
        for (int i = 0; i < regions.Count; ++i)
        {
            Vector2 newV = new Vector2(0, 0);

            //create the hash set of vertices -- this is neat as it will only contain 1 instance of each
            HashSet<Vector2> verts = new HashSet<Vector2>();

            var edge = regions[i].Edge;
            var first = edge.Origin.ID;

            do
            {
                //extract the vertices position
                Point p1 = new Point(edge.Origin.X, edge.Origin.Y);
                Point p2 = new Point(edge.Twin.Origin.X, edge.Twin.Origin.Y);

                //clip the edges
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

                //save the vertices
                verts.Add(VectorFromPoint(p1));
                verts.Add(VectorFromPoint(p2));

                edge = edge.Next;

            } while (edge != null && edge.Origin.ID != first);

            //compute the centroid
            var vertsEnum = verts.GetEnumerator();
            while (vertsEnum.MoveNext())
            {
                newV += vertsEnum.Current;
            }
            newV /= verts.Count;

            //insert back into the result polygon
            res.Add(new Vertex(newV.x, newV.y));
        }

        return res;
    }

    private Vector2 VectorFromPoint(Point point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (centroids != null)
        {
            for (int i = 0; i < centroids.Points.Count; i++)
            {
                Gizmos.DrawSphere(new Vector2((float)centroids.Points[i].X, (float)centroids.Points[i].Y), 1f);
            }
        }

        if (edges != null)
        {
            Gizmos.color = Color.white;

            foreach (var face in regions)
            {
                var edge = face.Edge;
                var first = edge.Origin.ID;

                do
                {
                    Vector2 origin = VectorFromPoint(edge.Origin);
                    Vector2 right = VectorFromPoint(edge.Twin.Origin);
                    Gizmos.DrawLine(origin, right);

                    edge = edge.Next;

                } while (edge != null && edge.Origin.ID != first);
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
        if(regionCount <= 2)
            regionCount = 3;
    }
}
