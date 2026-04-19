using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 아이템 슬롯 UI 컴포넌트
/// ResultPopupController의 보상 아이템 표시용
/// </summary>
public class ItemSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image itemIcon;           // 아이템 아이콘
    [SerializeField] private TextMeshProUGUI itemCountText; // 수량 텍스트 "×3"
    [SerializeField] private GameObject countPanel;    // 수량 배경 패널 (선택사항)
    
    
    /// <summary>
    /// 아이템 데이터 설정
    /// </summary>
    public void Setup(string itemId, int amount)
    {
        
        // 아이템 아이콘 로드
        LoadItemIcon(itemId);
        
        // 수량 표시
        if (itemCountText != null)
        {
            itemCountText.text = $"×{amount}";
            
            // 수량이 1개면 텍스트 숨김 (선택사항)
            if (amount <= 1)
            {
                itemCountText.gameObject.SetActive(false);
                if (countPanel != null)
                {
                    countPanel.SetActive(false);
                }
            }
            else
            {
                itemCountText.gameObject.SetActive(true);
                if (countPanel != null)
                {
                    countPanel.SetActive(true);
                }
            }
        }
    }
    
    /// <summary>
    /// 아이템 아이콘 로드
    /// </summary>
    private void LoadItemIcon(string itemId)
    {
        if (itemIcon == null)
        {
            Debug.LogWarning("⚠️ [ItemSlotUI] itemIcon이 할당되지 않았습니다!");
            return;
        }
        
        // ItemTemplateResolver로 EquipmentData 로드
        var equipmentData = ItemTemplateResolver.Load(itemId);
        
        if (equipmentData != null && equipmentData.icon != null)
        {
            itemIcon.sprite = equipmentData.icon;
            itemIcon.enabled = true;
            
        }
        else
        {
            // 기본 아이콘 또는 숨김 처리
            itemIcon.enabled = false;
            
        }
    }
    
    /// <summary>
    /// 슬롯 정리 (풀링 재사용 시)
    /// </summary>
    public void Clear()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }
        
        if (itemCountText != null)
        {
            itemCountText.text = "";
            itemCountText.gameObject.SetActive(false);
        }
        
        if (countPanel != null)
        {
            countPanel.SetActive(false);
        }
    }
}

