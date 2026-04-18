using UnityEngine;
using System.Collections;

/// <summary>
/// 제네릭 기반 액티브 스킬 기본 클래스
/// 각 직업별 ActiveSkillData 타입을 강제하여 타입 안전성 보장 (SRP 준수)
/// 모든 스킬의 공통 로직을 Template Method Pattern으로 구현
/// Phase 1: ActiveSkillData 기반으로 리팩토링됨
/// </summary>
public abstract class BaseSkill<T> : MonoBehaviour, ISkill where T : ActiveSkillData
{
    [Header("📊 스킬 데이터")]
    [SerializeField] protected T skillData;
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = true;
    
    #region ISkill 인터페이스 구현
    
    /// <summary>
    /// 스킬 이름 (SkillData에서 가져오거나 클래스명 사용)
    /// </summary>
    public virtual string SkillName => skillData != null ? skillData.skillName : GetType().Name;
    
    /// <summary>
    /// 스킬 쿨다운 시간 (클래스별 배율 적용 가능)
    /// Phase 1: ActiveSkillData의 baseCooldown 사용
    /// </summary>
    public virtual float Cooldown 
    { 
        get 
        {
            float baseCooldown = skillData != null ? skillData.baseCooldown : 2f;
            return ApplyCooldownMultiplier(baseCooldown);
        }
    }
    
    /// <summary>
    /// 스킬 사용 가능 여부 확인
    /// </summary>
    public virtual bool CanUse()
    {
        bool cooldownReady = GetCooldownRemaining() <= 0f;
        bool additionalConditions = CheckAdditionalConditions();
        
        bool canUse = cooldownReady && additionalConditions;
        
        
        
        return canUse;
    }
    
    /// <summary>
    /// 스킬 실행 메인 메서드 (Template Method Pattern)
    /// </summary>
    public virtual void Execute()
    {
        
        if (!CanUse())
        {
            Debug.LogWarning($"🟡 [BaseSkill] {SkillName} 사용 불가능!");
            return;
        }
        
        
        // 쿨다운 시작
        StartCooldown();
        
        // 템플릿 메서드 패턴: 각 스킬별 구현 호출
        OnExecuteSkill();
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 실제 스킬 효과 실행
    /// </summary>
    public abstract void OnAnimationEvent();
    
    /// <summary>
    /// 스킬 쿨다운 남은 시간
    /// </summary>
    public virtual float GetCooldownRemaining()
    {
        return Mathf.Max(0f, (lastSkillTime + Cooldown) - Time.time);
    }
    
    #endregion
    
    #region 보호된 프로퍼티 (자식 클래스에서 사용)
    
    /// <summary>
    /// 스킬 데이터 접근자 (타입 안전성 보장)
    /// </summary>
    protected T SkillData => skillData;
    
    /// <summary>
    /// 스킬 데이터 유효성 검사
    /// </summary>
    protected bool IsSkillDataValid => skillData != null;
    
    /// <summary>
    /// ⭐ 추가: 현재 쿨다운 상태 확인
    /// </summary>
    public bool IsOnCooldown => GetCooldownRemaining() > 0f;
    
    /// <summary>
    /// 스킬 기본 데미지 배율 (SkillData에서 가져옴)
    /// Phase 1: baseDamageMultiplier 사용 (150% = 1.5배)
    /// </summary>
    protected virtual float BaseDamage => skillData?.baseDamageMultiplier ?? 100f;
    
    /// <summary>
    /// 스킬 기본 사거리 (SkillData에서 가져옴)
    /// </summary>
    protected virtual float BaseRange => skillData?.range ?? 5f;
    
    #endregion
    
    #region 내부 상태 및 컴포넌트 참조
    
    // 쿨다운 관리
    protected float lastSkillTime = -Mathf.Infinity;
    
    // 공통 컴포넌트 참조
    protected PlayerAnimationController animationController;
    protected ActiveWeapon activeWeapon;
    protected Transform firePoint;
    
    // 이벤트
    public System.Action<float> OnSkillCooldownChanged;
    public System.Action<string> OnSkillExecuted;
    
    #endregion
    
    #region 쿨다운 관리 메서드
    
    /// <summary>
    /// ⭐ 추가: 쿨다운 시작
    /// </summary>
    protected virtual void StartCooldown()
    {
        lastSkillTime = Time.time;
        
        
        // 쿨다운 변경 이벤트 발생
        OnSkillCooldownChanged?.Invoke(Cooldown);
    }
    
    #endregion
    
    #region 추상 메서드 (자식 클래스에서 구현 필수)
    
    /// <summary>
    /// 실제 스킬 로직 구현 (자식 클래스에서 구현)
    /// </summary>
    protected abstract void OnExecuteSkill();
    
    #endregion
    
    #region 가상 메서드 (자식 클래스에서 오버라이드 가능)
    
    /// <summary>
    /// 스킬 실행 전 처리 (애니메이션 트리거 등)
    /// </summary>
    protected virtual void OnPreExecute()
    {
        // 애니메이션 트리거 (하위 클래스에서 구체적으로 구현)
        TriggerSkillAnimation();
    }
    
    /// <summary>
    /// 스킬 실행 후 처리 (사운드, 이벤트 등)
    /// </summary>
    protected virtual void OnPostExecute()
    {
        // 스킬 실행 이벤트 발생
        OnSkillExecuted?.Invoke(SkillName);
    }
    
    /// <summary>
    /// 추가 사용 조건 검사 (마나, 특수 조건 등)
    /// </summary>
    protected virtual bool CheckAdditionalConditions()
    {
        return true; // 기본적으로 추가 조건 없음
    }
    
    /// <summary>
    /// 쿨다운 배율 적용 (클래스별 특성 반영)
    /// </summary>
    protected virtual float ApplyCooldownMultiplier(float baseCooldown)
    {
        // BaseClassBehaviour에서 쿨다운 배율 가져오기
        var classComponent = GetComponent<BaseClassBehaviour>();
        if (classComponent != null)
        {
            return baseCooldown * classComponent.SkillCooldownMultiplier;
        }
        return baseCooldown;
    }
    
    /// <summary>
    /// 스킬 애니메이션 트리거 (하위 클래스에서 구체화)
    /// </summary>
    protected virtual void TriggerSkillAnimation()
    {
        // 기본 구현은 비워둠 (하위 클래스에서 구현)
    }
    
    /// <summary>
    /// 공통 이펙트 생성
    /// </summary>
    protected virtual void SpawnEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
    {
        if (effectPrefab == null) return;
        
        // GamePoolManager를 통한 이펙트 생성
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.SpawnFromPool(effectPrefab.name, position, rotation);
        }
        else
        {
            // Fallback: 직접 생성
            Instantiate(effectPrefab, position, rotation);
        }
    }
    
