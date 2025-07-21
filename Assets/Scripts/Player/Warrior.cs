using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 워리어 클래스 구현체
/// BaseClassBehaviour를 상속받아 워리어 고유의 특성과 능력을 제공
/// 방어력과 체력에 특화된 근접 탱커 클래스
/// </summary>
public class Warrior : BaseClassBehaviour
{
    #region IPlayerClass 기본 정보 (오버라이드)
    
    public override string ClassName => "워리어";
    public override PlayerType PlayerType => PlayerType.Warrior;
    
    #endregion
    
    #region 워리어 능력치 배율 (오버라이드)
    
    [Header("⚔️ 워리어 능력치 배율")]
    [SerializeField] private float attackPowerMultiplier = 1.1f;   // 10% 공격력 증가
    [SerializeField] private float moveSpeedMultiplier = 0.8f;     // 20% 이동속도 감소 (중갑)
    [SerializeField] private float skillCooldownMultiplier = 1.0f; // 기본 쿨다운 (밸런스)
    [SerializeField] private float healthMultiplier = 1.5f;        // 50% 체력 증가 (탱커)
    
    public override float AttackPowerMultiplier => attackPowerMultiplier;
    public override float MoveSpeedMultiplier => moveSpeedMultiplier;
    public override float SkillCooldownMultiplier => skillCooldownMultiplier;
    public override float HealthMultiplier => healthMultiplier;
    
    #endregion
    
    #region 워리어 고유 특성
    
    [Header("🛡️ 워리어 고유 특성")]
    [SerializeField] private float blockChance = 0.2f;           // 20% 블록 확률
    [SerializeField] private float blockDamageReduction = 0.5f;  // 블록 시 50% 데미지 감소
    [SerializeField] private float counterAttackChance = 0.15f;  // 15% 반격 확률
    [SerializeField] private float counterAttackDamage = 1.3f;   // 반격 데미지 130%
    [SerializeField] private float berserkerThreshold = 0.3f;    // 30% 체력 이하 시 버서커
    [SerializeField] private float berserkerDamageBonus = 1.5f;  // 버서커 모드 50% 데미지 증가
    [SerializeField] private float knockbackResistance = 0.5f;   // 50% 넉백 저항
    
    #endregion
    
    #region Unity 생명주기 오버라이드 (디버깅용)
    
    protected override void Start()
    {
        Debug.Log("⚔️ [Warrior] Start() 호출됨 - BaseClassBehaviour 상속 확인!");
        base.Start(); // BaseClassBehaviour.Start() 호출
    }
    
    protected override void Update()
    {
        base.Update(); // BaseClassBehaviour.Update() 호출
    }
    
    #endregion
    
    #region BaseClassBehaviour 추상 메서드 구현
    
    protected override void SetupClassSkills()
    {
        if (skillController == null) return;
        
        // 워리어 전용 스킬들을 자동으로 SkillController에 할당
        var warriorSkill1 = GetComponent<WarriorSkill1>();
        var warriorSkill2 = GetComponent<WarriorSkill2>();
        
        if (warriorSkill1 != null)
        {
            skillController.SkillSet.SetSkill(0, warriorSkill1);
            if (showDebugLogs)
                Debug.Log($"🎯 [Warrior] WarriorSkill1 자동 할당 완료");
        }
        
        if (warriorSkill2 != null)
        {
            skillController.SkillSet.SetSkill(1, warriorSkill2);
            if (showDebugLogs)
                Debug.Log($"🎯 [Warrior] WarriorSkill2 자동 할당 완료");
        }
    }
    
    protected override void ApplyLevelUpBonus(int newLevel)
    {
        if (showDebugLogs)
            Debug.Log($"🆙 [Warrior] 레벨업! Lv.{newLevel} - 워리어 보너스 적용");
        
        // 레벨업 시 워리어 고유 보너스
        // 예: 레벨마다 블록 확률 0.5% 증가
        blockChance += 0.005f;
        
        // 5레벨마다 반격 확률 1% 증가
        if (newLevel % 5 == 0)
        {
            counterAttackChance += 0.01f;
            if (showDebugLogs)
                Debug.Log($"   - 반격 확률 증가: {counterAttackChance * 100:F1}%");
        }
        
        // 10레벨마다 넉백 저항 5% 증가
        if (newLevel % 10 == 0)
        {
            knockbackResistance += 0.05f;
            knockbackResistance = Mathf.Min(knockbackResistance, 0.9f); // 최대 90%
            if (showDebugLogs)
                Debug.Log($"   - 넉백 저항 증가: {knockbackResistance * 100:F1}%");
        }
    }
    
