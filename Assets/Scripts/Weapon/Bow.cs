using UnityEngine;
using CueSystem; // ⭐ Phase 1-2: Cue 시스템 추가

public class Bow : MonoBehaviour, IWeapon
{
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnPoint;

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;

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
        
        if (newProjectile != null && newProjectile.TryGetComponent(out Projectile projectile))
        {
            projectile.UpdateProjectileRange(equipmentData.WeaponRange);
            
            // ⭐ Phase 1-2: 등급 정보 전달
            projectile.Initialize(equipmentData.itemGrade, equipmentData.WeaponType);
            
            if (showDebugLogs)
                Debug.Log($"🏹 [Bow] 발사체 스폰: {projectilePoolKey} (등급: {equipmentData.itemGrade})");
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
        
        if (showDebugLogs)
            Debug.Log($"🏹 [Bow] Muzzle Flash Cue 발행: {eventKey} (등급: {equipmentData?.itemGrade}, 강도: {magnitude}) → {cueSuccess}");
    }
    
    /// <summary>
    /// 무기 등급에 따른 원거리 공격 이벤트 키
    /// </summary>
    private string GetRangedAttackEventKey()
    {
        if (equipmentData == null)
        {
            return "attack.player.ranged"; // 기본값
        }
        
        // 무기 타입 확인 (Bow vs Staff)
        string weaponTypeKey = equipmentData.WeaponType == WeaponType.Bow ? "bow" : "staff";
        
        // 등급별 이벤트 키 매핑
        switch (equipmentData.itemGrade)
        {
            case ItemGrade.S:
                return $"attack.player.ranged.{weaponTypeKey}_s";
            case ItemGrade.A:
                return $"attack.player.ranged.{weaponTypeKey}_a";
            case ItemGrade.B:
                return $"attack.player.ranged.{weaponTypeKey}_b";
            case ItemGrade.C:
                return $"attack.player.ranged.{weaponTypeKey}_c";
            case ItemGrade.D:
                return $"attack.player.ranged.{weaponTypeKey}_d";
            default:
                return "attack.player.ranged"; // fallback
        }
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
            
            if (showDebugLogs)
                Debug.Log($"🔑 [Bow] projectileId: {equipmentData.projectileId} → poolKey: {poolKey}");
            
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