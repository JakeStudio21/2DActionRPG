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
        Debug.Log("🔵 [Bow] Attack() 시작");
        
        myAnimator.SetTrigger(FIRM_HASH);
        Debug.Log("🟢 [Bow] 애니메이션 트리거 실행");

        GameObject newArrow = GamePoolManager.Instance.SpawnFromPool("Arrow", arrowSpawnPoint.position, arrowSpawnPoint.rotation);
        
        if (newArrow != null)
        {
            Debug.Log("🟢 [Bow] 화살 스폰 성공: " + newArrow.name);
            
            if (newArrow.TryGetComponent(out Projectile projectile))
            {
                projectile.UpdateProjectileRange(weaponInfo.weaponRange);
                Debug.Log("🟢 [Bow] 화살 사거리 업데이트 완료");
            }
            else
            {
                Debug.LogWarning("🟡 [Bow] 화살에 Projectile 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("🔴 [Bow] 화살 스폰 실패! GamePoolManager에서 Arrow를 찾을 수 없습니다.");
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