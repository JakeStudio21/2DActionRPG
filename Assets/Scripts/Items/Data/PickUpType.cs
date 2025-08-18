namespace ItemSystem
{
    /// <summary>
    /// 픽업 아이템 타입 enum
    /// </summary>
    public enum PickUpType
    {
        GoldCoin = 0,
        StaminaGlobe = 1, // 호환성 유지를 위해 복원
        HealthGlobe = 2,  // 기존 프리팹 값과 일치
        EquipmentItem = 3 // 장비 아이템
    }
}
