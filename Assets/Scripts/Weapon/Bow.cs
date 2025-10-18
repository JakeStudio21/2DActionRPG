using UnityEngine;

public class Bow : MonoBehaviour, IWeapon
{
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    private Vector2 lastAttackDirection = Vector2.right;
    private Quaternion lastAttackRotation = Quaternion.identity;

    public void Attack()
    {
        SpawnArrow();
    }
    
    /// <summary>
    /// 화살 생성 로직 (순수 스폰 담당)
    /// </summary>
    public void SpawnArrow()
    {
        GameObject newArrow = GamePoolManager.Instance.SpawnFromPool(
            arrowPrefab.name, 
            arrowSpawnPoint.position, 
            lastAttackRotation
        );
        
        if (newArrow != null && newArrow.TryGetComponent(out Projectile projectile))
        {
            projectile.UpdateProjectileRange(equipmentData.WeaponRange);
        }
    }

    public EquipmentData GetEquipmentData()  // WeaponInfo → EquipmentData
    {
        return equipmentData;
    }

    public void UpdateDirection(Vector2 direction, bool facingLeft)
    {
        // 조이스틱 방향에 따른 무기 회전 (facingLeft 무시)
        if (direction.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // 방향 저장: Animation Event 지연을 대비해 입력 시점의 방향 보존
            lastAttackDirection = direction.normalized;
            lastAttackRotation = Quaternion.Euler(0, 0, angle);
        }
    }

} 