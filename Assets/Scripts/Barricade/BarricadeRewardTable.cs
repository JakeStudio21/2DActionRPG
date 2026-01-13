using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바리케이드 전용 보상 테이블 ScriptableObject
/// 특수 보상 시스템에서 사용
/// </summary>
[CreateAssetMenu(fileName = "BarricadeRewardTable", menuName = "Game/Barricade/Reward Table")]
public class BarricadeRewardTable : ScriptableObject
{
    [Header("==== 확정 보상 ====")]
    [Tooltip("확정으로 드롭되는 골드량")]
    public int guaranteedGold = 50;
    
    [Tooltip("확정으로 드롭되는 장비 (선택)")]
    public EquipmentData guaranteedEquipment;
    
    [Header("==== 랜덤 보상 풀 ====")]
    [Tooltip("랜덤 보상 엔트리들")]
    public RewardEntry[] randomRewards;
    
    [Tooltip("랜덤 보상 추첨 횟수")]
    [Range(0, 10)]
    public int randomRewardCount = 2;
    
    /// <summary>
    /// 보상 엔트리 정의
    /// </summary>
    [System.Serializable]
    public class RewardEntry
    {
        [Header("■ 기본 정보")]
        public string rewardName = "골드";
        public RewardType rewardType = RewardType.Gold;
        
        [Header("■ 드롭 확률")]
        [Tooltip("드롭 확률 (0~100%)")]
        [Range(0f, 100f)]
        public float dropChance = 50f;
        
        [Header("■ 수량 설정")]
        [Tooltip("최소 개수")]
        public int minAmount = 1;
        [Tooltip("최대 개수")]
        public int maxAmount = 3;
        
        [Header("■ 아이템 참조")]
        [Tooltip("장비 타입인 경우 EquipmentData")]
        public EquipmentData equipmentData;
        
        [Tooltip("아이템 타입인 경우 ItemID (예: ITEM_HEALTH_POTION)")]
        public string itemID;
    }
    
    /// <summary>
    /// 보상 타입
    /// </summary>
    public enum RewardType
    {
        Gold,           // 골드
        Equipment,      // 장비
        HealthPotion,   // 체력 포션
        SpecialKey      // 특수 열쇠 (다른 바리케이드 해제용)
    }
}

