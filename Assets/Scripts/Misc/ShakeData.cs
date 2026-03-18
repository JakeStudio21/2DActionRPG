using UnityEngine;

/// <summary>
/// 카메라 진동 설정을 담는 순수 데이터 컨테이너.
/// SkillData, ActiveSkillData, CueEntry 등 어디서든 [SerializeField]로 인스펙터에 노출 가능.
/// </summary>
[System.Serializable]
public class ShakeData
{
    [Tooltip("카메라 진동 사용 여부 (false면 모든 필드 무시)")]
    public bool useShake = false;

    [Tooltip("진동 강도 (ScreenShakeManager.globalShakeMultiplier 와 곱해진 값이 최종 적용됨)")]
    [Min(0f)]
    public float intensity = 1.0f;

    [Tooltip("진동 지속 시간 (초) — Cinemachine ImpulseDefinition.TimeEnvelope.SustainTime에 동적 반영")]
    [Min(0f)]
    public float duration = 0.2f;

    [Tooltip("진동 발동까지의 지연 시간 (초, 0이면 즉시 발동)")]
    [Min(0f)]
    public float delay = 0f;
}
