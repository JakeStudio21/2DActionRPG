using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 던전 버튼 UI 컴포넌트
/// 배경 이미지, 잠금 표시, 보상 슬롯 등 관리
/// </summary>
public class DungeonButtonUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;           // 배경 이미지
    public Image overlayImage;              // 잠금 오버레이 (어두운 반투명)
    public Image iconImage;                 // 던전 아이콘 (선택)
    public TextMeshProUGUI nameText;        // 던전 이름
    public TextMeshProUGUI levelText;       // 권장 레벨 ("권장 Lv.10")
    public GameObject lockIcon;             // 자물쇠 아이콘
    public Button button;                   // 버튼 컴포넌트
    
    [Header("Reward Slots")]
    public Transform rewardSlotsContainer;  // 보상 슬롯 컨테이너 ⭐
    public GameObject rewardSlotPrefab;     // InventorySlot 프리팹 ⭐
    
    private void Awake()
    {
        // 버튼 컴포넌트 자동 가져오기
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }
    
    /// <summary>
    /// 배경 이미지 설정
    /// </summary>
    public void SetBackgroundImage(Sprite sprite)
    {
        if (backgroundImage != null && sprite != null)
        {
            backgroundImage.sprite = sprite;
        }
    }
    
    /// <summary>
    /// 던전 이름 설정
    /// </summary>
    public void SetName(string text)
    {
        if (nameText != null)
        {
            nameText.text = text;
        }
    }
    
    /// <summary>
    /// 권장 레벨 설정 (잠금 상태에 따라 다르게 표시)
    /// </summary>
    public void SetRecommendedLevel(int level, bool isLocked = false)
    {
        if (levelText != null)
        {
            if (isLocked)
            {
                // 잠긴 던전: "요구레벨 Lv.20" 표시
                levelText.text = $"요구레벨 Lv.{level}";
                levelText.color = new Color(1f, 0.3f, 0.3f); // 빨간색 강조
            }
            else
            {
                // 해금된 던전: "권장 Lv.20" 표시
                levelText.text = $"권장 Lv.{level}";
                levelText.color = Color.white; // 기본 흰색
            }
        }
    }
    
    /// <summary>
    /// 잠금 상태 설정
    /// </summary>
    public void SetLocked(bool isLocked)
    {
        if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(isLocked);
        }
        
        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
        }
        
        if (nameText != null && isLocked)
        {
            nameText.color = Color.gray;
        }
    }
    
    /// <summary>
    /// 버튼 활성화 상태 설정
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
    
    /// <summary>
    /// 보상 슬롯 설정 (최대 3개) - 코루틴 버전 ⭐
    /// </summary>
    public void SetRewardSlots(List<DungeonSelectPanelController.RewardSlotData> rewards)
    {
        StartCoroutine(SetRewardSlotsAsync(rewards));
    }
    
    /// <summary>
    /// 보상 슬롯 설정 (비동기) - Layout Group 버그 방지
    /// </summary>
    private System.Collections.IEnumerator SetRewardSlotsAsync(List<DungeonSelectPanelController.RewardSlotData> rewards)
    {
        if (rewardSlotsContainer == null || rewardSlotPrefab == null)
        {
            Debug.LogWarning("[DungeonButtonUI] RewardSlotsContainer 또는 RewardSlotPrefab이 설정되지 않았습니다.");
            yield break;
        }
        
        // 기존 슬롯 제거
        foreach (Transform child in rewardSlotsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // 최대 3개만 표시
        int maxSlots = Mathf.Min(rewards.Count, 3);
        
        for (int i = 0; i < maxSlots; i++)
        {
            var rewardData = rewards[i];
            
            // 슬롯 생성
            GameObject slotObj = Instantiate(rewardSlotPrefab, rewardSlotsContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            
            if (slot == null)
            {
                Debug.LogError("[DungeonButtonUI] InventorySlot 컴포넌트를 찾을 수 없습니다!");
                Destroy(slotObj);
                continue;
            }
            
            // ⭐ 슬롯 크기를 80x80으로 강제 설정
            RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(80f, 80f);
            }
        }
        
        // ⭐ UI 레이아웃 그룹 업데이트를 위해 1프레임 대기
        yield return new WaitForEndOfFrame();
        
        // ⭐ 이제 슬롯 데이터 설정 (Layout Group 재계산 완료 후)
        int slotIndex = 0;
        foreach (Transform child in rewardSlotsContainer)
        {
            if (slotIndex >= maxSlots) break;
            
            var rewardData = rewards[slotIndex];
            InventorySlot slot = child.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                // 슬롯 데이터 설정
                if (rewardData.rewardType == DungeonSelectPanelController.RewardSlotData.RewardType.Equipment)
                {
                    // 장비 아이템
                    if (rewardData.equipmentData != null)
                    {
                        slot.SetEquipmentData(rewardData.equipmentData);
                    }
                }
                else if (rewardData.rewardType == DungeonSelectPanelController.RewardSlotData.RewardType.Material)
                {
                    // 재료 아이템
                    if (rewardData.materialStack != null)
                    {
                        slot.SetupMaterial(rewardData.materialStack);
                    }
                }
            }
            
            slotIndex++;
        }
    }
}

