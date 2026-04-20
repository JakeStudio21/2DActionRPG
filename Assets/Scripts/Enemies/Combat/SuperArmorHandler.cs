using UnityEngine;

/// <summary>
/// 슈퍼아머 상태 관리 컴포넌트
/// 활성화 중에는 스태거(넉백 + 스킬 캔슬)를 완전 차단한다.
///
/// 사용법:
///   - Boss 프리팹에 컴포넌트 추가
///   - BossSkillController.StartSkill() → Activate()
///   - BossSkillController.OnSkillComplete() / ForceCancelSkill() → Deactivate()
/// </summary>
public class SuperArmorHandler : MonoBehaviour
{
    [Header("🛡️ 슈퍼아머 상태")]
    [SerializeField] private bool isActive = false;

    /// <summary>현재 슈퍼아머가 활성화되어 있는지</summary>
    public bool IsActive => isActive;

    /// <summary>스킬 캐스팅/액션 시작 시 호출 → 슈퍼아머 ON</summary>
    public void Activate()
    {
        isActive = true;
    }

    /// <summary>스킬 완료/캔슬 시 호출 → 슈퍼아머 OFF</summary>
    public void Deactivate()
    {
        isActive = false;
    }
}
