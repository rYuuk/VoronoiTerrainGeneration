using UnityEngine;
using System.Collections.Generic;
using TriangleNet.Geometry;
using TriangleNet.Voronoi;
using System.Linq;
public class Map
{
    public int seed;
    public Rectangle bounds;
    public List<string> biomes;
    public List<Graph.Center> centers;
    public List<Graph.Corner> corners;
    public List<Graph.Edge> edges;

    public Map()
    {
        biomes = new List<string>();
        centers = new List<Graph.Center>();
        corners = new List<Graph.Corner>();
        edges = new List<Graph.Edge>();
    }
}

public class MapGenerator
{
    private Map data;
    private Rectangle rectangle;
    private IslandShape islandShape;
    private float height;
    private AnimationCurve heigtMap;

    public Map Generate(Vector2 dimensions, 
                        int seed, 
                        PointSelector.FaceType faceType, 
                        IslandShape.FunctionType functionType,
                        float height, 
                        AnimationCurve heightMap, 
                        int regionCount, 
                        int relaxationCount, 
                        float radius)
    {
        data = new Map();
        data.seed = seed;
        this.height = height;
        this.heigtMap = heightMap;
        islandShape = new IslandShape(seed, dimensions.x, dimensions.y, functionType);
        rectangle = new Rectangle(0, 0, dimensions.x, dimensions.y);
        if (faceType == PointSelector.FaceType.Hexagon || faceType == PointSelector.FaceType.Square)
            relaxationCount = 0;

        Polygon polygon = PointSelector.Generate(dimensions, seed, faceType, regionCount, radius);
        VoronoiBase voronoi = GenerateVoronoi(ref polygon, relaxationCount);
        Build(polygon, voronoi);
        ImproveCorners();
        // Determine the elevations and water at Voronoi corners.
        Elevation.AssignCorner(ref data, islandShape, faceType == PointSelector.FaceType.Hexagon || faceType == PointSelector.FaceType.Square);


        // Determine polygon and corner type: ocean, coast, land.
        Biomes.AssignOceanCoastAndLand(ref data);

        // Rescale elevations so that the highest is 1.0, and they're
        // distributed well. We want lower elevations to be more common
        // than higher elevations, in proportions approximately matching
        // concentric rings. That is, the lowest elevation is the
        // largest ring around the island, and therefore should more
        // land area than the highest elevation, which is the very
        // center of a perfectly circular island.

        List<Graph.Corner> corners = LandCorners(data.corners);
        Elevation.Redistribute(ref corners);

        // Assign elevations to non-land corners
        foreach (var q in data.corners)
        {
            if (q.ocean || q.coast)
                q.elevation = 0.0f;
        }

        // Polygon elevations are the average of their corners
        Elevation.AssignPolygon(ref data);

        // Determine moisture at corners, starting at rivers
        // and lakes, but not oceans. Then redistribute
        // moisture to cover the entire range evenly from 0.0
        // to 1.0. Then assign polygon moisture as the average
        // of the corner moisture.
        Moisture.AssignCorner(ref data);
        Moisture.Redistribute(ref corners);
        Moisture.AssignPolygon(ref data);

        Biomes.AssignBiomes(ref data);

        return data;
    }


    public MeshData GenerateMeshData()
    {
        MeshData meshData = new MeshData();

        foreach (var centre in data.centers)
        {
            List<Vector3> vertices = new List<Vector3>();
            centre.corners = centre.corners.OrderBy(x => Mathf.Atan2(centre.pos.y - x.pos.y, centre.pos.x - x.pos.x)).ToList();

            foreach (var corner in centre.corners)
                vertices.Add(new Vector3(corner.pos.x, corner.pos.y, - heigtMap.Evaluate(corner.elevation) * height * corner.elevation));


            meshData.vertices.AddRange(vertices);
            meshData.AddPolygon(vertices.Count, centre.biome);
        }

        return meshData;
    }


    private VoronoiBase GenerateVoronoi(ref Polygon polygon, int relaxationCount = 0)
    {
        if (polygon.Count < 3)
            return null;

        StandardVoronoi voronoi = null;
        for (int i = 0; i < relaxationCount + 1; i++)
        {
            TriangleNet.Mesh mesh = (TriangleNet.Mesh)polygon.Triangulate();
            data.bounds = mesh.Bounds;
            voronoi = new StandardVoronoi(mesh, rectangle);
            if (relaxationCount != 0)
                polygon = voronoi.LloydRelaxation(rectangle);
        }
        return voronoi;
    }

    private void AddToCornerList(List<Graph.Corner> corners, Graph.Corner corner)
    {
        if (corner != null && !corners.Contains(corner))
            corners.Add(corner);
    }

    private void AddToCenterList(List<Graph.Center> centers, Graph.Center center)
    {
        if (center != null && centers.IndexOf(center) < 0)
            centers.Add(center);

    }

    private Graph.Corner MakeCorner(Point point)
    {
        if (point == null)
            return null;
        for (int i = (int)(point.X) - 1; i <= (int)(point.X) + 1; i++)
        {
            if (_cornerMap.ContainsKey(i))
            {
                foreach (Graph.Corner corner in _cornerMap[i])
                {
                    float dx = (float)point.X - corner.pos.x;
                    float dy = (float)point.Y - corner.pos.y;
                    if (Mathf.Sqrt(dx * dx + dy * dy) < Mathf.Epsilon)
                        return corner;
                }
            }
        }

        int index = (int)point.X;
        if (!_cornerMap.ContainsKey(index) || _cornerMap[index] == null)
            _cornerMap[index] = new List<Graph.Corner>();
        Graph.Corner q = new Graph.Corner
        {
            id = data.corners.Count,
            pos = point.ToVector(),
            border = (point.X == 0 || point.X == rectangle.Width || point.Y == 0 || point.Y == rectangle.Height),
            touches = new List<Graph.Center>(),
            protrudes = new List<Graph.Edge>(),
            adjacent = new List<Graph.Corner>()
        };
        data.corners.Add(q);
        _cornerMap[index].Add(q);
        return q;
    }


