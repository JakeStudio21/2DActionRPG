using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 22f;
    [SerializeField] private GameObject particleOnHitPrefabVFX;
    [SerializeField] private bool isEnemyProjectile = false;
    [SerializeField] private float projectileRange = 10f;

    // 스킬 레벨별 이펙트 프리팹 배열 (Inspector에서 할당)
    public GameObject[] arrowEffectPrefabs;

    private Vector3 startPosition;
    private bool isReturningToPool = false; // 🔑 중복 반환 방지 플래그
    private bool needsStartPositionUpdate = false; // 🔑 startPosition 업데이트 플래그

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
                    player.TakeDamage(enemyDamage.damageAmount, transform);
                } else if (enemyHealth && !isEnemyProjectile) {
                    // 플레이어가 쏘는 발사체의 데미지 로직 (필요 시 수정)
                    int playerProjectileDamage = 1; // 예시 데미지
                    enemyHealth.TakeDamage(playerProjectileDamage);
                }

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
            // initialDirection = transform.right; // 현재 회전 방향
            initialDirection = transform.rotation * Vector3.right; // 실제 회전된 방향
            targetPosition = transform.position + initialDirection * projectileRange;
            totalDistance = Vector3.Distance(transform.position, targetPosition);
            
            Debug.Log($"🏹 [Projectile] 포물선 궤도 초기화 - 타겟: {targetPosition}, 거리: {totalDistance:F2}");
            Debug.Log($"🎯 [Projectile] 실제 발사 방향: {initialDirection}, 회전: {transform.rotation.eulerAngles}");
        }
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
            Debug.Log($"🏹 [MoveInArc] 포물선 이동 시작 - Start: {startPosition}, Target: {targetPosition}");
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
        
        // 🔧 바닥 도달 체크 및 풀링 반환 (개선된 방식)
        float groundLevel = startPosition.y - 0.1f; // 시작 지점보다 약간 아래를 바닥으로 간주 (여유 공간)
        bool reachedGround = transform.position.y <= groundLevel;
        bool reachedMaxDistance = progress >= 1.0f;
        
        if (reachedGround || reachedMaxDistance)
        {
            string reason = reachedGround ? "바닥 도달" : "최대 거리 도달";
            Debug.Log($"🏹 [MoveInArc] 포물선 완료! 사유: {reason}, Y좌표: {transform.position.y:F2} (기준: {groundLevel:F2}), progress: {progress:F3}");
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

/// <summary>
/// 발사체 궤도 타입
/// </summary>
    public enum TrajectoryType
    {
        Straight,   // 직선
        Arc         // 포물선
    }
}