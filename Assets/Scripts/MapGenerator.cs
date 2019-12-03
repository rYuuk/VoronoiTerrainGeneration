using UnityEngine;
using System.Collections;
using TriangleNet.Topology.DCEL;
using System.Collections.Generic;
using TriangleNet.Geometry;

public class Map
{
    public Polygon polygon;
    public List<MapCell> cells;
    public Map(Polygon polygon, List<Cell> cells)
    {
        this.polygon = polygon;
        this.cells = new List<MapCell>();

        if (cells != null)
            cells.ForEach(cell => this.cells.Add(new MapCell(cell.face, cell.neighbours)));
    }
}

public class MapCell : Cell
{
    public bool isWater;
    public bool isBorder;
    public MapCell(Face face, List<int> neighbours) : base(face, neighbours)
    {
        this.face = face;
        this.neighbours = neighbours;
    }
}

public class MapGenerator
{

}
