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
    public void Execute(BossSkillEntry skillEntry)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            Debug.LogError("[BossAOESkill] SkillEntry가 null!");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"💥 [BossAOESkill] {gameObject.name}: {skillEntry.skillData.SkillName} 실행!");
        }
        
        // AOE 이펙트 생성
        SpawnAOEEffect(skillEntry);
        
        // AOE 데미지 판정
        PerformAOEDamage(skillEntry);
    }
    
    /// <summary>
    /// AOE 데미지 판정
    /// </summary>
    private void PerformAOEDamage(BossSkillEntry skillEntry)
    {
        if (skillEntry?.skillData == null) return;
        
        SkillData skill = skillEntry.skillData;
        float scaleMultiplier = skillEntry.skillScaleMultiplier;
        
        Vector3 center = transform.position;
        Vector3 direction = GetDirectionToPlayer();
        
        Collider2D[] hits = null;
        
        switch (skill.AoeShape)
        {
            case AOEShapeType.Circle:
                float radius = skill.AoeRadius * scaleMultiplier;
                hits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Player"));
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[BossAOESkill] Circle AOE: 반경 {radius:F1}f");
                }
                break;
                
            case AOEShapeType.Triangle: // Fan
                float fanRadius = skill.AoeRadius * scaleMultiplier;
                hits = GetFanHits(center, direction, fanRadius, skill.AoeAngle);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[BossAOESkill] Fan AOE: 반경 {fanRadius:F1}f, 각도 {skill.AoeAngle}도");
                }
                break;
                
            case AOEShapeType.Rectangle:
                Vector2 size = skill.AoeSize * scaleMultiplier;
                hits = Physics2D.OverlapBoxAll(center, size, GetAngleToPlayer(), LayerMask.GetMask("Player"));
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[BossAOESkill] Rectangle AOE: 크기 ({size.x:F1}, {size.y:F1})");
                }
                break;
        }
        
        if (hits == null || hits.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BossAOESkill] AOE 데미지 대상 없음");
            }
            return;
        }
        
        // 플레이어에게 데미지 적용
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                ApplyDamageToPlayer(hit.gameObject, skillEntry);
                break;
            }
        }
    }
    
    /// <summary>
    /// 부채꼴 범위 충돌 감지
    /// </summary>
    private Collider2D[] GetFanHits(Vector3 center, Vector3 direction, float radius, float angle)
    {
        Collider2D[] allHits = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Player"));
        List<Collider2D> fanHits = new List<Collider2D>();
        
        foreach (var hit in allHits)
        {
            Vector3 toTarget = (hit.transform.position - center).normalized;
            float dotProduct = Vector3.Dot(direction, toTarget);
            float angleToTarget = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
            
            if (angleToTarget <= angle / 2f)
            {
                fanHits.Add(hit);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BossAOESkill] Fan 감지: 전체 {allHits.Length}개 중 범위 내 {fanHits.Count}개");
        }
        
        return fanHits.ToArray();
    }
    
    /// <summary>
    /// AOE 이펙트 생성
    /// </summary>
    private void SpawnAOEEffect(BossSkillEntry skillEntry)
    {
        if (skillEntry?.skillData == null || skillEntry.skillData.AoeEffect == null) return;
        
        Vector3 spawnPosition = transform.position;
        Quaternion rotation = CalculateAOERotation();
        
        GameObject effect = Instantiate(skillEntry.skillData.AoeEffect, spawnPosition, rotation);
        Destroy(effect, 2f);
    }
    
    /// <summary>
    /// AOE 회전 계산
    /// </summary>
    private Quaternion CalculateAOERotation()
    {
        Vector3 direction = GetDirectionToPlayer();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0, 0, angle - 90f);
    }
    
    /// <summary>
    /// 플레이어에게 데미지 적용
    /// </summary>
    private void ApplyDamageToPlayer(GameObject player, BossSkillEntry skillEntry)
    {
        if (player == null) return;
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;
        
        // 기본 데미지 획득
        int baseDamage = 10;
        
        if (baseEnemy != null)
        {
            var meleeAttack = baseEnemy.GetComponent<MeleeAttack>();
            if (meleeAttack != null && meleeAttack.AttackData != null)
            {
                baseDamage = meleeAttack.GetScaledDamage();
            }
        }
        
        // 스킬 데미지 계산
        SkillData skill = skillEntry.skillData;
        float totalMultiplier = skill.DamageMultiplier * skillEntry.skillScaleMultiplier;
        int skillDamage = Mathf.RoundToInt(baseDamage * totalMultiplier);
        
        // 데미지 적용
        playerHealth.TakeDamage(skillDamage, transform);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BossAOESkill] 플레이어 피격: {skillDamage} 데미지");
            Debug.Log($"   기본: {baseDamage}, 스킬 배율: {skill.DamageMultiplier}x, 페이즈 스케일: {skillEntry.skillScaleMultiplier}x");
        }
        
        // Hit 이펙트
        if (skill.HitEffect != null)
        {
            GameObject hitEffect = Instantiate(skill.HitEffect, player.transform.position, Quaternion.identity);
            Destroy(hitEffect, 2f);
        }
        
        // Screen Shake
        TriggerScreenShake(skill.ShakeIntensity);
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
    
    /// <summary>
    /// Screen Shake 트리거
    /// </summary>
    private void TriggerScreenShake(float intensity)
    {
        if (ScreenShakeManager.Instance != null)
        {
            ScreenShakeManager.Instance.ShakeScreen(intensity);
        }
    }
}

