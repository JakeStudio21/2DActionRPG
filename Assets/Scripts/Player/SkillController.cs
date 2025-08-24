using UnityEngine;
using System.Collections;
using CueSystem;

/// <summary>
/// 새로운 BaseSkill<T> 기반 스킬 시스템 컨트롤러
/// SRP 원칙에 따라 스킬 관리만 담당
/// </summary>
[System.Serializable]
public class SkillController : MonoBehaviour
{
    [Header("📊 스킬 시스템")]
    public SkillSet skillSet;
    
    // ❌ 제거: 사용되지 않는 호환성 필드들
    /*
    [Header("⏰ 호환성 필드 (UI 시스템용)")]
    [Tooltip("스킬1 쿨다운 시간 (UI 시스템 호환성용)")]
    public float cooldownTime = 2f;
    
    [Tooltip("스킬2 쿨다운 시간 (UI 시스템 호환성용)")]
    public float skill2CooldownTime = 3f;
    */
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = true;
    
    /// <summary>
    /// SkillSet 프로퍼티 (기존 코드 호환성용)
    /// </summary>
    public SkillSet SkillSet 
    { 
        get 
        { 
            if (skillSet == null)
            {
                skillSet = new SkillSet();
                skillSet.showDebugLogs = showDebugLogs;
            }
            return skillSet; 
        } 
    }

    void Awake()
    {
        if (skillSet == null)
        {
            skillSet = new SkillSet();
            skillSet.showDebugLogs = showDebugLogs;
            
            if (showDebugLogs)
                Debug.Log("🎯 [SkillController] SkillSet 초기화 완료");
        }
        
        // ⭐ 디버그: 초기 상태 로깅
        if (showDebugLogs)
        {
            Debug.Log("🔍 [SkillController] Awake - 초기 스킬 상태 확인:");
            LogSkillSetInfo();
        }
    }
    
    void Start()
    {
        // ⭐ 디버그: 모든 초기화 후 최종 상태 확인
        if (showDebugLogs)
        {
            Debug.Log("🔍 [SkillController] Start - 최종 스킬 할당 상태 확인:");
            LogSkillSetInfo();
            
            // 스킬 컴포넌트들 직접 확인
            CheckSkillComponents();
        }
        
        // ⭐ 추가: 스킬 할당 검증 강화
        StartCoroutine(VerifySkillAssignmentRoutine());
    }
    
    /// <summary>
    /// ⭐ 새로 추가: 스킬 컴포넌트들 직접 확인
    /// </summary>
    [ContextMenu("Check Skill Components")]
    public void CheckSkillComponents()
    {
        Debug.Log("🔍 [SkillController] 스킬 컴포넌트 직접 확인:");
        
        var assasinSkill1 = GetComponent<AssasinSkill1>();
        var assasinSkill2 = GetComponent<AssasinSkill2>();
        var warriorSkill1 = GetComponent<WarriorSkill1>();
        var warriorSkill2 = GetComponent<WarriorSkill2>();
        
        Debug.Log($"   - AssasinSkill1: {(assasinSkill1 != null ? "✅ 존재" : "❌ 없음")}");
        Debug.Log($"   - AssasinSkill2: {(assasinSkill2 != null ? "✅ 존재" : "❌ 없음")}");
        Debug.Log($"   - WarriorSkill1: {(warriorSkill1 != null ? "✅ 존재" : "❌ 없음")}");
        Debug.Log($"   - WarriorSkill2: {(warriorSkill2 != null ? "✅ 존재" : "❌ 없음")}");
    }

    public void TriggerSkill()
    {
        Debug.Log("🔵 [SkillController] 스킬1 실행 요청");
        
        // 🆕 Cue 이벤트 발행
        var context = new CueContext
        {
            position = transform.position,
            actorType = ActorType.Player,
            magnitude = 1.5f
        };
        
        CueEmitter.Emit("skill.player.skill1", "Player", context);
        
        if (skillSet != null)
        {
            // ⭐ 실행 전 스킬 상태 확인
            var skill = skillSet.GetSkill(0);
            if (skill != null)
            {
                Debug.Log($"🎯 [SkillController] 스킬1 찾음: {skill.SkillName}, CanUse: {skill.CanUse()}");
            }
            else
            {
                Debug.LogWarning("❌ [SkillController] 스킬1이 SkillSet에 없습니다!");
                LogSkillSetInfo(); // 현재 상태 다시 확인
                return;
            }
            
            bool success = skillSet.ExecuteSkill(0);
            if (!success)
            {
                Debug.LogWarning("🟡 스킬1 실행 실패!");
            }
        }
        else
        {
            Debug.LogError("❌ [SkillController] SkillSet이 null입니다!");
        }
    }

