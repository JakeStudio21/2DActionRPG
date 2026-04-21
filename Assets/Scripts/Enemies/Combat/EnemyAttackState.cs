using UnityEngine;
using UnityEngine.AI;

public class EnemyAttackState : IEnemyState
{
    private readonly IEnemy enemy;
    private float attackTimer = 0f;

    // ─── 공격 라이프사이클 추적 ───────────────────────────────────────
    // skillEverStarted  : IsPerformingSkill이 한 번이라도 true였는가
    //                     → true면 "스킬 경로", false면 "평타 경로"
    // completionTime    : IsPerformingSkill true→false 전환 시각
    //                     → 설정되면 PostAttackDelay 후 재평가
    // wasPerformingSkill: 이전 프레임 IsPerformingSkill 값 (전환 감지용)
    private bool skillEverStarted  = false;
    private float completionTime   = -1f;
    private bool wasPerformingSkill = false;

    // ─── 타이머 설정 ─────────────────────────────────────────────────
    // MeleeGuardTime  : 평타 애니메이션 가드 시간 (스킬 없는 공격 후 재평가 전 대기)
    // PostAttackDelay : 스킬 완료 후 재평가 전 짧은 텀
    // SafetyTimeout   : 무한 대기 방지 비상 탈출
    private const float MeleeGuardTime  = 0.8f;
    private const float PostAttackDelay = 0.2f;
    private const float SafetyTimeout   = 4.0f;

    public EnemyAttackState(IEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        // 거리 초과 시 즉시 Chase
        if (enemy.TargetPlayer != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
            float rangeThreshold = enemy.AttackRange;
            var bossAttack = GetBossAttack(enemy);
            if (bossAttack != null) rangeThreshold = bossAttack.RangedSkillRange;

            if (dist > rangeThreshold)
            {
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }

        // NavMesh 정지
        if (enemy is BaseEnemy baseEnemyNav && baseEnemyNav.IsUsingNavMesh)
            baseEnemyNav.Agent.isStopped = true;

        // 라이프사이클 초기화
        attackTimer        = 0f;
        skillEverStarted   = false;
        completionTime     = -1f;

        // 공격 실행
        enemy.Attack();

        // 엘리트 전용: Attack()이 Skip됐으면 즉시 Chase
        // → DecideAttack()이 Skip을 반환하면 아무 애니메이션도 시작되지 않으므로
        //   하드코딩 타이머를 기다릴 이유가 없음
        if (enemy is BaseEnemy beInit)
        {
            var eliteAttack = beInit.GetComponent<EliteAttackBehaviour>();
            if (eliteAttack != null && eliteAttack.LastAttackSkipped)
            {
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }

        // Attack() 직후 IsPerformingSkill 초기값 기록
        // (Jump 스킬은 Execute() 호출 즉시 isAsyncSkillPending = true가 될 수 있음)
        wasPerformingSkill = (enemy is BaseEnemy beSkill && beSkill.IsPerformingSkill);
        if (wasPerformingSkill) skillEverStarted = true;
    }

    public void Execute()
    {
        attackTimer += Time.deltaTime;

        // 공격 중 방향 유지 Idle 연출
        if (enemy is BaseEnemy baseEnemy && baseEnemy.AnimationController != null)
            baseEnemy.AnimationController.ForceIdleKeepDirection();

        // 비상 탈출 (무한 대기 방지)
        // 스킬 애니메이션이 중간에 막힌 경우 isCasting 등이 true로 남아
        // CanUseSkill() 영구 false → 공격 완전 중단을 방지하기 위해 강제 초기화
        if (attackTimer >= SafetyTimeout)
        {
            if (enemy is BaseEnemy beSafety)
            {
                var skillCtrl = beSafety.GetComponent<EliteSkillController>();
                skillCtrl?.ResetToIdle();
            }
            enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
            return;
        }

        // ── IsPerformingSkill 추적 ──────────────────────────────────
        bool isNowPerforming = (enemy is BaseEnemy beCheck && beCheck.IsPerformingSkill);

        // 스킬이 시작된 적 있음을 기록
        if (isNowPerforming) skillEverStarted = true;

        // true → false 전환 시 완료 시각 기록
        if (wasPerformingSkill && !isNowPerforming && completionTime < 0f)
            completionTime = Time.time;

        wasPerformingSkill = isNowPerforming;

        // 스킬 진행 중: 계속 대기
        if (isNowPerforming) return;

        // ── 재평가 시점 결정 ─────────────────────────────────────────
        bool readyToEvaluate;

        if (completionTime > 0f)
        {
            // 스킬 경로: 완료 후 PostAttackDelay 대기
            readyToEvaluate = Time.time >= completionTime + PostAttackDelay;
        }
        else if (!skillEverStarted)
        {
            // 평타 경로: 애니메이션 가드 시간 대기
            readyToEvaluate = attackTimer >= MeleeGuardTime;
        }
        else
        {
            // 스킬이 시작됐지만 아직 completionTime 미설정 → 다음 프레임에 처리
            readyToEvaluate = false;
        }

        if (!readyToEvaluate) return;

        EvaluateNextState();
    }

    public void Exit()
    {
        if (enemy is BaseEnemy baseEnemyNav && baseEnemyNav.IsUsingNavMesh)
            baseEnemyNav.Agent.isStopped = false;
    }

    // ─── 다음 상태 결정 ───────────────────────────────────────────────
    private void EvaluateNextState()
    {
        if (enemy.TargetPlayer == null)
        {
            enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
            return;
        }

        float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
        var bossAttack = GetBossAttack(enemy);

        // 보스: 히스테리시스 적용
        if (bossAttack != null)
        {
            float attackRange      = enemy.AttackRange;
            float chaseEndRange    = attackRange - 0.5f;
            float chaseStartRange  = attackRange + 1.5f;
            float rangedSkillRange = bossAttack.RangedSkillRange;

            if (dist <= chaseEndRange)
                enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
            else if (dist <= chaseStartRange)
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
            else if (dist <= rangedSkillRange && bossAttack.CanAttack())
                enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
            else
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
            return;
        }

        // 일반 / 엘리트
        if (dist <= enemy.AttackRange * 0.95f)
        {
            var eliteAttack = (enemy as BaseEnemy)?.GetComponent<EliteAttackBehaviour>();
            if (eliteAttack != null && !eliteAttack.CanAttack())
            {
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
            enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
        }
        else if (dist < enemy.AttackRange * 2f)
            enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
        else
            enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
    }

    private BossAttackBehaviour GetBossAttack(IEnemy e)
    {
        return (e as BaseEnemy)?.GetComponent<BossAttackBehaviour>();
    }
}
