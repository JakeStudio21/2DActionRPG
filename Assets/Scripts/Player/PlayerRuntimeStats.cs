using UnityEngine;
using System.Collections.Generic;
using System.Collections; // ✅ 추가: IEnumerator 사용을 위해 필요
using System.Linq; // 🆕 Phase 3: LINQ (OrderBy, Sum 등)

/// <summary>
/// 🎯 PlayerRuntimeStats - 런타임 전용 최종 스탯 계산/관리 클래스
/// 책임: SelectedPlayerData + 장비 + 클래스배율을 종합하여 최종 스탯 제공
/// 위치: 플레이어 캐릭터 프리팹에 붙여서 개별 관리
/// </summary>
public class PlayerRuntimeStats : MonoBehaviour
{
    // 🆕 정적 이벤트: PlayerRuntimeStats 초기화 완료 알림
    public static event System.Action<PlayerRuntimeStats> OnPlayerRuntimeStatsReady;
    
    [Header("📊 최종 계산된 스탯 (읽기 전용)")]
    [SerializeField] private float finalAttackDamage = 10f;
    [SerializeField] private float finalMoveSpeed = 4f;
    
    // 이동속도 퍼센트 보너스 단리 합산용 (장비 + 패시브 + StatModifier 모두 여기에 누적 후 한 번에 적용)
    private float moveSpeedPercentBonus = 0f;
    [SerializeField] private float finalMaxHealth = 200f;
    [SerializeField] private float finalAttackSpeed = 1f;
    [SerializeField] private float finalCriticalChance = 0f;
    [SerializeField] private float finalCriticalDamage = 1.5f;
    [SerializeField] private float finalDefense = 0f;
    [SerializeField] private float finalHealMultiplier = 1.0f;  // 🆕 회복 효율
    
    [Header("📊 특수 스탯 (10종 확장)")]
    [SerializeField] private float finalSkillDamageBonus = 0f;      // 스킬 피해 증가 (소수, 0.1 = 10%)
    [SerializeField] private float finalCooldownReduction = 0f;     // 쿨다운 감소 (소수, cap 0.5)
    [SerializeField] private float finalDamageReduction = 0f;       // 받는 피해 감소 (소수, cap 0.8)
    [SerializeField] private float finalHpRegen = 0f;               // 초당 체력 회복 (HP/sec)
    [SerializeField] private float finalLifeSteal = 0f;             // 흡혈 (소수, 0.1 = 10%)
    [SerializeField] private float finalArmorPenetration = 0f;      // 방어구 관통 (소수, 0.1 = 10%)
    [SerializeField] private float finalDodgeChance = 0f;           // 회피 확률 (소수, cap 1.0)
    [SerializeField] private float finalBlockChance = 0f;           // 블록 확률 (소수, cap 1.0)
    [SerializeField] private float finalExpGainBonus = 0f;          // 경험치 획득 증가 (소수, 0.1 = 10%)
    [SerializeField] private float finalStatusResist = 0f;          // 상태이상 저항 (소수, cap 1.0)
    [SerializeField] private float finalPierceDamageRetention = 0.5f; // 관통 시 데미지 유지율 (기본 50%, cap 1.0)

