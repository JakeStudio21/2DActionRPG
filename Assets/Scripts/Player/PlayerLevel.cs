using System;
using UnityEngine;

public class PlayerLevel : Singleton<PlayerLevel>
{
    [SerializeField] private int _currentLevel = 1;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _expToNextLevel = 100;
    
    public int CurrentLevel => _currentLevel;

    // 레벨이 변경될 때 다른 스크립트에 알려주기 위한 이벤트입니다.
    public event Action OnLevelChanged;

    private void Start()
    {
        // 게임 시작 시 UI를 초기화하기 위해 이벤트를 한번 발생시킵니다.
        OnLevelChanged?.Invoke();
    }

    /// <summary>
    /// 플레이어에게 경험치를 추가하고 레벨업을 확인합니다.
    /// </summary>
    /// <param name="expToAdd">추가할 경험치 양</param>
    public void AddExp(int expToAdd)
    {
        _currentExp += expToAdd;
        Debug.Log($"경험치 {expToAdd} 획득! 현재 경험치: {_currentExp}/{_expToNextLevel}");

        while (_currentExp >= _expToNextLevel)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        _currentExp -= _expToNextLevel;
        _currentLevel++;

        // TODO: 다음 레벨에 필요한 경험치를 동적으로 설정할 수 있습니다. (예: _expToNextLevel *= 1.5f;)
        
        Debug.Log($"레벨 업! 현재 레벨: {_currentLevel}");
        
        // 레벨이 변경되었음을 모든 구독자(UI 등)에게 알립니다.
        OnLevelChanged?.Invoke();
    }
} 