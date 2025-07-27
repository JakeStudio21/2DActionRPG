using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// 🤖 JSON 파일 변경 감지 및 자동 갱신 (완전 비활성화)
/// </summary>
public class EquipmentFileWatcher
{
    // 🔒 모든 기능 비활성화 (메모리 경고 방지)
    
    [MenuItem("Tools/Equipment Manager/Manual Refresh Only")]
    private static void ManualRefreshOnly()
    {
        try
        {
            AssetDatabase.Refresh();
            
            var windows = Resources.FindObjectsOfTypeAll<EquipmentDataManager>();
            if (windows != null && windows.Length > 0)
            {
                foreach (var window in windows)
                {
                    if (window != null)
                    {
                        window.Reset(); // 안전한 리셋만 수행
                    }
                }
                Debug.Log("✅ [Manual] EquipmentDataManager 수동 새로고침 완료");
            }
            else
            {
                Debug.Log("📭 [Manual] 열린 EquipmentDataManager 창이 없습니다");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [Manual] 수동 새로고침 실패: {ex.Message}");
        }
    }
    
    [MenuItem("Tools/Equipment Manager/Force Close All")]
    private static void ForceCloseAllWindows()
    {
        var windows = Resources.FindObjectsOfTypeAll<EquipmentDataManager>();
        if (windows != null)
        {
            foreach (var window in windows)
            {
                if (window != null)
                {
                    window.Close();
                }
            }
            Debug.Log($"🔒 [Force] {windows.Length}개 EquipmentDataManager 창 강제 종료");
        }
        
        // 가비지 컬렉션 강제 실행
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
        
        Debug.Log("🧹 [Force] 메모리 정리 완료");
    }
}
