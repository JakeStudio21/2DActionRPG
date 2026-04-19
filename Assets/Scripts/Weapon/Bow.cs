using UnityEngine;
using CueSystem; // ⭐ Phase 1-2: Cue 시스템 추가

public class Bow : MonoBehaviour, IWeapon
{
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    private Vector2 lastAttackDirection = Vector2.right;
    private Quaternion lastAttackRotation = Quaternion.identity;

    public void Attack()
    {
        // ⭐ Phase 1-2: 1단계 - 발사 순간 이펙트 (Muzzle Flash)
        EmitRangedAttackCue();
        
        // ⭐ Phase 1-2: 2단계 - 발사체 스폰
        SpawnProjectile();
    }
    
    /// <summary>
    /// ⭐ Phase 1-2: 발사체 생성 (등급별 프리팹)
    /// </summary>
    private void SpawnProjectile()
    {
        if (equipmentData == null)
        {
            Debug.LogWarning("🟡 [Bow] EquipmentData가 null입니다!");
            return;
        }
        
        // ⭐ projectileId로 발사체 프리팹 결정
        string projectilePoolKey = GetProjectilePoolKey();
        
        GameObject newProjectile = GamePoolManager.Instance.SpawnFromPool(
            projectilePoolKey, 
            arrowSpawnPoint.position, 
            lastAttackRotation
        );
        
        // 🧱 모든 투사체를 Projectile Layer로 설정
        if (newProjectile != null)
        {
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            newProjectile.layer = projectileLayer;
        }
        
        if (newProjectile != null && newProjectile.TryGetComponent(out Projectile projectile))
        {
            projectile.UpdateProjectileRange(equipmentData.WeaponRange);
            
            // ⭐ Phase 1-2: 등급 정보 전달
            projectile.Initialize(equipmentData.itemGrade, equipmentData.WeaponType);
            
        }
        else
        {
            Debug.LogWarning($"⚠️ [Bow] 발사체 스폰 실패: {projectilePoolKey}");
        }
    }
    
    /// <summary>
    /// 🔄 레거시 호환성: SpawnArrow → SpawnProjectile
    /// </summary>
    public void SpawnArrow()
    {
        SpawnProjectile();
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
    
    #region ⭐ Phase 1-2: 등급별 이펙트 시스템
    
    /// <summary>
    /// 🎨 발사 순간 이펙트 발행 (등급별)
    /// </summary>
    private void EmitRangedAttackCue()
    {
        if (arrowSpawnPoint == null) return;
        
        // 등급별 이벤트 키 생성
        string eventKey = GetRangedAttackEventKey();
        float magnitude = GetMagnitudeByGrade();
        
        var context = new CueContext
        {
            position = arrowSpawnPoint.position,
            rotation = lastAttackRotation,
            actorType = ActorType.Player,
            magnitude = magnitude,
            surfaceType = SurfaceType.Default,
            facingDir = lastAttackDirection
        };
        
        bool cueSuccess = CueEmitter.Emit(eventKey, "Player", context);
        
    }
    
    /// <summary>
    /// 원거리 공격 이벤트 키 반환 — player_base 단일 프로파일 관리 방식
    /// 등급 차이는 magnitude로 전달
    /// </summary>
    private string GetRangedAttackEventKey()
    {
        return "attack.player.ranged";
    }
    
    /// <summary>
    /// 발사체 풀 키 결정 (projectileId 기반)
    /// </summary>
    private string GetProjectilePoolKey()
    {
        // EquipmentData.projectileId 사용
        if (!string.IsNullOrEmpty(equipmentData.projectileId))
        {
            // "ITEM_ARROW_1" → "Arrow_1" 변환
            string poolKey = equipmentData.projectileId
                .Replace("ITEM_", "")
                .Replace("ARROW_", "Arrow_")
                .Replace("MAGICBULLET_", "MagicBullet_");
            
            
            return poolKey;
        }
        
        // Fallback: 무기 타입에 따른 기본 발사체
        if (equipmentData.WeaponType == WeaponType.Bow)
        {
            return arrowPrefab != null ? arrowPrefab.name : "Arrow";
        }
        else if (equipmentData.WeaponType == WeaponType.Magic)
        {
            return "Bullet";
        }
        
        return arrowPrefab != null ? arrowPrefab.name : "Arrow";
    }
    
    /// <summary>
    /// 등급에 따른 이펙트 강도
    /// </summary>
    private float GetMagnitudeByGrade()
    {
        if (equipmentData == null) return 1.0f;
        
        switch (equipmentData.itemGrade)
        {
            case ItemGrade.S: return 2.0f;
            case ItemGrade.A: return 1.5f;
            case ItemGrade.B: return 1.2f;
            case ItemGrade.C: return 1.0f;
            case ItemGrade.D: return 0.8f;
            default: return 1.0f;
        }
    }
    
    #endregion

} 