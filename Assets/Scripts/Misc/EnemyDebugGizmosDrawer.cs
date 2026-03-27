using UnityEngine;

/// <summary>
/// 몬스터 AI 범위 + 물리 콜라이더 디버그 기즈모 드로어 (Approach C - SRP)
///
/// 기존 ColliderGizmosDrawer(2000줄+, 20개 몬스터 하드코딩) 대체
/// → 모든 몬스터 공통 범용 처리, BaseEnemy + FSMController.CurrentState 기반
///
/// 프리팹에 추가 방법:
///   각 몬스터 Prefab Variant에 이 컴포넌트를 추가하면 됩니다.
///   (Enemy_MeleeBase / Enemy_RangedBase 베이스 프리팹에 넣으면 모든 Variant 자동 적용)
/// </summary>
[ExecuteAlways]
public class EnemyDebugGizmosDrawer : MonoBehaviour
{
    [Header("표시 토글")]
    [SerializeField] private bool showColliders    = true;
    [SerializeField] private bool showEnemyRanges  = true;
    [SerializeField] private bool showPlayerLine   = true;
    [SerializeField] private bool showDebugLabel   = true;

    [Header("콜라이더 색상")]
    [SerializeField] private Color colliderColor = Color.green;

    [Header("AI 범위 색상")]
    [SerializeField] private Color idlePatrolColor = Color.green;
    [SerializeField] private Color chaseColor      = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color attackColor     = Color.red;
    [SerializeField] private Color spawnRangeColor = Color.magenta;
    [SerializeField] private Color fallbackColor   = Color.yellow;

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (showColliders)   DrawColliderGizmos();
        if (showEnemyRanges) DrawEnemyRanges();
    }

    // ═══════════════════════════════════════════════════════════════
    //  AI 범위 시각화 (핵심 — 범용 데이터 기반)
    // ═══════════════════════════════════════════════════════════════

    private void DrawEnemyRanges()
    {
        var enemy = GetComponent<BaseEnemy>();
        if (enemy == null || enemy.EnemyData == null) return;

        Gizmos.matrix = Matrix4x4.identity;
        Vector3 pos = transform.position;

        // 스폰 지점 + 순찰 반경 (항상 표시)
        DrawSpawnInfo(enemy, pos);

        // 현재 AI 상태에 따른 색상/범위
        var currentState = enemy.FSMController?.CurrentState;

        if (currentState == null)
        {
            DrawFallback(enemy, pos);
            return;
        }

        DrawStateRange(enemy, pos, currentState);

        // 플레이어 연결선
        if (showPlayerLine && Application.isPlaying && enemy.TargetPlayer != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos, enemy.TargetPlayer.transform.position);
        }
    }

    private void DrawSpawnInfo(BaseEnemy enemy, Vector3 pos)
    {
        Vector2 spawnPoint = enemy.SpawnPoint;

        // 순찰 반경 (스폰 기준)
        Gizmos.color = spawnRangeColor;
        Gizmos.DrawWireSphere(spawnPoint, enemy.PatrolRadius);

        // 스폰 마커
        Gizmos.color = Color.white;
        Gizmos.DrawCube(spawnPoint, Vector3.one * 0.3f);
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.3f);

        // 현재 위치 → 스폰 연결선
        if (Vector2.Distance(pos, spawnPoint) > 0.1f)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(pos, spawnPoint);
        }
    }

    private void DrawStateRange(BaseEnemy enemy, Vector3 pos, IEnemyState state)
    {
        if (state is EnemyIdleState || state is EnemyPatrolState)
        {
            Gizmos.color = idlePatrolColor;
            Gizmos.DrawWireSphere(pos, enemy.DetectionRange);
            DrawLabel(pos, $"[Idle/Patrol]\nDetect: {enemy.DetectionRange:F1}");
        }
        else if (state is EnemyChaseState)
        {
            Gizmos.color = chaseColor;
            Gizmos.DrawWireSphere(pos, enemy.ChaseRange);
            DrawLabel(pos, $"[Chase]\nChase: {enemy.ChaseRange:F1}");
        }
        else if (state is EnemyAttackState)
        {
            Gizmos.color = attackColor;
            Gizmos.DrawWireSphere(pos, enemy.AttackRange);
            DrawLabel(pos, $"[Attack]\nAttack: {enemy.AttackRange:F1}");
        }
        else
        {
            // Hit / Die / ReturnToHome 등 — 감지 범위를 회색으로 표시
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(pos, enemy.DetectionRange);
            DrawLabel(pos, $"[{state.GetType().Name}]");
        }
    }

    private void DrawFallback(BaseEnemy enemy, Vector3 pos)
    {
        // 에디터 / 플레이 전 — EnemyData 기반 기본 표시
        Gizmos.color = fallbackColor;
        Gizmos.DrawWireSphere(pos, enemy.DetectionRange);

        Gizmos.color = chaseColor;
        Gizmos.DrawWireSphere(pos, enemy.ChaseRange);

        Gizmos.color = attackColor;
        Gizmos.DrawWireSphere(pos, enemy.AttackRange);

        DrawLabel(pos,
            $"[{(enemy.EnemyData != null ? enemy.EnemyData.name : gameObject.name)}]\n" +
            $"Detect:{enemy.DetectionRange:F1}  Chase:{enemy.ChaseRange:F1}  Attack:{enemy.AttackRange:F1}");
    }

    private void DrawLabel(Vector3 pos, string text)
    {
        if (!showDebugLabel) return;
        UnityEditor.Handles.Label(pos + Vector3.up * 1.5f, text);
    }

    // ═══════════════════════════════════════════════════════════════
    //  물리 콜라이더 시각화 (ColliderGizmosDrawer에서 그대로 이전)
    // ═══════════════════════════════════════════════════════════════

    private void DrawColliderGizmos()
    {
        Gizmos.color = colliderColor;

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCircle(circle.offset, circle.radius);
            Gizmos.matrix = Matrix4x4.identity;
        }

        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCapsule2D(capsule);
            Gizmos.matrix = Matrix4x4.identity;
        }

        var composite = GetComponent<CompositeCollider2D>();
        if (composite != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            DrawCompositeCollider2D(composite);
        }

        var polygon = GetComponent<PolygonCollider2D>();
        if (polygon != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawPolygonCollider2D(polygon);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    // ─── 콜라이더 헬퍼 메서드 ──────────────────────────────────────

    private void DrawWireCircle(Vector2 center, float radius, int segments = 32)
    {
        Vector3 prev = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void DrawWireCapsule2D(CapsuleCollider2D capsule, int segments = 16)
    {
        Vector2 center    = capsule.offset;
        Vector2 size      = capsule.size;
        bool    isVertical = capsule.direction == CapsuleDirection2D.Vertical;

        float radius = isVertical ? size.x / 2f : size.y / 2f;
        float height = (isVertical ? size.y : size.x) - 2f * radius;

        if (height > 0f)
        {
            if (isVertical)
            {
                Vector2 t = center + Vector2.up    * (height / 2f);
                Vector2 b = center - Vector2.up    * (height / 2f);
                Gizmos.DrawLine(t + Vector2.left  * radius, b + Vector2.left  * radius);
                Gizmos.DrawLine(t + Vector2.right * radius, b + Vector2.right * radius);
            }
            else
            {
                Vector2 r = center + Vector2.right * (height / 2f);
                Vector2 l = center - Vector2.right * (height / 2f);
                Gizmos.DrawLine(r + Vector2.up   * radius, l + Vector2.up   * radius);
                Gizmos.DrawLine(r + Vector2.down * radius, l + Vector2.down * radius);
            }
        }

        for (int i = 0; i < segments; i++)
        {
            float a1 = Mathf.PI * i / segments;
            float a2 = Mathf.PI * (i + 1) / segments;

            if (isVertical)
            {
                Vector2 tc = center + Vector2.up * (height / 2f);
                Vector2 bc = center - Vector2.up * (height / 2f);
                Gizmos.DrawLine(tc + new Vector2(Mathf.Cos(a1),          Mathf.Sin(a1))          * radius,
                                tc + new Vector2(Mathf.Cos(a2),          Mathf.Sin(a2))          * radius);
                Gizmos.DrawLine(bc + new Vector2(Mathf.Cos(a1 + Mathf.PI), Mathf.Sin(a1 + Mathf.PI)) * radius,
                                bc + new Vector2(Mathf.Cos(a2 + Mathf.PI), Mathf.Sin(a2 + Mathf.PI)) * radius);
            }
            else
            {
                Vector2 rc = center + Vector2.right * (height / 2f);
                Vector2 lc = center - Vector2.right * (height / 2f);
                Gizmos.DrawLine(rc + new Vector2(Mathf.Cos(a1 - Mathf.PI / 2f), Mathf.Sin(a1 - Mathf.PI / 2f)) * radius,
                                rc + new Vector2(Mathf.Cos(a2 - Mathf.PI / 2f), Mathf.Sin(a2 - Mathf.PI / 2f)) * radius);
                Gizmos.DrawLine(lc + new Vector2(Mathf.Cos(a1 + Mathf.PI / 2f), Mathf.Sin(a1 + Mathf.PI / 2f)) * radius,
                                lc + new Vector2(Mathf.Cos(a2 + Mathf.PI / 2f), Mathf.Sin(a2 + Mathf.PI / 2f)) * radius);
            }
        }
    }

    private void DrawCompositeCollider2D(CompositeCollider2D composite)
    {
        for (int p = 0; p < composite.pathCount; p++)
        {
            var pts = new Vector2[composite.GetPathPointCount(p)];
            composite.GetPath(p, pts);
            for (int i = 0; i < pts.Length; i++)
                Gizmos.DrawLine(pts[i], pts[(i + 1) % pts.Length]);
        }
    }

    private void DrawPolygonCollider2D(PolygonCollider2D polygon)
    {
        for (int p = 0; p < polygon.pathCount; p++)
        {
            var pts = polygon.GetPath(p);
            if (pts.Length < 3) continue;
            for (int i = 0; i < pts.Length; i++)
                Gizmos.DrawLine(pts[i], pts[(i + 1) % pts.Length]);
        }
    }

#endif
}
