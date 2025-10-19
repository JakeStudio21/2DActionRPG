using UnityEngine;
using System.Collections;
using CueSystem;

/// <summary>
/// AOE 영역 공격 구현체 - BaseAttackBehaviour 상속
/// ⭐ [New System] 영역 공격 전용 시스템 (삼각형, 사각형, 원형 지원)
/// </summary>
public class AOEAttack : BaseAttackBehaviour
{
    #region Inspector 설정
    
    [Header("AOE Specific Settings")]
    [SerializeField] private GameObject aoeEffectPrefab;
    [SerializeField] private Transform aoeSpawnPoint;
    
    [Header("Fallback Settings (AttackData 우선)")]
    [Tooltip("AOE 지속시간 (AttackData 없을 때만 사용)")]
    [SerializeField] private float fallbackAOEDuration = 1f;
    
    [Tooltip("AOE 모양 (AttackData 없을 때만 사용)")]
    [SerializeField] private AOEShapeType fallbackAOEShape = AOEShapeType.Circle;
    
    [Tooltip("AOE 크기 조절 (AttackData 없을 때만 사용)")]
    [SerializeField] private float fallbackAOEScale = 1f;
    
    [Header("Advanced Settings")]
    [Tooltip("AOE 생성 위치 오프셋")]
    [SerializeField] private Vector3 aoeOffset = Vector3.zero;
    
    [Tooltip("디버그 기즈모 표시")]
    [SerializeField] private bool showDebugGizmos = true;
    
    #endregion
    
    #region Private Fields
    
    // AOE 추적용
    private GameObject activeAOEEffect;
    
    // ⭐ 저장된 공격 방향 (Animation Event 지연 대응)
    private Vector2 savedAttackDirection = Vector2.right;
    
    #endregion
    
    #region BaseAttackBehaviour 추상 메서드 구현
    
    protected override void OnInitialize()
    {
        // AOE 생성 위치가 설정되지 않았으면 자신의 Transform 사용
        if (aoeSpawnPoint == null)
        {
            aoeSpawnPoint = transform;
        }
        
        ValidateAOESettings();
    }
    
    protected override void OnAttack()
    {
        // ⭐ RangedAttack 방식: 월드 좌표를 BlendTree 좌표로 직접 전달
        if (cachedPlayer != null && animationController != null)
        {
            Vector2 toPlayerWorld = (cachedPlayer.transform.position - transform.position).normalized;
            Vector2 toPlayerBlendTree = toPlayerWorld; // 아이소메트릭 변환 제거
            bool shouldFlipX = toPlayerWorld.x < 0;
            
            // ⭐ 방향 저장 (Animation Event에서 사용)
            savedAttackDirection = toPlayerWorld;
            
            animationController.UpdateAttackDirectionWithFlip(toPlayerBlendTree, shouldFlipX);
            
            Debug.Log($"[AOEAttack] {gameObject.name} - 공격 방향 저장: World({toPlayerWorld.x:F2}, {toPlayerWorld.y:F2}), flipX: {shouldFlipX}");
        }
    }
    
    protected override void ValidateAttackType()
    {
        if (AttackData != null && AttackData.AttackType != AttackType.AOE)
        {
            Debug.LogWarning($"[AOEAttack] {gameObject.name} - AttackData의 공격 타입이 AOE가 아닙니다: {AttackData.AttackType}");
        }
    }
    
    protected override int GetFallbackDamage() => 1; // AOE 이펙트에서 처리
    
    protected override float GetFallbackRange() => 5f; // 기본 범위 사용
    
    #endregion
    
    #region Animation Event 처리
    
