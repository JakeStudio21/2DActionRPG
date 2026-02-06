using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Systems;

namespace UI.Popups
{
    /// <summary>
    /// 귀속 경고 팝업
    /// - 아이템 장착 시 귀속 경고를 표시하고 사용자 확인을 받음
    /// </summary>
    public class BindWarningPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Toggle dontShowAgainToggle;
        
        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = false;
        
        private BindWarningData _currentWarningData;
        private System.Action<bool> _onUserResponse;
        
        private void Awake()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }
        
        /// <summary>
        /// 귀속 경고 팝업 표시
        /// </summary>
        public void Show(BindWarningData warningData, System.Action<bool> onUserResponse)
        {
            _currentWarningData = warningData;
            _onUserResponse = onUserResponse;
            
            if (messageText != null)
            {
                messageText.text = warningData.GetWarningMessage();
            }
            
            if (dontShowAgainToggle != null)
            {
                dontShowAgainToggle.isOn = false;
            }
            
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
            
            Log($"[BindWarningPopup] 귀속 경고 표시: {warningData.itemTemplateName}+{warningData.enhancementLevel}");
        }
        
        /// <summary>
        /// ⭐ 커스텀 메시지로 팝업 표시 (귀속 해제 등)
        /// </summary>
        public void ShowCustom(string title, string message, System.Action<bool> onUserResponse)
        {
            _currentWarningData = null; // 커스텀 모드
            _onUserResponse = onUserResponse;
            
            // 타이틀 설정 (titleText가 있으면)
            var titleText = popupPanel?.transform.Find("Title")?.GetComponent<TMPro.TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = title;
            }
            
            // 메시지 설정
            if (messageText != null)
            {
                messageText.text = message;
            }
            
            // "다시 보지 않기" 토글 숨김 (커스텀 경고에서는 사용 안 함)
            if (dontShowAgainToggle != null)
            {
                dontShowAgainToggle.gameObject.SetActive(false);
            }
            
            // 팝업 표시
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
            
            Log($"[BindWarningPopup] 커스텀 경고 표시: {title}");
        }
        
        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private void OnConfirmClicked()
        {
            bool dontShowAgain = dontShowAgainToggle != null && dontShowAgainToggle.isOn;
            
            if (dontShowAgain)
            {
                PlayerPrefs.SetInt("BindWarning_DontShowAgain", 1);
                PlayerPrefs.Save();
                Log("[BindWarningPopup] '다시 보지 않기' 설정 저장");
            }
            
            Log($"[BindWarningPopup] 사용자 확인: 장착 진행");
            
            _onUserResponse?.Invoke(true);
            Close();
        }
        
        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void OnCancelClicked()
        {
            Log($"[BindWarningPopup] 사용자 취소: 장착 중단");
            
            _onUserResponse?.Invoke(false);
            Close();
        }
        
        /// <summary>
        /// 팝업 닫기
        /// </summary>
        private void Close()
        {
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            
            _currentWarningData = null;
            _onUserResponse = null;
        }
        
        /// <summary>
        /// '다시 보지 않기' 설정 확인
        /// </summary>
        public static bool IsDontShowAgain()
        {
            return PlayerPrefs.GetInt("BindWarning_DontShowAgain", 0) == 1;
        }
        
        /// <summary>
        /// '다시 보지 않기' 설정 초기화
        /// </summary>
        public static void ResetDontShowAgain()
        {
            PlayerPrefs.DeleteKey("BindWarning_DontShowAgain");
            PlayerPrefs.Save();
        }
        
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }
    }
}

