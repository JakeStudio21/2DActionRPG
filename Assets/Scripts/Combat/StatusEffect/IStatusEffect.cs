using UnityEngine;

/// <summary>
/// 상태이상 인터페이스
/// ⚙️ Phase 4-C: 모든 상태이상은 이 인터페이스를 구현
/// </summary>
public interface IStatusEffect
{
    /// <summary>
    /// 상태이상 타입
    /// </summary>
    EStatusEffectType EffectType { get; }
    
    /// <summary>
    /// 남은 지속 시간 (초)
    /// </summary>
    float RemainingDuration { get; }
    
    /// <summary>
    /// 효과 값 (데미지량, 감소율 등)
    /// </summary>
    float Value { get; }
    
    /// <summary>
    /// 효과 적용 대상 (플레이어 또는 몬스터)
    /// </summary>
    GameObject Target { get; }
    
    /// <summary>
    /// 상태이상 적용 (시작)
    /// </summary>
    void Apply();
    
    /// <summary>
    /// 상태이상 제거 (종료)
    /// </summary>
    void Remove();
    
    /// <summary>
    /// 매 프레임 업데이트 (지속 피해, 지속시간 감소 등)
    /// </summary>
    /// <param name="deltaTime">Time.deltaTime</param>
    /// <returns>효과가 만료되었으면 true</returns>
    bool Tick(float deltaTime);
    
    /// <summary>
    /// 동일 타입 효과 중첩 처리
    /// </summary>
    /// <param name="newDuration">새로 추가될 지속시간</param>
    /// <param name="newValue">새로 추가될 효과 값</param>
    void RefreshOrStack(float newDuration, float newValue);
    
    /// <summary>
    /// 🛡️ Phase 1: 저항력 적용
    /// 피격자의 저항 수치에 따라 상태이상 지속시간을 감소시킴
    /// </summary>
    /// <param name="resistance">저항 수치 (0.0 ~ 1.0, 1.0 = 100% 저항)</param>
    /// <returns>true: 완전 저항 (효과 무효화), false: 부분 저항 (지속시간 감소 후 효과 적용)</returns>
    bool ApplyResistance(float resistance);
}

