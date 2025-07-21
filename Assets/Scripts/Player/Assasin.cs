using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 어쌔신 클래스 구현체
/// BaseClassBehaviour를 상속받아 어쌔신 고유의 특성과 능력을 제공
/// </summary>
public class Assasin : BaseClassBehaviour
{
    #region IPlayerClass 기본 정보 (오버라이드)
    
    public override string ClassName => "어쌔신";
    public override PlayerType PlayerType => PlayerType.Assasin;
    
    #endregion
    
    #region 어쌔신 능력치 배율 (오버라이드)
    
    [Header("🏹 어쌔신 능력치 배율")]
    [SerializeField] private float attackPowerMultiplier = 1.2f;   // 20% 공격력 증가
    [SerializeField] private float moveSpeedMultiplier = 1.3f;     // 30% 이동속도 증가  
    [SerializeField] private float skillCooldownMultiplier = 0.8f; // 20% 쿨다운 감소
    [SerializeField] private float healthMultiplier = 0.9f;        // 10% 체력 감소 (유리몸)
    
    public override float AttackPowerMultiplier => attackPowerMultiplier;
    public override float MoveSpeedMultiplier => moveSpeedMultiplier;
    public override float SkillCooldownMultiplier => skillCooldownMultiplier;
    public override float HealthMultiplier => healthMultiplier;
    
    #endregion
    
    #region 어쌔신 고유 특성
    
    [Header("🎯 어쌔신 고유 특성")]
    [SerializeField] private float criticalChance = 0.15f;      // 15% 크리티컬 확률
    [SerializeField] private float criticalDamage = 1.5f;       // 크리티컬 데미지 배율
    [SerializeField] private float stealthDuration = 2f;        // 은신 지속시간
    [SerializeField] private float dodgeChance = 0.1f;          // 10% 회피 확률
    [SerializeField] private float backAttackBonus = 1.3f;      // 백어택 보너스 30%
    
    #endregion
    
    #region Unity 생명주기 오버라이드 (디버깅용)
    
    protected override void Start()
    {
        Debug.Log("🏹 [Assasin] Start() 호출됨 - BaseClassBehaviour 상속 확인!");
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
        
        // 어쌔신 전용 스킬들을 자동으로 SkillController에 할당
        var assasinSkill1 = GetComponent<AssasinSkill1>();
        var assasinSkill2 = GetComponent<AssasinSkill2>();
        
        if (assasinSkill1 != null)
        {
            skillController.SkillSet.SetSkill(0, assasinSkill1);
            if (showDebugLogs)
                Debug.Log($"🎯 [Assasin] AssasinSkill1 자동 할당 완료");
        }
        
        if (assasinSkill2 != null)
        {
            skillController.SkillSet.SetSkill(1, assasinSkill2);
            if (showDebugLogs)
                Debug.Log($"🎯 [Assasin] AssasinSkill2 자동 할당 완료");
        }
    }
    
    protected override void ApplyLevelUpBonus(int newLevel)
    {
        if (showDebugLogs)
            Debug.Log($"🆙 [Assasin] 레벨업! Lv.{newLevel} - 어쌔신 보너스 적용");
        
        // 레벨업 시 어쌔신 고유 보너스
        // 예: 레벨마다 크리티컬 확률 0.5% 증가
        criticalChance += 0.005f;
        
        // 5레벨마다 회피 확률 1% 증가
        if (newLevel % 5 == 0)
        {
            dodgeChance += 0.01f;
            if (showDebugLogs)
                Debug.Log($"   - 회피 확률 증가: {dodgeChance * 100:F1}%");
        }
        
        // 10레벨마다 은신 지속시간 0.2초 증가
        if (newLevel % 10 == 0)
        {
            stealthDuration += 0.2f;
            if (showDebugLogs)
                Debug.Log($"   - 은신 지속시간 증가: {stealthDuration}초");
        }
    }
    
