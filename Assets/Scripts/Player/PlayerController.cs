using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem;

/// <summary>
/// 플레이어 공격 타입별 이동 제한 분류
/// </summary>
public enum PlayerAttackType
{
    BasicAttack,  // 기본공격 (80% 감쇠)
    Skill,        // 스킬 (완전정지)
    Dash,         // 대쉬 (50% 감쇠)
    Normal        // 정상 (제한없음)
}

public class PlayerController : MonoBehaviour
{
    // ⭐ 튜토리얼용 대시 이벤트
    public event Action OnDashPerformed;
    
    private bool _seenAnimLogger = false;
    public bool FacingLeft { get { return facingLeft; } }
    public Vector2 Movement { get { return movement; } }

    /// <summary>미니맵 마커 회전에 사용할 플레이어 이동 방향 (정규화된 벡터)</summary>
    public Vector2 FacingDirection { get; private set; } = Vector2.down;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float dashspeed = 4f;
    [SerializeField] private TrailRenderer myTrailRenderer;
    [SerializeField] private Transform weaponCollider;

    // ⭐ 아이소메트릭 디버그 설정 추가
    [Header("🧭 아이소메트릭 방향 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    // 🎯 반응성 튜닝 파라미터
    [SerializeField] private float deadZone = 0.12f;           // 미세 입력 무시
    [SerializeField] private float snapTurnThreshold = -0.2f;  // 역방향 전환 스냅 컷오프( -1 에 가까울수록 강함 )

    // 로그 주기 (초). 필요시 인스펙터에서 조절 가능
    [SerializeField] private float debugLogInterval = 1f;
    private float _nextAnimLogAt = 0f;


    // 🔍 디버깅용 공개 프로퍼티
    public float CurrentMoveSpeed => moveSpeed;
    public float CurrentDashSpeed => dashspeed;

    private PlayerControls playerControls;
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;
    private Knockback knockback;
    private PlayerHealth playerHealth; // ✅ FindObjectOfType 캐시
    private float startingMoveSpeed;

    private bool facingLeft = false;
    private bool isDashing = false;

    // ⭐ 마지막 이동 방향 저장 (새로 추가)
    private Vector2 lastMoveDirection = Vector2.down; // 기본값: 북쪽
    
    // ⭐ Phase 4: 마지막 대시 방향 저장 (Dash용)
    private Vector2 lastDashDirection = Vector2.right; // 기본값: 오른쪽

    // DynamicJoystick 참조
    [Header("조이스틱 입력")]
    public DynamicJoystick fixedJoystick;
    private bool joystickFound = false;

    // 무기/스킬별 레벨 통합 관리
    private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

    // ⚡ 액션 RPG 반응성 설정
    [Header("🎮 액션 RPG 반응성 설정")]
    [SerializeField] private float inputBufferTime = 0.1f;    // 입력 버퍼 시간
    [SerializeField] private float accelerationTime = 0.05f;  // 가속 시간 (0에 가까울수록 즉각적)
    [SerializeField] private float decelerationTime = 0.03f;  // 감속 시간 (0에 가까울수록 즉각적)

    [Header("⚔️ 공격 중 이동 제어")]
    [SerializeField] private bool showMovementDebug = false;

    // 🆕 이동 제어 변수들
    private float movementScale = 1.0f;          // 이동 속도 배율 (0.0 ~ 1.0)
    private bool isMovementLocked = false;       // 완전 이동 차단
    private float defaultMovementScale = 1.0f;   // 기본 배율

    // 🆕 부드러운 전환을 위한 변수들
    private float targetMovementScale = 1.0f;    // 목표 배율
    private float scaleTransitionSpeed = 5.0f;   // 전환 속도

    // 입력 버퍼링 변수
    private Vector2 bufferedInput = Vector2.zero;
    private float lastInputTime = -Mathf.Infinity;

    private void Awake()
    {
        playerControls = new PlayerControls();
        rb = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();
        knockback = GetComponent<Knockback>();
        playerHealth = FindObjectOfType<PlayerHealth>(); // ✅ 한 번만 찾고 캐시
        // 조이스틱 초기화는 Start에서 코루틴으로 처리
        
        // ⭐ Trail Renderer 초기 비활성화
        if (myTrailRenderer != null)
            myTrailRenderer.emitting = false;
    }

    private void Start()
    {
        playerControls.Combat.Dash.performed += _ => Dash();
        startingMoveSpeed = moveSpeed;

        // 조이스틱 찾기 코루틴 시작
        StartCoroutine(FindJoystickCoroutine());

        // ⭐ 안전한 Rigidbody2D 상태 확인
        StartCoroutine(SafeCheckRigidbodyState());

        // 액션 RPG 물리 최적화
        OptimizePhysicsForActionRPG();
        
        // ⭐ Phase 4: 마지막 대시 방향 지속적 업데이트
        StartCoroutine(UpdateLastDashDirectionCoroutine());
    }

    /// <summary>
    /// 안전한 Rigidbody2D 상태 확인 (코루틴으로 지연 실행)
    /// </summary>
    private IEnumerator SafeCheckRigidbodyState()
    {
        yield return new WaitForSeconds(0.1f); // 약간 지연

        try
        {
            if (rb == null)
            {
                Debug.LogError("🔴 [PlayerController] Rigidbody2D가 null입니다!");
                yield break;
            }

            Debug.Log($"🔍 [PlayerController] Rigidbody2D 상태:");
            Debug.Log($"   - isKinematic: {rb.isKinematic}");
            Debug.Log($"   - bodyType: {rb.bodyType}");
            Debug.Log($"   - position: {rb.position}");

            // ⭐ 문제 해결: Kinematic이면 Dynamic으로 변경
            if (rb.isKinematic)
            {
                Debug.LogWarning("🟡 [PlayerController] Rigidbody2D가 Kinematic입니다! Dynamic으로 변경");
                rb.isKinematic = false;
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔴 [PlayerController] Rigidbody2D 상태 확인 에러: {e.Message}");
        }
    }

    /// <summary>
    /// 안전한 조이스틱 찾기 코루틴
    /// </summary>
    private IEnumerator FindJoystickCoroutine()
    {
        float timeout = 5f; // 5초 타임아웃
        float elapsed = 0f;

        while (!joystickFound && elapsed < timeout)
        {
            fixedJoystick = FindObjectOfType<DynamicJoystick>();
            if (fixedJoystick != null)
            {
                joystickFound = true;
                Debug.Log("[PlayerController] 조이스틱을 찾았습니다!");
                break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (!joystickFound)
        {
            Debug.LogWarning("[PlayerController] 조이스틱을 찾을 수 없습니다. 키보드 입력만 사용됩니다.");
        }
    }

    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
            playerControls.Disable();
    }

    private void Update()
    {
        // ⭐ 조이스틱 연결 상태 실시간 체크 (안전하게)
        try
        {
            if ((!joystickFound || fixedJoystick == null) && Time.frameCount % 60 == 0)
            {
                var joystickInScene = FindObjectOfType<DynamicJoystick>();
                if (joystickInScene != null)
                {
                    Debug.Log("[PlayerController] Update에서 조이스틱 재연결 시도");
                    fixedJoystick = joystickInScene;
                    joystickFound = true;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔴 [PlayerController] 조이스틱 체크 에러: {e.Message}");
        }

        PlayerInput();
    }

    private void FixedUpdate()
    {
        // 이동 처리 (즉각 반응)
        MoveFast();

        // 최종 속도를 기준으로 애니/flipX를 "한 번만" 갱신
        UpdateAnimAndFlipFromVelocity(rb != null ? rb.velocity : Vector2.zero);
    }

    public void ReEnableControls()
    {
        playerControls.Disable();
        playerControls.Enable();
    }

    public Transform GetWeaponCollider()
    {
        return weaponCollider;
    }

    private void PlayerInput()
    {
        // 조이스틱 전용: 키보드 입력 제거
        movement = Vector2.zero;

        // 조이스틱이 발견되었고 유효하면 조이스틱 입력 사용
        if (joystickFound && fixedJoystick != null)
        {
            movement = fixedJoystick.Direction;
        }
        else
        {
            // ⭐ 디버그: 1초마다 한 번씩만 로그
            if (Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"[PlayerController] 조이스틱 없음 - joystickFound: {joystickFound}, fixedJoystick: {fixedJoystick}");

                // ⭐ 추가: 실제 씬에 조이스틱이 있는지 확인
                var joystickInScene = FindObjectOfType<DynamicJoystick>();
                Debug.Log($"[PlayerController] 씬에 조이스틱 존재 여부: {joystickInScene != null}");

                // ⭐ 씬에 조이스틱이 있는데 연결 안된 경우 강제 재연결
                if (joystickInScene != null && (!joystickFound || fixedJoystick == null))
                {
                    Debug.Log("[PlayerController] 조이스틱 발견! 강제 재연결 시도");
                    RefreshJoystickReference();
                }
            }
        }

        // ❌ moveX/moveY를 여기서 세팅하지 않습니다 (E5 미러링 충돌 방지)
        // myAnimator.SetFloat("moveX", movement.x);
        // myAnimator.SetFloat("moveY", movement.y);
    }

    // ⚡ 액션 RPG 스타일 즉각 반응 이동 시스템
    private void MoveFast()
    {
        // ⭐ 강제 디버그 - 매 2초마다
        if (Time.frameCount % 120 == 0) 
        {
            Debug.Log($"🔍 [MoveFast] === 이동 시스템 상태 ===");
            Debug.Log($"   movement 입력: ({movement.x:F3}, {movement.y:F3})");
            Debug.Log($"   rb.velocity: ({(rb?.velocity.x ?? 0):F3}, {(rb?.velocity.y ?? 0):F3})");
            Debug.Log($"   joystickFound: {joystickFound}, fixedJoystick: {fixedJoystick != null}");
            // 🆕 이동 제어 상태 로그 추가
            if (showMovementDebug)
            {
                Debug.Log($"   movementScale: {movementScale:F2}, isLocked: {isMovementLocked}");
            }
        }

        // 🆕 이동 잠금 체크 (최우선 체크)
        if (isMovementLocked)
        {
            if (showMovementDebug && Time.frameCount % 60 == 0)
                Debug.Log("🔒 [MoveFast] 이동 잠금됨 - 완전 정지");
            
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        // 🆕 이동 스케일 부드러운 전환
        movementScale = Mathf.MoveTowards(movementScale, targetMovementScale, 
                                          scaleTransitionSpeed * Time.fixedDeltaTime);

        // 상태 체크 (넉백/사망 시 이동 금지)
        if (knockback != null && knockback.GettingKnockedBack)
        {
            if (Time.frameCount % 60 == 0) Debug.Log("❌ [MoveFast] knockback 중단");
            return;
        }
        if (playerHealth != null && playerHealth.isDead)
        {
            if (Time.frameCount % 60 == 0) Debug.Log("❌ [MoveFast] 사망 상태 중단");
            return;
        }
        if (rb == null)
        {
            if (Time.frameCount % 60 == 0) Debug.Log("❌ [MoveFast] rb null 중단");
            return;
        }

        // ✅ 개선: deadZone 필드 사용 (더 작은 값으로 조정)
        float improvedDeadZone = deadZone * 0.5f; // deadZone 필드 사용하되 더 민감하게

        // ⭐ DeadZone 체크 디버그
        float movementMagnitude = movement.sqrMagnitude;
        bool inDeadZone = movementMagnitude < improvedDeadZone * improvedDeadZone;

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🔍 [MoveFast] DeadZone 체크 - magnitude: {movementMagnitude:F4}, threshold: {improvedDeadZone * improvedDeadZone:F4}, inDeadZone: {inDeadZone}");
        }

        // DeadZone: 미세 입력은 0으로 간주
        if (inDeadZone)
        {
            if (Time.frameCount % 60 == 0) Debug.Log("🔍 [MoveFast] DeadZone - 정지 상태");

            // ✅ 개선: 즉시 완전 정지 (관성 제거)
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f; // 회전 관성도 제거

            // 정지 애니메이션 신호는 FixedUpdate에서 일괄 처리
            return;
        }

        if (Time.frameCount % 60 == 0) Debug.Log("🔍 [MoveFast] 이동 로직 진입!");

        // ✅ 개선: 입력 방향 즉시 적용 (정규화 + 스케일링)
        var inputDir = movement.normalized;
        
        // 🆕 이동 스케일 적용
        var effectiveMoveSpeed = moveSpeed * movementScale;
        var targetVelocity = inputDir * effectiveMoveSpeed;
        
        // 🆕 이동 스케일 디버그
        if (showMovementDebug && Time.frameCount % 30 == 0)
        {
            Debug.Log($"⚔️ [MoveFast] 이동 스케일 적용 - 기본속도: {moveSpeed:F1}, 스케일: {movementScale:F2}, 최종속도: {effectiveMoveSpeed:F1}");
        }

        // ✅ 개선: 더 민감한 방향 전환 (액션 게임 스타일)
        float aggressiveSnapThreshold = snapTurnThreshold * 0.5f;

        // 역방향 전환 시 즉시 스냅
        if (rb.velocity.sqrMagnitude > 0.01f)
        {
            float dot = Vector2.Dot(rb.velocity.normalized, inputDir);
            if (dot < aggressiveSnapThreshold)
            {
                rb.velocity = Vector2.zero; // 즉시 리셋
            }
        }

        // ✅ 핵심 개선: 즉각적인 속도 적용 (관성 완전 제거)
        rb.velocity = targetVelocity;

        // 미니맵 마커 방향 갱신
        if (targetVelocity.sqrMagnitude > 0.01f)
            FacingDirection = targetVelocity.normalized;

        // ✅ 추가: 물리 드래그 동적 조정 (더 반응적으로)
        rb.drag = movement.sqrMagnitude > 0.01f ? 0f : 15f; // 이동 중: 드래그 0, 정지 시: 높은 드래그

        // ⭐ (중요) 여기서는 Animator/flipX를 건드리지 않는다.
        // 애니/flipX는 FixedUpdate 마지막에 UpdateAnimAndFlipFromVelocity()로 일원화 처리.
    }

    /// <summary>
    /// 🎮 액션 RPG 스타일 입력 처리 (버퍼링 + 즉각 반응)
    /// </summary>
    private void ProcessActionRPGInput()
    {
        // 현재 입력이 있으면 버퍼에 저장
        if (movement.sqrMagnitude > 0.01f)
        {
            bufferedInput = movement;
            lastInputTime = Time.time;
        }

        // 버퍼 시간 내의 입력 사용
        if (Time.time - lastInputTime <= inputBufferTime)
        {
            movement = bufferedInput;
        }
        else
        {
            // 버퍼 시간 초과 시 입력 초기화
            bufferedInput = Vector2.zero;
            movement = Vector2.zero;
        }
    }

    /// <summary>
    /// 🚀 부드럽지만 즉각적인 가속/감속 시스템 (미사용/참고용)
    /// </summary>
    private void MoveFastWithSmoothing()
    {
        if (knockback != null && knockback.GettingKnockedBack) return;
        if (playerHealth != null && playerHealth.isDead) return;
        if (rb == null) return;

        ProcessActionRPGInput();

        Vector2 targetVelocity = Vector2.zero;

        if (movement.sqrMagnitude > 0.05f * 0.05f)
        {
            targetVelocity = movement.normalized * moveSpeed;
            float accelerationRate = moveSpeed / Mathf.Max(accelerationTime, 0.01f);
            rb.velocity = Vector2.MoveTowards(rb.velocity, targetVelocity, accelerationRate * Time.fixedDeltaTime);
        }
        else
        {
            float decelerationRate = moveSpeed / Mathf.Max(decelerationTime, 0.01f);
            rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, decelerationRate * Time.fixedDeltaTime);
        }

        // 애니/flipX는 UpdateAnimAndFlipFromVelocity에서 처리
    }

    /// <summary>
    /// rb.velocity를 기준으로 5방향(E5) + flipX 방식으로
    /// Animator 파라미터(moveX, moveY, speed, isMoving)와 SpriteRenderer.flipX를 "단 한 곳에서" 갱신
    /// </summary>
    private void UpdateAnimAndFlipFromVelocity(Vector2 velocity)
    {
        if (myAnimator == null || mySpriteRender == null)
        {
            if (Time.frameCount % 60 == 0)
                Debug.LogError("🔴 [UpdateAnim] myAnimator 또는 mySpriteRender가 null입니다!");
            return;
        }

        float speed = velocity.magnitude;
        myAnimator.SetFloat("speed", speed);
        myAnimator.SetBool("isMoving", speed > 0.01f);

        // ⭐ 항상 출력되는 기본 디버깅
        if (Time.frameCount % 30 == 0) // 0.5초마다
        {
            Debug.Log($"🔍 [UpdateAnim] === 기본 정보 ===");
            Debug.Log($"   velocity: ({velocity.x:F3}, {velocity.y:F3}) | speed: {speed:F3}");
            Debug.Log($"   lastMoveDirection: ({lastMoveDirection.x:F3}, {lastMoveDirection.y:F3})");
            Debug.Log($"   isMoving: {speed > 0.01f} | 정지조건: {speed < 0.1f}");
        }

        if (speed < 0.1f)
        {
            // ⭐ 정지 상태 상세 디버깅
            Debug.Log($"🛑 [IDLE] === 정지 상태 진입 ===");
            Debug.Log($"   speed: {speed:F3} < 0.1f (정지 조건 만족)");
            Debug.Log($"   현재 lastMoveDirection: ({lastMoveDirection.x:F3}, {lastMoveDirection.y:F3})");
            
            // ✅ 수정: 정지 시 마지막 방향 유지
            Vector2 idleDirection = lastMoveDirection.normalized;
            Debug.Log($"   정규화된 idleDirection: ({idleDirection.x:F3}, {idleDirection.y:F3})");
            
            // ⬅️ 좌측 방향이면 flipX + 양수 변환
            if (idleDirection.x < -0.1f)
            {
                Debug.Log($"   🔄 좌측 처리: x={idleDirection.x:F3} < -0.1");
                mySpriteRender.flipX = true;
                myAnimator.SetFloat("moveX", Mathf.Abs(idleDirection.x));
                myAnimator.SetFloat("moveY", idleDirection.y);
                Debug.Log($"   설정값: flipX=true, moveX={Mathf.Abs(idleDirection.x):F3}, moveY={idleDirection.y:F3}");
            }
            // ➡️ 우측 방향이면 그대로
            else if (idleDirection.x > 0.1f)
            {
                Debug.Log($"   ➡️ 우측 처리: x={idleDirection.x:F3} > 0.1");
                mySpriteRender.flipX = false;
                myAnimator.SetFloat("moveX", idleDirection.x);
                myAnimator.SetFloat("moveY", idleDirection.y);
                Debug.Log($"   설정값: flipX=false, moveX={idleDirection.x:F3}, moveY={idleDirection.y:F3}");
            }
            // ⬆️⬇️ 수직 방향
            else
            {
                Debug.Log($"   ⬆️⬇️ 수직 처리: x={idleDirection.x:F3} (-0.1~0.1 범위)");
                myAnimator.SetFloat("moveX", 0f);
                myAnimator.SetFloat("moveY", idleDirection.y);
                Debug.Log($"   설정값: moveX=0.0, moveY={idleDirection.y:F3}, flipX 유지");
            }
            
            // ⭐ 설정 후 실제 Animator 값 확인
            Debug.Log($"🎬 [IDLE] 실제 Animator 설정값:");
            Debug.Log($"   moveX: {myAnimator.GetFloat("moveX"):F3}");
            Debug.Log($"   moveY: {myAnimator.GetFloat("moveY"):F3}");
            Debug.Log($"   speed: {myAnimator.GetFloat("speed"):F3}");
            Debug.Log($"   isMoving: {myAnimator.GetBool("isMoving")}");
            Debug.Log($"   flipX: {mySpriteRender.flipX}");
            
            return;
        }

        // ✅ 이동 중: 현재 방향 저장 + 애니메이션 적용
        Vector2 dir = velocity.normalized;
        Vector2 previousLastMove = lastMoveDirection; // 이전 값 저장
        lastMoveDirection = dir; // ⭐ 마지막 방향 업데이트

        // ⭐ 이동 중 디버깅
        if (Time.frameCount % 30 == 0) // 0.5초마다
        {
            Debug.Log($"🏃 [MOVING] === 이동 상태 ===");
            Debug.Log($"   dir: ({dir.x:F3}, {dir.y:F3})");
            Debug.Log($"   lastMoveDirection 업데이트: ({previousLastMove.x:F3}, {previousLastMove.y:F3}) → ({lastMoveDirection.x:F3}, {lastMoveDirection.y:F3})");
        }

        // ⬅️ 좌측: flipX = true, moveX는 양수(Abs)로 (오른쪽 전용 5방향 블렌드 사용)
        if (dir.x < -0.1f)
        {
            mySpriteRender.flipX = true;
            facingLeft = true;

            myAnimator.SetFloat("moveX", Mathf.Abs(dir.x)); // ★★ 핵심: 양수
            myAnimator.SetFloat("moveY", dir.y);            // Y는 그대로
            
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"🏃 [MOVING] 좌측: moveX={Mathf.Abs(dir.x):F3}, moveY={dir.y:F3}, flipX=true");
            }
        }
        // ➡️ 우측: flipX = false, 파라미터 그대로
        else if (dir.x > 0.1f)
        {
            mySpriteRender.flipX = false;
            facingLeft = false;

            myAnimator.SetFloat("moveX", dir.x);
            myAnimator.SetFloat("moveY", dir.y);
            
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"🏃 [MOVING] 우측: moveX={dir.x:F3}, moveY={dir.y:F3}, flipX=false");
            }
        }
        // ⬆️⬇️ 수직 이동: flipX 유지, X=0
        else
        {
            myAnimator.SetFloat("moveX", 0f);
            myAnimator.SetFloat("moveY", dir.y);
            
            if (Time.frameCount % 30 == 0)
            {
                Debug.Log($"🏃 [MOVING] 수직: moveX=0.0, moveY={dir.y:F3}, flipX 유지");
            }
        }
    }

