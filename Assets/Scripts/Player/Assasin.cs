using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection; // 🆕 Reflection 사용을 위해 추가

/// <summary>
/// 어쌔신 클래스 구현체
/// BaseClassBehaviour를 상속받아 어쌔신 고유의 특성과 능력을 제공
/// </summary>
public class Assasin : BaseClassBehaviour
{
    #region IPlayerClass 기본 정보 (오버라이드)
    
    public override string ClassName => assasinData?.className ?? "어쌔신";
    public override PlayerType PlayerType => assasinData?.playerType ?? PlayerType.Assasin;
    
    #endregion
    
    #region 📊 ScriptableObject 데이터 연동
    
    [Header("📊 어쌔신 데이터 연동")]
    [SerializeField] private AssasinData assasinData; // ScriptableObject 참조
    
    // ScriptableObject에서 값 가져오기 (null 안전성 포함)
    public override float AttackPowerMultiplier => assasinData?.AttackPowerMultiplier ?? 1.2f;
    public override float MoveSpeedMultiplier => assasinData?.MoveSpeedMultiplier ?? 1.3f;
    public override float SkillCooldownMultiplier => assasinData?.SkillCooldownMultiplier ?? 0.8f;
    public override float HealthMultiplier => assasinData?.HealthMultiplier ?? 0.9f;
    
    #endregion
    
    #region 🎯 어쌔신 고유 특성 (Phase 1: 임시 호환 속성)
    
    // ⚠️ Phase 1 임시 호환: 기존 코드와의 호환성 유지
    // Phase 2에서 완전히 PassiveSkillData로 전환 예정
    
    public float StealthDuration => assasinData?.assasinStealthDuration ?? 2f;
    public float DodgeChance => assasinData?.assasinDodgeChance ?? 0.15f;
    public float BackAttackBonus => assasinData?.assasinBackAttackBonus ?? 1.5f;
    public float CriticalChanceBonus => 0.1f;
    
    // 🎮 런타임 상태 변수들 (ScriptableObject와 무관)
    private bool isStealthActive = false;  // 은신 상태 플래그
    
    #endregion
    
    #region 🆕 BaseClassBehaviour 추상 메서드 구현 (ScriptableObject 기본값)
    
    public override float GetBaseMoveSpeed()
    {
        return assasinData?.baseMoveSpeed ?? 4f; // AssasinData에서 가져오거나 기본값 4
    }

    public override float GetBaseMaxHealth()
    {
        return assasinData?.baseMaxHealth ?? 100f; // AssasinData에서 가져오거나 기본값 100
    }
    
    // ⭐ 신규 추가: Knockback 관련 메서드들
    public override float GetBaseKnockbackThrust()
    {
        return assasinData?.baseKnockbackThrust ?? 10f; // AssasinData에서 가져오거나 기본값 10
    }
    
    public override float GetBaseKnockbackTime()
    {
        return assasinData?.knockbackTime ?? 0.2f; // AssasinData에서 가져오거나 기본값 0.2초
    }
    
    // ⭐ 신규 추가: Flash 관련 메서드
    public override float GetBaseFlashDuration()
    {
        return assasinData?.flashDuration ?? 0.1f; // AssasinData에서 가져오거나 기본값 0.1초
    }
    
    // 🆕 기본 전투 스탯 메서드 추가
    public override float GetBaseAttackDamage()
    {
        return assasinData?.baseAttackDamage ?? 10f; // AssasinData에서 가져오거나 기본값 10
    }
    
    public override float GetBaseDefense()
    {
        return assasinData?.baseDefense ?? 0f; // AssasinData에서 가져오거나 기본값 0
    }
    
    #endregion

    #region Unity 생명주기 오버라이드 (디버깅용)
    
    protected override void Start()
    {
        
        if (assasinData == null)
            Debug.LogError("❌ [Assasin] AssasinData가 null입니다!");
        
        // 🎯 현재 오버라이드 값 확인
        
        base.Start(); // BaseClassBehaviour.Start() 호출 - 자동 적용
        
        // 🗑️ ForceApplyAssasinData() 제거됨 - BaseClassBehaviour가 자동 처리
        
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
        }
        