    Dictionary<int, List<Graph.Corner>> _cornerMap = new Dictionary<int, List<Graph.Corner>>();

    private void Build(Polygon polygon, VoronoiBase voronoi)
    {
        Dictionary<Point, Graph.Center> centerLoopup = new Dictionary<Point, Graph.Center>();

        foreach (var point in polygon.Points)
        {
            Graph.Center center = new Graph.Center
            {
                id = data.centers.Count,
                pos = point.ToVector(),
                neighbours = new List<Graph.Center>(),
                borders = new List<Graph.Edge>(),
                corners = new List<Graph.Corner>()
            };
            data.centers.Add(center);
            centerLoopup[point] = center;
        }

        foreach (var face in voronoi.Faces)
        {
            face.LoopEdges(halfEdge =>
            {
                Point voronoiEdge1 = halfEdge.Origin;
                Point voronoiEdge2 = halfEdge.Twin.Origin;
                Point delaunayEdge1 = polygon.Points[halfEdge.Face.ID];
                Point delaunayEdge2 = polygon.Points[halfEdge.Twin.Face.ID];

                Graph.Edge edge = new Graph.Edge
                {
                    id = data.edges.Count,
                    midPoint = new Vector2((float)(voronoiEdge1.X + voronoiEdge2.X) / 2, (float)(voronoiEdge1.Y + voronoiEdge2.Y) / 2),
                    v0 = MakeCorner(voronoiEdge1),
                    v1 = MakeCorner(voronoiEdge2),
                    d0 = centerLoopup[delaunayEdge1],
                    d1 = centerLoopup[delaunayEdge2]
                };

                if (edge.d0 != null) { edge.d0.borders.Add(edge); }
                if (edge.d1 != null) { edge.d1.borders.Add(edge); }
                if (edge.v0 != null) { edge.v0.protrudes.Add(edge); }
                if (edge.v1 != null) { edge.v1.protrudes.Add(edge); }

                data.edges.Add(edge);

                // Centers point to centers.
                if (edge.d0 != null && edge.d1 != null)
                {
                    AddToCenterList(edge.d0.neighbours, edge.d1);
                    AddToCenterList(edge.d1.neighbours, edge.d0);
                }

                // Corners point to corners
                if (edge.v0 != null && edge.v1 != null)
                {
                    AddToCornerList(edge.v0.adjacent, edge.v1);
                    AddToCornerList(edge.v1.adjacent, edge.v0);
                }

                // Centers point to corners
                if (edge.d0 != null)
                {
                    AddToCornerList(edge.d0.corners, edge.v0);
                    AddToCornerList(edge.d0.corners, edge.v1);
                }
                if (edge.d1 != null)
                {
                    AddToCornerList(edge.d1.corners, edge.v0);
                    AddToCornerList(edge.d1.corners, edge.v1);
                }

                // Corners point to centers
                if (edge.v0 != null)
                {
                    AddToCenterList(edge.v0.touches, edge.d0);
                    AddToCenterList(edge.v0.touches, edge.d1);
                }
                if (edge.v1 != null)
                {
                    AddToCenterList(edge.v1.touches, edge.d0);
                    AddToCenterList(edge.v1.touches, edge.d1);
                }
            });
        }
    }

    // Although Lloyd relaxation improves the uniformity of polygon
    // sizes, it doesn't help with the edge lengths. Short edges can
    // be bad for some games, and lead to weird artifacts on
    // rivers. We can easily lengthen short edges by moving the
    // corners, but **we lose the Voronoi property**.  The corners are
    // moved to the average of the polygon centers around them. Short
    // edges become longer. Long edges tend to become shorter. The
    // polygons tend to be more uniform after this step.
    private void ImproveCorners()
    {
        Vector2[] newCorners = new Vector2[data.corners.Count];
        // First we compute the average of the centers next to each corner.
        foreach (var q in data.corners)
        {
            if (q.border)
            {
                newCorners[q.id] = q.pos;
            }
            else
            {
                Vector3 point = Vector2.zero;
                foreach (var r in q.touches)
                {
                    point.x += r.pos.x;
                    point.y += r.pos.y;
                }
                point.x /= q.touches.Count;
                point.y /= q.touches.Count;
                newCorners[q.id] = point;
            }
        }

        // Move the corners to the new locations.
        for (int i = 0; i < data.corners.Count; i++)
        {
            data.corners[i].pos = newCorners[i];
        }

        // The edge midpoints were computed for the old corners and need
        // to be recomputed.
        foreach (var edge in data.edges)
        {
            if (edge.v0 != null && edge.v1 != null)
            {
                edge.midPoint = (edge.v0.pos + edge.v1.pos) / 2;
            }
        }
    }

    // Create an array of corners that are on land only, for use by
    // algorithms that work only on land.  We return an array instead
    // of a vector because the redistribution algorithms want to sort
    // this array using Array.sortOn.
    public List<Graph.Corner> LandCorners(List<Graph.Corner> corners)
    {
        List<Graph.Corner> locations = new List<Graph.Corner>();
        foreach (var q in corners)
        {
            if (!q.ocean && !q.coast)
                locations.Add(q);
        }
        return locations;
    }
}
