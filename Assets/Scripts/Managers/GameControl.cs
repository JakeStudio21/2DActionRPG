using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 입력 처리를 중앙화하는 매니저
/// 키보드, 마우스, 조이스틱 입력을 통합 관리
/// </summary>
public class GameControl : Singleton<GameControl>
{
    [Header("입력 설정")]
    [SerializeField] private bool enableKeyboardInput = true;
    [SerializeField] private bool enableJoystickInput = true;
    [SerializeField] private bool enableMouseInput = true;

    [Header("조이스틱 참조")]
    [SerializeField] private DynamicJoystick movementJoystick;
    [SerializeField] private AttackJoystickInput attackJoystick;

    // 입력 상태
    public Vector2 MovementInput { get; private set; }
    public Vector2 AttackDirection { get; private set; }
    public bool AttackPressed { get; private set; }
    public bool SkillPressed { get; private set; }
    public bool DashPressed { get; private set; }

    // 이벤트
    public System.Action OnAttackPressed;
    public System.Action OnSkillPressed;
    public System.Action OnDashPressed;

    protected override void Awake()
    {
        base.Awake();
        InitializeControls();
    }

    private void Start()
    {
        // 조이스틱 찾기
        StartCoroutine(FindJoysticksCoroutine());
    }

    private void Update()
    {
        ProcessMovementInput();
        ProcessAttackInput();
        ProcessSkillInput();
        ProcessDashInput();
    }

    /// <summary>
    /// 컨트롤 초기화
    /// </summary>
    private void InitializeControls()
    {
        MovementInput = Vector2.zero;
        AttackDirection = Vector2.zero;
        AttackPressed = false;
        SkillPressed = false;
        DashPressed = false;
    }

    /// <summary>
    /// 조이스틱 찾기 코루틴
    /// </summary>
    private IEnumerator FindJoysticksCoroutine()
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            // 이동 조이스틱 찾기
            if (movementJoystick == null)
            {
                movementJoystick = FindObjectOfType<DynamicJoystick>();
            }

            // 공격 조이스틱 찾기
            if (attackJoystick == null)
            {
                attackJoystick = FindObjectOfType<AttackJoystickInput>();
            }

            // 둘 다 찾았으면 종료
            if (movementJoystick != null && attackJoystick != null)
            {
                Debug.Log("[GameControl] 조이스틱 초기화 완료!");
                break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning("[GameControl] 조이스틱 찾기 타임아웃. 키보드 입력만 사용됩니다.");
        }
    }

    /// <summary>
    /// 이동 입력 처리
    /// </summary>
    private void ProcessMovementInput()
    {
        MovementInput = Vector2.zero;

        // 조이스틱 입력
        if (enableJoystickInput && movementJoystick != null)
        {
            MovementInput = movementJoystick.Direction;
        }

        // 키보드 이동 입력 - 모바일 우선 개발로 주석처리
        // if (enableKeyboardInput && MovementInput.magnitude < 0.1f)
        // {
        //     float horizontal = Input.GetAxis("Horizontal");
        //     float vertical = Input.GetAxis("Vertical");
        //     MovementInput = new Vector2(horizontal, vertical);
        // }
    }

    /// <summary>
    /// 공격 입력 처리 - 완전 비활성화 (PlayerAttackInput.cs에서 A키 처리)
    /// </summary>
    private void ProcessAttackInput()
    {
        AttackPressed = false;
        AttackDirection = Vector2.zero;

        // 🔴 모든 공격 입력 비활성화 - A키는 PlayerAttackInput.cs에서만 처리
        // 조이스틱, Space, 마우스클릭 모두 비활성화
    }

    /// <summary>
    /// 스킬 입력 처리 - 완전 비활성화 (PlayerAttackInput.cs에서 S, D키 처리)
    /// </summary>
    private void ProcessSkillInput()
    {
        SkillPressed = false;

        // 🔴 모든 스킬 입력 비활성화 - S, D키는 PlayerAttackInput.cs에서만 처리
        // Q, LeftShift 모두 비활성화
    }

    /// <summary>
    /// 대시 입력 처리 - Spacebar 활성화
    /// </summary>
    private void ProcessDashInput()
    {
        DashPressed = false;

        // 🆕 Spacebar 대시 입력 (PC 전용)
#if UNITY_EDITOR || UNITY_STANDALONE
        if (enableKeyboardInput && Input.GetKeyDown(KeyCode.Space))
        {
            DashPressed = true;
            OnDashPressed?.Invoke();
            Debug.Log("[GameControl] Spacebar 대시 입력 감지");
        }
#endif
    }

    /// <summary>
    /// 입력 모드 설정
    /// </summary>
    public void SetInputMode(bool keyboard, bool joystick, bool mouse)
    {
        enableKeyboardInput = keyboard;
        enableJoystickInput = joystick;
        enableMouseInput = mouse;
    }

    /// <summary>
    /// 조이스틱 참조 새로고침
    /// </summary>
    public void RefreshJoystickReferences()
    {
        StartCoroutine(FindJoysticksCoroutine());
    }
} 