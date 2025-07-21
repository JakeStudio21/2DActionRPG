using UnityEngine;

/// <summary>
/// 플레이어 선택 데이터를 저장하는 ScriptableObject
/// 선택된 캐릭터 타입, 무기 등의 정보 포함
/// </summary>
[CreateAssetMenu(fileName = "SelectedPlayerData", menuName = "Game/SelectedPlayerData")]
public class SelectedPlayerData : ScriptableObject
{
    [Header("플레이어 기본 정보")]
    public PlayerType selectedPlayerType = PlayerType.None;
    public string weaponName = "";
    
    [Header("게임 진행 데이터")]
    public int currentLevel = 1;
    public int currentGold = 0;
    public int currentExp = 0;
    
    /// <summary>
    /// 데이터 초기화
    /// </summary>
    public void Reset()
    {
        selectedPlayerType = PlayerType.None;
        weaponName = "";
        currentLevel = 1;
        currentGold = 0;
        currentExp = 0;
    }
    
    /// <summary>
    /// 플레이어가 선택되었는지 확인
    /// </summary>
    public bool IsPlayerSelected()
    {
        return selectedPlayerType != PlayerType.None && !string.IsNullOrEmpty(weaponName);
    }
    
    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public override string ToString()
    {
        return $"Player: {selectedPlayerType}, Weapon: {weaponName}, Level: {currentLevel}";
    }
}

/// <summary>
/// 플레이어 캐릭터 타입 열거형
/// </summary>
public enum PlayerType
{
    None = 0,
    Warrior = 1,    // 전사 - 검 사용
    Assasin = 2,    // 어쌔신 - 활 사용  
    Wizard = 3      // 마법사 - 스태프 사용
}

/// <summary>
/// PlayerType 관련 유틸리티 메서드
/// </summary>
public static class PlayerTypeExtensions
{
    /// <summary>
    /// 플레이어 타입에 따른 기본 무기 이름 반환
    /// </summary>
    public static string GetDefaultWeapon(this PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Warrior => "Sword",
            PlayerType.Assasin => "Bow", 
            PlayerType.Wizard => "Staff",
            _ => ""
        };
    }
    
    /// <summary>
    /// 플레이어 타입에 따른 표시 이름 반환
    /// </summary>
    public static string GetDisplayName(this PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Warrior => "전사",
            PlayerType.Assasin => "어쌔신",
            PlayerType.Wizard => "마법사",
            _ => "선택 안함"
        };
    }
    
    /// <summary>
    /// 플레이어 타입이 유효한지 확인
    /// </summary>
    public static bool IsValid(this PlayerType playerType)
    {
        return playerType != PlayerType.None;
    }
}
