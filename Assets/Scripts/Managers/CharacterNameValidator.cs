using System;
using System.Text.RegularExpressions;

/// <summary>
/// 유효성 검사 결과
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; }
    
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true, ErrorMessage = null };
    }
    
    public static ValidationResult Fail(string message)
    {
        return new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}

/// <summary>
/// 캐릭터 이름 유효성 검사기
/// 책임: 이름 규칙 검증, 중복 검사
/// </summary>
public class CharacterNameValidator
{
    // 상수
    private const int MIN_NAME_LENGTH = 2;
    private const int MAX_NAME_LENGTH = 12;
    private const string NAME_PATTERN = @"^[가-힣a-zA-Z0-9]+$";
    
    /// <summary>
    /// 이름 유효성 검사 (전체)
    /// </summary>
    public ValidationResult ValidateName(string name)
    {
        if (IsNameEmpty(name))
        {
            return ValidationResult.Fail("Please enter your character name.");
        }
        
        if (IsNameTooShort(name))
        {
            return ValidationResult.Fail("Character names must be at least 2 characters long.");
        }
        
        if (IsNameTooLong(name))
        {
            return ValidationResult.Fail("Character names must be 12 characters or less.");
        }
        
        if (!IsNameFormatValid(name))
        {
            return ValidationResult.Fail("Character names can only use Korean, English, and numbers");
        }
        
        if (IsNameDuplicated(name))
        {
            return ValidationResult.Fail("The character name is already in use.");
        }
        
        return ValidationResult.Success();
    }
    
    /// <summary>
    /// 이름이 비어있는지 확인
    /// </summary>
    public bool IsNameEmpty(string name)
    {
        return string.IsNullOrEmpty(name);
    }
    
    /// <summary>
    /// 이름이 너무 짧은지 확인
    /// </summary>
    public bool IsNameTooShort(string name)
    {
        return !string.IsNullOrEmpty(name) && name.Length < MIN_NAME_LENGTH;
    }
    
    /// <summary>
    /// 이름이 너무 긴지 확인
    /// </summary>
    public bool IsNameTooLong(string name)
    {
        return !string.IsNullOrEmpty(name) && name.Length > MAX_NAME_LENGTH;
    }
    
    /// <summary>
    /// 이름 형식이 유효한지 확인 (한글, 영문, 숫자만 허용)
    /// </summary>
    public bool IsNameFormatValid(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return Regex.IsMatch(name, NAME_PATTERN);
    }
    
    /// <summary>
    /// 이름이 중복되는지 확인
    /// </summary>
    public bool IsNameDuplicated(string name)
    {
        if (PlayerDataManager.Instance == null) return false;
        
        // 모든 슬롯을 확인하여 동일한 이름이 있는지 검사
        for (int i = 0; i < 3; i++) // maxSlots = 3
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(i);
            if (slotData != null && slotData.isSlotUsed)
            {
                if (string.Equals(slotData.playerName, name, StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Debug.Log($"[CharacterNameValidator] 이름 중복 발견: '{name}' (슬롯 {i})");
                    return true;
                }
            }
        }
        
        return false;
    }
}

