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
        
        // ⭐ D키: 스킬2 (통합 관리) - 새로 추가
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
        
        var activeWeapon = ActiveWeapon.Instance;
        if (activeWeapon == null)
        {
            Debug.LogError("🔴 [PlayerAttackInput] ActiveWeapon.Instance가 null입니다!");
            
            // 대안: FindObjectOfType으로 찾아보기
            var foundActiveWeapon = FindObjectOfType<ActiveWeapon>();
            if (foundActiveWeapon == null)
            {
                Debug.LogError("🔴 [PlayerAttackInput] FindObjectOfType<ActiveWeapon>()도 null입니다!");
            }
            else
            {
                Debug.Log("🟡 [PlayerAttackInput] FindObjectOfType으로 ActiveWeapon 찾음: " + foundActiveWeapon.name);
                foundActiveWeapon.PerformAttack();
            }
            return;
        }
        
        Debug.Log("🟢 [PlayerAttackInput] ActiveWeapon 찾음: " + activeWeapon.name);
        Debug.Log("🟢 [PlayerAttackInput] CurrentActiveWeapon: " + (activeWeapon.CurrentActiveWeapon != null ? activeWeapon.CurrentActiveWeapon.name : "NULL"));
        
        activeWeapon.PerformAttack();
        Debug.Log("🟢 [PlayerAttackInput] PerformAttack() 호출 완료");
    }

    /// <summary>
    /// 스킬1 실행
    /// </summary>
    private void PerformSkill()
    {
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
        {
            skillController.TriggerSkill();
            if (showDebugLogs) Debug.Log("[PlayerAttackInput] S키 스킬1 실행");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("[PlayerAttackInput] SkillController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 스킬2 실행
    /// </summary>
    private void PerformSkill2()
    {
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
        {
            // TODO: SkillController에 스킬2 메서드가 있다면 호출
            // skillController.TriggerSkill2();
            if (showDebugLogs) Debug.Log("[PlayerAttackInput] D키 스킬2 실행 (TODO: 구현 필요)");
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("[PlayerAttackInput] SkillController를 찾을 수 없습니다!");
        }
    }
} 