        if (assasinSkill2 != null)  // ← 주석 해제
        {
            skillController.SkillSet.SetSkill(1, assasinSkill2);
        }
    }
    
    protected override void ApplyLevelUpBonus(int newLevel)
    {
            Dbg.Log($"🆙 [Assasin] 레벨업! Lv.{newLevel} - 어쌔신 보너스 적용");
        
        // 레벨업 시 어쌔신 고유 보너스
        // 예: 레벨마다 크리티컬 확률 0.5% 증가
        // criticalChance += 0.005f; // 이제 ScriptableObject에서 관리
        
        // 5레벨마다 회피 확률 1% 증가
        if (newLevel % 5 == 0)
        {
            // dodgeChance += 0.01f; // 이제 ScriptableObject에서 관리
        }
        
        // 10레벨마다 은신 지속시간 0.2초 증가
        if (newLevel % 10 == 0)
        {
            // stealthDuration += 0.2f; // 이제 ScriptableObject에서 관리
        }
    }
    
    protected override float ApplyAdditionalDamageModifiers(float modifiedDamage, float baseDamage)
    {
        // 🗑️ 크리티컬 계산 제거됨: 이제 EquipmentData에서 관리
        // 향후 장비 시스템과 연동하여 재구현 예정
        
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
                
            }
        }
        else
        {
            // Fallback: 기존 방식
        }
    }
    
    /// <summary>
    /// 은신 스킬 (어쌔신 고유) - 개선된 버전
    /// </summary>
    public void UseStealth()
    {
        if (!isActive) return;
        
        StartCoroutine(StealthCoroutine());
    }
    
    private IEnumerator StealthCoroutine()
    {
        isStealthActive = true; // 🆕 은신 상태 시작
        
        
        // 🆕 실제 은신 효과 적용
        var spriteRenderer = GetComponent<SpriteRenderer>();
        var collider = GetComponent<Collider2D>();
        
        Color originalColor = Color.white;
        int originalLayer = gameObject.layer;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            // 반투명 효과
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.3f);
        }
        
        // 🆕 적 AI에서 감지되지 않도록 레이어 변경 (옵션)
        try
        {
            int stealthLayer = LayerMask.NameToLayer("StealthPlayer");
            if (stealthLayer != -1)
            {
                gameObject.layer = stealthLayer;
            }
        }
        catch (System.Exception) { }
        
        // 🆕 은신 이펙트 생성
        if (GamePoolManager.Instance != null)
        {
            var stealthEffect = GamePoolManager.Instance.SpawnFromPool("Stealth Effect", transform.position, Quaternion.identity);
            if (stealthEffect != null)
            {
                // 은신 이펙트를 플레이어에 부착
                stealthEffect.transform.SetParent(transform);
                stealthEffect.transform.localPosition = Vector3.zero;
            }
        }
        
        yield return new WaitForSeconds(StealthDuration); // 이제 ScriptableObject에서 관리
        
        // 원상복구
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        gameObject.layer = originalLayer;
        
        isStealthActive = false; // 🆕 은신 상태 종료
        
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
            return BackAttackBonus; // 이제 ScriptableObject에서 관리
        }
        
        return 1f;
    }
    
    #endregion
    
    #region 공개 유틸리티 메서드
    
    /// <summary>
    /// 외부에서 회피 확률 조회
    /// </summary>
    public float GetDodgeChance() => DodgeChance; // 이제 ScriptableObject에서 관리
    
    /// <summary>
    /// 현재 어쌔신 상태 정보 출력 (BaseClass 확장)
    /// </summary>
    public override void PrintStatus()
    {
        base.PrintStatus(); // 기본 정보 출력
        
    }
    
    #endregion
    
    #region ⭐ [Phase C] 저장/로드 시스템
    
    /// <summary>
    /// 저장할 Assasin 데이터를 SelectedPlayerData에 저장
    /// </summary>
    public void SaveAssasinData()
    {
        // ⭐ 새로운 구조: SelectedPlayerData 사용
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            return;
        }
        
        var selectedData = PlayerDataManager.Instance.selectedPlayerData;
        if (selectedData == null) return;
        
        // Assasin 특성 데이터 저장
        selectedData.SetRuntimeStat("dodgeChance", DodgeChance);
        selectedData.SetRuntimeStat("stealthDuration", StealthDuration);
        selectedData.SetRuntimeStat("backAttackBonus", BackAttackBonus);
        selectedData.classLevel = playerLevel?.CurrentLevel ?? 1;
        
        // 슬롯에 저장
        PlayerDataManager.Instance.SaveCurrentSlot();
        
    }
    
    /// <summary>
    /// 저장된 Assasin 데이터를 불러와서 적용
    /// </summary>
    public void LoadAssasinData()
    {
        // ⭐ 새로운 구조: SelectedPlayerData 사용
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.LogWarning("⚠️ [Assasin] PlayerDataManager 없음 또는 슬롯 미선택!");
            return;
        }
        
        var selectedData = PlayerDataManager.Instance.selectedPlayerData;
        if (selectedData == null) return;
        
        try
        {
            // Assasin 특성 로드
            if (selectedData.RuntimeExtraStats.ContainsKey("dodgeChance"))
            {
                float savedDodgeChance = selectedData.GetRuntimeStat("dodgeChance");
            }
            
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 [Assasin] 데이터 로드 중 에러: {ex.Message}");
        }
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

    /// <summary>
    /// 크리티컬 판정 포함 데미지 계산 (BaseClassBehaviour 오버라이드 확장) - ⭐ 최우선 추가
    /// </summary>
    public override float GetModifiedDamage(float baseDamage)
    {
        float modifiedDamage = base.GetModifiedDamage(baseDamage); // 기본 배율 적용
        
        // 추가 크리티컬 로직은 ApplyAdditionalDamageModifiers에서 처리됨
        return modifiedDamage;
    }

    /// <summary>
    /// 현재 은신 상태 확인 - ⭐ 최우선 추가
    /// </summary>
    public bool IsInStealth()
    {
        return isStealthActive;
    }

    /// <summary>
    /// Inspector에서 AssasinData 변경 시 실시간 능력치 재적용 (완전 안전 버전)
    /// </summary>
    void OnValidate()
    {
        // 🛡️ 모든 안전성 검사
        if (!Application.isPlaying) return;
        if (assasinData == null) return;
        if (!gameObject.activeInHierarchy) return;
        if (!enabled) return;
        if (playerController == null) return;
        
        try
        {
            ApplyClassStats();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"🟡 [Assasin] 능력치 재적용 중 오류 (무시됨): {e.Message}");
        }
    }

    #region 🗺️ 아이소메트릭 데이터 활용 (신규 추가)
    
    /// <summary>
    /// ScriptableObject에서 아이소메트릭 데이터 가져오기
    /// </summary>
    public override IsometricCharacterData GetIsometricData()
    {
        return assasinData?.IsometricData ?? CreateDefaultIsometricData();
    }
    
    /// <summary>
    /// 발 위치 오프셋 가져오기
    /// </summary>
    public override Vector2 GetFootOffset()
    {
        var isometricData = GetIsometricData();
        return isometricData.FootOffset;
    }
    
    /// <summary>
    /// 방향 프리셋 가져오기
    /// </summary>
    public override DirectionPreset GetDirectionPreset()
    {
        var isometricData = GetIsometricData();
        return isometricData.DirectionPreset;
    }
    
    /// <summary>
    /// 높이 오프셋 계산 (점프, 스킬 등에 사용)
    /// </summary>
    /// <param name="t">높이 곡선 시간 (0~1)</param>
    /// <returns>계산된 높이 오프셋</returns>
    public override int CalculateHeightOffset(float t)
    {
        var isometricData = GetIsometricData();
        int heightOffset = isometricData.CalculateHeightOffset(t);
        
        return heightOffset;
    }
    
    /// <summary>
    /// 월드 좌표 기준 발 위치 계산
    /// </summary>
    /// <param name="centerPosition">캐릭터 중심 위치</param>
    /// <returns>발 위치 월드 좌표</returns>
    public Vector3 GetFootWorldPosition(Vector3 centerPosition)
    {
        var isometricData = GetIsometricData();
        return isometricData.GetFootWorldPosition(centerPosition);
    }
    
    /// <summary>
    /// 아이소메트릭 데이터 유효성 검증 및 로그 출력
    /// </summary>
    private void ValidateIsometricData()
    {
    }
    
    /// <summary>
    /// 기본 아이소메트릭 데이터 생성 (Fallback)
    /// </summary>
    private IsometricCharacterData CreateDefaultIsometricData()
    {
        var defaultData = new IsometricCharacterData();
        defaultData.SetDefaults();
        
            
        return defaultData;
    }
    
    #endregion
}
