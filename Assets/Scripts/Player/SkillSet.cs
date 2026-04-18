using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 클래스별 스킬 모음을 관리하는 클래스
/// List<ISkill>로 스킬 슬롯(Skill1, Skill2, Skill3...)을 통합 관리
/// </summary>
[System.Serializable]
public class SkillSet
{
    [Header("스킬 슬롯 관리")]
    [SerializeField] private List<ISkill> skills = new List<ISkill>();
    
    [Header("디버그")]
    public bool showDebugLogs = true;
    
    /// <summary>
    /// 스킬 개수
    /// </summary>
    public int SkillCount => skills.Count;
    
    /// <summary>
    /// 특정 슬롯의 스킬 가져오기
    /// </summary>
    /// <param name="slot">스킬 슬롯 (0: Skill1, 1: Skill2, ...)</param>
    /// <returns>해당 슬롯의 ISkill, 없으면 null</returns>
    public ISkill GetSkill(int slot)
    {
        if (slot < 0 || slot >= skills.Count)
        {
            return null;
        }
        
        return skills[slot];
    }
    
    /// <summary>
    /// 스킬 슬롯에 스킬 설정 (디버그 강화)
    /// </summary>
    /// <param name="slot">스킬 슬롯</param>
    /// <param name="skill">설정할 스킬</param>
    public void SetSkill(int slot, ISkill skill)
    {
        
        // 리스트 크기 확장
        while (skills.Count <= slot)
        {
            skills.Add(null);
        }
        
        // 이전 값 기록
        var previousSkill = skills[slot];
        
        skills[slot] = skill;
        
        // 설정 후 확인
        
            
        // ⭐ 즉시 GetSkill로 검증
        var verifySkill = GetSkill(slot);
    }
    
    /// <summary>
    /// 스킬 추가 (다음 빈 슬롯에)
    /// </summary>
    /// <param name="skill">추가할 스킬</param>
    /// <returns>할당된 슬롯 번호</returns>
    public int AddSkill(ISkill skill)
    {
        skills.Add(skill);
        int slot = skills.Count - 1;
        
            
        return slot;
    }
    
    /// <summary>
    /// 특정 슬롯의 스킬 실행
    /// </summary>
    /// <param name="slot">실행할 스킬 슬롯</param>
    /// <returns>실행 성공 여부</returns>
    public bool ExecuteSkill(int slot)
    {
        var skill = GetSkill(slot);
        if (skill == null)
        {
            return false;
        }
        
            
        skill.Execute();
        return true;
    }
    
    /// <summary>
    /// 특정 슬롯 스킬의 Animation Event 호출
    /// </summary>
    /// <param name="slot">Animation Event를 호출할 스킬 슬롯</param>
    public void OnSkillAnimationEvent(int slot)
    {
        var skill = GetSkill(slot);
        if (skill == null)
        {
            // ⭐ 추가: 현재 스킬 상태 로그
            Debug.LogWarning($"🔍 [SkillSet] 현재 스킬 개수: {SkillCount}");
            for (int i = 0; i < SkillCount; i++)
            {
                var checkSkill = GetSkill(i);
                Debug.LogWarning($"   - 슬롯 {i}: {(checkSkill != null ? checkSkill.SkillName : "null")}");
            }
            return;
        }
        
            
        // ⭐ CanUse() 체크 제거: Animation Event는 이미 실행된 스킬의 결과이므로 무조건 실행
        // 쿨다운 체크는 TriggerSkill1/2()에서 이미 수행됨
        skill.OnAnimationEvent();
    }
    
    /// <summary>
    /// 특정 슬롯 스킬 사용 가능 여부 확인
    /// </summary>
    /// <param name="slot">확인할 스킬 슬롯</param>
    /// <returns>사용 가능하면 true</returns>
    public bool CanUseSkill(int slot)
    {
        var skill = GetSkill(slot);
        if (skill == null)
            return false;
            
        return skill.CanUse();
    }
    
    /// <summary>
    /// 특정 슬롯 스킬의 쿨다운 남은 시간
    /// </summary>
    /// <param name="slot">확인할 스킬 슬롯</param>
    /// <returns>쿨다운 남은 시간(초)</returns>
    public float GetSkillCooldownRemaining(int slot)
    {
        var skill = GetSkill(slot);
        if (skill == null)
            return 0f;
            
        return skill.GetCooldownRemaining();
    }
    
    /// <summary>
    /// 모든 스킬 정보 로깅 (디버깅용)
    /// </summary>
    public void LogAllSkills()
    {
    }
    
    /// <summary>
    /// MonoBehaviour 컴포넌트들로부터 SkillSet 자동 구성
    /// </summary>
    /// <param name="gameObject">스킬 컴포넌트들이 있는 GameObject</param>
    public void InitializeFromGameObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            Debug.LogError("🔴 [SkillSet] InitializeFromGameObject: GameObject가 null입니다!");
            return;
        }
        
        // 기존 스킬 리스트 클리어
        skills.Clear();
        
        // ISkill을 구현하는 모든 컴포넌트 찾기
        var skillComponents = gameObject.GetComponents<ISkill>();
        
        foreach (var skillComponent in skillComponents)
        {
            AddSkill(skillComponent);
        }
        
    }
}
