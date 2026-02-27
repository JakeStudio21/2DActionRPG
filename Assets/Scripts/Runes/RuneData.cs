using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부옵션 드랍 정보 (가중치 포함)
/// ⚙️ Phase 4-D-2: 부옵션 가중치 시스템
/// </summary>
[Serializable]
public struct SubStatDropInfo
{
    [Tooltip("부옵션 ConditionalModifier ID")]
    public string modifierId;
    
    [Tooltip("뽑기 가중치 (값이 클수록 확률이 높음)")]
    [Range(1, 100)]
    public int weight;
    
    public SubStatDropInfo(string modifierId, int weight)
    {
        this.modifierId = modifierId;
        this.weight = weight;
    }
}

/// <summary>
/// 룬 데이터 ScriptableObject (정적 템플릿)
/// ⚙️ Phase 4-D: 엔드 콘텐츠 확장 - 레벨업/한계돌파 시스템
/// 일반 스탯(StatModifier)이 아닌 특수 조건부 스탯(ConditionalModifier) 전용
/// 
/// 핵심 룰:
/// - 주옵션: 1개만 존재 (전략형 룬의 정체성 명확화)
/// - 부옵션: 3, 6, 9레벨에서 가중치 기반 랜덤 개방 (총 최대 3개)
/// </summary>
[CreateAssetMenu(fileName = "Rune_", menuName = "Data/Rune", order = 100)]
public class RuneData : ScriptableObject
{
    #region 기본 정보
    
    [Header("=== 기본 정보 ===")]
    [Tooltip("룬의 고유 ID (예: RUNE_BOSS_HUNTER)")]
    public string runeId;
    
    [Tooltip("룬의 표시 이름 (예: 보스 사냥꾼)")]
    public string runeName;
    
    [Tooltip("룬 타입 (8종류: Attack1~3, Survival1~3, Utility1~2)")]
    public RuneType runeType = RuneType.Attack1;
    
    [Tooltip("룬 설명 (UI 표시용)")]
    [TextArea(3, 5)]
    public string description;
    
    [Tooltip("룬 아이콘 (UI 표시용)")]
    public Sprite icon;
    
    #endregion
    
    #region 주옵션 (Main Stat)
    
    [Header("=== 주옵션 (Main Stat) ===")]
    [Tooltip("이 룬의 주옵션 효과 (1개만 존재). 획득 시 즉시 적용되며, 레벨업 시 수치가 강화됩니다.")]
    [SerializeField]
    private string mainStatModifierId = "";
    
    /// <summary>
    /// 주옵션 ModifierId (읽기 전용)
    /// ⚠️ 룬 1개당 주옵션은 무조건 1개만 존재 (전략형 룬의 정체성 명확화)
    /// </summary>
    public string MainStatModifierId => mainStatModifierId;
    
    #endregion
    
    #region 성장 시스템
    
    [Header("=== 성장 설정 ===")]
    [Tooltip("기본 최대 레벨 (한계돌파 없이 도달 가능한 최대 레벨, 기본값 10)")]
    public int baseMaxLevel = 10;
    
    [Tooltip("최대 한계돌파 횟수 (중복 룬으로 최대 레벨 상한 확장, 기본 10 + 5한돌 = 최대 15레벨)")]
    public int maxLimitBreak = 5;
    
    [Tooltip("레벨 당 주옵션 능력치 상승 배율 (예: 0.05 = 레벨당 5% 증가)")]
    [Range(0f, 0.5f)]
    public float mainStatLevelGrowth = 0.05f;
    
    #endregion
    
    #region 부옵션 시스템 (가중치 기반)
    
    [Header("=== 부옵션 설정 (가중치) ===")]
    [Tooltip("3, 6, 9레벨 도달 시 가중치 기반으로 랜덤 부여될 부옵션 후보군\n⚠️ 한계돌파와 무관하게 레벨 마일스톤에서만 개방됨")]
    [SerializeField]
    private List<SubStatDropInfo> subStatPool = new List<SubStatDropInfo>();
    
    /// <summary>
    /// 부옵션 후보군 (읽기 전용)
    /// 3, 6, 9레벨 도달 시 이 풀에서 가중치 기반으로 랜덤 추첨 (1개씩, 총 3개)
    /// </summary>
    public IReadOnlyList<SubStatDropInfo> SubStatPool => subStatPool;
    
    #endregion
    
    #region 메타데이터
    
    [Header("=== 메타데이터 ===")]
    [Tooltip("획득 방법 (예: 보스 드랍, 상점 구매)")]
    public string obtainMethod;
    
