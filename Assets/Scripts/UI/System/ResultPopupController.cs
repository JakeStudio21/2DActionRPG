using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // ⭐ DOTween 추가

public class ResultPopupController : MonoBehaviour
{
    [Header("UI Elements - Basic")]
    public GameObject popupPanel;        // ResultPopupPanel
    public GameObject victoryImage;      // Victory 이미지
    public GameObject defeatImage;       // Defeat 이미지
    public Button confirmButton;         // OK 버튼
    public TextMeshProUGUI confirmButtonText; // 확인 버튼 텍스트 (선택사항)
    
    [Header("UI Elements - Reward Display")]
    public GameObject rewardPanel;       // 보상 표시 패널 (승리 시에만 활성화)
    public TextMeshProUGUI rewardGoldText;    // 골드 보상 텍스트 "골드: +1000"
    public TextMeshProUGUI rewardExpText;     // 경험치 보상 텍스트 "경험치: +500"
    public Transform rewardItemsContainer;    // 아이템 슬롯들이 들어갈 부모 Transform
    public GameObject itemSlotPrefab;         // ⭐ InventorySlot 프리팹 (등급 표시 지원)
    
    [Header("Item Layout Settings")]
    [SerializeField] private Vector2 largeSlotSize = new Vector2(110f, 110f);  // 1~6개: 큰 크기
    [SerializeField] private Vector2 smallSlotSize = new Vector2(80f, 80f);    // 7~12개: 작은 크기
    [SerializeField] private float largeSpacing = 20f;   // 큰 슬롯 간격
    [SerializeField] private float smallSpacing = 15f;   // 작은 슬롯 간격
    
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    void Awake()
    {
        popupPanel.SetActive(false);     // 팝업 패널 숨기기
        victoryImage.SetActive(false);   // Victory 이미지 숨기기
        defeatImage.SetActive(false);    // Defeat 이미지 숨기기
        
        // 보상 패널 초기화 (null 체크)
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
        
        confirmButton.onClick.AddListener(OnConfirm);
    }

    /// <summary>
    /// Legacy 메서드: 하위 호환성 유지
    /// </summary>
    public void Show(bool isVictory)
    {
        if (isVictory)
        {
            ShowVictory(new StageResultData(true, 0, 0));
        }
        else
        {
            ShowDefeat();
        }
    }
    
    /// <summary>
    /// 승리 팝업 표시 (보상 데이터 포함)
    /// </summary>
    public void ShowVictory(StageResultData resultData)
    {
        if (gameObject == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🏆 [ResultPopupController] 승리 팝업 표시 - 골드: {resultData.goldReward}, EXP: {resultData.expReward}, 아이템: {resultData.itemRewards.Count}개");
        }

        popupPanel.SetActive(true);
        
        // Victory 이미지 활성화
        victoryImage.SetActive(true);
        defeatImage.SetActive(false);
        
        // 버튼 텍스트 변경 (선택사항)
        if (confirmButtonText != null)
        {
            confirmButtonText.text = "확인";
        }
        
        // 보상 UI 표시
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            DisplayRewards(resultData);
        }
        else
        {
            Debug.LogWarning("⚠️ [ResultPopupController] rewardPanel이 할당되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 실패 팝업 표시 (보상 없음)
    /// </summary>
    public void ShowDefeat()
    {
        if (gameObject == null) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"💀 [ResultPopupController] 실패 팝업 표시");
        }

        popupPanel.SetActive(true);
        
        // Defeat 이미지 활성화
        victoryImage.SetActive(false);
        defeatImage.SetActive(true);
        
        // 버튼 텍스트 변경 (선택사항)
        if (confirmButtonText != null)
        {
            confirmButtonText.text = "로비로";
        }
        
        // 보상 UI 숨김
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 아이템 슬롯 등장 애니메이션 (페이드 인 + 스케일 업)
    /// </summary>
    private void AnimateItemSlot(GameObject slotObj, int index)
    {
        // CanvasGroup 추가 (페이드 애니메이션용)
        CanvasGroup canvasGroup = slotObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = slotObj.AddComponent<CanvasGroup>();
        }
        
        // 초기 상태 설정
        canvasGroup.alpha = 0f; // 투명
        slotObj.transform.localScale = Vector3.zero; // 크기 0
        
        // 순차 등장 딜레이 (0.1초씩 증가)
        float delay = 0.5f + (index * 0.1f);
        
        // 스케일 업 애니메이션 (0 → 1, OutBack 이징)
        slotObj.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack)
            .SetDelay(delay);
        
