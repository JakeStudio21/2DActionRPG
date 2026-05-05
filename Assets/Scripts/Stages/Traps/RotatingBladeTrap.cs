using UnityEngine;
using CueSystem;

/// <summary>
/// 회전 칼날 기믹 (고정형 / 이동형 공용).
///
/// 오브젝트 계층 구조:
///   RotatingBladeTrap (이 컴포넌트)
///   └── BladeRoot          - Animator (루프 회전 클립)
///       ├── SpriteRenderer - 칼날 스프라이트
///       └── DamageZone     - TrapDamageZone (BladeRoot 자식 → 칼날과 함께 회전)
///           └── Collider2D
///
/// DamageZone을 BladeRoot의 자식으로 배치하면 애니메이션 회전과 함께
/// 판정 영역도 자동으로 돌기 때문에 별도 타이밍 계산이 필요 없음.
///
/// 동작 흐름:
///   Activate() → 애니메이션 재생 (루프) + DamageZone 상시 ON
///   Deactivate() → 애니메이션 정지 + DamageZone OFF
///
/// 이동형 사용 (레일을 따라 이동하는 톱날):
///   이 오브젝트를 DOTween 등으로 이동시키면
///   DamageZone이 함께 이동하므로 추가 처리 없음.
///   이동 제어는 외부 스크립트(예: MovingObstacleMover)에서 담당.
/// </summary>
public class RotatingBladeTrap : BaseTrap
{
    [Header("애니메이션")]
    [Tooltip("칼날 회전 Animator (BladeRoot 오브젝트). Animator Loop Time ON 필수.")]
    [SerializeField] private Animator bladeAnimator;

    [Tooltip("재생할 애니메이션 클립 이름")]
    [SerializeField] private string animationClipName = "BladeRotate";

    [Tooltip("애니메이션 재생 속도 배율. 높을수록 빠르게 회전.")]
    [SerializeField] private float animationSpeed = 1f;

    [Header("데미지 존")]
    [Tooltip("BladeRoot의 자식으로 배치한 TrapDamageZone.\n" +
             "칼날과 함께 회전하므로 판정이 자동으로 따라감.")]
    [SerializeField] private TrapDamageZone damageZone;

    [Header("Cue 이벤트")]
    [Tooltip("기믹 활성화 시 발행할 이벤트 키 (회전 시작 사운드 등)")]
    [SerializeField] private string activateCueKey = "";

    [Tooltip("기믹 비활성화 시 발행할 이벤트 키 (회전 정지 사운드 등)")]
    [SerializeField] private string deactivateCueKey = "";

    // ── BaseTrap 구현 ─────────────────────────────────────────────────────────

    protected override void OnActivate()
    {
        PlayAnimation();
        damageZone?.Activate();

        if (!string.IsNullOrEmpty(activateCueKey))
            CueEmitter.EmitAt(transform.position, activateCueKey, "Stage");
    }

    protected override void OnDeactivate()
    {
        StopAnimation();
        damageZone?.Deactivate();

        if (!string.IsNullOrEmpty(deactivateCueKey))
            CueEmitter.EmitAt(transform.position, deactivateCueKey, "Stage");
    }

    // ── 애니메이션 제어 ───────────────────────────────────────────────────────

    private void PlayAnimation()
    {
        if (bladeAnimator == null) return;

        bladeAnimator.speed = animationSpeed;
        bladeAnimator.Play(animationClipName, 0, 0f);
    }

    private void StopAnimation()
    {
        if (bladeAnimator == null) return;

        bladeAnimator.speed = 0f;
    }

    // ── 에디터 Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (damageZone == null) return;

        Gizmos.color = new Color(0.8f, 0.1f, 0.1f, 0.8f);
        Gizmos.DrawLine(transform.position, damageZone.transform.position);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"Blade Speed: x{animationSpeed}\n" +
            $"DamageZone: {(damageZone != null ? damageZone.name : "없음")}");
    }
#endif
}
