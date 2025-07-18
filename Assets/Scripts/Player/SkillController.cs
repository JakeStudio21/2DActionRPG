using UnityEngine;
using System.Collections;

public class SkillController : MonoBehaviour
{
    [Header("✨ SkillSet 기반 스킬 시스템")]
    [SerializeField] private SkillSet skillSet = new SkillSet();
    
    [Header("🎯 스킬 컴포넌트 (수동 할당 전용)")]
    [Tooltip("슬롯 0: 스킬1 - Inspector에서 AssasinSkill1 컴포넌트를 드래그하세요")]
    public MonoBehaviour skill1Component;
    [Tooltip("슬롯 1: 스킬2 - Inspector에서 AssasinSkill2 컴포넌트를 드래그하세요")]
    public MonoBehaviour skill2Component;

    // UI 연동을 위한 필수 필드들
    [HideInInspector] public float cooldownTime = 2f;
    [HideInInspector] public float skill2CooldownTime = 3f;
    
    // UI에서 참조하는 프로퍼티들 (필수 유지)
    public float CooldownTime => cooldownTime;
    public float Skill2CooldownTime => skill2CooldownTime;
    public SkillSet SkillSet => skillSet;

    void Awake()
    {
        Debug.Log("🎯 [SkillController] 수동 할당 전용 테스트 시작");
        
        // ⭐ Inspector 할당 상태만 확인 (자동 할당 코드 완전 제거)
        Debug.Log("📋 Inspector 수동 할당 확인:");
        Debug.Log($"   - skill1Component: {(skill1Component != null ? $"✅ {skill1Component.name}({skill1Component.GetType().Name})" : "❌ NULL")}");
        Debug.Log($"   - skill2Component: {(skill2Component != null ? $"✅ {skill2Component.name}({skill2Component.GetType().Name})" : "❌ NULL")}");
        
        // ⭐ SkillSet 초기화
        if (skillSet == null) skillSet = new SkillSet();
        
        // ⭐ Inspector에서 할당된 것만 사용 (자동 할당 없음)
        if (skill1Component != null)
        {
            if (skill1Component is ISkill skill1)
            {
                skillSet.SetSkill(0, skill1);
                Debug.Log($"✅ 스킬1 '{skill1.SkillName}' 수동 할당 완료");
            }
            else
            {
                Debug.LogError($"🔴 skill1Component '{skill1Component.name}'은 ISkill 인터페이스를 구현하지 않습니다!");
            }
        }
        else
        {
            Debug.LogWarning("🟡 skill1Component가 Inspector에서 할당되지 않았습니다!");
        }
        
        if (skill2Component != null)
        {
            if (skill2Component is ISkill skill2)
            {
                skillSet.SetSkill(1, skill2);
                Debug.Log($"✅ 스킬2 '{skill2.SkillName}' 수동 할당 완료");
            }
            else
            {
                Debug.LogError($"🔴 skill2Component '{skill2Component.name}'은 ISkill 인터페이스를 구현하지 않습니다!");
            }
        }
        else
        {
            Debug.LogWarning("🟡 skill2Component가 Inspector에서 할당되지 않았습니다!");
        }
        
        Debug.Log($"🎯 SkillSet 초기화 완료 - 스킬 개수: {skillSet.SkillCount}");
        
        // UI 연동
        var skillUI = FindObjectOfType<SkillUIController>();
        if (skillUI != null) cooldownTime = skillUI.cooldownTime;
        
        var skill2UI = FindObjectOfType<Skill2UIController>();
        if (skill2UI != null) skill2CooldownTime = skill2UI.cooldownTime;
        
        Debug.Log("✨ [SkillController] 수동 할당 초기화 완료!");
    }

    void Start()
    {
        // SkillSet 상태 확인
        skillSet.LogAllSkills();
        Debug.Log("🟢 [SkillController] SkillSet 시스템 활성화 완료");
    }

    public void TriggerSkill()
    {
        Debug.Log("🔵 [SkillController] 스킬1 실행 요청");
        
        if (skillSet != null)
        {
            bool success = skillSet.ExecuteSkill(0);
            if (!success)
            {
                Debug.LogWarning("🟡 스킬1 실행 실패 - Inspector에서 skill1Component를 할당했는지 확인하세요!");
            }
        }
    }

    public void TriggerSkill2()
    {
        Debug.Log("🔵 [SkillController] 스킬2 실행 요청");
        
        if (skillSet != null)
        {
            bool success = skillSet.ExecuteSkill(1);
            if (!success)
            {
                Debug.LogWarning("🟡 스킬2 실행 실패 - Inspector에서 skill2Component를 할당했는지 확인하세요!");
            }
        }
    }

    public bool TriggerSkill(int slot)
    {
        return skillSet.ExecuteSkill(slot);
    }

    public void OnSkillAnimationEvent(int slot)
    {
        if (skillSet != null)
        {
            skillSet.OnSkillAnimationEvent(slot);
        }
    }
    
    public void OnSkill1AnimationEvent() => OnSkillAnimationEvent(0);
    public void OnSkill2AnimationEvent() => OnSkillAnimationEvent(1);

    public float GetCooldownRemaining()
    {
        if (skillSet != null)
        {
            return skillSet.GetSkillCooldownRemaining(0);
        }
        return 0f;
    }

    public float GetSkill2CooldownRemaining()
    {
        if (skillSet != null)
        {
            return skillSet.GetSkillCooldownRemaining(1);
        }
        return 0f;
    }

    [ContextMenu("Log SkillSet Info")]
    public void LogSkillSetInfo()
    {
        if (skillSet != null)
        {
            skillSet.LogAllSkills();
        }
    }
} 