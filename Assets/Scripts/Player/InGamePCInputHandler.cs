using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// PC 전용 입력 중앙 관리자
///
/// ■ 담당 입력 (한 곳에서 집중 관리)
///   마우스 좌클릭  → 기본공격 (UI 위 클릭 무시, 마우스 방향으로 에임)
///   Q             → 스킬1
///   E             → 스킬2
///   M             → 미니맵/레이더맵 토글
///   C             → 설정 패널 토글
///   ESC           → 패널 닫기 우선 / 일시정지 팝업 열기·닫기
///
/// ■ 담당하지 않는 입력 (기존 코드 유지)
///   WASD          → PlayerController.PlayerInput() 에서 처리
///   Space (대시)  → PlayerController 가 PlayerControls.Combat.Dash 로 처리
///   I (인벤토리)  → IntegratedInventoryController 가 처리
///
/// ■ 활성 조건
///   PC/Editor 빌드 AND 로비 씬이 아닐 때만 동작
/// </summary>
public class InGamePCInputHandler : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────

    [Header("참조 (비어있으면 Start에서 자동 탐색)")]
    [SerializeField] private PlayerAttackInput          playerAttackInput;
    [SerializeField] private PlayerController           playerController;
    [SerializeField] private PauseMenuController        pauseMenuController;
    [SerializeField] private SettingsUIController       settingsUIController;
    [SerializeField] private IntegratedInventoryController inventoryController;

    // ─────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
#if !(UNITY_EDITOR || UNITY_STANDALONE)
        // 모바일 빌드에서는 이 컴포넌트 전체 비활성화
        this.enabled = false;
        return;
#endif

        // 로비 씬에서는 비활성화 (인게임 단축키 차단)
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Lobby" || sceneName.Contains("Lobby"))
        {
            this.enabled = false;
            return;
        }

        // 레퍼런스 자동 탐색
        // SettingsUIController 는 Start() 에서 gameObject.SetActive(false) 하므로
        // 비활성 오브젝트까지 포함해서 찾아야 한다 (includeInactive: true)
        if (playerAttackInput    == null) playerAttackInput    = FindObjectOfType<PlayerAttackInput>(true);
        if (playerController     == null) playerController     = FindObjectOfType<PlayerController>(true);
        if (pauseMenuController  == null) pauseMenuController  = FindObjectOfType<PauseMenuController>(true);
        if (settingsUIController == null) settingsUIController = FindObjectOfType<SettingsUIController>(true);
        if (inventoryController  == null) inventoryController  = FindObjectOfType<IntegratedInventoryController>(true);

        // 씬 전환 시 레퍼런스 재탐색 + 모바일 UI 재숨기기 안전망 등록
        SceneManager.sceneLoaded += OnSceneLoaded;

        // PC 빌드에서 모바일 전용 UI 숨기기 (한 프레임 뒤에 실행 — 씬 초기화 완료 후)
        StartCoroutine(HideMobileUINextFrame());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 새로 로드될 때마다 실행됩니다.
    /// PlayerSpawner가 새 씬에서 플레이어를 다시 인스턴스화하므로
    /// 보통은 Start()가 재실행되지만, DontDestroyOnLoad 경로를 위한 안전망입니다.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 로비 씬이면 비활성화
        if (scene.name == "Lobby" || scene.name.Contains("Lobby"))
        {
            this.enabled = false;
            return;
        }

        this.enabled = true;

        // 씬의 새 오브젝트들로 레퍼런스 갱신
        playerAttackInput    = FindObjectOfType<PlayerAttackInput>(true);
        playerController     = FindObjectOfType<PlayerController>(true);
        pauseMenuController  = FindObjectOfType<PauseMenuController>(true);
        settingsUIController = FindObjectOfType<SettingsUIController>(true);
        inventoryController  = FindObjectOfType<IntegratedInventoryController>(true);

        // 새 씬의 UICanvas에 있는 모바일 UI 다시 숨기기
        StartCoroutine(HideMobileUINextFrame());
    }

    // ─────────────────────────────────────────────────────────────
    //  모바일 UI 숨기기
    // ─────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator HideMobileUINextFrame()
    {
        yield return null; // 모든 오브젝트 Start() 완료 후 실행
        HideMobileUI();
    }

    /// <summary>
    /// PC 빌드에서 모바일 전용 UI 오브젝트를 비활성화합니다.
    /// - 조이스틱: DynamicJoystick 컴포넌트로 탐색
    /// - 공격/스킬 버튼: AttackButtonController 컴포넌트로 탐색
    /// - 대시 버튼: DashButtonController 컴포넌트로 탐색
    /// - 원형 HUD 버튼: 이름으로 탐색 (비활성 포함)
    /// </summary>
    private void HideMobileUI()
    {
        // 조이스틱
        var joystick = FindObjectOfType<DynamicJoystick>(true);
        if (joystick != null)
            joystick.gameObject.SetActive(false);

        // 공격/스킬 버튼 (AttackButtonController 가 붙어있는 모든 오브젝트)
        foreach (var ctrl in FindObjectsOfType<AttackButtonController>(true))
            ctrl.gameObject.SetActive(false);

        // 대시 버튼
        foreach (var ctrl in FindObjectsOfType<DashButtonController>(true))
            ctrl.gameObject.SetActive(false);

        // 이름으로 탐색 — UICanvas 직속 자식 버튼 + 원형 HUD 버튼
        // Skill1Button / Skill2Button: AttackButtonController 없는 별도 오브젝트 (UICanvas 직속)
        string[] namedButtons =
        {
            "Skill1Button",
            "Skill2Button",
            "CirclePanel_Quit Button",
            "CirclePanel_Bag Button",
            "CirclePanel_SettingsButton",
        };
        foreach (string btnName in namedButtons)
        {
            GameObject go = GameObject.Find(btnName);
            if (go == null)
            {
                // Find() 는 비활성 오브젝트를 못 찾으므로 전체 Transform 탐색으로 fallback
                foreach (var t in FindObjectsOfType<Transform>(true))
                {
                    if (t.name == btnName) { go = t.gameObject; break; }
                }
            }
            if (go != null)
                go.SetActive(false);
            else
                Debug.LogWarning($"[InGamePCInputHandler] 모바일 UI를 찾을 수 없음: '{btnName}'");
        }
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        // 씬 전환 시 로비로 돌아오면 비활성화
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "Lobby" || sceneName.Contains("Lobby")) return;

        // Start() 실행 순서 문제로 못 찾은 레퍼런스 재시도 (비활성 포함)
        if (settingsUIController == null)
            settingsUIController = FindObjectOfType<SettingsUIController>(true);

        HandleAttackInput();
        HandleSkillInput();
        HandleMinimapToggle();
        HandleSettingsToggle();
        HandleEscInput();
