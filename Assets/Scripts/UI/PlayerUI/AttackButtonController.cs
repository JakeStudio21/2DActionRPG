using UnityEngine;

// 이 스크립트는 AttackButton UI에 부착되어 OnClick 이벤트를 처리합니다.
public class AttackButtonController : MonoBehaviour
{
    // 버튼이 클릭되었을 때 호출될 공용 메서드입니다.
    public void OnAttackButtonPressed()
    {
        // ActiveWeapon의 싱글톤 인스턴스가 존재하는지 확인합니다.
        // (플레이어가 스폰되면 인스턴스가 설정됩니다)
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null)
        {
            // 존재한다면, 그 인스턴스의 공격 메서드를 호출합니다.
            // 이렇게 하면 어떤 캐릭터(Warrior, Assassin)가 스폰되든 상관없이
            // 현재 활성화된 무기로 공격할 수 있습니다.
            activeWeapon.PerformAttack();
        }
        else
        {
            // 만약의 경우를 대비한 경고 메시지입니다.
            Debug.LogWarning("공격 버튼이 눌렸지만 ActiveWeapon.Instance가 없습니다!");
        }
    }
} 