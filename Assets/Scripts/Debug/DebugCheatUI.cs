using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DebugTools
{
    /// <summary>
    /// 디버그 치트 UI 패널
    /// - InputField: 텍스트 명령어 입력
    /// - Buttons: 프리셋 명령어 버튼
    /// - F1 키: 패널 토글
    /// </summary>
    public class DebugCheatUI : MonoBehaviour
    {
        [Header("📋 명령어 입력")]
        [SerializeField] private TMP_InputField commandInputField;
        [SerializeField] private Button executeButton;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button helpButton;
        
        [Header("🎮 프리셋 버튼 - 아이템")]
        [SerializeField] private Button btnAddSwordC;
        [SerializeField] private Button btnAddSwordC_Plus5;
        [SerializeField] private Button btnAddSwordA_Plus10;
        [SerializeField] private Button btnAddArmorS_Plus7;
        
        [Header("🎮 프리셋 버튼 - 재료")]
        [SerializeField] private Button btnAddWeaponFragment;
        [SerializeField] private Button btnAddWeaponCrystal;
        [SerializeField] private Button btnAddArmorFragment;
        [SerializeField] private Button btnAddArmorCrystal;
        [SerializeField] private Button btnAddAccessoryFragment;
        
        [Header("🎮 프리셋 버튼 - 골드")]
        [SerializeField] private Button btnAdd10kGold;
        [SerializeField] private Button btnAdd100kGold;
        [SerializeField] private Button btnAdd1mGold;
        
        [Header("⚙️ 설정")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private float resultDisplayDuration = 3f;
        
        private void Start()
        {
            // 명령어 입력 이벤트
            if (executeButton != null)
                executeButton.onClick.AddListener(OnExecuteButtonClicked);
            
            if (commandInputField != null)
                commandInputField.onSubmit.AddListener(OnCommandSubmit);
            
            if (helpButton != null)
                helpButton.onClick.AddListener(OnHelpButtonClicked);
            
            // 프리셋 버튼 이벤트
            SetupPresetButtons();
            
            // 초기 메시지
            if (resultText != null)
                resultText.text = "치트 명령어를 입력하세요. (도움말 버튼 클릭)";
        }
        
        private void SetupPresetButtons()
        {
            // 아이템 버튼
            if (btnAddSwordC != null)
                btnAddSwordC.onClick.AddListener(() => 
                    ExecutePreset("add ITEM_SWORD_C 1", "C등급 검 생성"));
            
            if (btnAddSwordC_Plus5 != null)
                btnAddSwordC_Plus5.onClick.AddListener(() => 
                    ExecutePreset("add ITEM_SWORD_C 1 enhance:5", "C등급 검 +5 생성"));
            
            if (btnAddSwordA_Plus10 != null)
                btnAddSwordA_Plus10.onClick.AddListener(() => 
                    ExecutePreset("add ITEM_SWORD_A 1 enhance:10 bound:true", "A등급 검 +10 귀속 생성"));
            
            if (btnAddArmorS_Plus7 != null)
                btnAddArmorS_Plus7.onClick.AddListener(() => 
                    ExecutePreset("add ITEM_ARMOR_ASSASIN_S 1 enhance:7", "S등급 갑옷 +7 생성"));
            
            // 재료 버튼
            if (btnAddWeaponFragment != null)
                btnAddWeaponFragment.onClick.AddListener(() => 
                    ExecutePreset("addmaterial WeaponFragment 100", "무기 파편 100개"));
            
            if (btnAddWeaponCrystal != null)
                btnAddWeaponCrystal.onClick.AddListener(() => 
                    ExecutePreset("addmaterial WeaponCrystal 50", "무기 결정 50개"));
            
            if (btnAddArmorFragment != null)
                btnAddArmorFragment.onClick.AddListener(() => 
                    ExecutePreset("addmaterial ArmorFragment 100", "방어구 파편 100개"));
            
            if (btnAddArmorCrystal != null)
                btnAddArmorCrystal.onClick.AddListener(() => 
                    ExecutePreset("addmaterial ArmorCrystal 50", "방어구 결정 50개"));
            
            if (btnAddAccessoryFragment != null)
                btnAddAccessoryFragment.onClick.AddListener(() => 
                    ExecutePreset("addmaterial AccessoryFragment 100", "악세사리 파편 100개"));
            
            // 골드 버튼
            if (btnAdd10kGold != null)
                btnAdd10kGold.onClick.AddListener(() => 
                    ExecutePreset("addgold 10000", "골드 10,000G"));
            
            if (btnAdd100kGold != null)
                btnAdd100kGold.onClick.AddListener(() => 
                    ExecutePreset("addgold 100000", "골드 100,000G"));
            
            if (btnAdd1mGold != null)
                btnAdd1mGold.onClick.AddListener(() => 
                    ExecutePreset("addgold 1000000", "골드 1,000,000G"));
        }
        
        // ========================================
        // 이벤트 핸들러
        // ========================================
        
        private void OnExecuteButtonClicked()
        {
            if (commandInputField != null)
            {
                ExecuteCommand(commandInputField.text);
            }
        }
        
        private void OnCommandSubmit(string command)
        {
            ExecuteCommand(command);
        }
        
        private void OnHelpButtonClicked()
        {
            string helpText = DebugCheatManager.Instance.GetHelpText();
            ShowResult(helpText, true);
            
            if (showDebugLogs)
                Debug.Log(helpText);
        }
        
        private void ExecutePreset(string command, string description)
        {
            if (showDebugLogs)
                Debug.Log($"🎮 [CheatUI] 프리셋 실행: {description} ({command})");
            
            // InputField에 명령어 표시
            if (commandInputField != null)
                commandInputField.text = command;
            
            ExecuteCommand(command);
        }
        
        private void ExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                ShowResult("❌ 명령어가 비어있습니다.", false);
                return;
            }
            
            // CheatManager를 통해 실행
            var result = DebugCheatManager.Instance.ExecuteCommand(command);
            
            // 결과 표시
            ShowResult(result.message, result.success);
            
            // InputField 초기화
            if (result.success && commandInputField != null)
            {
                commandInputField.text = "";
            }
        }
        
        private void ShowResult(string message, bool success)
        {
            if (resultText != null)
            {
                resultText.text = message;
                resultText.color = success ? Color.green : Color.red;
            }
            
            if (showDebugLogs)
            {
                if (success)
                    Debug.Log($"✅ [CheatUI] {message}");
                else
                    Debug.LogWarning($"❌ [CheatUI] {message}");
            }
        }
        
        /// <summary>
        /// 패널 강제 열기
        /// </summary>
        public void OpenPanel()
        {
            gameObject.SetActive(true);
            
            if (commandInputField != null)
            {
                commandInputField.Select();
                commandInputField.ActivateInputField();
            }
        }
        
        /// <summary>
        /// 패널 강제 닫기
        /// </summary>
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }
    }
}

