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
            Debug.Log("🟢 [PlayerAttackInput] A키 입력 감지됨!");
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
        Debug.Log("🔵 [PlayerAttackInput] PerformAttack() 시작");
        
        // PlayerAnimationController 우선 사용
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerAttack();
            
            if (success)
            {
                Debug.Log("🟢 [PlayerAttackInput] PlayerAnimationController 공격 성공!");
                return;
            }
            else
            {
                Debug.LogWarning("🟡 [PlayerAttackInput] PlayerAnimationController 공격 실패!");
            }
        }
        
        // 백업: ActiveWeapon 직접 호출
        var foundActiveWeapon = FindObjectOfType<ActiveWeapon>();
        if (foundActiveWeapon != null)
        {
            foundActiveWeapon.ExecuteWeaponAttack();  // ✅ PerformAttack() → ExecuteWeaponAttack()
            return;
        }
        
        var activeWeapon = ActiveWeapon.Instance;
        if (activeWeapon != null)
        {
            activeWeapon.ExecuteWeaponAttack();  // ✅ PerformAttack() → ExecuteWeaponAttack()
            Debug.Log("🟢 [PlayerAttackInput] ExecuteWeaponAttack() 호출 완료");
        }
        else
        {
            Debug.LogError("🔴 [PlayerAttackInput] ActiveWeapon을 찾을 수 없습니다!");
        }
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
} 