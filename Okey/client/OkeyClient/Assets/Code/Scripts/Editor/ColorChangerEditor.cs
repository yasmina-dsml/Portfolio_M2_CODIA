using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine;

[CustomEditor(typeof(ColorChanger))]
public class ColorChangerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Dessine l'inspecteur par défaut

        ColorChanger script = (ColorChanger)target;

        if (GUILayout.Button("Change Color"))
        {
            script.ChangeColor();
        }
    }
}
