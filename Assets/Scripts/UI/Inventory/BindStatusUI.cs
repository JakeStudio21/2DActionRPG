using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Systems;

namespace UI.Inventory
{
    /// <summary>
    /// 귀속 상태 UI 표시
    /// - 인벤토리/장비창 슬롯에 귀속 아이콘 표시
    /// - 툴팁에 귀속 정보 표시
    /// </summary>
    public class BindStatusUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject bindIcon;          // 🔒 아이콘
        [SerializeField] private TextMeshProUGUI bindText;     // "귀속됨" 텍스트
        [SerializeField] private Image bindIconImage;          // 아이콘 이미지
        
        [Header("Settings")]
        [SerializeField] private Color boundColor = new Color(1f, 0.3f, 0.3f); // 귀속 (빨강)
        [SerializeField] private Color unboundColor = new Color(0.8f, 0.8f, 0.8f); // 미귀속 (회색)
        [SerializeField] private bool enableDebugLogs = false;
        
        private ItemInstanceId _currentItemId;
        private bool _isBound;
        private int _boundCharacterSlot = -1;
        
        private void Awake()
        {
            if (bindIcon != null)
            {
                bindIcon.SetActive(false);
            }
        }
        
        /// <summary>
        /// 아이템의 귀속 상태 업데이트
        /// </summary>
        public void UpdateBindStatus(ItemInstanceId itemId)
        {
            _currentItemId = itemId;
            
            if (!itemId.IsValid() || !AccountDataManager.IsInitialized())
            {
                Hide();
                return;
            }
            
            var account = AccountDataManager.Instance;
            var bindInfo = account.GetBindInfo(itemId);
            
            _isBound = bindInfo.isBound;
            _boundCharacterSlot = bindInfo.characterSlotIndex;
            
            if (_isBound)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        /// <summary>
        /// 귀속 아이콘 표시
        /// </summary>
        private void Show()
        {
            if (bindIcon != null)
            {
                bindIcon.SetActive(true);
            }
            
            if (bindIconImage != null)
            {
                bindIconImage.color = boundColor;
            }
            
            if (bindText != null)
            {
                var playerData = PlayerDataManager.Instance;
                string characterName = "Unknown";
                
                if (playerData != null && _boundCharacterSlot >= 0)
                {
                    var slotData = playerData.GetSlotData(_boundCharacterSlot);
                    if (slotData != null)
                    {
                        characterName = slotData.playerName;
                    }
                }
                
                bindText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(boundColor)}>🔒 {characterName}</color>";
            }
            
            Log($"[BindStatusUI] 귀속 아이콘 표시: {_currentItemId.id.Substring(0, 8)}... → 슬롯 {_boundCharacterSlot}");
        }
        
        /// <summary>
        /// 귀속 아이콘 숨기기
        /// </summary>
        private void Hide()
        {
            if (bindIcon != null)
            {
                bindIcon.SetActive(false);
            }
        }
        
        /// <summary>
        /// 툴팁용 귀속 정보 가져오기
        /// </summary>
        public string GetBindTooltip()
        {
            if (!_isBound)
            {
                return "<color=yellow>⚠️ 장착 시 캐릭터에 귀속됩니다</color>";
            }
            
            var playerData = PlayerDataManager.Instance;
            if (playerData == null || _boundCharacterSlot < 0)
            {
                return "<color=red>🔒 귀속됨</color>";
            }
            
            var slotData = playerData.GetSlotData(_boundCharacterSlot);
            if (slotData == null)
            {
                return "<color=red>🔒 귀속됨</color>";
            }
            
            int currentSlot = playerData.CurrentSlotIndex;
            
            if (_boundCharacterSlot == currentSlot)
            {
                return $"<color=green>🔒 {slotData.playerName}에게 귀속됨 (현재 캐릭터)</color>";
            }
            else
            {
                return $"<color=red>🔒 {slotData.playerName}에게 귀속됨\n(다른 캐릭터는 장착 불가)</color>";
            }
        }
        
        /// <summary>
        /// 현재 캐릭터가 장착 가능한지 확인
        /// </summary>
        public bool CanEquipByCurrentCharacter()
        {
            if (!_isBound)
            {
                return true; // 귀속되지 않은 아이템은 누구나 장착 가능
            }
            
            var playerData = PlayerDataManager.Instance;
            if (playerData == null || !playerData.IsSlotSelected)
            {
                return false;
            }
            
            return _boundCharacterSlot == playerData.CurrentSlotIndex;
        }
        
        /// <summary>
        /// 계정 창고로 이동 가능한지 확인
        /// </summary>
        public bool CanMoveToAccountStorage()
        {
            return !_isBound; // 귀속되지 않은 아이템만 계정 창고로 이동 가능
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

