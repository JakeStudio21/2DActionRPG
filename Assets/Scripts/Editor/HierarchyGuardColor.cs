using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class HierarchyGuardColor
{
    // 표시할 태그 이름 (이 태그를 달면 색 표시)
    const string GuardTag = "MustBeDisabled";

    static HierarchyGuardColor()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect rect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        // 1) GuardTag가 달린 오브젝트가 "활성화" 상태면 빨강 표시
        if (obj.CompareTag(GuardTag) && obj.activeSelf)
        {
            EditorGUI.DrawRect(rect, new Color(0.5f, 0f, 0f, 0.1f)); // 빨강
        }

        // 2) GuardTag가 달린 오브젝트가 "비활성" 상태면 초록 표시
        if (obj.CompareTag(GuardTag) && !obj.activeSelf)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 1f, 0f, 0.15f)); // 초록
        }
    }
}
