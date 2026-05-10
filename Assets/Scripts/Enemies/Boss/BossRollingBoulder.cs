using System.Collections;
using UnityEngine;
using CueSystem;

/// <summary>
/// 보스 스킬 전용 구르는 바위 컴포넌트.
/// 기존 스테이지 기믹 RollingBoulder와 완전히 분리된 독립 구현.
///
/// 동작 구조:
///   BossBoulderSkill.Execute() → Initialize() 호출
///   → Dynamic Rigidbody2D + 초기 velocity 부여
///   → 매 프레임 원형 전투 영역 경계 감지 → Vector2.Reflect 반사
///   → 플레이어 충돌(TriggerStay) → 데미지 + 넉백
///   → TakeDamage() → HP 0 시 Large=Split(), Small=Die()
///   → BossBoulderSkill.OnBoulderDestroyed() 콜백으로 완료 알림
///
/// 분열 방식:
///   Large 바위: autoSplitTime 경과 시 자동 분열 → Small 바위 스폰
///              플레이어가 HP 0 만들면 분열 없이 즉시 소멸 (공략 보상)
///   Small 바위: HP 0이면 그냥 소멸 (재분열 없음)
/// </summary>
public class BossRollingBoulder : MonoBehaviour
{
    // ── 바위 설정 ────────────────────────────────────────────────────────────

    [Header("🪨 바위 타입")]
    [Tooltip("true = Small 바위 (분열 불가), false = Large 바위 (분열 가능)")]
    [SerializeField] private bool isSmall = false;

