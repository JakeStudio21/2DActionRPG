using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 2D 아이소메트릭 뷰 전역 설정값
/// </summary>
[CreateAssetMenu(fileName = "IsometricSettings", menuName = "Settings/Isometric Settings")]
public class IsometricSettings : ScriptableObject
{
    [Header("Grid 설정")]
    public Vector3 cellSize = new Vector3(1f, 1f, 0f);
    public Vector3 cellGap = Vector3.zero;
    public Grid.CellLayout cellLayout = Grid.CellLayout.IsometricZAsY;
    
    [Header("소팅 설정")]
    public Vector3 transparencySortAxis = new Vector3(0, 1, 0);
    
    [Header("카메라 설정")]
    public float cameraSize = 8f;
    public Vector3 cameraRotation = Vector3.zero; // 0, 0, 0 유지
    
    /// <summary>
    /// Unity Editor에서 프로젝트 설정 자동 적용
    /// </summary>
    [ContextMenu("Apply Isometric Settings")]
    public void ApplySettings()
    {
        #if UNITY_EDITOR
        // Transparency Sort 설정은 GraphicsSettings에 있지만 직접 접근할 수 없음
        // 수동으로 Project Settings → Graphics에서 설정해야 함
        
        // Grid 설정은 자동으로 적용 가능
        ApplyGridSettingsToScene();
        #endif
    }
    
    #if UNITY_EDITOR
    private void ApplyGridSettingsToScene()
    {
        Grid[] grids = FindObjectsOfType<Grid>();
        int updated = 0;
        
        foreach (Grid grid in grids)
        {
            if (grid.cellLayout != cellLayout)
            {
                UnityEditor.Undo.RecordObject(grid, "Apply Isometric Settings");
                grid.cellLayout = cellLayout;
                grid.cellSize = cellSize;
                grid.cellGap = cellGap;
                updated++;
                
                UnityEditor.EditorUtility.SetDirty(grid);
            }
        }
        
        if (updated > 0)
        {
        }
    }
    #endif
}
