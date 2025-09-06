using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
     public bool FacingLeft { get { return facingLeft; } }
     public Vector2 Movement { get { return movement; } }


     [SerializeField] private float moveSpeed = 1f;
     [SerializeField] private float dashspeed = 4f;
     [SerializeField] private TrailRenderer myTrailRenderer;
     [SerializeField] private Transform weaponCollider;
     // 🎯 반응성 튜닝 파라미터
     [SerializeField] private float deadZone = 0.12f;           // 미세 입력 무시
     [SerializeField] private float snapTurnThreshold = -0.2f;  // 역방향 전환 스냅 컷오프( -1 에 가까울수록 강함 )

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

     // FixedJoystick 참조 추가
     [Header("조이스틱 입력")]
     public FixedJoystick fixedJoystick;
     private bool joystickFound = false;

     // 무기/스킬별 레벨 통합 관리
     private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

     // ⚡ 액션 RPG 반응성 설정
     [Header("🎮 액션 RPG 반응성 설정")]
     [SerializeField] private float inputBufferTime = 0.1f;    // 입력 버퍼 시간
     [SerializeField] private float accelerationTime = 0.05f;  // 가속 시간 (0에 가까울수록 즉각적)
     [SerializeField] private float decelerationTime = 0.03f;  // 감속 시간 (0에 가까울수록 즉각적)

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
     }

     private void Start() 
     {
          playerControls.Combat.Dash.performed += _ => Dash();
          startingMoveSpeed = moveSpeed;

          // 조이스틱 찾기 코루틴 시작
          StartCoroutine(FindJoystickCoroutine());
          
          // ⭐ 안전한 Rigidbody2D 상태 확인
          StartCoroutine(SafeCheckRigidbodyState());

          // 로비에서 선택한 무기를 장착하는 새로운 로직으로 대체하므로 이 줄을 주석 처리합니다.
          // FindObjectOfType<ActiveInventory>().EquipStartingweapon();

          // 액션 RPG 물리 최적화
          OptimizePhysicsForActionRPG();
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
               fixedJoystick = FindObjectOfType<FixedJoystick>();
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
          playerControls.Enable();
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
                    var joystickInScene = FindObjectOfType<FixedJoystick>();
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
          
          // ⭐ 강제 위치 변경 테스트 제거 (에러 원인 제거)
          
          PlayerInput();
     }

     private void FixedUpdate()
     {
          AdjustPlayerFacingDirection();
          
          // ✅ 선택 1: 완전 즉각적 (격투 게임 스타일)
          MoveFast();
          
          // ✅ 선택 2: 부드럽지만 빠른 반응 (액션 RPG 스타일)
          // MoveFastWithSmoothing();
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
                    var joystickInScene = FindObjectOfType<FixedJoystick>();
                    Debug.Log($"[PlayerController] 씬에 조이스틱 존재 여부: {joystickInScene != null}");

                    // ⭐ 씬에 조이스틱이 있는데 연결 안된 경우 강제 재연결
                    if (joystickInScene != null && (!joystickFound || fixedJoystick == null))
                    {
                         Debug.Log("[PlayerController] 조이스틱 발견! 강제 재연결 시도");
                         RefreshJoystickReference();
                    }
               }
          }

          myAnimator.SetFloat("moveX", movement.x);
          myAnimator.SetFloat("moveY", movement.y);
     }

     // private void Move() 
     // {
     //      // ⭐ 안전한 디버깅 (try-catch 추가)
     //      try
     //      {
     //          if (Time.frameCount % 60 == 0)
     //          {
     //              Debug.Log($"🔍 [PlayerController] Move 체크:");
     //           //    Debug.Log($"   - movement: {movement}");
     //              Debug.Log($"   - rb가 null인가: {rb == null}");
     //              Debug.Log($"   - knockback가 null인가: {knockback == null}");

     //              if (knockback != null)
     //                  Debug.Log($"   - knockback.GettingKnockedBack: {knockback.GettingKnockedBack}");
     //          }
     //      }
     //      catch (System.Exception e)
     //      {
     //          Debug.LogError($"🔴 [PlayerController] 디버깅 로그 에러: {e.Message}");
     //      }

     //      // ⭐ 안전한 조건 확인
     //      try
     //      {
     //          if (knockback != null && knockback.GettingKnockedBack) 
     //          { 
     //              return; 
     //          }

     //          var playerHealth = FindObjectOfType<PlayerHealth>();
     //          if (playerHealth != null && playerHealth.isDead) 
     //          { 
     //              return; 
     //          }
     //      }
     //      catch (System.Exception e)
     //      {
     //          Debug.LogError($"🔴 [PlayerController] Move 조건 확인 에러: {e.Message}");
     //          return;
     //      


     //      // ⭐ 안전한 이동 실행
     //      try
     //      {
     //          if (rb != null && movement.magnitude > 0.01f)
     //          {
     //              Vector2 newPosition = rb.position + movement * (moveSpeed * Time.fixedDeltaTime);
     //              rb.MovePosition(newPosition);

     //              // ⭐ 간단한 이동 확인 (에러 방지)
     //              if (Time.frameCount % 120 == 0) // 2초마다
     //              {
     //                  Debug.Log($"🚀 [PlayerController] 이동 실행: {movement} → {rb.position}");
     //              }
     //          }
     //      }
     //      catch (System.Exception e)
     //      {
     //          Debug.LogError($"🔴 [PlayerController] rb.MovePosition 에러: {e.Message}");
     //      }
     // }

     // ⚡ 액션 RPG 스타일 즉각 반응 이동 시스템
     private void MoveFast()
     {
         // 상태 체크 (넉백/사망 시 이동 금지)
         if (knockback != null && knockback.GettingKnockedBack) return;
         if (playerHealth != null && playerHealth.isDead) return;
         if (rb == null) return;

         // ✅ 개선: 더 작은 DeadZone으로 미세한 입력도 반응
         float improvedDeadZone = 0.05f; // 0.12f → 0.05f로 감소
         
         // DeadZone: 미세 입력은 0으로 간주
         if (movement.sqrMagnitude < improvedDeadZone * improvedDeadZone)
         {
             // ✅ 개선: 즉시 완전 정지 (관성 제거)
             rb.velocity = Vector2.zero;
             rb.angularVelocity = 0f; // 회전 관성도 제거
             
             // Animation 파라미터 처리 (IsMoving 에러 해결)
             if (myAnimator != null)
             {
                 // moveX, moveY를 0으로 설정하여 정지 애니메이션 트리거
                 myAnimator.SetFloat("moveX", 0f);
                 myAnimator.SetFloat("moveY", 0f);
             }
             return;
         }

         // ✅ 개선: 입력 방향 즉시 적용 (정규화 + 스케일링)
         var inputDir = movement.normalized;
         var targetVelocity = inputDir * moveSpeed;

         // ✅ 개선: 더 민감한 방향 전환 (액션 게임 스타일)
         float aggressiveSnapThreshold = -0.1f; // -0.2f → -0.1f로 더 민감하게
         
         // 역방향 전환 시 즉시 스냅
         if (rb.velocity.sqrMagnitude > 0.01f) // 더 작은 임계값
         {
             float dot = Vector2.Dot(rb.velocity.normalized, inputDir);
             if (dot < aggressiveSnapThreshold)
             {
                 rb.velocity = Vector2.zero; // 즉시 리셋
             }
         }

         // ✅ 핵심 개선: 즉각적인 속도 적용 (관성 완전 제거)
         rb.velocity = targetVelocity;
         
         // ✅ 추가: 물리 드래그 동적 조정 (더 반응적으로)
         rb.drag = movement.sqrMagnitude > 0.01f ? 0f : 15f; // 이동 중: 드래그 0, 정지 시: 높은 드래그

         // Animation 파라미터 업데이트 (moveX, moveY 사용)
         if (myAnimator != null)
         {
             myAnimator.SetFloat("moveX", movement.x);
             myAnimator.SetFloat("moveY", movement.y);
         }
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
     /// 🚀 부드럽지만 즉각적인 가속/감속 시스템
     /// </summary>
     private void MoveFastWithSmoothing()
     {
         // 상태 체크
         if (knockback != null && knockback.GettingKnockedBack) return;
         if (playerHealth != null && playerHealth.isDead) return;
         if (rb == null) return;

         // 입력 버퍼링 처리
         ProcessActionRPGInput();

         Vector2 targetVelocity = Vector2.zero;
         
         if (movement.sqrMagnitude > 0.05f * 0.05f)
         {
             // 목표 속도 계산
             targetVelocity = movement.normalized * moveSpeed;
             
             // ✅ 즉각적인 가속 (액션 게임 스타일)
             float accelerationRate = moveSpeed / Mathf.Max(accelerationTime, 0.01f);
             rb.velocity = Vector2.MoveTowards(rb.velocity, targetVelocity, accelerationRate * Time.fixedDeltaTime);
         }
         else
         {
             // ✅ 즉각적인 감속 (정지)
             float decelerationRate = moveSpeed / Mathf.Max(decelerationTime, 0.01f);
             rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, decelerationRate * Time.fixedDeltaTime);
         }

         // Animation 업데이트
         if (myAnimator != null)
         {
             myAnimator.SetFloat("moveX", rb.velocity.x / moveSpeed);
             myAnimator.SetFloat("moveY", rb.velocity.y / moveSpeed);
         }
     }


     private void AdjustPlayerFacingDirection()
     {
          if (movement.x < 0)
          {
               mySpriteRender.flipX = true;
               facingLeft = true;
          }
          else if (movement.x > 0)
          {
               mySpriteRender.flipX = false;
               facingLeft = false;
          }
     }

     private void Dash() 
     {
          // Stamina 체크 제거 - 이제 스태미나 제한 없이 대시 가능
          if ( !isDashing ) 
          {
               // Stamina.Instance.UseStamina(); // 주석처리              
               isDashing = true;
               moveSpeed += dashspeed;
               myTrailRenderer.emitting = true;
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

     private IEnumerator EndDashRoutine() 
     {
          float dashTime = .2f;
          float dashCD = .25f;
          yield return new WaitForSecondsRealtime(dashTime);
          moveSpeed = startingMoveSpeed;
          myTrailRenderer.emitting = false;
          yield return new WaitForSecondsRealtime(dashCD);
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
             // ✅ 즉각 반응을 위한 Rigidbody2D 설정
             rb.gravityScale = 0f;           // 2D 탑뷰이므로 중력 제거
             rb.drag = 0f;                   // 기본 드래그 0 (MoveFast에서 동적 조정)
             rb.angularDrag = 10f;           // 회전 저항 높임
             rb.interpolation = RigidbodyInterpolation2D.Interpolate; // 부드러운 움직임
             rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 빠른 움직임에서 충돌 감지
             rb.freezeRotation = true;       // Z축 회전 고정 (캐릭터가 넘어지지 않음)
             
             Debug.Log("🎮 [PlayerController] 액션 RPG 물리 설정 완료");
         }
     }
}

