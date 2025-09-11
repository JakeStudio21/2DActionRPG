using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR && FALSE  // FALSE 추가로 비활성화
using UnityEditor;

/// <summary>
/// IsometricCharacterData Inspector 커스터마이징 (안전한 버전)
/// </summary>
[CustomPropertyDrawer(typeof(IsometricCharacterData))]
public class IsometricDataEditor : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        float currentY = position.y;
        float fullWidth = position.width;
        
        // 헤더 - 폴드아웃
        Rect foldoutRect = new Rect(position.x, currentY, fullWidth, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        currentY += EditorGUIUtility.singleLineHeight + 2;
        
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            
            // Direction Preset
            var directionProp = property.FindPropertyRelative("directionPreset");
            if (directionProp != null)
            {
                Rect directionRect = new Rect(position.x, currentY, fullWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(directionRect, directionProp);
                currentY += EditorGUIUtility.singleLineHeight + 2;
            }
            
            // Foot Offset
            var footOffsetProp = property.FindPropertyRelative("footOffset");
            if (footOffsetProp != null)
            {
                float footOffsetHeight = EditorGUI.GetPropertyHeight(footOffsetProp);
                Rect footOffsetRect = new Rect(position.x, currentY, fullWidth, footOffsetHeight);
                EditorGUI.PropertyField(footOffsetRect, footOffsetProp);
                currentY += footOffsetHeight + 2;
            }
            
            // Height Curve
            var heightCurveProp = property.FindPropertyRelative("heightCurve");
            if (heightCurveProp != null)
            {
                float curveHeight = EditorGUI.GetPropertyHeight(heightCurveProp);
                Rect heightCurveRect = new Rect(position.x, currentY, fullWidth, curveHeight);
                EditorGUI.PropertyField(heightCurveRect, heightCurveProp);
                currentY += curveHeight + 2;
            }
            
            // Height Amplitude  
            var heightAmplitudeProp = property.FindPropertyRelative("heightAmplitude");
            if (heightAmplitudeProp != null)
            {
                Rect heightAmplitudeRect = new Rect(position.x, currentY, fullWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.PropertyField(heightAmplitudeRect, heightAmplitudeProp);
                currentY += EditorGUIUtility.singleLineHeight + 2;
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight + 2; // 헤더
        
        if (property.isExpanded)
        {
            var directionProp = property.FindPropertyRelative("directionPreset");
            if (directionProp != null)
                totalHeight += EditorGUIUtility.singleLineHeight + 2;
            
            var footOffsetProp = property.FindPropertyRelative("footOffset");
            if (footOffsetProp != null)
                totalHeight += EditorGUI.GetPropertyHeight(footOffsetProp) + 2;
            
            var heightCurveProp = property.FindPropertyRelative("heightCurve");
            if (heightCurveProp != null)
                totalHeight += EditorGUI.GetPropertyHeight(heightCurveProp) + 2;
            
            var heightAmplitudeProp = property.FindPropertyRelative("heightAmplitude");
            if (heightAmplitudeProp != null)
                totalHeight += EditorGUIUtility.singleLineHeight + 2;
        }
        
        return totalHeight;
    }
}
#endif