    protected override float ApplyAdditionalDamageModifiers(float modifiedDamage, float baseDamage)
    {
        // 버서커 모드 판정 (체력 30% 이하)
        if (playerHealth != null)
        {
            // PlayerHealth에서 현재 체력 비율 확인
            var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxHealthField = typeof(PlayerHealth).GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (currentHealthField != null && maxHealthField != null)
            {
                int currentHealth = (int)currentHealthField.GetValue(playerHealth);
                int maxHealth = (int)maxHealthField.GetValue(playerHealth);
                float healthRatio = (float)currentHealth / maxHealth;
                
                if (healthRatio <= berserkerThreshold)
                {
                    modifiedDamage *= berserkerDamageBonus;
                    if (showDebugLogs && Time.frameCount % 60 == 0) // 1초마다 로그
                        Debug.Log($"🔥 [Warrior] 버서커 모드! 데미지: {baseDamage} → {modifiedDamage}");
                }
            }
        }
        
        return modifiedDamage;
    }
    
    public override void ApplyPassiveEffects()
    {
        // 워리어 패시브 효과들을 여기서 지속적으로 처리
        // 실제 구현은 공격/피격 시점에 다른 시스템에서 호출하도록 설계
    }
    
    #endregion
    
    #region 워리어 고유 기능
    
    /// <summary>
    /// 워리어 고유 스킬 사용
    /// </summary>
    public void UseWarriorSkill(int skillSlot = 0)
    {
        if (!isActive || !isInitialized)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [Warrior] 클래스가 비활성화되어 있거나 초기화되지 않았습니다.");
            return;
        }
        
