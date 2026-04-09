using UnityEngine;

/// <summary>
/// Tutorial 전용 경량 플레이어 데이터
/// - Tutorial 씬에서만 사용
/// - 최소한의 정보만 포함
/// - 저장/로드 기능 없음 (Tutorial은 저장하지 않음)
/// - 일반 클래스 (ScriptableObject 아님)
/// </summary>
public class TutorialPlayerData
{
    // 기본 정보
    public string playerName = "Tutorial_Player";
    public PlayerType playerClass = PlayerType.Assasin;
    public int level = 1;
    
    // 무기 (단순 참조)
    public EquipmentData equippedWeapon;
    
    // 튜토리얼용 스킬 (인게임 첫 지급 스킬과 동일하게 설정)
    public ActiveSkillData tutorialSkill1;
    public ActiveSkillData tutorialSkill2;
    
    // 생성자
    public TutorialPlayerData(PlayerType classType)
    {
        playerClass = classType;
        playerName = "Tutorial_Player";
        level = 1;
        equippedWeapon = null;
        tutorialSkill1 = null;
        tutorialSkill2 = null;
    }
    
    /// <summary>
    /// 기본 무기 할당
    /// </summary>
    public void SetDefaultWeapon(EquipmentData weapon)
    {
        equippedWeapon = weapon;
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public override string ToString()
    {
        return $"[TutorialPlayerData] {playerName}({playerClass}) Lv.{level} " +
               $"Weapon:{(equippedWeapon != null ? equippedWeapon.equipmentName : "None")} " +
               $"Skill1:{(tutorialSkill1 != null ? tutorialSkill1.skillName : "None")} " +
               $"Skill2:{(tutorialSkill2 != null ? tutorialSkill2.skillName : "None")}";
    }
}

