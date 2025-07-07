using UnityEngine;

public class PlayerAttackInput : MonoBehaviour
{
    public ActiveWeapon activeWeapon;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            activeWeapon.PerformAttack();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            // activeWeapon.PerformSkill(); // 스킬 함수가 있다면 여기에 추가
        }
    }
} 