    /// <summary>
    /// (unused) 방향 보정은 UpdateAnimAndFlipFromVelocity로 통합
    /// </summary>
    private void AdjustPlayerFacingDirection() { }

    /// <summary>
    /// (unused) 좌우 미러링/애니 파라미터는 UpdateAnimAndFlipFromVelocity에서만 처리
    /// </summary>
    private void UpdateIsometricDirection(Vector2 normalizedMovement) { }

    private void Dash()
    {
        if (!isDashing)
        {
            // ⭐ 대시 방향 결정 (마지막 이동 방향 사용)
            Vector2 dashDirection;
            if (movement.magnitude > 0.1f)
            {
                // 이동 중: 현재 이동 방향
                dashDirection = movement.normalized;
                lastDashDirection = dashDirection; // 저장
            }
            else
            {
                // 정지 상태: 마지막 저장된 방향 사용
                dashDirection = lastDashDirection;
            }
            
            // ⭐ 대시 애니메이션 트리거 추가
            var animController = GetComponent<PlayerAnimationController>();
            if (animController != null)
            {
                animController.TriggerDash(dashDirection);
            }

            isDashing = true;
            dashCooldownStartTime = Time.time;
            moveSpeed += dashspeed;
            if (myTrailRenderer != null)
                myTrailRenderer.emitting = true;
            
            // ⭐ CueSystem으로 Dash 이펙트 발행
            float angle = Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg + 180f;  // 뒤로 이펙트
            
            var context = new CueContext
            {
                position = transform.position,
                rotation = Quaternion.Euler(0, 0, angle),
                actorType = ActorType.Player,
                magnitude = 1.5f,
                surfaceType = SurfaceType.Default,
                facingDir = dashDirection,
                follow = transform  // 캐릭터 따라다님
            };
            
            bool cueSuccess = CueEmitter.Emit("player.dash.start", "Player", context);
            
            if (showDebugLogs)
                Debug.Log($"🏃 [Dash] 이펙트 발행 → {cueSuccess}, 방향: {dashDirection}, 각도: {angle:F1}°");
            
            // ⭐ 튜토리얼용 대시 이벤트 발생
            OnDashPerformed?.Invoke();
            
            StartCoroutine(EndDashRoutine());
        }
    }

