using UnityEngine;

/// <summary>
/// HUD 공격/스킬 버튼 컨트롤러 (모바일 전용)
/// PlayerAttackInput을 통해 자동 타겟팅 포함 정상 공격 플로우 실행
///
/// [설계 원칙]
/// 이 컴포넌트는 스스로 PlayerAttackInput을 찾지 않는다.
/// PlayerSpawner가 플레이어 스폰 완료 후 Bind()를 호출해 참조를 주입한다.
/// </summary>
public class AttackButtonController : MonoBehaviour
{
    [Header("디버그")]

    private PlayerAttackInput playerAttackInput;

    /// <summary>
    /// PlayerSpawner가 플레이어 스폰 완료 후 호출 — 참조 주입
    /// </summary>
    public void Bind(PlayerAttackInput input)
    {
        playerAttackInput = input;

    }

    /// <summary>HUD 기본공격 버튼 — 자동 타겟팅 포함</summary>
    public void OnAttackButtonPressed()
    {

        if (playerAttackInput == null)
        {
            Debug.LogWarning("[AttackButtonController] PlayerAttackInput이 바인딩되지 않았습니다. (PlayerSpawner.Bind 호출 확인)");
            return;
        }

        playerAttackInput.PerformAttack();
    }

    /// <summary>HUD 스킬1 버튼</summary>
    public void OnSkill1ButtonPressed()
    {

        if (playerAttackInput == null)
        {
            Debug.LogWarning("[AttackButtonController] PlayerAttackInput이 바인딩되지 않았습니다.");
            return;
        }

        playerAttackInput.PerformSkill();
    }

    /// <summary>HUD 스킬2 버튼</summary>
    public void OnSkill2ButtonPressed()
    {

        if (playerAttackInput == null)
        {
            Debug.LogWarning("[AttackButtonController] PlayerAttackInput이 바인딩되지 않았습니다.");
            return;
        }

        playerAttackInput.PerformSkill2();
    }
}
