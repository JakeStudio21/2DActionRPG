using UnityEngine;
using System.Collections;

/// <summary>
/// Assasin 스킬2: Power Arrow (1개 강력한 화살 발사)
/// ISkill 인터페이스를 구현하여 모듈식 스킬 시스템에 통합
/// ScriptableObject 기반 데이터 분리 적용
/// </summary>
public class AssasinSkill2 : MonoBehaviour, ISkill
{
    [Header("스킬 데이터 (ScriptableObject)")]
    public SkillData skillData; // Inspector에서 할당
    
    [Header("디버그")]
    public bool showDebugLogs = true;
    
    // ISkill 인터페이스 구현
    public string SkillName => skillData != null ? skillData.skillName : "Power Arrow";
    public float Cooldown => skillData != null ? skillData.cooldown : 3f;
    
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
            Debug.Log("🟢 [AssasinSkill2] Awake() 시작");
            
        // 컴포넌트 참조 초기화
        animationController = GetComponent<PlayerAnimationController>();
        if (animationController == null)
            animationController = GetComponentInParent<PlayerAnimationController>();
            
        if (animationController == null)
        {
            Debug.LogWarning("🟡 [AssasinSkill2] PlayerAnimationController를 찾을 수 없습니다!");
        }
        else if (showDebugLogs)
        {
            Debug.Log("🟢 [AssasinSkill2] PlayerAnimationController 찾음");
        }
    }
    
    void Start()
    {
        // ⭐ SkillData 유효성 검사 강화
        if (skillData == null)
        {
            Debug.LogError("🔴 [AssasinSkill2] SkillData가 할당되지 않았습니다! Inspector에서 AssasinSkill2Data를 할당하세요!");
            return;
        }
        
        Debug.Log($"🟢 [AssasinSkill2] SkillData 로드 성공:");
        Debug.Log($"   - 스킬명: {skillData.skillName}");
        Debug.Log($"   - 쿨다운: {skillData.cooldown}초");
        Debug.Log($"   - 프리팹: {skillData.projectilePrefab?.name ?? "None"}");
        Debug.Log($"   - 속도: {skillData.projectileSpeed}");
        Debug.Log($"   - 크기: {skillData.projectileScale}");
        
        if (showDebugLogs)
            Debug.Log("🟢 [AssasinSkill2] 초기화 완료!");
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
                Debug.Log($"🟢 [AssasinSkill2] 무기 '{bowTransform.name}'의 발사 지점 '{firePoint.name}' 찾음");
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
        float cooldown = skillData != null ? skillData.cooldown : 3f;
        bool canUse = Time.time >= lastSkillTime + cooldown;
        
        if (showDebugLogs && !canUse)
        {
            Debug.Log($"🟡 [AssasinSkill2] CanUse = false, 남은 쿨다운: {GetCooldownRemaining():F1}초");
        }
        
        return canUse;
    }
    
    public void Execute()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [AssasinSkill2] Execute() 호출됨");
            
        if (!CanUse())
        {
            Debug.LogWarning($"🟡 [AssasinSkill2] 스킬2 쿨다운 중! 남은 시간: {GetCooldownRemaining():F1}초");
            return;
        }
        
        // 쿨다운 시작
        lastSkillTime = Time.time;
        
        // 애니메이션 트리거 (PlayerAnimationController를 통해)
        if (animationController != null)
        {
            bool success = animationController.TriggerSkill2();
            if (success)
            {
                if (OnSkillCooldownChanged != null)
                    OnSkillCooldownChanged(Cooldown);
                    
                Debug.Log($"🟢 [AssasinSkill2] 스킬2(Power Arrow) 애니메이션 트리거 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [AssasinSkill2] 애니메이션 트리거 실패 - 직접 실행");
                OnAnimationEvent();
            }
        }
        else
        {
            // PlayerAnimationController가 없으면 직접 실행
            Debug.LogWarning("🟡 [AssasinSkill2] PlayerAnimationController가 없음 - 직접 실행");
            OnAnimationEvent();
        }
    }
    
    public void OnAnimationEvent()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [AssasinSkill2] OnAnimationEvent() 호출됨 - 실제 Power Arrow 발사");
            
        if (bowTransform == null || firePoint == null)
        {
            Debug.LogWarning("[AssasinSkill2] Bow 또는 FirePoint가 없습니다!");
            UpdateBowReference(); // 다시 한 번 시도
            if (bowTransform == null || firePoint == null)
                return;
        }
        
        if (GamePoolManager.Instance == null)
        {
            Debug.LogError("[AssasinSkill2] GamePoolManager.Instance가 null입니다!");
            return;
        }
        
        // SkillData에서 값 읽어오기 (fallback 포함)
        GameObject prefab = skillData?.projectilePrefab;
        float speed = skillData?.projectileSpeed ?? 15f;
        Vector3 scale = skillData?.projectileScale ?? Vector3.one * 1.5f;
        
        // ⭐ 중요: SkillData의 프리팹이 없으면 기본값 사용
        string poolTag = "Arrow"; // 기본값
        if (prefab != null)
        {
            poolTag = prefab.name; // SkillData의 프리팹 이름 사용
            if (showDebugLogs)
                Debug.Log($"🔵 [AssasinSkill2] SkillData 프리팹 사용: {poolTag}");
        }
        else
        {
            Debug.LogWarning("🟡 [AssasinSkill2] SkillData에 프리팹이 없어서 기본 'Arrow' 사용");
        }
        
        Vector2 dir = bowTransform.right;
        
        // SkillData 기반 프리팹 사용
        GameObject powerArrow = GamePoolManager.Instance.SpawnFromPool(poolTag, firePoint.position, Quaternion.identity);
        
        if (powerArrow != null)
        {
            // 방향 설정
            powerArrow.transform.right = dir;
            
            // SkillData 기반 크기 설정
            powerArrow.transform.localScale = scale;
            
            // Projectile 컴포넌트로 강화된 속도 설정
            if (powerArrow.TryGetComponent(out Projectile projectile))
            {
                projectile.UpdateMoveSpeed(speed);
            }
            else if (powerArrow.TryGetComponent(out Rigidbody2D rb))
            {
                rb.velocity = dir.normalized * speed;
            }
            
            Debug.Log($"🟢 [AssasinSkill2] Power Arrow 발사 완료: {powerArrow.name} (속도: {speed}, 크기: {scale})");
        }
        else
        {
            Debug.LogError($"[AssasinSkill2] GamePoolManager에서 PowerArrow 생성 실패!");
        }
    }
    
    public float GetCooldownRemaining()
    {
        float cooldown = skillData != null ? skillData.cooldown : 3f;
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
                Debug.Log($"🔵 [AssasinSkill2] SkillData 쿨다운 업데이트: {skillData.cooldown}초");
        }
    }
    
    /// <summary>
    /// 현재 스킬 상태 로깅 (디버깅용)
    /// </summary>
    public void LogCurrentState()
    {
        Debug.Log($"🔍 [AssasinSkill2] 현재 상태:");
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
            Debug.Log($"   - ProjectileSpeed: {skillData.projectileSpeed}");
            Debug.Log($"   - ProjectileScale: {skillData.projectileScale}");
            Debug.Log($"   - ProjectilePrefab: {(skillData.projectilePrefab != null ? skillData.projectilePrefab.name : "없음")}");
        }
    }

    #region ⭐ 안전한 Enemy 탐지 시스템 (Warrior와 동일)

    /// <summary>
    /// 안전한 Enemy 탐지 (Warrior 스킬과 동일한 로직)
    /// </summary>
    private GameObject[] FindEnemiesSafely()
    {
        GameObject[] enemies = null;
        
        try
        {
            // 1순위: Enemy 태그 사용 시도
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (showDebugLogs)
                Debug.Log($"🎯 [AssasinSkill2] Enemy 태그로 {enemies.Length}명의 적 발견");
        }
        catch (UnityException)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [AssasinSkill2] Enemy 태그가 정의되지 않음. 다른 방법 시도...");
            enemies = null;
        }
        
        // 2순위: Enemy 태그가 없으면 EnemyHealth 컴포넌트로 찾기
        if (enemies == null || enemies.Length == 0)
        {
            EnemyHealth[] enemyHealths = FindObjectsOfType<EnemyHealth>();
            enemies = new GameObject[enemyHealths.Length];
            for (int i = 0; i < enemyHealths.Length; i++)
            {
                enemies[i] = enemyHealths[i].gameObject;
            }
            
            if (showDebugLogs)
                Debug.Log($"🎯 [AssasinSkill2] EnemyHealth 컴포넌트로 {enemies.Length}명의 적 발견");
        }
        
        return enemies;
    }

    /// <summary>
    /// 가장 가까운 적 찾기 (안전한 버전)
    /// </summary>
    private Transform FindNearestEnemySafely()
    {
        GameObject[] enemies = FindEnemiesSafely();
        
        if (enemies == null || enemies.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [AssasinSkill2] 적을 찾을 수 없습니다!");
            return null;
        }
        
        Transform nearest = null;
        float minDistance = Mathf.Infinity;
        float maxRange = 15f; // 최대 탐지 범위
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue; // null 체크 추가
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance && distance <= maxRange)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }
        
        if (nearest != null && showDebugLogs)
            Debug.Log($"🎯 [AssasinSkill2] 가장 가까운 적: {nearest.name} (거리: {minDistance:F1})");
        
        return nearest;
    }

    #endregion
}