using UnityEngine;

namespace DebugTools
{
    /// <summary>
    /// 디버그 치트 패널 토글러
    /// - 항상 활성화된 Canvas에 부착
    /// - F1 키로 CheatPanel 활성화/비활성화
    /// </summary>
    public class DebugCheatToggler : MonoBehaviour
    {
        [Header("🎮 대상 패널")]
        [SerializeField] private GameObject cheatPanel;
        
        [Header("⚙️ 설정")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool showDebugLogs = true;
        
        private void Update()
        {
            // F1 키로 패널 토글
            if (Input.GetKeyDown(toggleKey))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🔑 [CheatToggler] {toggleKey} 키 입력 감지됨");
                }
                TogglePanel();
            }
            
            // ESC 키로 패널 닫기
            if (cheatPanel != null && cheatPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            {
                cheatPanel.SetActive(false);
                
                if (showDebugLogs)
                {
                    Debug.Log($"🔑 [CheatToggler] ESC 키로 패널 닫힘");
                }
            }
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
    }
}

