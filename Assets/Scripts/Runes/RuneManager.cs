using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 룬 시스템 관리자
/// ⚙️ Phase 4-D: 엔드 콘텐츠 확장 - 레벨업/한계돌파 시스템
/// 
/// 핵심 룰:
/// - 장착 슬롯: 최대 3개 (극딜형 빌드 가능)
/// - 중복 장착 불가: 같은 RuneId의 룬은 동시에 장착 불가
/// - 부옵션: 3, 6, 9레벨 도달 시 랜덤 개방 (한계돌파와 무관)
/// - 한계돌파: 중복 룬으로 최대 레벨 상한만 확장 (10→15)
/// </summary>
public class RuneManager : MonoBehaviour
{
    #region Singleton
    
    private static RuneManager _instance;
    public static RuneManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<RuneManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("RuneManager");
                    _instance = go.AddComponent<RuneManager>();
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
        
        Initialize();
    }
    
    #endregion
    
    #region 필드
    
    [Header("=== 룬 슬롯 설정 ===")]
    [Tooltip("최대 장착 가능한 룬 개수 (기본 3개, 극딜 빌드 가능)")]
    [SerializeField] private int maxRuneSlots = 3;
    
    [Header("=== 현재 장착된 룬 ===")]
    [SerializeField] private List<RuneInstance> equippedRunes = new List<RuneInstance>();
    
    /// <summary>
    /// 활성화된 모든 조건부 모디파이어 (캐시)
    /// </summary>
    private List<ConditionalModifier> cachedActiveModifiers = new List<ConditionalModifier>();
    
    /// <summary>
    /// 캐시 무효화 플래그
    /// </summary>
    private bool isDirty = true;
    
    #endregion
    
    #region 이벤트
    
    /// <summary>
    /// 룬 장착 시 발생하는 이벤트 (RuneInstance)
    /// </summary>
    public event System.Action<RuneInstance> OnRuneEquipped;
    
    /// <summary>
    /// 룬 해제 시 발생하는 이벤트 (RuneInstance, slotIndex)
    /// </summary>
    public event System.Action<RuneInstance, int> OnRuneUnequipped;
    
    /// <summary>
    /// 룬 변경 시 발생하는 이벤트 (전체 재계산 필요)
    /// </summary>
    public event System.Action OnRunesChanged;
    
    #endregion
    
    #region 초기화
    
    private void Initialize()
    {
        // 슬롯 초기화
        if (equippedRunes == null)
        {
            equippedRunes = new List<RuneInstance>(maxRuneSlots);
        }
        
        // 슬롯을 null로 채움 (빈 슬롯)
        while (equippedRunes.Count < maxRuneSlots)
        {
            equippedRunes.Add(null);
        }
        
        Dbg.Log($"[RuneManager] 초기화 완료. 최대 슬롯: {maxRuneSlots}");
    }
    
    #endregion
    
    #region 룬 장착/해제
    
    /// <summary>
    /// 룬 인스턴스를 특정 슬롯에 장착
    /// </summary>
    /// <param name="instance">장착할 룬 인스턴스</param>
    /// <param name="slotIndex">슬롯 인덱스 (0부터 시작)</param>
    /// <returns>장착 성공 여부</returns>
    public bool EquipRune(RuneInstance instance, int slotIndex)
    {
        // 유효성 검사
        if (instance == null)
        {
            Debug.LogWarning("[RuneManager] 장착할 룬 인스턴스가 null입니다.");
            return false;
        }
        
        if (instance.baseData == null)
        {
            Debug.LogWarning("[RuneManager] 룬 인스턴스의 baseData가 null입니다.");
            return false;
        }
        
        if (!instance.baseData.IsValid())
        {
            Debug.LogWarning($"[RuneManager] 유효하지 않은 룬: {instance.baseData.name}");
            return false;
        }
        
        if (slotIndex < 0 || slotIndex >= maxRuneSlots)
        {
            Debug.LogWarning($"[RuneManager] 잘못된 슬롯 인덱스: {slotIndex} (최대: {maxRuneSlots - 1})");
            return false;
        }
        
        // [핵심 제약] 중복 장착 방지: 같은 runeId를 가진 룬이 이미 장착되어 있는지 확인
        string targetRuneId = instance.baseDataId;
        for (int i = 0; i < equippedRunes.Count; i++)
        {
            // 같은 슬롯은 제외 (교체 허용)
            if (i == slotIndex) continue;
            
            var equippedRune = equippedRunes[i];
            if (equippedRune != null && equippedRune.baseDataId == targetRuneId)
            {
                Debug.LogWarning($"[RuneManager] ⚠️ 중복 장착 불가: [{targetRuneId}] {instance.baseData.runeName}이(가) 이미 슬롯 {i}에 장착되어 있습니다.");
                Debug.LogWarning($"  💡 Tip: 같은 종류의 룬은 동시에 장착할 수 없습니다.");
                return false;
            }
        }
        
        // 이미 장착된 룬이 있다면 먼저 해제
        if (equippedRunes[slotIndex] != null)
        {
            UnequipRune(slotIndex);
        }
        
        // 룬 장착
        equippedRunes[slotIndex] = instance;
        isDirty = true; // 캐시 무효화
        
        Dbg.Log($"[RuneManager] 룬 장착: {instance} → 슬롯 {slotIndex}");
        
        // 이벤트 발생
        OnRuneEquipped?.Invoke(instance);
        OnRunesChanged?.Invoke();
        
        // PlayerRuntimeStats에 조건부 모디파이어 전달
        UpdatePlayerStats();
        
        return true;
    }
    
    /// <summary>
    /// [Obsolete] 기존 호환성을 위한 래퍼 메서드
    /// 실제로는 임시 인스턴스를 생성하여 장착 (레벨 1, 한돌 0)
    /// </summary>
    [System.Obsolete("Use EquipRune(RuneInstance) instead. This method creates a temporary instance at level 1.")]
    public bool EquipRune(RuneData rune, int slotIndex)
    {
        if (rune == null)
        {
            Debug.LogWarning("[RuneManager] 장착할 룬이 null입니다.");
            return false;
        }
        
        // 임시 인스턴스 생성 (레벨 1, 한돌 0)
        var tempInstance = new RuneInstance(rune);
        Debug.LogWarning($"[RuneManager] Obsolete 메서드 사용: EquipRune(RuneData) → 임시 인스턴스 생성 (레벨 1)");
        
        return EquipRune(tempInstance, slotIndex);
    }
    
    /// <summary>
    /// 특정 슬롯의 룬을 해제
    /// </summary>
    /// <param name="slotIndex">슬롯 인덱스</param>
    /// <returns>해제 성공 여부</returns>
    public bool UnequipRune(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxRuneSlots)
        {
            Debug.LogWarning($"[RuneManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }
        
        var instance = equippedRunes[slotIndex];
        if (instance == null)
        {
            Debug.LogWarning($"[RuneManager] 슬롯 {slotIndex}에 장착된 룬이 없습니다.");
            return false;
        }
        
        // 룬 해제
        equippedRunes[slotIndex] = null;
        isDirty = true; // 캐시 무효화
        
        Dbg.Log($"[RuneManager] 룬 해제: {instance} ← 슬롯 {slotIndex}");
        
        // 이벤트 발생
        OnRuneUnequipped?.Invoke(instance, slotIndex);
        OnRunesChanged?.Invoke();
        
        // PlayerRuntimeStats에 조건부 모디파이어 갱신
        UpdatePlayerStats();
        
        return true;
    }
    
    /// <summary>
    /// 모든 룬 해제
    /// </summary>
    public void UnequipAllRunes()
    {
        for (int i = 0; i < maxRuneSlots; i++)
        {
            if (equippedRunes[i] != null)
            {
                UnequipRune(i);
            }
        }
    }
    
    #endregion
    
    #region 조회 메서드
    
    /// <summary>
    /// 현재 장착된 모든 룬 인스턴스 (null 포함)
    /// </summary>
    public IReadOnlyList<RuneInstance> GetEquippedRunes()
    {
        return equippedRunes;
    }
    
    /// <summary>
    /// 현재 장착된 유효한 룬 인스턴스만 (null 제외)
    /// </summary>
    public List<RuneInstance> GetActiveRunes()
    {
        return equippedRunes.Where(r => r != null).ToList();
    }
    
    /// <summary>
    /// 특정 슬롯의 룬 인스턴스 가져오기
    /// </summary>
    public RuneInstance GetRuneAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxRuneSlots)
            return null;
        
        return equippedRunes[slotIndex];
    }
    
    /// <summary>
    /// 현재 활성화된 모든 조건부 모디파이어 가져오기
    /// ⚡ GC 최적화: 캐시 사용
    /// </summary>
    public List<ConditionalModifier> GetActiveConditionalModifiers()
    {
        if (isDirty)
        {
            cachedActiveModifiers = CollectConditionalModifiers();
            isDirty = false;
        }
        
        return cachedActiveModifiers;
    }
    
    /// <summary>
    /// 현재 장착된 룬들로부터 조건부 모디파이어 수집 (레벨 보정 + 부옵션 포함)
    /// ⚙️ Phase 4-D: 레벨 보정 시스템 통합
    /// </summary>
    private List<ConditionalModifier> CollectConditionalModifiers()
    {
        var result = new List<ConditionalModifier>();
        
        foreach (var instance in GetActiveRunes())
        {
            if (instance.baseData == null)
            {
                Debug.LogWarning($"[RuneManager] 룬 인스턴스의 baseData가 null입니다. (UID: {instance.instanceUID})");
                continue;
            }
            
            // 1. 주옵션 (레벨 보정 적용, 1개만 존재)
            string mainModId = instance.baseData.MainStatModifierId;
            if (!string.IsNullOrEmpty(mainModId))
            {
                var mod = ConditionalModifierDatabase.GetModifierById(mainModId);
                if (mod != null)
                {
                    // ⚡ 레벨 보정: value × mainStatMultiplier
                    // 원본 ConditionalModifier는 불변이므로 새 인스턴스 생성
                    var adjustedMod = new ConditionalModifier(
                        mainModId,
                        mod.displayName,
                        mod.valueType,
                        mod.unit,
                        mod.conditionType,
                        mod.conditionParam,
                        mod.effectType,
                        mod.value * instance.GetMainStatMultiplier(), // 레벨 보정!
                        mod.applyPhase,
                        mod.targetScope,
                        mod.notes,
                        $"Rune: {instance.baseData.runeName} Lv.{instance.currentLevel}"
                    );
                    
                    result.Add(adjustedMod);
                }
                else
                {
                    Debug.LogWarning($"[RuneManager] 모디파이어를 찾을 수 없음: {mainModId} (룬: {instance.baseData.runeName})");
                }
            }
            
            // 2. 부옵션 (레벨 보정 없음, 원본 값 그대로)
            foreach (var subModId in instance.allocatedSubStatModifierIds)
            {
                var subMod = ConditionalModifierDatabase.GetModifierById(subModId);
                if (subMod != null)
                {
                    // 원본 값 그대로 사용 (부옵션은 레벨 보정 안 함)
                    var subModCopy = new ConditionalModifier(
                        subMod.modifierId,
                        subMod.displayName,
                        subMod.valueType,
                        subMod.unit,
                        subMod.conditionType,
                        subMod.conditionParam,
                        subMod.effectType,
                        subMod.value, // 원본 값 그대로!
                        subMod.applyPhase,
                        subMod.targetScope,
                        subMod.notes,
                        $"Rune: {instance.baseData.runeName} (부옵션)"
                    );
                    
                    result.Add(subModCopy);
                }
                else
                {
                    Debug.LogWarning($"[RuneManager] 부옵션 모디파이어를 찾을 수 없음: {subModId} (룬: {instance.baseData.runeName})");
                }
            }
        }
        
        Dbg.Log($"[RuneManager] 활성 조건부 모디파이어: {result.Count}개 (레벨 보정 적용)");
        return result;
    }
    
    #endregion
    
    #region PlayerRuntimeStats 연동
    
    /// <summary>
    /// PlayerRuntimeStats에 조건부 모디파이어 전달
    /// </summary>
    private void UpdatePlayerStats()
    {
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("[RuneManager] PlayerRuntimeStats를 찾을 수 없습니다. (로비에서는 정상)");
            return;
        }
        
        // 조건부 모디파이어 갱신
        var modifiers = GetActiveConditionalModifiers();
        playerStats.SetConditionalModifiers(modifiers);
        
        Dbg.Log($"[RuneManager] PlayerRuntimeStats에 {modifiers.Count}개 조건부 모디파이어 전달");
    }
    
    #endregion
}

