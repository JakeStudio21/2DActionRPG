using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 세이브 데이터 (직렬화 전용)
/// ⚙️ Phase 4-D-2: 인벤토리 및 세이브/로드 시스템
/// 
/// 역할:
/// - RuneInstance는 ScriptableObject 참조(baseData)를 가지므로 JSON 직렬화 불가
/// - 이를 해결하기 위해 문자열(runeId)로 변환하여 저장하는 전용 클래스
/// - 로드 시 RuneDatabase를 통해 runeId → RuneData 재연결
/// </summary>
[Serializable]
public class RuneSaveData
{
    #region 저장 필드
    
    /// <summary>
    /// 고유 식별자 (GUID)
    /// </summary>
    public string instanceUID;
    
    /// <summary>
    /// 원본 RuneData의 runeId (문자열)
    /// ⚠️ 로드 시 RuneDatabase.GetRuneData(baseDataId)로 재연결
    /// </summary>
    public string baseDataId;
    
    /// <summary>
    /// 현재 레벨 (1~15)
    /// </summary>
    public int currentLevel;
    
    /// <summary>
    /// 현재 한계돌파 단계 (0~5)
    /// </summary>
    public int currentLimitBreak;
    
    /// <summary>
    /// 잠금 여부
    /// </summary>
    public bool isLocked;
    
    /// <summary>
    /// 부여된 부옵션 ID 목록 (3, 6, 9레벨에서 획득)
    /// </summary>
    public List<string> allocatedSubStatModifierIds;
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 기본 생성자 (역직렬화용)
    /// </summary>
    public RuneSaveData()
    {
        allocatedSubStatModifierIds = new List<string>();
    }
    
    /// <summary>
    /// RuneInstance → RuneSaveData 변환 생성자
    /// </summary>
    /// <param name="instance">저장할 RuneInstance</param>
    public RuneSaveData(RuneInstance instance)
    {
        if (instance == null)
        {
            Debug.LogError("[RuneSaveData] RuneInstance가 null입니다!");
            return;
        }
        
        // 기본 데이터 복사
        this.instanceUID = instance.instanceUID;
        this.baseDataId = instance.baseDataId;
        this.currentLevel = instance.currentLevel;
        this.currentLimitBreak = instance.currentLimitBreak;
        this.isLocked = instance.isLocked;
        
        // 부옵션 목록 깊은 복사
        this.allocatedSubStatModifierIds = new List<string>(instance.allocatedSubStatModifierIds);
    }
    
    #endregion
    
    #region 변환 메서드
    
    /// <summary>
    /// RuneSaveData → RuneInstance 변환
    /// ⚠️ RuneDatabase를 통해 baseData 재연결 필요
    /// </summary>
    /// <returns>복원된 RuneInstance (baseData 재연결 실패 시 null)</returns>
    public RuneInstance ToRuneInstance()
    {
        // 1. RuneDatabase에서 원본 RuneData 찾기
        RuneData baseData = RuneDatabase.GetRuneData(baseDataId);
        
        if (baseData == null)
        {
            Debug.LogError($"[RuneSaveData] RuneData를 찾을 수 없습니다: {baseDataId} (UID: {instanceUID})");
            return null;
        }
        
        // 2. RuneInstance 재조립
        RuneInstance instance = new RuneInstance
        {
            instanceUID = this.instanceUID,
            baseData = baseData,
            baseDataId = this.baseDataId,
            currentLevel = this.currentLevel,
            currentLimitBreak = this.currentLimitBreak,
            isLocked = this.isLocked,
            allocatedSubStatModifierIds = new List<string>(this.allocatedSubStatModifierIds)
        };
        
        return instance;
    }
    
    #endregion
    
    #region 검증
    
    /// <summary>
    /// 저장 데이터 유효성 검증
    /// </summary>
    /// <returns>유효하면 true</returns>
    public bool IsValid()
    {
        // 필수 필드 체크
        if (string.IsNullOrEmpty(instanceUID))
        {
            Debug.LogWarning("[RuneSaveData] instanceUID가 비어있습니다.");
            return false;
        }
        
        if (string.IsNullOrEmpty(baseDataId))
        {
            Debug.LogWarning($"[RuneSaveData] baseDataId가 비어있습니다. (UID: {instanceUID})");
            return false;
        }
        
        // 레벨 범위 체크 (1~15)
        if (currentLevel < 1 || currentLevel > 15)
        {
            Debug.LogWarning($"[RuneSaveData] 잘못된 레벨: {currentLevel} (UID: {instanceUID})");
            return false;
        }
        
        // 한계돌파 범위 체크 (0~5)
        if (currentLimitBreak < 0 || currentLimitBreak > 5)
        {
            Debug.LogWarning($"[RuneSaveData] 잘못된 한계돌파 단계: {currentLimitBreak} (UID: {instanceUID})");
            return false;
        }
        
        return true;
    }
    
    #endregion
    
    #region 디버그
    
    /// <summary>
    /// 디버그 출력
    /// </summary>
    public override string ToString()
    {
        return $"[{baseDataId}] Lv.{currentLevel} (한돌 {currentLimitBreak}) | 부옵션 {allocatedSubStatModifierIds?.Count ?? 0}개 | UID: {instanceUID}";
    }
    
    #endregion
}

/// <summary>
/// 룬 인벤토리 세이브 데이터 (전체 인벤토리 저장용)
/// </summary>
[Serializable]
public class RuneInventorySaveData
{
    /// <summary>
    /// 보유 중인 모든 룬 세이브 데이터
    /// </summary>
    public List<RuneSaveData> runeInstances;
    
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public RuneInventorySaveData()
    {
        runeInstances = new List<RuneSaveData>();
    }
    
    /// <summary>
    /// 저장 시간 (메타데이터, 선택사항)
    /// </summary>
    public string saveTimestamp;
    
    /// <summary>
    /// 저장 버전 (추후 호환성 관리용)
    /// </summary>
    public int saveVersion = 1;
}

