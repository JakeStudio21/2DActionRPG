using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 플레이어 클래스별 공통 기능을 구현하는 추상 클래스
/// 모든 플레이어 클래스(Assasin, Warrior, Wizard)가 이 클래스를 상속받아 구현
/// ⭐ [Phase C] 다중 클래스 관리 기능 통합
/// </summary>
public abstract class BaseClassBehaviour : MonoBehaviour, IPlayerClass
{
    #region IPlayerClass 추상 속성 (자식 클래스에서 구현)
    
    public abstract string ClassName { get; }
    public abstract PlayerType PlayerType { get; }
    public abstract float AttackPowerMultiplier { get; }
    public abstract float MoveSpeedMultiplier { get; }
    public abstract float SkillCooldownMultiplier { get; }
    public abstract float HealthMultiplier { get; }
    
    #endregion
    
    #region ⭐ [Phase C] 다중 클래스 관리 설정
    
    [Header("🎯 다중 클래스 관리 (첫 번째 클래스에서만 설정)")]
    [SerializeField] protected bool allowMultipleClasses = false;
    [SerializeField] protected bool isClassManager = false; // 이 클래스가 매니저 역할을 할지
    
    // 정적 관리 데이터
    private static List<BaseClassBehaviour> allClasses = new List<BaseClassBehaviour>();
    private static BaseClassBehaviour managerClass = null;
    private static bool isSystemInitialized = false;
    
    #endregion
    
    #region 공통 설정
    
    [Header("🔧 공통 디버그")]
    [SerializeField] protected bool showDebugLogs = true;
    
    #endregion
    
    #region 공통 컴포넌트 참조
    
    protected PlayerController playerController;
    protected PlayerHealth playerHealth;
    protected SkillController skillController;
    protected ActiveWeapon activeWeapon;
    protected PlayerLevel playerLevel;
    
    protected bool isInitialized = false;
    protected bool isActive = false;
    
    // ⭐ [Phase B 에러 수정] isActive에 대한 public 접근자 추가
    public bool IsActiveClass => isActive;
    
    #endregion
    
    #region Unity 생명주기 (공통)
    
    protected virtual void Awake()
    {
        // ⭐ [Phase C] 다중 클래스 시스템에 등록
        RegisterToClassSystem();
    }
    
    protected virtual void Start()
    {
        // 컴포넌트 참조 획득
        GetComponentReferences();
        
        // ⭐ [Phase C] 클래스 매니저인 경우 시스템 초기화
        if (isClassManager || managerClass == null)
        {
            managerClass = this;
            InitializeClassSystem();
        }
        
        // 개별 클래스 초기화는 시스템에서 관리
        if (!isSystemInitialized)
        {
            // 클래스 초기화
            InitializeClass();
            
            // PlayerLevel 이벤트 구독
            if (playerLevel != null)
            {
                playerLevel.OnLevelChanged += HandleLevelUp;
            }
        }
    }
    
    protected virtual void Update()
    {
        if (isActive && isInitialized)
        {
            // 패시브 효과 지속 적용
            ApplyPassiveEffects();
        }
    }
    
    protected virtual void OnDestroy()
    {
        // ⭐ [Phase C] 시스템에서 제거
        UnregisterFromClassSystem();
        
        // PlayerLevel 이벤트 구독 해제
        if (playerLevel != null)
        {
            playerLevel.OnLevelChanged -= HandleLevelUp;
        }
    }
    
    #endregion
    
    #region ⭐ [Phase C] 다중 클래스 관리 시스템
    
    /// <summary>
    /// 클래스 시스템에 등록
    /// </summary>
    private void RegisterToClassSystem()
    {
        if (!allClasses.Contains(this))
        {
            allClasses.Add(this);
            
            if (showDebugLogs)
                Debug.Log($"🔍 [ClassSystem] {ClassName} 등록됨 (총 {allClasses.Count}개 클래스)");
        }
    }
    
    /// <summary>
    /// 클래스 시스템에서 제거
    /// </summary>
    private void UnregisterFromClassSystem()
    {
        allClasses.Remove(this);
        
        if (managerClass == this)
        {
            managerClass = allClasses.FirstOrDefault();
            if (showDebugLogs && managerClass != null)
                Debug.Log($"🔄 [ClassSystem] 매니저 변경: {managerClass.ClassName}");
        }
    }
    