    [Header("📊 스탯")]
    [SerializeField] private int maxHP = 150;
    [SerializeField] private int damage = 30;
    [Tooltip("플레이어에게 가하는 넉백 힘")]
    [SerializeField] private float knockbackThrust = 10f;
    [Tooltip("연속 피격 방지 쿨타임 (초)")]
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("💥 분열 설정 (Large 전용)")]
    [Tooltip("스폰 후 자동 분열까지 대기 시간 (초)\n" +
             "이 시간이 경과하면 Large 바위가 Small 바위로 자동 분열됨.\n" +
             "플레이어가 그 전에 HP 0으로 만들면 분열 없이 소멸.")]
    [SerializeField] private float autoSplitTime = 3f;
    [Tooltip("분열 시 생성할 Small 바위 프리팹")]
    [SerializeField] private GameObject smallBoulderPrefab;
    [Tooltip("분열 시 생성할 Small 바위 수")]
    [Range(1, 4)]
    [SerializeField] private int splitCount = 2;
    [Tooltip("분열 퍼짐 각도 범위 (±각도의 절반). 120 = ±60° 범위로 부채꼴 분열")]
    [Range(60f, 180f)]
    [SerializeField] private float splitSpreadAngle = 120f;
    [Tooltip("Small 바위 초기 속도 배율 (Large 속도 기준)")]
    [SerializeField] private float splitSpeedMultiplier = 1.3f;

    [Header("🎯 충돌 설정")]
    [SerializeField] private LayerMask playerLayer;

    [Header("🔄 경계 반사 설정")]
    [Tooltip("경계 보정 여유값 — 경계 안쪽으로 밀어넣는 거리")]
    [SerializeField] private float boundaryPushback = 0.15f;
    [Tooltip("반사 후 속도 유지 비율 (1.0 = 완전 탄성)")]
    [Range(0.8f, 1.0f)]
    [SerializeField] private float restitution = 1.0f;

    [Header("🎬 애니메이션")]
    [Tooltip("자식 오브젝트의 SpriteRenderer (null이면 자동 탐색)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("자식 오브젝트의 Animator (null이면 자동 탐색)")]
    [SerializeField] private Animator animator;
    [Tooltip("애니메이션 갱신 최소 속도 — 이 값 미만이면 마지막 방향 유지")]
    [SerializeField] private float minSpeedForDirection = 0.1f;
    [Tooltip("Split 트리거 재생 후 Small 바위 스폰까지 대기 시간 (초). 0이면 즉시 분열.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float splitDelay = 0.15f;

    [Header("💨 먼지 이펙트")]
    [Tooltip("굴링 먼지 ParticleSystem (자식 오브젝트)\n" +
             "ParticleSystem > Main > Simulation Space 를 World 로 설정할 것")]
    [SerializeField] private ParticleSystem dustEffect;
    [Tooltip("먼지 이펙트가 활성화되는 최소 속도")]
    [SerializeField] private float dustMinSpeed = 1f;
    [Tooltip("최대 속도일 때 먼지 방출량 (particles/sec)")]
    [SerializeField] private float dustMaxEmissionRate = 30f;


    // ── 내부 참조 ─────────────────────────────────────────────────────────────

    private Rigidbody2D rb;

    // ── 런타임 상태 ───────────────────────────────────────────────────────────

    private int currentHP;
    private bool isDead = false;
    private float lastDamageTime = -99f;

    // 전투 영역 정보 (Initialize 시 BossBoulderSkill이 전달)
    private Vector2 arenaCenter;
    private float arenaRadius;

    // 현재 속도 크기 저장 (반사 후에도 속도 유지용)
    private float currentSpeed;

    // 스킬 엔트리 (데미지 계산용)
    private BossSkillEntry skillEntry;

    // 완료 알림용 오너 참조
    private BossBoulderSkill ownerSkill;

    // 애니메이션 파라미터 해시 (성능 최적화)
    private static readonly int MoveXHash      = Animator.StringToHash("moveX");
    private static readonly int MoveYHash      = Animator.StringToHash("moveY");
    private static readonly int RollSpeedHash  = Animator.StringToHash("rollSpeed");
    private static readonly int SpawnTrigger   = Animator.StringToHash("Spawn");
    private static readonly int SplitTrigger   = Animator.StringToHash("Split");
    private static readonly int DieTrigger     = Animator.StringToHash("Die");

    // 마지막 방향 (히스테리시스용)
    private Vector2 lastAnimDirection = Vector2.right;

    // ── 초기화 ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 자동 탐색: Inspector에서 연결하지 않았을 때 자식에서 검색
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (dustEffect == null)
            dustEffect = GetComponentInChildren<ParticleSystem>();
    }

    /// <summary>
    /// BossBoulderSkill.Execute()에서 호출.
    /// 물리 바위를 초기화하고 지정 방향으로 발사한다.
    /// </summary>
    /// <param name="center">전투 영역 중심 월드 좌표</param>
    /// <param name="radius">전투 영역 반경</param>
    /// <param name="initialVelocity">초기 velocity</param>
    /// <param name="entry">스킬 엔트리 (데미지 배율 등)</param>
    /// <param name="owner">완료 콜백 수신자 (null 허용 — 테스트 환경)</param>
    public void Initialize(
        Vector2 center, float radius,
        Vector2 initialVelocity,
        BossSkillEntry entry,
        BossBoulderSkill owner = null)
    {
        arenaCenter = center;
        arenaRadius = radius;
        skillEntry  = entry;
        ownerSkill  = owner;

        currentHP       = maxHP;
        isDead          = false;
        lastDamageTime  = -99f;
        currentSpeed    = initialVelocity.magnitude;

        rb.velocity = initialVelocity;

        // Large 바위만 자동 분열 타이머 시작
        if (!isSmall)
            StartCoroutine(AutoSplitCountdown());
    }

    // ── 업데이트 ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (isDead) return;

        UpdateAnimationDirection();
        UpdateDustEffect();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        ReflectIfOutOfBounds();
    }

    /// <summary>
    /// 전투 영역 경계를 벗어났을 때 velocity를 반사한다.
    /// Unity Physics2D 대신 코드로 처리하므로 CircleCollider는 Trigger로 설정한다.
    /// </summary>
    private void ReflectIfOutOfBounds()
    {
        Vector2 pos        = rb.position;
        Vector2 toCenter   = arenaCenter - pos;
        float   dist       = toCenter.magnitude;

        if (dist < arenaRadius) return;

        // 경계 법선 (중심 → 바위 방향)
        Vector2 normal = -toCenter.normalized;

        // 현재 velocity 반사
        Vector2 reflected = Vector2.Reflect(rb.velocity, normal);

        // 속도 크기 보존 (수치 오차 방지)
        if (reflected.magnitude > 0.01f)
            rb.velocity = reflected.normalized * currentSpeed * restitution;

        // 위치를 경계 안쪽으로 보정
        Vector2 boundaryPoint = arenaCenter + normal * (arenaRadius - boundaryPushback);
        rb.position = boundaryPoint;
    }

    // ── 애니메이션 방향 ───────────────────────────────────────────────────────

    /// <summary>
    /// velocity를 기반으로 Animator(moveX/moveY/rollSpeed)와 SpriteRenderer(flipX)를 갱신.
    /// PlayerController.UpdateAnimAndFlipFromVelocity와 동일한 방식:
    ///   - rb.velocity.normalized를 변환 없이 그대로 사용
    ///   - 매 프레임 즉시 갱신 (히스테리시스 없음)
    ///   - x < 0 이면 flipX = true, moveX = Abs(x)
    ///   - x > 0 이면 flipX = false, moveX = x
    ///   - x ≈ 0 이면 moveX = 0 (수직 이동)
    /// </summary>
    private void UpdateAnimationDirection()
    {
        if (animator == null) return;

        float speed = rb.velocity.magnitude;

        // rollSpeed는 항상 갱신 (구르는 애니메이션 속도)
        animator.SetFloat(RollSpeedHash, speed);

        // 속도가 너무 낮으면 방향 유지 (멈출 때 마지막 방향 유지)
        if (speed < minSpeedForDirection) return;

        // 플레이어와 동일: 월드 velocity 방향을 변환 없이 그대로 사용
        Vector2 dir = rb.velocity.normalized;
        lastAnimDirection = dir;

        // ⬅️ 좌측: flipX = true, moveX = Abs(x)
        if (dir.x < -0.1f)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = true;
            animator.SetFloat(MoveXHash, Mathf.Abs(dir.x));
            animator.SetFloat(MoveYHash, dir.y);
        }
        // ➡️ 우측: flipX = false, moveX = x
        else if (dir.x > 0.1f)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = false;
            animator.SetFloat(MoveXHash, dir.x);
            animator.SetFloat(MoveYHash, dir.y);
        }
        // ⬆️⬇️ 수직: moveX = 0
        else
        {
            animator.SetFloat(MoveXHash, 0f);
            animator.SetFloat(MoveYHash, dir.y);
        }
    }

    // ── 먼지 이펙트 ───────────────────────────────────────────────────────────

    /// <summary>
    /// 매 프레임 이동 방향 반대쪽으로 먼지 파티클 방출 방향을 회전시킨다.
    /// ParticleSystem은 Simulation Space = World 여야 이미 나온 파티클이 흘러가지 않는다.
    /// </summary>
    private void UpdateDustEffect()
    {
        if (dustEffect == null) return;

        float speed = rb.velocity.magnitude;
        var emission = dustEffect.emission;

        if (speed < dustMinSpeed)
        {
            emission.rateOverTime = 0f;
            return;
        }

        // 이동 방향 각도 → 반대 방향(+180°)에서 먼지가 나오도록
        float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
        dustEffect.transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);

        // 속도 비례로 방출량 조절 (currentSpeed가 기준 최대값)
        float speedRatio = currentSpeed > 0.01f ? Mathf.Clamp01(speed / currentSpeed) : 1f;
        emission.rateOverTime = dustMaxEmissionRate * speedRatio;
    }

    // ── 플레이어 충돌 데미지 ──────────────────────────────────────────────────

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        if (Time.time - lastDamageTime < damageCooldown) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        lastDamageTime = Time.time;

        int dmg = CalculateDamage();

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        playerHealth?.TakeDamage(dmg, transform);

        Knockback knockback = other.GetComponent<Knockback>();
        knockback?.GetKnockedBack(transform, knockbackThrust);

        CueEmitter.EmitAt(other.transform.position, "hit.player.normal", "Player");
    }

    // ── 피격 (플레이어 공격) ──────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        DamageSource damageSource = other.GetComponent<DamageSource>();
        if (damageSource == null) return;

        int dmg = Mathf.RoundToInt(damageSource.GetCurrentBaseDamage());
        TakeDamage(dmg);
    }

    /// <summary>
    /// 플레이어 공격에 맞아 HP 감소.
    /// HP가 0 이하가 되면 isSmall 여부에 따라 Split 또는 Die 처리.
    /// </summary>
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= dmg;

        DamageNumberManager.Instance?.ShowDamage(transform.position, dmg, false, transform);
        CueEmitter.EmitAt(transform.position, "boulder.hit.damage", "Stage");

        if (currentHP <= 0)
        {
            isDead = true;
            // Large/Small 모두 플레이어가 HP 0으로 만들면 분열 없이 소멸 (공략 보상)
            // Large의 자동 분열 코루틴은 StopMovement 전에 StopAllCoroutines로 취소
            StopAllCoroutines();
            StopMovement();
            Die();
        }
    }

    // ── 분열 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Large 바위 전용 자동 분열 타이머.
    /// autoSplitTime 경과 후 SplitRoutine() 호출.
    /// 플레이어가 먼저 HP 0으로 만들면 TakeDamage()의 StopAllCoroutines()에 의해 취소됨.
    /// </summary>
    private IEnumerator AutoSplitCountdown()
    {
        yield return new WaitForSeconds(autoSplitTime);

        if (isDead) yield break;

        isDead = true;
        StopMovement();
        yield return StartCoroutine(SplitRoutine());
    }

    /// <summary>
    /// 자동 분열 또는 ForceDestroy 시 호출.
    /// Split 애니메이션 재생 → splitDelay 대기 → Small 바위 분열 스폰.
    /// </summary>
    private IEnumerator SplitRoutine()
    {
        // Split 애니메이션 트리거 (클립이 없어도 오류 없음)
        if (animator != null)
            animator.SetTrigger(SplitTrigger);

        CueEmitter.EmitAt(transform.position, "boulder.split", "Stage");

        // 분열 연출 대기
        if (splitDelay > 0f)
            yield return new WaitForSeconds(splitDelay);

        SpawnSmallBoulders();
        NotifyDestroyed();
        Destroy(gameObject);
    }

    /// <summary>
    /// Small 바위를 현재 진행 방향 기준 부채꼴로 스폰.
    /// </summary>
    private void SpawnSmallBoulders()
    {
        if (smallBoulderPrefab == null)
        {
            Dbg.LogWarning("[BossRollingBoulder] smallBoulderPrefab이 없습니다. Split을 건너뜁니다.");
            return;
        }

        // 분열 기준 방향: 사망 직전 이동 방향 사용 (StopMovement 전에 저장된 값)
        Vector2 baseDir = lastAnimDirection.magnitude > 0.01f ? lastAnimDirection : Vector2.right;
        float splitSpeed = currentSpeed * splitSpeedMultiplier;

        for (int i = 0; i < splitCount; i++)
        {
            // splitSpreadAngle 범위 안에 균등 배치
            float t           = splitCount > 1 ? (float)i / (splitCount - 1) : 0.5f;
            float angleOffset = Mathf.Lerp(-splitSpreadAngle * 0.5f, splitSpreadAngle * 0.5f, t);
            Vector2 splitDir  = RotateVector(baseDir, angleOffset);

            GameObject obj = Instantiate(smallBoulderPrefab, transform.position, Quaternion.identity);
            BossRollingBoulder small = obj.GetComponent<BossRollingBoulder>();
            if (small != null)
            {
                small.Initialize(arenaCenter, arenaRadius, splitDir * splitSpeed, skillEntry, ownerSkill);
                // Small 바위를 오너 스킬 추적 목록에 등록 (타이머 종료/취소 시 ForceDestroy 대상)
                ownerSkill?.RegisterBoulder(small);
            }
            else
            {
                Dbg.LogWarning("[BossRollingBoulder] smallBoulderPrefab에 BossRollingBoulder 컴포넌트가 없습니다.");
                Destroy(obj);
            }
        }
    }

    // ── 소멸 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// HP 소진(Small) 시 소멸.
    /// </summary>
    private void Die()
    {
        if (animator != null)
            animator.SetTrigger(DieTrigger);

        CueEmitter.EmitAt(transform.position, "boulder.destroy", "Stage");
        NotifyDestroyed();
        Destroy(gameObject);
    }

    /// <summary>
    /// BossBoulderSkill 타이머 종료 또는 스킬 강제 취소 시 외부에서 호출.
    /// 즉시 소멸 처리 (오너가 직접 정리 중이므로 콜백 없음).
    /// </summary>
    public void ForceDestroy()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        StopMovement();
        CueEmitter.EmitAt(transform.position, "boulder.destroy", "Stage");
        Destroy(gameObject);
    }

    /// <summary>
    /// 이동을 즉시 정지시킨다 (사망/취소 시 호출).
    /// </summary>
    private void StopMovement()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;

        if (animator != null)
            animator.SetFloat(RollSpeedHash, 0f);

        if (dustEffect != null)
        {
            var emission = dustEffect.emission;
            emission.rateOverTime = 0f;
        }
    }

    // ── 유틸리티 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 오너 스킬에 이 바위가 소멸했음을 알린다.
    /// </summary>
    private void NotifyDestroyed()
    {
        ownerSkill?.OnBoulderDestroyed(this);
    }

    /// <summary>
    /// SkillEntry의 DamageMultiplier × skillScaleMultiplier 기반 데미지 계산.
    /// SkillEntry가 없으면 Inspector의 damage 값 그대로 사용.
    /// </summary>
    private int CalculateDamage()
    {
        if (skillEntry?.skillData == null)
            return damage;

        float multiplier = skillEntry.skillData.DamageMultiplier * skillEntry.skillScaleMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
    }

    /// <summary>
    /// 2D 벡터를 지정 각도(도)로 회전.
    /// </summary>
    private static Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    /// <summary>현재 이동 방향 반환 (외부 참조용).</summary>
    public Vector2 GetMoveDirection()
    {
        if (rb == null || rb.velocity.magnitude < 0.01f) return Vector2.zero;
        return rb.velocity.normalized;
    }

    /// <summary>현재 속도 크기 반환 (외부 참조용).</summary>
    public float GetSpeed() => rb != null ? rb.velocity.magnitude : 0f;
}