    /// <summary>
    /// 🆕 외부에서 호출 가능한 대시 메서드 (모바일 버튼용)
    /// </summary>
    public void PerformDash()
    {
        Debug.Log("[PlayerController] 대시 실행 요청");
        Dash();
    }

    // Dash Radial용 쿨다운 추적
    private float dashCooldownStartTime = -999f;
    private const float DashActiveDuration = 0.35f;
    private const float DashCooldownDuration = 1f;
    
    /// <summary>
    /// Dash Radial UI용: (잔여시간, 총쿨다운) 반환
    /// </summary>
    public void GetDashCooldownInfo(out float remaining, out float total)
    {
        total = DashActiveDuration + DashCooldownDuration; // 1.35f
        if (!isDashing)
        {
            remaining = 0f;
        }
        else
        {
            float elapsed = Time.time - dashCooldownStartTime;
            remaining = Mathf.Max(0f, total - elapsed);
        }
    }
    
    private IEnumerator EndDashRoutine()
    {
        yield return new WaitForSecondsRealtime(DashActiveDuration);
        moveSpeed = startingMoveSpeed;
        if (myTrailRenderer != null)
            myTrailRenderer.emitting = false;
        
        yield return new WaitForSecondsRealtime(DashCooldownDuration);
        isDashing = false;
    }

