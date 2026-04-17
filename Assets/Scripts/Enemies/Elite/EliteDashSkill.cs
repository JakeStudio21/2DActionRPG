using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem;

/// <summary>
/// 엘리트 몬스터 돌진 스킬 실행기
/// BossDashSkill을 엘리트용으로 경량화 (페이즈 스케일 없음)
/// EliteSkillController의 SkillType.Dash 분기에서 호출됨
/// </summary>
public class EliteDashSkill : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;

    [Header("🏃 돌진 설정")]
    [Tooltip("돌진 기본 속도 (초당 유닛)")]
    [SerializeField] private float dashSpeed = 10f;

    [Tooltip("돌진 중 이동 불가 레이어 (장애물)")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = false;

    // 진행 중인 대시 코루틴 (중복 실행 방지)
    private Coroutine dashCoroutine;

    private void Awake()
    {
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
    }

    /// <summary>
    /// 돌진 실행 (EliteSkillController에서 호출)
    /// </summary>
    /// <param name="skill">스킬 데이터</param>
    /// <param name="direction">Cast 시점에 저장된 방향 벡터 (cachedTargetDirection)</param>
    /// <param name="enemy">기준 BaseEnemy (null이면 this에서 참조)</param>
    public void Execute(SkillData skill, Vector3 direction, BaseEnemy enemy = null)
    {
        if (skill == null)
        {
            Debug.LogError("[EliteDashSkill] SkillData가 null!");
            return;
        }

        if (enemy != null)
            baseEnemy = enemy;

        // 이전 대시가 남아있으면 중단
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }

        dashCoroutine = StartCoroutine(DashRoutine(skill, direction.normalized));
    }

    /// <summary>
    /// 돌진 코루틴 - 이동 + 경로 데미지 판정 + 도착 이펙트
    /// </summary>
    private IEnumerator DashRoutine(SkillData skill, Vector3 direction)
    {
        Vector3 startPosition = transform.position;

        // AoeRadius를 돌진 거리로 사용 (BossDashSkill과 동일)
        float dashDistance = skill.AoeRadius;
        Vector3 targetPosition = startPosition + direction * dashDistance;

        // 이동 시간 계산 (거리 / 속도)
        float dashDuration = dashSpeed > 0f ? dashDistance / dashSpeed : skill.ActionDuration;

        if (enableDebugLogs)
        {
            Debug.Log($"🏃 [EliteDashSkill] 돌진 시작: {startPosition} → {targetPosition}");
            Debug.Log($"   거리: {dashDistance:F1}u, 속도: {dashSpeed:F1}u/s, 예상 시간: {dashDuration:F2}s");
        }

        float elapsed = 0f;

        // 중복 타격 방지용 Set
        HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>();

        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);

            // 선형 이동
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            // 이동 중 경로 데미지 판정
            CheckDashPathDamage(skill, direction, alreadyHit);

            yield return null;
        }

        // 최종 위치 보정
        transform.position = targetPosition;

        // 도착 지점 AOE 이펙트 생성
        SpawnAOEEffect(skill, direction);

        if (enableDebugLogs)
            Debug.Log($"🏁 [EliteDashSkill] 돌진 완료: {transform.position}");

        dashCoroutine = null;
    }

    /// <summary>
    /// 이동 경로 데미지 판정 (매 프레임, 중복 방지)
    /// AoeSize.X = 판정 너비, AoeSize.Y = 판정 높이
    /// </summary>
    private void CheckDashPathDamage(SkillData skill, Vector3 direction, HashSet<Collider2D> alreadyHit)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Vector2 boxSize = skill.AoeSize;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            transform.position,
            boxSize,
            angle,
            LayerMask.GetMask("Player")
        );

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            if (alreadyHit.Contains(hit)) continue;

            alreadyHit.Add(hit);
            ApplyDamageToPlayer(hit.gameObject, skill);

            if (enableDebugLogs)
                Debug.Log($"💥 [EliteDashSkill] 플레이어 타격! (위치: {transform.position})");
        }
    }

    /// <summary>
    /// 플레이어에게 데미지 적용
    /// </summary>
    private void ApplyDamageToPlayer(GameObject player, SkillData skill)
    {
        if (player == null) return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        // 기본 공격력 획득 (MeleeAttack → RangedAttack 순)
        int baseDamage = GetBaseDamage();
        int skillDamage = skill.GetScaledDamage(baseDamage);

        playerHealth.TakeDamage(skillDamage, transform);

        if (enableDebugLogs)
            Debug.Log($"[EliteDashSkill] 데미지 적용: {skillDamage} (기본: {baseDamage} x 배율: {skill.DamageMultiplier})");

        // 타격 연출 — CueSystem 위임
        if (!string.IsNullOrEmpty(skill.HitCueKey))
        {
            string domain = baseEnemy != null ? baseEnemy.CueEmitDomain : "Enemy";
            CueEmitter.Emit(skill.HitCueKey, domain, new CueContext { position = player.transform.position });
        }
    }

    /// <summary>
    /// 기본 공격력 획득 (MeleeAttack 우선, 없으면 RangedAttack)
    /// </summary>
    private int GetBaseDamage()
    {
        if (baseEnemy == null) return 10;

        var melee = baseEnemy.GetComponent<MeleeAttack>();
        if (melee != null && melee.AttackData != null)
            return melee.GetScaledDamage();

        var ranged = baseEnemy.GetComponent<RangedAttack>();
        if (ranged != null && ranged.AttackData != null)
            return ranged.GetScaledDamage();

        return 10;
    }

    /// <summary>
    /// 도착 지점 AOE 이펙트 생성
    /// </summary>
    private void SpawnAOEEffect(SkillData skill, Vector3 direction)
    {
        if (skill.AoeEffect == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);

        GameObject effect = Instantiate(skill.AoeEffect, transform.position, rotation);
        Destroy(effect, 2f);

        if (enableDebugLogs)
            Debug.Log($"🎆 [EliteDashSkill] AOE 이펙트 생성: {skill.AoeEffect.name} at {transform.position}");
    }

    #region 디버그

    private void OnDrawGizmos()
    {
        // 에디터에서 돌진 방향 시각화 (Play 모드 중)
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    #endregion
}
