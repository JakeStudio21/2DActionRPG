using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bow : MonoBehaviour, IWeapon
{

    [SerializeField] private WeaponInfo weaponInfo;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    // ⭐ 애니메이션 트리거는 PlayerAnimationController에서 관리하므로 제거
    // readonly int FIRM_HASH = Animator.StringToHash("Fire");
    // private Animator myAnimator;

    private void Awake()
    {
        // ⭐ Animator 참조 제거 - PlayerAnimationController에서 관리
        // myAnimator = GetComponent<Animator>();
    }

    public void Attack()
    {
        // ⭐ 디버깅 로그 정리
        Debug.Log("🔵 [Bow] Attack() 실행");
        
        // ⭐ 애니메이션 트리거 제거 - PlayerAnimationController에서 관리
        // myAnimator.SetTrigger(FIRM_HASH);
        // Debug.Log("🟢 [Bow] 애니메이션 트리거 실행");

        // 순수 발사체 생성 로직만 담당
        SpawnArrow();
    }
    
    /// <summary>
    /// 화살 생성 로직 (Animation Event에서도 호출 가능)
    /// </summary>
    public void SpawnArrow()
    {
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
        // 조이스틱 방향에 따른 무기 회전 (facingLeft 무시)
        if (direction.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

} 