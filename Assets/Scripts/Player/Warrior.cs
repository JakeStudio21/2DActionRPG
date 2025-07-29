using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection; // 🆕 Reflection 사용을 위해 추가 (Assasin과 일관성)

/// <summary>
/// 워리어 클래스 구현체
/// BaseClassBehaviour를 상속받아 워리어 고유의 특성과 능력을 제공
/// 방어력과 체력에 특화된 근접 탱커 클래스
/// </summary>
public class Warrior : BaseClassBehaviour
{
    #region IPlayerClass 기본 정보 (오버라이드)
    
    public override string ClassName => warriorData?.className ?? "워리어";
    public override PlayerType PlayerType => warriorData?.playerType ?? PlayerType.Warrior;
    
    #endregion
    
    #region 📊 ScriptableObject 데이터 연동
    
    [Header("📊 워리어 데이터 연동")]
    [SerializeField] private WarriorData warriorData; // ScriptableObject 참조
    
    // ScriptableObject에서 값 가져오기 (null 안전성 포함)
    public override float AttackPowerMultiplier => warriorData?.AttackPowerMultiplier ?? 1.1f;
    public override float MoveSpeedMultiplier => warriorData?.MoveSpeedMultiplier ?? 0.8f;
    public override float SkillCooldownMultiplier => warriorData?.SkillCooldownMultiplier ?? 1.0f;
    public override float HealthMultiplier => warriorData?.HealthMultiplier ?? 1.5f;
    
    #endregion
    
    #region 🆕 BaseClassBehaviour 추상 메서드 구현 (ScriptableObject 기본값)
    
    public override float GetBaseMoveSpeed()
    {
        return warriorData?.baseMoveSpeed ?? 4f; // WarriorData에서 가져오거나 기본값 4
    }

    public override float GetBaseMaxHealth()
    {
        return warriorData?.baseMaxHealth ?? 100f; // WarriorData에서 가져오거나 기본값 100
    }
    
    #endregion
    
    #region 🛡️ 워리어 고유 특성 (ScriptableObject 연동)
    
    // ScriptableObject에서 고유 특성 값들 가져오기 (네이밍 개선)
    public float BlockChance => warriorData?.warriorBlockChance ?? 0.2f;
    public float BlockDamageReduction => warriorData?.warriorBlockDamageReduction ?? 0.5f;
    public float CounterAttackChance => warriorData?.warriorCounterAttackChance ?? 0.15f;
    public float CounterAttackDamage => warriorData?.warriorCounterAttackDamage ?? 1.3f;
    public float BerserkerThreshold => warriorData?.warriorBerserkerThreshold ?? 0.3f;
    public float BerserkerDamageBonus => warriorData?.warriorBerserkerDamageBonus ?? 1.5f;
    public float KnockbackResistance => warriorData?.warriorKnockbackResistance ?? 0.5f;
    
    // 🎮 런타임 상태 변수들 (ScriptableObject와 무관)
    // 향후 고급 워리어 기능 구현 시 사용 예정
    // private bool isCounterAttackReady = true;  // 반격 준비 상태 플래그 (반격 쿨다운 시스템용)
    // private bool isBerserkerModeActive = false; // 버서커 모드 활성화 플래그 (버서커 상태 관리용)
    
    #endregion
    
    #region Unity 생명주기 오버라이드 (디버깅용)
    
