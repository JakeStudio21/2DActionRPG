using System.Collections;
using UnityEngine;
using Cinemachine;

/// <summary>
/// 스크린 셰이크 중앙 제어 매니저 (Data-Driven + Centralized)
///
/// ■ 핵심 설계 원칙
///   - 모든 진동 요청은 PlayShake(ShakeData) 단일 진입점을 통해 처리
///   - globalShakeMultiplier로 전역 강도 조절 (접근성 / 멀미 방지 옵션)
///   - Throttling: 약한 진동 직후 강한 진동이 오면 Override, 동등 이하는 무시
///   - Duration: ShakeData.duration 값을 Cinemachine ImpulseDefinition에 동적 반영
/// </summary>
public class ScreenShakeManager : Singleton<ScreenShakeManager>
{
    // ───────────────────────────────────────────
    //  Inspector 설정
    // ───────────────────────────────────────────

    [Header("🌐 글로벌 설정")]
    [Tooltip("전역 진동 강도 배율 (0 = 진동 없음, 1 = 원본 강도)\n접근성 옵션 / 멀미 방지용")]
    [Range(0f, 1f)]
    [SerializeField] private float globalShakeMultiplier = 1.0f;

    [Header("⏱️ Throttling 설정")]
    [Tooltip("중복 호출 방어 기본 쿨다운 (초)\n동등·이하 강도의 연속 호출을 이 시간 동안 무시")]
    [SerializeField] private float shakeCooldown = 0.05f;

    [Header("🔧 디버그")]
    [SerializeField] private bool showDebugLogs = false;

    // ───────────────────────────────────────────
    //  Internal State
    // ───────────────────────────────────────────

    private CinemachineImpulseSource _source;
    private float  _cooldownTimer    = 0f;   // 쿨다운 잔여 시간
    private float  _currentIntensity = 0f;   // 현재 진행 중인 진동 강도 (Override 판단용)
    private Coroutine _delayedShakeCoroutine;

    // ───────────────────────────────────────────
    //  Unity Lifecycle
    // ───────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _source = GetComponent<CinemachineImpulseSource>();

        if (_source == null)
            Debug.LogWarning("[ScreenShakeManager] CinemachineImpulseSource 컴포넌트를 찾을 수 없습니다.");
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= Time.unscaledDeltaTime;
            if (_cooldownTimer <= 0f)
            {
                _cooldownTimer    = 0f;
                _currentIntensity = 0f;
            }
        }
    }

    // ───────────────────────────────────────────
    //  Public API
    // ───────────────────────────────────────────

    /// <summary>
    /// 메인 진입점 — ShakeData 기반 진동 요청.
    /// delay > 0 이면 코루틴으로 지연 후 실행.
    /// </summary>
    public void PlayShake(ShakeData data)
    {
        if (data == null || !data.useShake) return;
        if (globalShakeMultiplier <= 0f)    return;
        if (_source == null)                return;

        float finalIntensity = data.intensity * globalShakeMultiplier;

        // Throttling: 쿨다운 중이지만 새 강도가 더 강하면 Override, 아니면 무시
        if (_cooldownTimer > 0f)
        {
            if (finalIntensity <= _currentIntensity)
            {
                if (showDebugLogs)
                    Debug.Log($"[ScreenShakeManager] 쿨다운 중 무시 (현재: {_currentIntensity:F2}, 요청: {finalIntensity:F2})");
                return;
            }

            // 더 강한 진동 → 진행 중인 지연 코루틴 취소 후 즉시 Override
            if (_delayedShakeCoroutine != null)
            {
                StopCoroutine(_delayedShakeCoroutine);
                _delayedShakeCoroutine = null;
            }

            if (showDebugLogs)
                Debug.Log($"[ScreenShakeManager] Override (현재: {_currentIntensity:F2} → 새 강도: {finalIntensity:F2})");
        }

        if (data.delay > 0f)
        {
            _delayedShakeCoroutine = StartCoroutine(PlayShakeDelayed(data, finalIntensity));
        }
        else
        {
            ExecuteShake(finalIntensity, data.duration);
        }
    }

    /// <summary>
    /// 하위 호환용 직접 강도 호출.
    /// BossDashSkill, EliteSkillController, DamageArea 등 기존 코드에서 사용.
    /// </summary>
    public void ShakeScreen(float intensity)
    {
        var data = new ShakeData
        {
            useShake  = true,
            intensity = intensity,
            duration  = 0.2f,
            delay     = 0f
        };
        PlayShake(data);
    }

    /// <summary>
    /// 글로벌 강도 배율 런타임 변경 (설정 화면 등에서 호출).
    /// </summary>
    public void SetGlobalMultiplier(float multiplier)
    {
        globalShakeMultiplier = Mathf.Clamp01(multiplier);
    }

    /// <summary>
    /// 현재 글로벌 강도 배율 반환.
    /// </summary>
    public float GetGlobalMultiplier() => globalShakeMultiplier;

    // ───────────────────────────────────────────
    //  Internal
    // ───────────────────────────────────────────

    /// <summary>
    /// delay 후 진동 실행 코루틴.
    /// </summary>
    private IEnumerator PlayShakeDelayed(ShakeData data, float finalIntensity)
    {
        yield return new WaitForSecondsRealtime(data.delay);
        ExecuteShake(finalIntensity, data.duration);
        _delayedShakeCoroutine = null;
    }

    /// <summary>
    /// 실제 Cinemachine Impulse 발생.
    /// duration 값을 ImpulseDefinition.TimeEnvelope.SustainTime에 동적 반영.
    /// </summary>
    private void ExecuteShake(float finalIntensity, float duration)
    {
        if (_source == null) return;

        // Duration 동적 적용 — ImpulseDefinition TimeEnvelope 수정
        ApplyDuration(duration);

        _source.GenerateImpulse(finalIntensity);

        // 쿨다운 갱신
        _cooldownTimer    = shakeCooldown;
        _currentIntensity = finalIntensity;

        if (showDebugLogs)
            Debug.Log($"[ScreenShakeManager] 진동 발생 — 강도: {finalIntensity:F2}, 지속: {duration:F2}s");
    }

    /// <summary>
    /// ShakeData.duration을 Cinemachine ImpulseDefinition의 SustainTime에 적용.
    /// TimeEnvelope는 struct이므로 반드시 값을 꺼냈다가 다시 할당해야 함.
    /// </summary>
    private void ApplyDuration(float duration)
    {
        if (_source.m_ImpulseDefinition == null) return;

        var envelope = _source.m_ImpulseDefinition.m_TimeEnvelope;
        envelope.m_SustainTime = Mathf.Max(0f, duration);
        _source.m_ImpulseDefinition.m_TimeEnvelope = envelope;
    }
}
