using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGeneratorExample))]
public class VoronoiDiagramEditor : Editor
{

    public override void OnInspectorGUI()
    {
        TerrainGeneratorExample voronoiDiagram = (TerrainGeneratorExample)target;
        
        if (DrawDefaultInspector())
        {
            if (voronoiDiagram.autoUpdate)
                voronoiDiagram.Generate();
        }

        if (GUILayout.Button("Generate"))
            voronoiDiagram.Generate();
    }
}
