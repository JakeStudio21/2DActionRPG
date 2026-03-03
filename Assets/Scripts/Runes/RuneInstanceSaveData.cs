using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// [Phase 9] 룬 인스턴스 저장 데이터
/// JSON 직렬화를 위한 DTO (Data Transfer Object)
/// </summary>
[Serializable]
public class RuneInstanceSaveData
{
    [Tooltip("룬 고유 ID (예: RUNE_BOSS_HUNTER)")]
    public string runeId;
    
    [Tooltip("인스턴스 고유 UID")]
    public string instanceUID;
    
    [Tooltip("현재 레벨 (1~15)")]
    public int currentLevel;
    
    [Tooltip("현재 한계돌파 횟수 (0~5)")]
    public int currentLimitBreak;
    
    [Tooltip("잠금 여부")]
    public bool isLocked;
    
    [Tooltip("개방된 부옵션 Modifier ID 리스트 (3, 6, 9레벨)")]
    public List<string> allocatedSubStatModifierIds = new List<string>();
    
    /// <summary>
    /// RuneInstance → SaveData 변환
    /// </summary>
    public static RuneInstanceSaveData FromRuneInstance(RuneInstance rune)
    {
        if (rune == null) return null;
        
        return new RuneInstanceSaveData
        {
            runeId = rune.baseDataId, // RuneInstance는 baseDataId 사용
            instanceUID = rune.instanceUID,
            currentLevel = rune.currentLevel,
            currentLimitBreak = rune.currentLimitBreak,
            isLocked = rune.isLocked,
            allocatedSubStatModifierIds = new List<string>(rune.allocatedSubStatModifierIds)
        };
    }
    
    /// <summary>
    /// SaveData → RuneInstance 복원
    /// </summary>
    public RuneInstance ToRuneInstance()
    {
        // RuneData 로드
        var runeData = RuneDatabase.GetRuneData(runeId);
        
        if (runeData == null)
        {
            Debug.LogError($"❌ [RuneInstanceSaveData] 룬 데이터를 찾을 수 없습니다: {runeId}");
            return null;
        }
        
        // RuneInstance 생성 및 복원
        var instance = new RuneInstance(runeData)
        {
            instanceUID = this.instanceUID,
            currentLevel = this.currentLevel,
            currentLimitBreak = this.currentLimitBreak,
            isLocked = this.isLocked,
            allocatedSubStatModifierIds = new List<string>(this.allocatedSubStatModifierIds)
        };
        
        return instance;
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"RuneSave[{runeId}] Lv.{currentLevel} LB:{currentLimitBreak} {(isLocked ? "🔒" : "")}";
    }
}
