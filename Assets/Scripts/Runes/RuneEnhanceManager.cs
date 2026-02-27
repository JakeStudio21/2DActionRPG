using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 룬 강화 매니저 (Singleton)
/// ⚙️ Phase 4-D-3: 성장(레벨업) 및 한계돌파 코어 로직
/// 
/// 역할:
/// - 룬 레벨업: 1~15레벨까지 성장, 3/6/9레벨에 부옵션 추첨
/// - 룬 한계돌파: 중복 룬 소모하여 최대 레벨 상한 확장 (10→15)
/// 
/// 핵심 설계 원칙:
/// ⚠️ 한계돌파는 부옵션을 주지 않으며, 오직 '최대 레벨 상한'만 확장함
/// ⚠️ 부옵션은 오직 3, 6, 9레벨 도달 시에만 가중치 기반 랜덤 추첨으로 획득
/// </summary>
public class RuneEnhanceManager : MonoBehaviour
{
    #region Singleton
    
    private static RuneEnhanceManager _instance;
    public static RuneEnhanceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<RuneEnhanceManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("RuneEnhanceManager");
                    _instance = go.AddComponent<RuneEnhanceManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    #endregion
    
    #region 레벨업 결과 Enum
    
    /// <summary>
    /// 레벨업 결과
    /// </summary>
    public enum LevelUpResult
    {
        Success,             // 성공
        AlreadyMaxLevel,     // 이미 최대 레벨
        RuneNotFound,        // 룬을 찾을 수 없음
        InsufficientGold,    // 골드 부족 (추후 구현)
        InsufficientMaterial // 재료 부족 (추후 구현)
    }
    
    /// <summary>
    /// 한계돌파 결과
    /// </summary>
    public enum LimitBreakResult
    {
        Success,                // 성공
        BaseRuneNotFound,       // 베이스 룬을 찾을 수 없음
        MaterialRuneNotFound,   // 재료 룬을 찾을 수 없음
        SameRune,               // 동일한 룬 (UID가 같음)
        DifferentRuneType,      // 다른 종류의 룬 (runeId 불일치)
        AlreadyMaxLimitBreak,   // 이미 최대 한계돌파
        MaterialRuneLocked,     // 재료 룬이 잠김
        BaseRuneNotMaxLevel,    // 베이스 룬이 최대 레벨 미달
        InsufficientGold        // 골드 부족 (추후 구현)
    }
    
    #endregion
    
    #region 이벤트
    
    /// <summary>
    /// 레벨업 성공 시 발생 (룬 UID, 새 레벨)
    /// </summary>
    public event Action<string, int> OnLevelUp;
    
    /// <summary>
    /// 부옵션 추가 시 발생 (룬 UID, 추가된 부옵션 ID)
    /// </summary>
    public event Action<string, string> OnSubStatAdded;
    
    /// <summary>
    /// 한계돌파 성공 시 발생 (베이스 룬 UID, 새 한계돌파 단계)
    /// </summary>
    public event Action<string, int> OnLimitBreak;
    
    #endregion
    
    #region 레벨업 시스템
    
    /// <summary>
    /// 룬 레벨업 시도
    /// ⚠️ 3, 6, 9레벨 도달 시 자동으로 부옵션 추첨
    /// </summary>
    /// <param name="targetRuneUID">레벨업할 룬의 UID</param>
    /// <returns>레벨업 결과</returns>
    public LevelUpResult TryLevelUp(string targetRuneUID)
    {
        Debug.Log($"[RuneEnhanceManager] 레벨업 시도: {targetRuneUID}");
        
        // 1. 룬 찾기
        RuneInstance targetRune = RuneInventoryManager.Instance.GetRuneByUID(targetRuneUID);
        
        if (targetRune == null)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 룬을 찾을 수 없습니다: {targetRuneUID}");
            return LevelUpResult.RuneNotFound;
        }
        
        Debug.Log($"  대상 룬: {targetRune}");
        
        // 2. 최대 레벨 체크
        int currentMaxLevel = targetRune.GetCurrentMaxLevel();
        
