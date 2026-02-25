using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 부옵션 직렬화용 구조체
/// Unity는 Dictionary를 직렬화하지 않으므로 List<SubStatSaveData>로 변환
/// </summary>
[System.Serializable]
public struct SubStatSaveData
{
    [Tooltip("스탯 타입 (예: CRIT_RATE, ASPD)")]
    public EStatType statType;
    
    [Tooltip("스탯 최종 수치 (예: 0.05, 0.10)")]
    public float value;
}

/// <summary>
/// 아이템 인스턴스 메타데이터
/// 강화, 커스텀 이름, 획득 시간 등 인스턴스별 정보 저장
/// </summary>
[System.Serializable]
public class ItemInstanceData
{
    [Header("🆔 고유 식별자")]
    public ItemInstanceID instanceId;
    
    [Header("📋 템플릿 정보")]
    [Tooltip("EquipmentData의 이름 (예: Sword_D_Equipment)")]
    public string templateName;
    
    [Header("⚡ 강화 정보")]
    [Tooltip("강화 레벨 (0~15)")]
    public int enhancementLevel = 0;
    
    [Tooltip("강화 시도 횟수")]
    public int enhancementAttempts = 0;
    
    [Header("🎨 커스텀 정보")]
    [Tooltip("사용자 지정 이름 (비어있으면 기본 이름 사용)")]
    public string customName = "";
    
    [Header("📅 메타데이터")]
    [Tooltip("획득 시간 (yyyy-MM-dd HH:mm:ss)")]
    public string acquiredTime = "";
    
    [Tooltip("합성으로 제작된 경우 레시피 ID (-1: 드롭/상점)")]
    public int craftedFromRecipeId = -1;
    
    [Header("🎲 동적 스탯 (Random Stats)")]
    [Tooltip("등급 배율이 적용된 최종 주옵션 수치")]
    public float finalMainStatValue = 0f;
    
    [Tooltip("랜덤 부옵션 리스트 (직렬화용)")]
    public List<SubStatSaveData> randomSubStats = new List<SubStatSaveData>();
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        string name = !string.IsNullOrEmpty(customName) ? customName : templateName;
        return $"{name} +{enhancementLevel} (ID: {instanceId.Value?.Substring(0, 8)}...)";
    }
}

