using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 🎯 PlayerRuntimeStats - 런타임 전용 최종 스탯 계산/관리 클래스
/// 책임: SelectedPlayerData + 장비 + 클래스배율을 종합하여 최종 스탯 제공
/// 위치: 플레이어 캐릭터 프리팹에 붙여서 개별 관리
/// </summary>
public class PlayerRuntimeStats : MonoBehaviour
{
    [Header("📊 최종 계산된 스탯 (읽기 전용)")]
    [SerializeField] private float finalAttackDamage = 10f;
    [SerializeField] private float finalMoveSpeed = 4f;
    [SerializeField] private float finalMaxHealth = 200f;
    [SerializeField] private float finalAttackSpeed = 1f;
    [SerializeField] private float finalCriticalChance = 0f;
    [SerializeField] private float finalCriticalDamage = 1.5f;
    [SerializeField] private float finalDefense = 0f;
    
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
    
    private void Start()
    {
        InitializeReferences();
        RecalculateAllStats();
        
        // 🆕 PlayerDataManager 이벤트 연결
        if (dataManager != null)
        {
            dataManager.OnItemEquipped += OnItemEquipped;
            dataManager.OnItemUnequipped += OnItemUnequipped;
            dataManager.OnLevelChanged += OnLevelChanged;
        }
        
        if (showDebugLogs)
            Debug.Log($"🎯 [PlayerRuntimeStats] 초기화 완료 - 공격력: {finalAttackDamage}, 이동속도: {finalMoveSpeed}");
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
        // PlayerDataManager 참조
        dataManager = PlayerDataManager.Instance;
        if (dataManager != null)
        {
            playerData = dataManager.selectedPlayerData;
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
            Debug.Log($"🔗 [PlayerRuntimeStats] 참조 초기화:");
            Debug.Log($"   - PlayerData: {(playerData != null ? "연결됨" : "없음")}");
            Debug.Log($"   - CurrentClass: {(currentClass != null ? currentClass.ClassName : "없음")}");
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
        
        // 2단계: 장비 스탯 추가
        ApplyEquipmentStats();
        
        // 3단계: 클래스 배율 적용
        ApplyClassMultipliers();
        
        // 4단계: 추가 효과 적용 (버프/디버프)
        ApplyBuffEffects();
        
        // 5단계: 최종 검증 및 제한
        ValidateStats();
        
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
    /// </summary>
    private void CalculateBaseStats()
    {
        int currentLevel = playerData.currentLevel;
        
        // 🆕 ScriptableObject에서 기본값 가져오기
        float baseAttack = GetBaseAttackDamageFromClass();
        float baseDefense = GetBaseDefenseFromClass();
        float baseHealth = GetBaseMaxHealthFromClass();
        float baseMoveSpeed = GetBaseMoveSpeedFromClass();
        
        // 기본 공식 (ScriptableObject 기본값 + 레벨당 증가)
        finalAttackDamage = baseAttack + (currentLevel - 1) * 2f;        // 클래스 기본값 + 레벨당 +2 공격력
        finalMoveSpeed = baseMoveSpeed;                                   // 클래스 기본값 (클래스에서 조정)
        finalMaxHealth = baseHealth + (currentLevel - 1) * 20f;          // 클래스 기본값 + 레벨당 +20 체력
        finalAttackSpeed = 1f;                                           // 기본 공격속도
        finalCriticalChance = 0f;                                        // 기본 크리티컬 확률
        finalCriticalDamage = 1.5f;                                      // 기본 크리티컬 데미지
        finalDefense = baseDefense + (currentLevel - 1) * 1f;            // 클래스 기본값 + 레벨당 +1 방어력
        
        if (showDebugLogs)
            Debug.Log($"📊 [PlayerRuntimeStats] 기본 스탯 (Lv.{currentLevel}) - 공격력: {finalAttackDamage}, 체력: {finalMaxHealth}, 방어력: {finalDefense}");
    }
    
    /// <summary>
    /// ⚔️ 2단계: 장비 스탯 추가
    /// </summary>
    private void ApplyEquipmentStats()
    {
        var equippedItems = playerData.RuntimeEquippedItems;
        
        foreach (var kvp in equippedItems)
        {
            EquipmentSlot slot = kvp.Key;
            EquipmentData equipment = kvp.Value;
            
            if (equipment == null) continue;
            
            // 무기 스탯 적용
            if (slot == EquipmentSlot.MainWeapon && equipment.equipmentType == EquipmentType.Weapon)
            {
                finalAttackDamage += equipment.attackDamage;
                finalAttackSpeed *= equipment.attackSpeed;
                finalCriticalChance += equipment.criticalChance;
                finalCriticalDamage += (equipment.criticalDamage - 1f); // 1.5 + (2.0 - 1.0) = 2.5
                
                if (showDebugLogs)
                    Debug.Log($"⚔️ [PlayerRuntimeStats] 무기 적용: {equipment.equipmentName} (+{equipment.attackDamage} 공격력)");
            }
            
            // 방어구 스탯 적용
            if (equipment.equipmentType == EquipmentType.Armor)
            {
                finalDefense += equipment.defenseBonus;
                
                if (showDebugLogs)
                    Debug.Log($"🛡️ [PlayerRuntimeStats] 방어구 적용: {equipment.equipmentName} (+{equipment.defenseBonus} 방어력)");
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
        finalMoveSpeed *= currentClass.MoveSpeedMultiplier;
        finalMaxHealth *= currentClass.HealthMultiplier;
        // finalAttackSpeed는 SkillCooldownMultiplier와 별개로 관리
        
        if (showDebugLogs)
        {
            Debug.Log($"🎭 [PlayerRuntimeStats] {currentClass.ClassName} 배율 적용:");
            Debug.Log($"   - 공격력 x{currentClass.AttackPowerMultiplier} = {finalAttackDamage}");
            Debug.Log($"   - 이동속도 x{currentClass.MoveSpeedMultiplier} = {finalMoveSpeed}");
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
        finalMoveSpeed = Mathf.Clamp(finalMoveSpeed, 0.5f, 20f);
        finalMaxHealth = Mathf.Max(1f, finalMaxHealth);
        finalAttackSpeed = Mathf.Clamp(finalAttackSpeed, 0.1f, 5f);
        finalCriticalChance = Mathf.Clamp01(finalCriticalChance);
        finalCriticalDamage = Mathf.Max(1f, finalCriticalDamage);
        finalDefense = Mathf.Max(0f, finalDefense);
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
        finalCriticalChance = 0f;
        finalCriticalDamage = 1.5f;
        finalDefense = 0f;
    }
    
    /// <summary>
    /// 📝 최종 스탯 로그 출력 (강화된 버전)
    /// </summary>
    private void LogFinalStats()
    {
        Debug.Log($"📊 [PlayerRuntimeStats] =====최종 스탯 계산 완료=====");
        Debug.Log($"   ⚔️ 공격력: {finalAttackDamage:F1} (변경: {finalAttackDamage - previousAttackDamage:+F1;-F1;±0})");
        Debug.Log($"   🏃 이동속도: {finalMoveSpeed:F1} (변경: {finalMoveSpeed - previousMoveSpeed:+F1;-F1;±0})");
        Debug.Log($"   ❤️ 최대체력: {finalMaxHealth:F0} (변경: {finalMaxHealth - previousMaxHealth:+F0;-F0;±0})");
        Debug.Log($"   ⚡ 공격속도: {finalAttackSpeed:F2}");
        Debug.Log($"   🎯 크리티컬: {finalCriticalChance:P1} (x{finalCriticalDamage:F1})");
        Debug.Log($"   🛡️ 방어력: {finalDefense:F1} (변경: {finalDefense - previousDefense:+F1;-F1;±0})");
        Debug.Log($"=====================================");
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
        // PlayerController 동기화
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerController.SyncWithRuntimeStats();
        }
        
        // PlayerHealth 동기화
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.SyncWithRuntimeStats();
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
}
