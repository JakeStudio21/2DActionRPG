using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🛡️ Phase 1: 플레이어 상태이상 저항 스탯 관리
/// 상태이상별 저항 수치(0.0 ~ 1.0)를 관리하며, 저항률에 따라 지속시간이 감소함
/// </summary>
public class PlayerResistanceStats : MonoBehaviour
{
    #region 필드
    
    [Header("=== 🛡️ 상태이상 저항 시스템 (Phase 1) ===")]
    [Header("디버그: Inspector에서 실시간 수정 가능 (0.0 ~ 1.0)")]
    [Space(5)]
    
    [Tooltip("중독 저항 (0.0 = 저항 없음, 1.0 = 완전 면역)")]
    [Range(0f, 1f)]
    [SerializeField] private float poisonResistance = 0f;
    
    [Tooltip("화상 저항 (0.0 = 저항 없음, 1.0 = 완전 면역)")]
    [Range(0f, 1f)]
    [SerializeField] private float burnResistance = 0f;
    
    [Tooltip("속박 저항 (0.0 = 저항 없음, 1.0 = 완전 면역)")]
    [Range(0f, 1f)]
    [SerializeField] private float bindResistance = 0f;
    
    [Tooltip("둔화 저항 (0.0 = 저항 없음, 1.0 = 완전 면역)")]
    [Range(0f, 1f)]
    [SerializeField] private float slowResistance = 0f;
    
    [Tooltip("출혈 저항 (0.0 = 저항 없음, 1.0 = 완전 면역)")]
    [Range(0f, 1f)]
    [SerializeField] private float bleedResistance = 0f;
    
    [Tooltip("기절 저항 (0.0 = 저항 없음, 1.0 = 완전 면역)")]
    [Range(0f, 1f)]
    [SerializeField] private float stunResistance = 0f;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;  // ⭐ Production: false
    
    /// <summary>
    /// 저항 데이터 (상태이상 타입별)
    /// </summary>
    private Dictionary<EStatusEffectType, float> resistances;
    
    #endregion
    
    #region Unity 생명주기
    
    private void Awake()
    {
        InitializeResistances();
    }
    
    private void Start()
    {
        // 🛡️ Phase 2: 게임 시작 시 저장된 저항 데이터 자동 로드
        LoadFromPlayerData();
    }
    
    // 🔧 Phase 1: Update() 제거 - 성능 최적화 (매 프레임 동기화 불필요)
    // Inspector 값 변경은 OnValidate()에서 자동 처리됨
    
    private void OnValidate()
    {
        // Editor Only: Inspector 값 변경 시 자동 동기화
        if (Application.isPlaying && resistances != null)
        {
            SyncInspectorToDictionary();
        }
    }
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// 저항 Dictionary 초기화
    /// </summary>
    private void InitializeResistances()
    {
        resistances = new Dictionary<EStatusEffectType, float>
        {
            { EStatusEffectType.Poison, 0f },
            { EStatusEffectType.Burn, 0f },
            { EStatusEffectType.Bind, 0f },
            { EStatusEffectType.Slow, 0f },
            { EStatusEffectType.Bleed, 0f },
            { EStatusEffectType.Stun, 0f },
            { EStatusEffectType.Weaken, 0f },
            { EStatusEffectType.Vulnerable, 0f },
            { EStatusEffectType.HealBlock, 0f }
        };
        
        // Inspector 초기값 적용
        SyncInspectorToDictionary();
        
        if (enableDebugLogs)
            Dbg.Log($"🛡️ [PlayerResistanceStats] 저항 시스템 초기화 완료");
    }
    