        // 페이드 인 애니메이션 (0 → 1, OutQuad 이징)
        canvasGroup.DOFade(1f, 0.3f)
            .SetEase(Ease.OutQuad)
            .SetDelay(delay);
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎬 [ResultPopupController] 슬롯 #{index} 애니메이션 시작 (딜레이: {delay}초)");
        }
    }
    
    /// <summary>
    /// 아이템 개수에 따라 레이아웃 조정 (가운데 정렬)
    /// 1개: ⬜⬜⬜█⬜⬜⬜ (가운데)
    /// 2개: ⬜⬜█ █⬜⬜⬜ (가운데에서 좌우로)
    /// 3개: ⬜⬜█ █ █⬜⬜ (가운데에서 좌우로)
    /// </summary>
    private void AdjustItemLayout(int itemCount)
    {
        // Grid Layout Group 가져오기
        UnityEngine.UI.GridLayoutGroup gridLayout = rewardItemsContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        
        if (gridLayout == null)
        {
            Debug.LogWarning("⚠️ [ResultPopupController] GridLayoutGroup을 찾을 수 없습니다!");
            return;
        }
        
        // 아이템 개수에 따라 Cell Size 및 Spacing 조정
        if (itemCount <= 6)
        {
            // 1~6개: 큰 크기 (110×110px, 간격 20px)
            gridLayout.cellSize = largeSlotSize;
            gridLayout.spacing = new Vector2(largeSpacing, largeSpacing);
            gridLayout.constraintCount = 6; // 한 줄 최대 6개
            
            if (enableDebugLogs)
            {
                Debug.Log($"📐 [ResultPopupController] 레이아웃: 큰 크기 ({largeSlotSize}), 한 줄 최대 6개, 아이템: {itemCount}개");
            }
        }
        else // 7~12개
        {
            // 7~12개: 작은 크기 (80×80px, 간격 15px)
            gridLayout.cellSize = smallSlotSize;
            gridLayout.spacing = new Vector2(smallSpacing, smallSpacing);
            gridLayout.constraintCount = 6; // 한 줄 최대 6개 (2줄)
            
            if (enableDebugLogs)
            {
                Debug.Log($"📐 [ResultPopupController] 레이아웃: 작은 크기 ({smallSlotSize}), 2줄 (최대 6개/줄), 아이템: {itemCount}개");
            }
        }
        
        // ⭐ 가운데 정렬 (아이템이 가운데서부터 좌우로 늘어남)
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        
        // Constraint 설정 확인 (Fixed Column Count)
        gridLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
    }
    
    /// <summary>
    /// 보상 UI 표시
    /// </summary>
    private void DisplayRewards(StageResultData resultData)
    {
        // 골드 카운팅 애니메이션 (0 → 최종값)
        if (rewardGoldText != null)
        {
            rewardGoldText.text = "골드: +0";
            
            // DOTween 카운팅 애니메이션
            DOTween.To(() => 0, 
                x => rewardGoldText.text = $"골드: +{x}", 
                resultData.goldReward, 
                1.0f)
                .SetEase(Ease.OutQuad)
                .SetDelay(0.3f); // 팝업 등장 후 시작
        }
        else
        {
            Debug.LogWarning("⚠️ [ResultPopupController] rewardGoldText가 할당되지 않았습니다!");
        }
        
        // 경험치 카운팅 애니메이션 (0 → 최종값)
        if (rewardExpText != null)
        {
            rewardExpText.text = "경험치: +0";
            
            // DOTween 카운팅 애니메이션
            DOTween.To(() => 0, 
                x => rewardExpText.text = $"경험치: +{x}", 
                resultData.expReward, 
                1.0f)
                .SetEase(Ease.OutQuad)
                .SetDelay(0.3f); // 팝업 등장 후 시작
        }
        else
        {
            Debug.LogWarning("⚠️ [ResultPopupController] rewardExpText가 할당되지 않았습니다!");
        }
        
        // 아이템 + 재료 슬롯 동적 생성
        if (rewardItemsContainer != null && itemSlotPrefab != null)
        {
            // 기존 슬롯 제거
            foreach (Transform child in rewardItemsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // ⭐ 장비 + 재료 개수 합산
            int equipmentCount = resultData.itemRewards != null ? resultData.itemRewards.Count : 0;
            int materialCount = resultData.materialRewards != null ? resultData.materialRewards.Count : 0;
            int totalRewardCount = equipmentCount + materialCount;
            
            // 보상이 없으면 컨테이너 숨김
            if (totalRewardCount == 0)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"📦 [ResultPopupController] 보상 아이템이 없습니다.");
                }
                
                rewardItemsContainer.gameObject.SetActive(false);
                return;
            }
            
            // 컨테이너 활성화
            rewardItemsContainer.gameObject.SetActive(true);
            
            // 아이템 개수에 따라 레이아웃 조정
            AdjustItemLayout(totalRewardCount);
            
            if (enableDebugLogs)
            {
                Debug.Log($"📦 [ResultPopupController] 보상 표시: 장비 {equipmentCount}개 + 재료 {materialCount}개 = 총 {totalRewardCount}개");
            }
            
            int slotIndex = 0;
            
            // ⭐ 1단계: 장비 아이템 슬롯 생성
            if (resultData.itemRewards != null)
            {
                foreach (var itemReward in resultData.itemRewards)
                {
                    // 프리팹 인스턴스 생성
                    GameObject slotObj = Instantiate(itemSlotPrefab, rewardItemsContainer);
                    
                    // InventorySlot 컴포넌트 가져오기
                    InventorySlot inventorySlot = slotObj.GetComponent<InventorySlot>();
                    
                    if (inventorySlot != null)
                    {
                        // EquipmentData 로드
                        var equipmentData = ItemTemplateResolver.Load(itemReward.itemId);
                        
                        if (equipmentData != null)
                        {
                            // ⭐ 장비 데이터 설정 (ItemInstanceID 전달로 동적 스탯 지원!)
                            inventorySlot.SetEquipmentData(equipmentData, itemReward.instanceId);
                            
                            // ⭐ 등장 애니메이션 (페이드 인 + 스케일 업)
                            AnimateItemSlot(slotObj, slotIndex);
                            
                            if (enableDebugLogs)
                            {
                                Debug.Log($"✅ [ResultPopupController] 장비 슬롯 생성: {itemReward.itemId} (등급: {equipmentData.itemGrade})");
                            }
                            
                            slotIndex++; // 다음 슬롯 인덱스
                        }
                        else
                        {
                            Debug.LogError($"⚠️ [ResultPopupController] EquipmentData 로드 실패: {itemReward.itemId}");
                            Destroy(slotObj); // 로드 실패 시 슬롯 삭제
                        }
                    }
                    else
                    {
                        Debug.LogError($"⚠️ [ResultPopupController] InventorySlot 컴포넌트를 찾을 수 없습니다! Prefab: {itemSlotPrefab.name}");
                        Destroy(slotObj);
                    }
                }
            }
            
            // ⭐ 2단계: 재료 아이템 슬롯 생성
            if (resultData.materialRewards != null)
            {
                foreach (var materialStack in resultData.materialRewards)
                {
                    // 프리팹 인스턴스 생성
                    GameObject slotObj = Instantiate(itemSlotPrefab, rewardItemsContainer);
                    
                    // InventorySlot 컴포넌트 가져오기
                    InventorySlot inventorySlot = slotObj.GetComponent<InventorySlot>();
                    
                    if (inventorySlot != null)
                    {
                        // ⭐ Instantiate() 후 1프레임 대기 (Unity Layout Group 재계산 완료 대기)
                        StartCoroutine(SetupMaterialSlotDelayed(inventorySlot, materialStack, slotObj, slotIndex));
                        
                        slotIndex++; // 다음 슬롯 인덱스
                    }
                    else
                    {
                        Debug.LogError($"⚠️ [ResultPopupController] InventorySlot 컴포넌트를 찾을 수 없습니다!");
                        Destroy(slotObj);
                    }
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"📦 [ResultPopupController] 슬롯 생성 완료: 장비 {equipmentCount}개 + 재료 {materialCount}개");
            }
        }
        else
        {
            if (rewardItemsContainer == null)
            {
                Debug.LogWarning("⚠️ [ResultPopupController] rewardItemsContainer가 할당되지 않았습니다!");
            }
            
            if (itemSlotPrefab == null)
            {
                Debug.LogWarning("⚠️ [ResultPopupController] itemSlotPrefab이 할당되지 않았습니다!");
            }
        }
    }
    
    /// <summary>
    /// ⭐ Instantiate() 후 1프레임 대기하여 Layout Group 재계산 완료 후 SetupMaterial 호출
    /// (Unity Layout Group 버그 해결: Instantiate() 직후 SetupMaterial()하면 Image.enabled가 false로 변경됨)
    /// </summary>
    IEnumerator SetupMaterialSlotDelayed(InventorySlot inventorySlot, MaterialStack materialStack, GameObject slotObj, int slotIndex)
    {
        // ⭐ 1프레임 대기 (Layout Group 재계산 완료 대기)
        yield return null;
        
        // ⭐ 재료 데이터 설정 (SetupMaterial 사용)
        inventorySlot.SetupMaterial(materialStack);
        
        // ⭐ 등장 애니메이션 (페이드 인 + 스케일 업)
        AnimateItemSlot(slotObj, slotIndex);
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [ResultPopupController] 재료 슬롯 생성 완료: {materialStack.materialType.GetDisplayName()} x{materialStack.count}");
        }
    }
    
    void OnConfirm()
    {
        // 중복 클릭 방지를 위해 리스너를 잠시 제거하고, 코루틴을 통해 로비로 돌아갑니다.
        confirmButton.interactable = false;
        StartCoroutine(ReturnToLobbyRoutine());
    }

    IEnumerator ReturnToLobbyRoutine()
    {
        Time.timeScale = 1f;

        // 🔧 의미 있는 이벤트: 로비 복귀 → 저장
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("ResultPopup_ReturnToLobby");
        }

        // ✅ Unity가 자동으로 오브젝트를 정리하므로 수동 파괴 제거
        // 씬 전환 시 모든 오브젝트는 자동으로 정리됨
        
        yield return new WaitForEndOfFrame();

        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

    // void OnConfirm()
    // {
    //     Time.timeScale = 1f; // 혹시 멈춰있으면 재개
    //     SceneManager.LoadScene("Lobby");
    // }
} 