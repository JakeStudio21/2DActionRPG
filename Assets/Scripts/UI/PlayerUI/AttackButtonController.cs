using UnityEngine;

/// <summary>
/// HUD 공격 버튼 컨트롤러
/// PlayerAnimationController를 통해 정상적인 공격 플로우 실행
/// </summary>
public class AttackButtonController : MonoBehaviour
{
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    /// <summary>
    /// HUD 공격 버튼 클릭 이벤트
    /// </summary>
    public void OnAttackButtonPressed()
    {
        if (showDebugLogs)
            Debug.Log("🎯 [AttackButtonController] 공격 버튼 클릭됨!");
        
        // ⭐ PlayerAnimationController를 통해 정상적인 공격 플로우 실행
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerAttack();
            
            if (showDebugLogs)
            {
                if (success)
                {
                    Debug.Log("🟢 [AttackButtonController] 공격 성공 (쿨다운 체크 + 애니메이션)");
                }
                else
                {
                    Debug.LogWarning("🟡 [AttackButtonController] 공격 실패 (쿨다운 중 또는 다른 액션 중)");
                }
            }
        }
        else
        {
            Debug.LogError("🔴 [AttackButtonController] PlayerAnimationController를 찾을 수 없습니다!");
        }
    }
} 