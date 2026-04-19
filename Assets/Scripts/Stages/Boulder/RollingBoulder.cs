using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using CueSystem;

/// <summary>
/// 구르는 바위 메인 컴포넌트.
///
/// 구조:
///   [RollingBoulder Root]  - 이 스크립트, Rigidbody2D(Kinematic), CircleCollider2D(Trigger)
///     └── [BoulderSprite] - SpriteRenderer, Animator (3종 중 하나 활성화)
///
/// 동작 흐름:
///   Initialize(pathData) 호출
///   → DOPath로 경로 이동 (CatmullRom 곡선)
///   → 이동 중 Animator RollSpeed 파라미터 업데이트 (굴러가는 애니메이션)
///   → DOShakePosition으로 울퉁불퉁 지형 느낌 연출
///   → 플레이어와 충돌 시 데미지 + 넉백
///   → TakeDamage()로 HP 감소 → 0이면 Large는 Split(), Small은 소멸
///   → 도착지점에서 소멸
///
/// 분열(Split) 경로 이탈 방지:
///   분열 직후 Rigidbody2D.AddForce로 좌우 퍼짐 → splitSnapDelay 후
///   Rigidbody2D를 다시 Kinematic으로 전환 + 남은 경로 DOPath 재개
/// </summary>
public class RollingBoulder : MonoBehaviour, ITargetable
{
    // ── 바위 설정 ────────────────────────────────────────────────────────────
    [Header("바위 타입")]
    [Tooltip("true = Small 바위 (분열 불가), false = Large 바위 (분열 가능)")]
    [SerializeField] private bool isSmall = false;

    [Header("스탯")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int damage = 100;
    [Tooltip("플레이어에게 가하는 넉백 힘")]
    [SerializeField] private float knockbackThrust = 12f;
    [Tooltip("충돌 데미지 쿨타임 (연속 피격 방지)")]
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("스프라이트 변형 3종 (자식 Animator 배열)")]
    [Tooltip("3종 바위 Animator. Initialize() 시 랜덤 1개 활성화")]
    [SerializeField] private Animator[] boulderVariants = new Animator[3];

    [Header("분열 프리팹")]
    [Tooltip("분열 시 생성할 SmallBoulder 프리팹")]
    [SerializeField] private GameObject smallBoulderPrefab;

    [Header("레이어")]
    [SerializeField] private LayerMask playerLayer;

    [Header("HP 바 UI")]
    [Tooltip("자식 오브젝트에 배치한 Slider (선택). null이면 HP 바 미표시")]
    [SerializeField] private Slider hpBar;

    [Tooltip("HP가 최대치일 때 HP 바를 숨길지 여부")]
    [SerializeField] private bool hideHpBarAtFull = true;

    // ── 내부 참조 ─────────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator activeAnimator;

    private int currentHP;
    private bool isDead = false;
    private float lastDamageTime = -99f;

    private BoulderPathData pathData;
    private int currentWaypointIndex = 0;   // 분열 시 경로 재개 지점 추적
    private Tweener moveTween;
    private Tweener shakeTween;
    private BoulderSpawner ownerSpawner;    // 소멸 시 카운터 반환용

    private static readonly int RollSpeedParam = Animator.StringToHash("RollSpeed");

