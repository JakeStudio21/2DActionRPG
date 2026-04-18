using System.Collections;
using UnityEngine;
using CueSystem;

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
    
    private BossSkillController skillController;
    
    private void Awake()
    {
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
        
        skillController = GetComponent<BossSkillController>();
    }
    
    /// <summary>
    /// 돌진 스킬 실행 (외부에서 호출 - 하위 호환성: VFX와 Damage 동시 실행)
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향 (Cast 시작 시점 저장됨, null이면 현재 플레이어 방향 사용)</param>
    public void Execute(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        // 기존 동작 유지 (하위 호환성)
        StartCoroutine(DashRoutine(skillEntry, targetDirection, true, true));
    }
    
    /// <summary>
    /// VFX만 실행 (BossSkillController에서 호출)
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향 (Cast 시작 시점 저장됨, 텔레그래프/데미지와 동기화)</param>
    public void ExecuteVFXOnly(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            Debug.LogError("[BossDashSkill] SkillEntry가 null!");
            return;
        }
        
        // ⭐ 저장된 방향 사용 (텔레그래프/데미지와 동일한 방향)
        SpawnAOEEffect(skillEntry, targetDirection);
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
            Debug.LogError("[BossDashSkill] SkillEntry가 null!");
            return;
        }
        
        StartCoroutine(DashRoutine(skillEntry, targetDirection, false, true));
    }
    
    /// <summary>
    /// 돌진 코루틴
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향</param>
    /// <param name="executeVFX">VFX 실행 여부</param>
    /// <param name="executeDamage">Damage 실행 여부</param>
    private IEnumerator DashRoutine(BossSkillEntry skillEntry, Vector3? targetDirection, bool executeVFX, bool executeDamage)
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
        
        
        // 돌진 중 프레임 업데이트
        float elapsedTime = 0f;
        float dashDuration = dashDistance / dashSpeed;
        
        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / dashDuration;
            
            // 선형 이동
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            
            // 돌진 경로에 사각 AOE 데미지 판정 (executeDamage가 true일 때만)
            if (executeDamage)
            {
                CheckDashPathDamage(skillEntry);
            }
            
            yield return null;
        }
        
        // 최종 위치 보정
        transform.position = targetPosition;
        
        
        // AOE 이펙트 생성 (도착 지점, executeVFX가 true일 때만)
        if (executeVFX)
        {
            // ⭐ 저장된 방향 사용 (텔레그래프/데미지와 동일한 방향)
            SpawnAOEEffect(skillEntry, targetDirection);
        }
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
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향 (null이면 현재 플레이어 방향 사용)</param>
    private void SpawnAOEEffect(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        if (skillEntry?.skillData == null || skillEntry.skillData.AoeEffect == null) return;
        
        Vector3 spawnPosition = transform.position;
        // ⭐ 저장된 방향 사용 (텔레그래프/데미지와 동일한 방향)
        Quaternion rotation = CalculateAOERotation(targetDirection);
        
        GameObject effect = Instantiate(skillEntry.skillData.AoeEffect, spawnPosition, rotation);
        Destroy(effect, 2f);
    }
    
    /// <summary>
    /// AOE 회전 계산
    /// </summary>
    /// <param name="targetDirection">타겟 방향 (null이면 현재 플레이어 방향 사용)</param>
    private Quaternion CalculateAOERotation(Vector3? targetDirection = null)
    {
        // ⭐ 저장된 방향 우선, 없으면 현재 플레이어 방향 사용
        Vector3 direction = targetDirection.HasValue ? targetDirection.Value : GetDirectionToPlayer();
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
        
        
        // 타격 연출 — CueSystem 위임
        if (!string.IsNullOrEmpty(skill.HitCueKey))
            CueEmitter.Emit(skill.HitCueKey, baseEnemy != null ? baseEnemy.CueEmitDomain : "Enemy", new CueContext { position = player.transform.position });
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
    
}

