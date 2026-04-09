using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DynamicJoystick))]
public class DynamicJoystickEditor : JoystickEditor
{
    private SerializedProperty moveThreshold;
    private SerializedProperty slideMultiplier;
    private SerializedProperty maxSlideDistance;

    protected override void OnEnable()
    {
        base.OnEnable();
        moveThreshold = serializedObject.FindProperty("moveThreshold");
        slideMultiplier = serializedObject.FindProperty("slideMultiplier");
        maxSlideDistance = serializedObject.FindProperty("maxSlideDistance");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (background != null)
        {
            RectTransform backgroundRect = (RectTransform)background.objectReferenceValue;
            backgroundRect.anchorMax = Vector2.zero;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.pivot = center;
        }
    }

    protected override void DrawValues()
    {
        base.DrawValues();
        EditorGUILayout.PropertyField(moveThreshold, new GUIContent("Move Threshold", "The distance away from the center input has to be before the joystick begins to move."));
        EditorGUILayout.PropertyField(slideMultiplier, new GUIContent("Slide Multiplier", "Scales how fast the background slides. 0 = fixed, 1 = original dynamic behavior."));
        EditorGUILayout.PropertyField(maxSlideDistance, new GUIContent("Max Slide Distance", "Maximum distance (pixels) the background can move from the initial touch position."));
    }
}