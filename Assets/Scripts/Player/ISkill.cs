using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 행동을 추상화하는 인터페이스
/// 모든 플레이어 클래스(Assasin, Warrior, Wizard 등)의 스킬이 이 인터페이스를 구현
/// </summary>
public interface ISkill
{
    /// <summary>
    /// 스킬 이름
    /// </summary>
    string SkillName { get; }
    
    /// <summary>
    /// 스킬 쿨다운 시간(초)
    /// </summary>
    float Cooldown { get; }
    
    /// <summary>
    /// 스킬 사용 가능 여부 확인 (쿨다운, 마나, 조건 등)
    /// </summary>
    /// <returns>사용 가능하면 true</returns>
    bool CanUse();
    
    /// <summary>
    /// 스킬 실행 (애니메이션 트리거, 쿨다운 시작 등)
    /// </summary>
    void Execute();
    
    /// <summary>
    /// Animation Event에서 호출되는 실제 스킬 효과 실행
    /// (발사체 생성, 데미지 판정 등)
    /// </summary>
    void OnAnimationEvent();
    
    /// <summary>
    /// 스킬 쿨다운 남은 시간
    /// </summary>
    /// <returns>남은 쿨다운 시간(초)</returns>
    float GetCooldownRemaining();
}
