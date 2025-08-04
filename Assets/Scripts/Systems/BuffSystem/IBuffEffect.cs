using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🔥 버프/디버프 효과 인터페이스
/// 모든 일시적 스탯 변화 효과가 이 인터페이스를 구현
/// </summary>
public interface IBuffEffect
{
    /// <summary>
    /// 효과 고유 ID (중복 방지용)
    /// </summary>
    string EffectID { get; }
    
    /// <summary>
    /// 효과 이름 (UI 표시용)
    /// </summary>
    string EffectName { get; }
    
    /// <summary>
    /// 효과 아이콘 (UI 표시용)
    /// </summary>
    Sprite EffectIcon { get; }
    
    /// <summary>
    /// 효과 지속시간 (초 단위, -1이면 영구)
    /// </summary>
    float Duration { get; }
    
    /// <summary>
    /// 남은 시간 (실시간 감소)
    /// </summary>
    float RemainingTime { get; set; }
    
    /// <summary>
    /// 중첩 가능 여부
    /// </summary>
    bool CanStack { get; }
    
    /// <summary>
    /// 현재 중첩 수
    /// </summary>
    int StackCount { get; set; }
    
    /// <summary>
    /// 최대 중첩 수
    /// </summary>
    int MaxStackCount { get; }
    
    /// <summary>
    /// 효과 타입 (버프/디버프)
    /// </summary>
    BuffEffectType EffectType { get; }
    
    /// <summary>
    /// 🆕 효과 초기화 (생성 시 호출)
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 효과 적용 (PlayerRuntimeStats에 스탯 변화 적용)
    /// </summary>
    /// <param name="stats">대상 스탯 시스템</param>
    void Apply(PlayerRuntimeStats stats);
    
    /// <summary>
    /// 효과 제거 (PlayerRuntimeStats에서 스탯 변화 제거)
    /// </summary>
    /// <param name="stats">대상 스탯 시스템</param>
    void Remove(PlayerRuntimeStats stats);
    
    /// <summary>
    /// 효과 갱신 (매 프레임 호출, 지속시간 관리)
    /// </summary>
    /// <param name="deltaTime">델타타임</param>
    /// <returns>효과가 만료되었으면 true</returns>
    bool Update(float deltaTime);
    
    /// <summary>
    /// 동일한 효과와 중첩 시 호출
    /// </summary>
    /// <param name="newEffect">새로 추가되는 동일 효과</param>
    void OnStack(IBuffEffect newEffect);
    
    /// <summary>
    /// 효과 복사본 생성 (인스턴스별 독립성)
    /// </summary>
    IBuffEffect Clone();
}

/// <summary>
/// 버프/디버프 효과 타입
/// </summary>
public enum BuffEffectType
{
    Buff,       // 긍정적 효과
    Debuff,     // 부정적 효과
    Neutral     // 중립적 효과
}
