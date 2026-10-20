using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ResizableUIObject))]
[CanEditMultipleObjects]
public class ResizableUIEditor : Editor
{

    SerializedProperty resizeDirection;
    GUIContent resizeDirectionContent;

    SerializedProperty lockCorners;
    SerializedProperty lockCornersTopLeft;
    SerializedProperty lockCornersTopRight;
    SerializedProperty lockCornersBottomLeft;
    SerializedProperty lockCornersBottomRight;

    GUIContent cornerLockTopLeftContent;
    GUIContent cornerLockTopRightContent;
    GUIContent cornerLockBottomLeftContent;
    GUIContent cornerLockBottomRightContent;


    SerializedProperty minSizeBool;
    SerializedProperty minSizeX;
    SerializedProperty minSizeY;

    GUIContent minSizeXContent;
    GUIContent minSizeYContent;
    void OnEnable()
    {

        resizeDirection = serializedObject.FindProperty("_draggableDirection");
        resizeDirectionContent = new GUIContent
        {
            text = "Resize Directions",
            tooltip = "Select Resize Directions"
        };

        lockCorners = serializedObject.FindProperty("_lockCorners");
        lockCornersTopLeft = serializedObject.FindProperty("_lockCornerTopLeft");
        lockCornersTopRight = serializedObject.FindProperty("_lockCornerTopRight");
        lockCornersBottomLeft = serializedObject.FindProperty("_lockCornerBottomLeft");
        lockCornersBottomRight = serializedObject.FindProperty("_lockCornerBottomRight");

        cornerLockTopLeftContent = new GUIContent
        {
            text = "Top Left",
            tooltip = "Lock Top Left Corner"
        };
        cornerLockTopRightContent = new GUIContent
        {
            text = "Top Right",
            tooltip = "Lock Top Right Corner"
        };
        cornerLockBottomLeftContent = new GUIContent
        {
            text = "Bottom Left",
            tooltip = "Lock Bottom Left Corner"
        };
        cornerLockBottomRightContent = new GUIContent
        {
            text = "Bottom Right",
            tooltip = "Lock Bottom Right Corner"
        };


        minSizeBool = serializedObject.FindProperty("_minSize");
        minSizeX = serializedObject.FindProperty("_minSizeX");
        minSizeY = serializedObject.FindProperty("_minSizeY");
        minSizeXContent = new GUIContent
        {
            text = "X",
            tooltip = "Minimum Object Size X"
        };
        minSizeYContent = new GUIContent
        {
            text = "Y",
            tooltip = "Minimum Object Size Y"
        };

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(resizeDirection, resizeDirectionContent);

        EditorGUILayout.PropertyField(lockCorners);
        if (lockCorners.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lockCornersTopLeft, cornerLockTopLeftContent);
            EditorGUILayout.PropertyField(lockCornersTopRight, cornerLockTopRightContent);
            EditorGUILayout.PropertyField(lockCornersBottomLeft, cornerLockBottomLeftContent);
            EditorGUILayout.PropertyField(lockCornersBottomRight, cornerLockBottomRightContent);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        EditorGUILayout.PropertyField(minSizeBool);
        if (minSizeBool.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(minSizeX, minSizeXContent);
            EditorGUILayout.PropertyField(minSizeY, minSizeYContent);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        serializedObject.ApplyModifiedProperties();


    }

}