    protected override float ApplyAdditionalDamageModifiers(float modifiedDamage, float baseDamage)
    {
        // 크리티컬 판정
        if (Random.Range(0f, 1f) < criticalChance)
        {
            modifiedDamage *= criticalDamage;
            if (showDebugLogs)
                Debug.Log($"💥 [Assasin] 크리티컬 히트! 데미지: {baseDamage} → {modifiedDamage}");
        }
        
        return modifiedDamage;
    }
    
    public override void ApplyPassiveEffects()
    {
        // 어쌔신 패시브 효과들을 여기서 지속적으로 처리
        // 예: 크리티컬 판정, 회피 판정 등
        
        // 실제 구현은 공격/피격 시점에 다른 시스템에서 호출하도록 설계
    }
    
    #endregion
    
    #region 어쌔신 고유 기능
    
    /// <summary>
    /// 어쌔신 고유 스킬 사용 (기존 UseSkill 대체)
    /// </summary>
    public void UseAssassinSkill(int skillSlot = 0)
    {
        if (!isActive || !isInitialized)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [Assasin] 클래스가 비활성화되어 있거나 초기화되지 않았습니다.");
            return;
        }
        
        if (skillController != null)
        {
            // SkillController를 통해 해당 슬롯의 스킬 실행
            var skill = skillController.SkillSet.GetSkill(skillSlot);
            if (skill != null && skill.CanUse())
            {
                // 어쌔신 쿨다운 배율 적용
                skill.Execute();
                
                if (showDebugLogs)
                    Debug.Log($"🏹 [Assasin] 스킬 사용: {skill.SkillName}");
            }
        }
        else
        {
            // Fallback: 기존 방식
            Debug.Log("🏹 어쌔신 스킬 발동!");
        }
    }
    
    /// <summary>
    /// 은신 스킬 (어쌔신 고유)
    /// </summary>
    public void UseStealth()
    {
        if (!isActive) return;
        
        StartCoroutine(StealthCoroutine());
    }
    
    private IEnumerator StealthCoroutine()
    {
        if (showDebugLogs)
            Debug.Log($"👻 [Assasin] 은신 발동! 지속시간: {stealthDuration}초");
        
        // 은신 효과 적용 (투명도, 무적 등)
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
            
            yield return new WaitForSeconds(stealthDuration);
            
            spriteRenderer.color = originalColor;
        }
        
        if (showDebugLogs)
            Debug.Log($"👻 [Assasin] 은신 해제");
    }
    
    /// <summary>
    /// 백어택 보너스 판정
    /// </summary>
    public float GetBackAttackMultiplier(Vector3 playerPos, Vector3 enemyPos, Vector3 enemyFacing)
    {
        Vector3 toEnemy = (enemyPos - playerPos).normalized;
        float dot = Vector3.Dot(toEnemy, enemyFacing);
        
        // 적의 뒤쪽에서 공격하는 경우 (dot > 0.5)
        if (dot > 0.5f)
        {
            if (showDebugLogs)
                Debug.Log($"🗡️ [Assasin] 백어택 성공! 보너스: {backAttackBonus}x");
            return backAttackBonus;
        }
        
        return 1f;
    }
    
    #endregion
    
    #region 공개 유틸리티 메서드
    
    /// <summary>
    /// 외부에서 크리티컬 확률 조회
    /// </summary>
    public float GetCriticalChance() => criticalChance;
    
    /// <summary>
    /// 외부에서 회피 확률 조회
    /// </summary>
    public float GetDodgeChance() => dodgeChance;
    
    /// <summary>
    /// 현재 어쌔신 상태 정보 출력 (BaseClass 확장)
    /// </summary>
    public override void PrintStatus()
    {
        base.PrintStatus(); // 기본 정보 출력
        
        Debug.Log($"🏹 [Assasin] 고유 특성:");
        Debug.Log($"   - 크리티컬 확률: {criticalChance * 100:F1}%");
        Debug.Log($"   - 크리티컬 데미지: {criticalDamage}x");
        Debug.Log($"   - 회피 확률: {dodgeChance * 100:F1}%");
        Debug.Log($"   - 은신 지속시간: {stealthDuration}초");
        Debug.Log($"   - 백어택 보너스: {backAttackBonus}x");
    }
    
    #endregion
    
    #region ⭐ [Phase C] 저장/로드 시스템
    
    /// <summary>
    /// 현재 Assasin 데이터를 저장
    /// </summary>
    public void SaveAssasinData()
    {
        // ⭐ 게임 종료 중일 때는 저장하지 않음 (SaveManager가 파괴될 수 있음)
        if (SaveManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.Log("ℹ️ [Assasin] SaveManager 없음. 게임 종료 중이므로 저장 생략.");
            return;
        }
        
        var saveData = new BaseClassSaveData();
        saveData.classType = PlayerType.Assasin;
        saveData.classLevel = playerLevel?.CurrentLevel ?? 1;
        saveData.isUnlocked = true;
        saveData.wasActiveLastTime = IsActiveClass;
        
        // Assasin 특성 데이터 저장
        saveData.SetProperty("criticalChance", criticalChance);
        saveData.SetProperty("criticalDamage", criticalDamage);
        saveData.SetProperty("dodgeChance", dodgeChance);
        saveData.SetProperty("stealthDuration", stealthDuration);
        saveData.SetProperty("backAttackBonus", backAttackBonus);
        
        // 캐릭터 인덱스는 현재 기본값 0 사용 (추후 확장 가능)
        int characterIndex = 0;
        SaveManager.Instance.SaveClassData(characterIndex, PlayerType.Assasin, saveData);
        
        if (showDebugLogs)
            Debug.Log($"💾 [Assasin] 데이터 저장 완료: {saveData}");
    }
    
    /// <summary>
    /// 저장된 Assasin 데이터를 불러와서 적용
    /// </summary>
    public void LoadAssasinData()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("💥 [Assasin] SaveManager.Instance가 null입니다!");
            return;
        }
        
        // 캐릭터 인덱스는 현재 기본값 0 사용 (추후 확장 가능)
        int characterIndex = 0;
        var saveData = SaveManager.Instance.LoadClassData(characterIndex, PlayerType.Assasin);
        
        if (saveData == null)
        {
            Debug.LogWarning("⚠️ [Assasin] 저장 데이터가 없습니다. 기본값 사용");
            return;
        }
        
        // Assasin 특성 데이터 복원
        criticalChance = saveData.GetProperty("criticalChance", 0.15f);
        criticalDamage = saveData.GetProperty("criticalDamage", 2.0f);
        dodgeChance = saveData.GetProperty("dodgeChance", 0.05f);
        stealthDuration = saveData.GetProperty("stealthDuration", 2.0f);
        backAttackBonus = saveData.GetProperty("backAttackBonus", 1.5f);
        
        // 활성화 상태 복원
        if (saveData.wasActiveLastTime)
        {
            SetActive(true);
        }
        
        if (showDebugLogs)
            Debug.Log($"📁 [Assasin] 데이터 불러오기 완료: {saveData}");
    }
    
    /// <summary>
    /// 레벨업이나 특성 변경 시 즉시 저장 (공개 메서드)
    /// </summary>
    public void SaveDataNow()
    {
        SaveAssasinData();
    }
    
    /// <summary>
    /// 게임 시작 시 자동 로드 (BaseClassBehaviour.Awake 후 실행)
    /// </summary>
    protected override void Awake()
    {
        base.Awake(); // 부모 클래스 Awake 먼저 실행
        
        // 자동으로 저장된 데이터 불러오기
        Invoke(nameof(LoadAssasinData), 0.2f); // Warrior보다 조금 늦게 로드
    }
    
    /// <summary>
    /// 게임 종료 시 자동 저장
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveAssasinData();
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveAssasinData();
    }
    
    protected override void OnDestroy()
    {
        // ⭐ OnDestroy에서는 저장하지 않음 (SaveManager가 이미 파괴될 수 있음)
        // 대신 OnApplicationPause, OnApplicationFocus에서 저장
        base.OnDestroy(); // 부모 클래스 OnDestroy 호출
    }
    
    #endregion
}
