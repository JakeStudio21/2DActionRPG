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
    /// AOE 스킬 실행 (외부에서 호출)
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향 (Cast 시작 시점 저장됨, null이면 현재 플레이어 방향 사용)</param>
    public void Execute(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            Debug.LogError("[BossAOESkill] SkillEntry가 null!");
            return;
        }
        
        // AOE 이펙트 생성 (저장된 방향 사용)
        SpawnAOEEffect(skillEntry, targetDirection);
        
        // ⭐ AOE 데미지 판정 (DamageArea 사용)
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
        
        // DamageArea 초기화 (SkillData의 AoeCenterMode 사용)
        damageArea.Initialize(skillEntry.skillData, skillEntry, origin, forward, baseEnemy);
        
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
    /// AOE 이펙트 생성
    /// </summary>
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
        return Quaternion.Euler(0, 0, angle - 90f);
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

