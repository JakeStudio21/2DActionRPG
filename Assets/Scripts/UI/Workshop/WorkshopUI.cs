using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace UI.Workshop
{
    /// <summary>
    /// 🏭 공방(제작) UI 메인 컨트롤러
    /// 책임:
    /// - 강화/합성/분해 탭 전환 관리
    /// - 서브 패널 활성화/비활성화
    /// - 공통 UI 요소 관리 (닫기 버튼 등)
    /// 
    /// 의존성:
    /// - EnhancementUI (강화 UI)
    /// - FusionUI (합성 UI)
    /// - DismantleUI (분해 UI)
    /// </summary>
    public class WorkshopUI : MonoBehaviour
    {
        /// <summary>
        /// 공방 탭 타입
        /// </summary>
        public enum WorkshopTabType
        {
            Enhancement,    // 강화
            Fusion,         // 합성
            Dismantle       // 분해
        }
        
        [Header("📑 탭 버튼")]
        [SerializeField] private Button enhancementTabButton;   // 강화 탭 버튼
        [SerializeField] private Button fusionTabButton;        // 합성 탭 버튼
        [SerializeField] private Button dismantleTabButton;     // 분해 탭 버튼
        
        [Header("📑 탭 텍스트")]
        [SerializeField] private TMP_Text enhancementTabText;   // 강화 탭 텍스트
        [SerializeField] private TMP_Text fusionTabText;        // 합성 탭 텍스트
        [SerializeField] private TMP_Text dismantleTabText;     // 분해 탭 텍스트
        
        [Header("📦 서브 패널")]
        [SerializeField] private GameObject enhancementSubPanel;  // 강화 UI 패널
        [SerializeField] private GameObject fusionSubPanel;       // 합성 UI 패널
        [SerializeField] private GameObject dismantleSubPanel;    // 분해 UI 패널
        
        [Header("🔘 공통 버튼")]
        [SerializeField] private Button closeButton;            // 닫기 버튼 (X)
        
        [Header("🔗 연동 컴포넌트")]
        [SerializeField] private WorkshopInventoryUI workshopInventoryUI;
        
        [Header("🎮 UI Controllers")]
        [SerializeField] private EnhancementUI enhancementUI;
        [SerializeField] private FusionUI fusionUI;
        [SerializeField] private DismantleUI dismantleUI;
        
        [Header("📊 디버그")]
        
        // 현재 활성 탭
        private WorkshopTabType currentTab = WorkshopTabType.Enhancement;
        
        // 이벤트
        public event Action<WorkshopTabType> OnTabChanged;
        
        void Awake()
        {
        }
        
        void Start()
        {
            SetupEventListeners();
            
            // 초기 탭 설정 (강화)
            SwitchTab(WorkshopTabType.Enhancement);
            
        }
        
        /// <summary>
        /// 이벤트 리스너 연결
        /// </summary>
        private void SetupEventListeners()
        {
            // 탭 버튼 이벤트
            if (enhancementTabButton != null)
            {
                enhancementTabButton.onClick.AddListener(() => SwitchTab(WorkshopTabType.Enhancement));
            }
            else
            {
                Debug.LogError("🔴 [WorkshopUI] enhancementTabButton이 null입니다!");
            }
            
            if (fusionTabButton != null)
            {
                fusionTabButton.onClick.AddListener(() => SwitchTab(WorkshopTabType.Fusion));
            }
            else
            {
                Debug.LogError("🔴 [WorkshopUI] fusionTabButton이 null입니다!");
            }
            
            if (dismantleTabButton != null)
            {
                dismantleTabButton.onClick.AddListener(() => SwitchTab(WorkshopTabType.Dismantle));
            }
            else
            {
                Debug.LogError("🔴 [WorkshopUI] dismantleTabButton이 null입니다!");
            }
            
            // 닫기 버튼 이벤트
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
            else
            {
                Debug.LogError("🔴 [WorkshopUI] closeButton이 null입니다!");
            }
        }
        
        /// <summary>
        /// 탭 전환
        /// </summary>
        public void SwitchTab(WorkshopTabType tab)
        {
            
            currentTab = tab;
            
            // 모든 서브 패널 비활성화
            DeactivateAllSubPanels();
            
            // 선택된 서브 패널만 활성화
            switch (tab)
            {
                case WorkshopTabType.Enhancement:
                    if (enhancementSubPanel != null)
                    {
                        enhancementSubPanel.SetActive(true);
                        
                        // ⭐ EnhancementUI 명시적 초기화 (탭 전환 시 상태 초기화)
                        if (enhancementUI != null)
                        {
                            enhancementUI.Initialize();
                        }
                        else
                        {
                            Debug.LogError("🔴 [WorkshopUI] EnhancementUI 참조가 null입니다!");
                        }
                        
                    }
                    break;
                    
                case WorkshopTabType.Fusion:
                    if (fusionSubPanel != null)
                    {
                        fusionSubPanel.SetActive(true);
                        
                        // ⭐ FusionUI 명시적 초기화 (탭 전환 시 상태 초기화)
                        if (fusionUI != null)
                        {
                            fusionUI.Initialize();
                        }
                        else
                        {
                            Debug.LogError("🔴 [WorkshopUI] FusionUI 참조가 null입니다!");
                        }
                        
                    }
                    break;
                    
                case WorkshopTabType.Dismantle:
                    if (dismantleSubPanel != null)
                    {
                        dismantleSubPanel.SetActive(true);
                        
                        // ⭐ DismantleUI 명시적 초기화
                        if (dismantleUI != null)
                        {
                            dismantleUI.Initialize();
                        }
                        else
                        {
                            Debug.LogError("🔴 [WorkshopUI] DismantleUI 참조가 null입니다!");
                        }
                        
                    }
                    break;
            }
            
            // 탭 버튼 상태 업데이트
            UpdateTabButtonStates();
            
            // ⭐ 탭 전환 시 항상 다중 선택 모드 OFF (디폴트: 단일 선택)
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.SetMultiSelectMode(false);
                
            }
            
            // 이벤트 발행
            OnTabChanged?.Invoke(tab);
        }
        
        /// <summary>
        /// 모든 서브 패널 비활성화
        /// </summary>
        private void DeactivateAllSubPanels()
        {
            if (enhancementSubPanel != null)
                enhancementSubPanel.SetActive(false);
            
            if (fusionSubPanel != null)
                fusionSubPanel.SetActive(false);
            
            if (dismantleSubPanel != null)
                dismantleSubPanel.SetActive(false);
        }
        
        /// <summary>
        /// 탭 버튼 상태 업데이트 (활성/비활성 색상)
        /// </summary>
        private void UpdateTabButtonStates()
        {
            // 강화 탭
            if (enhancementTabText != null)
            {
                Color color = enhancementTabText.color;
                color.a = (currentTab == WorkshopTabType.Enhancement) ? 1.0f : 0.5f;
                enhancementTabText.color = color;
            }
            
            // 합성 탭
            if (fusionTabText != null)
            {
                Color color = fusionTabText.color;
                color.a = (currentTab == WorkshopTabType.Fusion) ? 1.0f : 0.5f;
                fusionTabText.color = color;
            }
            
            // 분해 탭
            if (dismantleTabText != null)
            {
                Color color = dismantleTabText.color;
                color.a = (currentTab == WorkshopTabType.Dismantle) ? 1.0f : 0.5f;
                dismantleTabText.color = color;
            }
            
        }
        
        /// <summary>
        /// 닫기 버튼 클릭 처리
        /// </summary>
        private void OnCloseButtonClicked()
        {
            
            // LobbyUIController를 통해 로비로 복귀
            var lobbyUIController = FindObjectOfType<LobbyUIController>();
            if (lobbyUIController != null)
            {
                lobbyUIController.OnBackToLobby();
            }
            else
            {
                Debug.LogError("🔴 [WorkshopUI] LobbyUIController를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 공방 패널 열릴 때 호출 (외부에서)
        /// </summary>
        public void OnPanelOpened()
        {
            
            // 초기 탭으로 리셋
            SwitchTab(WorkshopTabType.Enhancement);
            
            // 필요한 데이터 갱신 (Phase 2 이후)
        }
        
        /// <summary>
        /// 공방 패널 닫힐 때 호출 (외부에서)
        /// </summary>
        public void OnPanelClosed()
        {
            
            // 필요한 정리 작업 (Phase 2 이후)
        }
        
        /// <summary>
        /// 현재 활성 탭 반환
        /// </summary>
        public WorkshopTabType GetCurrentTab()
        {
            return currentTab;
        }
    }
}

