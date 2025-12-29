using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 범용 AOE 스킬 패턴
/// Circle, Triangle(Fan), Rectangle 형태 지원
/// </summary>
public class BossAOESkill : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private BossSkillController skillController;
    
    private void Awake()
    {
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
        
        skillController = GetComponent<BossSkillController>();
    }
    
    /// <summary>
    /// AOE 스킬 실행 (외부에서 호출 - 하위 호환성: VFX와 Damage 동시 실행)
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향 (Cast 시작 시점 저장됨, null이면 현재 플레이어 방향 사용)</param>
    public void Execute(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        // 기존 동작 유지 (하위 호환성)
        ExecuteVFXOnly(skillEntry, targetDirection);
        ExecuteDamageOnly(skillEntry, targetDirection);
    }
    
    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 4] VFX만 실행
    /// Phase 4부터 VFX는 SpawnDamageArea()에서 자동으로 생성됨
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향</param>
    [System.Obsolete("Phase 4: VFX는 SpawnDamageArea()에서 자동으로 생성됨. ExecuteDamageOnly() 단독 사용 권장")]
    public void ExecuteVFXOnly(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            Debug.LogError("[BossAOESkill] SkillEntry가 null!");
            return;
        }
        
        Debug.LogWarning("[BossAOESkill] ExecuteVFXOnly()는 Deprecated! VFX는 자동으로 생성됩니다.");
        // Phase 4: VFX는 SpawnDamageArea()에서 자동 생성되므로 여기서는 아무것도 하지 않음
    }
    
    /// <summary>
    /// Damage만 실행 (BossSkillController에서 호출)
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향</param>
    public void ExecuteDamageOnly(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            Debug.LogError("[BossAOESkill] SkillEntry가 null!");
            return;
        }
        
        SpawnDamageArea(skillEntry, targetDirection);
    }
    
    /// <summary>
    /// DamageArea 생성 및 데미지 판정
    /// </summary>
    private void SpawnDamageArea(BossSkillEntry skillEntry, Vector3? targetDirection)
    {
        if (skillEntry?.skillData == null)
        {
            Debug.LogError("[BossAOESkill] SkillEntry가 null!");
            return;
        }
        
        // Origin 계산 (보스 중심 위치)
        Vector3 origin = GetSkillOrigin(skillEntry);
        
        // Forward 방향 결정
        Vector3 forward = targetDirection.HasValue ? targetDirection.Value : GetDirectionToPlayer();
        forward = forward.normalized;
        
        // DamageArea 프리팹 로드
        GameObject damageAreaPrefab = Resources.Load<GameObject>("Prefabs/VFX/DamageArea");
        if (damageAreaPrefab == null)
        {
            Debug.LogError("[BossAOESkill] DamageArea 프리팹을 찾을 수 없습니다! 경로: Prefabs/VFX/DamageArea");
            return;
        }
        
        // DamageArea 인스턴스 생성
        GameObject damageAreaGO = Instantiate(damageAreaPrefab);
        DamageArea damageArea = damageAreaGO.GetComponent<DamageArea>();
        
        if (damageArea == null)
        {
            Debug.LogError("[BossAOESkill] DamageArea 컴포넌트가 없습니다!");
            Destroy(damageAreaGO);
            return;
        }
        
        // ⭐ Phase 4: DamageArea 초기화 (정책 명시)
        damageArea.Initialize(
            skillData: skillEntry.skillData,
            skillEntry: skillEntry,
            origin: origin,
            forward: forward,
            enemy: baseEnemy,
            policy: AOEDamagePolicy.Once  // ⭐ 명시적 정책 (Once/Window/Tick 선택 가능)
        );
        
        // ⭐ Phase 4: DamageArea의 Left Pivot 보정 위치를 사용하여 VFX 생성
        Vector3 effectPosition = damageArea.GetEffectSpawnPositionForLeftPivot();
        SpawnAOEEffectAtCenter(skillEntry, effectPosition, targetDirection);
        
        // 데미지 판정 실행
        damageArea.PerformDamage();
        
        // ⭐ 1초 후 제거 (Gizmos 확인용)
        Destroy(damageAreaGO, 1.0f);
    }
    
    /// <summary>
    /// 스킬 Origin 위치 계산 (보스 중심 위치)
    /// </summary>
    private Vector3 GetSkillOrigin(BossSkillEntry skillEntry)
    {
        return transform.position;
    }
    
    /// <summary>
    /// ⭐ Phase 4: AOE 이펙트 생성 (DamageArea Center 기준)
    /// </summary>
    private void SpawnAOEEffectAtCenter(BossSkillEntry skillEntry, Vector3 center, Vector3? targetDirection)
    {
        if (skillEntry?.skillData == null || skillEntry.skillData.AoeEffect == null) return;
        
        Quaternion rotation = CalculateAOERotation(targetDirection);
        
        GameObject effect = Instantiate(skillEntry.skillData.AoeEffect, center, rotation);
        Destroy(effect, 2f);
    }
    
    /// <summary>
    /// ⚠️ [DEPRECATED - Phase 4] AOE 이펙트 생성 (Origin 기준)
    /// SpawnAOEEffectAtCenter() 사용 권장
    /// </summary>
    [System.Obsolete("Phase 4: SpawnAOEEffectAtCenter(BossSkillEntry, Vector3, Vector3?) 사용 권장. DamageArea.GetCalculatedCenter()와 동기화 필요")]
    private void SpawnAOEEffect(BossSkillEntry skillEntry, Vector3? targetDirection)
    {
        if (skillEntry?.skillData == null || skillEntry.skillData.AoeEffect == null) return;
        
        Vector3 spawnPosition = transform.position;
        Quaternion rotation = CalculateAOERotation(targetDirection);
        
        GameObject effect = Instantiate(skillEntry.skillData.AoeEffect, spawnPosition, rotation);
        Destroy(effect, 2f);
    }
    
    /// <summary>
    /// AOE 회전 계산
    /// </summary>
    private Quaternion CalculateAOERotation(Vector3? targetDirection)
    {
        // ⭐ 방향 결정: 저장된 방향 우선, 없으면 현재 플레이어 방향
        Vector3 direction = targetDirection.HasValue ? targetDirection.Value : GetDirectionToPlayer();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // ⭐ 이펙트 프리팹이 정상 방향이므로 보정 제거
        return Quaternion.Euler(0, 0, angle);
    }
    
    
    /// <summary>
    /// 플레이어 방향 벡터
    /// </summary>
    private Vector3 GetDirectionToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            return (player.transform.position - transform.position).normalized;
        }
        return Vector3.down;
    }
    
    /// <summary>
    /// 플레이어 각도
    /// </summary>
    private float GetAngleToPlayer()
    {
        Vector3 direction = GetDirectionToPlayer();
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
    
}