    [Tooltip("필요 레벨")]
    public int requiredLevel = 1;
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 룬이 유효한지 검증 (ID와 이름이 있는지)
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(runeId) && !string.IsNullOrEmpty(runeName);
    }
    
    /// <summary>
    /// 특정 레벨에서의 주옵션 배율 계산
    /// </summary>
    /// <param name="level">현재 레벨 (1~15)</param>
    /// <returns>배율 (예: 레벨 1 = 1.0, 레벨 10 = 1.45)</returns>
    public float GetMainStatMultiplier(int level)
    {
        // 레벨 1 = 기본값 (배율 1.0)
        // 레벨 2 이상부터 mainStatLevelGrowth 적용
        return 1.0f + (level - 1) * mainStatLevelGrowth;
    }
    
    /// <summary>
    /// 최종 도달 가능한 최대 레벨
    /// </summary>
    public int GetAbsoluteMaxLevel()
    {
        return baseMaxLevel + maxLimitBreak;
    }
    
    /// <summary>
    /// 디버그용 ToString
    /// </summary>
    public override string ToString()
    {
        string mainStat = string.IsNullOrEmpty(mainStatModifierId) ? "없음" : mainStatModifierId;
        return $"[{runeId}] {runeName} ({runeType}) - 주옵션: {mainStat}, 부옵션 후보: {subStatPool.Count}개";
    }
    
    /// <summary>
    /// Inspector에서 효과 목록 미리보기
    /// </summary>
    [ContextMenu("Preview Effects")]
    private void PreviewEffects()
    {
        Debug.Log($"========== [{runeName}] 룬 정보 ==========");
        Debug.Log($"타입: {runeType}");
        Debug.Log($"획득 조건: {obtainMethod} | 필요 레벨: {requiredLevel}");
        Debug.Log("");
        Debug.Log($"📊 성장 구조:");
        Debug.Log($"  - 기본 최대 레벨: {baseMaxLevel}");
        Debug.Log($"  - 한계돌파: 최대 {maxLimitBreak}회 (중복 룬으로 최대 레벨 상한 확장)");
        Debug.Log($"  - 최종 도달 가능 레벨: {GetAbsoluteMaxLevel()}");
        Debug.Log($"  - 레벨당 주옵션 성장: {mainStatLevelGrowth * 100}%");
        Debug.Log("");
        
        // 주옵션 (1개만 존재)
        Debug.Log($"=== 📌 주옵션 (1개) ===");
        Debug.Log($"💡 획득 시 즉시 적용되며, 매 레벨업마다 수치 증가");
        Debug.Log("");
        
        if (string.IsNullOrEmpty(mainStatModifierId))
        {
            Debug.LogWarning("⚠️ 주옵션이 설정되지 않았습니다!");
        }
        else
        {
            var mod = ConditionalModifierDatabase.GetModifierById(mainStatModifierId);
            if (mod != null)
            {
                Debug.Log($"  [{mainStatModifierId}] {mod.displayName}");
                Debug.Log($"    효과: {mod.effectType} | 조건: {mod.conditionType}");
                Debug.Log($"    Lv.1: {mod.value:F3} → Lv.10: {mod.value * GetMainStatMultiplier(10):F3} → Lv.15: {mod.value * GetMainStatMultiplier(15):F3}");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ [{mainStatModifierId}]: 모디파이어를 찾을 수 없음!");
            }
        }
        
        Debug.Log("");
        
        // 부옵션 (가중치 기반)
        Debug.Log($"=== 🎲 부옵션 후보 ({subStatPool.Count}개) ===");
        Debug.Log($"💡 3, 6, 9레벨 도달 시 이 풀에서 가중치 기반으로 랜덤 추첨 (1개씩, 총 3개)");
        Debug.Log($"⚠️ 한계돌파와 무관하게 레벨 마일스톤에서만 개방됨");
        Debug.Log("");
        
        if (subStatPool.Count == 0)
        {
            Debug.LogWarning("⚠️ 부옵션 후보가 없습니다!");
        }
        else
        {
            int totalWeight = 0;
            foreach (var dropInfo in subStatPool)
            {
                totalWeight += dropInfo.weight;
            }
            
            foreach (var dropInfo in subStatPool)
            {
                var subMod = ConditionalModifierDatabase.GetModifierById(dropInfo.modifierId);
                if (subMod != null)
                {
                    float probability = (float)dropInfo.weight / totalWeight * 100f;
                    Debug.Log($"  [{dropInfo.modifierId}] {subMod.displayName} (가중치: {dropInfo.weight}, 확률: {probability:F1}%)");
                    Debug.Log($"    효과: {subMod.effectType} | 값: {subMod.value:F3} (고정값, 레벨 보정 없음)");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ [{dropInfo.modifierId}]: 모디파이어를 찾을 수 없음! (가중치: {dropInfo.weight})");
                }
            }
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
}