    [Header("🔗 데이터 연결")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 프로퍼티로 외부 접근 제공
    public float FinalAttackDamage => finalAttackDamage;
    public float FinalMoveSpeed => finalMoveSpeed;
    public float FinalMaxHealth => finalMaxHealth;
    public float FinalAttackSpeed => finalAttackSpeed;
    public float FinalCriticalChance => finalCriticalChance;
    public float FinalCriticalDamage => finalCriticalDamage;
    public float FinalDefense => finalDefense;
    public float FinalHealMultiplier => finalHealMultiplier;  // 🆕 회복 효율
    
    // 특수 스탯 프로퍼티 (10종)
    public float FinalSkillDamageBonus => finalSkillDamageBonus;
    public float FinalCooldownReduction => finalCooldownReduction;
    public float FinalDamageReduction => finalDamageReduction;
    public float FinalHpRegen => finalHpRegen;
    public float FinalLifeSteal => finalLifeSteal;
    public float FinalArmorPenetration => finalArmorPenetration;
    public float FinalDodgeChance => finalDodgeChance;
    public float FinalBlockChance => finalBlockChance;
    public float FinalExpGainBonus => finalExpGainBonus;
    public float FinalStatusResist => finalStatusResist;
    public float FinalPierceDamageRetention => finalPierceDamageRetention;
    
#if UNITY_EDITOR
    // ─── 🔧 DEBUG: 임시 오버라이드 (에디터 전용, 빌드 제외, 저장 안됨) ───────
    
    /// <summary>
    /// StatDebugOverrideWindow에서 전달하는 임시 스탯 보너스 구조체.
    /// 빌드 시 완전히 제거됩니다.
    /// </summary>
    public struct DebugStatBonus
    {
        public float atkFlat;       // ATK_FLAT 직접 가산
        public float atkPercent;    // ATK_PERCENT — 플랫 합산 후 곱연산
        public float maxHp;
        public float defense;
        public float moveSpeed;
        public float atkSpeed;
        public float critChance;
        public float critDmg;
        public float healMult;
        public float skillDmg;
        public float cdr;
        public float dmgRed;
        public float hpRegen;
        public float lifeSteal;
        public float armorPen;
        public float dodge;
        public float block;
        public float expGain;
        public float statusResist;
        public float pierceRetention;
    }
    
    private DebugStatBonus _debugBonus;
    
    /// <summary>디버그 보너스 설정 후 즉시 재계산 — StatDebugOverrideWindow에서 호출</summary>
    public void SetDebugBonus(DebugStatBonus bonus)
    {
        _debugBonus = bonus;
        RecalculateAllStats();
    }
    
    /// <summary>현재 적용 중인 디버그 보너스 반환 — 에디터 창 값 동기화용</summary>
    public DebugStatBonus GetDebugBonus() => _debugBonus;
    
    /// <summary>디버그 보너스 전체 초기화 후 재계산</summary>
    public void ClearDebugBonus()
    {
        _debugBonus = default;
        RecalculateAllStats();
    }
    
    private bool HasAnyDebugBonus()
    {
        var b = _debugBonus;
        return b.atkFlat != 0 || b.atkPercent != 0 || b.maxHp != 0 || b.defense != 0 ||
               b.moveSpeed != 0 || b.atkSpeed != 0 || b.critChance != 0 || b.critDmg != 0 ||
               b.healMult != 0 || b.skillDmg != 0 || b.cdr != 0 || b.dmgRed != 0 ||
               b.hpRegen != 0 || b.lifeSteal != 0 || b.armorPen != 0 || b.dodge != 0 ||
               b.block != 0 || b.expGain != 0 || b.statusResist != 0 || b.pierceRetention != 0;
    }
    
    /// <summary>
    /// ValidateStats() 직후 호출. 최종 필드에 디버그 보너스를 직접 더하고 범위를 재검증한다.
    /// 흐름: [flat 보너스] → [ATK_PERCENT 곱연산] → [재 Clamp]
    /// </summary>
    private void ApplyDebugBonusOverride()
    {
        if (!HasAnyDebugBonus()) return;
        
        var b = _debugBonus;
        
        // Step 1: 플랫(Flat) 보너스 직접 가산
        finalAttackDamage    += b.atkFlat;
        finalMaxHealth       += b.maxHp;
        finalDefense         += b.defense;
        finalMoveSpeed       += b.moveSpeed;
        finalAttackSpeed     += b.atkSpeed;
        finalCriticalChance  += b.critChance;
        finalCriticalDamage  += b.critDmg;
        finalHealMultiplier  += b.healMult;
        finalSkillDamageBonus  += b.skillDmg;
        finalCooldownReduction += b.cdr;
        finalDamageReduction   += b.dmgRed;
        finalHpRegen         += b.hpRegen;
        finalLifeSteal       += b.lifeSteal;
        finalArmorPenetration += b.armorPen;
        finalDodgeChance     += b.dodge;
        finalBlockChance     += b.block;
        finalExpGainBonus    += b.expGain;
        finalStatusResist           += b.statusResist;
        finalPierceDamageRetention  += b.pierceRetention;
        
        // Step 2: ATK_PERCENT 보너스 — 플랫 합산 후 곱연산 (ATK_PERCENT 단독 테스트용)
        if (b.atkPercent != 0f)
            finalAttackDamage *= (1f + b.atkPercent);
        
        // Step 3: 오버라이드 후 범위 재검증 (cap 초과 방지)
        finalAttackDamage      = Mathf.Max(1f, finalAttackDamage);
        finalMaxHealth         = Mathf.Max(1f, finalMaxHealth);
        finalDefense           = Mathf.Max(0f, finalDefense);
        finalMoveSpeed         = Mathf.Clamp(finalMoveSpeed, 0.3f, 20f);
        finalAttackSpeed       = Mathf.Clamp(finalAttackSpeed, 0.1f, 5f);
        finalCriticalChance    = Mathf.Clamp01(finalCriticalChance);
        finalCriticalDamage    = Mathf.Max(1f, finalCriticalDamage);
        finalSkillDamageBonus  = Mathf.Max(0f, finalSkillDamageBonus);
        finalCooldownReduction = Mathf.Clamp(finalCooldownReduction, 0f, 0.5f);
        finalDamageReduction   = Mathf.Clamp(finalDamageReduction,   0f, 0.8f);
        finalHpRegen           = Mathf.Max(0f, finalHpRegen);
        finalLifeSteal         = Mathf.Clamp(finalLifeSteal,         0f, 1.0f);
        finalArmorPenetration  = Mathf.Clamp(finalArmorPenetration,  0f, 1.0f);
        finalDodgeChance       = Mathf.Clamp01(finalDodgeChance);
        finalBlockChance       = Mathf.Clamp01(finalBlockChance);
        finalExpGainBonus      = Mathf.Max(0f, finalExpGainBonus);
        finalStatusResist      = Mathf.Clamp01(finalStatusResist);
        
        if (showDebugLogs)
            Debug.Log($"🔧 [PlayerRuntimeStats] DEBUG 오버라이드 적용 — " +
                      $"ATK +{b.atkFlat:F1} x{1f + b.atkPercent:F2}, HP +{b.maxHp:F0}, DEF +{b.defense:F1}");
    }
#endif
    
    /// <summary>
    /// 현재 플레이어 레벨 (Dynamic K 계산용)
    /// </summary>
    public int CurrentLevel => playerData != null ? playerData.currentLevel : 1;
    
    // 내부 참조
    private SelectedPlayerData playerData;
    private IPlayerClass currentClass;
    private PlayerDataManager dataManager;
    
    // 🆕 확장된 이벤트 시스템
    public System.Action OnStatsRecalculated;
    public System.Action<float, float> OnAttackDamageChanged;  // (old, new)
    public System.Action<float, float> OnMoveSpeedChanged;     // (old, new)
    public System.Action<float, float> OnMaxHealthChanged;    // (old, new)
    public System.Action<float, float> OnCriticalChanceChanged; // (old, new)
    public System.Action<float, float> OnDefenseChanged;      // (old, new)
    
    // 이전 값 저장 (변경 감지용)
    private float previousAttackDamage = 0f;
    private float previousMoveSpeed = 0f;
    private float previousMaxHealth = 0f;
    private float previousCriticalChance = 0f;
    private float previousDefense = 0f;
    
    // 🆕 임시 스탯 변경 시스템
    private List<IBuffEffect> activeBuffs = new List<IBuffEffect>();
    private float temporaryAttackDamage = 0f;
    private float temporaryMoveSpeed = 0f;
    private float temporaryMaxHealth = 0f;
    private float temporaryDefense = 0f;
    
    // 🆕 Phase 3: StatModifier 시스템
    private List<StatModifier> activeModifiers = new List<StatModifier>();
    private Dictionary<EStatType, float> modifierCache = new Dictionary<EStatType, float>();
    private bool isModifierCacheDirty = true;
    
    // 🆕 Phase 4-C: ConditionalModifier 시스템 (룬 전용)
    private List<ConditionalModifier> activeConditionalModifiers = new List<ConditionalModifier>();
    
    // 🆕 Phase 1: 패시브 스킬 스탯 보너스 (스킬 시스템 개편)
    private Dictionary<string, List<PassiveStatBonus>> passiveSkillBonuses = new Dictionary<string, List<PassiveStatBonus>>();
    
    private void Awake()
    {
        // 🔧 추가: 참조 초기화를 Awake에서 실행 (Start보다 먼저)
        InitializeReferences();
    }

    private void Start()
    {
        // 🗑️ 제거: InitializeReferences() 호출 제거 (Awake로 이동)
        // InitializeReferences();
        
        // 모든 스탯 계산
        RecalculateAllStats();
        
        if (showDebugLogs)
            Debug.Log("🔗 [PlayerRuntimeStats] 초기화 완료");
    }
    
    private void OnDestroy()
    {
        // 🆕 이벤트 해제
        if (dataManager != null)
        {
            dataManager.OnItemEquipped -= OnItemEquipped;
            dataManager.OnItemUnequipped -= OnItemUnequipped;
            dataManager.OnLevelChanged -= OnLevelChanged;
        }
        
        // 🗑️ 제거: 존재하지 않는 이벤트 참조 삭제
        // playerData.OnRuntimeEquippedItemsChanged -= OnEquipmentChanged;
    }
    
    /// <summary>
    /// 🎯 장비 착용 시 스탯 재계산
    /// </summary>
    private void OnItemEquipped(EquipmentSlot slot, EquipmentData item)
    {
        RecalculateAllStats();
        if (showDebugLogs)
            Debug.Log($"📦 [PlayerRuntimeStats] 장비 착용으로 스탯 재계산: {item?.equipmentName}");
    }
    
    /// <summary>
    /// 🎯 장비 해제 시 스탯 재계산
    /// </summary>
    private void OnItemUnequipped(EquipmentSlot slot, EquipmentData item)
    {
        RecalculateAllStats();
        if (showDebugLogs)
            Debug.Log($"📤 [PlayerRuntimeStats] 장비 해제로 스탯 재계산: {item?.equipmentName}");
    }
    
    /// <summary>
    /// 🎯 레벨업 시 스탯 재계산
    /// </summary>
    private void OnLevelChanged(int newLevel)
    {
        RecalculateAllStats();
        if (showDebugLogs)
            Debug.Log($"⬆️ [PlayerRuntimeStats] 레벨업으로 스탯 재계산: Lv.{newLevel}");
    }
    
    /// <summary>
    /// 🔗 필수 참조들 초기화
    /// </summary>
    private void InitializeReferences()
    {
        // ⭐ Tutorial 모드 확인
        if (TutorialManager.Instance != null)
        {
            if (showDebugLogs)
                Debug.Log("🎓 [PlayerRuntimeStats] Tutorial 모드 감지 - 기본 스탯 사용");
            
            // Tutorial 모드에서는 dataManager, playerData를 null로 유지
            // RecalculateAllStats()에서 자동으로 기본값 사용됨
        }
        else
        {
            // 일반 모드: PlayerDataManager 참조
            dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                playerData = dataManager.selectedPlayerData;
                // 🗑️ 제거: 존재하지 않는 이벤트 참조 삭제  
                // playerData.OnRuntimeEquippedItemsChanged += OnEquipmentChanged;
            }
        }
        
        // 현재 활성 클래스 찾기
        currentClass = GetComponent<IPlayerClass>();
        if (currentClass == null)
        {
            // 자식 컴포넌트에서 찾기
            currentClass = GetComponentInChildren<IPlayerClass>();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"🔗 [PlayerRuntimeStats] 참조 초기화 완료");
            Debug.Log($"   - Tutorial 모드: {(TutorialManager.Instance != null ? "✅" : "❌")}");
            Debug.Log($"   - DataManager: {(dataManager != null ? "✅" : "❌")}");
            Debug.Log($"   - PlayerData: {(playerData != null ? "✅" : "❌")}");
            Debug.Log($"   - CurrentClass: {(currentClass != null ? currentClass.GetType().Name : "❌")}");
        }
    }
    
    /// <summary>
    /// 🔄 전체 스탯 재계산 (무기교체, 레벨업, 장비변경 시 호출)
    /// </summary>
    public void RecalculateAllStats()
    {
        if (playerData == null)
        {
            Debug.LogWarning("⚠️ [PlayerRuntimeStats] PlayerData가 없어 기본값 사용");
            SetDefaultStats();
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"🔄 [PlayerRuntimeStats] 스탯 재계산 시작...");
        
        // 🆕 이전 값 저장 (변경 감지용)
        StorePreviousStats();
        
        // 1단계: 기본 스탯 계산 (레벨 기반)
        CalculateBaseStats();
        
        // 2단계: 장비 스탯 추가 (기존 시스템)
        ApplyEquipmentStats();
        
        // 🆕 2.5단계: 패시브 스킬 스탯 적용 (Phase 1: 스킬 시스템)
        ApplyPassiveSkillStats();
        
        // 🆕 2.7단계: StatModifier 시스템 적용 (Phase 순서대로)
        ApplyStatModifiers();
        
        // 3단계: 클래스 배율 적용
        ApplyClassMultipliers();
        
        // 4단계: 추가 효과 적용 (버프/디버프)
        ApplyBuffEffects();
        
        // 5단계: 최종 검증 및 제한
        ValidateStats();
        
#if UNITY_EDITOR
        // 5.5단계: DEBUG 임시 오버라이드 (에디터 전용, 저장 안됨)
        ApplyDebugBonusOverride();
#endif
        
        // 🆕 6단계: 스탯 변경 감지 및 이벤트 발생
        DetectAndTriggerStatChanges();
        
        // 7단계: 다른 컴포넌트들과 자동 동기화
        SyncWithOtherComponents();
        
        // 총괄 이벤트 발생
        OnStatsRecalculated?.Invoke();
        
        if (showDebugLogs)
            LogFinalStats();
    }
    
    /// <summary>
    /// 📊 이전 스탯 값 저장
    /// </summary>
    private void StorePreviousStats()
    {
        previousAttackDamage = finalAttackDamage;
        previousMoveSpeed = finalMoveSpeed;
        previousMaxHealth = finalMaxHealth;
        previousCriticalChance = finalCriticalChance;
        previousDefense = finalDefense;
    }
    
    /// <summary>
    /// 🔍 스탯 변경 감지 및 개별 이벤트 발생
    /// </summary>
    private void DetectAndTriggerStatChanges()
    {
        const float threshold = 0.01f; // 변경 감지 임계값
        
        // 공격력 변경 감지
        if (Mathf.Abs(finalAttackDamage - previousAttackDamage) > threshold)
        {
            OnAttackDamageChanged?.Invoke(previousAttackDamage, finalAttackDamage);
            if (showDebugLogs)
                Debug.Log($"⚔️ [PlayerRuntimeStats] 공격력 변경: {previousAttackDamage:F1} → {finalAttackDamage:F1}");
        }
        
        // 이동속도 변경 감지
        if (Mathf.Abs(finalMoveSpeed - previousMoveSpeed) > threshold)
        {
            OnMoveSpeedChanged?.Invoke(previousMoveSpeed, finalMoveSpeed);
            if (showDebugLogs)
                Debug.Log($"🏃 [PlayerRuntimeStats] 이동속도 변경: {previousMoveSpeed:F1} → {finalMoveSpeed:F1}");
        }
        
        // 최대체력 변경 감지
        if (Mathf.Abs(finalMaxHealth - previousMaxHealth) > threshold)
        {
            OnMaxHealthChanged?.Invoke(previousMaxHealth, finalMaxHealth);
            if (showDebugLogs)
                Debug.Log($"❤️ [PlayerRuntimeStats] 최대체력 변경: {previousMaxHealth:F0} → {finalMaxHealth:F0}");
        }
        
        // 크리티컬 확률 변경 감지
        if (Mathf.Abs(finalCriticalChance - previousCriticalChance) > threshold)
        {
            OnCriticalChanceChanged?.Invoke(previousCriticalChance, finalCriticalChance);
            if (showDebugLogs)
                Debug.Log($"🎯 [PlayerRuntimeStats] 크리티컬 확률 변경: {previousCriticalChance:P1} → {finalCriticalChance:P1}");
        }
        
        // 방어력 변경 감지
        if (Mathf.Abs(finalDefense - previousDefense) > threshold)
        {
            OnDefenseChanged?.Invoke(previousDefense, finalDefense);
            if (showDebugLogs)
                Debug.Log($"🛡️ [PlayerRuntimeStats] 방어력 변경: {previousDefense:F1} → {finalDefense:F1}");
        }
    }
    
    /// <summary>
    /// 📊 1단계: 기본 스탯 계산 (레벨 기반)
    /// ✅ CSV 밸런싱: ScriptableObject에서 모든 기본값 가져옴
    /// </summary>
    private void CalculateBaseStats()
    {
        int currentLevel = playerData.currentLevel;
        
        // ========================================
        // 📈 성장 스탯 (레벨업으로 증가)
        // ========================================
        
        // ⚔️ 공격력: 목표 역산 방식 (기존 공식 유지)
        float characterBaseAttack = GetCharacterBaseAttack(currentLevel);
        float levelBonus = (currentLevel - 1) * 2f;  // 범용 레벨 보너스
        finalAttackDamage = characterBaseAttack + levelBonus;
        
        // ❤️ 체력: baseMaxHealth + (레벨당 증가량)
        float baseHealth = GetBaseMaxHealthFromClass();
        float hpGain = GetHpGainPerLevelFromClass();
        finalMaxHealth = baseHealth + (currentLevel - 1) * hpGain;
        
        // 🛡️ 방어력: baseDefense + (레벨당 증가량) ✅ CSV 조정 가능
        float baseDefense = GetBaseDefenseFromClass();
        float defenseGain = GetDefenseGainPerLevelFromClass();
        finalDefense = baseDefense + (currentLevel - 1) * defenseGain;
        
        // ========================================
        // 🎯 고정 스탯 (클래스 고유 특성)
        // ========================================
        
        // 🎯 크리티컬 ✅ CSV 조정 가능
        float baseCritRate = GetBaseCritRateFromClass();
        float baseCritDmg = GetBaseCritDamageFromClass();
        finalCriticalChance = baseCritRate;
        finalCriticalDamage = baseCritDmg;
        
        // ⚡ 공격속도 ✅ CSV 조정 가능
        float baseAtkSpeed = GetBaseAttackSpeedFromClass();
        finalAttackSpeed = baseAtkSpeed;
        
        // 🏃 이동속도 (클래스 배율은 ApplyClassMultipliers에서 적용)
        float baseMoveSpeed = GetBaseMoveSpeedFromClass();
        finalMoveSpeed = baseMoveSpeed;
        moveSpeedPercentBonus = 0f; // 퍼센트 보너스 누적용 초기화 (매 재계산마다 리셋)
        
        // 💚 회복 효율 ✅ CSV 조정 가능
        float healMult = GetHealMultiplierFromClass();
        finalHealMultiplier = healMult;
        
        // 📊 특수 스탯 10종 초기화 (매 재계산 시 누적 방지)
        finalSkillDamageBonus = 0f;
        finalCooldownReduction = 0f;
        finalDamageReduction = 0f;
        finalHpRegen = 0f;
        finalLifeSteal = 0f;
        finalArmorPenetration = 0f;
        finalDodgeChance = 0f;
        finalBlockChance = 0f;
        finalExpGainBonus = 0f;
        finalStatusResist = 0f;
        finalPierceDamageRetention = 0.5f; // 기본값 50%
        
        if (showDebugLogs)
        {
            Debug.Log($"📊 [PlayerRuntimeStats] 기본 스탯 계산 완료 (Lv.{currentLevel})");
            Debug.Log($"  📈 성장: 공격력 {finalAttackDamage:F1}, 체력 {finalMaxHealth:F0}, 방어력 {finalDefense:F1}");
            Debug.Log($"  🎯 고정: 크리 {finalCriticalChance:P0}(x{finalCriticalDamage:F1}), 공속 {finalAttackSpeed:F1}, 회복 {finalHealMultiplier:P0}");
        }
    }
    
    /// <summary>
    /// ⚔️ 2단계: 장비 스탯 추가 (StatModifier 기반 - Phase A 완전 전환)
    /// </summary>
    private void ApplyEquipmentStats()
    {
        if (playerData == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[PlayerRuntimeStats] PlayerData가 없습니다. 장비 스탯 적용 스킵.");
            return;
        }
        
        var equippedItems = playerData.RuntimeEquippedItems;
        var equippedInstanceIds = playerData.RuntimeEquippedInstanceIds;
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [PlayerRuntimeStats] 장비 스탯 적용: {equippedItems.Count}개 장비");
        
        foreach (var kvp in equippedItems)
        {
            EquipmentSlot slot = kvp.Key;
            EquipmentData equipment = kvp.Value;
            
            if (equipment == null) continue;
            
            // EquipmentInstance 기반으로 스탯 조회 (동적 주옵션/부옵션 포함)
            List<StatModifier> modifiers = null;
            
            if (AccountDataManager.Instance != null &&
                equippedInstanceIds.TryGetValue(slot, out ItemInstanceID instanceId) &&
                !instanceId.IsEmpty)
            {
                var equipmentInstance = AccountDataManager.Instance.CreateEquipmentInstance(instanceId);
                if (equipmentInstance != null)
                    modifiers = equipmentInstance.GetStatModifiers();
            }
            
            // 폴백: EquipmentInstance 생성 실패 시 EquipmentData 사용
            if (modifiers == null)
            {
                modifiers = equipment.GetStatModifiers();
                if (showDebugLogs)
                    Debug.LogWarning($"  ⚠️ {slot}: {equipment.equipmentName} - EquipmentInstance 없음, EquipmentData 폴백 사용");
            }
            
            if (showDebugLogs)
                Debug.Log($"  📦 {slot}: {equipment.equipmentName} - {modifiers.Count}개 StatModifier");
            
            foreach (var modifier in modifiers)
            {
                // StatModifier는 이미 /100f 변환 완료
                switch (modifier.statType)
                {
                    case EStatType.ATK_FLAT:
                        finalAttackDamage += modifier.value;
                        if (showDebugLogs) Debug.Log($"    ⚔️ ATK_FLAT: +{modifier.value:F2} → {finalAttackDamage:F2}");
                        break;
                        
                    case EStatType.DEF_FLAT:
                        finalDefense += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🛡️ DEF_FLAT: +{modifier.value:F2} → {finalDefense:F2}");
                        break;
                        
                    case EStatType.HP_FLAT:
                        finalMaxHealth += modifier.value;
                        if (showDebugLogs) Debug.Log($"    ❤️ HP_FLAT: +{modifier.value:F0} → {finalMaxHealth:F0}");
                        break;
                        
                    case EStatType.MOVE_SPEED:
                        moveSpeedPercentBonus += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🏃 MOVE_SPEED(장비): +{modifier.value:P0} 누적 → 보너스 합계 {moveSpeedPercentBonus:P0}");
                        break;
                        
                    case EStatType.ASPD:
                        finalAttackSpeed *= (1f + modifier.value);
                        if (showDebugLogs) Debug.Log($"    💨 ASPD: x{1f + modifier.value:F2} → {finalAttackSpeed:F2}");
                        break;
                        
                    case EStatType.CRIT_RATE:
                        finalCriticalChance += modifier.value; // 0.30 = 30%
                        if (showDebugLogs) Debug.Log($"    🎯 CRIT_RATE: +{modifier.value:P2} → {finalCriticalChance:P2}");
                        break;
                        
                    case EStatType.CRIT_DMG:
                        finalCriticalDamage += modifier.value;
                        if (showDebugLogs) Debug.Log($"    💥 CRIT_DMG: +{modifier.value:P2} → {finalCriticalDamage:P2}");
                        break;
                    
                    case EStatType.SKILL_DMG_PERCENT:
                        finalSkillDamageBonus += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🎯 SKILL_DMG_PERCENT: +{modifier.value:P2} → {finalSkillDamageBonus:P2}");
                        break;
                    
                    case EStatType.COOLDOWN_REDUCTION:
                        finalCooldownReduction += modifier.value;
                        if (showDebugLogs) Debug.Log($"    ⏱️ COOLDOWN_REDUCTION: +{modifier.value:P2} → {finalCooldownReduction:P2}");
                        break;
                    
                    case EStatType.DAMAGE_REDUCTION_PERCENT:
                        finalDamageReduction += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🛡️ DAMAGE_REDUCTION_PERCENT: +{modifier.value:P2} → {finalDamageReduction:P2}");
                        break;
                    
                    case EStatType.HP_REGEN:
                        finalHpRegen += modifier.value;
                        if (showDebugLogs) Debug.Log($"    💚 HP_REGEN: +{modifier.value:F2} → {finalHpRegen:F2}");
                        break;
                    
                    case EStatType.LIFESTEAL:
                        finalLifeSteal += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🩸 LIFESTEAL: +{modifier.value:P2} → {finalLifeSteal:P2}");
                        break;
                    
                    case EStatType.ARMOR_PENETRATION:
                        finalArmorPenetration += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🔓 ARMOR_PENETRATION: +{modifier.value:P2} → {finalArmorPenetration:P2}");
                        break;
                    
                    case EStatType.DODGE_CHANCE:
                        finalDodgeChance += modifier.value;
                        if (showDebugLogs) Debug.Log($"    💨 DODGE_CHANCE: +{modifier.value:P2} → {finalDodgeChance:P2}");
                        break;
                    
                    case EStatType.BLOCK_CHANCE:
                        finalBlockChance += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🛑 BLOCK_CHANCE: +{modifier.value:P2} → {finalBlockChance:P2}");
                        break;
                    
                    case EStatType.EXP_GAIN_PERCENT:
                        finalExpGainBonus += modifier.value;
                        if (showDebugLogs) Debug.Log($"    ⭐ EXP_GAIN_PERCENT: +{modifier.value:P2} → {finalExpGainBonus:P2}");
                        break;
                    
                    case EStatType.STATUS_RESIST_ALL:
                        finalStatusResist += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🔮 STATUS_RESIST_ALL: +{modifier.value:P2} → {finalStatusResist:P2}");
                        break;
                    
                    case EStatType.PIERCE_DAMAGE_RETENTION:
                        finalPierceDamageRetention += modifier.value;
                        if (showDebugLogs) Debug.Log($"    🏹 PIERCE_DAMAGE_RETENTION: +{modifier.value:P2} → {finalPierceDamageRetention:P2}");
                        break;
                        
                    default:
                        if (showDebugLogs) Debug.LogWarning($"    ⚠️ 처리되지 않은 스탯: {modifier.statType}");
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// 🎭 3단계: 클래스 배율 적용
    /// </summary>
    private void ApplyClassMultipliers()
    {
        if (currentClass == null) return;
        
        // 클래스별 배율 적용
        finalAttackDamage *= currentClass.AttackPowerMultiplier;
        
        // 이동속도: 퍼센트 보너스 단리 일괄 적용 → 클래스 배율 순서로 적용
        // 공식: finalMoveSpeed = baseMoveSpeed * (1 + 장비%합 + 패시브%합 + Modifier%합) * 클래스배율
        finalMoveSpeed *= (1f + moveSpeedPercentBonus);
        finalMoveSpeed *= currentClass.MoveSpeedMultiplier;
        
        finalMaxHealth *= currentClass.HealthMultiplier;
        // finalAttackSpeed는 SkillCooldownMultiplier와 별개로 관리
        
        if (showDebugLogs)
        {
            Debug.Log($"🎭 [PlayerRuntimeStats] {currentClass.ClassName} 배율 적용:");
            Debug.Log($"   - 공격력 x{currentClass.AttackPowerMultiplier} = {finalAttackDamage}");
            Debug.Log($"   - 이동속도: base x(1+{moveSpeedPercentBonus:P0}) x{currentClass.MoveSpeedMultiplier} = {finalMoveSpeed:F2}");
            Debug.Log($"   - 체력 x{currentClass.HealthMultiplier} = {finalMaxHealth}");
        }
    }
    
    /// <summary>
    /// ✨ 4단계: 추가 효과 적용 (버프/디버프)
    /// </summary>
    private void ApplyBuffEffects()
    {
        // 임시 스탯 변경 적용
        finalAttackDamage += temporaryAttackDamage;
        finalMoveSpeed += temporaryMoveSpeed;
        finalMaxHealth += temporaryMaxHealth;
        finalDefense += temporaryDefense;
        
        if (showDebugLogs && (temporaryAttackDamage != 0 || temporaryMoveSpeed != 0 || temporaryMaxHealth != 0 || temporaryDefense != 0))
        {
            Debug.Log($"✨ [PlayerRuntimeStats] 버프 효과 적용:");
            if (temporaryAttackDamage != 0) Debug.Log($"   - 임시 공격력: +{temporaryAttackDamage}");
            if (temporaryMoveSpeed != 0) Debug.Log($"   - 임시 이동속도: +{temporaryMoveSpeed}");
            if (temporaryMaxHealth != 0) Debug.Log($"   - 임시 체력: +{temporaryMaxHealth}");
            if (temporaryDefense != 0) Debug.Log($"   - 임시 방어력: +{temporaryDefense}");
        }
    }
    
    /// <summary>
    /// ✅ 5단계: 최종 검증 및 제한
    /// </summary>
    private void ValidateStats()
    {
        // 최소/최대값 제한
        finalAttackDamage = Mathf.Max(1f, finalAttackDamage);
        finalMoveSpeed = Mathf.Clamp(finalMoveSpeed, 0.3f, 20f);
        finalMaxHealth = Mathf.Max(1f, finalMaxHealth);
        finalAttackSpeed = Mathf.Clamp(finalAttackSpeed, 0.1f, 5f);
        finalCriticalChance = Mathf.Clamp01(finalCriticalChance);
        finalCriticalDamage = Mathf.Max(1f, finalCriticalDamage);
        finalDefense = Mathf.Max(0f, finalDefense);
        
        // 특수 스탯 10종 범위 제한
        finalSkillDamageBonus  = Mathf.Max(0f, finalSkillDamageBonus);          // 하한 0%
        finalCooldownReduction = Mathf.Clamp(finalCooldownReduction, 0f, 0.5f); // 0% ~ 50%
        finalDamageReduction   = Mathf.Clamp(finalDamageReduction,   0f, 0.8f); // 0% ~ 80%
        finalHpRegen           = Mathf.Max(0f, finalHpRegen);                   // 하한 0
        finalLifeSteal         = Mathf.Clamp(finalLifeSteal,         0f, 1.0f); // 0% ~ 100%
        finalArmorPenetration  = Mathf.Clamp(finalArmorPenetration,  0f, 1.0f); // 0% ~ 100%
        finalDodgeChance       = Mathf.Clamp01(finalDodgeChance);               // 0% ~ 100%
        finalBlockChance       = Mathf.Clamp01(finalBlockChance);               // 0% ~ 100%
        finalExpGainBonus           = Mathf.Max(0f, finalExpGainBonus);         // 하한 0%
        finalStatusResist           = Mathf.Clamp01(finalStatusResist);         // 0% ~ 100%
        finalPierceDamageRetention  = Mathf.Clamp01(finalPierceDamageRetention); // 0% ~ 100%
    }
    
    /// <summary>
    /// 🔧 기본값 설정 (데이터 없을 때)
    /// </summary>
    private void SetDefaultStats()
    {
        finalAttackDamage = 10f;
        finalMoveSpeed = 4f;
        finalMaxHealth = 200f;
        finalAttackSpeed = 1f;
        finalCriticalChance = 0.05f;  // 5%
        finalCriticalDamage = 1.5f;
        finalDefense = 5f;
        finalHealMultiplier = 1.0f;  // 🆕
        
        // 특수 스탯 10종 기본값
        finalSkillDamageBonus = 0f;
        finalCooldownReduction = 0f;
        finalDamageReduction = 0f;
        finalHpRegen = 0f;
        finalLifeSteal = 0f;
        finalArmorPenetration = 0f;
        finalDodgeChance = 0f;
        finalBlockChance = 0f;
        finalExpGainBonus = 0f;
        finalStatusResist = 0f;
        finalPierceDamageRetention = 0.5f;
    }
    
    /// <summary>
    /// 📝 최종 스탯 로그 출력 (강화된 버전)
    /// </summary>
    private void LogFinalStats()
    {
        Debug.Log($"📊 [PlayerRuntimeStats] =====최종 스탯 계산 완료=====");
#if UNITY_EDITOR
        if (HasAnyDebugBonus())
            Debug.Log($"   🔧 [DEBUG 오버라이드 활성] ATK+{_debugBonus.atkFlat:F1}(x{1f+_debugBonus.atkPercent:F2})" +
                      $" HP+{_debugBonus.maxHp:F0} DEF+{_debugBonus.defense:F1} — 저장 안됨");
#endif
        Debug.Log($"   📈 성장 스탯:");
        Debug.Log($"      ⚔️ 공격력: {finalAttackDamage:F1} (변경: {FormatDelta(finalAttackDamage - previousAttackDamage, "F1")})");
        Debug.Log($"      ❤️ 최대체력: {finalMaxHealth:F0} (변경: {FormatDelta(finalMaxHealth - previousMaxHealth, "F0")})");
        Debug.Log($"      🛡️ 방어력: {finalDefense:F1} (변경: {FormatDelta(finalDefense - previousDefense, "F1")})");
        Debug.Log($"   🎯 고정 스탯:");
        Debug.Log($"      🎯 크리티컬: {finalCriticalChance:P1} (x{finalCriticalDamage:F1})");
        Debug.Log($"      ⚡ 공격속도: {finalAttackSpeed:F2}");
        Debug.Log($"      🏃 이동속도: {finalMoveSpeed:F1} (변경: {FormatDelta(finalMoveSpeed - previousMoveSpeed, "F1")})");
        Debug.Log($"      💚 회복 효율: {finalHealMultiplier:P0}");
        Debug.Log($"========================================");
    }
    
    private static string FormatDelta(float delta, string fmt)
    {
        if (Mathf.Approximately(delta, 0f)) return "±0";
        return delta > 0
            ? $"+{delta.ToString(fmt)}"
            : delta.ToString(fmt);
    }

    /// <summary>
    /// 🔄 외부에서 스탯 재계산 요청
    /// </summary>
    public void RefreshStats()
    {
        RecalculateAllStats();
    }
    
    /// <summary>
    /// 🎮 특정 스탯 조회 (디버그용)
    /// </summary>
    public float GetStat(string statName)
    {
        return statName.ToLower() switch
        {
            "attack" or "damage" => finalAttackDamage,
            "speed" or "movespeed" => finalMoveSpeed,
            "health" or "hp" => finalMaxHealth,
            "attackspeed" => finalAttackSpeed,
            "critical" or "crit" => finalCriticalChance,
            "defense" or "def" => finalDefense,
            _ => 0f
        };
    }

    /// <summary>
    /// 🔗 다른 컴포넌트들과 자동 동기화
    /// </summary>
    private void SyncWithOtherComponents()
    {
        // PlayerController 동기화 (이동속도)
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.SyncWithRuntimeStats();
        }
        
        // PlayerHealth 동기화 (최대 체력)
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SyncWithRuntimeStats();
        }
        
        // PlayerAnimationController 동기화 (공격속도 — ASPD 배율 재계산)
        var animController = FindObjectOfType<PlayerAnimationController>();
        if (animController != null)
        {
            animController.SyncWithRuntimeStats();
        }
        
        if (showDebugLogs)
            Debug.Log($"🔗 [PlayerRuntimeStats] 다른 컴포넌트들과 동기화 완료");
    }

    /// <summary>
    /// 🔧 디버그용: 이벤트 구독자 수 확인
    /// </summary>
    [ContextMenu("Print Event Subscribers")]
    private void PrintEventSubscribers()
    {
        Debug.Log($"🔍 [PlayerRuntimeStats] 이벤트 구독자 현황:");
        Debug.Log($"   - OnStatsRecalculated: {OnStatsRecalculated?.GetInvocationList().Length ?? 0}개");
        Debug.Log($"   - OnAttackDamageChanged: {OnAttackDamageChanged?.GetInvocationList().Length ?? 0}개");
        Debug.Log($"   - OnMoveSpeedChanged: {OnMoveSpeedChanged?.GetInvocationList().Length ?? 0}개");
        Debug.Log($"   - OnMaxHealthChanged: {OnMaxHealthChanged?.GetInvocationList().Length ?? 0}개");
        Debug.Log($"   - OnCriticalChanceChanged: {OnCriticalChanceChanged?.GetInvocationList().Length ?? 0}개");
        Debug.Log($"   - OnDefenseChanged: {OnDefenseChanged?.GetInvocationList().Length ?? 0}개");
    }

    /// <summary>
    /// 🔥 버프/디버프 추가
    /// </summary>
    public void AddBuff(IBuffEffect buff)
    {
        if (buff == null) return;
        
        // 동일한 효과가 이미 있는지 확인
        var existing = activeBuffs.Find(b => b.EffectID == buff.EffectID);
        if (existing != null)
        {
            // 중첩 처리
            existing.OnStack(buff);
            existing.Remove(this);  // 기존 효과 제거
            existing.Apply(this);   // 새로운 중첩 효과 적용
        }
        else
        {
            // 새 효과 추가
            var clonedBuff = buff.Clone();
            clonedBuff.Initialize();
            activeBuffs.Add(clonedBuff);
            clonedBuff.Apply(this);
        }
        
        // 스탯 재계산
        RecalculateAllStats();
        
        if (showDebugLogs)
            Debug.Log($"🔥 [PlayerRuntimeStats] 버프 추가: {buff.EffectName}");
    }
    
    /// <summary>
    /// 🗑️ 버프/디버프 제거
    /// </summary>
    public void RemoveBuff(string effectID)
    {
        var buff = activeBuffs.Find(b => b.EffectID == effectID);
        if (buff != null)
        {
            buff.Remove(this);
            activeBuffs.Remove(buff);
            RecalculateAllStats();
            
            if (showDebugLogs)
                Debug.Log($"🗑️ [PlayerRuntimeStats] 버프 제거: {buff.EffectName}");
        }
    }
    
    /// <summary>
    /// ⏰ 버프/디버프 시간 업데이트 (매 프레임)
    /// </summary>
    private void Update()
    {
        if (activeBuffs.Count == 0) return;
        
        // 만료된 버프들 찾기
        var expiredBuffs = new List<IBuffEffect>();
        
        foreach (var buff in activeBuffs)
        {
            if (buff.Update(Time.deltaTime))
            {
                expiredBuffs.Add(buff);
            }
        }
        
        // 만료된 버프들 제거
        foreach (var expiredBuff in expiredBuffs)
        {
            expiredBuff.Remove(this);
            activeBuffs.Remove(expiredBuff);
            
            if (showDebugLogs)
                Debug.Log($"⏰ [PlayerRuntimeStats] 버프 만료: {expiredBuff.EffectName}");
        }
        
        // 만료된 버프가 있으면 스탯 재계산
        if (expiredBuffs.Count > 0)
        {
            RecalculateAllStats();
        }
    }
    
    #region 임시 스탯 변경 메서드들
    
    public void AddTemporaryAttackDamage(float amount) => temporaryAttackDamage += amount;
    public void RemoveTemporaryAttackDamage(float amount) => temporaryAttackDamage -= amount;
    public void AddTemporaryMoveSpeed(float amount) => temporaryMoveSpeed += amount;
    public void RemoveTemporaryMoveSpeed(float amount) => temporaryMoveSpeed -= amount;
    public void AddTemporaryMaxHealth(float amount) => temporaryMaxHealth += amount;
    public void RemoveTemporaryMaxHealth(float amount) => temporaryMaxHealth -= amount;
    public void AddTemporaryDefense(float amount) => temporaryDefense += amount;
    public void RemoveTemporaryDefense(float amount) => temporaryDefense -= amount;
    
    #endregion

    #region 🧪 테스트 및 검증 메서드들
    
    /// <summary>
    /// 🧪 무기교체 테스트 (디버그용)
    /// </summary>
    [ContextMenu("Test Weapon Change")]
    private void TestWeaponChange()
    {
        Debug.Log("🧪 [테스트] 무기교체 시뮬레이션 시작");
        
        // 현재 스탯 기록
        float beforeAttack = finalAttackDamage;
        
        // 스탯 재계산 강제 실행
        RecalculateAllStats();
        
        // 변화 확인
        Debug.Log($"🧪 [테스트] 무기교체 결과: 공격력 {beforeAttack:F1} → {finalAttackDamage:F1}");
    }
    
    /// <summary>
    /// 🧪 버프 효과 테스트 (디버그용)
    /// </summary>
    [ContextMenu("Test Buff Effects")]
    private void TestBuffEffects()
    {
        Debug.Log("🧪 [테스트] 버프 효과 시뮬레이션 시작");
        
        // 공격력 버프 추가
        var attackBuff = new AttackPowerBuff(15f, 5f);
        AddBuff(attackBuff);
        
        Debug.Log($"🧪 [테스트] 버프 적용 후 공격력: {finalAttackDamage:F1}");
        
        // 5초 후 자동 제거 확인용 (실제로는 Update에서 처리)
        StartCoroutine(TestBuffRemovalCoroutine(attackBuff.EffectID));
    }
    
    private System.Collections.IEnumerator TestBuffRemovalCoroutine(string effectID)
    {
        yield return new WaitForSeconds(5.1f);
        Debug.Log($"🧪 [테스트] 5초 후 버프 상태 확인 - 활성 버프 수: {activeBuffs.Count}");
        Debug.Log($"🧪 [테스트] 버프 만료 후 공격력: {finalAttackDamage:F1}");
    }
    
    /// <summary>
    /// 🧪 UI 동기화 테스트 (디버그용)
    /// </summary>
    [ContextMenu("Test UI Sync")]
    private void TestUISync()
    {
        Debug.Log("🧪 [테스트] UI 동기화 시뮬레이션 시작");
        
        // 이벤트 구독자 수 확인
        int subscribers = OnStatsRecalculated?.GetInvocationList().Length ?? 0;
        Debug.Log($"🧪 [테스트] OnStatsRecalculated 구독자 수: {subscribers}");
        
        // 강제로 스탯 변경 후 이벤트 발생
        float oldAttack = finalAttackDamage;
        finalAttackDamage += 100f; // 임시로 100 증가
        
        OnStatsRecalculated?.Invoke();
        
        // 원복
        finalAttackDamage = oldAttack;
        OnStatsRecalculated?.Invoke();
        
        Debug.Log($"🧪 [테스트] UI 동기화 테스트 완료");
    }
    
    /// <summary>
    /// 🧪 종합 시스템 검증
    /// </summary>
    [ContextMenu("Comprehensive System Test")]
    private void ComprehensiveSystemTest()
    {
        Debug.Log("🧪 [종합 테스트] 전체 시스템 검증 시작");
        
        // 1. 기본 스탯 확인
        Debug.Log($"📊 [종합 테스트] 현재 기본 스탯:");
        Debug.Log($"   공격력: {finalAttackDamage:F1}, 이동속도: {finalMoveSpeed:F1}, 체력: {finalMaxHealth:F0}");
        
        // 2. 데이터 연결 확인
        bool dataOK = playerData != null;
        bool classOK = currentClass != null;
        bool managerOK = dataManager != null;
        
        Debug.Log($"🔗 [종합 테스트] 데이터 연결 상태:");
        Debug.Log($"   PlayerData: {(dataOK ? "✅" : "❌")}, IPlayerClass: {(classOK ? "✅" : "❌")}, DataManager: {(managerOK ? "✅" : "❌")}");
        
        // 3. 이벤트 시스템 확인
        int eventSubscribers = OnStatsRecalculated?.GetInvocationList().Length ?? 0;
        Debug.Log($"📡 [종합 테스트] 이벤트 구독자: {eventSubscribers}개");
        
        // 4. 버프 시스템 확인
        Debug.Log($"🔥 [종합 테스트] 활성 버프: {activeBuffs.Count}개");
        
        // 5. 컴포넌트 동기화 확인
        var playerController = FindObjectOfType<PlayerController>();
        var playerHealth = FindObjectOfType<PlayerHealth>();
        
        Debug.Log($"🔗 [종합 테스트] 컴포넌트 연결:");
        Debug.Log($"   PlayerController: {(playerController != null ? "✅" : "❌")}, PlayerHealth: {(playerHealth != null ? "✅" : "❌")}");
        
        Debug.Log("🧪 [종합 테스트] 전체 시스템 검증 완료");
    }
    
    /// <summary>
    /// 🧪 성능 테스트 (많은 버프 적용/해제)
    /// </summary>
    [ContextMenu("Performance Test")]
    private void PerformanceTest()
    {
        Debug.Log("🧪 [성능 테스트] 다량 버프 적용/해제 시작");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // 100개의 버프 추가
        for (int i = 0; i < 100; i++)
        {
            var buff = new AttackPowerBuff(1f, 0.1f); // 짧은 지속시간
            AddBuff(buff);
        }
        
        stopwatch.Stop();
        Debug.Log($"🧪 [성능 테스트] 100개 버프 추가: {stopwatch.ElapsedMilliseconds}ms");
        Debug.Log($"🧪 [성능 테스트] 현재 활성 버프: {activeBuffs.Count}개");
        Debug.Log($"🧪 [성능 테스트] 최종 공격력: {finalAttackDamage:F1}");
    }
    
    #endregion

    /// <summary>
    /// 🔗 클래스별 기본값 가져오기 헬퍼 메서드들
    /// </summary>
    
    /// <summary>
    /// ⚔️ 레벨별 캐릭터 기본 공격력 (목표 역산 방식)
    /// BaseClassData에서 자동 계산된 성장률 사용
    /// </summary>
    private float GetCharacterBaseAttack(int level)
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            // BaseClassData에서 찾기 (Assasin, Warrior, Wizard 등)
            BaseClassData classData = GetClassDataFromBehaviour(baseClass);
            
            if (classData != null)
            {
                // BaseClassData.GetCharacterBaseAttack()가 자동으로 목표 역산 계산
                return classData.GetCharacterBaseAttack(level);
            }
            
            // Fallback: BaseClassData가 없으면 기본 공격력만 사용
            Debug.LogWarning($"[PlayerRuntimeStats] {playerClass.ClassName}의 BaseClassData를 찾을 수 없습니다. 기본값 사용.");
            return baseClass.GetBaseAttackDamage();
        }
        return 10f; // Fallback
    }
    
    /// <summary>
    /// BaseClassBehaviour에서 사용 중인 ScriptableObject 찾기
    /// </summary>
    private BaseClassData GetClassDataFromBehaviour(BaseClassBehaviour baseClass)
    {
        // Assasin인 경우
        if (baseClass is Assasin assasin)
        {
            // Reflection으로 assasinData 필드 접근
            var field = typeof(Assasin).GetField("assasinData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(assasin) as BaseClassData;
            }
        }
        
        // Warrior인 경우
        if (baseClass is Warrior warrior)
        {
            var field = typeof(Warrior).GetField("warriorData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return field.GetValue(warrior) as BaseClassData;
            }
        }
        
        // TODO: Wizard 추가 시 여기에 추가
        
        return null;
    }
    
    private float GetBaseAttackDamageFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            return baseClass.GetBaseAttackDamage();
        }
        return 10f; // 기본값
    }
    
    private float GetBaseDefenseFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            return baseClass.GetBaseDefense();
        }
        return 0f; // 기본값
    }
    
    private float GetBaseMaxHealthFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            return baseClass.GetBaseMaxHealth();
        }
        return 200f; // 기본값
    }
    
    private float GetBaseMoveSpeedFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            return baseClass.GetBaseMoveSpeed();
        }
        return 4f; // 기본값
    }
    
    /// <summary>
    /// 🆕 레벨당 체력 증가량 가져오기
    /// </summary>
    private float GetHpGainPerLevelFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            BaseClassData classData = GetClassDataFromBehaviour(baseClass);
            if (classData != null)
            {
                return classData.hpGainPerLevel;
            }
        }
        return 20f; // 기본값
    }
    
    /// <summary>
    /// 🆕 레벨당 방어력 증가량 가져오기 (CSV 조정 가능)
    /// </summary>
    private float GetDefenseGainPerLevelFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            BaseClassData classData = GetClassDataFromBehaviour(baseClass);
            if (classData != null)
            {
                return classData.defenseGainPerLevel;
            }
        }
        return 1f; // 기본값
    }
    
    /// <summary>
    /// 🆕 기본 크리티컬 확률 가져오기 (CSV 조정 가능)
    /// </summary>
    private float GetBaseCritRateFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            BaseClassData classData = GetClassDataFromBehaviour(baseClass);
            if (classData != null)
            {
                return classData.baseCritRate;
            }
        }
        return 0.05f; // 기본값 5%
    }
    
    /// <summary>
    /// 🆕 기본 크리티컬 데미지 가져오기 (CSV 조정 가능)
    /// </summary>
    private float GetBaseCritDamageFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            BaseClassData classData = GetClassDataFromBehaviour(baseClass);
            if (classData != null)
            {
                return classData.baseCritDamage;
            }
        }
        return 1.5f; // 기본값 150%
    }
    
    /// <summary>
    /// 🆕 기본 공격속도 가져오기 (CSV 조정 가능)
    /// </summary>
    private float GetBaseAttackSpeedFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            BaseClassData classData = GetClassDataFromBehaviour(baseClass);
            if (classData != null)
            {
                return classData.baseAttackSpeed;
            }
        }
        return 1.0f; // 기본값
    }
    
    /// <summary>
    /// 🆕 회복 효율 배율 가져오기 (CSV 조정 가능)
    /// </summary>
    private float GetHealMultiplierFromClass()
    {
        var playerClass = GetComponent<IPlayerClass>();
        if (playerClass is BaseClassBehaviour baseClass)
        {
            BaseClassData classData = GetClassDataFromBehaviour(baseClass);
            if (classData != null)
            {
                return classData.healMultiplier;
            }
        }
        return 1.0f; // 기본값 100%
    }
    
    #region 🆕 Phase 3: StatModifier 시스템
    
    /// <summary>
    /// StatModifier 추가
    /// </summary>
    public void AddStatModifier(StatModifier modifier)
    {
        if (modifier == null) return;
        
        activeModifiers.Add(modifier);
        isModifierCacheDirty = true;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerRuntimeStats] StatModifier 추가: {modifier}");
    }
    
    /// <summary>
    /// StatModifier 제거
    /// </summary>
    public void RemoveStatModifier(StatModifier modifier)
    {
        if (modifier == null) return;
        
        // statType과 source가 일치하는 것 제거
        activeModifiers.RemoveAll(m => 
            m.statType == modifier.statType && 
            m.source == modifier.source
        );
        
        isModifierCacheDirty = true;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerRuntimeStats] StatModifier 제거: {modifier}");
    }
    
    /// <summary>
    /// 모든 StatModifier 제거
    /// </summary>
    public void ClearAllStatModifiers()
    {
        activeModifiers.Clear();
        modifierCache.Clear();
        isModifierCacheDirty = true;
    }
    
    /// <summary>
    /// StatModifier 적용 (ApplyPhase 순서대로 정렬)
    /// </summary>
    private void ApplyStatModifiers()
    {
        if (activeModifiers.Count == 0) return;
        
        // 캐시 갱신 필요한 경우
        if (isModifierCacheDirty)
        {
            RebuildModifierCache();
        }
        
        // 캐시된 값 적용
        ApplyCachedModifiers();
    }
    
    /// <summary>
    /// Modifier 캐시 재구축 (ApplyPhase 순서대로 정렬 + 합산)
    /// </summary>
    private void RebuildModifierCache()
    {
        modifierCache.Clear();
        
        // 🔑 핵심: ApplyPhase 순서대로 정렬
        var sortedModifiers = activeModifiers
            .OrderBy(m => m.applyPhase)
            .ThenBy(m => m.statType)
            .ToList();
        
        // statType별로 그룹화
        var groupedModifiers = sortedModifiers
            .GroupBy(m => m.statType)
            .ToList();
        
        foreach (var group in groupedModifiers)
        {
            EStatType statType = group.Key;
            var mods = group.ToList();
            
            // StackRule에 따라 계산
            float finalValue = CalculateStackedValue(mods);
            
            modifierCache[statType] = finalValue;
        }
        
        isModifierCacheDirty = false;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerRuntimeStats] Modifier 캐시 재구축: {modifierCache.Count}개 스탯");
    }
    
    /// <summary>
    /// StackRule에 따른 값 계산
    /// </summary>
    private float CalculateStackedValue(List<StatModifier> mods)
    {
        if (mods.Count == 0) return 0f;
        
        StatStackRule stackRule = mods[0].stackRule;
        
        switch (stackRule)
        {
            case StatStackRule.Add:
                // 단순 합산
                return mods.Sum(m => m.value);
                
            case StatStackRule.AddThenMultiply:
                // 합산 후 곱셈 형태로 변환 (1 + sum)
                float sum = mods.Sum(m => m.value);
                return 1f + sum;
                
            case StatStackRule.AddThenCap:
                // 합산 후 상한 적용
                float total = mods.Sum(m => m.value);
                float cap = mods[0].capValue ?? float.MaxValue;
                return Mathf.Min(total, cap);
                
            default:
                return mods.Sum(m => m.value);
        }
    }
    
    /// <summary>
    /// 캐시된 Modifier 값 적용
    /// </summary>
    private void ApplyCachedModifiers()
    {
        foreach (var kvp in modifierCache)
        {
            EStatType statType = kvp.Key;
            float value = kvp.Value;
            
            ApplyModifierToStat(statType, value);
        }
    }
    
    /// <summary>
    /// 특정 스탯에 Modifier 적용
    /// </summary>
    private void ApplyModifierToStat(EStatType statType, float value)
    {
        switch (statType)
        {
            case EStatType.ATK_FLAT:
                finalAttackDamage += value;
                break;
                
            case EStatType.ATK_PERCENT:
                finalAttackDamage *= value;
                break;
                
            case EStatType.ASPD:
                finalAttackSpeed *= value;
                break;
                
            case EStatType.CRIT_RATE:
                finalCriticalChance += value;
                if (showDebugLogs)
                    Debug.Log($"🎯 [PlayerRuntimeStats] CRIT_RATE 적용: +{value:P2} → 총 {finalCriticalChance:P2}");
                break;
                
            case EStatType.CRIT_DMG:
                finalCriticalDamage += value;
                break;
                
            case EStatType.DEF_FLAT:
                finalDefense += value;
                break;
                
            case EStatType.HP_FLAT:
                finalMaxHealth += value;
                break;
                
            case EStatType.MOVE_SPEED:
                moveSpeedPercentBonus += value;
                if (showDebugLogs)
                    Debug.Log($"🏃 [PlayerRuntimeStats] MOVE_SPEED(Modifier): +{value:P0} 누적 → 보너스 합계 {moveSpeedPercentBonus:P0}");
                break;
                
            case EStatType.PIERCE_DAMAGE_RETENTION:
                finalPierceDamageRetention += value;
                break;
            
            default:
                if (showDebugLogs)
                    Debug.LogWarning($"[PlayerRuntimeStats] 지원하지 않는 스탯 타입: {statType}");
                break;
        }
    }
    
    #endregion
    
    #region 🆕 Phase 1: 패시브 스킬 스탯 시스템 (스킬 시스템 개편)
    
    /// <summary>
    /// 패시브 스킬의 스탯 보너스 추가
    /// </summary>
    public void AddPassiveStatBonus(string skillID, EStatType statType, float value, StatModifierType modifierType)
    {
        if (!passiveSkillBonuses.ContainsKey(skillID))
        {
            passiveSkillBonuses[skillID] = new List<PassiveStatBonus>();
        }
        
        passiveSkillBonuses[skillID].Add(new PassiveStatBonus
        {
            statType = statType,
            value = value,
            modifierType = modifierType
        });
        
        if (showDebugLogs)
            Debug.Log($"📊 [PlayerRuntimeStats] 패시브 스탯 보너스 추가: {skillID} - {statType} +{value} ({modifierType})");
    }
    
    /// <summary>
    /// 특정 스킬의 패시브 보너스 제거
    /// </summary>
    public void RemovePassiveStatBonus(string skillID)
    {
        if (passiveSkillBonuses.ContainsKey(skillID))
        {
            int count = passiveSkillBonuses[skillID].Count;
            passiveSkillBonuses.Remove(skillID);
            
            if (showDebugLogs)
                Debug.Log($"🗑️ [PlayerRuntimeStats] 패시브 스탯 보너스 제거: {skillID} ({count}개)");
        }
    }
    
    /// <summary>
    /// 특정 스탯의 패시브 보너스 합계 계산
    /// </summary>
    private float GetPassiveBonusForStat(EStatType statType, StatModifierType modifierType)
    {
        float total = 0f;
        
        foreach (var bonusList in passiveSkillBonuses.Values)
        {
            foreach (var bonus in bonusList)
            {
                if (bonus.statType == statType && bonus.modifierType == modifierType)
                {
                    total += bonus.value;
                }
            }
        }
        
        return total;
    }
    
    /// <summary>
    /// 모든 패시브 스킬 스탯 적용 (ApplyEquipmentStats 이후에 호출)
    /// 기존 EStatType 사용: ATK_FLAT, ATK_PERCENT, CRIT_RATE 등
    /// </summary>
    private void ApplyPassiveSkillStats()
    {
        if (passiveSkillBonuses.Count == 0) return;
        
        if (showDebugLogs)
            Debug.Log($"🛡️ [PlayerRuntimeStats] 패시브 스킬 스탯 적용: {passiveSkillBonuses.Count}개 패시브");
        
        // 공격력 (가산) - ATK_FLAT
        float atkFlat = GetPassiveBonusForStat(EStatType.ATK_FLAT, StatModifierType.Additive);
        finalAttackDamage += atkFlat;
        if (showDebugLogs && atkFlat > 0)
            Debug.Log($"  ⚔️ ATK_FLAT: +{atkFlat:F2} → {finalAttackDamage:F2}");
        
        // 공격력 (배수) - ATK_PERCENT (CSV: 소수 형태, 0.05 = 5%)
        float atkPercent = GetPassiveBonusForStat(EStatType.ATK_PERCENT, StatModifierType.Multiplicative);
        if (atkPercent > 0)
        {
            finalAttackDamage *= (1f + atkPercent);
            if (showDebugLogs)
                Debug.Log($"  ⚔️ ATK_PERCENT: x{1f + atkPercent:F2} → {finalAttackDamage:F2}");
        }
        
        // 방어력 (가산) - DEF_FLAT
        float defFlat = GetPassiveBonusForStat(EStatType.DEF_FLAT, StatModifierType.Additive);
        finalDefense += defFlat;
        if (showDebugLogs && defFlat > 0)
            Debug.Log($"  🛡️ DEF_FLAT: +{defFlat:F2} → {finalDefense:F2}");
        
        // 최대 체력 (가산) - HP_FLAT
        float hpFlat = GetPassiveBonusForStat(EStatType.HP_FLAT, StatModifierType.Additive);
        finalMaxHealth += hpFlat;
        if (showDebugLogs && hpFlat > 0)
            Debug.Log($"  ❤️ HP_FLAT: +{hpFlat:F0} → {finalMaxHealth:F0}");
        
        // 이동속도 (퍼센트) - MOVE_SPEED (CSV: 소수 형태, 0.05 = 5%)
        // 단리 합산을 위해 moveSpeedPercentBonus에 누적 (ApplyClassMultipliers에서 일괄 적용)
        float moveSpeedBonus = GetPassiveBonusForStat(EStatType.MOVE_SPEED, StatModifierType.Multiplicative);
        if (moveSpeedBonus > 0)
        {
            moveSpeedPercentBonus += moveSpeedBonus;
            if (showDebugLogs)
                Debug.Log($"  🏃 MOVE_SPEED(패시브): +{moveSpeedBonus:P0} 누적 → 보너스 합계 {moveSpeedPercentBonus:P0}");
        }
        
        // 크리티컬 확률 (가산) - CRIT_RATE
        float critRate = GetPassiveBonusForStat(EStatType.CRIT_RATE, StatModifierType.Additive);
        finalCriticalChance += critRate; // 이미 소수 형태 (0.05 = 5%)
        if (showDebugLogs && critRate > 0)
            Debug.Log($"  🎯 CRIT_RATE: +{critRate:P2} → {finalCriticalChance:P2}");
        
        // 크리티컬 데미지 (가산) - CRIT_DMG
        float critDmg = GetPassiveBonusForStat(EStatType.CRIT_DMG, StatModifierType.Additive);
        finalCriticalDamage += critDmg;
        if (showDebugLogs && critDmg > 0)
            Debug.Log($"  💥 CRIT_DMG: +{critDmg:F2} → {finalCriticalDamage:F2}");
        
        // 공격속도 (배수) - ASPD (CSV: 소수 형태, 0.05 = 5%)
        float atkSpeed = GetPassiveBonusForStat(EStatType.ASPD, StatModifierType.Multiplicative);
        if (atkSpeed > 0)
        {
            finalAttackSpeed *= (1f + atkSpeed);
            if (showDebugLogs)
                Debug.Log($"  ⚡ ASPD: x{1f + atkSpeed:F2} → {finalAttackSpeed:F2}");
        }
        
        // 📊 특수 스탯 10종 패시브 보너스 (Additive)
        float skillDmgBonus = GetPassiveBonusForStat(EStatType.SKILL_DMG_PERCENT, StatModifierType.Additive);
        if (skillDmgBonus > 0)
        {
            finalSkillDamageBonus += skillDmgBonus;
            if (showDebugLogs) Debug.Log($"  🎯 SKILL_DMG_PERCENT: +{skillDmgBonus:P2} → {finalSkillDamageBonus:P2}");
        }
        
        float cdrBonus = GetPassiveBonusForStat(EStatType.COOLDOWN_REDUCTION, StatModifierType.Additive);
        if (cdrBonus > 0)
        {
            finalCooldownReduction += cdrBonus;
            if (showDebugLogs) Debug.Log($"  ⏱️ COOLDOWN_REDUCTION: +{cdrBonus:P2} → {finalCooldownReduction:P2}");
        }
        
        float dmgReducBonus = GetPassiveBonusForStat(EStatType.DAMAGE_REDUCTION_PERCENT, StatModifierType.Additive);
        if (dmgReducBonus > 0)
        {
            finalDamageReduction += dmgReducBonus;
            if (showDebugLogs) Debug.Log($"  🛡️ DAMAGE_REDUCTION_PERCENT: +{dmgReducBonus:P2} → {finalDamageReduction:P2}");
        }
        
        float hpRegenBonus = GetPassiveBonusForStat(EStatType.HP_REGEN, StatModifierType.Additive);
        if (hpRegenBonus > 0)
        {
            finalHpRegen += hpRegenBonus;
            if (showDebugLogs) Debug.Log($"  💚 HP_REGEN: +{hpRegenBonus:F2} → {finalHpRegen:F2}");
        }
        
        float lifeStealBonus = GetPassiveBonusForStat(EStatType.LIFESTEAL, StatModifierType.Additive);
        if (lifeStealBonus > 0)
        {
            finalLifeSteal += lifeStealBonus;
            if (showDebugLogs) Debug.Log($"  🩸 LIFESTEAL: +{lifeStealBonus:P2} → {finalLifeSteal:P2}");
        }
        
        float armorPenBonus = GetPassiveBonusForStat(EStatType.ARMOR_PENETRATION, StatModifierType.Additive);
        if (armorPenBonus > 0)
        {
            finalArmorPenetration += armorPenBonus;
            if (showDebugLogs) Debug.Log($"  🔓 ARMOR_PENETRATION: +{armorPenBonus:P2} → {finalArmorPenetration:P2}");
        }
        
        float dodgeBonus = GetPassiveBonusForStat(EStatType.DODGE_CHANCE, StatModifierType.Additive);
        if (dodgeBonus > 0)
        {
            finalDodgeChance += dodgeBonus;
            if (showDebugLogs) Debug.Log($"  💨 DODGE_CHANCE: +{dodgeBonus:P2} → {finalDodgeChance:P2}");
        }
        
        float blockBonus = GetPassiveBonusForStat(EStatType.BLOCK_CHANCE, StatModifierType.Additive);
        if (blockBonus > 0)
        {
            finalBlockChance += blockBonus;
            if (showDebugLogs) Debug.Log($"  🛑 BLOCK_CHANCE: +{blockBonus:P2} → {finalBlockChance:P2}");
        }
        
        float expGainBonus = GetPassiveBonusForStat(EStatType.EXP_GAIN_PERCENT, StatModifierType.Additive);
        if (expGainBonus > 0)
        {
            finalExpGainBonus += expGainBonus;
            if (showDebugLogs) Debug.Log($"  ⭐ EXP_GAIN_PERCENT: +{expGainBonus:P2} → {finalExpGainBonus:P2}");
        }
        
        float statusResistBonus = GetPassiveBonusForStat(EStatType.STATUS_RESIST_ALL, StatModifierType.Additive);
        if (statusResistBonus > 0)
        {
            finalStatusResist += statusResistBonus;
            if (showDebugLogs) Debug.Log($"  🔮 STATUS_RESIST_ALL: +{statusResistBonus:P2} → {finalStatusResist:P2}");
        }
        
        float pierceRetentionBonus = GetPassiveBonusForStat(EStatType.PIERCE_DAMAGE_RETENTION, StatModifierType.Additive);
        if (pierceRetentionBonus > 0)
        {
            finalPierceDamageRetention += pierceRetentionBonus;
            if (showDebugLogs) Debug.Log($"  🏹 PIERCE_DAMAGE_RETENTION: +{pierceRetentionBonus:P2} → {finalPierceDamageRetention:P2}");
        }
    }
    
    /// <summary>
    /// 패시브 스탯 보너스 데이터 구조
    /// </summary>
    [System.Serializable]
    private class PassiveStatBonus
    {
        public EStatType statType;
        public float value;
        public StatModifierType modifierType;
    }
    
    #endregion
    
    #region 🆕 Phase 4-C: ConditionalModifier 시스템 (룬 전용)
    
    /// <summary>
    /// 룬에서 활성화된 조건부 모디파이어 목록 가져오기
    /// ⚙️ CombatFormula에서 조건 판정 시 사용
    /// </summary>
    public List<ConditionalModifier> GetActiveConditionalModifiers()
    {
        return activeConditionalModifiers;
    }
    
    /// <summary>
    /// 조건부 모디파이어 설정 (룬 장착/해제 시 호출)
    /// ⚙️ RuneManager에서 호출
    /// </summary>
    /// <param name="modifiers">활성화할 조건부 모디파이어 목록</param>
    public void SetConditionalModifiers(List<ConditionalModifier> modifiers)
    {
        if (modifiers == null)
        {
            activeConditionalModifiers.Clear();
        }
        else
        {
            activeConditionalModifiers = new List<ConditionalModifier>(modifiers);
        }
        
        if (showDebugLogs)
            Debug.Log($"[PlayerRuntimeStats] 조건부 모디파이어 설정: {activeConditionalModifiers.Count}개");
    }
    
    /// <summary>
    /// 조건부 모디파이어 추가 (개별)
    /// </summary>
    public void AddConditionalModifier(ConditionalModifier modifier)
    {
        if (modifier == null)
        {
            Debug.LogWarning("[PlayerRuntimeStats] 추가할 조건부 모디파이어가 null입니다.");
            return;
        }
        
        activeConditionalModifiers.Add(modifier);
        
        if (showDebugLogs)
            Debug.Log($"[PlayerRuntimeStats] 조건부 모디파이어 추가: {modifier.displayName}");
    }
    
    /// <summary>
    /// 조건부 모디파이어 제거 (개별)
    /// </summary>
    public void RemoveConditionalModifier(ConditionalModifier modifier)
    {
        if (modifier == null) return;
        
        int removed = activeConditionalModifiers.RemoveAll(m => 
            m.modifierId == modifier.modifierId && 
            m.source == modifier.source
        );
        
        if (showDebugLogs && removed > 0)
            Debug.Log($"[PlayerRuntimeStats] 조건부 모디파이어 제거: {modifier.displayName} ({removed}개)");
    }
    
    /// <summary>
    /// 모든 조건부 모디파이어 제거
    /// </summary>
    public void ClearAllConditionalModifiers()
    {
        activeConditionalModifiers.Clear();
        
        if (showDebugLogs)
            Debug.Log("[PlayerRuntimeStats] 모든 조건부 모디파이어 제거");
    }
    
    #endregion
}
