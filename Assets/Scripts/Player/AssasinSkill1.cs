using UnityEngine;
using System.Collections;

/// <summary>
/// Assasin 스킬1: Multi-Arrow (5개 화살 부채꼴 발사)
/// ISkill 인터페이스를 구현하여 모듈식 스킬 시스템에 통합
/// ScriptableObject 기반 데이터 분리 적용
/// </summary>
public class AssasinSkill1 : MonoBehaviour, ISkill
{
    [Header("스킬 데이터 (ScriptableObject)")]
    public SkillData skillData; // Inspector에서 할당
    
    [Header("디버그")]
    public bool showDebugLogs = true;
    
    // ISkill 인터페이스 구현
    public string SkillName => skillData != null ? skillData.skillName : "Multi Arrow";
    public float Cooldown => skillData != null ? skillData.cooldown : 2f;
    
    // 내부 상태
    private float lastSkillTime = -Mathf.Infinity;
    private Transform bowTransform;
    private Transform firePoint;
    private PlayerAnimationController animationController;
    
    // 이벤트
    public System.Action<float> OnSkillCooldownChanged;
    
    void Awake()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [AssasinSkill1] Awake() 시작");
            
        // 컴포넌트 참조 초기화
        animationController = GetComponent<PlayerAnimationController>();
        if (animationController == null)
            animationController = GetComponentInParent<PlayerAnimationController>();
            