    // ── 초기화 ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.None; // Position Freeze 해제 (DOTween 이동 허용)
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 위치 보간
    }

    /// <summary>
    /// BoulderSpawner 또는 Split()에서 호출. 경로 데이터를 받아 이동을 시작한다.
    /// spawner를 전달하면 소멸 시 NotifyBoulderDestroyed()가 자동 호출된다.
    /// </summary>
    public void Initialize(BoulderPathData data, int startWaypointIndex = 0, BoulderSpawner spawner = null)
    {
        pathData = data;
        currentWaypointIndex = startWaypointIndex;
        currentHP = maxHP;
        isDead = false;
        lastDamageTime = -99f;
        ownerSpawner = spawner;

        RefreshHpBar();
        if (hideHpBarAtFull && hpBar != null)
            hpBar.gameObject.SetActive(false);

        ActivateRandomVariant();
        StartMovement();
    }

    private void ActivateRandomVariant()
    {
        foreach (var anim in boulderVariants)
            if (anim != null) anim.gameObject.SetActive(false);

        int pick = Random.Range(0, boulderVariants.Length);
        for (int i = 0; i < boulderVariants.Length; i++)
        {
            if (boulderVariants[i] == null) continue;
            if (i == pick)
            {
                boulderVariants[i].gameObject.SetActive(true);
                activeAnimator = boulderVariants[i];
            }
        }
    }

    // ── 이동 ──────────────────────────────────────────────────────────────────

    private void StartMovement()
    {
        if (pathData == null || pathData.waypoints == null || pathData.waypoints.Length < 2)
        {
            Debug.LogWarning($"[RollingBoulder] {name}: BoulderPathData가 없거나 웨이포인트가 부족합니다.");
            ReturnToPool();
            return;
        }

        // 시작 웨이포인트부터의 남은 경로만 사용
        Vector3[] remainingPath = GetRemainingPath(currentWaypointIndex);
        if (remainingPath.Length < 2)
        {
            ReturnToPool();
            return;
        }

        // 남은 거리 비율로 duration 계산
        float durationRatio = (float)remainingPath.Length / pathData.waypoints.Length;
        float remainingDuration = pathData.duration * durationRatio;

        // 진단 로그: 웨이포인트 좌표 및 현재 위치 출력
        for (int i = 0; i < remainingPath.Length; i++)

        // DOPath: 메인 transform이 경로를 따라 이동
        moveTween = transform.DOPath(remainingPath, remainingDuration, PathType.CatmullRom)
            .SetEase(pathData.easeType)
            .SetOptions(false)
            .OnWaypointChange(OnPassWaypoint)
            .OnComplete(OnReachEnd);

        // 울퉁불퉁 진동: DOShakePosition이 DOPath와 같은 transform을 쓰면 위치가 충돌함
        // → 활성화된 스프라이트 자식(activeAnimator.transform)에만 shake 적용
        if (pathData.bumpStrength > 0f && activeAnimator != null)
        {
            shakeTween = activeAnimator.transform.DOShakePosition(
                remainingDuration,
                new Vector3(pathData.bumpStrength, pathData.bumpStrength * 0.5f, 0f),
                pathData.bumpVibrato,
                90f, false, false
            ).SetLoops(1);
        }

        // 굴러가는 동안 주기적으로 미세 카메라 셰이크 발행
        if (pathData.rollingShakeInterval > 0f)
            StartCoroutine(RollingShakeRoutine(remainingDuration));

        this.Emit("boulder.spawn", "Stage");
    }

    /// <summary>
    /// 이동 중 일정 간격으로 boulder.rolling 이벤트를 발행해 카메라 미세 진동을 일으킨다.
    /// CueProfile에 shakeData만 연결하면 VFX/SFX 없이 셰이크만 발생한다.
    /// </summary>
    private IEnumerator RollingShakeRoutine(float totalDuration)
    {
        float elapsed = 0f;
        float interval = pathData.rollingShakeInterval;

        while (elapsed < totalDuration && !isDead)
        {
            yield return new WaitForSeconds(interval);
            elapsed += interval;

            if (!isDead && moveTween != null && moveTween.IsActive() && moveTween.IsPlaying())
                CueEmitter.EmitAt(transform.position, "boulder.rolling", "Stage");
        }
    }

    private Vector3[] GetRemainingPath(int fromIndex)
    {
        int count = pathData.waypoints.Length - fromIndex;
        if (count < 2) return new Vector3[0];

        Vector3[] path = new Vector3[count];
        for (int i = 0; i < count; i++)
            path[i] = pathData.waypoints[fromIndex + i];
        return path;
    }

    private void OnPassWaypoint(int waypointIndex)
    {
        currentWaypointIndex = waypointIndex;
        CueEmitter.EmitAt(transform.position, "boulder.roll.dust", "Stage");
    }

    private void OnReachEnd()
    {
        CueEmitter.EmitAt(transform.position, "boulder.destroy", "Stage");
        ReturnToPool();
    }

    // ── 애니메이션 ────────────────────────────────────────────────────────────

    private Vector3 prevPosition;

    private void Update()
    {
        if (isDead || activeAnimator == null) return;

        float moveSpeed = (transform.position - prevPosition).magnitude / Time.deltaTime;
        prevPosition = transform.position;

        // Animator RollSpeed 파라미터 업데이트 → 굴러가는 속도 연동
        activeAnimator.SetFloat(RollSpeedParam, moveSpeed);
    }

    // ── 플레이어 충돌 ─────────────────────────────────────────────────────────

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;
        if (Time.time - lastDamageTime < damageCooldown) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        lastDamageTime = Time.time;

        // 플레이어 피격
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damage, transform);

        // 넉백: 바위 진행 방향으로 밀어냄
        Knockback knockback = other.GetComponent<Knockback>();
        if (knockback != null)
            knockback.GetKnockedBack(transform, knockbackThrust);

        // 피격 이펙트/사운드는 피격자(Player) 책임 → Player 도메인 이벤트 키 사용
        CueEmitter.EmitAt(other.transform.position, "hit.player.normal", "Player");
    }

    // ── 피격 (플레이어 공격) ──────────────────────────────────────────────────

    /// <summary>
    /// Barricade와 동일한 패턴: 바위의 트리거 영역에 DamageSource가 진입하면 피격 처리.
    /// DamageSource.OnTriggerEnter2D에서 감지하는 역방향 의존을 제거하고
    /// 바위 스스로 무기를 감지한다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        DamageSource damageSource = other.GetComponent<DamageSource>();
        if (damageSource == null) return;

        int dmg = Mathf.RoundToInt(damageSource.GetCurrentBaseDamage());
        TakeDamage(dmg);
    }

    /// <summary>
    /// 플레이어 무기에 맞았을 때 HP 감소.
    /// </summary>
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHP -= dmg;
        CueEmitter.EmitAt(transform.position, "boulder.hit.damage", "Stage");

        // 데미지 숫자 표시
        DamageNumberManager.Instance?.ShowDamage(transform.position, dmg, false, transform);

        // HP 바 업데이트
        if (hpBar != null)
        {
            if (hideHpBarAtFull) hpBar.gameObject.SetActive(true);
            RefreshHpBar();
        }

        if (currentHP <= 0)
        {
            isDead = true;
            if (hpBar != null) hpBar.gameObject.SetActive(false);
            if (isSmall)
                Die();
            else
                Split();
        }
    }

    private void RefreshHpBar()
    {
        if (hpBar == null) return;
        hpBar.minValue = 0f;
        hpBar.maxValue = maxHP;
        hpBar.value    = Mathf.Max(0, currentHP);
    }

    // ── 분열 ──────────────────────────────────────────────────────────────────

    private void Split()
    {
        if (pathData == null) { ReturnToPool(); return; }

        CueEmitter.EmitAt(transform.position, "boulder.split", "Stage");

        for (int i = 0; i < pathData.splitCount; i++)
        {
            GameObject smallObj = smallBoulderPrefab != null
                ? Instantiate(smallBoulderPrefab, transform.position, Quaternion.identity)
                : GamePoolManager.Instance?.SpawnFromPool("SmallBoulder", transform.position, Quaternion.identity);

            if (smallObj == null) continue;
            RollingBoulder small = smallObj.GetComponent<RollingBoulder>();
            if (small == null) continue;

            switch (pathData.splitMode)
            {
                case SplitMode.Scatter:
                    // 광장: 랜덤 산란 후 scatterDuration 초 뒤 자연 소멸
                    small.StartCoroutine(small.SplitScatterRoutine(
                        pathData.splitScatterForce, pathData.splitSnapDelay,
                        pathData.scatterDuration, pathData.scatterDrag, i, pathData.splitCount));
                    break;

                case SplitMode.OwnPath:
                    // 특수: 전용 경로 중 랜덤 선택
                    if (pathData.smallPathOptions != null && pathData.smallPathOptions.Length > 0)
                    {
                        BoulderPathData chosen = pathData.smallPathOptions[
                            Random.Range(0, pathData.smallPathOptions.Length)];
                        small.StartCoroutine(small.SplitWithOwnPathRoutine(
                            chosen, pathData.splitScatterForce, pathData.splitSnapDelay,
                            i, pathData.splitCount));
                    }
                    else
                    {
                        Debug.LogWarning($"[RollingBoulder] OwnPath 모드인데 smallPathOptions가 비어있습니다. Scatter로 fallback.");
                        small.StartCoroutine(small.SplitScatterRoutine(
                            pathData.splitScatterForce, pathData.splitSnapDelay,
                            pathData.scatterDuration, pathData.scatterDrag, i, pathData.splitCount));
                    }
                    break;

                default: // SplitMode.Corridor
                    // 통로: Large 남은 경로 이어받기
                    small.StartCoroutine(small.SplitAndSnapRoutine(
                        pathData, currentWaypointIndex, i, pathData.splitCount));
                    break;
            }
        }

        ReturnToPool();
    }

    /// <summary>
    /// 산란 소멸 루틴 (Scatter 모드 - 광장).
    /// 경로 없이 랜덤 방향으로 굴러다니다 scatterDuration 초 후 자연 소멸.
    /// </summary>
    public IEnumerator SplitScatterRoutine(
        float scatterForce, float scatterDelay, float scatterDuration, float scatterDrag,
        int index, int total)
    {
        currentHP = maxHP;
        isDead = false;

        ActivateRandomVariant();

        // 랜덤 방향으로 물리 힘 적용
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.drag = scatterDrag;

        float angle = (360f / total) * index + Random.Range(-40f, 40f);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        rb.AddForce(dir * scatterForce, ForceMode2D.Impulse);

        // scatterDelay 동안 굴러다님
        yield return new WaitForSeconds(scatterDelay);

        // RollSpeed 애니메이션 계속 업데이트하면서 대기
        float elapsed = 0f;
        while (elapsed < scatterDuration)
        {
            if (activeAnimator != null)
                activeAnimator.SetFloat(RollSpeedParam, rb.velocity.magnitude);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 소멸
        rb.drag = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        Die();
    }

    /// <summary>
    /// 전용 경로 분열 루틴 (광장 등 넓은 공간).
    /// smallPathOptions가 설정된 경우 사용.
    /// 1. 잠깐 물리 힘으로 퍼짐 (시각적 분열 연출)
    /// 2. splitSnapDelay 후 전용 경로 WP0부터 DOPath 시작
    /// </summary>
    public IEnumerator SplitWithOwnPathRoutine(
        BoulderPathData ownPath, float scatterForce, float snapDelay, int index, int total)
    {
        pathData = ownPath;
        currentHP = maxHP;
        isDead = false;

        ActivateRandomVariant();

        // 1단계: 짧은 물리 퍼짐 (분열 연출)
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;

        float angle = (360f / total) * index + Random.Range(-20f, 20f);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        rb.AddForce(dir * scatterForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(snapDelay);

        // 2단계: Kinematic 복귀 → 전용 경로 WP0부터 시작
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.None;

        Initialize(ownPath, 0);
    }

    /// <summary>
    /// 분열 시 경로 이탈 방지 루틴 (좁은 통로 fallback).
    /// 1. Rigidbody2D Dynamic으로 전환 + 랜덤 방향 힘 적용 (퍼짐)
    /// 2. splitSnapDelay 후 Dynamic → Kinematic 복귀
    /// 3. 남은 경로의 다음 웨이포인트부터 DOPath 재개
    /// </summary>
    public IEnumerator SplitAndSnapRoutine(BoulderPathData data, int fromWaypoint, int index, int total)
    {
        pathData = data;
        currentHP = maxHP;
        isDead = false;

        ActivateRandomVariant();

        // 1단계: Dynamic으로 전환 + 퍼지는 힘
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;

        float angle = (360f / total) * index + Random.Range(-20f, 20f);
        Vector2 scatterDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        rb.AddForce(scatterDir * data.splitScatterForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(data.splitSnapDelay);

        // 2단계: Kinematic 복귀 + 속도 초기화
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.None;

        // 3단계: fromWaypoint의 다음 WP부터 시작 (뒤로 돌아가지 않도록)
        int nextIndex = fromWaypoint + 1;
        if (nextIndex >= data.waypoints.Length - 1)
        {
            // 남은 경로가 없으면 소멸
            ReturnToPool();
            yield break;
        }

        Initialize(data, nextIndex);
    }

    // ── 소멸 ──────────────────────────────────────────────────────────────────

    private void Die()
    {
        CueEmitter.EmitAt(transform.position, "boulder.destroy", "Stage");
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        moveTween?.Kill();
        shakeTween?.Kill();

        if (activeAnimator != null)
            activeAnimator.SetFloat(RollSpeedParam, 0f);

        // 스폰한 Spawner의 활성 카운터 반환
        ownerSpawner?.NotifyBoulderDestroyed();
        ownerSpawner = null;

        if (GamePoolManager.Instance != null)
            GamePoolManager.Instance.ReturnToPool(isSmall ? "SmallBoulder" : "LargeBoulder", gameObject);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        shakeTween?.Kill();
    }

    // ── ITargetable 구현 (오토타게팅 시스템) ──────────────────────────────────
    private TargetOutlineEffect _outlineEffect;

    bool ITargetable.IsAlive() => !isDead;

    EnemyRank ITargetable.GetRank() => EnemyRank.Obstacle;

    Transform ITargetable.GetTransform() => transform;

    void ITargetable.ActivateLockOn()
    {
        if (_outlineEffect == null) _outlineEffect = GetComponentInChildren<TargetOutlineEffect>();
        _outlineEffect?.Activate();
    }

    void ITargetable.DeactivateLockOn()
    {
        _outlineEffect?.Deactivate();
    }
}
