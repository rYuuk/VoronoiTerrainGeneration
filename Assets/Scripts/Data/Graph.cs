using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Graph
{
    public class Corner
    {
        public int id;
        public Vector3 pos;
        public bool border;
        public bool water;
        public bool ocean;
        public bool coast;
        public int river;
        public float elevation;
        public float moisture;
        public List<Corner> adjacent;
        public List<Center> touches;
        public List<Edge> protrudes;
    }

    public class Center
    {
        public int id;
        public Vector3 pos;
        public bool border;
        public bool ocean;
        public bool water;
        public bool coast;
        public float elevation;
        public float moisture;
        public string biome;
        public List<Center> neighbours;
        public List<Edge> borders;
        public List<Corner> corners;

        public List<Vector3> vertices;
        public List<int> triangles;
    }

    public class Edge
    {
        public int id;
        public Center d0, d1;
        public Corner v0, v1;
        public Vector3 midPoint;
    }
}