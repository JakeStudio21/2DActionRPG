using UnityEngine;

// 이 스크립트는 AttackButton UI에 부착되어 OnClick 이벤트를 처리합니다.
public class AttackButtonController : MonoBehaviour
{
    // 버튼이 클릭되었을 때 호출될 공용 메서드입니다.
    public void OnAttackButtonPressed()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null)
        {
            activeWeapon.PerformAttack();  // 변경: Attack() → PerformAttack()
        }
        else
        {
            Debug.LogWarning("공격 버튼이 눌렸지만 ActiveWeapon을 찾을 수 없습니다!");
        }
    }
} 