    /// <summary>
    /// Animation Event에서 호출되는 AOE 이펙트 생성
    /// </summary>
    public void SpawnAOEEffect()
    {
        Debug.Log($"[AOEAttack] {gameObject.name} - Animation Event AOE 이펙트 생성!");
        
        // ✅ Cue 시스템 발행
        EmitAOECues();
        
        // ✅ 사운드 재생
        PlayAttackSound();
        
        // ⭐ 핵심: AOE 이펙트 생성
        CreateAOEEffect();
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 AOE 이펙트 생성 (다른 이름)
    /// </summary>
    public void SpawnAOEEffectAnimEvent()
    {
        SpawnAOEEffect();
    }
    
    #endregion
    
    #region AOE 이펙트 생성 로직 (핵심)
    
    /// <summary>
    /// AOE 이펙트 생성 및 설정
    /// </summary>
    private void CreateAOEEffect()
    {
        GameObject prefabToUse = GetCurrentAOEPrefab();
        
        if (prefabToUse == null)
        {
            Debug.LogError($"[AOEAttack] {gameObject.name}: AOE 이펙트 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        // ⭐ AOE 생성 위치 계산
        Vector3 spawnPosition = CalculateAOESpawnPosition();
        
        // ⭐ AOE 이펙트 생성
        GameObject aoeEffect = CreateAOEEffect(prefabToUse, spawnPosition);
        
        if (aoeEffect != null)
        {
            // ⭐ AOE 이펙트 설정
            ConfigureAOEEffect(aoeEffect);
            
            // 추적 목록에 추가
            activeAOEEffect = aoeEffect;
            
            Debug.Log($"[AOEAttack] AOE 이펙트 생성 완료: {aoeEffect.name} at {spawnPosition} (골렘 위치에서 시작)");
        }
    }
    
    /// <summary>
    /// AOE 생성 위치 계산
    /// </summary>
    private Vector3 CalculateAOESpawnPosition()
    {
        Vector3 basePosition = aoeSpawnPoint.position + aoeOffset;
        
        // ⭐ 골렘 위치에서 시작 (플레이어 위치가 아님!)
        // 플레이어 방향으로 회전은 CalculateAOERotation()에서 처리
        return basePosition;
    }
    
    /// <summary>
    /// AOE 이펙트 생성 (풀링 시스템)
    /// </summary>
    private GameObject CreateAOEEffect(GameObject prefab, Vector3 spawnPosition)
    {
        GameObject effect = null;
        
        // ⭐ 플레이어 방향으로 회전 계산
        Quaternion rotation = CalculateAOERotation();
        
        // GamePoolManager 사용 시도
        try
        {
            if (GamePoolManager.Instance != null)
            {
                string poolTag = GetPoolTagFromPrefab(prefab);
                effect = GamePoolManager.Instance.SpawnFromPool(poolTag, spawnPosition, rotation);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[AOEAttack] GamePoolManager 사용 실패: {e.Message}");
        }
        
        // 풀링 실패 시 직접 생성
        if (effect == null)
        {
            effect = Instantiate(prefab, spawnPosition, rotation);
            Debug.Log($"[AOEAttack] 프리팹을 직접 생성했습니다: {prefab.name}");
        }
        
        return effect;
    }
    
    /// <summary>
    /// AOE 이펙트 회전 계산 (플레이어 방향)
    /// </summary>
    private Quaternion CalculateAOERotation()
    {
        // 플레이어가 있으면 플레이어 방향으로 회전
        if (cachedPlayer != null)
        {
            Vector3 directionToPlayer = (cachedPlayer.transform.position - transform.position).normalized;
            float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
            
            Debug.Log($"[AOEAttack] 플레이어 방향 회전: {angle:F1}도, 방향: {directionToPlayer}");
            return Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // 플레이어가 없으면 저장된 방향으로 회전
        if (savedAttackDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(savedAttackDirection.y, savedAttackDirection.x) * Mathf.Rad2Deg;
            Debug.Log($"[AOEAttack] 저장된 방향 회전: {angle:F1}도, 방향: {savedAttackDirection}");
            return Quaternion.AngleAxis(angle, Vector3.forward);
        }
        
        // 기본값: 동쪽 방향 (E)
        Debug.Log($"[AOEAttack] 기본 방향 회전: 0도 (동쪽)");
        return Quaternion.identity;
    }
    
    /// <summary>
    /// AOE 이펙트 Flip 처리 (파티클 시스템용)
    /// ⭐ 수정: 좌측 방향에서는 Y축 Flip (땅이 위로 솟아오르는 효과)
    /// </summary>
    private void ApplyAOEFlip(GameObject aoeEffect)
    {
        // 플레이어 방향 확인
        if (cachedPlayer != null)
        {
            Vector3 directionToPlayer = (cachedPlayer.transform.position - transform.position).normalized;
            bool shouldFlipY = directionToPlayer.x < 0; // 서쪽 방향이면 Y축 Flip
            
            if (shouldFlipY)
            {
                // Y축으로 Flip (위아래 반전) - 땅이 위로 솟아오르는 효과
                Vector3 scale = aoeEffect.transform.localScale;
                scale.y = -Mathf.Abs(scale.y); // Y축을 음수로 만들어 Flip
                aoeEffect.transform.localScale = scale;
                
                Debug.Log($"[AOEAttack] AOE 이펙트 Y축 Flip 적용: 서쪽 방향 땅 솟아오름 효과");
            }
        }
    }
    
    /// <summary>
    /// AOE 이펙트 설정 (크기, 데미지, 지속시간)
    /// </summary>
    private void ConfigureAOEEffect(GameObject aoeEffect)
    {
        // ⭐ 크기 설정
        float currentScale = GetCurrentAOEScale();
        aoeEffect.transform.localScale = Vector3.one * currentScale;
        
        // ⭐ Y축 Flip 처리 (서쪽 방향 땅 솟아오름 효과)
        ApplyAOEFlip(aoeEffect);
        
        // ⭐ 데미지 설정
        SetAOEDamage(aoeEffect);
        
        // ⭐ 지속시간 설정 (자동 제거)
        float duration = GetCurrentAOEDuration();
        if (duration > 0)
        {
            Destroy(aoeEffect, duration);
        }
        
        Debug.Log($"[AOEAttack] AOE 이펙트 설정 완료: 크기={currentScale:F1}, 지속시간={duration:F1}초");
    }
    
    /// <summary>
    /// AOE 이펙트에 데미지 설정
    /// </summary>
    private void SetAOEDamage(GameObject aoeEffect)
    {
        int currentDamage = GetScaledDamage();
        
        // ⭐ AOEDamage 컴포넌트 설정
        if (aoeEffect.TryGetComponent(out AOEDamage aoeDamage))
        {
            aoeDamage.SetDamage(currentDamage);
            aoeDamage.SetAOEShape(GetCurrentAOEShape());
            
            // ⭐ 넉백 강도 설정 (EnemyData 기반)
            float knockbackThrust = GetKnockbackThrust();
            aoeDamage.SetKnockbackThrust(knockbackThrust);
            
            // ⭐ 넉백 방향 소스 설정 (몬스터 Transform)
            aoeDamage.SetKnockbackSource(transform);
        }
        // ⭐ 범용 데미지 컴포넌트 지원
        else if (aoeEffect.TryGetComponent(out EnemyDamage enemyDamage))
        {
            enemyDamage.damageAmount = currentDamage;
        }
        
        Debug.Log($"[AOEAttack] AOE 데미지 설정: {currentDamage}, 넉백 강도: {GetKnockbackThrust()}");
    }
    
    /// <summary>
    /// 넉백 강도 계산 (EnemyData 기반)
    /// </summary>
    private float GetKnockbackThrust()
    {
        if (BaseEnemy != null && BaseEnemy.EnemyData != null)
        {
            return BaseEnemy.EnemyData.KnockBackThrust;
        }
        
        return 15f; // 기본 넉백 강도
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 프리팹에서 풀 태그 추출
    /// </summary>
    private string GetPoolTagFromPrefab(GameObject prefab)
    {
        return prefab.name switch
        {
            var name when name.Contains("AOE") => "AOE_Effect",
            var name when name.Contains("Triangle") => "Triangle_AOE",
            var name when name.Contains("Rectangle") => "Rectangle_AOE",
            var name when name.Contains("Circle") => "Circle_AOE",
            _ => prefab.name
        };
    }
    
    #endregion
    
    #region 데이터 기반 속성 계산 (AttackData 우선순위)
    
    /// <summary>
    /// 현재 AOE 프리팹 반환
    /// </summary>
    private GameObject GetCurrentAOEPrefab()
    {
        // 1순위: AttackData
        if (AttackData != null && AttackData.AOEEffectPrefab != null)
        {
            return AttackData.AOEEffectPrefab;
        }
        
        // 2순위: Inspector 설정
        return aoeEffectPrefab;
    }
    
    
    /// <summary>
    /// 현재 AOE 지속시간 반환
    /// </summary>
    private float GetCurrentAOEDuration()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.AOEDuration;
        }
        
        // 2순위: Inspector 설정
        return fallbackAOEDuration;
    }
    
    /// <summary>
    /// 현재 AOE 모양 반환
    /// </summary>
    private AOEShapeType GetCurrentAOEShape()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.AOEShape;
        }
        
        // 2순위: Inspector 설정
        return fallbackAOEShape;
    }
    
    /// <summary>
    /// 현재 AOE 크기 반환
    /// </summary>
    private float GetCurrentAOEScale()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.AOEScale;
        }
        
        // 2순위: Inspector 설정
        return fallbackAOEScale;
    }
    