    // Bow, Sword 등 스킬/무기 이름으로 레벨 조회
    public int GetSkillLevel(string skillName)
    {
        if (skillLevels.ContainsKey(skillName))
            return skillLevels[skillName];
        return 0; // 기본값
    }

    // Bow, Sword 등 스킬/무기 이름으로 레벨 설정
    public void SetSkillLevel(string skillName, int level)
    {
        skillLevels[skillName] = level;
    }

    /// <summary>
    /// 외부에서 조이스틱 참조를 다시 설정할 수 있는 메서드 (강제 재연결)
    /// </summary>
    public void RefreshJoystickReference()
    {
        // ⭐ 핵심 수정: 무조건 강제로 초기화 후 재탐색
        joystickFound = false;
        fixedJoystick = null;
        StartCoroutine(FindJoystickCoroutine());

        Debug.Log("[PlayerController] 조이스틱 강제 재연결 시도 - joystickFound를 false로 초기화");
    }

    // 🔧 클래스별 능력치 적용용 공개 메서드 추가
    public void SetMoveSpeed(float newMoveSpeed)
    {
        moveSpeed = newMoveSpeed;
        startingMoveSpeed = newMoveSpeed;
        Debug.Log($"🔧 [PlayerController] moveSpeed 직접 설정: {newMoveSpeed}");
    }

