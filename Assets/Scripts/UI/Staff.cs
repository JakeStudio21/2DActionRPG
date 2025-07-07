using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Staff : MonoBehaviour, IWeapon
{
    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject MagicLaser;
    [SerializeField] private Transform magicLaserSpawnPoint;

    private Animator myAnimator;

    readonly int ATTACK_HASH = Animator.StringToHash("Attack");

    private void Awake() {
        myAnimator = GetComponent<Animator>();
    }

    public void Attack()  {

        myAnimator.SetTrigger(ATTACK_HASH);
    }

    public void SpawnStaffProjectileAnimEvent() {
        GameObject newLaser = Instantiate(MagicLaser, magicLaserSpawnPoint.position, Quaternion.identity);
        newLaser.GetComponent<MagicLaser>().UpdateLaserRange(weaponInfo.weaponRange);
    }

    public WeaponInfo GetWeaponInfo()
    {
        return weaponInfo;
    }

    public void UpdateDirection(Vector2 direction, bool facingLeft)
    {
        // 스태프는 방향에 따라 특별한 처리가 없다면 빈 메서드로 둡니다.
        // 필요시 여기에 회전/FlipX 등 추가
    }

    // public void UpdateDirection(Vector2 direction, bool facingLeft) { } // [백업: 기존에는 없었음]
}