    /// <summary>
    /// 클래스 시스템 초기화 (매니저 클래스에서 호출)
    /// </summary>
    private void InitializeClassSystem()
    {
        if (isSystemInitialized) return;
        
        if (showDebugLogs)
            Debug.Log($"🎯 [ClassSystem] 시스템 초기화 시작 - 매니저: {ClassName}");
        
        // 모든 클래스 초기 비활성화
        foreach (var classComp in allClasses)
        {
            classComp.SetActive(false);
        }
        
        // 다중 클래스 허용 여부에 따라 활성화
        if (allowMultipleClasses)
        {
            ActivateAllClasses();
        }
        else
        {
            ActivateHighestPriorityClass();
        }
        
        isSystemInitialized = true;
        
        if (showDebugLogs)
            Debug.Log($"✅ [ClassSystem] 시스템 초기화 완료");
    }
    
    /// <summary>
    /// 우선순위가 가장 높은 클래스만 활성화
    /// </summary>
    private void ActivateHighestPriorityClass()
    {
        if (allClasses.Count == 0) return;
        
        // 우선순위 순으로 정렬 (Warrior > Assasin > Wizard)
        var sortedClasses = allClasses.OrderByDescending(GetClassPriority).ToList();
        
        // 가장 높은 우선순위 클래스만 활성화
        var highestPriorityClass = sortedClasses[0];
        highestPriorityClass.SetActive(true);
        highestPriorityClass.InitializeClass();
        
        if (showDebugLogs)
        {
            Debug.Log($"🎯 [ClassSystem] 단일 클래스 모드: {highestPriorityClass.ClassName} 활성화");
            
            // 비활성화된 클래스들 로그
            for (int i = 1; i < sortedClasses.Count; i++)
            {
                Debug.Log($"   - {sortedClasses[i].ClassName}: 비활성화 (우선순위 낮음)");
            }
        }
    }
    
    /// <summary>
    /// 모든 클래스 활성화 (다중 클래스 모드)
    /// </summary>
    private void ActivateAllClasses()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [ClassSystem] 다중 클래스 모드: 모든 클래스 활성화");
        
        foreach (var classComp in allClasses)
        {
            classComp.SetActive(true);
            classComp.InitializeClass();
            
            if (showDebugLogs)
                Debug.Log($"   - {classComp.ClassName}: 활성화");
        }
        
