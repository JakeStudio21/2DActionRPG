using System.Collections;
using UnityEngine;
using CueSystem;

/// <summary>
/// 내리치는 거대 망치 기믹.
///
/// 오브젝트 계층 구조:
///   HammerTrap (이 컴포넌트)
///   ├── WallMount        - 벽 톱니 장치 스프라이트 (고정, Animator 없음)
///   ├── HammerRoot       - 애니메이션 적용 오브젝트 (Animator 필수)
///   │   └── SpriteRenderer
///   └── DamageZone       - TrapDamageZone 컴포넌트 (망치 착지 위치에 배치)
///
/// Animator Controller 구성:
///   [HammerIdle] ──StartStrike──→ [HammerCycle] ──자동복귀──→ [HammerIdle]
///    Loop ON        트리거          Loop OFF      Has Exit Time ON
///    (대기 포즈)    Duration:0       (순수 타격)   Exit Time:1, Duration:0
///
/// 타이밍 설계:
///   cycleDuration = 타격 클립 길이 + Idle 대기 시간
///   예) 타격 1.5초 + 대기 2.5초 = cycleDuration 4초, strikeTime 0.9초
///   → Idle 대기 시간은 클립 수정 없이 cycleDuration만 변경하면 조절 가능
///
/// 드리프트 방지:
///   WaitForSeconds 상대 대기 대신 활성화 시점(activationTime) 기준
///   절대 시간으로 각 타격/사이클 시작 시간을 계산.
///   사이클이 반복되어도 오차가 누적되지 않음.
///
/// 여러 망치 시차:
///   startDelay를 각 망치에 다르게 설정 (예: 0s / 1s / 2s / 3s)
///   TrapActivationZone.autoDistributeOffset = false 권장
/// </summary>
public class HammerTrap : BaseTrap
{
    [Header("애니메이션")]
    [Tooltip("망치 애니메이션을 가진 Animator (HammerRoot 오브젝트)")]
    [SerializeField] private Animator hammerAnimator;

    [Tooltip("HammerIdle → HammerCycle 트랜지션에 연결된 Trigger 파라미터 이름")]
    [SerializeField] private string strikeTriggerName = "StartStrike";

    [Tooltip("전체 사이클 길이 (초) = 타격 클립 길이 + Idle 대기 시간.\n" +
             "이 값만 변경하면 타격 간격 조절 가능 (클립 수정 불필요).\n" +
             "예) 타격 1.5초 + 대기 2.5초 = 4")]
    [SerializeField] private float cycleDuration = 4f;

    [Tooltip("사이클 시작 기준으로 망치가 착지하는 시간 (초).\n" +
             "타격 클립 내 착지 순간과 맞춰야 함.\n" +
             "예) 클립 1.5초, 착지 0.9초 → strikeTime = 0.9")]
    [SerializeField] private float strikeTime = 0.9f;

    [Tooltip("착지 판정 유지 시간 (초)")]
    [SerializeField] private float damageDuration = 0.2f;

    [Tooltip("애니메이션 재생 속도 배율. 1=기본, 1.5=빠름, 0.7=느림.")]
    [SerializeField] private float animationSpeed = 1f;

    [Header("데미지 존")]
    [Tooltip("망치 착지 위치에 배치한 TrapDamageZone 컴포넌트")]
    [SerializeField] private TrapDamageZone damageZone;

    [Header("Cue 이벤트")]
    [Tooltip("망치가 착지할 때 발행할 이벤트 키 (카메라 쉐이크, 사운드 등)")]
    [SerializeField] private string impactCueKey = "trap.hammer.impact";

    [Tooltip("기믹 활성화 시 발행할 이벤트 키")]
    [SerializeField] private string activateCueKey = "";

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private Coroutine strikeRoutine;
    private bool isDeactivated = false;

    private static readonly int StrikeTriggerHash = Animator.StringToHash("StartStrike");

    // ── BaseTrap 구현 ─────────────────────────────────────────────────────────

    protected override void OnActivate()
    {
        isDeactivated = false;

        if (!string.IsNullOrEmpty(activateCueKey))
            CueEmitter.EmitAt(transform.position, activateCueKey, "Stage");

        strikeRoutine = StartCoroutine(HammerCycleRoutine());
    }

    protected override void OnDeactivate()
    {
        isDeactivated = true;

        if (strikeRoutine != null)
        {
            StopCoroutine(strikeRoutine);
            strikeRoutine = null;
        }

        damageZone?.Deactivate();
        StopAnimation();
    }

    // ── 망치 동작 루틴 ────────────────────────────────────────────────────────

    private IEnumerator HammerCycleRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (isDeactivated) yield break;

        // 활성화 기준 절대 시간 고정 → 드리프트 방지의 기준점
        float activationTime         = Time.time;
        float adjustedCycleDuration  = cycleDuration / animationSpeed;
        float adjustedStrikeTime     = strikeTime / animationSpeed;
        int   cycleIndex             = 0;

        while (!isDeactivated)
        {
            // ① 사이클 시작: 애니메이션 트리거 (Idle → HammerCycle)
            TriggerAnimation();

            // ② 이번 사이클의 착지 절대 시간 계산
            float strikeAbsTime = activationTime
                                  + cycleIndex * adjustedCycleDuration
                                  + adjustedStrikeTime;

            float waitForStrike = strikeAbsTime - Time.time;
            if (waitForStrike > 0f)
                yield return new WaitForSeconds(waitForStrike);

            if (isDeactivated) yield break;

            // ③ 타격 판정
            OnStrike();

            // ④ 다음 사이클 시작 절대 시간 계산
            cycleIndex++;
            float nextCycleAbsTime = activationTime + cycleIndex * adjustedCycleDuration;

            float waitForNextCycle = nextCycleAbsTime - Time.time;
            if (waitForNextCycle > 0f)
                yield return new WaitForSeconds(waitForNextCycle);
        }
    }

    private void OnStrike()
    {
        if (isDeactivated) return;

        damageZone?.Activate(damageDuration / animationSpeed);

        if (!string.IsNullOrEmpty(impactCueKey))
            CueEmitter.EmitAt(transform.position, impactCueKey, "Stage");
    }

    // ── 애니메이션 제어 ───────────────────────────────────────────────────────

    private void TriggerAnimation()
    {
        if (hammerAnimator == null) return;

        hammerAnimator.speed = animationSpeed;

        if (strikeTriggerName == "StartStrike")
            hammerAnimator.SetTrigger(StrikeTriggerHash);
        else
            hammerAnimator.SetTrigger(strikeTriggerName);
    }

    private void StopAnimation()
    {
        if (hammerAnimator == null) return;

        hammerAnimator.speed = 0f;
    }

    // ── 에디터 Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (damageZone == null) return;

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.8f);
        Gizmos.DrawLine(transform.position, damageZone.transform.position);
        Gizmos.DrawWireSphere(damageZone.transform.position, 0.1f);

        float idleWaitTime = cycleDuration / animationSpeed - strikeTime / animationSpeed;

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.6f,
            $"Cycle: {cycleDuration}s  Strike@{strikeTime}s  Idle대기: {idleWaitTime:F1}s\n" +
            $"Speed: x{animationSpeed}  StartDelay: {startDelay}s\n" +
            $"Trigger: \"{strikeTriggerName}\"");
    }
#endif
}
