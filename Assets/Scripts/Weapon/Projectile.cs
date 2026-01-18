using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CueSystem; // ⭐ Phase 1-2: 히트 이펙트 Cue 시스템

public class Projectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 22f;
    [SerializeField] private GameObject particleOnHitPrefabVFX;
    [SerializeField] private bool isEnemyProjectile = false;
    [SerializeField] private float projectileRange = 10f;

    // 스킬 레벨별 이펙트 프리팹 배열 (Inspector에서 할당)
    public GameObject[] arrowEffectPrefabs;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    [Header("🧱 벽 충돌 설정")]
    [SerializeField] private LayerMask wallLayer; // Inspector에서 Wall 선택

    private Vector3 startPosition;
    private bool isReturningToPool = false; // 🔑 중복 반환 방지 플래그
    private bool needsStartPositionUpdate = false; // 🔑 startPosition 업데이트 플래그
    
    // ⭐ Phase 1-2: 등급 정보 저장
    private ItemGrade projectileGrade = ItemGrade.C;
    private WeaponType weaponType = WeaponType.Bow;

    [Header("🏹 궤도 시스템")]
    [SerializeField] private TrajectoryType trajectoryType = TrajectoryType.Straight;
    [SerializeField] private float arcHeight = 2f; // 포물선 높이 (단위: Unity units)
    [SerializeField] private AnimationCurve trajectoryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 궤도 계산용 변수
    private Vector3 targetPosition;
    private float totalDistance;
    private float traveledDistance = 0f;
    private Vector3 initialDirection;

    void Start() {
        // 스킬 레벨별 이펙트 적용
        int skillLevel = 0;
        var player = FindObjectOfType<PlayerController>();  // 변경: PlayerController.Instance → FindObjectOfType<PlayerController>()
        if (player != null)
            skillLevel = player.GetSkillLevel("Bow");

        // 🔧 제거: 스킬 레벨에 따른 이펙트 적용 (메서드가 없음)
        // ApplySkillLevelEffects(skillLevel);
    }

    private void Update()
    {
        if (isReturningToPool) return; // 🔑 반환 중이면 업데이트 중단
        
        // ⭐ 핵심 수정: 발사할 때마다 startPosition 업데이트
        if (needsStartPositionUpdate)
        {
            startPosition = transform.position;
            needsStartPositionUpdate = false;
            
            // 🚨 N/S 방향 특별 체크
            float angle = transform.rotation.eulerAngles.z;
            bool isNorthSouth = (Mathf.Abs(angle - 90f) < 10f) || (Mathf.Abs(angle - 270f) < 10f);
            if (isNorthSouth)
            {
                Debug.LogWarning($"🚨🚨🚨 [N/S DIRECTION] 감지! 각도: {angle:F1}도, 위치: {transform.position}");
                Debug.LogWarning($"🚨🚨🚨 [N/S DIRECTION] 활성화 상태: {gameObject.activeInHierarchy}");
            }
            
            // 🆕 startPosition 설정 후 궤도 초기화
            InitializeTrajectory();
            
            // 🔧 위치 설정 완료 후 TrailRenderer 최종 초기화
            var trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                Debug.Log("🔧 [Projectile] 위치 설정 후 TrailRenderer 최종 초기화");
            }
        }
        MoveProjectile();
        DetectFireDistance();
    }

    public void UpdateProjectileRange(float projectileRange){
        this.projectileRange = projectileRange;
    }

    public void UpdateMoveSpeed(float moveSpeed)
    {
        this.moveSpeed = moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (isReturningToPool) return; // 🔑 이미 반환 중이면 무시
        
        // 🧱 벽 충돌 감지 (모든 투사체)
        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        
        if (wallLayerIndex != -1 && other.gameObject.layer == wallLayerIndex)
        {
            OnHitWall(other);
            return;
        }
        
        // 🚨 N/S 방향에서 충돌 로그
        float angle = transform.rotation.eulerAngles.z;
        bool isNorthSouth = (Mathf.Abs(angle - 90f) < 10f) || (Mathf.Abs(angle - 270f) < 10f);
        if (isNorthSouth)
        {
            Debug.LogWarning($"🚨🚨🚨 [N/S COLLISION] {other.gameObject.name}와 충돌! 위치: {transform.position}");
        }
        
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        Indestructible indestructible = other.gameObject.GetComponent<Indestructible>();
        PlayerHealth player = other.gameObject.GetComponent<PlayerHealth>();

        if (!other.isTrigger && (enemyHealth || indestructible || player)) {

            if ((player && isEnemyProjectile) || (enemyHealth && !isEnemyProjectile))
            {
                // 데미지를 입히는 로직을 PlayerHealth와 EnemyHealth의 OnCollision/OnTrigger가 담당하도록 변경합니다.
                // Projectile은 시각 효과와 소멸만 처리합니다.
                
                // EnemyDamage 컴포넌트에서 데미지 값을 가져와서 적용
                EnemyDamage enemyDamage = GetComponent<EnemyDamage>();
                if (player && isEnemyProjectile && enemyDamage != null) {
                    // 적 발사체 → 플레이어 피격
                    player.TakeDamage(enemyDamage.damageAmount, transform);
                }
                // ✅ 플레이어 발사체는 DamageSource.cs가 데미지를 처리하므로 여기서는 Skip
                // else if (enemyHealth && !isEnemyProjectile) {
                //     // DamageSource.cs가 이미 데미지를 적용함
                // }

                // 🔑 VFX 생성 (Fallback)
                if (particleOnHitPrefabVFX != null)
                {
                    GamePoolManager.Instance.SpawnFromPool(particleOnHitPrefabVFX.name, transform.position, transform.rotation);
                }
                else
                {
                    Debug.LogWarning($"[Projectile] particleOnHitPrefabVFX가 할당되지 않음: {gameObject.name}");
                }

                // 🔑 한 번만 반환
                ReturnProjectileToPool();
                
            } else if (!other.isTrigger && indestructible) {
                // 🔑 VFX 생성
                if (particleOnHitPrefabVFX != null)
                {
                    GamePoolManager.Instance.SpawnFromPool(particleOnHitPrefabVFX.name, transform.position, transform.rotation);
                }
                else
                {
                    Debug.LogWarning($"[Projectile] particleOnHitPrefabVFX가 할당되지 않음: {gameObject.name}");
                }
                
                // 🔑 한 번만 반환
                ReturnProjectileToPool();
            }
        }
            
    }

    private void DetectFireDistance() {
        if (isReturningToPool) return; // 🔑 이미 반환 중이면 무시
        
        // 🔧 포물선은 MoveInArc()에서 progress 기반으로 체크하므로 직선만 처리
        if (trajectoryType == TrajectoryType.Arc) return;
        
        float currentDistance = Vector3.Distance(transform.position, startPosition);
        
        if (currentDistance > projectileRange) {
            Debug.Log($"🏹 [DetectFireDistance] 직선 발사체 사거리 초과: {currentDistance:F2} > {projectileRange:F2}");
            ReturnProjectileToPool();
        }
    }

    // 🔑 새로운 통합 반환 메서드
    private void ReturnProjectileToPool()
    {
        if (isReturningToPool) return; // 🔑 중복 반환 방지
        
        // 🚨 N/S 방향에서 반환 로그
        float angle = transform.rotation.eulerAngles.z;
        bool isNorthSouth = (Mathf.Abs(angle - 90f) < 10f) || (Mathf.Abs(angle - 270f) < 10f);
        if (isNorthSouth)
        {
            Debug.LogWarning($"🚨🚨🚨 [N/S POOL RETURN] 풀 반환됨! 각도: {angle:F1}도, 위치: {transform.position}");
        }
        
        isReturningToPool = true; // 🔑 반환 중 플래그 설정
        
        if (GamePoolManager.Instance != null)
        {
            // 🔧 수정: 동적 풀 태그 사용 (하드코딩 제거)
            string poolTag = gameObject.name.Replace("(Clone)", "").Trim();
            GamePoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 🔑 풀에서 다시 사용할 때 초기화 - startPosition 업데이트 플래그 설정
    // private void OnEnable()
    // {
    //     Debug.Log("🔵🔵🔵 [PROJECTILE DEBUG] OnEnable() 호출됨!");
        
    //     isReturningToPool = false;
    //     needsStartPositionUpdate = true;
        
    //     // 🆕 궤도 초기화
    //     InitializeTrajectory();
        
    //     // 🚨 TrailRenderer 즉시 초기화 문제 해결
    //     StartCoroutine(DelayedTrailInitialization());
    // }
    private void OnEnable()
    {
        Debug.Log("🔵🔵🔵 [PROJECTILE DEBUG] OnEnable() 호출됨!");
        Debug.Log($"🚨 [SPAWN POSITION] 스폰 위치: {transform.position}");
        Debug.Log($"🚨 [SPAWN ROTATION] 스폰 회전: {transform.rotation.eulerAngles}");
        
        // 🚨 실제 발사인지 풀 초기화인지 구분
        bool isActualFire = transform.position.magnitude > 0.1f; // 원점이 아니면 실제 발사
        Debug.Log($"🚨 [FIRE TYPE] {(isActualFire ? "실제 발사" : "풀 초기화")}");
        
        if (isActualFire)
        {
            Debug.LogWarning($"🚨🚨🚨 [REAL FIRE] 실제 발사 감지! 위치: {transform.position}, 회전: {transform.rotation.eulerAngles}");
            
            // 🆕 N/S 방향 확인 및 상위 시스템 상태 진단
            float angle = transform.rotation.eulerAngles.z;
            bool isNorthSouth = (Mathf.Abs(angle - 90f) < 10f) || (Mathf.Abs(angle - 270f) < 10f);
            
            if (isNorthSouth)
            {
                Debug.LogWarning($"🎉🎉🎉 [N/S SUCCESS] N/S 방향 발사 성공! 각도: {angle:F1}도");
                Debug.LogWarning($"🎉🎉🎉 [N/S SUCCESS] 상위 시스템이 정상 작동함!");
            }
            else
            {
                Debug.Log($"📍 [E/W FIRE] E/W 방향 발사 - 각도: {angle:F1}도");
            }
        }
        
        isReturningToPool = false;
        needsStartPositionUpdate = true;
        
        // 🚨 InitializeTrajectory() 제거 - Update()에서 startPosition 설정 후 호출
        
        // 🚨 TrailRenderer 즉시 초기화 문제 해결
        StartCoroutine(DelayedTrailInitialization());
    }


    /// <summary>
    /// 궤도 초기화
    /// </summary>
    private void InitializeTrajectory()
    {
        traveledDistance = 0f;
        
        if (trajectoryType == TrajectoryType.Arc)
        {
            // 포물선용 타겟 위치 계산 (사거리 기반)
            initialDirection = transform.rotation * Vector3.right; // 실제 회전된 방향
            
            // ⭐ 아이소메트릭 수정: 방향을 정규화하여 일정한 거리 보장
            initialDirection.Normalize();
            targetPosition = transform.position + initialDirection * projectileRange;
            
            // ⭐ 거리는 projectileRange 고정 (3D Distance 사용 안 함)
            totalDistance = projectileRange;
            
            Debug.Log($"🏹 [Projectile] 포물선 궤도 초기화 - Start: {transform.position}, Target: {targetPosition}, 고정거리: {totalDistance:F2}");
            Debug.Log($"🎯 [Projectile] 정규화 방향: {initialDirection}, 회전: {transform.rotation.eulerAngles}");
        }
    }

    /// <summary>
    /// 방향을 이름으로 변환 (디버그용)
    /// </summary>
    private string GetDirectionName(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        if (angle >= -22.5f && angle < 22.5f) return "E";
        if (angle >= 22.5f && angle < 67.5f) return "NE";
        if (angle >= 67.5f && angle < 112.5f) return "N";
        if (angle >= 112.5f && angle < 157.5f) return "NW";
        if (angle >= 157.5f || angle < -157.5f) return "W";
        if (angle >= -157.5f && angle < -112.5f) return "SW";
        if (angle >= -112.5f && angle < -67.5f) return "S";
        if (angle >= -67.5f && angle < -22.5f) return "SE";
        
        return "UNKNOWN";
    }
    
    /// <summary>
    /// 🔧 지연된 TrailRenderer 초기화 (위치 설정 후)
    /// </summary>
    private System.Collections.IEnumerator DelayedTrailInitialization()
    {
        var trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer != null)
        {
            // 1단계: TrailRenderer 일시 비활성화
            trailRenderer.emitting = false;
            Debug.Log("🔧 [Projectile] TrailRenderer 일시 비활성화");
            
            // 2단계: 한 프레임 대기 (위치 설정 완료까지)
            yield return null;
            
            // 3단계: 궤적 완전 제거 + 재활성화
            trailRenderer.Clear();
            trailRenderer.emitting = true;
            Debug.Log("🔧 [Projectile] TrailRenderer 재활성화 완료");
        }
    }


    private void MoveProjectile()
    {
        switch (trajectoryType)
        {
            case TrajectoryType.Straight:
                MoveStraight();
                break;
            case TrajectoryType.Arc:
                MoveInArc();
                break;
        }
    }

    /// <summary>
    /// 직선 이동 (수정: 실제 회전 방향으로 이동)
    /// </summary>
    private void MoveStraight()
    {
        // 🔧 수정: 발사체의 실제 회전 방향으로 이동
        Vector3 moveDirection = transform.rotation * Vector3.right;
        transform.position += moveDirection * Time.deltaTime * moveSpeed;
    }

    /// <summary>
    /// 포물선 이동 (새로운 방식)
    /// </summary>
    private void MoveInArc()
    {
        // 🆕 디버그: 첫 프레임에만 로그 출력
        if (traveledDistance == 0f)
        {
            Vector2 direction2D = (targetPosition - startPosition).normalized;
            string directionName = GetDirectionName(direction2D);
            Debug.Log($"🏹 [MoveInArc] 포물선 이동 시작 [{directionName}] - Start: {startPosition}, Target: {targetPosition}, 거리: {totalDistance:F2}");
        }
        
        // 거리 업데이트
        float deltaDistance = moveSpeed * Time.deltaTime;
        traveledDistance += deltaDistance;
        
        // 진행률 계산 (0 ~ 1)
        float progress = Mathf.Clamp01(traveledDistance / totalDistance);
        
        // 직선 보간으로 기본 위치 계산
        Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        
        // Y축 오프셋 계산 (포물선 곡선)
        float heightOffset = arcHeight * trajectoryCurve.Evaluate(progress);
        
        // 최종 위치 설정
        transform.position = linearPosition + Vector3.up * heightOffset;
        
        // ⭐ 아이소메트릭 수정: progress >= 1.0만으로 착탄 판정 (groundLevel 제거)
        // 이유: 아이소메트릭에서 startPosition.y는 방향에 따라 달라져서 일관성 없음
        if (progress >= 1.0f)
        {
            float actualDistance = Vector2.Distance(new Vector2(startPosition.x, startPosition.y), 
                                                    new Vector2(transform.position.x, transform.position.y));
            Debug.Log($"🎯 [MoveInArc] 포물선 착탄! progress: {progress:F3}, 실제거리: {actualDistance:F2}, 목표거리: {totalDistance:F2}");
            ReturnProjectileToPool();
            return;
        }
        
        // 포물선 방향으로 회전 (선택적)
        if (progress < 1f)
        {
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, progress + 0.01f);
            float nextHeightOffset = arcHeight * trajectoryCurve.Evaluate(progress + 0.01f);
            Vector3 nextPosition = nextPos + Vector3.up * nextHeightOffset;
            
            Vector3 direction = (nextPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }
    
    #region ⭐ Phase 1-2: 등급별 히트 이펙트 시스템
    
    /// <summary>
    /// 발사체 초기화 (무기에서 호출)
    /// </summary>
    public void Initialize(ItemGrade grade, WeaponType type)
    {
        projectileGrade = grade;
        weaponType = type;
        
        if (showDebugLogs)
            Debug.Log($"🏹 [Projectile] 초기화: 등급={grade}, 타입={type}");
    }
    
    /// <summary>
    /// 🎨 히트 이펙트 발행 (등급별)
    /// </summary>
    private void EmitHitEffectCue(Vector3 hitPosition)
    {
        // 등급별 히트 이벤트 키 생성
        string eventKey = GetHitEffectEventKey();
        float magnitude = GetHitMagnitudeByGrade();
        
        var context = new CueContext
        {
            position = hitPosition,
            rotation = transform.rotation,
            actorType = ActorType.Player,
            magnitude = magnitude,
            isCritical = false, // 크리티컬은 별도 판정
            surfaceType = SurfaceType.Default
        };
        
        bool cueSuccess = CueEmitter.Emit(eventKey, "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"💥 [Projectile] Hit Effect Cue 발행: {eventKey} (등급: {projectileGrade}, 강도: {magnitude}) → {cueSuccess}");
    }
    
    /// <summary>
    /// 등급별 히트 이벤트 키
    /// </summary>
    private string GetHitEffectEventKey()
    {
        // 무기 타입 문자열
        string weaponTypeStr = weaponType == WeaponType.Bow ? "arrow" : 
                               weaponType == WeaponType.Magic ? "magic" : 
                               "ranged";
        
        // 등급별 이벤트 키
        switch (projectileGrade)
        {
            case ItemGrade.S:
                return $"hit.player.{weaponTypeStr}_s";
            case ItemGrade.A:
                return $"hit.player.{weaponTypeStr}_a";
            case ItemGrade.B:
                return $"hit.player.{weaponTypeStr}_b";
            case ItemGrade.C:
                return $"hit.player.{weaponTypeStr}_c";
            case ItemGrade.D:
                return $"hit.player.{weaponTypeStr}_d";
            default:
                return "hit.player.normal"; // fallback
        }
    }
    
    /// <summary>
    /// 등급별 히트 이펙트 강도
    /// </summary>
    private float GetHitMagnitudeByGrade()
    {
        switch (projectileGrade)
        {
            case ItemGrade.S: return 2.5f;
            case ItemGrade.A: return 1.8f;
            case ItemGrade.B: return 1.3f;
            case ItemGrade.C: return 1.0f;
            case ItemGrade.D: return 0.7f;
            default: return 1.0f;
        }
    }
    
    #endregion
    
    #region 🧱 벽 충돌 시스템
    
    /// <summary>
    /// 벽 충돌 처리
    /// </summary>
    private void OnHitWall(Collider2D wall)
    {
        // 벽 충돌 이펙트 (VFX가 있으면 재생)
        if (particleOnHitPrefabVFX != null)
        {
            GamePoolManager.Instance.SpawnFromPool(
                particleOnHitPrefabVFX.name, 
                transform.position, 
                transform.rotation
            );
        }
        
        // 투사체 반환
        ReturnProjectileToPool();
        
        if (showDebugLogs)
            Debug.Log($"🧱 [Projectile] 벽 충돌로 파괴됨: {gameObject.name}");
    }
    
    #endregion
}

/// <summary>
/// 발사체 궤도 타입
/// </summary>
public enum TrajectoryType
{
    Straight,   // 직선
    Arc         // 포물선
}