    /// <summary>
    /// Inspector 값 → Dictionary 동기화 (디버그용)
    /// </summary>
    private void SyncInspectorToDictionary()
    {
        if (resistances == null) return;
        
        resistances[EStatusEffectType.Poison] = poisonResistance;
        resistances[EStatusEffectType.Burn] = burnResistance;
        resistances[EStatusEffectType.Bind] = bindResistance;
        resistances[EStatusEffectType.Slow] = slowResistance;
        resistances[EStatusEffectType.Bleed] = bleedResistance;
        resistances[EStatusEffectType.Stun] = stunResistance;
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 특정 상태이상에 대한 저항값 가져오기
    /// </summary>
    /// <param name="type">상태이상 타입</param>
    /// <returns>저항값 (0.0 ~ 1.0)</returns>
    public float GetResistance(EStatusEffectType type)
    {
        if (resistances == null)
        {
            Debug.LogWarning($"[PlayerResistanceStats] resistances가 초기화되지 않았습니다!");
            return 0f;
        }
        
        if (resistances.ContainsKey(type))
        {
            return resistances[type];
        }
        
        // 등록되지 않은 타입은 저항 0
        return 0f;
    }
    
    /// <summary>
    /// 특정 상태이상에 대한 저항값 설정
    /// </summary>
    /// <param name="type">상태이상 타입</param>
    /// <param name="value">저항값 (0.0 ~ 1.0, 자동 클램프)</param>
    public void SetResistance(EStatusEffectType type, float value)
    {
        if (resistances == null)
        {
            Debug.LogWarning($"[PlayerResistanceStats] resistances가 초기화되지 않았습니다!");
            return;
        }
        
        // 값 클램핑 (0.0 ~ 1.0)
        value = Mathf.Clamp01(value);
        
        resistances[type] = value;
        
        // Inspector 역동기화
        SyncDictionaryToInspector();
    }
    
    /// <summary>
    /// 특정 상태이상에 대한 저항값 추가 (누적)
    /// </summary>
    /// <param name="type">상태이상 타입</param>
    /// <param name="delta">추가할 저항값 (음수 가능)</param>
    public void AddResistance(EStatusEffectType type, float delta)
    {
        float currentValue = GetResistance(type);
        float newValue = Mathf.Clamp01(currentValue + delta);
        
        SetResistance(type, newValue);
    }
    
    /// <summary>
    /// Dictionary → Inspector 역동기화
    /// </summary>
    private void SyncDictionaryToInspector()
    {
        if (resistances == null) return;
        
        if (resistances.ContainsKey(EStatusEffectType.Poison))
            poisonResistance = resistances[EStatusEffectType.Poison];
        
        if (resistances.ContainsKey(EStatusEffectType.Burn))
            burnResistance = resistances[EStatusEffectType.Burn];
        
        if (resistances.ContainsKey(EStatusEffectType.Bind))
            bindResistance = resistances[EStatusEffectType.Bind];
        
        if (resistances.ContainsKey(EStatusEffectType.Slow))
            slowResistance = resistances[EStatusEffectType.Slow];
        
        if (resistances.ContainsKey(EStatusEffectType.Bleed))
            bleedResistance = resistances[EStatusEffectType.Bleed];
        
        if (resistances.ContainsKey(EStatusEffectType.Stun))
            stunResistance = resistances[EStatusEffectType.Stun];
    }
    
    #endregion
    
    #region 디버그 메서드
    
    /// <summary>
    /// 디버그 전용: 퍼센트 단위로 저항 설정 (0 ~ 100)
    /// </summary>
    public void DEBUG_SetResistancePercent(EStatusEffectType type, int percent)
    {
        float value = Mathf.Clamp01(percent / 100f);
        SetResistance(type, value);
        
    }
    
    /// <summary>
    /// 현재 저항 수치 출력
    /// </summary>
    [ContextMenu("Debug: Print All Resistances")]
    public void DEBUG_PrintAllResistances()
    {
        if (resistances == null)
        {
            return;
        }
        
        string info = "=== 🛡️ 플레이어 저항 수치 ===\n";
        
        foreach (var kvp in resistances)
        {
            if (kvp.Value > 0f)
            {
                info += $"  {kvp.Key}: {kvp.Value * 100:F0}%\n";
            }
        }
        
        if (resistances.Count == 0 || !info.Contains("%"))
        {
            info += "  (저항 없음)\n";
        }
        
    }
    
    /// <summary>
    /// 모든 저항 초기화
    /// </summary>
    [ContextMenu("Debug: Reset All Resistances")]
    public void DEBUG_ResetAllResistances()
    {
        foreach (var key in new List<EStatusEffectType>(resistances.Keys))
        {
            resistances[key] = 0f;
        }
        
        SyncDictionaryToInspector();
        
        Dbg.Log("🔧 [DEBUG] 모든 저항 초기화 완료");
    }
    
    #endregion
    
    #region Phase 2 준비: 저장/로드 인터페이스
    
    /// <summary>
    /// Phase 2 준비: Dictionary → 직렬화 가능한 형태로 변환
    /// </summary>
    public Dictionary<string, float> ExportResistances()
    {
        var export = new Dictionary<string, float>();
        
        foreach (var kvp in resistances)
        {
            export[kvp.Key.ToString()] = kvp.Value;
        }
        
        return export;
    }
    
    /// <summary>
    /// Phase 2 준비: 직렬화된 데이터 → Dictionary 로드
    /// </summary>
    public void ImportResistances(Dictionary<string, float> data)
    {
        if (data == null) return;
        
        foreach (var kvp in data)
        {
            if (System.Enum.TryParse<EStatusEffectType>(kvp.Key, out var type))
            {
                SetResistance(type, kvp.Value);
            }
        }
    }
    
    #endregion
    
    #region Phase 2: 저장/로드 시스템
    
    /// <summary>
    /// 🛡️ Phase 2: PlayerData에서 저항 데이터 로드
    /// </summary>
    public void LoadFromPlayerData()
    {
        var playerData = PlayerDataManager.Instance?.selectedPlayerData;
        if (playerData == null)
        {
            Debug.LogWarning("[PlayerResistanceStats] selectedPlayerData가 null입니다!");
            return;
        }
        
        if (playerData.resistanceStats == null || playerData.resistanceStats.Count == 0)
        {
            if (enableDebugLogs)
            return;
        }
        
        // List<ResistanceSaveData> → Dictionary 변환
        foreach (var data in playerData.resistanceStats)
        {
            SetResistance(data.type, data.value);
        }
        
        // Inspector에도 반영
        SyncDictionaryToInspector();
    }
    
    /// <summary>
    /// 🛡️ Phase 2: 현재 저항 데이터를 PlayerData에 저장
    /// </summary>
    public void SaveToPlayerData()
    {
        var playerData = PlayerDataManager.Instance?.selectedPlayerData;
        if (playerData == null)
        {
            Debug.LogWarning("[PlayerResistanceStats] selectedPlayerData가 null입니다!");
            return;
        }
        
        // Dictionary → List<ResistanceSaveData> 변환
        playerData.resistanceStats.Clear();
        foreach (var kvp in resistances)
        {
            if (kvp.Value > 0f) // 0보다 큰 값만 저장 (최적화)
            {
                playerData.resistanceStats.Add(new ResistanceSaveData(kvp.Key, kvp.Value));
            }
        }
        
        // 디스크 저장
        PlayerDataManager.Instance.SaveOnMeaningfulEvent("ResistanceStatsUpdated");
    }
    
    #endregion
}

