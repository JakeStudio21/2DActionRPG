using UnityEngine;
using UnityEngine.UI;

namespace DebugTools
{
    /// <summary>
    /// 디버그 치트 패널 토글러
    /// - 항상 활성화된 Canvas에 부착
    /// - F1 키 또는 모바일 버튼으로 CheatPanel 활성화/비활성화
    /// - 로비의 Panel_Lobby에서만 모바일 버튼 표시
    /// </summary>
    public class DebugCheatToggler : MonoBehaviour
    {
        [Header("🎮 대상 패널")]
        [SerializeField] private GameObject cheatPanel;
        
        [Header("📱 모바일 버튼")]
        [SerializeField] private GameObject mobileDebugButton;
        [Tooltip("모바일 빌드에서만 버튼 표시 (체크 시)")]
        [SerializeField] private bool onlyShowOnMobile = true;
        [Tooltip("로비의 Panel_Lobby에서만 버튼 표시 (체크 시)")]
        [SerializeField] private bool onlyShowInLobbyPanel = true;
        
        [Header("🏠 로비 패널 관리자")]
        [SerializeField] private LobbyPanelManager lobbyPanelManager;
        [SerializeField] private GameObject lobbyPanel; // Panel_Lobby 참조
        
        [Header("⚙️ 설정")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool showDebugLogs = true;
        
        private void Start()
        {
            // LobbyPanelManager 자동 찾기
            if (lobbyPanelManager == null)
            {
                lobbyPanelManager = FindObjectOfType<LobbyPanelManager>();
                
                if (lobbyPanelManager != null && showDebugLogs)
                {
                    Debug.Log($"🎮 [CheatToggler] LobbyPanelManager 자동 검색 성공");
                }
            }
            
            // lobbyPanel 자동 찾기
            if (lobbyPanel == null && lobbyPanelManager != null)
            {
                lobbyPanel = lobbyPanelManager.lobbyPanel;
                
                if (lobbyPanel != null && showDebugLogs)
                {
                    Debug.Log($"🎮 [CheatToggler] LobbyPanel 자동 검색 성공: {lobbyPanel.name}");
                }
            }
            
            // 패널 변경 이벤트 구독
            if (lobbyPanelManager != null && onlyShowInLobbyPanel)
            {
                lobbyPanelManager.OnPanelChanged += OnLobbyPanelChanged;
                
                if (showDebugLogs)
                {
                    Debug.Log($"🎮 [CheatToggler] 패널 변경 이벤트 구독 완료");
                }
            }
            
            // 모바일 버튼 초기 설정
            if (mobileDebugButton != null)
            {
                // 버튼 이벤트 연결
                Button button = mobileDebugButton.GetComponentInChildren<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(OnMobileButtonClicked);
                }
                
                // 초기 표시 여부 결정
                UpdateMobileButtonVisibility();
            }
        }
        
        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (lobbyPanelManager != null)
            {
                lobbyPanelManager.OnPanelChanged -= OnLobbyPanelChanged;
            }
        }
        
        /// <summary>
        /// 로비 패널 변경 시 호출되는 콜백
        /// </summary>
        private void OnLobbyPanelChanged()
        {
            UpdateMobileButtonVisibility();
        }
        
        /// <summary>
        /// 모바일 버튼 표시 여부 업데이트
        /// </summary>
        private void UpdateMobileButtonVisibility()
        {
            if (mobileDebugButton == null) return;
            
            bool shouldShow = false;
            
            // 1. 모바일 플랫폼 체크
            if (onlyShowOnMobile && !IsMobilePlatform())
            {
                shouldShow = false;
            }
            else
            {
                // 2. 로비 패널 체크
                if (onlyShowInLobbyPanel)
                {
                    // LobbyPanelManager가 있으면 현재 활성 패널 확인
                    if (lobbyPanelManager != null && lobbyPanel != null)
                    {
                        shouldShow = lobbyPanelManager.IsActivePanel(lobbyPanel);
                    }
                    else
                    {
                        // LobbyPanelManager가 없으면 기본적으로 숨김
                        shouldShow = false;
                    }
                }
                else
                {
                    // onlyShowInLobbyPanel이 체크 해제되어 있으면 항상 표시
                    shouldShow = true;
                }
            }
            
            mobileDebugButton.SetActive(shouldShow);
            
            if (showDebugLogs)
            {
                string reason = shouldShow ? "표시" : "숨김";
                string panelName = lobbyPanelManager?.GetCurrentActivePanel()?.name ?? "알 수 없음";
                Debug.Log($"🎮 [CheatToggler] 모바일 버튼 {reason} (현재 패널: {panelName})");
            }
        }
        
        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(toggleKey))
            {
                if (showDebugLogs) Debug.Log($"🔑 [CheatToggler] {toggleKey} 키 입력 감지됨");
                TogglePanel();
            }

            if (cheatPanel != null && cheatPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                cheatPanel.SetActive(false);
                if (showDebugLogs) Debug.Log($"🔑 [CheatToggler] ESC 키로 패널 닫힘");
            }
#endif
        }
        
        /// <summary>
        /// 모바일 버튼 클릭 이벤트 (public - Inspector에서 연결 가능)
        /// </summary>
        public void OnMobileButtonClicked()
        {
            if (showDebugLogs)
            {
                Debug.Log($"📱 [CheatToggler] 모바일 버튼 클릭됨");
            }
            TogglePanel();
        }
        
        private void TogglePanel()
        {
            if (cheatPanel == null)
            {
                Debug.LogError("❌ [CheatToggler] CheatPanel이 연결되지 않았습니다!");
                return;
            }
            
            bool newState = !cheatPanel.activeSelf;
            cheatPanel.SetActive(newState);
            
            if (showDebugLogs)
            {
                string stateText = newState ? "열림" : "닫힘";
                Debug.Log($"🎮 [CheatToggler] 치트 패널 {stateText}");
            }
        }
        
        /// <summary>
        /// 현재 플랫폼이 모바일인지 확인
        /// </summary>
        private bool IsMobilePlatform()
        {
            return Application.platform == RuntimePlatform.Android || 
                   Application.platform == RuntimePlatform.IPhonePlayer;
        }
    }
}