    protected override void Start()
    {
        Debug.Log("🔵 [Warrior] Start() 시작");
        
        // 🔍 WarriorData 상태 상세 확인
        if (warriorData != null)
        {
            Debug.Log($"✅ [Warrior] WarriorData 연결됨: {warriorData.name}");
            Debug.Log($"📊 [Warrior] WarriorData 실제 설정값들:");
            Debug.Log($"   - attackPowerMultiplier: {warriorData.AttackPowerMultiplier}");
            Debug.Log($"   - moveSpeedMultiplier: {warriorData.MoveSpeedMultiplier}");
            Debug.Log($"   - skillCooldownMultiplier: {warriorData.SkillCooldownMultiplier}");
            Debug.Log($"   - healthMultiplier: {warriorData.HealthMultiplier}");
            Debug.Log($"   - baseMoveSpeed: {warriorData.baseMoveSpeed}");
            Debug.Log($"   - baseMaxHealth: {warriorData.baseMaxHealth}");
            Debug.Log($"🔍 [Warrior] GetBaseMoveSpeed() 결과: {GetBaseMoveSpeed()}");
            Debug.Log($"🛡️ [Warrior] 워리어 고유 특성들:");
            Debug.Log($"   - 블록 확률: {BlockChance * 100:F1}%");
            Debug.Log($"   - 반격 확률: {CounterAttackChance * 100:F1}%");
            Debug.Log($"   - 버서커 임계점: {BerserkerThreshold * 100:F1}%");
        }
        else
        {
            Debug.LogError("❌ [Warrior] WarriorData가 null입니다!");
            Debug.Log($"🛡️ [Warrior] Fallback 값들:");
            Debug.Log($"   - AttackPowerMultiplier: {AttackPowerMultiplier}");
            Debug.Log($"   - MoveSpeedMultiplier: {MoveSpeedMultiplier}");
            Debug.Log($"   - HealthMultiplier: {HealthMultiplier}");
        }
        
        // 🎯 현재 오버라이드 값 확인
        Debug.Log($"🔧 [Warrior] 현재 배율 값들:");
        Debug.Log($"   - AttackPowerMultiplier: {AttackPowerMultiplier}");
        Debug.Log($"   - MoveSpeedMultiplier: {MoveSpeedMultiplier}");
        Debug.Log($"   - HealthMultiplier: {HealthMultiplier}");
        
        base.Start(); // BaseClassBehaviour.Start() 호출 - 자동 적용
        
        Debug.Log("🔵 [Warrior] Start() 완료 - BaseClassBehaviour 자동 적용만 사용");
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
        // blockChance += 0.005f; // 이제 ScriptableObject에서 관리 - 향후 레벨업 시스템 재설계 필요
        
        // 5레벨마다 반격 확률 1% 증가
        if (newLevel % 5 == 0)
        {
            // counterAttackChance += 0.01f; // 이제 ScriptableObject에서 관리 - 향후 레벨업 시스템 재설계 필요
            // Debug.Log($"   - 반격 확률 증가: {counterAttackChance * 100:F1}%");
        }
        
        // 10레벨마다 넉백 저항 5% 증가
        if (newLevel % 10 == 0)
        {
            // knockbackResistance += 0.05f; // ScriptableObject에서 관리
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
                
                if (healthRatio <= BerserkerThreshold)
                {
                    modifiedDamage *= BerserkerDamageBonus;
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
        if (Random.Range(0f, 1f) < BlockChance)
        {
            if (showDebugLogs)
                Debug.Log($"🛡️ [Warrior] 블록 성공! 데미지 {BlockDamageReduction * 100}% 감소");
            
            // 반격 판정
            if (Random.Range(0f, 1f) < CounterAttackChance)
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
            Debug.Log($"⚡ [Warrior] 반격 발동! 데미지 {CounterAttackDamage}배");
        
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
                    baseDamage = (int)weapon.GetEquipmentData().attackDamage;  // GetWeaponInfo() → GetEquipmentData(), weaponDamage → attackDamage, float → int 변환
                }
            }
            
            float counterDamage = baseDamage * CounterAttackDamage; // 130% 데미지
            
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
            
            return healthRatio <= BerserkerThreshold;
        }
        
        return false;
    }
    
    /// <summary>
    /// 넉백 저항 적용
    /// </summary>
    public float ApplyKnockbackResistance(float knockbackForce)
    {
        float resistedForce = knockbackForce * (1f - KnockbackResistance);
        
        if (showDebugLogs && knockbackForce > resistedForce)
            Debug.Log($"🏋️ [Warrior] 넉백 저항! {knockbackForce} → {resistedForce}");
        
        return resistedForce;
    }
    
