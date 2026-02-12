using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyColor
{
    static HierarchyColor()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        // 이름에 "Panel" 포함되면 색 변경
        if (obj.name.Contains("Panel"))
        {
            EditorGUI.DrawRect(selectionRect, new Color(0.5f, 0.5f, 0.5f, 0.1f));
        }

        // 비활성 오브젝트는 빨간톤
        if (!obj.activeSelf)
        {
            EditorGUI.DrawRect(selectionRect, new Color(0f, 0f, 0.3f, 0.1f));
        }
    }
}