using UnityEngine;

/// <summary>
/// 플레이어 키보드 입력 통합 관리자 (A키 공격 + S키 스킬1 + D키 스킬2)
/// </summary>
public class PlayerAttackInput : MonoBehaviour
{
    [Header("입력 설정")]
    [SerializeField] private bool enableKeyboardInput = true;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true; // ⭐ true로 변경
    
    // 🔍 키 입력 추적
    private static int aKeyPressCount = 0;

    void Start()
    {
        Debug.Log("🔵 [PlayerAttackInput] 컴포넌트 시작됨! enableKeyboardInput: " + enableKeyboardInput);
    }

    void Update()
    {
        if (!enableKeyboardInput) 
        {
            // 키보드 입력이 비활성화된 경우 (1초마다 로그)
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning("🟡 [PlayerAttackInput] 키보드 입력이 비활성화되어 있습니다!");
            }
            return;
        }
        
        // ⭐ A키: 기본공격 (통합 관리)
        if (Input.GetKeyDown(KeyCode.A))
        {
            aKeyPressCount++;
            Debug.Log($"🔥🔥🔥 [PlayerAttackInput] A키 입력 #{aKeyPressCount} 감지됨! 🔥🔥🔥");
            Debug.Log($"   - 현재 시간: {Time.time:F3}");
            Debug.Log($"   - enableKeyboardInput: {enableKeyboardInput}");
            Debug.Log($"   - A키 누른 총 횟수: {aKeyPressCount}");
            PerformAttack();
        }
        
        // ⭐ S키: 스킬1 (통합 관리)
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("🟢 [PlayerAttackInput] S키 입력 감지됨!");
            PerformSkill();
        }
        
        // ⭐ D키: 스킬2 (통합 관리) - 새로 활성화
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("🟢 [PlayerAttackInput] D키 입력 감지됨!");
            PerformSkill2();
        }
    }

    /// <summary>
    /// 기본공격 실행
    /// </summary>
    private void PerformAttack()
    {
        Debug.Log($"🔵 [PlayerAttackInput] PerformAttack() 시작 (A키 입력 #{aKeyPressCount})");
        
        // PlayerAnimationController 우선 사용
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            Debug.Log($"🎯 [PlayerAttackInput] TriggerAttack() 호출 시작 (A키 #{aKeyPressCount})");
            bool success = playerAnimationController.TriggerAttack();
            Debug.Log($"🎯 [PlayerAttackInput] TriggerAttack() 호출 완료 (A키 #{aKeyPressCount})");
            
            if (success)
            {
                Debug.Log($"🟢 [PlayerAttackInput] PlayerAnimationController 공격 성공! (A키 #{aKeyPressCount})");
            }
            else
            {
                Debug.LogWarning($"🟡 [PlayerAttackInput] PlayerAnimationController 공격 실패! (A키 #{aKeyPressCount}) (쿨다운 중)");
            }
            
            return; // ⭐ 성공/실패와 관계없이 여기서 종료
        }
        
        // ❌ 백업 시스템 완전 제거 (77-95번 라인 모두 삭제)
        Debug.LogError($"🔴 [PlayerAttackInput] PlayerAnimationController를 찾을 수 없습니다! (A키 #{aKeyPressCount})");
    }

    /// <summary>
    /// 스킬1 실행
    /// </summary>
    private void PerformSkill()
    {
        Debug.Log("🔵 [PlayerAttackInput] PerformSkill() 시작");
        
        // ⭐ PlayerAnimationController 우선 사용
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill1();
            
            if (success)
            {
                Debug.Log("🟢 [PlayerAttackInput] PlayerAnimationController 스킬1 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [PlayerAttackInput] PlayerAnimationController 스킬1 실패!");
            }
            
            return;
        }
        
        // ⭐ 기존 방식 (fallback) - PlayerAnimationController가 없을 때
        Debug.LogWarning("🟡 [PlayerAttackInput] PlayerAnimationController 없음 - 기존 방식 사용");
        
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
        {
            skillController.TriggerSkill();
            if (showDebugLogs) Debug.Log("[PlayerAttackInput] S키 스킬1 실행 (기존 방식)");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("[PlayerAttackInput] SkillController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 스킬2 실행 - ⭐ 완전 구현
    /// </summary>
    private void PerformSkill2()
    {
        Debug.Log("🔵 [PlayerAttackInput] PerformSkill2() 시작");
        
        // ⭐ PlayerAnimationController 우선 사용 (스킬1과 동일한 패턴)
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill2();
            
            if (success)
            {
                Debug.Log("🟢 [PlayerAttackInput] PlayerAnimationController 스킬2 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [PlayerAttackInput] PlayerAnimationController 스킬2 실패!");
            }
            
            return;
        }
        
        // ⭐ 기존 방식 (fallback) - PlayerAnimationController가 없을 때
        Debug.LogWarning("🟡 [PlayerAttackInput] PlayerAnimationController 없음 - 기존 방식 사용");
        
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
        {
            skillController.TriggerSkill2(); // ⭐ TODO 제거하고 실제 구현
            if (showDebugLogs) Debug.Log("[PlayerAttackInput] D키 스킬2 실행 (기존 방식)");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("[PlayerAttackInput] SkillController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 대시 실행
    /// </summary>
    private void PerformDash()
    {
        if (showDebugLogs) Debug.Log("[PlayerAttackInput] 대시 실행 시도");
        
        // PlayerController의 Dash 메서드 직접 호출
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            // PlayerController의 Dash 메서드를 public으로 만들어야 함
            playerController.PerformDash();
            if (showDebugLogs) Debug.Log("🟢 [PlayerAttackInput] PlayerController 대시 성공!");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("🟡 [PlayerAttackInput] PlayerController를 찾을 수 없습니다!");
        }
    }
} 