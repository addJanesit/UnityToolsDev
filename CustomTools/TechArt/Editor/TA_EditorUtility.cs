using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public abstract class TA_EditorUtility : EditorWindow
{
    public const string _techArtToolsLable = "TechArtTools/";



    // Data for working in a scene
    public UnityEditor.Editor _myEditor;
    public Vector2 _scrollPos;

    public void Draw_Documentation(string linkToOpen, float width, float height)
    {
        if (GUILayout.Button("Open Documentation", GUILayout.Width(width), GUILayout.Height(height)))
        {
            Application.OpenURL(linkToOpen);
        }
    }

     public virtual void Draw_Header(string lastUpdate)
    {
        GUILayout.Label("\tIn-House: Asset Importer Tool", EditorStyles.whiteLargeLabel);
        GUILayout.Label("\t\tLast update: " + lastUpdate);
        GUILayout.Label("--------------------------------------------------");

        //Call "Draw_Documentation()"

        GUILayout.Label("--------------------------------------------------");
    }
}
