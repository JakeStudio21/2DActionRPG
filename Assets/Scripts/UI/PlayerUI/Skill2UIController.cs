using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬2 UI 컨트롤러 - 스킬2 쿨다운 및 버튼 관리
/// </summary>
public class Skill2UIController : MonoBehaviour
{
    public Image cooldownImage;
    [Tooltip("스킬2 쿨다운 시간(초) - 여기서만 입력!")]
    public float cooldownTime = 3f; // 스킬2는 기본 3초 쿨다운 (스킬1보다 길게)
    
    private SkillController skillController;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f; // UI 업데이트 간격 (0.1초)

    void Start()
    {
        Debug.Log("[Skill2UI] Skill2UIController 초기화 시작");
        InitializeSkillController();
        ValidateComponents();
    }

    void Update()
    {
        // UI 업데이트를 0.1초마다만 실행하여 성능 최적화
        if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
        {
            UpdateCooldownUI();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// SkillController 초기화 및 쿨다운 시간 동기화
    /// </summary>
    private void InitializeSkillController()
    {
        skillController = FindObjectOfType<SkillController>();
        if (skillController == null)
        {
            Debug.LogWarning("[Skill2UI] SkillController를 찾지 못함! 씬에 SkillController가 있는지 확인하세요.");
            return;
        }

        Debug.Log("[Skill2UI] SkillController를 정상적으로 찾음!");
        
        // SkillController의 스킬2 쿨다운 시간은 항상 UI에서 입력한 값으로 동기화
        skillController.skill2CooldownTime = cooldownTime;
        Debug.Log($"[Skill2UI] 스킬2 쿨다운 시간 동기화 완료: {cooldownTime}초");
    }

    /// <summary>
    /// 필수 컴포넌트들이 올바르게 할당되었는지 검증
    /// </summary>
    private void ValidateComponents()
    {
        if (cooldownImage == null)
        {
            Debug.LogWarning("[Skill2UI] CooldownImage가 할당되지 않았습니다! Inspector에서 할당해주세요.");
        }
        else
        {
            Debug.Log("[Skill2UI] 모든 컴포넌트가 정상적으로 할당됨");
        }
    }

    /// <summary>
    /// 스킬2 쿨다운 UI 업데이트 (성능 최적화됨)
    /// </summary>
    private void UpdateCooldownUI()
    {
        if (skillController != null && cooldownImage != null)
        {
            float remain = skillController.GetSkill2CooldownRemaining();
            float newFillAmount = remain / skillController.Skill2CooldownTime;
            
            // fillAmount가 실제로 변경되었을 때만 업데이트
            if (Mathf.Abs(cooldownImage.fillAmount - newFillAmount) > 0.01f)
            {
                cooldownImage.fillAmount = newFillAmount;
            }
        }
    }

    /// <summary>
    /// 스킬2 버튼이 클릭되었을 때 호출되는 공용 메서드
    /// ⭐ 신규: SkillController.SkillSet으로 직접 연결 (PlayerAnimationController 완전 우회)
    /// </summary>
    public void OnSkill2ButtonPressed()
    {
        Debug.Log("🔥 [Skill2UI] 스킬2 버튼 클릭! - SkillSet 직접 실행");
        
        // ⭐ SkillController.SkillSet으로 직접 연결
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
        {
            // SkillSet을 통한 직접 실행 (PlayerAnimationController 완전 우회)
            bool success = skillController.SkillSet.ExecuteSkill(1);
            
            if (success)
            {
                Debug.Log("🟢 [Skill2UI] SkillSet 직접 실행 성공!");
            }
            else
            {
                Debug.LogError("🔴 [Skill2UI] SkillSet 직접 실행 실패!");
            }
        }
        else
        {
            Debug.LogError("🔴 [Skill2UI] SkillController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 런타임에서 스킬2 쿨다운 시간을 변경할 때 사용하는 공용 메서드
    /// </summary>
    public void SetCooldownTime(float newCooldownTime)
    {
        cooldownTime = newCooldownTime;
        if (skillController != null)
        {
            skillController.skill2CooldownTime = cooldownTime;
            Debug.Log($"[Skill2UI] 스킬2 쿨다운 시간이 {newCooldownTime}초로 변경됨");
        }
    }

    /// <summary>
    /// 스킬2가 사용 가능한지 확인
    /// </summary>
    public bool IsSkill2Available()
    {
        if (skillController == null) return false;
        return skillController.GetSkill2CooldownRemaining() <= 0f;
    }

    /// <summary>
    /// 스킬2 쿨다운 진행률 (0~1)
    /// </summary>
    public float GetCooldownProgress()
    {
        if (skillController == null) return 0f;
        return 1f - (skillController.GetSkill2CooldownRemaining() / skillController.Skill2CooldownTime);
    }
} 