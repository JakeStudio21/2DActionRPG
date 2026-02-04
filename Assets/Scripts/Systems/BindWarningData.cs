using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 귀속 경고 데이터
    /// - 아이템 장착 시 귀속 여부를 사용자에게 경고
    /// </summary>
    [System.Serializable]
    public class BindWarningData
    {
        public ItemInstanceId itemInstanceId;
        public string itemTemplateName;
        public int enhancementLevel;
        public EquipmentSlot targetSlot;
        public int currentCharacterSlot;
        public string characterName;
        
        public BindWarningData(ItemInstanceId itemId, string templateName, int enhancement, 
                                EquipmentSlot slot, int charSlot, string charName)
        {
            itemInstanceId = itemId;
            itemTemplateName = templateName;
            enhancementLevel = enhancement;
            targetSlot = slot;
            currentCharacterSlot = charSlot;
            characterName = charName;
        }
        
        /// <summary>
        /// 경고 메시지 생성
        /// </summary>
        public string GetWarningMessage()
        {
            string itemName = $"{itemTemplateName}";
            if (enhancementLevel > 0)
            {
                itemName += $"+{enhancementLevel}";
            }
            
            return $"<b>{itemName}</b>\n\n" +
                   $"이 아이템은 장착 시 <color=red><b>캐릭터에 귀속</b></color>됩니다.\n\n" +
                   $"귀속된 아이템은:\n" +
                   $"• 다른 캐릭터가 장착할 수 없습니다\n" +
                   $"• 계정 창고로 이동할 수 없습니다\n" +
                   $"• <color=yellow>캐릭터 가방에서만 보관 가능</color>합니다\n\n" +
                   $"정말 장착하시겠습니까?";
        }
        
        /// <summary>
        /// 간단한 경고 메시지 (UI 툴팁용)
        /// </summary>
        public string GetSimpleMessage()
        {
            return $"장착 시 <color=red>{characterName}</color>에게 귀속됩니다.";
        }
    }
}

