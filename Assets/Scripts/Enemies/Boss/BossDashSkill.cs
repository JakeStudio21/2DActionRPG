using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 돌진 스킬 패턴
/// 플레이어 방향으로 돌진하며 경로상의 적에게 데미지
/// </summary>
public class BossDashSkill : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;
    
    [Header("🏃 돌진 설정")]
    [Tooltip("기본 돌진 속도")]
    [SerializeField] private float baseDashSpeed = 8f;
    
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
    /// 돌진 스킬 실행 (외부에서 호출)
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향 (Cast 시작 시점 저장됨, null이면 현재 플레이어 방향 사용)</param>
    public void Execute(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            Debug.LogError("[BossDashSkill] SkillEntry가 null!");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🏃 [BossDashSkill] {gameObject.name}: 돌진 스킬 실행!");
            if (targetDirection.HasValue)
            {
                Debug.Log($"   📍 저장된 방향 사용: {targetDirection.Value}");
            }
            else
            {
                Debug.Log($"   📍 현재 플레이어 방향 사용");
            }
        }
        
        StartCoroutine(DashRoutine(skillEntry, targetDirection));
    }
    
    /// <summary>
    /// 돌진 코루틴
    /// </summary>
    private IEnumerator DashRoutine(BossSkillEntry skillEntry, Vector3? targetDirection)
    {
        SkillData skill = skillEntry.skillData;
        float scaleMultiplier = skillEntry.skillScaleMultiplier;
        
        // ⭐ 방향 결정: 저장된 방향 우선, 없으면 현재 플레이어 방향
        Vector3 direction = targetDirection.HasValue ? targetDirection.Value : GetDirectionToPlayer();
        Vector3 startPosition = transform.position;
        
        // 돌진 거리 (스킬 데이터의 AoeRadius 사용)
        float dashDistance = skill.AoeRadius * scaleMultiplier;
        Vector3 targetPosition = startPosition + (direction * dashDistance);
        
        // 돌진 속도 계산 (페이즈별 속도 증가)
        float dashSpeed = baseDashSpeed * scaleMultiplier;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🏃 돌진 시작: {startPosition} → {targetPosition}");
            Debug.Log($"   거리: {dashDistance:F1}f, 속도: {dashSpeed:F1}f/s, 스케일: {scaleMultiplier}x");
        }
        
        // 돌진 중 프레임 업데이트
        float elapsedTime = 0f;
        float dashDuration = dashDistance / dashSpeed;
        
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dashDuration;
            
            // 선형 이동
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // 돌진 경로에 사각 AOE 데미지 판정
            CheckDashPathDamage(skillEntry);
            
            yield return null;
        }
        
        // 최종 위치 보정
        transform.position = targetPosition;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🏁 돌진 완료: {transform.position}");
        }
        
        // AOE 이펙트 생성 (도착 지점)
        SpawnAOEEffect(skillEntry);
    }
    
    /// <summary>
    /// 돌진 경로 데미지 체크 (사각 AOE)
    /// </summary>
    private void CheckDashPathDamage(BossSkillEntry skillEntry)
    {
        SkillData skill = skillEntry.skillData;
        float scaleMultiplier = skillEntry.skillScaleMultiplier;
        
        // 사각 AOE 크기
        Vector2 boxSize = skill.AoeSize * scaleMultiplier;
        
        // 보스 중심에서 사각 충돌 체크
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f, LayerMask.GetMask("Player"));
        
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    ApplyDamageToPlayer(hit.gameObject, skillEntry);
                    break; // 플레이어는 한 번만
                }
            }
        }
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
            Debug.Log($"[BossDashSkill] 플레이어 피격: {skillDamage} 데미지");
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

