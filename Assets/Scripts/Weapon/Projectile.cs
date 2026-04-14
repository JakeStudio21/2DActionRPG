using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CueSystem; // ⭐ Phase 1-2: 히트 이펙트 Cue 시스템

public class Projectile : MonoBehaviour, IPoolTagReceiver
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
    private string _poolTag = ""; // SpawnFromPool에서 주입된 풀 태그
    
    // 🏹 관통 시스템
    private bool isPiercing = false;                              // 관통 여부 (SkillController에서 주입)
    private float pierceDamageRetention = 0.5f;                   // 관통 시 데미지 유지율 (SkillController에서 주입)
    private float currentPierceMultiplier = 1.0f;                 // 현재 관통 데미지 배율 (타격마다 감소)
    private readonly HashSet<GameObject> hitTargets = new HashSet<GameObject>(); // 다단 히트 방지
    
    // 💥 폭발 시스템 (Explosive Arrow)
    private bool hasExplosionOnHit = false;                       // 소멸 시 폭발 AOE 생성 여부
    private float explosionRadius = 2f;                           // 폭발 반경
    private int explosionDamageAmount = 0;                        // 폭발 데미지 (사전 계산값)
    private string explosionCueKey;                               // 폭발 VFX/SFX Cue 키
    
    // ⛓️ 체인 시스템 (Chain Shot)
    private bool isChainShot = false;                             // 체인 발사체 여부
    private int remainingChainCount = 0;                          // 남은 연쇄 횟수
    private float chainRadius = 5f;                               // 다음 적 탐색 반경
    private float chainDamageReduction = 0.1f;                    // 연쇄 1회당 데미지 감소율
    private float currentChainMultiplier = 1.0f;                  // 현재 연쇄 단계 데미지 배율 (1.0 → 0.9 → ...)
    private int chainBaseDamage = 0;                              // SkillController에서 주입한 스킬 기본 데미지
    private float chainDelay = 0.05f;                             // 착탄 후 다음 타겟 이동 전 Hit-Stop 정지 시간
    private bool isChainWaiting = false;                          // Hit-Stop 대기 중 플래그 (이동/충돌 중단용)
    private string chainHitCueKey;                                // 착탄 이펙트 CueKey (VFX+SFX+CameraShake 모두 CueEntry에서 관리)
    private readonly HashSet<GameObject> chainHitTargets = new HashSet<GameObject>(); // 중복 타격 방지
    private PlayerRuntimeStats _chainPlayerStats;                 // 체인 데미지 계산용 (지연 캐싱)
    private Collider2D _cachedCollider;                           // Hit-Stop 중 충돌 비활성화용 (Awake에서 캐싱)
    
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

    private void Awake()
    {
        // Collider2D를 Awake에서 캐싱 — Hit-Stop 도중 enabled 제어에 사용
        _cachedCollider = GetComponent<Collider2D>();
    }

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
    
    /// <summary>
    /// 적 투사체 여부 설정 (SimpleMobShooter 등 적이 발사할 때 호출)
    /// true: 플레이어에게 데미지 / 아군(SimpleMob)에게 소멸 안 함
    /// false(기본): 플레이어 발사체 — SimpleMob/EnemyHealth에 소멸
    /// </summary>
    public void SetAsEnemyProjectile(bool isEnemy)
    {
        isEnemyProjectile = isEnemy;
    }

    /// <summary>
    /// 관통 설정 주입 (SkillController에서 발사 시 호출)
    /// </summary>
    public void SetPierceData(bool piercing, float retention)
    {
        isPiercing = piercing;
        pierceDamageRetention = retention;
    }
    
    /// <summary>
    /// 현재 관통 배율 반환 (DamageSource가 AttackContext.pierceMultiplier에 주입)
    /// </summary>
    public float GetCurrentPierceMultiplier() => isPiercing ? currentPierceMultiplier : 1.0f;

    /// <summary>
    /// 배율을 다음 타격용으로 감소 — DamageSource가 타격 완료 후 명시적으로 호출
    /// 호출 순서: GetCurrentPierceMultiplier() → TakeDamage() → AdvancePierceMultiplier()
    /// </summary>
    public void AdvancePierceMultiplier()
    {
        if (!isPiercing) return;
        currentPierceMultiplier *= pierceDamageRetention;
    }
    
    /// <summary>
    /// 폭발 데이터 주입 (SkillController에서 발사 시 호출)
    /// hasExplosion=true 시 소멸 위치에서 DamageArea 기반 폭발 AOE가 생성됩니다.
    /// </summary>
    public void SetExplosionData(bool hasExplosion, float radius, int damageAmount, string cueKey)
    {
        hasExplosionOnHit = hasExplosion;
        explosionRadius = radius;
        explosionDamageAmount = damageAmount;
        explosionCueKey = cueKey;
    }

    /// <summary>
    /// 체인 데이터 주입 (SkillController에서 발사 시 호출)
    /// isChain=true 시 적 적중 때마다 방향을 꺾어 다음 적으로 날아감
    /// </summary>
    public void SetChainData(bool isChain, int chainCount, float radius, float reduction,
                             string hitCueKey, int baseDamage, float hitStopDelay = 0.05f)
    {
        isChainShot            = isChain;
        remainingChainCount    = chainCount;
        chainRadius            = radius;
        chainDamageReduction   = reduction;
        chainHitCueKey         = hitCueKey;
        chainBaseDamage        = baseDamage;
        chainDelay             = Mathf.Max(0f, hitStopDelay);
        currentChainMultiplier = 1.0f;
        chainHitTargets.Clear();
        _chainPlayerStats = null;
    }
    
    /// <summary>
    /// 체인 발사체 여부 (DamageSource가 데미지 처리를 스킵하는지 판단)
    /// </summary>
    public bool IsChainShotActive => isChainShot;

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
        
        // 🆕 SimpleMob 체크
        SimpleMob simpleMob = other.gameObject.GetComponent<SimpleMob>();
        
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        Indestructible indestructible = other.gameObject.GetComponent<Indestructible>();
        PlayerHealth player = other.gameObject.GetComponent<PlayerHealth>();

        if (!other.isTrigger && (simpleMob || enemyHealth || indestructible || player)) {

            if ((player && isEnemyProjectile) || 
                (enemyHealth && !isEnemyProjectile) ||
                (simpleMob && !isEnemyProjectile)) // 🆕
            {
                // 적 발사체 → 플레이어 피격
                EnemyDamage enemyDamage = GetComponent<EnemyDamage>();
                if (player && isEnemyProjectile && enemyDamage != null) {
                    player.TakeDamage(enemyDamage.damageAmount, transform, transform.position);
                }
                
                // ⛓️ 체인 발사체 처리 (DamageSource는 IsChainShotActive 체크로 스킵됨)
                if (isChainShot && !isEnemyProjectile && (enemyHealth || simpleMob))
                {
                    // 이미 이번 체인 시퀀스에서 타격한 적이면 무시
                    if (chainHitTargets.Contains(other.gameObject)) return;
                    // Hit-Stop 대기 중이면 추가 충돌 무시 (Collider 비활성화 보조 방어선)
                    if (isChainWaiting) return;
                    
                    // 타격 등록
                    chainHitTargets.Add(other.gameObject);
                    
                    // 체인 데미지 직접 처리
                    ApplyChainDamage(simpleMob, enemyHealth, other);
                    
                    // ⭐ 착탄 이펙트 — CueSystem 경유 (VFX + SFX + CameraShake 모두 CueEntry 한 곳에서 처리)
                    if (!string.IsNullOrEmpty(chainHitCueKey))
                    {
                        CueSystem.CueEmitter.Emit(chainHitCueKey, "Player", new CueSystem.CueContext
                        {
                            position   = other.transform.position,
                            rotation   = transform.rotation,
                            facingDir  = transform.right,
                            magnitude  = 1.0f,
                            scale      = 1.0f
                        });
                    }
                    
                    // 배율 감소
                    currentChainMultiplier *= (1f - chainDamageReduction);
                    remainingChainCount--;
                    
                    if (remainingChainCount <= 0)
                    {
                        ReturnProjectileToPool();
                        return;
                    }
                    
                    // Hit-Stop: 코루틴에서 대기 → 다음 타겟 탐색 → 방향 전환
                    StartCoroutine(ChainJumpRoutine());
                    return; // 발사체를 파괴하지 않음
                }
                
                // 🏹 플레이어 발사체 관통 처리
                if (!isEnemyProjectile && (enemyHealth || simpleMob))
                {
                    if (isPiercing)
                    {
                        // 이미 타격한 적이면 무시
                        if (hitTargets.Contains(other.gameObject)) return;
                        
                        // 새 적 등록 (배율 감소는 DamageSource가 타격 완료 후 AdvancePierceMultiplier()로 처리)
                        hitTargets.Add(other.gameObject);
                        
                        // ✅ 관통: ReturnProjectileToPool 호출 안 함 (계속 진행)
                        return;
                    }
                }
                
                // 비관통이거나 적 발사체: 첫 타격 후 소멸
                // ⭐ 보강: 풀 반환 전에 폭발 생성 → transform.position이 유효한 상태 보장
                // ⭐ 충돌한 몬스터 중심 위치를 전달하여 DamageArea가 정확한 위치에 생성됨
                if (!isEnemyProjectile) SpawnExplosion(other.transform.position);
                ReturnProjectileToPool();
                
            } else if (!other.isTrigger && indestructible) {
                ReturnProjectileToPool();
            }
        }
            
    }

    private void DetectFireDistance() {
        if (isReturningToPool) return; // 🔑 이미 반환 중이면 무시
        if (isChainWaiting) return;    // Hit-Stop 대기 중 사거리 초과 판정 스킵
        
        // 🔧 포물선은 MoveInArc()에서 progress 기반으로 체크하므로 직선만 처리
        if (trajectoryType == TrajectoryType.Arc) return;
        
        float currentDistance = Vector3.Distance(transform.position, startPosition);
        
        if (currentDistance > projectileRange) {
            Debug.Log($"🏹 [DetectFireDistance] 직선 발사체 사거리 초과: {currentDistance:F2} > {projectileRange:F2}");
            SpawnExplosion(transform.position);
            ReturnProjectileToPool();
        }
    }

    public void SetPoolTag(string tag)
    {
        _poolTag = tag;
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
        
        isReturningToPool = true; // 🔑 즉시 플래그 설정 — 이후 OnTriggerEnter2D 중복 진입 차단
        
        // ⭐ SetActive(false)를 1프레임 지연
        // 이유: Projectile.OnTriggerEnter2D → ReturnToPool → SetActive(false) 가 동기 실행되면
        //       Unity가 같은 충돌 이벤트의 DamageSource.OnTriggerEnter2D 콜백을 취소한다.
        //       isReturningToPool=true로 즉시 중복 충돌을 막고, 실제 비활성화는 1프레임 후 처리.
        StartCoroutine(DeferredPoolReturn());
    }
    
    private IEnumerator DeferredPoolReturn()
    {
        yield return null; // 현재 물리 스텝의 모든 OnTriggerEnter2D 콜백이 완료될 때까지 대기
        
        // OnEnable에서 isReturningToPool이 false로 리셋된 경우 = 풀에서 재사용 중 → 취소
        if (!isReturningToPool) yield break;
        
        if (GamePoolManager.Instance != null)
        {
            // SpawnFromPool에서 주입된 태그 사용, 없으면 이름으로 추론 (폴백)
            string tagToUse = !string.IsNullOrEmpty(_poolTag)
                ? _poolTag
                : gameObject.name.Replace("(Clone)", "").Trim();
            
            if (string.IsNullOrEmpty(_poolTag))
            {
                Debug.LogWarning($"[Projectile] {gameObject.name}: _poolTag가 주입되지 않음. 이름으로 추론: '{tagToUse}'");
            }
            
            GamePoolManager.Instance.ReturnToPool(tagToUse, gameObject);
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
        isReturningToPool = false;
        needsStartPositionUpdate = true;
        
        // 🏹 관통 상태 초기화 (풀 재사용 시 이전 상태 제거)
        currentPierceMultiplier = 1.0f;
        hitTargets.Clear();
        
        // 💥 폭발 상태 초기화
        hasExplosionOnHit = false;
        explosionDamageAmount = 0;
        explosionCueKey = null;
        
        // ⛓️ 체인 상태 초기화
        isChainShot            = false;
        remainingChainCount    = 0;
        currentChainMultiplier = 1.0f;
        chainBaseDamage        = 0;
        chainDelay             = 0.05f;
        chainHitCueKey         = null;
        chainHitTargets.Clear();
        _chainPlayerStats = null;
        
        // ⭐ Hit-Stop 풀링 안전장치: 대기 중 비활성화된 Collider2D를 반드시 복원
        // ChainJumpRoutine에서 collider를 꺼놓은 채 풀에 반환될 경우 유령 화살 버그 방지
        isChainWaiting = false;
        if (_cachedCollider != null) _cachedCollider.enabled = true;
        
        // ⭐ 보완사항 1 - TrailRenderer 잔상 버그 완벽 차단
        // DelayedTrailInitialization 코루틴과 별개로, 재사용 즉시 명시적으로 궤적 클리어
        var trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.Clear();
        
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
        // Hit-Stop 대기 중에는 이동 정지
        if (isChainWaiting) return;
        
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
            SpawnExplosion(transform.position);
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
    
    #endregion
    
    #region 💥 폭발 시스템 (Explosive Arrow)
    
    /// <summary>
    /// 지정 위치에서 폭발 AOE 생성 — ReturnProjectileToPool() 직전에 반드시 호출해야 함
    /// hitPosition: 몬스터 충돌 시 other.transform.position, 사거리 끝/포물선 착탄 시 transform.position
    /// ⭐ 폭발 이펙트는 hitPosition에서 1번만 직접 Emit — hitCueKey 미사용으로 중복 발동 방지
    /// </summary>
    private void SpawnExplosion(Vector3 hitPosition)
    {
        if (!hasExplosionOnHit || explosionDamageAmount <= 0) return;
        
        GameObject damageAreaPrefab = Resources.Load<GameObject>("Prefabs/VFX/DamageArea");
        if (damageAreaPrefab == null)
        {
            Debug.LogError("❌ [Projectile] DamageArea 프리팹을 찾을 수 없습니다! 경로: Resources/Prefabs/VFX/DamageArea");
            return;
        }
        
        // 폭발 이펙트를 착탄 위치에서 정확히 1번만 발동
        // hitCueKey로 전달하면 AOE 범위 내 몬스터마다 반복 발동되므로 직접 Emit 사용
        if (!string.IsNullOrEmpty(explosionCueKey))
        {
            CueSystem.CueEmitter.Emit(explosionCueKey, "Player", new CueSystem.CueContext
            {
                position = hitPosition,
                rotation = transform.rotation,
                facingDir = transform.right,
                magnitude = 1.0f,
                scale = 1.0f
            });
        }
        
        GameObject daGO = Object.Instantiate(damageAreaPrefab);
        DamageArea da = daGO.GetComponent<DamageArea>();
        if (da == null)
        {
            Debug.LogError("❌ [Projectile] DamageArea 컴포넌트가 없습니다!");
            Destroy(daGO);
            return;
        }
        
        da.InitializeForPlayer(
            AOEShapeType.Circle,
            hitPosition,        // 착탄 위치 기준으로 DamageArea 생성
            transform.right,
            explosionRadius,
            Vector2.one * explosionRadius * 2f,
            360f,
            explosionDamageAmount,
            hitCueKey: null     // 폭발 이펙트는 위에서 직접 발동 완료
        );
        
        Destroy(daGO, 2f);
        
        Debug.Log($"💥 [Projectile] 폭발 생성: pos={hitPosition}, radius={explosionRadius}, damage={explosionDamageAmount}");
    }
    
    #endregion
    
    #region ⛓️ 체인 시스템 (Chain Shot)
    
    /// <summary>
    /// Hit-Stop 코루틴 — 착탄 후 발사체를 잠깐 정지시킨 뒤 다음 타겟으로 방향 전환
    /// ① 이동 정지 + Collider 비활성화 (대기 중 추가 충돌 방지)
    /// ② chainDelay 대기
    /// ③ FindNextChainTarget() 호출
    /// ④ 타겟 있음: Collider 복원 + 이동 재개 / 없음: 풀 반환
    /// </summary>
    private IEnumerator ChainJumpRoutine()
    {
        // ① 이동 정지 + 충돌 비활성화
        isChainWaiting = true;
        if (_cachedCollider != null) _cachedCollider.enabled = false;
        
        // ② Hit-Stop 대기
        if (chainDelay > 0f)
            yield return new WaitForSeconds(chainDelay);
        
        // 대기 도중 이미 풀로 반환됐다면 중단 (코루틴이 늦게 재개될 경우 방어)
        if (isReturningToPool)
        {
            isChainWaiting = false;
            if (_cachedCollider != null) _cachedCollider.enabled = true;
            yield break;
        }
        
        // ③ 다음 타겟 탐색 및 방향 전환
        if (!FindNextChainTarget())
        {
            // 유효 타겟 없음 — Collider 복원 후 소멸
            if (_cachedCollider != null) _cachedCollider.enabled = true;
            isChainWaiting = false;
            ReturnProjectileToPool();
            yield break;
        }
        
        // ④ 이동 재개
        if (_cachedCollider != null) _cachedCollider.enabled = true;
        isChainWaiting = false;
    }
    
    /// <summary>
    /// 체인 데미지를 직접 계산하여 적용 — DamageSource를 우회하여 배율 완전 제어
    /// chainBaseDamage × currentChainMultiplier 기준으로 CombatFormula 실행
    /// </summary>
    private void ApplyChainDamage(SimpleMob simpleMob, EnemyHealth enemyHealth, Collider2D hit)
    {
        int damageThisHit = Mathf.RoundToInt(chainBaseDamage * currentChainMultiplier);
        if (damageThisHit <= 0) return;
        
        if (_chainPlayerStats == null)
            _chainPlayerStats = Object.FindObjectOfType<PlayerRuntimeStats>();
        
        if (simpleMob != null)
        {
            if (_chainPlayerStats == null)
            {
                simpleMob.TakeDamage(damageThisHit);
                return;
            }
            var ctx = BuildChainAttackContext(damageThisHit, hit, defense: 0f, target: null, hpPercent: 1f);
            var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
            simpleMob.TakeDamage(result.finalDamage);
            
            if (showDebugLogs)
                Debug.Log($"⛓️ [Projectile] 체인 SimpleMob 데미지: {result.finalDamage} (배율 {currentChainMultiplier:F2}) → {hit.name}");
        }
        else if (enemyHealth != null)
        {
            if (_chainPlayerStats == null)
            {
                enemyHealth.TakeDamage(damageThisHit);
                return;
            }
            var baseEnemy = hit.GetComponent<BaseEnemy>();
            float defense = baseEnemy != null ? baseEnemy.GetScaledDefense() : 0f;
            var enemyTarget = hit.GetComponent<IEnemyTarget>();
            float hpPercent = enemyTarget != null ? enemyTarget.GetCurrentHpPercent() : 1f;
            
            var ctx = BuildChainAttackContext(damageThisHit, hit, defense, enemyTarget, hpPercent);
            var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
            result.hitPosition = hit.transform.position;
            enemyHealth.TakeDamage(result, transform);
            
            if (showDebugLogs)
                Debug.Log($"⛓️ [Projectile] 체인 EnemyHealth 데미지: {result.finalDamage} (배율 {currentChainMultiplier:F2}) → {hit.name}");
        }
    }
    
    private CombatFormula.AttackContext BuildChainAttackContext(int damage, Collider2D hit,
                                                                float defense, IEnemyTarget target, float hpPercent)
    {
        return new CombatFormula.AttackContext
        {
            baseAttack         = damage,
            attackerClass      = null,
            targetDefense      = defense,
            targetTransform    = hit.transform,
            attackerTransform  = transform,
            isSkillAttack      = true,
            skillMultiplier    = 1.0f,
            criticalChance     = _chainPlayerStats.FinalCriticalChance,
            criticalMultiplier = _chainPlayerStats.FinalCriticalDamage,
            isPlayerAttack     = true,
            attackerLevel      = _chainPlayerStats.CurrentLevel,
            isBerserkerState   = false,
            armorPenetration   = _chainPlayerStats.FinalArmorPenetration,
            lifeStealPercent   = _chainPlayerStats.FinalLifeSteal,
            target             = target,
            selfHpPercent      = 1.0f,
            targetHpPercent    = hpPercent
        };
    }
    
    /// <summary>
    /// 현재 위치 기준으로 가장 가까운 유효 체인 타겟을 탐색하여 방향 전환
    /// 조건: 미타격 + 벽에 가려지지 않음 + chainRadius 이내
    /// </summary>
    /// <returns>유효 타겟을 찾아 방향 전환 성공하면 true</returns>
    private bool FindNextChainTarget()
    {
        int enemyLayer   = LayerMask.GetMask("Enemy");
        int obstacleLayer = LayerMask.GetMask("Wall");  // 벽/지형 레이어
        
        Collider2D[] candidates = Physics2D.OverlapCircleAll(transform.position, chainRadius, enemyLayer);
        
        Transform bestTarget    = null;
        float     bestDistance  = float.MaxValue;
        
        foreach (Collider2D candidate in candidates)
        {
            if (candidate == null) continue;
            
            // 이미 타격한 적 제외
            if (chainHitTargets.Contains(candidate.gameObject)) continue;
            
            // 죽은 SimpleMob 제외
            SimpleMob mob = candidate.GetComponent<SimpleMob>();
            if (mob != null && mob.IsDead) continue;
            
            // 벽 차단 검사 — Linecast로 중간에 장애물이 있는지 확인
            if (obstacleLayer != 0)
            {
                RaycastHit2D wallHit = Physics2D.Linecast(transform.position, candidate.transform.position, obstacleLayer);
                if (wallHit.collider != null) continue; // 벽에 가려진 적 제외
            }
            
            float dist = Vector2.Distance(transform.position, candidate.transform.position);
            if (dist < bestDistance)
            {
                bestDistance = dist;
                bestTarget   = candidate.transform;
            }
        }
        
        if (bestTarget == null)
        {
            if (showDebugLogs) Debug.Log("⛓️ [Projectile] 유효한 다음 체인 타겟 없음 → 소멸");
            return false;
        }
        
        // 방향 전환: transform.rotation 갱신 → MoveStraight()가 다음 프레임부터 새 방향으로 이동
        Vector2 newDir  = (bestTarget.position - transform.position).normalized;
        float   newAngle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);
        
        // 사거리 리셋 — 새 타겟까지 날아가야 하므로 startPosition 갱신
        startPosition = transform.position;
        
        if (showDebugLogs)
            Debug.Log($"⛓️ [Projectile] 체인 방향 전환 → {bestTarget.name} (거리 {bestDistance:F2}, 배율 {currentChainMultiplier:F2})");
        
        return true;
    }
    
    #endregion
    
    #region 🧱 벽 충돌 시스템
    
    /// <summary>
    /// 벽 충돌 처리
    /// </summary>
    private void OnHitWall(Collider2D wall)
    {
        // 🎵 CueSystem: 벽 충돌 이펙트 + 사운드 재생
        Vector3 hitPosition = transform.position;
        Vector3 hitNormal = (hitPosition - wall.transform.position).normalized;
        
        var context = new CueSystem.CueContext
        {
            position = hitPosition,
            rotation = transform.rotation,
            normal = hitNormal,
            facingDir = transform.right,
            follow = null,
            actorType = CueSystem.ActorType.Player,
            surfaceType = CueSystem.SurfaceType.Stone,
            magnitude = 1.0f,
            isCritical = false,
            scale = 1.0f
        };
        
        CueSystem.CueEmitter.Emit("projectile.hit.wall", "Player", context);
        
        // ⚠️ Fallback: CueSystem 실패 시 기존 VFX 사용
        if (particleOnHitPrefabVFX != null && CueSystem.CuePlayer.Instance == null)
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