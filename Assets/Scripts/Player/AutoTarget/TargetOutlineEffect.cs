using UnityEngine;

/// <summary>
/// ITargetable 구현체에 붙는 락온 아웃라인 비주얼 이펙트.
/// URP 2D Shader Graph 와 MaterialPropertyBlock을 사용합니다.
///
/// MPB 값 규칙 (셰이더와 동일하게 맞출 것):
///   평상시: _ThicknessMult = 0, _OutlineEnabled = 0
///   타겟팅: _ThicknessMult = 0.4, _OutlineEnabled = 1
///
/// 셰이더에서는 최종 두께에 _ThicknessMult를 곱하고, 아웃라인 색/합성에 _OutlineEnabled를 곱하는 식으로 연결합니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TargetOutlineEffect : MonoBehaviour
{
    [Header("셰이더 파라미터 이름 (Shader Graph Property Reference 와 일치)")]
    [SerializeField] private string propEnabled       = "_OutlineEnabled";
    [SerializeField] private string propThicknessMult = "_ThicknessMult";

    private SpriteRenderer _sr;
    private MaterialPropertyBlock _mpb;
    private bool _isActive;

    private void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();

        ApplyDisabled();
    }

    // ──────────────────────────────────────────────
    #region Public API

    /// <summary>락온 활성화: 아웃라인 ON (OutlineEnabled 1, ThicknessMult 0.4)</summary>
    public void Activate()
    {
        if (_isActive) return;
        _isActive = true;

        _sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(propEnabled, 1f);
        _mpb.SetFloat(propThicknessMult, 0.4f);
        _sr.SetPropertyBlock(_mpb);
    }

    /// <summary>락온 해제: 아웃라인 OFF (둘 다 0)</summary>
    public void Deactivate()
    {
        if (!_isActive) return;
        _isActive = false;

        ApplyDisabled();
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Internal

    private void ApplyDisabled()
    {
        _sr.GetPropertyBlock(_mpb);
        _mpb.SetFloat(propEnabled, 0f);
        _mpb.SetFloat(propThicknessMult, 0f);
        _sr.SetPropertyBlock(_mpb);
    }

    #endregion
}
