using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem; // 🆕 Cue 시스템 네임스페이스 추가

public class Bow : MonoBehaviour, IWeapon
{
    // 🔍 디버깅용 static 카운터
    private static int attackCallCount = 0;
    private static int spawnArrowCallCount = 0;

    [SerializeField] private EquipmentData equipmentData;  // WeaponInfo → EquipmentData
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
        attackCallCount++;
        Debug.Log($"🔵🔵🔵 [BOW DEBUG] Attack() 호출됨! (호출 횟수: {attackCallCount})");
        
        // ⭐ 디버깅 로그 정리
        Debug.Log("🔵 [Bow] Attack() 실행");
        
        // 🆕 Cue 이벤트 발행 - 공격 시작 시점
        var context = new CueContext
        {
            position = arrowSpawnPoint != null ? arrowSpawnPoint.position : transform.position,
            rotation = arrowSpawnPoint != null ? arrowSpawnPoint.rotation : transform.rotation,
            actorType = ActorType.Player,
            magnitude = 1.0f,
            surfaceType = SurfaceType.Default
        };
        
        bool cueSuccess = CueEmitter.Emit("attack.player.ranged", "Player", context);
        Debug.Log($"🏹 [Bow] 발사 이펙트 Cue 발행: {cueSuccess}");
        
        // ⭐ 애니메이션 트리거 제거 - PlayerAnimationController에서 관리
        // myAnimator.SetTrigger(FIRM_HASH);
        // Debug.Log("🟢 [Bow] 애니메이션 트리거 실행");

        // 순수 발사체 생성 로직만 담당
        SpawnArrow();
    }
    
    /// <summary>
    /// 화살 생성 로직 (순수 스폰 담당)
    /// </summary>
    public void SpawnArrow()
    {
        spawnArrowCallCount++;
        Debug.Log($"🔴🔴🔴 [BOW DEBUG] SpawnArrow() 호출됨! (호출 횟수: {spawnArrowCallCount})");
        
        // 🔧 Cue 발행 제거 - Attack()에서 이미 처리했으므로 중복 방지
        // 순수 화살 스폰 로직만 담당
        
        Debug.Log("🔴🔴🔴 [BOW DEBUG] SpawnArrow() 호출됨!");
        Debug.Log($"🔍 [BOW DEBUG] GamePoolManager.Instance 존재: {GamePoolManager.Instance != null}");
        
        // ⭐ 호출 전 로그
        Debug.Log("🔥 [BOW DEBUG] SpawnFromPool() 호출 전!");
        
        GameObject newArrow = GamePoolManager.Instance.SpawnFromPool(arrowPrefab.name, arrowSpawnPoint.position, arrowSpawnPoint.rotation);
        
        // ⭐ 호출 후 로그
        Debug.Log("🔥 [BOW DEBUG] SpawnFromPool() 호출 후!");
        
        if (newArrow != null)
        {
            Debug.Log($"🟢 [BOW DEBUG] 화살 스폰 성공: {newArrow.name} (InstanceID: {newArrow.GetInstanceID()})");
            
            if (newArrow.TryGetComponent(out Projectile projectile))
            {
                projectile.UpdateProjectileRange(equipmentData.WeaponRange);
                Debug.Log("🟢 [Bow] 화살 사거리 업데이트 완료");
            }
            else
            {
                Debug.LogWarning("🟡 [Bow] 화살에 Projectile 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("🔴 [Bow] 화살 스폰 실패!");
        }
        
        Debug.Log("🔴🔴🔴 [BOW DEBUG] SpawnArrow() 완료!");
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
        }
    }

} 