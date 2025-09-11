#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// FootPositionSorter Inspector 커스터마이징
/// </summary>
[CustomEditor(typeof(FootPositionSorter))]
public class IsometricSortingEditor : Editor
{
    private FootPositionSorter sorter;
    
    private void OnEnable()
    {
        sorter = (FootPositionSorter)target;
    }
    
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 그리기
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        // 실시간 정보 표시
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("실시간 정보", EditorStyles.boldLabel);
            
            GUI.enabled = false;
            EditorGUILayout.IntField("현재 Sorting Order", sorter.GetCurrentSortingOrder());
            
            Vector3 footPos = sorter.transform.position; // 간단히 transform 사용
            EditorGUILayout.FloatField("발 위치 Y", footPos.y);
            
            float gridCellY = IsometricSorting.GetGridCellSizeY();
            EditorGUILayout.FloatField("Grid Cell Size Y", gridCellY);
            GUI.enabled = true;
        }
        
        EditorGUILayout.Space();
        
        // 유틸리티 버튼들
        EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);
        
        if (GUILayout.Button("즉시 소팅 업데이트"))
        {
            sorter.UpdateSortingOrderImmediate();
        }
        
        if (GUILayout.Button("소팅 초기화"))
        {
            sorter.ResetSorting();
        }
        
        if (Application.isPlaying && GUILayout.Button("소팅 정보 로그"))
        {
            Debug.Log($"📊 [SortingEditor] {sorter.name} 소팅 정보:");
            Debug.Log($"   - 현재 Order: {sorter.GetCurrentSortingOrder()}");
            Debug.Log($"   - Y 위치: {sorter.transform.position.y:F2}");
            Debug.Log($"   - Grid Cell Y: {IsometricSorting.GetGridCellSizeY()}");
        }
    }
}
#endif