#endif
    }

    // ─────────────────────────────────────────────────────────────
    //  입력 처리
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 마우스 좌클릭 → 기본공격
    /// - UI 위 클릭은 무시 (EventSystem.IsPointerOverGameObject)
    /// - 클릭 위치(월드) 방향으로 에임 설정 후 PerformAttack 호출
    /// </summary>
    private void HandleAttackInput()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // UI 위 클릭이면 공격 실행 안 함
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (playerAttackInput == null || playerController == null) return;

        // 마우스 스크린 좌표 → 월드 좌표 → 에임 방향
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        mouseWorld.z = playerController.transform.position.z;

        Vector2 aimDir = (Vector2)mouseWorld - (Vector2)playerController.transform.position;
        if (aimDir.sqrMagnitude > 0.001f)
            playerAttackInput.SetPCAimDirection(aimDir.normalized);

        playerAttackInput.PerformAttack();
    }

    /// <summary>Q → 스킬1 / E → 스킬2 (마우스 방향으로 캐릭터 회전 후 발동)</summary>
    private void HandleSkillInput()
    {
        if (playerAttackInput == null) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            FaceMouseDirection();
            playerAttackInput.PerformSkill();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            FaceMouseDirection();
            playerAttackInput.PerformSkill2();
        }
    }

    /// <summary>
    /// 마우스 방향으로 캐릭터를 즉시 전환합니다.
    /// SetFacingDirectionForSkill() 을 사용하므로 flipX + moveX/moveY 가 모두 갱신되지만
    /// _attackDirectionLocked 는 건드리지 않아 스킬 후 이동이 바로 정상 동작합니다.
    /// </summary>
    private void FaceMouseDirection()
    {
        if (playerController == null || Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane));
        mouseWorld.z = playerController.transform.position.z;

        Vector2 dir = (Vector2)mouseWorld - (Vector2)playerController.transform.position;
        if (dir.sqrMagnitude > 0.001f)
            playerController.SetFacingDirectionForSkill(dir.normalized);
    }

    /// <summary>M → 미니맵 ↔ 레이더맵 토글</summary>
    private void HandleMinimapToggle()
    {
        if (!Input.GetKeyDown(KeyCode.M)) return;

        if (MiniMapUIManager.Instance != null)
            MiniMapUIManager.Instance.ToggleMinimap();
    }

    /// <summary>C → 설정 패널 열기/닫기</summary>
    private void HandleSettingsToggle()
    {
        if (!Input.GetKeyDown(KeyCode.C)) return;
        if (settingsUIController == null) return;

        if (settingsUIController.IsOpen)
            settingsUIController.Close();
        else
            settingsUIController.Open(SettingsOpenMode.FromHUD);
    }

    /// <summary>
    /// ESC 우선순위 처리:
    ///   1) 인벤토리 열림 → IntegratedInventoryController 의 closeKey(ESC) 에 위임
    ///   2) 설정 패널 열림 → 닫기
    ///   3) 일시정지 메뉴 열림 → 닫기 (계속하기)
    ///   4) 모두 닫힌 상태 → 일시정지 메뉴 열기 (나가기 확인 팝업)
    /// </summary>
    private void HandleEscInput()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        // 1) 인벤토리가 열려있으면 IntegratedInventoryController 의 ESC 처리에 위임
        //    (같은 프레임에 HandleInputs() 에서 ESC 를 감지해 닫아줌)
        if (inventoryController != null && inventoryController.IsInventoryOpen)
            return;

        // 2) 설정 패널 열림 → 닫기
        if (settingsUIController != null && settingsUIController.IsOpen)
        {
            settingsUIController.Close();
            return;
        }

        // 3) 일시정지 메뉴 열림 → 닫기 (계속하기 효과)
        if (pauseMenuController != null && pauseMenuController.IsPauseMenuOpen)
        {
            pauseMenuController.HidePauseMenu();
            return;
        }

        // 4) 모두 닫힌 상태 → 일시정지 메뉴 열기
        if (pauseMenuController != null)
            pauseMenuController.ShowPauseMenu();
    }
}