    public void SetDashSpeed(float newDashSpeed)
    {
        dashspeed = newDashSpeed;
        Debug.Log($"🔧 [PlayerController] dashSpeed 직접 설정: {newDashSpeed}");
    }

    /// <summary>
    /// 🎯 PlayerRuntimeStats에서 이동속도 동기화
    /// </summary>
    public void SyncWithRuntimeStats()
    {
        var playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerRuntimeStats != null)
        {
            float newMoveSpeed = playerRuntimeStats.FinalMoveSpeed;
            float newDashSpeed = playerRuntimeStats.FinalMoveSpeed * 2f; // 대시는 2배

            SetMoveSpeed(newMoveSpeed);
            SetDashSpeed(newDashSpeed);

            Debug.Log($"🎯 [PlayerController] PlayerRuntimeStats와 동기화: 이동속도 {newMoveSpeed:F1}, 대시속도 {newDashSpeed:F1}");
        }
        else
        {
            Debug.LogWarning("⚠️ [PlayerController] PlayerRuntimeStats를 찾을 수 없어 동기화 실패");
        }
    }

    /// <summary>
    /// 액션 RPG 스타일 물리 설정 최적화
    /// </summary>
    private void OptimizePhysicsForActionRPG()
    {
        if (rb != null)
        {
            rb.gravityScale = 0f;           // 2D 탑뷰이므로 중력 제거
            rb.drag = 0f;                   // 기본 드래그 0 (MoveFast에서 동적 조정)
            rb.angularDrag = 10f;           // 회전 저항 높임
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 움직임
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 움직임에서 충돌 감지
            rb.freezeRotation = true;       // Z축 회전 고정
            Debug.Log("🎮 [PlayerController] 액션 RPG 물리 설정 완료");
        }
    }

    #region ⚔️ 공격 중 이동 제어 시스템

    /// <summary>
    /// 이동 스케일 설정 (0.0 = 완전정지, 1.0 = 정상속도)
    /// </summary>
    public void SetMovementScale(float scale)
    {
        targetMovementScale = Mathf.Clamp01(scale);
        
        if (showMovementDebug)
            Debug.Log($"⚔️ [PlayerController] 이동 스케일 설정: {scale:F2} (현재: {movementScale:F2} → 목표: {targetMovementScale:F2})");
    }

    /// <summary>
    /// 이동 완전 잠금/해제
    /// </summary>
    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
        
        if (showMovementDebug)
            Debug.Log($"🔒 [PlayerController] 이동 잠금 {(locked ? "활성화" : "해제")}");
        
        // 잠금 해제 시 즉시 정지
        if (locked && rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// 공격 타입별 이동 제한 적용
    /// </summary>
    public void ApplyAttackMovementRestriction(PlayerAttackType attackType)
    {
        switch (attackType)
        {
            case PlayerAttackType.BasicAttack:
                SetMovementScale(0.2f); // 80% 감쇠
                break;
                
            case PlayerAttackType.Skill:
                SetMovementLocked(true); // 완전 정지
                break;
                
            case PlayerAttackType.Dash:
                SetMovementScale(0.5f); // 50% 감쇠
                break;
                
            default:
                SetMovementScale(1.0f); // 정상 이동
                break;
        }
        
        if (showMovementDebug)
            Debug.Log($"⚔️ [PlayerController] {attackType} 공격 - 이동 제한 적용");
    }

    /// <summary>
    /// 이동 제한 해제 (정상 상태로 복구)
    /// </summary>
    public void RestoreNormalMovement()
    {
        SetMovementLocked(false);
        SetMovementScale(1.0f);
        
        if (showMovementDebug)
            Debug.Log("✅ [PlayerController] 정상 이동 복구");
    }

    /// <summary>
    /// 현재 이동 제한 상태 조회
    /// </summary>
    public bool IsMovementRestricted()
    {
        return isMovementLocked || movementScale < 0.99f;
    }

    #endregion
    
    #region ⭐ Phase 4: 대시 방향 저장 시스템
    
    /// <summary>
    /// 백그라운드에서 마지막 대시 방향 지속적 업데이트
    /// </summary>
    private IEnumerator UpdateLastDashDirectionCoroutine()
    {
        while (true)
        {
            // 현재 이동 방향 체크
            if (movement.magnitude > 0.1f)
            {
                lastDashDirection = movement.normalized;
            }
            
            // 60FPS로 업데이트 (스킬 시스템과 동일)
            yield return new WaitForSeconds(1f / 60f);
        }
    }
    
    #endregion
}
