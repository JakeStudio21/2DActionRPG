using UnityEngine;

/// <summary>
/// 조건부 모디파이어 시스템에서 적 대상을 식별하기 위한 인터페이스
/// BaseEnemy와 같은 몬스터 클래스들이 이 인터페이스를 구현해야 함
/// ⚙️ Phase 4: ConditionalModifier 시스템용
/// </summary>
public interface IEnemyTarget
{
    /// <summary>
    /// 몬스터 타입 반환 (Basic, Elite, Boss)
    /// </summary>
    EnemyType GetEnemyType();
    
    /// <summary>
    /// 보스 몬스터인지 확인
    /// </summary>
    bool IsBoss();
    
    /// <summary>
    /// 엘리트 몬스터인지 확인
    /// </summary>
    bool IsElite();
    
    /// <summary>
    /// 현재 체력 비율 반환 (0.0 ~ 1.0)
    /// 예: 30/100 HP = 0.3
    /// </summary>
    float GetCurrentHpPercent();
    
    /// <summary>
    /// GameObject 참조 (위치, 이름 등 디버그용)
    /// </summary>
    GameObject GetGameObject();
}

