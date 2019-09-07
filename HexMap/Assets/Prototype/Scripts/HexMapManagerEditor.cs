using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexMapManager))]
public class HexMapManagerEditor : Editor
{
    private SerializedProperty versionProp, filePathProp, loadOnStartProp;

    private void OnEnable()
    {
        versionProp = serializedObject.FindProperty("mapVersion");
        filePathProp = serializedObject.FindProperty("mapFilePath");
        loadOnStartProp = serializedObject.FindProperty("loadOnStart");
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(versionProp);
        EditorGUILayout.PropertyField(loadOnStartProp);
        string path = filePathProp.stringValue;
        if (path == null)
            path = "";
        EditorGUILayout.LabelField("Map File Path: " + path);
        if(GUILayout.Button("Set Path"))
        {
            OpenLoadDialong();
        }
        serializedObject.ApplyModifiedProperties();
    }

    private void OpenLoadDialong()
    {
        string path = EditorUtility.OpenFilePanel("Load map file", Application.persistentDataPath, "map");
        filePathProp.stringValue = path;
    }
}
