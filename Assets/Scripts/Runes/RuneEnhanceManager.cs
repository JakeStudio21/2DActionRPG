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
        MaterialRuneNotFound,   // [Deprecated] 재료 룬을 찾을 수 없음 (기획 변경: 재료 룬 제거)
        SameRune,               // [Deprecated] 동일한 룬 (기획 변경: 재료 룬 제거)
        DifferentRuneType,      // [Deprecated] 다른 종류의 룬 (기획 변경: 재료 룬 제거)
        AlreadyMaxLimitBreak,   // 이미 최대 한계돌파
        MaterialRuneLocked,     // [Deprecated] 재료 룬이 잠김 (기획 변경: 재료 룬 제거)
        BaseRuneNotMaxLevel,    // 베이스 룬이 최대 레벨 미달
        InsufficientGold        // 파편 부족 (기존 enum 재사용)
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
    
    #region 재화 비용 상수
    
    /// <summary>
    /// 룬 해금 비용 (룬 파편)
    /// </summary>
    private const int UNLOCK_COST = 100;
    
    /// <summary>
    /// 레벨업 비용 (룬 파편)
    /// </summary>
    private const int LEVELUP_COST = 10;
    
    /// <summary>
    /// 한계돌파 비용 (룬 파편)
    /// </summary>
    private const int LIMIT_BREAK_COST = 200;
    
    #endregion
    
    #region 룬 해금 시스템
    
    /// <summary>
    /// 룬 해금 결과
    /// </summary>
    public enum UnlockResult
    {
        Success,                // 성공
        AlreadyUnlocked,        // 이미 해금됨
        InsufficientFragments,  // 파편 부족
        InvalidRuneData         // 유효하지 않은 RuneData
    }
    
    /// <summary>
    /// 룬 해금 시도 (룬 파편 소모)
    /// ⚠️ 기획 변경: 각 룬은 종류별로 1개만 보유 가능
    /// ⚠️ 미보유 상태일 때만 파편을 소모하여 Lv.1 룬 생성
    /// </summary>
    /// <param name="runeId">해금할 룬의 ID (예: RUNE_BOSS_HUNTER)</param>
    /// <returns>해금 결과</returns>
    public UnlockResult TryUnlockRune(string runeId)
    {
        
        // 1. RuneData 가져오기
        RuneData runeData = RuneDatabase.GetRuneData(runeId);
        
        if (runeData == null)
        {
            Debug.LogError($"[RuneEnhanceManager] 유효하지 않은 runeId: {runeId}");
            return UnlockResult.InvalidRuneData;
        }
        
        
        // 2. 이미 보유 중인지 확인
        var inventoryManager = RuneInventoryManager.Instance;
        var existingRunes = inventoryManager.GetRunesByDataId(runeId);
        
        if (existingRunes.Count > 0)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 이미 해금된 룬입니다: {runeData.runeName}");
            Debug.LogWarning($"  보유 개수: {existingRunes.Count}개");
            return UnlockResult.AlreadyUnlocked;
        }
        
        // 3. 해당 룬의 고유 조각 소지 확인 (Phase 6.5)
        int currentFragments = inventoryManager.GetFragmentCount(runeId);
        if (currentFragments < UNLOCK_COST)
        {
            Debug.LogWarning($"[RuneEnhanceManager] [{runeData.runeName}] 조각이 부족합니다!");
            Debug.LogWarning($"  필요: {UNLOCK_COST}개 | 보유: {currentFragments}개");
            return UnlockResult.InsufficientFragments;
        }
        
        // 4. 조각 차감 (Phase 6.5: 해당 룬의 고유 조각만 차감)
        if (!inventoryManager.TryConsumeFragments(runeId, UNLOCK_COST))
        {
            Debug.LogError($"[RuneEnhanceManager] 조각 차감 실패: {runeId}");
            return UnlockResult.InsufficientFragments;
        }
        
        // 5. Lv.1 룬 생성 (인벤토리에 추가)
        RuneInstance newRune = inventoryManager.AddRune(runeData);
        
        if (newRune == null)
        {
            Debug.LogError($"[RuneEnhanceManager] 룬 생성 실패: {runeId}");
            // 실패 시 조각 복구
            inventoryManager.AddFragments(runeId, UNLOCK_COST);
            return UnlockResult.InvalidRuneData;
        }
        
        Dbg.Log($"[RuneEnhanceManager] ✅ 룬 해금 성공!");
        Dbg.Log($"  생성된 룬: {newRune}");
        
        return UnlockResult.Success;
    }
    
    /// <summary>
    /// 룬 해금 가능 여부 확인 (UI에서 사용)
    /// </summary>
    /// <param name="runeId">확인할 룬의 ID</param>
    /// <returns>해금 가능 여부</returns>
    public bool CanUnlockRune(string runeId)
    {
        // RuneData 유효성
        RuneData runeData = RuneDatabase.GetRuneData(runeId);
        if (runeData == null)
            return false;
        
        // 이미 보유 중인지 확인
        var inventoryManager = RuneInventoryManager.Instance;
        var existingRunes = inventoryManager.GetRunesByDataId(runeId);
        if (existingRunes.Count > 0)
            return false;
        
        // 해당 룬의 고유 조각 확인 (Phase 6.5)
        return inventoryManager.GetFragmentCount(runeId) >= UNLOCK_COST;
    }
    
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
        
        // 1. 룬 찾기
        RuneInstance targetRune = RuneInventoryManager.Instance.GetRuneByUID(targetRuneUID);
        
        if (targetRune == null)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 룬을 찾을 수 없습니다: {targetRuneUID}");
            return LevelUpResult.RuneNotFound;
        }
        
        
        // 2. 최대 레벨 체크
        int currentMaxLevel = targetRune.GetCurrentMaxLevel();
        
        if (targetRune.currentLevel >= currentMaxLevel)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 이미 최대 레벨입니다: Lv.{targetRune.currentLevel} (최대: {currentMaxLevel})");
            return LevelUpResult.AlreadyMaxLevel;
        }
        
        int oldLevel = targetRune.currentLevel;
        string runeId = targetRune.baseData.runeId;
        
        // 3. 해당 룬의 고유 조각 소지 확인 (Phase 6.5)
        var inventoryManager = RuneInventoryManager.Instance;
        int currentFragments = inventoryManager.GetFragmentCount(runeId);
        if (currentFragments < LEVELUP_COST)
        {
            Debug.LogWarning($"[RuneEnhanceManager] [{targetRune.baseData.runeName}] 조각이 부족합니다!");
            Debug.LogWarning($"  필요: {LEVELUP_COST}개 | 보유: {currentFragments}개");
            return LevelUpResult.InsufficientGold; // 기존 enum 재사용
        }
        
        // 4. 조각 차감 (Phase 6.5: 해당 룬의 고유 조각만 차감)
        if (!inventoryManager.TryConsumeFragments(runeId, LEVELUP_COST))
        {
            Debug.LogError($"[RuneEnhanceManager] 조각 차감 실패: {runeId}");
            return LevelUpResult.InsufficientGold;
        }
        
        // 5. 레벨업 실행
        targetRune.currentLevel++;
        int newLevel = targetRune.currentLevel;
        
        Dbg.Log($"[RuneEnhanceManager] ✅ 레벨업 성공: Lv.{oldLevel} → Lv.{newLevel}");
        
        // 6. 이벤트 발생
        OnLevelUp?.Invoke(targetRuneUID, newLevel);
        
        // 7. [중요] 3, 6, 9레벨 도달 시 부옵션 추첨
        if (newLevel == 3 || newLevel == 6 || newLevel == 9)
        {
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
        
        
        // 6. 이벤트 발생
        OnSubStatAdded?.Invoke(targetRune.instanceUID, selectedModifierId);
    }
    
    #endregion
    
    #region 한계돌파 시스템
    
    /// <summary>
    /// 한계돌파 시도 (룬 파편 소모)
    /// ⚠️ 기획 변경: 재료 룬 대신 룬 파편을 소모하여 한계돌파
    /// ⚠️ 한계돌파는 부옵션을 주지 않으며, 오직 '최대 레벨 상한'만 확장함
    /// ⚠️ 베이스 룬은 최대 레벨 도달 상태여야 함
    /// </summary>
    /// <param name="baseRuneUID">한계돌파할 룬의 UID</param>
    /// <returns>한계돌파 결과</returns>
    public LimitBreakResult TryLimitBreak(string baseRuneUID)
    {
        
        // 1. 베이스 룬 찾기
        var inventoryManager = RuneInventoryManager.Instance;
        RuneInstance baseRune = inventoryManager.GetRuneByUID(baseRuneUID);
        
        if (baseRune == null)
        {
            Debug.LogWarning($"[RuneEnhanceManager] 베이스 룬을 찾을 수 없습니다: {baseRuneUID}");
            return LimitBreakResult.BaseRuneNotFound;
        }
        
        
        // 2. [예외 처리] 베이스 룬이 최대 레벨에 도달했는지 확인
        if (baseRune.currentLevel < baseRune.GetCurrentMaxLevel())
        {
            int currentMaxLevel = baseRune.GetCurrentMaxLevel();
            Debug.LogWarning($"[RuneEnhanceManager] 베이스 룬이 최대 레벨에 도달하지 않았습니다: Lv.{baseRune.currentLevel} < {currentMaxLevel}");
            Debug.LogWarning("  💡 Tip: 한계돌파는 최대 레벨 도달 후에만 가능합니다.");
            return LimitBreakResult.BaseRuneNotMaxLevel;
        }
        
        // 3. [예외 처리] 이미 최대 한계돌파에 도달했는지 확인
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
        
        string runeId = baseRune.baseData.runeId;
        
        // 4. 해당 룬의 고유 조각 소지 확인 (Phase 6.5)
        int currentFragments = inventoryManager.GetFragmentCount(runeId);
        if (currentFragments < LIMIT_BREAK_COST)
        {
            Debug.LogWarning($"[RuneEnhanceManager] [{baseRune.baseData.runeName}] 조각이 부족합니다!");
            Debug.LogWarning($"  필요: {LIMIT_BREAK_COST}개 | 보유: {currentFragments}개");
            return LimitBreakResult.InsufficientGold; // 기존 enum 재사용
        }
        
        // 5. 조각 차감 (Phase 6.5: 해당 룬의 고유 조각만 차감)
        if (!inventoryManager.TryConsumeFragments(runeId, LIMIT_BREAK_COST))
        {
            Debug.LogError($"[RuneEnhanceManager] 조각 차감 실패: {runeId}");
            return LimitBreakResult.InsufficientGold;
        }
        
        // 6. 한계돌파 실행
        int oldLimitBreak = baseRune.currentLimitBreak;
        int oldMaxLevel = baseRune.GetCurrentMaxLevel();
        
        baseRune.currentLimitBreak++;
        
        int newLimitBreak = baseRune.currentLimitBreak;
        int newMaxLevel = baseRune.GetCurrentMaxLevel();
        
        Dbg.Log($"[RuneEnhanceManager] ✅ 한계돌파 성공!");
        Dbg.Log($"  한계돌파: {oldLimitBreak} → {newLimitBreak}");
        Dbg.Log($"  최대 레벨: Lv.{oldMaxLevel} → Lv.{newMaxLevel}");
        
        // 7. 이벤트 발생
        OnLimitBreak?.Invoke(baseRuneUID, newLimitBreak);
        
        // 8. 중요 안내 로그
        
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
}

