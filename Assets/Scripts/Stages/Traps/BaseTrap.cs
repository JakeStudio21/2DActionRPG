using UnityEngine;

/// <summary>
/// 모든 기믹 장치의 공통 베이스 클래스.
///
/// 파생 클래스 구현 방법:
///   1. BaseTrap을 상속받는다.
///   2. OnActivate() 오버라이드: 기믹 동작 시작 (애니메이션, 코루틴 등)
///   3. OnDeactivate() 오버라이드: 기믹 동작 중단 (코루틴 정지, 상태 초기화 등)
///
/// 예시:
///   public class HammerTrap : BaseTrap
///   {
///       protected override void OnActivate() { ... }
///       protected override void OnDeactivate() { ... }
///   }
/// </summary>
public abstract class BaseTrap : MonoBehaviour
{
    [Header("기믹 공통 설정")]
    [Tooltip("기믹 활성화 후 첫 동작까지 대기 시간 (초). 여러 기믹이 동시에 켜질 때 비동기화에 사용.")]
    [SerializeField] protected float startDelay = 0f;

    [Tooltip("한 번 활성화된 이후 다시 활성화되기까지의 쿨타임 (초). 0이면 즉시 재활성화 가능.")]
    [SerializeField] protected float cooldown = 3f;

    // ── 상태 ──────────────────────────────────────────────────────────────────
    private bool isRunning = false;
    private float lastActivateTime = -999f;

    public bool IsRunning => isRunning;

    // ── 공개 API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// TrapActivationZone이 호출. 쿨타임 체크 후 기믹을 활성화한다.
    /// </summary>
    public void Activate()
    {
        if (isRunning) return;
        if (Time.time - lastActivateTime < cooldown) return;

        isRunning = true;
        lastActivateTime = Time.time;
        OnActivate();
    }

    /// <summary>
    /// TrapActivationZone이 호출. 기믹을 중단시킨다.
    /// activationMode = WhileInside일 때 플레이어가 영역을 벗어나면 호출된다.
    /// </summary>
    public void Deactivate()
    {
        if (!isRunning) return;

        isRunning = false;
        OnDeactivate();
    }

    /// <summary>
    /// cycleOffset을 외부(TrapActivationZone)에서 주입할 때 사용.
    /// HammerTrap 등 애니메이션 오프셋이 필요한 기믹에서 override.
    /// </summary>
    public virtual void SetCycleOffset(float normalizedOffset) { }

    // ── 파생 클래스 구현부 ────────────────────────────────────────────────────

    /// <summary>기믹 동작을 시작한다. 파생 클래스에서 반드시 구현.</summary>
    protected abstract void OnActivate();

    /// <summary>기믹 동작을 중단한다. 파생 클래스에서 반드시 구현.</summary>
    protected abstract void OnDeactivate();

    // ── 헬퍼: 내부에서 사이클 완료 통보 ──────────────────────────────────────

    /// <summary>
    /// 파생 클래스에서 1회 사이클이 끝났을 때 호출.
    /// isRunning을 false로 리셋해 다음 쿨타임이 올바르게 계산되도록 한다.
    /// 반복 기믹(WhileInside)은 OnActivate 내에서 루프를 직접 관리하므로 호출 불필요.
    /// </summary>
    protected void NotifyCycleComplete()
    {
        isRunning = false;
    }
}