    #endregion
    
    #region 공개 유틸리티 메서드
    
    /// <summary>
    /// 외부에서 블록 확률 조회
    /// </summary>
    public float GetBlockChance() => BlockChance;
    
    /// <summary>
    /// 외부에서 반격 확률 조회
    /// </summary>
    public float GetCounterAttackChance() => CounterAttackChance;
    
    /// <summary>
    /// 외부에서 버서커 임계점 조회
    /// </summary>
    public float GetBerserkerThreshold() => BerserkerThreshold;
    
    /// <summary>
    /// 현재 워리어 상태 정보 출력 (BaseClass 확장)
    /// </summary>
    public override void PrintStatus()
    {
        base.PrintStatus(); // 기본 정보 출력
        
        Debug.Log($"⚔️ [Warrior] 고유 특성:");
        Debug.Log($"   - 블록 확률: {BlockChance * 100:F1}%");
        Debug.Log($"   - 블록 데미지 감소: {BlockDamageReduction * 100:F1}%");
        Debug.Log($"   - 반격 확률: {CounterAttackChance * 100:F1}%");
        Debug.Log($"   - 버서커 임계점: {BerserkerThreshold * 100:F1}%");
        Debug.Log($"   - 넉백 저항: {KnockbackResistance * 100:F1}%");
        Debug.Log($"   - 버서커 모드: {(IsInBerserkerMode() ? "활성" : "비활성")}");
    }
    
    #endregion
    
    #region ⭐ [Phase C] 저장/로드 시스템
    
    /// <summary>
    /// 현재 Warrior 데이터를 저장
    /// </summary>
    public void SaveWarriorData()
    {
        // ⭐ PlayerDataManager 통합으로 변경
        if (PlayerDataManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.Log("ℹ️ [Warrior] PlayerDataManager 없음. 게임 종료 중이므로 저장 생략.");
            return;
        }
        
        var saveData = new BaseClassSaveData();
        saveData.classType = PlayerType.Warrior;
        saveData.classLevel = playerLevel?.CurrentLevel ?? 1;
        saveData.isUnlocked = true;
        saveData.wasActiveLastTime = IsActiveClass;
        
        // 🔧 메서드 이름 수정: BlockChance → GetBlockChance()
        saveData.SetProperty("blockChance", GetBlockChance());
        saveData.SetProperty("counterAttackChance", GetCounterAttackChance());
        saveData.SetProperty("berserkerThreshold", GetBerserkerThreshold());
        
        // ⭐ SaveManager 대신 PlayerDataManager 사용
        PlayerDataManager.Instance.SaveClassData(PlayerType.Warrior, saveData);
        
        if (showDebugLogs)
            Debug.Log($"💾 [Warrior] 데이터 저장 완료: {saveData}");
    }

    /// <summary>
    /// 저장된 Warrior 데이터를 불러와서 적용
    /// </summary>
    public void LoadWarriorData()
    {
        // ⭐ PlayerDataManager 통합으로 변경
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("💥 [Warrior] PlayerDataManager.Instance가 null입니다!");
            return;
        }
        
        // ⭐ SaveManager 대신 PlayerDataManager 사용
        var saveData = PlayerDataManager.Instance.LoadClassData(PlayerType.Warrior);
        
        if (saveData == null)
        {
            Debug.LogWarning("⚠️ [Warrior] 저장 데이터가 없습니다. 기본값 사용");
            return;
        }
        
        // 🔧 HasProperty 대신 Properties.ContainsKey 사용
        if (saveData.Properties.ContainsKey("blockChance"))
        {
            float savedBlockChance = saveData.GetProperty("blockChance");
            if (showDebugLogs)
                Debug.Log($"📁 [Warrior] 저장된 블록 확률: {savedBlockChance} (현재: {GetBlockChance()})");
        }
        
        if (showDebugLogs)
            Debug.Log($"📁 [Warrior] 데이터 로드 완료: {saveData}");
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
