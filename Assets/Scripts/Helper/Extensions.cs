using UnityEngine;
using System.Collections;
using TriangleNet.Geometry;
using TriangleNet.Tools;
using TriangleNet.Voronoi;
using System.Collections.Generic;
using TriangleNet.Topology.DCEL;
using Vertex = TriangleNet.Geometry.Vertex;

public static class Extensions
{
    public static Vector2 ToVector(this Point point)
    {
        return new Vector2((float)point.X, (float)point.Y);
    }

    public static Vertex ToVertex(this Vector2 vector)
    {
        return new Vertex(vector.x, vector.y);
    }

    /// <summary>
    /// Returns polygon with points at centroid
    /// Source: https://penetcedric.wordpress.com/2017/06/13/polygon-maps/
    /// </summary>
    /// <param name="voronoi">VoronoiBase such as StandardVoronoi or Bounded Voronoi</param>
    public static Polygon LloydRelaxation(this VoronoiBase voronoi, Rectangle rectangle)
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

            voronoi.Faces[i].LoopEdges(rectangle, true, (v1, v2) =>
            {
                if (!verts.Contains(v1))
                    verts.Add(v1);
                if (!verts.Contains(v2))
                    verts.Add(v2);
            });

            if (verts.Count == 0)
                continue;

            //compute the centroid
            var vertsEnum = verts.GetEnumerator();
            while (vertsEnum.MoveNext())
                average += vertsEnum.Current;
            average /= verts.Count;
            centroid.Add(average.ToVertex());
        }
        return centroid;
    }


    public static void LoopEdges(this Face face, Rectangle rectangle, bool clipEdges, System.Action<Vector2, Vector2> OnEdge)
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
                if ((rectangle.Contains(p1) && !rectangle.Contains(p2)))
                {
                    IntersectionHelper.BoxRayIntersection(rectangle, p1, p2, ref p2); //case 1
                }
                else if (!rectangle.Contains(p1) && rectangle.Contains(p2))
                {
                    IntersectionHelper.BoxRayIntersection(rectangle, p2, p1, ref p1); //case 2
                }
                else if (!rectangle.Contains(p1) && !rectangle.Contains(p2)) //case 3
                {
                    edge = edge.Next;
                    continue;
                }
            }

            Vector2 origin = p1.ToVector();
            Vector2 end = p2.ToVector();

            OnEdge?.Invoke(origin, end);
            edge = edge.Next;
        } while (edge != null && edge.Origin.ID != first);
    }

    public static void LoopEdges(this Face face, System.Action<HalfEdge> OnEdge)
    {
        var edge = face.Edge;
        var first = edge.Origin.ID;
        do
        {
            OnEdge?.Invoke(edge);
            edge = edge.Next;
        } while (edge != null && edge.Origin.ID != first);
    }


}
