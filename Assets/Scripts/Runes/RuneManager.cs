using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 룬 시스템 관리자
/// ⚙️ Phase 4-C: 로비에서 룬을 장착/해제하고 조건부 효과를 PlayerRuntimeStats에 전달
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
    [Tooltip("최대 장착 가능한 룬 개수")]
    [SerializeField] private int maxRuneSlots = 5;
    
    [Header("=== 현재 장착된 룬 ===")]
    [SerializeField] private List<RuneData> equippedRunes = new List<RuneData>();
    
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
    /// 룬 장착 시 발생하는 이벤트 (RuneData)
    /// </summary>
    public event System.Action<RuneData> OnRuneEquipped;
    
    /// <summary>
    /// 룬 해제 시 발생하는 이벤트 (RuneData, slotIndex)
    /// </summary>
    public event System.Action<RuneData, int> OnRuneUnequipped;
    
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
            equippedRunes = new List<RuneData>(maxRuneSlots);
        }
        
        // 슬롯을 null로 채움 (빈 슬롯)
        while (equippedRunes.Count < maxRuneSlots)
        {
            equippedRunes.Add(null);
        }
        
        Debug.Log($"[RuneManager] 초기화 완료. 최대 슬롯: {maxRuneSlots}");
    }
    
    #endregion
    
    #region 룬 장착/해제
    
    /// <summary>
    /// 룬을 특정 슬롯에 장착
    /// </summary>
    /// <param name="rune">장착할 룬</param>
    /// <param name="slotIndex">슬롯 인덱스 (0부터 시작)</param>
    /// <returns>장착 성공 여부</returns>
    public bool EquipRune(RuneData rune, int slotIndex)
    {
        // 유효성 검사
        if (rune == null)
        {
            Debug.LogWarning("[RuneManager] 장착할 룬이 null입니다.");
            return false;
        }
        
        if (!rune.IsValid())
        {
            Debug.LogWarning($"[RuneManager] 유효하지 않은 룬: {rune.name}");
            return false;
        }
        
        if (slotIndex < 0 || slotIndex >= maxRuneSlots)
        {
            Debug.LogWarning($"[RuneManager] 잘못된 슬롯 인덱스: {slotIndex} (최대: {maxRuneSlots - 1})");
            return false;
        }
        
        // 이미 장착된 룬이 있다면 먼저 해제
        if (equippedRunes[slotIndex] != null)
        {
            UnequipRune(slotIndex);
        }
        
        // 룬 장착
        equippedRunes[slotIndex] = rune;
        isDirty = true; // 캐시 무효화
        
        Debug.Log($"[RuneManager] 룬 장착: [{rune.runeName}] → 슬롯 {slotIndex}");
        
        // 이벤트 발생
        OnRuneEquipped?.Invoke(rune);
        OnRunesChanged?.Invoke();
        
        // PlayerRuntimeStats에 조건부 모디파이어 전달
        UpdatePlayerStats();
        
        return true;
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
        
        var rune = equippedRunes[slotIndex];
        if (rune == null)
        {
            Debug.LogWarning($"[RuneManager] 슬롯 {slotIndex}에 장착된 룬이 없습니다.");
            return false;
        }
        
        // 룬 해제
        equippedRunes[slotIndex] = null;
        isDirty = true; // 캐시 무효화
        
        Debug.Log($"[RuneManager] 룬 해제: [{rune.runeName}] ← 슬롯 {slotIndex}");
        
        // 이벤트 발생
        OnRuneUnequipped?.Invoke(rune, slotIndex);
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
    /// 현재 장착된 모든 룬 (null 포함)
    /// </summary>
    public IReadOnlyList<RuneData> GetEquippedRunes()
    {
        return equippedRunes;
    }
    
    /// <summary>
    /// 현재 장착된 유효한 룬만 (null 제외)
    /// </summary>
    public List<RuneData> GetActiveRunes()
    {
        return equippedRunes.Where(r => r != null).ToList();
    }
    
    /// <summary>
    /// 특정 슬롯의 룬 가져오기
    /// </summary>
    public RuneData GetRuneAtSlot(int slotIndex)
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
    /// 현재 장착된 룬들로부터 조건부 모디파이어 수집
    /// </summary>
    private List<ConditionalModifier> CollectConditionalModifiers()
    {
        var result = new List<ConditionalModifier>();
        
        foreach (var rune in GetActiveRunes())
        {
            foreach (var modId in rune.ConditionalModifierIds)
            {
                var mod = ConditionalModifierDatabase.GetModifierById(modId);
                if (mod != null)
                {
                    // 출처 기록 (디버깅용)
                    mod.source = $"Rune: {rune.runeName}";
                    result.Add(mod);
                }
                else
                {
                    Debug.LogWarning($"[RuneManager] 모디파이어를 찾을 수 없음: {modId} (룬: {rune.runeName})");
                }
            }
        }
        
        Debug.Log($"[RuneManager] 활성 조건부 모디파이어: {result.Count}개");
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
        
        Debug.Log($"[RuneManager] PlayerRuntimeStats에 {modifiers.Count}개 조건부 모디파이어 전달");
    }
    
    #endregion
    
    #region 디버그
    
    [ContextMenu("Print Equipped Runes")]
    private void PrintEquippedRunes()
    {
        Debug.Log("=== 장착된 룬 ===");
        for (int i = 0; i < equippedRunes.Count; i++)
        {
            var rune = equippedRunes[i];
            if (rune != null)
            {
                Debug.Log($"  슬롯 {i}: {rune.runeName} ({rune.ConditionalModifierIds.Count}개 효과)");
            }
            else
            {
                Debug.Log($"  슬롯 {i}: (비어있음)");
            }
        }
    }
    
    [ContextMenu("Print Active Modifiers")]
    private void PrintActiveModifiers()
    {
        var modifiers = GetActiveConditionalModifiers();
        Debug.Log($"=== 활성 조건부 모디파이어: {modifiers.Count}개 ===");
        foreach (var mod in modifiers)
        {
            Debug.Log($"  - {mod.displayName} (출처: {mod.source})");
        }
    }
    
    #endregion
}