        if (skillController != null)
        {
            // SkillController를 통해 해당 슬롯의 스킬 실행
            var skill = skillController.SkillSet.GetSkill(skillSlot);
            if (skill != null && skill.CanUse())
            {
                skill.Execute();
                
                if (showDebugLogs)
                    Debug.Log($"⚔️ [Warrior] 스킬 사용: {skill.SkillName}");
            }
        }
        else
        {
            // Fallback: 기존 방식
            Debug.Log("⚔️ 워리어 스킬 발동!");
        }
    }
    
    /// <summary>
    /// 블록 판정 (피격 시 호출)
    /// </summary>
    public bool TryBlock()
    {
        if (Random.Range(0f, 1f) < blockChance)
        {
            if (showDebugLogs)
                Debug.Log($"🛡️ [Warrior] 블록 성공! 데미지 {blockDamageReduction * 100}% 감소");
            
            // 반격 판정
            if (Random.Range(0f, 1f) < counterAttackChance)
            {
                TriggerCounterAttack();
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 반격 실행 - ⭐ [Phase B] 실제 데미지 적용 개선
    /// </summary>
    private void TriggerCounterAttack()
    {
        if (showDebugLogs)
            Debug.Log($"⚡ [Warrior] 반격 발동! 데미지 {counterAttackDamage}배");
        
        // ⭐ [Phase B] 실제 반격 데미지 처리 - 주변 적들에게 즉시 데미지
        float counterRange = 3f; // 반격 범위
        Collider2D[] nearbyEnemies = null;
        
        try
        {
            // 1순위: Enemy 레이어로 탐지
            nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, counterRange, LayerMask.GetMask("Enemy"));
            if (showDebugLogs)
                Debug.Log($"⚡ [Warrior] Enemy 레이어로 {nearbyEnemies.Length}명의 적 감지!");
        }
        catch (System.Exception)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [Warrior] Enemy 레이어가 정의되지 않음. EnemyHealth 컴포넌트 검색...");
            nearbyEnemies = null;
        }
        
        // 2순위: EnemyHealth 컴포넌트로 찾기
        if (nearbyEnemies == null || nearbyEnemies.Length == 0)
        {
            Collider2D[] allColliders = Physics2D.OverlapCircleAll(transform.position, counterRange);
            List<Collider2D> enemyColliders = new List<Collider2D>();
            
            foreach (Collider2D collider in allColliders)
            {
                if (collider != null && collider.GetComponent<EnemyHealth>() != null)
                {
                    enemyColliders.Add(collider);
                }
            }
            
            nearbyEnemies = enemyColliders.ToArray();
            if (showDebugLogs)
                Debug.Log($"⚡ [Warrior] EnemyHealth 컴포넌트로 {nearbyEnemies.Length}명의 적 감지!");
        }
        
        // 반격 데미지 적용
        if (nearbyEnemies != null && nearbyEnemies.Length > 0)
        {
            // 기본 무기 데미지 가져오기
            float baseDamage = 10f; // 기본값
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
            {
                var weapon = activeWeapon.CurrentActiveWeapon as IWeapon;
                if (weapon != null)
                {
                    baseDamage = weapon.GetWeaponInfo().weaponDamage;
                }
            }
            
            float counterDamage = baseDamage * counterAttackDamage; // 130% 데미지
            
            foreach (Collider2D enemyCollider in nearbyEnemies)
            {
                if (enemyCollider == null) continue;
                
                var enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(Mathf.RoundToInt(counterDamage));
                    
                    if (showDebugLogs)
                        Debug.Log($"⚡ [Warrior] 반격으로 {enemyCollider.name}에게 {counterDamage} 데미지!");
                }
            }
            
            // 반격 이펙트 생성 (옵션)
            if (GamePoolManager.Instance != null)
            {
                var counterEffect = GamePoolManager.Instance.SpawnFromPool("Slash Prefab", transform.position, Quaternion.identity);
                if (counterEffect != null)
                {
                    // 반격 이펙트는 금색으로 변경
                    var spriteRenderer = counterEffect.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = Color.yellow;
                    }
                }
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("🟡 [Warrior] 반격 범위 내에 적이 없습니다!");
        }
    }
    
    /// <summary>
    /// 버서커 모드 상태 확인
    /// </summary>
    public bool IsInBerserkerMode()
    {
        if (playerHealth == null) return false;
        
        var currentHealthField = typeof(PlayerHealth).GetField("currentHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxHealthField = typeof(PlayerHealth).GetField("maxHealth", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (currentHealthField != null && maxHealthField != null)
        {
            int currentHealth = (int)currentHealthField.GetValue(playerHealth);
            int maxHealth = (int)maxHealthField.GetValue(playerHealth);
            float healthRatio = (float)currentHealth / maxHealth;
            
            return healthRatio <= berserkerThreshold;
        }
        
        return false;
    }
    
    /// <summary>
    /// 넉백 저항 적용
    /// </summary>
    public float ApplyKnockbackResistance(float knockbackForce)
    {
        float resistedForce = knockbackForce * (1f - knockbackResistance);
        
        if (showDebugLogs && knockbackForce > resistedForce)
            Debug.Log($"🏋️ [Warrior] 넉백 저항! {knockbackForce} → {resistedForce}");
        
        return resistedForce;
    }
    
    #endregion
    
    #region 공개 유틸리티 메서드
    
    /// <summary>
    /// 외부에서 블록 확률 조회
    /// </summary>
    public float GetBlockChance() => blockChance;
    
    /// <summary>
    /// 외부에서 반격 확률 조회
    /// </summary>
    public float GetCounterAttackChance() => counterAttackChance;
    
    /// <summary>
    /// 현재 워리어 상태 정보 출력 (BaseClass 확장)
    /// </summary>
    public override void PrintStatus()
    {
        base.PrintStatus(); // 기본 정보 출력
        
        Debug.Log($"⚔️ [Warrior] 고유 특성:");
        Debug.Log($"   - 블록 확률: {blockChance * 100:F1}%");
        Debug.Log($"   - 블록 데미지 감소: {blockDamageReduction * 100:F1}%");
        Debug.Log($"   - 반격 확률: {counterAttackChance * 100:F1}%");
        Debug.Log($"   - 버서커 임계점: {berserkerThreshold * 100:F1}%");
        Debug.Log($"   - 넉백 저항: {knockbackResistance * 100:F1}%");
        Debug.Log($"   - 버서커 모드: {(IsInBerserkerMode() ? "활성" : "비활성")}");
    }
    
    #endregion
    
    #region ⭐ [Phase C] 저장/로드 시스템
    
    /// <summary>
    /// 현재 Warrior 데이터를 저장
    /// </summary>
    public void SaveWarriorData()
    {
        // ⭐ 게임 종료 중일 때는 저장하지 않음 (SaveManager가 파괴될 수 있음)
        if (SaveManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.Log("ℹ️ [Warrior] SaveManager 없음. 게임 종료 중이므로 저장 생략.");
            return;
        }
        
        var saveData = new BaseClassSaveData();
        saveData.classType = PlayerType.Warrior;
        saveData.classLevel = playerLevel?.CurrentLevel ?? 1;
        saveData.isUnlocked = true;
        saveData.wasActiveLastTime = IsActiveClass;
        
        // Warrior 특성 데이터 저장
        saveData.SetProperty("blockChance", blockChance);
        saveData.SetProperty("counterChance", counterAttackChance);
        saveData.SetProperty("berserkerThreshold", berserkerThreshold);
        saveData.SetProperty("blockDamageReduction", blockDamageReduction);
        saveData.SetProperty("counterAttackDamage", counterAttackDamage);
        saveData.SetProperty("berserkerDamageMultiplier", berserkerDamageBonus);
        saveData.SetProperty("knockbackResistance", knockbackResistance);
        
        // 캐릭터 인덱스는 현재 기본값 0 사용 (추후 확장 가능)
        int characterIndex = 0;
        SaveManager.Instance.SaveClassData(characterIndex, PlayerType.Warrior, saveData);
        
        if (showDebugLogs)
            Debug.Log($"💾 [Warrior] 데이터 저장 완료: {saveData}");
    }
    
    /// <summary>
    /// 저장된 Warrior 데이터를 불러와서 적용
    /// </summary>
    public void LoadWarriorData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("💥 [Warrior] SaveManager.Instance가 null입니다!");
            return;
        }
        
        // 캐릭터 인덱스는 현재 기본값 0 사용 (추후 확장 가능)
        int characterIndex = 0;
        var saveData = SaveManager.Instance.LoadClassData(characterIndex, PlayerType.Warrior);
        
        if (saveData == null)
        {
            Debug.LogWarning("⚠️ [Warrior] 저장 데이터가 없습니다. 기본값 사용");
            return;
        }
        
        // Warrior 특성 데이터 복원
        blockChance = saveData.GetProperty("blockChance", 0.15f);
        counterAttackChance = saveData.GetProperty("counterChance", 0.1f);
        berserkerThreshold = saveData.GetProperty("berserkerThreshold", 0.3f);
        blockDamageReduction = saveData.GetProperty("blockDamageReduction", 0.5f);
        counterAttackDamage = saveData.GetProperty("counterAttackDamage", 1.5f);
        berserkerDamageBonus = saveData.GetProperty("berserkerDamageMultiplier", 1.5f);
        knockbackResistance = saveData.GetProperty("knockbackResistance", 0.8f);
        
        // 활성화 상태 복원
        if (saveData.wasActiveLastTime)
        {
            SetActive(true);
        }
        
        if (showDebugLogs)
            Debug.Log($"📁 [Warrior] 데이터 불러오기 완료: {saveData}");
    }
    
    /// <summary>
    /// 게임 시작 시 자동 로드 (BaseClassBehaviour.Awake 후 실행)
    /// </summary>
    protected override void Awake()
    {
        base.Awake(); // 부모 클래스 Awake 먼저 실행
        
        // 자동으로 저장된 데이터 불러오기
        Invoke(nameof(LoadWarriorData), 0.1f); // 다른 시스템 초기화 후 실행
    }
    
    /// <summary>
    /// 레벨업이나 특성 변경 시 즉시 저장 (공개 메서드)
    /// </summary>
    public void SaveDataNow()
    {
        SaveWarriorData();
    }
    
    /// <summary>
    /// 게임 종료 시 자동 저장
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveWarriorData();
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveWarriorData();
    }
    
    protected override void OnDestroy()
    {
        // ⭐ OnDestroy에서는 저장하지 않음 (SaveManager가 이미 파괴될 수 있음)
        // 대신 OnApplicationPause, OnApplicationFocus에서 저장
        base.OnDestroy(); // 부모 클래스 OnDestroy 호출
    }
    
    #endregion
}
