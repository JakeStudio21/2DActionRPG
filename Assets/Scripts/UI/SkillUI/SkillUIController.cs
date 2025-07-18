using UnityEngine;
using UnityEngine.UI;

public class SkillUIController : MonoBehaviour
{
    public Image cooldownImage;
    [Tooltip("스킬 쿨다운 시간(초) - 여기서만 입력!")]
    public float cooldownTime = 2f; // 쿨다운 시간은 여기서만 입력!
    
    private SkillController skillController;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f; // UI 업데이트 간격 (0.1초)

    void Start()
    {
        Debug.Log("[SkillUI] SkillUIController 초기화 시작");
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
            Debug.LogWarning("[SkillUI] SkillController를 찾지 못함! 씬에 SkillController가 있는지 확인하세요.");
            return;
        }

        Debug.Log("[SkillUI] SkillController를 정상적으로 찾음!");
        
        // SkillController의 쿨다운 시간은 항상 UI에서 입력한 값으로 동기화
        skillController.cooldownTime = cooldownTime;
        Debug.Log($"[SkillUI] 쿨다운 시간 동기화 완료: {cooldownTime}초");
    }

    /// <summary>
    /// 필수 컴포넌트들이 올바르게 할당되었는지 검증
    /// </summary>
    private void ValidateComponents()
    {
        if (cooldownImage == null)
        {
            Debug.LogWarning("[SkillUI] CooldownImage가 할당되지 않았습니다! Inspector에서 할당해주세요.");
        }
        else
        {
            Debug.Log("[SkillUI] 모든 컴포넌트가 정상적으로 할당됨");
        }
    }

    /// <summary>
    /// 쿨다운 UI 업데이트 (성능 최적화됨)
    /// </summary>
    private void UpdateCooldownUI()
    {
        if (skillController != null && cooldownImage != null)
        {
            float remain = skillController.GetCooldownRemaining();
            float newFillAmount = remain / skillController.CooldownTime;
            
            // fillAmount가 실제로 변경되었을 때만 업데이트
            if (Mathf.Abs(cooldownImage.fillAmount - newFillAmount) > 0.01f)
            {
                cooldownImage.fillAmount = newFillAmount;
            }
        }
    }

    /// <summary>
    /// 스킬 버튼이 클릭되었을 때 호출되는 공용 메서드
    /// Unity Button의 OnClick 이벤트에서 호출됩니다.
    /// ⭐ 신규: SkillController.SkillSet으로 직접 연결 (PlayerAnimationController 완전 우회)
    /// </summary>
    public void OnSkillButtonPressed()
    {
        Debug.Log("🔥 [SkillUI] 스킬1 버튼 클릭! - SkillSet 직접 실행");
        
        // ⭐ SkillController.SkillSet으로 직접 연결
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
        {
            // SkillSet을 통한 직접 실행 (PlayerAnimationController 완전 우회)
            bool success = skillController.SkillSet.ExecuteSkill(0);
            
            if (success)
            {
                Debug.Log("�� [SkillUI] SkillSet 직접 실행 성공!");
            }
            else
            {
                Debug.LogError("🔴 [SkillUI] SkillSet 직접 실행 실패!");
            }
        }
        else
        {
            Debug.LogError("🔴 [SkillUI] SkillController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 런타임에서 쿨다운 시간을 변경할 때 사용하는 공용 메서드
    /// </summary>
    public void SetCooldownTime(float newCooldownTime)
    {
        cooldownTime = newCooldownTime;
        if (skillController != null)
        {
            skillController.cooldownTime = cooldownTime;
            Debug.Log($"[SkillUI] 쿨다운 시간이 {newCooldownTime}초로 변경됨");
        }
    }
} 