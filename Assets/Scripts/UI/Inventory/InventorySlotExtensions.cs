using UnityEngine;
using Systems;

namespace UI.Inventory
{
    /// <summary>
    /// 인벤토리 슬롯 확장 메서드
    /// - 기존 InventorySlot에 귀속 상태 표시 기능 추가
    /// </summary>
    public static class InventorySlotExtensions
    {
        /// <summary>
        /// 인벤토리 슬롯에 귀속 상태 표시
        /// </summary>
        public static void UpdateBindStatus(this GameObject slotObject, ItemInstanceId itemId)
        {
            if (slotObject == null)
                return;
            
            var bindStatusUI = slotObject.GetComponentInChildren<BindStatusUI>();
            if (bindStatusUI != null)
            {
                bindStatusUI.UpdateBindStatus(itemId);
            }
        }
        
        /// <summary>
        /// 귀속 툴팁 가져오기
        /// </summary>
        public static string GetBindTooltip(this GameObject slotObject)
        {
            if (slotObject == null)
                return "";
            
            var bindStatusUI = slotObject.GetComponentInChildren<BindStatusUI>();
            if (bindStatusUI != null)
            {
                return bindStatusUI.GetBindTooltip();
            }
            
            return "";
        }
        
        /// <summary>
        /// 현재 캐릭터가 장착 가능한지 확인
        /// </summary>
        public static bool CanEquipByCurrentCharacter(this GameObject slotObject)
        {
            if (slotObject == null)
                return false;
            
            var bindStatusUI = slotObject.GetComponentInChildren<BindStatusUI>();
            if (bindStatusUI != null)
            {
                return bindStatusUI.CanEquipByCurrentCharacter();
            }
            
            return true; // BindStatusUI가 없으면 장착 가능으로 간주
        }
        
        /// <summary>
        /// 계정 창고로 이동 가능한지 확인
        /// </summary>
        public static bool CanMoveToAccountStorage(this GameObject slotObject)
        {
            if (slotObject == null)
                return false;
            
            var bindStatusUI = slotObject.GetComponentInChildren<BindStatusUI>();
            if (bindStatusUI != null)
            {
                return bindStatusUI.CanMoveToAccountStorage();
            }
            
            return true; // BindStatusUI가 없으면 이동 가능으로 간주
        }
    }
}

