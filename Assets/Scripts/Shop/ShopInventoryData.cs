using System;
using System.Collections.Generic;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// 상점 인벤토리 데이터 (클래스별 + 장비 슬롯별)
    /// - 각 클래스의 특정 슬롯에 대한 상점 아이템 목록
    /// </summary>
    [System.Serializable]
    public class ShopInventoryData
    {
        /// <summary>
        /// 대상 클래스
        /// </summary>
        public PlayerClass targetClass;
        
        /// <summary>
        /// 장비 슬롯 (Weapon, Armor, Boots, Helmet, Belt, Gloves)
        /// </summary>
        public EquipmentSlot slot;
        
        /// <summary>
        /// 해당 슬롯의 아이템들 (D/C/B/A 등급, 최대 4개)
        /// </summary>
        public List<ShopItemEntry> items = new List<ShopItemEntry>();
        
        /// <summary>
        /// 생성자
        /// </summary>
        public ShopInventoryData(PlayerClass targetClass, EquipmentSlot slot)
        {
            this.targetClass = targetClass;
            this.slot = slot;
            this.items = new List<ShopItemEntry>();
        }
        
        /// <summary>
        /// 아이템 추가
        /// </summary>
        public void AddItem(ShopItemEntry item)
        {
            if (item != null && !items.Contains(item))
            {
                items.Add(item);
            }
        }
        
        /// <summary>
        /// 등급순 정렬 (D → C → B → A)
        /// </summary>
        public void SortByGrade()
        {
            items.Sort((a, b) => a.grade.CompareTo(b.grade));
        }
    }
    
    /// <summary>
    /// 상점 아이템 엔트리
    /// - 전시용 ItemInstance + EquipmentData
    /// </summary>
    [System.Serializable]
    public class ShopItemEntry
    {
        /// <summary>
        /// 전시용 ItemInstanceID (상점 UI에 표시)
        /// </summary>
        public ItemInstanceID displayInstanceId;
        
        /// <summary>
        /// 장비 데이터 (ScriptableObject)
        /// </summary>
        public EquipmentData equipmentData;
        
        /// <summary>
        /// 아이템 등급 (정렬용)
        /// </summary>
        public ItemGrade grade;
        
        /// <summary>
        /// 생성자
        /// </summary>
        public ShopItemEntry(ItemInstanceID displayInstanceId, EquipmentData equipmentData)
        {
            this.displayInstanceId = displayInstanceId;
            this.equipmentData = equipmentData;
            this.grade = equipmentData != null ? equipmentData.itemGrade : ItemGrade.D;
        }
        
        /// <summary>
        /// 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            return !displayInstanceId.IsEmpty && equipmentData != null;
        }
    }
    
    /// <summary>
    /// 상점 카테고리 타입 (UI 표시용)
    /// </summary>
    public enum ShopCategory
    {
        Weapon,   // 무기
        Armor,    // 갑옷
        Boots,    // 신발
        Helmet,   // 투구
        Belt,     // 벨트
        Gloves    // 장갑
    }
    
    /// <summary>
    /// ShopCategory ↔ EquipmentSlot 변환 헬퍼
    /// </summary>
    public static class ShopCategoryHelper
    {
        /// <summary>
        /// ShopCategory → EquipmentSlot 변환
        /// </summary>
        public static EquipmentSlot ToEquipmentSlot(ShopCategory category)
        {
            return category switch
            {
                ShopCategory.Weapon => EquipmentSlot.MainWeapon,
                ShopCategory.Armor => EquipmentSlot.Armor,
                ShopCategory.Boots => EquipmentSlot.Boots,
                ShopCategory.Helmet => EquipmentSlot.Helmet,
                ShopCategory.Belt => EquipmentSlot.Belt,
                ShopCategory.Gloves => EquipmentSlot.Gloves,
                _ => EquipmentSlot.MainWeapon
            };
        }
        
        /// <summary>
        /// 모든 상점 카테고리 반환
        /// </summary>
        public static List<ShopCategory> GetAllCategories()
        {
            return new List<ShopCategory>
            {
                ShopCategory.Weapon,
                ShopCategory.Armor,
                ShopCategory.Boots,
                ShopCategory.Helmet,
                ShopCategory.Belt,
                ShopCategory.Gloves
            };
        }
        
        /// <summary>
        /// 카테고리 한글 이름
        /// </summary>
        public static string GetCategoryName(ShopCategory category)
        {
            return category switch
            {
                ShopCategory.Weapon => "무기",
                ShopCategory.Armor => "갑옷",
                ShopCategory.Boots => "신발",
                ShopCategory.Helmet => "투구",
                ShopCategory.Belt => "벨트",
                ShopCategory.Gloves => "장갑",
                _ => "알 수 없음"
            };
        }
        
        /// <summary>
        /// 🎨 카테고리 아이콘 스프라이트 로드
        /// </summary>
        public static UnityEngine.Sprite GetCategoryIcon(ShopCategory category)
        {
            string iconFileName = category switch
            {
                ShopCategory.Weapon => "IconSet_Equip_Weapon",
                ShopCategory.Armor => "IconSet_Equip_Armor",
                ShopCategory.Boots => "IconSet_Equip_Boots",
                ShopCategory.Helmet => "IconSet_Equip_Helmet",
                ShopCategory.Belt => "IconSet_Equip_Belt",
                ShopCategory.Gloves => "IconSet_Equip_Glove",  // ⚠️ "Glove" (단수)
                _ => null
            };
            
            if (string.IsNullOrEmpty(iconFileName))
            {
                UnityEngine.Debug.LogWarning($"⚠️ [ShopCategoryHelper] 알 수 없는 카테고리: {category}");
                return null;
            }
            
            // 🔧 Resources.Load 시도 (여러 경로)
            // 1순위: Assets/Resources/IconMisc/{iconFileName}
            string resourcePath = $"IconMisc/{iconFileName}";
            UnityEngine.Sprite sprite = UnityEngine.Resources.Load<UnityEngine.Sprite>(resourcePath);
            
            // 2순위: Assets/Resources/Sprites/Component/IconMisc/{iconFileName}
            if (sprite == null)
            {
                resourcePath = $"Sprites/Component/IconMisc/{iconFileName}";
                sprite = UnityEngine.Resources.Load<UnityEngine.Sprite>(resourcePath);
            }
            
            if (sprite == null)
            {
                UnityEngine.Debug.LogWarning($"⚠️ [ShopCategoryHelper] 아이콘을 찾을 수 없습니다. 다음 경로를 확인하세요:");
                UnityEngine.Debug.LogWarning($"   - Assets/Resources/IconMisc/{iconFileName}.png");
                UnityEngine.Debug.LogWarning($"   - Assets/Resources/Sprites/Component/IconMisc/{iconFileName}.png");
            }
            else
            {
                UnityEngine.Debug.Log($"✅ [ShopCategoryHelper] 아이콘 로드 성공: {resourcePath}");
            }
            
            return sprite;
        }
    }
}

