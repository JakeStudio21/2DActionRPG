using UnityEngine;

/// <summary>
/// 자동 타겟팅 스코어링 엔진.
/// Physics2D.OverlapCircleNonAlloc으로 범위 내 적을 탐색한 뒤
/// TargetingProfile 가중치에 따라 최적 타겟을 결정합니다.
///
/// [스코어 공식]
///   Score = (maxDist - dist) × distWeight
///         + (dot + 1)       × angleWeight   ← dot: aimDir vs toTarget (0~2 범위)
///         + rankBonus
///         + closePrtBonus                    ← dist < closeProtectionRadius
///         + stickinessBonus                  ← 현재 타겟 유지 보너스
/// </summary>
public class AutoTargetResolver : MonoBehaviour
{
    [Header("레이어 설정")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("디버그")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs   = false;

    private readonly Collider2D[] _results = new Collider2D[30];
    private ITargetable _currentTarget;
    public ITargetable CurrentTarget => _currentTarget;

    // ──────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// 가장 높은 점수의 타겟을 찾아 반환합니다.
    /// 결과가 이전과 다르면 락온 UI를 교체합니다.
    /// </summary>
    /// <param name="aimDir">이동 조이스틱 방향 또는 캐릭터 FacingDirection (정규화 권장)</param>
    /// <param name="profile">사용할 TargetingProfile</param>
    /// <returns>최적 ITargetable (없으면 null)</returns>
    public ITargetable FindBestTarget(Vector2 aimDir, TargetingProfile profile)
    {
        if (profile == null)
        {
            if (showDebugLogs)
                Debug.Log("[AT_DBG] FindBestTarget: profile=null → 스킵");
            return _currentTarget;
        }

        // 현재 타겟 유효성 검사
        // ITargetable은 인터페이스라 != null이 C# 참조 비교를 사용하며,
        // 파괴된 Unity 오브젝트도 null로 인식하지 못한다.
        // UnityEngine.Object로 캐스팅해야 Unity의 == 연산자 오버로딩이 적용된다.
        if (_currentTarget != null)
        {
            bool isUnityDestroyed = (_currentTarget as UnityEngine.Object) == null;
            if (isUnityDestroyed || !_currentTarget.IsAlive())
            {
                // 파괴된 오브젝트에서 DeactivateLockOn 호출 방지: Unity null 여부 재확인
                if (!isUnityDestroyed)
                    _currentTarget.DeactivateLockOn();
                _currentTarget = null;
            }
            else
            {
                // 살아있는 오브젝트에서만 Transform 접근 (MissingReferenceException 방지)
                float distToLast = Vector2.Distance(transform.position, _currentTarget.GetTransform().position);
                if (distToLast > profile.targetLostRadius)
                    ClearCurrentTarget();
            }
        }

        Vector2 origin = transform.position;
        int count = Physics2D.OverlapCircleNonAlloc(origin, profile.detectionRadius, _results, enemyLayer);

        if (showDebugLogs)
        {
            Debug.Log($"[AT_DBG] FindBestTarget: origin={origin}, radius={profile.detectionRadius:F1}, " +
                      $"enemyLayerMask={enemyLayer.value}, OverlapCount={count} " +
                      $"(count=0이면 레이어/반경/콜라이더 확인)");
        }

        ITargetable bestTarget = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < count; i++)
        {
            if (_results[i] == null) continue;
            ITargetable candidate = _results[i].GetComponentInParent<ITargetable>()
                                 ?? _results[i].GetComponent<ITargetable>();
            if (candidate == null || !candidate.IsAlive()) continue;

            float score = ComputeScore(candidate, aimDir, profile);

            if (showDebugLogs)
                Debug.Log($"[AutoTargetResolver] {_results[i].name} → score={score:F1}");

            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = candidate;
            }
        }

        // 타겟 변경 시 락온 UI 교체
        if (bestTarget != _currentTarget)
        {
            ClearCurrentTarget();
            _currentTarget = bestTarget;
            _currentTarget?.ActivateLockOn();
        }

        return _currentTarget;
    }

    /// <summary>현재 락온 타겟을 강제 해제합니다.</summary>
    public void ClearCurrentTarget()
    {
        if (_currentTarget != null && (_currentTarget as UnityEngine.Object) != null)
            _currentTarget.DeactivateLockOn();
        _currentTarget = null;
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Scoring

    private float ComputeScore(ITargetable target, Vector2 aimDir, TargetingProfile profile)
    {
        Transform t = target.GetTransform();
        if (t == null) return float.MinValue;

        Vector2 origin    = transform.position;
        Vector2 targetPos = t.position;
        float dist        = Vector2.Distance(origin, targetPos);
        Vector2 toTarget  = (targetPos - origin).normalized;

        // 거리 점수: 탐지 최대 반경 - 현재 거리 → 가까울수록 높음
        float distScore   = (profile.detectionRadius - dist) * profile.distanceWeight;

        // 각도 점수: dot(-1~1) → +1 → (0~2) × angleWeight
        float dot         = aimDir.sqrMagnitude > 0.01f ? Vector2.Dot(aimDir.normalized, toTarget) : 0f;
        float angleScore  = (dot + 1f) * profile.angleWeight;

        // 랭크 보너스
        float rankScore   = profile.GetRankBonus(target.GetRank());

        // 근접 보호망
        float closeScore  = dist < profile.closeProtectionRadius ? profile.closeProtectionBonus : 0f;

        // 타겟 고정 보너스
        float sticky      = (target == _currentTarget) ? profile.stickinessBonus : 0f;

        return distScore + angleScore + rankScore + closeScore + sticky;
    }

    #endregion

    // ──────────────────────────────────────────────
    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // 탐지 반경 시각화 (프로파일 없으면 기본값)
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 6f);

        if (_currentTarget != null && _currentTarget.GetTransform() != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _currentTarget.GetTransform().position);
            Gizmos.DrawWireSphere(_currentTarget.GetTransform().position, 0.4f);
        }
    }
#endif

    #endregion
}
