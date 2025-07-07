using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bow : MonoBehaviour, IWeapon
{

    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    readonly int FIRM_HASH = Animator.StringToHash("Fire");

    private Animator myAnimator;

    private void Awake()
    {
        myAnimator = GetComponent<Animator>();
    }

    public void Attack()
    {
        myAnimator.SetTrigger(FIRM_HASH);

        GameObject newArrow = GamePoolManager.Instance.SpawnFromPool("Arrow", arrowSpawnPoint.position, arrowSpawnPoint.rotation);
        
        if (newArrow != null && newArrow.TryGetComponent(out Projectile projectile))
        {
            projectile.UpdateProjectileRange(weaponInfo.weaponRange);
        }
    }

    public WeaponInfo GetWeaponInfo() 
    {
        return weaponInfo;
    }

    public void UpdateDirection(Vector2 direction, bool facingLeft)
    {
        // 활은 회전(Quaternion)으로 처리
        if (direction.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        // flipX는 사용하지 않음
    }

}