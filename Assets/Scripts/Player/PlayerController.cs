using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
     public bool FacingLeft { get { return facingLeft; } }
     public Vector2 Movement { get { return movement; } }


     [SerializeField] private float moveSpeed = 1f;
     [SerializeField] private float dashspeed = 4f;
     [SerializeField] private TrailRenderer myTrailRenderer;
     [SerializeField] private Transform weaponCollider;

     private PlayerControls playerControls;
     private Vector2 movement;
     private Rigidbody2D rb;
     private Animator myAnimator;
     private SpriteRenderer mySpriteRender;
     private Knockback knockback;
     private float startingMoveSpeed;

     private bool facingLeft = false;
     private bool isDashing = false;

     // FixedJoystick 참조 추가
     [Header("조이스틱 입력")]
     public FixedJoystick fixedJoystick;
     private bool joystickFound = false;

     // 무기/스킬별 레벨 통합 관리
     private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

     protected override void Awake() 
     {
          base.Awake();
          playerControls = new PlayerControls();
          rb = GetComponent<Rigidbody2D>();
          myAnimator = GetComponent<Animator>();
          mySpriteRender = GetComponent<SpriteRenderer>();
          knockback = GetComponent<Knockback>();
          
          // 조이스틱 초기화는 Start에서 코루틴으로 처리
     }

     private void Start() 
     {
          playerControls.Combat.Dash.performed += _ => Dash();
          startingMoveSpeed = moveSpeed;

          // 조이스틱 찾기 코루틴 시작
          StartCoroutine(FindJoystickCoroutine());

          // 로비에서 선택한 무기를 장착하는 새로운 로직으로 대체하므로 이 줄을 주석 처리합니다.
          // FindObjectOfType<ActiveInventory>().EquipStartingweapon();
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
          // ⭐ 추가: 조이스틱 연결 상태 실시간 체크
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
          
          PlayerInput();
     }

     private void FixedUpdate() 
     {
          AdjustPlayerFacingDirection();
          Move();
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

     private void Move() 
     {
          if (knockback.GettingKnockedBack || FindObjectOfType<PlayerHealth>().isDead) { return; }

          rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
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
}

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class PlayerController : Singleton<PlayerController>
// {
//      public bool FacingLeft { get { return facingLeft; } }
//      public Vector2 Movement { get { return movement; } }


//      [SerializeField] private float moveSpeed = 1f;
//      [SerializeField] private float dashspeed = 4f;
//      [SerializeField] private TrailRenderer myTrailRenderer;
//      [SerializeField] private Transform weaponCollider;

//      private PlayerControls playerControls;
//      private Vector2 movement;
//      private Rigidbody2D rb;
//      private Animator myAnimator;
//      private SpriteRenderer mySpriteRender;
//      private Knockback knockback;
//      private float startingMoveSpeed;

//      private bool facingLeft = false;
//      private bool isDashing = false;

//      // FixedJoystick 참조 추가
//      [Header("조이스틱 입력")]
//      public FixedJoystick fixedJoystick;

//      // 무기/스킬별 레벨 통합 관리
//      private Dictionary<string, int> skillLevels = new Dictionary<string, int>();

//      protected override void Awake() {
//           base.Awake();
//           playerControls = new PlayerControls();
//           rb = GetComponent<Rigidbody2D>();
//           myAnimator = GetComponent<Animator>();
//           mySpriteRender = GetComponent<SpriteRenderer>();
//           knockback = GetComponent<Knockback>();
//           if (fixedJoystick == null)
//           {
//                fixedJoystick = FindObjectOfType<FixedJoystick>();
//           }
//      }

//      private void Start() {
//           playerControls.Combat.Dash.performed += _ => Dash();

//           startingMoveSpeed = moveSpeed;

//           // 로비에서 선택한 무기를 장착하는 새로운 로직으로 대체하므로 이 줄을 주석 처리합니다.
//           // FindObjectOfType<ActiveInventory>().EquipStartingweapon();
//      }

//      private void OnEnable() {
//           playerControls.Enable();
//      }

//      private void OnDisable() {
//      if (playerControls != null)
//           playerControls.Disable();
//      }

//      private void Update()
//      {
//           // 동적 할당: fixedJoystick이 끊겼을 때 자동 재연결
//           if (fixedJoystick == null)
//           {
//                fixedJoystick = FindObjectOfType<FixedJoystick>();
//           }
//           PlayerInput();
//      }

//      private void FixedUpdate() {
//           AdjustPlayerFacingDirection();
//           Move();
//      }

//      public void ReEnableControls()
//      {
//         playerControls.Disable();
//         playerControls.Enable();
//      }

//      public Transform GetWeaponCollider() {
//           return weaponCollider;
//      }

//      private void PlayerInput() {
//           // movement = playerControls.Movement.Move.ReadValue<Vector2>(); // 기존 키보드 입력 주석처리
//           if (fixedJoystick != null)
//           {
//                movement = fixedJoystick.Direction;
//           }
//           else
//           {
//                movement = Vector2.zero;
//           }
//           myAnimator.SetFloat("moveX", movement.x);
//           myAnimator.SetFloat("moveY", movement.y);
//      } 

//      private void Move() {
//           if (knockback.GettingKnockedBack || FindObjectOfType<PlayerHealth>().isDead) { return; }

//           rb.MovePosition(rb.position + movement * (moveSpeed * Time.fixedDeltaTime));
//      }

//      private void AdjustPlayerFacingDirection() {
//          if (movement.x < 0) {
//                mySpriteRender.flipX = true;
//                facingLeft = true;
//          } else if (movement.x > 0) {
//                mySpriteRender.flipX = false;
//                facingLeft = false;
//          }
//      }

//      private void Dash() {
//           if ( !isDashing && Stamina.Instance.CurrentStamina > 0 ) {
//                Stamina.Instance.UseStamina();              
//                isDashing = true;
//                moveSpeed += dashspeed;
//                myTrailRenderer.emitting = true;
//                StartCoroutine(EndDashRoutine());
//           }
//      }

//      private IEnumerator EndDashRoutine() {
//           float dashTime = .2f;
//           float dashCD = .25f;
//           yield return new WaitForSecondsRealtime(dashTime);
//           moveSpeed = startingMoveSpeed;
//           myTrailRenderer.emitting = false;
//           yield return new WaitForSecondsRealtime(dashCD);
//           isDashing = false;
//      }

//      // Bow, Sword 등 스킬/무기 이름으로 레벨 조회
//      public int GetSkillLevel(string skillName)
//      {
//           if (skillLevels.ContainsKey(skillName))
//                return skillLevels[skillName];
//           return 0; // 기본값
//      }

//      // Bow, Sword 등 스킬/무기 이름으로 레벨 설정
//      public void SetSkillLevel(string skillName, int level)
//      {
//           skillLevels[skillName] = level;
//      }
// } 

