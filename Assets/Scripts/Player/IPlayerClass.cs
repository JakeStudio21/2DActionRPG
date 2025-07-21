using UnityEngine;

/// <summary>
/// 플레이어 클래스별 공통 기능을 정의하는 인터페이스
/// 모든 플레이어 클래스(Assasin, Warrior, Wizard)가 이 인터페이스를 구현
/// </summary>
public interface IPlayerClass
{
    /// <summary>
    /// 클래스 이름 (예: "어쌔신", "워리어", "마법사")
    /// </summary>
    string ClassName { get; }
    
    /// <summary>
    /// 플레이어 타입 (enum)
    /// </summary>
    PlayerType PlayerType { get; }
    
    /// <summary>
    /// 공격력 배율 (1.0 = 기본, 1.2 = 20% 증가)
    /// </summary>
    float AttackPowerMultiplier { get; }
    
    /// <summary>
    /// 이동속도 배율 (1.0 = 기본, 1.3 = 30% 증가)
    /// </summary>
    float MoveSpeedMultiplier { get; }
    
    /// <summary>
    /// 스킬 쿨다운 배율 (1.0 = 기본, 0.8 = 20% 감소)
    /// </summary>
    float SkillCooldownMultiplier { get; }
    
    /// <summary>
    /// 체력 배율 (1.0 = 기본, 1.5 = 50% 증가)
    /// </summary>
    float HealthMultiplier { get; }
    
    /// <summary>
    /// 클래스 초기화 (게임 시작 시 호출)
    /// PlayerController, SkillController 등과 연동하여 클래스별 설정 적용
    /// </summary>
    void InitializeClass();
    
    /// <summary>
    /// 클래스별 능력치를 다른 컴포넌트에 적용
    /// PlayerController 이동속도, PlayerHealth 체력 등에 배율 적용
    /// </summary>
    void ApplyClassStats();
    
    /// <summary>
    /// 레벨업 시 클래스별 보너스 적용
    /// </summary>
    /// <param name="newLevel">새로운 레벨</param>
    void OnLevelUp(int newLevel);
    
    /// <summary>
    /// 클래스별 패시브 스킬 효과 적용
    /// 예: 어쌔신 크리티컬, 워리어 방어력, 마법사 마나 회복
    /// </summary>
    void ApplyPassiveEffects();
    
    /// <summary>
    /// 클래스별 스킬 쿨다운 배율 적용
    /// </summary>
    /// <param name="baseCooldown">기본 쿨다운 시간</param>
    /// <returns>클래스별 배율이 적용된 쿨다운 시간</returns>
    float GetModifiedCooldown(float baseCooldown);
    
    /// <summary>
    /// 클래스별 공격력 배율 적용
    /// </summary>
    /// <param name="baseDamage">기본 공격력</param>
    /// <returns>클래스별 배율이 적용된 공격력</returns>
    float GetModifiedDamage(float baseDamage);
    
    /// <summary>
    /// 클래스 활성화 상태 확인
    /// </summary>
    /// <returns>현재 클래스가 활성화되어 있으면 true</returns>
    bool IsActive();
    
    /// <summary>
    /// 클래스 활성화/비활성화
    /// 캐릭터 선택 시 해당 클래스만 활성화
    /// </summary>
    /// <param name="active">활성화 여부</param>
    void SetActive(bool active);
} 