    #endregion
    
    #region Cue 시스템 연동
    
    /// <summary>
    /// 🎵 AOE 이펙트 Cue 발행
    /// </summary>
    private void EmitAOECues()
    {
        try
        {
            var context = new CueContext
            {
                position = aoeSpawnPoint.position,
                rotation = aoeSpawnPoint.rotation,
                normal = Vector3.up,
                facingDir = savedAttackDirection,
                follow = null,
                actorType = ActorType.Enemy,
                surfaceType = SurfaceType.Default,
                magnitude = 1f, // 기본 크기 사용
                isCritical = false,
                scale = GetCurrentAOEScale()
            };
            
            string eventKey = "attack.aoe.explosion";
            bool success = CueEmitter.Emit(eventKey, "Enemy", context);
            
            Debug.Log($"🎵 [AOEAttack] Cue 발행: {eventKey} → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [AOEAttack] Cue 발행 오류: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 검증 및 설정
    
    /// <summary>
    /// AOE 설정 검증
    /// </summary>
    private void ValidateAOESettings()
    {
        if (AttackData != null)
        {
            Debug.Log($"[AOEAttack] AttackData 기반 AOE 설정:");
            Debug.Log($"  - 공격명: {AttackData.AttackName}");
            Debug.Log($"  - AOE 지속시간: {GetCurrentAOEDuration():F1}초");
            Debug.Log($"  - AOE 모양: {GetCurrentAOEShape()}");
            Debug.Log($"  - AOE 크기: {GetCurrentAOEScale():F1}");
            Debug.Log($"  - 데미지: {GetScaledDamage()}");
            
            if (GetCurrentAOEPrefab() == null)
            {
                Debug.LogError($"[AOEAttack] AOE 이펙트 프리팹이 설정되지 않았습니다!");
            }
        }
        else
        {
            Debug.Log($"[AOEAttack] Fallback 설정 사용:");
            Debug.Log($"  - AOE 프리팹: {(aoeEffectPrefab != null ? aoeEffectPrefab.name : "없음")}");
            Debug.Log($"  - 지속시간: {fallbackAOEDuration}");
            Debug.Log($"  - 모양: {fallbackAOEShape}, 크기: {fallbackAOEScale}");
        }
    }
    
    #endregion
    
    #region 디버그 및 시각화
    
    /// <summary>
    /// AOE 범위 시각화 (에디터에서만)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // AOE 범위 표시
        Vector3 center = aoeSpawnPoint.position + aoeOffset;
        float range = 5f; // 기본 범위
        
        Gizmos.color = Color.red;
        
        switch (GetCurrentAOEShape())
        {
            case AOEShapeType.Circle:
                Gizmos.DrawWireSphere(center, range);
                break;
                
            case AOEShapeType.Rectangle:
                Gizmos.DrawWireCube(center, Vector3.one * range);
                break;
                
            case AOEShapeType.Triangle:
                // 삼각형은 복잡하므로 원으로 대체 표시
                Gizmos.DrawWireSphere(center, range);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(center, range * 0.5f);
                break;
        }
        
        // AOE 생성 지점 표시
        if (aoeSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(aoeSpawnPoint.position, 0.2f);
        }
    }
    
    /// <summary>
    /// AOE 디버그 정보
    /// </summary>
    [ContextMenu("Debug AOE Info")]
    public void DebugAOEInfo()
    {
        string info = $"=== AOEAttack {gameObject.name} ===\n";
        info += $"AOE Duration: {GetCurrentAOEDuration():F1}초\n";
        info += $"AOE Shape: {GetCurrentAOEShape()}\n";
        info += $"AOE Scale: {GetCurrentAOEScale():F1}\n";
        info += $"AOE Damage: {GetScaledDamage()}\n";
        info += $"Current Range: {GetScaledRange():F1}\n";
        info += $"Active AOE Effect: {(activeAOEEffect != null ? activeAOEEffect.name : "없음")}\n";
        
        if (AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += AttackData.GetDebugInfo(BaseEnemy?.CurrentLevel ?? 1);
        }
        else
        {
            info += "\n=== Fallback 정보 ===\n";
            info += $"AOE Prefab: {(aoeEffectPrefab != null ? aoeEffectPrefab.name : "없음")}\n";
            info += $"Duration: {fallbackAOEDuration}\n";
            info += $"Shape: {fallbackAOEShape}, Scale: {fallbackAOEScale}";
        }
        
        Debug.Log(info);
    }
    
    #endregion
}