        // 다중 클래스 모드에서는 충돌 해결
        ResolveMultiClassConflicts();
    }
    
    /// <summary>
    /// 다중 클래스 충돌 해결
    /// </summary>
    private void ResolveMultiClassConflicts()
    {
        if (showDebugLogs)
            Debug.Log("🔧 [ClassSystem] 다중 클래스 충돌 해결 중...");
        
        ResolveSkillSlotConflicts();
        ResolveStatConflicts();
    }
    
    /// <summary>
    /// 스킬 슬롯 충돌 해결
    /// </summary>
    private void ResolveSkillSlotConflicts()
    {
        if (skillController == null) return;
        
        var activeClasses = allClasses.Where(c => c.IsActive()).OrderByDescending(GetClassPriority).ToList();
        int slotIndex = 0;
        
        foreach (var classComp in activeClasses)
        {
            AssignClassSkillsToSlots(classComp, ref slotIndex);
        }
        
        if (showDebugLogs)
            Debug.Log($"   - 스킬 슬롯 재배치 완료 (총 {slotIndex}개 슬롯 사용)");
    }
    
    /// <summary>
    /// 능력치 충돌 해결
    /// </summary>
    private void ResolveStatConflicts()
    {
        var activeClasses = allClasses.Where(c => c.IsActive()).OrderByDescending(GetClassPriority).ToList();
        
        if (activeClasses.Count <= 1) return; // 충돌 없음
        
        // 최고 우선순위 클래스의 능력치만 적용 (간단한 해결책)
        var dominantClass = activeClasses[0];
        
        // 다른 클래스들은 능력치 적용을 건너뛰도록 플래그 설정
        foreach (var classComp in activeClasses.Skip(1))
        {
            classComp.isActive = true; // 활성은 유지
            // 하지만 ApplyClassStats는 건너뛰도록
        }
        
        if (showDebugLogs)
            Debug.Log($"   - 능력치 충돌 해결: {dominantClass.ClassName}의 능력치 적용");
    }
    
    /// <summary>
    /// 특정 클래스의 스킬을 슬롯에 할당
    /// </summary>
    private void AssignClassSkillsToSlots(BaseClassBehaviour classComp, ref int startSlot)
    {
        if (classComp.PlayerType == PlayerType.Warrior)
        {
            var skill1 = classComp.GetComponent<WarriorSkill1>();
            var skill2 = classComp.GetComponent<WarriorSkill2>();
            
            if (skill1 != null && skillController != null)
            {
                skillController.SkillSet.SetSkill(startSlot++, skill1);
                if (showDebugLogs)
                    Debug.Log($"     - WarriorSkill1 → 슬롯 {startSlot - 1}");
            }
            
            if (skill2 != null && skillController != null)
            {
                skillController.SkillSet.SetSkill(startSlot++, skill2);
                if (showDebugLogs)
                    Debug.Log($"     - WarriorSkill2 → 슬롯 {startSlot - 1}");
            }
        }
        else if (classComp.PlayerType == PlayerType.Assasin)
        {
            var skill1 = classComp.GetComponent<AssasinSkill1>();
            var skill2 = classComp.GetComponent<AssasinSkill2>();
            
            if (skill1 != null && skillController != null)
            {
                skillController.SkillSet.SetSkill(startSlot++, skill1);
                if (showDebugLogs)
                    Debug.Log($"     - AssasinSkill1 → 슬롯 {startSlot - 1}");
            }
            
            if (skill2 != null && skillController != null)
            {
                skillController.SkillSet.SetSkill(startSlot++, skill2);
                if (showDebugLogs)
                    Debug.Log($"     - AssasinSkill2 → 슬롯 {startSlot - 1}");
            }
        }
        // 향후 Wizard 등 추가
    }
    
    /// <summary>
    /// 클래스 우선순위 반환
    /// </summary>
    private int GetClassPriority(BaseClassBehaviour classComp)
    {
        return classComp.PlayerType switch
        {
            PlayerType.Warrior => 3,
            PlayerType.Assasin => 2,
            PlayerType.Wizard => 1,
            _ => 0
        };
    }
    
    /// <summary>
    /// 현재 활성 클래스들 반환
    /// </summary>
    public static List<BaseClassBehaviour> GetAllActiveClasses()
    {
        return allClasses.Where(c => c.IsActive()).ToList();
    }
    
    /// <summary>
    /// 특정 타입의 클래스 강제 활성화
    /// </summary>
    public static void ForceActivateClass(PlayerType playerType)
    {
        var targetClass = allClasses.FirstOrDefault(c => c.PlayerType == playerType);
        
        if (targetClass != null && managerClass != null)
        {
            if (!managerClass.allowMultipleClasses)
            {
                // 단일 모드: 다른 클래스 비활성화
                foreach (var classComp in allClasses)
                    classComp.SetActive(false);
            }
            
            targetClass.SetActive(true);
            targetClass.InitializeClass();
            
            if (managerClass.allowMultipleClasses)
                managerClass.ResolveMultiClassConflicts();
                
            Debug.Log($"🔄 [ClassSystem] {playerType} 클래스 강제 활성화");
        }
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Print Class System Status")]
    public void PrintClassSystemStatus()
    {
        Debug.Log("📊 [ClassSystem] 통합 클래스 시스템 상태:");
        Debug.Log($"   - 매니저 클래스: {(managerClass?.ClassName ?? "없음")}");
        Debug.Log($"   - 다중 클래스 허용: {allowMultipleClasses}");
        Debug.Log($"   - 총 클래스 수: {allClasses.Count}");
        
        foreach (var classComp in allClasses)
        {
            string status = classComp.IsActive() ? "🟢 활성" : "🔴 비활성";
            int priority = GetClassPriority(classComp);
            Debug.Log($"   - {classComp.ClassName}: {status} (우선순위: {priority})");
        }
    }
    
    #endregion
    
    #region IPlayerClass 공통 구현
    
    public virtual void InitializeClass()
    {
        if (showDebugLogs)
            Debug.Log($"🎯 [BaseClass] {ClassName} 클래스 초기화 시작");
        
        // 컴포넌트 참조 확인
        if (!ValidateComponents())
        {
            Debug.LogError($"🔴 [BaseClass] {ClassName} 필수 컴포넌트가 누락되어 초기화를 중단합니다.");
            return;
        }
        
        // 클래스별 능력치 적용
        ApplyClassStats();
        
        // 클래스별 스킬 설정
        SetupClassSkills();
        
        // 패시브 효과 초기 적용
        ApplyPassiveEffects();
        
        isInitialized = true;
        SetActive(true);
        
        if (showDebugLogs)
            Debug.Log($"🟢 [BaseClass] {ClassName} 클래스 초기화 완료!");
    }
    
    public virtual void ApplyClassStats()
    {
        if (showDebugLogs)
            Debug.Log($"🎯 [BaseClass] {ClassName} 클래스별 능력치 적용 중...");
        
        // ⭐ [Phase C] 다중 클래스 충돌 방지
        if (managerClass != null && managerClass.allowMultipleClasses)
        {
            var activeClasses = allClasses.Where(c => c.IsActive()).OrderByDescending(GetClassPriority).ToList();
            
            // 최고 우선순위 클래스가 아니면 능력치 적용 건너뜀
            if (activeClasses.Count > 1 && activeClasses[0] != this)
            {
                if (showDebugLogs)
                    Debug.Log($"🟡 [BaseClass] {ClassName} 다중 클래스 모드에서 능력치 적용 건너뜀 (우선순위 낮음)");
                return;
            }
        }
        
        // PlayerController에 이동속도 배율 적용
        ApplyMoveSpeedMultiplier();
        
        // PlayerHealth에 체력 배율 적용  
        ApplyHealthMultiplier();
        
        // SkillController에 쿨다운 배율 적용
        ApplySkillCooldownMultiplier();
        
        // 자식 클래스별 추가 능력치 적용
        ApplyAdditionalStats();
    }
    
    public virtual void OnLevelUp(int newLevel)
    {
        if (showDebugLogs)
            Debug.Log($"🆙 [BaseClass] {ClassName} 레벨업! Lv.{newLevel}");
        
        // 자식 클래스에서 구체적인 레벨업 보너스 구현
        ApplyLevelUpBonus(newLevel);
    }
    
    public virtual void ApplyPassiveEffects()
    {
        // 자식 클래스에서 패시브 효과 구현
    }
    
    public virtual float GetModifiedCooldown(float baseCooldown)
    {
        float modifiedCooldown = baseCooldown * SkillCooldownMultiplier;
        
        if (showDebugLogs && Time.frameCount % 300 == 0) // 5초마다 로그
            Debug.Log($"🕐 [BaseClass] {ClassName} 쿨다운 수정: {baseCooldown}초 → {modifiedCooldown}초");
        
        return modifiedCooldown;
    }
    
    public virtual float GetModifiedDamage(float baseDamage)
    {
        float modifiedDamage = baseDamage * AttackPowerMultiplier;
        
        // 자식 클래스에서 추가 데미지 계산 (크리티컬 등)
        modifiedDamage = ApplyAdditionalDamageModifiers(modifiedDamage, baseDamage);
        
        return modifiedDamage;
    }
    
    public virtual bool IsActive()
    {
        return isActive;
    }
    
    public virtual void SetActive(bool active)
    {
        isActive = active;
        
        if (showDebugLogs)
            Debug.Log($"🔄 [BaseClass] {ClassName} 클래스 활성화 상태: {(active ? "활성화" : "비활성화")}");
    }
    
    #endregion
    
    #region 공통 유틸리티 메서드
    
    /// <summary>
    /// 컴포넌트 참조 획득
    /// </summary>
    protected virtual void GetComponentReferences()
    {
        playerController = GetComponent<PlayerController>();
        playerHealth = GetComponent<PlayerHealth>();
        skillController = GetComponent<SkillController>();
        activeWeapon = GetComponent<ActiveWeapon>();
        playerLevel = FindObjectOfType<PlayerLevel>();
        
        if (showDebugLogs)
            Debug.Log($"🔗 [BaseClass] {ClassName} 컴포넌트 참조 획득 완료");
    }
    
    /// <summary>
    /// 필수 컴포넌트 유효성 검사
    /// </summary>
    protected virtual bool ValidateComponents()
    {
        bool isValid = true;
        
        if (playerController == null)
        {
            Debug.LogError($"🔴 [BaseClass] {ClassName} PlayerController가 없습니다!");
            isValid = false;
        }
        
        if (playerHealth == null)
        {
            Debug.LogError($"🔴 [BaseClass] {ClassName} PlayerHealth가 없습니다!");
            isValid = false;
        }
        
        if (skillController == null)
        {
            Debug.LogWarning($"🟡 [BaseClass] {ClassName} SkillController가 없습니다. 스킬 기능이 제한됩니다.");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// PlayerLevel 레벨업 이벤트 핸들러
    /// </summary>
    private void HandleLevelUp()
    {
        if (playerLevel != null)
        {
            OnLevelUp(playerLevel.CurrentLevel);
        }
    }
    
    /// <summary>
    /// 현재 클래스 상태 정보 출력
    /// </summary>
    public virtual void PrintStatus()
    {
        Debug.Log($"🎯 [BaseClass] {ClassName} 상태 정보:");
        Debug.Log($"   - 공격력 배율: {AttackPowerMultiplier}x");
        Debug.Log($"   - 이동속도 배율: {MoveSpeedMultiplier}x");
        Debug.Log($"   - 쿨다운 배율: {SkillCooldownMultiplier}x");
        Debug.Log($"   - 체력 배율: {HealthMultiplier}x");
        Debug.Log($"   - 활성화 상태: {(isActive ? "활성" : "비활성")}");
        Debug.Log($"   - 초기화 상태: {(isInitialized ? "완료" : "미완료")}");
    }
    
    #endregion
    
    #region 실제 능력치 적용 메서드 (Option 3에서 구현)
    
    /// <summary>
    /// PlayerController에 이동속도 배율 적용
    /// </summary>
    protected virtual void ApplyMoveSpeedMultiplier()
    {
        if (playerController != null)
        {
            // Reflection을 사용하여 private 필드에 접근
            var moveSpeedField = typeof(PlayerController).GetField("moveSpeed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var startingMoveSpeedField = typeof(PlayerController).GetField("startingMoveSpeed", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (moveSpeedField != null && startingMoveSpeedField != null)
            {
                float baseMoveSpeed = (float)moveSpeedField.GetValue(playerController);
                float baseStartingMoveSpeed = (float)startingMoveSpeedField.GetValue(playerController);
                
                // 기본 속도가 아직 설정되지 않았다면 현재 속도를 기본값으로 저장
                if (baseStartingMoveSpeed == 0f)
                {
                    baseStartingMoveSpeed = baseMoveSpeed;
                    startingMoveSpeedField.SetValue(playerController, baseStartingMoveSpeed);
                }
                
                // 클래스별 배율 적용
                float newMoveSpeed = baseStartingMoveSpeed * MoveSpeedMultiplier;
                moveSpeedField.SetValue(playerController, newMoveSpeed);
                
                if (showDebugLogs)
                    Debug.Log($"   - 이동속도 배율: {MoveSpeedMultiplier}x 적용 ({baseStartingMoveSpeed} → {newMoveSpeed})");
            }
            else
            {
                Debug.LogWarning($"🟡 [BaseClass] {ClassName} moveSpeed 필드에 접근할 수 없습니다.");
            }
        }
    }
    
    /// <summary>
    /// PlayerHealth에 체력 배율 적용
    /// </summary>
    protected virtual void ApplyHealthMultiplier()
    {
        if (playerHealth != null)
        {
            // Reflection을 사용하여 private 필드에 접근
            var maxHealthField = typeof(PlayerHealth).GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (maxHealthField != null && currentHealthField != null)
            {
                int baseMaxHealth = (int)maxHealthField.GetValue(playerHealth);
                int baseCurrentHealth = (int)currentHealthField.GetValue(playerHealth);
                
                // 클래스별 체력 배율 적용
                int newMaxHealth = Mathf.RoundToInt(baseMaxHealth * HealthMultiplier);
                newMaxHealth = Mathf.Max(1, newMaxHealth); // 최소 1 보장
                
                // maxHealth 업데이트
                maxHealthField.SetValue(playerHealth, newMaxHealth);
                
                // currentHealth도 비례적으로 조정 (체력이 풀인 상태라면 새로운 최대치로 설정)
                if (baseCurrentHealth >= baseMaxHealth)
                {
                    currentHealthField.SetValue(playerHealth, newMaxHealth);
                }
                else
                {
                    // 현재 체력 비율 유지
                    float healthRatio = (float)baseCurrentHealth / baseMaxHealth;
                    int newCurrentHealth = Mathf.RoundToInt(newMaxHealth * healthRatio);
                    newCurrentHealth = Mathf.Max(1, newCurrentHealth);
                    currentHealthField.SetValue(playerHealth, newCurrentHealth);
                }
                
                // UI 업데이트 (PlayerHealth의 UpdateHealthSlider 메서드 호출)
                var updateMethod = typeof(PlayerHealth).GetMethod("UpdateHealthSlider", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (updateMethod != null)
                {
                    updateMethod.Invoke(playerHealth, null);
                }
                
                if (showDebugLogs)
                    Debug.Log($"   - 체력 배율: {HealthMultiplier}x 적용 ({baseMaxHealth} → {newMaxHealth})");
            }
            else
            {
                Debug.LogWarning($"🟡 [BaseClass] {ClassName} maxHealth 필드에 접근할 수 없습니다.");
            }
        }
    }
    
    /// <summary>
    /// SkillController에 쿨다운 배율 적용
    /// </summary>
    protected virtual void ApplySkillCooldownMultiplier()
    {
        if (skillController != null)
        {
            // SkillController의 기본 쿨다운 필드들에 배율 적용
            var cooldownTimeField = typeof(SkillController).GetField("cooldownTime", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var skill2CooldownTimeField = typeof(SkillController).GetField("skill2CooldownTime", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (cooldownTimeField != null)
            {
                float baseCooldownTime = (float)cooldownTimeField.GetValue(skillController);
                float newCooldownTime = baseCooldownTime * SkillCooldownMultiplier;
                cooldownTimeField.SetValue(skillController, newCooldownTime);
                
                if (showDebugLogs)
                    Debug.Log($"   - 스킬1 쿨다운: {baseCooldownTime}초 → {newCooldownTime}초");
            }
            
            if (skill2CooldownTimeField != null)
            {
                float baseSkill2CooldownTime = (float)skill2CooldownTimeField.GetValue(skillController);
                float newSkill2CooldownTime = baseSkill2CooldownTime * SkillCooldownMultiplier;
                skill2CooldownTimeField.SetValue(skillController, newSkill2CooldownTime);
                
                if (showDebugLogs)
                    Debug.Log($"   - 스킬2 쿨다운: {baseSkill2CooldownTime}초 → {newSkill2CooldownTime}초");
            }
            
            // 추가: SkillSet의 개별 스킬들에도 배율 적용
            ApplySkillSetCooldownMultiplier();
            
            if (showDebugLogs)
                Debug.Log($"   - 스킬 쿨다운 배율: {SkillCooldownMultiplier}x 적용 완료");
        }
    }
    
    /// <summary>
    /// SkillSet의 개별 스킬들에 쿨다운 배율 적용
    /// </summary>
    private void ApplySkillSetCooldownMultiplier()
    {
        if (skillController?.SkillSet == null) return;
        
        for (int i = 0; i < skillController.SkillSet.SkillCount; i++)
        {
            var skill = skillController.SkillSet.GetSkill(i);
            if (skill != null)
            {
                // ISkill의 Cooldown은 readonly property이므로 직접 수정 불가
                // 대신 GetModifiedCooldown 메서드를 통해 런타임에 배율 적용
                if (showDebugLogs)
                    Debug.Log($"   - {skill.SkillName}: 런타임 쿨다운 배율 적용 준비");
            }
        }
    }
    
    #endregion
    
    #region 추상/가상 메서드 (자식 클래스에서 구현)
    
    /// <summary>
    /// 클래스별 스킬 설정 (자식 클래스에서 구현)
    /// </summary>
    protected abstract void SetupClassSkills();
    
    /// <summary>
    /// 클래스별 추가 능력치 적용 (자식 클래스에서 구현)
    /// </summary>
    protected virtual void ApplyAdditionalStats() { }
    
    /// <summary>
    /// 레벨업 시 클래스별 보너스 적용 (자식 클래스에서 구현)
    /// </summary>
    protected virtual void ApplyLevelUpBonus(int newLevel) { }
    
    /// <summary>
    /// 클래스별 추가 데미지 계산 (크리티컬 등, 자식 클래스에서 구현)
    /// </summary>
    protected virtual float ApplyAdditionalDamageModifiers(float modifiedDamage, float baseDamage)
    {
        return modifiedDamage;
    }
    
    #endregion
} 