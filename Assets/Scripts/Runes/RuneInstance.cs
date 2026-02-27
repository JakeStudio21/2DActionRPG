using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 인스턴스 (동적 데이터)
/// 유저가 획득하여 인벤토리에 보관/저장하는 개별 룬 객체
/// ⚙️ Phase 4-D: 엔드 콘텐츠 확장
/// </summary>
[Serializable]
public class RuneInstance
{
    #region 고유 식별자
    
    /// <summary>
    /// 고유 식별자 (GUID 기반)
    /// Save/Load 시 이 룬을 구분하는 핵심 키
    /// </summary>
    public string instanceUID;
    
    #endregion
    
    #region 원본 데이터 참조
    
    /// <summary>
    /// 이 인스턴스의 원본 RuneData (ScriptableObject)
    /// ⚠️ 주의: 직렬화 시 runeId만 저장하고, 로드 시 Resources.Load로 복원
    /// </summary>
    [NonSerialized]
    public RuneData baseData;
    
    /// <summary>
    /// 직렬화용 RuneData ID (baseData.runeId)
    /// Save 시: baseData.runeId → baseDataId
    /// Load 시: baseDataId → Resources.Load<RuneData>() → baseData
    /// </summary>
    public string baseDataId;
    
    #endregion
    
    #region 성장 상태
    
    /// <summary>
    /// 현재 레벨 (1 ~ GetCurrentMaxLevel())
    /// </summary>
    public int currentLevel = 1;
    
    /// <summary>
    /// 현재 한계돌파 단계 (0 ~ baseData.maxLimitBreak)
    /// 예: 0 = 한돌 안함, 1 = 1한돌, 5 = 5한돌 (최대)
    /// </summary>
    public int currentLimitBreak = 0;
    
    #endregion
    
    #region 부옵션 (서브스탯)
    
    /// <summary>
    /// 레벨 마일스톤 도달 시 부여된 부옵션 ID 목록
    /// ⚠️ 중요: 3, 6, 9레벨 도달 시 subStatModifierPool에서 랜덤으로 1개씩 추가됨 (총 최대 3개)
    /// ⚠️ 한계돌파와 무관하게 레벨 마일스톤에서만 개방됨
    /// 예: Lv.3 → "BOSS_IGNORE_DEF" 추가, Lv.6 → "LOW_HP_DR" 추가, Lv.9 → "BOSS_AREA_DMG_REDUCE" 추가
    /// </summary>
    public List<string> allocatedSubStatModifierIds = new List<string>();
    
    #endregion
    
    #region 잠금/보호
    
    /// <summary>
    /// 잠금 여부 (true일 경우 한계돌파 재료로 소모 불가)
    /// 사용자가 중요한 룬을 실수로 분해하지 않도록 보호
    /// </summary>
    public bool isLocked = false;
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 기본 생성자 (역직렬화용)
    /// </summary>
    public RuneInstance()
    {
    }
    
    /// <summary>
    /// 새 룬 획득 시 생성자
    /// </summary>
    /// <param name="baseRuneData">원본 RuneData</param>
    public RuneInstance(RuneData baseRuneData)
    {
        this.instanceUID = GenerateUID();
        this.baseData = baseRuneData;
        this.baseDataId = baseRuneData != null ? baseRuneData.runeId : "";
        this.currentLevel = 1;
        this.currentLimitBreak = 0;
        this.allocatedSubStatModifierIds = new List<string>();
        this.isLocked = false;
    }
    
    /// <summary>
    /// 고유 ID 생성 (GUID 기반)
    /// </summary>
    private string GenerateUID()
    {
        return System.Guid.NewGuid().ToString();
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 현재 도달 가능한 최대 레벨
    /// 한계돌파 1회당 최대 레벨 +1
    /// </summary>
    /// <returns>현재 최대 레벨 (예: 기본 10 + 3한돌 = 13)</returns>
    public int GetCurrentMaxLevel()
    {
        if (baseData == null)
        {
            Debug.LogWarning($"[RuneInstance] baseData가 null입니다. (UID: {instanceUID})");
            return 10; // fallback
        }
        
        return baseData.baseMaxLevel + currentLimitBreak;
    }
    
    /// <summary>
    /// 레벨업 가능 여부
    /// </summary>
    public bool CanLevelUp()
    {
        return currentLevel < GetCurrentMaxLevel();
    }
    
    /// <summary>
    /// 한계돌파 가능 여부
    /// </summary>
    public bool CanLimitBreak()
    {
        if (baseData == null) return false;
        
        // 조건 1: 현재 레벨이 최대 레벨이어야 함
        if (currentLevel < GetCurrentMaxLevel())
            return false;
        
        // 조건 2: 한계돌파 횟수가 최대치 미만이어야 함
        if (currentLimitBreak >= baseData.maxLimitBreak)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// 현재 활성화된 모든 ConditionalModifier ID 목록 가져오기
    /// = 주옵션 (mainStatModifierId, 1개) + 부옵션 (allocatedSubStatModifierIds, 최대 3개)
    /// </summary>
    /// <returns>ModifierId 목록</returns>
    public List<string> GetAllActiveModifierIds()
    {
        var result = new List<string>();
        
        // 1. 주옵션 추가 (mainStatModifierId, 1개만 존재)
        if (baseData != null && !string.IsNullOrEmpty(baseData.MainStatModifierId))
        {
            result.Add(baseData.MainStatModifierId);
        }
        
        // 2. 부옵션 추가 (allocatedSubStatModifierIds)
        result.AddRange(allocatedSubStatModifierIds);
        
        return result;
    }
    
    /// <summary>
    /// 주옵션의 현재 레벨 보정 배율
    /// </summary>
    public float GetMainStatMultiplier()
    {
        if (baseData == null) return 1.0f;
        
        return baseData.GetMainStatMultiplier(currentLevel);
    }
    
    /// <summary>
    /// 디버그 출력
    /// </summary>
    public override string ToString()
    {
        string name = baseData != null ? baseData.runeName : baseDataId;
        return $"[{name}] Lv.{currentLevel} (한돌 {currentLimitBreak}) | 부옵션 {allocatedSubStatModifierIds.Count}개";
    }
    
    #endregion
    
    #region 직렬화 지원
    
    /// <summary>
    /// Save 직전 호출: baseData → baseDataId 변환
    /// </summary>
    public void PrepareForSave()
    {
        if (baseData != null)
        {
            baseDataId = baseData.runeId;
        }
    }
    
    /// <summary>
    /// Load 직후 호출: baseDataId → baseData 복원
    /// </summary>
    public void RestoreAfterLoad()
    {
        if (!string.IsNullOrEmpty(baseDataId))
        {
            // Resources/Runes/ 폴더에서 로드
            baseData = Resources.Load<RuneData>($"Runes/{baseDataId}");
            
            if (baseData == null)
            {
                Debug.LogError($"[RuneInstance] RuneData를 찾을 수 없습니다: {baseDataId}");
            }
        }
    }
    
    #endregion
}