        if (animationController == null)
        {
            Debug.LogWarning("🟡 [AssasinSkill1] PlayerAnimationController를 찾을 수 없습니다!");
        }
        else if (showDebugLogs)
        {
            Debug.Log("🟢 [AssasinSkill1] PlayerAnimationController 찾음");
        }
    }
    
    void Start()
    {
        // ⭐ SkillData 유효성 검사 강화
        if (skillData == null)
        {
            Debug.LogError("🔴 [AssasinSkill1] SkillData가 할당되지 않았습니다! Inspector에서 AssasinSkill1Data를 할당하세요!");
            return;
        }
        
        Debug.Log($"🟢 [AssasinSkill1] SkillData 로드 성공:");
        Debug.Log($"   - 스킬명: {skillData.skillName}");
        Debug.Log($"   - 쿨다운: {skillData.cooldown}초");
        Debug.Log($"   - 프리팹: {skillData.projectilePrefab?.name ?? "None"}");
        Debug.Log($"   - 속도: {skillData.projectileSpeed}");
        Debug.Log($"   - 개수: {skillData.projectileCount}");
        Debug.Log($"   - 각도: {skillData.spreadAngle}도");
        Debug.Log($"   - 크기: {skillData.projectileScale}");
        
        if (showDebugLogs)
            Debug.Log("🟢 [AssasinSkill1] 초기화 완료!");
    }
    
    void Update()
    {
        // Bow가 동적으로 바뀔 수 있으므로 주기적으로 체크
        if (bowTransform == null || firePoint == null)
        {
            UpdateBowReference();
        }
    }
    
    /// <summary>
    /// ⭐ 범용 무기 참조 업데이트 (SkillController와 일관성 유지)
    /// </summary>
    private void UpdateBowReference()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
        {
            bowTransform = activeWeapon.CurrentActiveWeapon.transform;
            firePoint = FindGenericFirePoint(bowTransform);
            
            if (showDebugLogs && firePoint != null)
                Debug.Log($"🟢 [AssasinSkill1] 무기 '{bowTransform.name}'의 발사 지점 '{firePoint.name}' 찾음");
        }
    }

    /// <summary>
    /// ⭐ 범용 발사 지점 찾기 (SkillController와 동일한 로직)
    /// </summary>
    private Transform FindGenericFirePoint(Transform weaponTransform)
    {
        if (weaponTransform == null) return null;
        
        // 우선순위 순으로 발사 지점 찾기
        string[] firePointNames = { 
            "Arrow Spawn Point", "Fire Point", "FirePoint", "Spawn Point", "SpawnPoint",
            "Projectile Spawn", "Attack Point", "AttackPoint", "Muzzle", "Tip", "End Point"
        };
        
        foreach (string firePointName in firePointNames)
        {
            Transform firePoint = weaponTransform.Find(firePointName);
            if (firePoint != null)
                return firePoint;
        }
        
        // 자식 Transform 중에서 키워드 포함된 것 찾기
        for (int i = 0; i < weaponTransform.childCount; i++)
        {
            Transform child = weaponTransform.GetChild(i);
            string childName = child.name.ToLower();
            if (childName.Contains("point") || childName.Contains("spawn") || childName.Contains("fire") ||
                childName.Contains("tip") || childName.Contains("muzzle") || childName.Contains("end"))
                return child;
        }
        
        // 못 찾으면 무기 Transform 자체 사용
        return weaponTransform;
    }
    
    #region ISkill 인터페이스 구현
    
    public bool CanUse()
    {
        float cooldown = skillData != null ? skillData.cooldown : 2f;
        bool canUse = Time.time >= lastSkillTime + cooldown;
        
        if (showDebugLogs && !canUse)
        {
            Debug.Log($"🟡 [AssasinSkill1] CanUse = false, 남은 쿨다운: {GetCooldownRemaining():F1}초");
        }
        
        return canUse;
    }
    
    public void Execute()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [AssasinSkill1] Execute() 호출됨");
            
        if (!CanUse())
        {
            Debug.LogWarning($"🟡 [AssasinSkill1] 스킬1 쿨다운 중! 남은 시간: {GetCooldownRemaining():F1}초");
            return;
        }
        
        // 쿨다운 시작
        lastSkillTime = Time.time;
        
        // 애니메이션 트리거 (PlayerAnimationController를 통해)
        if (animationController != null)
        {
            bool success = animationController.TriggerSkill1();
            if (success)
            {
                if (OnSkillCooldownChanged != null)
                    OnSkillCooldownChanged(Cooldown);
                    
                Debug.Log($"🟢 [AssasinSkill1] 스킬1(Multi-Arrow) 애니메이션 트리거 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [AssasinSkill1] 애니메이션 트리거 실패 - 직접 실행");
                OnAnimationEvent();
            }
        }
        else
        {
            // PlayerAnimationController가 없으면 직접 실행
            Debug.LogWarning("🟡 [AssasinSkill1] PlayerAnimationController가 없음 - 직접 실행");
            OnAnimationEvent();
        }
    }
    
    public void OnAnimationEvent()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [AssasinSkill1] OnAnimationEvent() 호출됨 - 실제 화살 발사");
            
        if (bowTransform == null || firePoint == null)
        {
            Debug.LogWarning("[AssasinSkill1] Bow 또는 FirePoint가 없습니다!");
            UpdateBowReference(); // 다시 한 번 시도
            if (bowTransform == null || firePoint == null)
                return;
        }
        
        if (GamePoolManager.Instance == null)
        {
            Debug.LogError("[AssasinSkill1] GamePoolManager.Instance가 null입니다!");
            return;
        }
        
        // SkillData에서 값 읽어오기 (fallback 포함)
        GameObject prefab = skillData?.projectilePrefab;
        float speed = skillData?.projectileSpeed ?? 10f;
        int count = skillData?.projectileCount ?? 5;
        float spread = skillData?.spreadAngle ?? 15f;
        Vector3 scale = skillData?.projectileScale ?? Vector3.one;
        
        // ⭐ 중요: SkillData의 프리팹이 없으면 기본값 사용
        string poolTag = "Arrow"; // 기본값
        if (prefab != null)
        {
            poolTag = prefab.name; // SkillData의 프리팹 이름 사용
            if (showDebugLogs)
                Debug.Log($"🔵 [AssasinSkill1] SkillData 프리팹 사용: {poolTag}");
        }
        else
        {
            Debug.LogWarning("🟡 [AssasinSkill1] SkillData에 프리팹이 없어서 기본 'Arrow' 사용");
        }
        
        Vector2 baseDir = bowTransform.right;
        float startAngle = -spread * (count - 1) / 2f;
        
        int successCount = 0;
        
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + spread * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
            
            GameObject arrow = GamePoolManager.Instance.SpawnFromPool(poolTag, firePoint.position, Quaternion.identity);
            
            if (arrow != null)
            {
                // SkillData 기반 설정
                arrow.transform.localScale = scale;
                arrow.transform.right = dir;
                
                // Projectile 컴포넌트로 속도 설정
                if (arrow.TryGetComponent(out Projectile projectile))
                {
                    projectile.UpdateMoveSpeed(speed);
                }
                else if (arrow.TryGetComponent(out Rigidbody2D rb))
                {
                    rb.velocity = dir.normalized * speed;
                }
                
                successCount++;
                
                if (showDebugLogs)
                    Debug.Log($"[AssasinSkill1] 화살 {i+1} 생성 성공: {arrow.name}");
            }
            else
            {
                Debug.LogError($"[AssasinSkill1] GamePoolManager에서 Arrow {i+1} 생성 실패!");
            }
        }
        
        Debug.Log($"🟢 [AssasinSkill1] Multi-Arrow 발사 완료! ({successCount}/{count}개 성공, 속도: {speed}, 크기: {scale})");
    }
    
    public float GetCooldownRemaining()
    {
        float cooldown = skillData != null ? skillData.cooldown : 2f;
        float elapsed = Time.time - lastSkillTime;
        return Mathf.Clamp(cooldown - elapsed, 0, cooldown);
    }
    
    #endregion
    
    /// <summary>
    /// 외부에서 쿨다운 시간 업데이트 (SkillController 등에서 호출)
    /// </summary>
    public void UpdateCooldown(float newCooldown)
    {
        if (skillData != null)
        {
            skillData.cooldown = newCooldown;
            if (showDebugLogs)
                Debug.Log($"🔵 [AssasinSkill1] SkillData 쿨다운 업데이트: {skillData.cooldown}초");
        }
    }
    
    /// <summary>
    /// 현재 스킬 상태 로깅 (디버깅용)
    /// </summary>
    public void LogCurrentState()
    {
        Debug.Log($"🔍 [AssasinSkill1] 현재 상태:");
        Debug.Log($"   - SkillName: {SkillName}");
        Debug.Log($"   - Cooldown: {Cooldown}초");
        Debug.Log($"   - CanUse: {CanUse()}");
        Debug.Log($"   - CooldownRemaining: {GetCooldownRemaining():F1}초");
        Debug.Log($"   - BowTransform: {(bowTransform != null ? "있음" : "없음")}");
        Debug.Log($"   - FirePoint: {(firePoint != null ? "있음" : "없음")}");
        Debug.Log($"   - AnimationController: {(animationController != null ? "있음" : "없음")}");
        Debug.Log($"   - SkillData: {(skillData != null ? skillData.name : "없음")}");
        
        if (skillData != null)
        {
            Debug.Log($"   - ProjectileCount: {skillData.projectileCount}");
            Debug.Log($"   - ProjectileSpeed: {skillData.projectileSpeed}");
            Debug.Log($"   - SpreadAngle: {skillData.spreadAngle}");
        }
    }
}