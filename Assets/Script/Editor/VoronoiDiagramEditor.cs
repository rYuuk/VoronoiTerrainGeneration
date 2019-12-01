using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VoronoiDiagram))]
public class VoronoiDiagramEditor : Editor
{

    public override void OnInspectorGUI()
    {
        VoronoiDiagram voronoiDiagram = (VoronoiDiagram)target;


        if (DrawDefaultInspector())
        {
            if (voronoiDiagram.autoUpdate)
                voronoiDiagram.Generate();
        }

        if (GUILayout.Button("Generate"))
            voronoiDiagram.Generate();
    }
}