        if (targetRune.currentLevel >= currentMaxLevel)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 이미 최대 레벨입니다: Lv.{targetRune.currentLevel} (최대: {currentMaxLevel})");
            return LevelUpResult.AlreadyMaxLevel;
        }
        
        int oldLevel = targetRune.currentLevel;
        
        // 3. [TODO] 재화 소모 로직 (골드, 강화석 등)
        // if (!HasEnoughResources())
        // {
        //     return LevelUpResult.InsufficientGold;
        // }
        
        // 4. 레벨업 실행
        targetRune.currentLevel++;
        int newLevel = targetRune.currentLevel;
        
        Debug.Log($"[RuneEnhanceManager] ✅ 레벨업 성공: Lv.{oldLevel} → Lv.{newLevel}");
        
        // 5. 이벤트 발생
        OnLevelUp?.Invoke(targetRuneUID, newLevel);
        
        // 6. [중요] 3, 6, 9레벨 도달 시 부옵션 추첨
        if (newLevel == 3 || newLevel == 6 || newLevel == 9)
        {
            Debug.Log($"[RuneEnhanceManager] 🎲 부옵션 추첨 마일스톤 달성! (Lv.{newLevel})");
            RollAndAddSubStat(targetRune);
        }
        
        return LevelUpResult.Success;
    }
    
    /// <summary>
    /// 부옵션 추첨 (가중치 기반 랜덤)
    /// ⚠️ 3, 6, 9레벨 도달 시 자동 호출
    /// ⚠️ 중복 가능 (같은 부옵션이 2번 뽑히면 CombatFormula에서 합산됨)
    /// </summary>
    /// <param name="targetRune">부옵션을 추가할 룬</param>
    private void RollAndAddSubStat(RuneInstance targetRune)
    {
        if (targetRune == null || targetRune.baseData == null)
        {
            Debug.LogError("[RuneEnhanceManager] RollAndAddSubStat: 룬 또는 baseData가 null입니다!");
            return;
        }
        
        // 1. 부옵션 후보군 가져오기
        var subStatPool = targetRune.baseData.SubStatPool;
        
        if (subStatPool == null || subStatPool.Count == 0)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 부옵션 후보군이 비어있습니다: {targetRune.baseData.runeName}");
            return;
        }
        
        Debug.Log($"  부옵션 후보: {subStatPool.Count}개");
        
        // 2. 총 가중치 계산
        int totalWeight = 0;
        foreach (var dropInfo in subStatPool)
        {
            totalWeight += dropInfo.weight;
        }
        
        if (totalWeight <= 0)
        {
            Debug.LogError("[RuneEnhanceManager] 총 가중치가 0 이하입니다!");
            return;
        }
        
        Debug.Log($"  총 가중치: {totalWeight}");
        
        // 3. 가중치 기반 랜덤 뽑기
        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;
        string selectedModifierId = null;
        int selectedWeight = 0;
        
        foreach (var dropInfo in subStatPool)
        {
            cumulativeWeight += dropInfo.weight;
            
            if (randomValue < cumulativeWeight)
            {
                selectedModifierId = dropInfo.modifierId;
                selectedWeight = dropInfo.weight;
                break;
            }
        }
        
        // 4. 선택된 부옵션 추가
        if (string.IsNullOrEmpty(selectedModifierId))
        {
            Debug.LogError("[RuneEnhanceManager] 부옵션 추첨 실패!");
            return;
        }
        
        targetRune.allocatedSubStatModifierIds.Add(selectedModifierId);
        
        // 5. 디버그 로그 (상세)
        var modifier = ConditionalModifierDatabase.GetModifierById(selectedModifierId);
        string modifierName = modifier != null ? modifier.displayName : selectedModifierId;
        
        float probability = (float)selectedWeight / totalWeight * 100f;
        
        Debug.Log($"[RuneEnhanceManager] 🎉 부옵션 추첨 성공!");
        Debug.Log($"  추첨값: {randomValue} / {totalWeight}");
        Debug.Log($"  선택: [{selectedModifierId}] {modifierName}");
        Debug.Log($"  가중치: {selectedWeight} (확률: {probability:F1}%)");
        Debug.Log($"  현재 부옵션 개수: {targetRune.allocatedSubStatModifierIds.Count}개");
        
        // 6. 이벤트 발생
        OnSubStatAdded?.Invoke(targetRune.instanceUID, selectedModifierId);
    }
    
    #endregion
    
    #region 한계돌파 시스템
    
    /// <summary>
    /// 한계돌파 시도 (중복 룬 소모)
    /// ⚠️ 한계돌파는 부옵션을 주지 않으며, 오직 '최대 레벨 상한'만 확장함
    /// ⚠️ 베이스 룬은 최대 레벨 도달 상태여야 함
    /// </summary>
    /// <param name="baseRuneUID">한계돌파할 룬의 UID</param>
    /// <param name="materialRuneUID">재료로 소모할 룬의 UID</param>
    /// <returns>한계돌파 결과</returns>
    public LimitBreakResult TryLimitBreak(string baseRuneUID, string materialRuneUID)
    {
        Debug.Log($"[RuneEnhanceManager] 한계돌파 시도");
        Debug.Log($"  베이스 룬: {baseRuneUID}");
        Debug.Log($"  재료 룬: {materialRuneUID}");
        
        // 1. 베이스 룬 찾기
        RuneInstance baseRune = RuneInventoryManager.Instance.GetRuneByUID(baseRuneUID);
        
        if (baseRune == null)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 베이스 룬을 찾을 수 없습니다: {baseRuneUID}");
            return LimitBreakResult.BaseRuneNotFound;
        }
        
        // 2. 재료 룬 찾기
        RuneInstance materialRune = RuneInventoryManager.Instance.GetRuneByUID(materialRuneUID);
        
        if (materialRune == null)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 재료 룬을 찾을 수 없습니다: {materialRuneUID}");
            return LimitBreakResult.MaterialRuneNotFound;
        }
        
        Debug.Log($"  베이스: {baseRune}");
        Debug.Log($"  재료: {materialRune}");
        
        // 3. [예외 처리] 동일한 룬인지 확인
        if (baseRuneUID == materialRuneUID)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 동일한 룬으로 한계돌파할 수 없습니다!");
            return LimitBreakResult.SameRune;
        }
        
        // 4. [예외 처리] 같은 종류의 룬인지 확인 (runeId 동일)
        if (baseRune.baseDataId != materialRune.baseDataId)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 다른 종류의 룬입니다: {baseRune.baseDataId} ≠ {materialRune.baseDataId}");
            return LimitBreakResult.DifferentRuneType;
        }
        
        // 5. [예외 처리] 베이스 룬이 최대 레벨에 도달했는지 확인
        if (baseRune.currentLevel < baseRune.GetCurrentMaxLevel())
        {
            int currentMaxLevel = baseRune.GetCurrentMaxLevel();
            Debug.LogWarning($"[RuneEnhanceManager] 베이스 룬이 최대 레벨에 도달하지 않았습니다: Lv.{baseRune.currentLevel} < {currentMaxLevel}");
            Debug.LogWarning("  💡 Tip: 한계돌파는 최대 레벨 도달 후에만 가능합니다.");
            return LimitBreakResult.BaseRuneNotMaxLevel;
        }
        
        // 6. [예외 처리] 이미 최대 한계돌파에 도달했는지 확인
        if (baseRune.baseData == null)
        {
            Debug.LogError("[RuneEnhanceManager] baseRune.baseData가 null입니다!");
            return LimitBreakResult.BaseRuneNotFound;
        }
        
        if (baseRune.currentLimitBreak >= baseRune.baseData.maxLimitBreak)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 이미 최대 한계돌파에 도달했습니다: {baseRune.currentLimitBreak}/{baseRune.baseData.maxLimitBreak}");
            return LimitBreakResult.AlreadyMaxLimitBreak;
        }
        
        // 7. [예외 처리] 재료 룬이 잠금 상태인지 확인
        if (materialRune.isLocked)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 재료 룬이 잠금 상태입니다: {materialRune}");
            Debug.LogWarning("  💡 Tip: 잠금을 해제한 후 사용해주세요.");
            return LimitBreakResult.MaterialRuneLocked;
        }
        
        // 8. [TODO] 재화 소모 로직 (골드 등)
        // if (!HasEnoughGold())
        // {
        //     return LimitBreakResult.InsufficientGold;
        // }
        
        // 9. 한계돌파 실행
        int oldLimitBreak = baseRune.currentLimitBreak;
        int oldMaxLevel = baseRune.GetCurrentMaxLevel();
        
        baseRune.currentLimitBreak++;
        
        int newLimitBreak = baseRune.currentLimitBreak;
        int newMaxLevel = baseRune.GetCurrentMaxLevel();
        
        Debug.Log($"[RuneEnhanceManager] ✅ 한계돌파 성공!");
        Debug.Log($"  한계돌파: {oldLimitBreak} → {newLimitBreak}");
        Debug.Log($"  최대 레벨: Lv.{oldMaxLevel} → Lv.{newMaxLevel}");
        
        // 10. 재료 룬 삭제
        bool removed = RuneInventoryManager.Instance.RemoveRune(materialRuneUID);
        
        if (removed)
        {
            Debug.Log($"  재료 룸 소모: {materialRune.baseData.runeName} (UID: {materialRuneUID.Substring(0, 8)}...)");
        }
        else
        {
            Debug.LogError($"[RuneEnhanceManager] 재료 룬 삭제 실패: {materialRuneUID}");
        }
        
        // 11. 이벤트 발생
        OnLimitBreak?.Invoke(baseRuneUID, newLimitBreak);
        
        // 12. 중요 안내 로그
        Debug.Log("  ⚠️ 한계돌파는 부옵션을 주지 않습니다.");
        Debug.Log("  💡 부옵션은 3, 6, 9레벨 도달 시에만 획득 가능합니다.");
        
        return LimitBreakResult.Success;
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 레벨업 가능 여부 확인 (UI에서 사용)
    /// </summary>
    public bool CanLevelUp(string runeUID)
    {
        RuneInstance rune = RuneInventoryManager.Instance.GetRuneByUID(runeUID);
        
        if (rune == null)
            return false;
        
        return rune.CanLevelUp();
    }
    
    /// <summary>
    /// 한계돌파 가능 여부 확인 (UI에서 사용)
    /// </summary>
    public bool CanLimitBreak(string runeUID)
    {
        RuneInstance rune = RuneInventoryManager.Instance.GetRuneByUID(runeUID);
        
        if (rune == null)
            return false;
        
        return rune.CanLimitBreak();
    }
    
    /// <summary>
    /// 다음 부옵션 획득까지 남은 레벨 (UI에서 사용)
    /// </summary>
    /// <returns>남은 레벨 (0이면 다음 레벨업에 부옵션 획득)</returns>
    public int GetLevelsUntilNextSubStat(string runeUID)
    {
        RuneInstance rune = RuneInventoryManager.Instance.GetRuneByUID(runeUID);
        
        if (rune == null)
            return -1;
        
        int currentLevel = rune.currentLevel;
        
        // 다음 마일스톤 찾기 (3, 6, 9)
        int[] milestones = { 3, 6, 9 };
        
        foreach (int milestone in milestones)
        {
            if (currentLevel < milestone)
            {
                return milestone - currentLevel;
            }
        }
        
        // 9레벨 이상이면 더 이상 부옵션 없음
        return -1;
    }
    
    #endregion
    
    #region 디버그
    
    /// <summary>
    /// 룬 강화 정보 출력
    /// </summary>
    [ContextMenu("Print Rune Enhance Info")]
    public void PrintRuneEnhanceInfo()
    {
        Debug.Log("========== [RuneEnhanceManager] 강화 정보 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.Log("  (인벤토리가 비어있음)");
        }
        else
        {
            foreach (var rune in allRunes)
            {
                Debug.Log($"\n[{rune.baseData.runeName}]");
                Debug.Log($"  레벨: Lv.{rune.currentLevel} / {rune.GetCurrentMaxLevel()}");
                Debug.Log($"  한계돌파: {rune.currentLimitBreak} / {rune.baseData.maxLimitBreak}");
                Debug.Log($"  부옵션: {rune.allocatedSubStatModifierIds.Count}개");
                Debug.Log($"  레벨업 가능: {(CanLevelUp(rune.instanceUID) ? "✅" : "❌")}");
                Debug.Log($"  한계돌파 가능: {(CanLimitBreak(rune.instanceUID) ? "✅" : "❌")}");
                
                int levelsUntilNext = GetLevelsUntilNextSubStat(rune.instanceUID);
                if (levelsUntilNext > 0)
                {
                    Debug.Log($"  다음 부옵션: {levelsUntilNext}레벨 후");
                }
                else if (levelsUntilNext == 0)
                {
                    Debug.Log($"  다음 부옵션: 다음 레벨업!");
                }
                else
                {
                    Debug.Log($"  다음 부옵션: 없음 (9레벨 이상)");
                }
            }
        }
        
        Debug.Log("\n====================================================");
    }
    
    #endregion
}

