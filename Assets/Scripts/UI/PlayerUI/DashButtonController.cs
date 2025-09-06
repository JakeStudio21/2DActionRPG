using UnityEngine;

/// <summary>
/// 대시 버튼 UI 컨트롤러 - AttackButtonController와 동일한 패턴
/// </summary>
public class DashButtonController : MonoBehaviour
{
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    /// <summary>
    /// 대시 버튼이 클릭되었을 때 호출될 공용 메서드
    /// </summary>
    public void OnButtonClick()
    {
        if (showDebugLogs) Debug.Log("[DashButtonController] 대시 버튼 클릭됨!");
        
        // PlayerController의 PerformDash 메서드 호출
        var playerController = FindObjectOfType<PlayerController>();  // 변경: PlayerController.Instance → FindObjectOfType<PlayerController>()
        if (playerController != null)
        {
            playerController.PerformDash();
            if (showDebugLogs) Debug.Log("[DashButtonController] 대시 실행 성공!");
        }
        else
        {
            Debug.LogWarning("[DashButtonController] PlayerController를 찾을 수 없습니다!");
        }
    }
}
