using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어 이름 UI 관리
/// </summary>
public class PlayerNameUI : MonoBehaviour
{
    private TextMeshProUGUI _nameText;
    private bool _isSubscribed = false;
    
    private void Awake()
    {
        _nameText = GetComponent<TextMeshProUGUI>();
        _nameText.text = "Player"; // 기본값
    }
    
    private void Update()
    {
        if (!_isSubscribed && PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            // 현재 플레이어 이름 표시
            string playerName = PlayerDataManager.Instance.selectedPlayerData.playerName;
            _nameText.text = playerName;
            _isSubscribed = true;
            
        }
    }
}