    /// <summary>
    /// 스킬 데이터 유효성 검증
    /// </summary>
    protected virtual bool ValidateSkillData()
    {
        bool isValid = skillData != null;
        
        if (!isValid && showDebugLogs)
        {
            Debug.LogWarning($"🟡 [{GetType().Name}] SkillData가 할당되지 않았습니다!");
        }
        
        return isValid;
    }
    
    #endregion
    
    #region Unity 라이프사이클
    
    protected virtual void Awake()
    {
        InitializeComponents();
        
            Dbg.Log($"🟢 [{GetType().Name}] 컴포넌트 초기화 완료");
    }
    
    protected virtual void Start()
    {
        // ⭐ Phase 4: SkillData가 없어도 경고만 출력 (새 시스템에서는 사용 안 함)
        if (!ValidateSkillData())
        {
            return; // 초기화 중단
        }
        
        // 초기화 완료 로그
        if (showDebugLogs && IsSkillDataValid)
        {
            Dbg.Log($"🎯 [{GetType().Name}] 스킬 '{SkillName}' 초기화 완료 " +
                     $"(쿨다운: {Cooldown:F1}초, 데미지: {BaseDamage:F0})");
        }
    }
    
    /// <summary>
    /// 필요한 컴포넌트들 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // PlayerAnimationController 찾기
        animationController = GetComponent<PlayerAnimationController>();
        if (animationController == null)
            animationController = GetComponentInParent<PlayerAnimationController>();
            
        // ActiveWeapon 찾기
        activeWeapon = FindObjectOfType<ActiveWeapon>();
        
        // FirePoint 찾기 (무기가 있는 경우)
        UpdateFirePoint();
    }
    
    /// <summary>
    /// 발사 지점 업데이트 (무기 변경 시 호출 가능)
    /// </summary>
    protected virtual void UpdateFirePoint()
    {
        if (activeWeapon?.CurrentActiveWeapon != null)
        {
            firePoint = FindFirePoint(activeWeapon.CurrentActiveWeapon.transform);
        }
    }
    
    /// <summary>
    /// 발사 지점 찾기 (범용)
    /// </summary>
    private Transform FindFirePoint(Transform weaponTransform)
    {
        if (weaponTransform == null) return null;
        
        // 표준 발사 지점 이름들
        string[] firePointNames = { 
            "Arrow Spawn Point", "Fire Point", "FirePoint", "Spawn Point", "SpawnPoint",
            "Projectile Spawn", "Attack Point", "AttackPoint", "Muzzle", "Tip", "End Point"
        };
        
        foreach (string firePointName in firePointNames)
        {
            Transform point = weaponTransform.Find(firePointName);
            if (point != null) return point;
        }
        
        // 키워드 포함된 자식 Transform 찾기
        for (int i = 0; i < weaponTransform.childCount; i++)
        {
            Transform child = weaponTransform.GetChild(i);
            string childName = child.name.ToLower();
            if (childName.Contains("point") || childName.Contains("spawn") || 
                childName.Contains("fire") || childName.Contains("tip") || 
                childName.Contains("muzzle") || childName.Contains("end"))
                return child;
        }
        
        // 못 찾으면 무기 Transform 자체 사용
        return weaponTransform;
    }
    
    #endregion
}