    public void TriggerSkill2()
    {
        Debug.Log("🔵 [SkillController] 스킬2 실행 요청");
        
        // 🆕 Cue 이벤트 발행  
        var context = new CueContext
        {
            position = transform.position,
            actorType = ActorType.Player,
            magnitude = 2.0f
        };
        
        CueEmitter.Emit("skill.player.skill2", "Player", context);
        
        if (skillSet != null)
        {
            // ⭐ 실행 전 스킬 상태 확인
            var skill = skillSet.GetSkill(1);
            if (skill != null)
            {
                Debug.Log($"🎯 [SkillController] 스킬2 찾음: {skill.SkillName}, CanUse: {skill.CanUse()}");
            }
            else
            {
                Debug.LogWarning("❌ [SkillController] 스킬2가 SkillSet에 없습니다!");
                LogSkillSetInfo(); // 현재 상태 다시 확인
                return;
            }
            
            bool success = skillSet.ExecuteSkill(1);
            if (!success)
            {
                Debug.LogWarning("🟡 스킬2 실행 실패!");
            }
        }
        else
        {
            Debug.LogError("❌ [SkillController] SkillSet이 null입니다!");
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

    /// <summary>
    /// ⭐ SRP 준수: 스킬1 쿨다운 시작 (PlayerAnimationController에서 위임받음)
    /// </summary>
    // ❌ 삭제: 잘못된 SRP 구현
    /*
    public void StartSkill1Cooldown()
    {
        var skill1 = SkillSet.GetSkill(0);
        if (skill1 != null)
        {
            // BaseSkill의 StartCooldown() 호출
            skill1.Execute(); // 이미 쿨다운 시작 로직 포함
            
            if (showDebugLogs)
                Debug.Log($"🕐 [SkillController] 스킬1 쿨다운 시작: {skill1.Cooldown}초");
        }
        else
        {
            Debug.LogWarning("🟡 [SkillController] 스킬1을 찾을 수 없어서 쿨다운 시작 불가");
        }
    }
    
    /// <summary>
    /// ⭐ SRP 준수: 스킬2 쿨다운 시작 (PlayerAnimationController에서 위임받음)
    /// </summary>
    public void StartSkill2Cooldown()
    {
        var skill2 = SkillSet.GetSkill(1);
        if (skill2 != null)
        {
            // BaseSkill의 StartCooldown() 호출
            skill2.Execute(); // 이미 쿨다운 시작 로직 포함
            
            if (showDebugLogs)
                Debug.Log($"🕐 [SkillController] 스킬2 쿨다운 시작: {skill2.Cooldown}초");
        }
        else
        {
            Debug.LogWarning("🟡 [SkillController] 스킬2를 찾을 수 없어서 쿨다운 시작 불가");
        }
    }
    */

    [ContextMenu("Log SkillSet Info")]
    public void LogSkillSetInfo()
    {
        if (skillSet != null)
        {
            skillSet.LogAllSkills();
        }
        else
        {
            Debug.LogWarning("❌ [SkillController] SkillSet이 null이어서 로그를 출력할 수 없습니다!");
        }
    }

    /// <summary>
    /// 스킬 할당 검증 코루틴 (약간의 딜레이 후 체크)
    /// </summary>
    private IEnumerator VerifySkillAssignmentRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 다른 시스템 초기화 대기
        
        Debug.Log("🔍 [SkillController] 스킬 할당 검증 시작");
        
        if (skillSet == null)
        {
            Debug.LogError("❌ [SkillController] SkillSet이 null입니다!");
            yield break;
        }
        
        Debug.Log($"🔍 [SkillController] 현재 스킬 개수: {skillSet.SkillCount}");
        
        for (int i = 0; i < skillSet.SkillCount; i++)
        {
            var skill = skillSet.GetSkill(i);
            if (skill != null)
            {
                Debug.Log($"   ✅ 슬롯 {i}: {skill.SkillName} ({skill.GetType().Name})");
            }
            else
            {
                Debug.LogWarning($"   ❌ 슬롯 {i}: null");
            }
        }
        
        // 수동 재할당 시도
        if (skillSet.SkillCount == 0)
        {
            Debug.LogWarning("🟡 [SkillController] 스킬이 하나도 없습니다. 수동 재할당 시도...");
            TryManualSkillAssignment();
        }
    }

    /// <summary>
    /// 수동 스킬 할당 시도
    /// </summary>
    private void TryManualSkillAssignment()
    {
        var assasinSkill1 = GetComponent<AssasinSkill1>();
        var assasinSkill2 = GetComponent<AssasinSkill2>();
        var warriorSkill1 = GetComponent<WarriorSkill1>();
        var warriorSkill2 = GetComponent<WarriorSkill2>();
        
        int slot = 0;
        
        if (assasinSkill1 != null)
        {
            skillSet.SetSkill(slot++, assasinSkill1);
            Debug.Log($"🔧 [SkillController] 수동 할당: AssasinSkill1 → 슬롯 {slot-1}");
        }
        
        if (assasinSkill2 != null)
        {
            skillSet.SetSkill(slot++, assasinSkill2);
            Debug.Log($"🔧 [SkillController] 수동 할당: AssasinSkill2 → 슬롯 {slot-1}");
        }
        
        if (warriorSkill1 != null)
        {
            skillSet.SetSkill(slot++, warriorSkill1);
            Debug.Log($"🔧 [SkillController] 수동 할당: WarriorSkill1 → 슬롯 {slot-1}");
        }
        
        if (warriorSkill2 != null)
        {
            skillSet.SetSkill(slot++, warriorSkill2);
            Debug.Log($"🔧 [SkillController] 수동 할당: WarriorSkill2 → 슬롯 {slot-1}");
        }
    }
} 