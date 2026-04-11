using UnityEngine;

[ExecuteAlways]
public class ColliderGizmosDrawer : MonoBehaviour
{
    public Color gizmoColor = Color.green;
    public static bool showGizmos = true;
    
    [Header("Monster Range Settings")]
    public bool showMonsterRanges = true;
    public Color attackRangeColor = Color.red;
    public Color roamingRangeColor = Color.magenta;
    public Color detectionRangeColor = Color.yellow;
    public Color chaseRangeColor = new Color(1f, 0.5f, 0f, 1f);

    void Update()
    {
#if UNITY_EDITOR
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.leftShiftKey.isPressed)
        {
            if (kb.f1Key.wasPressedThisFrame)
            {
                showGizmos = !showGizmos;
                Debug.Log("콜라이더 표시: " + (showGizmos ? "ON" : "OFF") + " (Shift+F1로 토글)");
            }
            if (kb.f2Key.wasPressedThisFrame)
            {
                showMonsterRanges = !showMonsterRanges;
                Debug.Log("몬스터 범위 표시: " + (showMonsterRanges ? "ON" : "OFF") + " (Shift+F2로 토글)");
            }
        }
#endif
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        DrawColliderGizmos();
        if (showMonsterRanges) DrawMonsterRanges();
    }
    
    private void DrawColliderGizmos()
    {
        Gizmos.color = gizmoColor;

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }

        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCircle(circle.offset, circle.radius);
        }

        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCapsule2D(capsule);
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
        }
    }
    
    private void DrawMonsterRanges()
    {
        Gizmos.matrix = Matrix4x4.identity;

        // ─── Generic BaseEnemy (GenericMeleeEnemy / GenericRangedEnemy) ───────
        // Elite/Boss는 아래 개별 블록에서 처리하므로 여기선 제외
        var baseEnemy = GetComponent<BaseEnemy>();
        bool isEliteOrBoss = GetComponent<Elite_SandGolem>() != null
                          || GetComponent<Elite_Orc>() != null
                          || GetComponent<Elite_Boar>() != null
                          || GetComponent<Boss_SandElemental>() != null
                          || GetComponent<Boss_ForestElemental>() != null;

        if (baseEnemy != null && !isEliteOrBoss)
        {
            Vector2 spawnPoint   = baseEnemy.SpawnPoint;
            float patrolRadius   = baseEnemy.PatrolRadius;
            float attackRange    = baseEnemy.AttackRange;
            float detectionRange = baseEnemy.DetectionRange;
            float chaseRange     = baseEnemy.ChaseRange;

            Gizmos.color = roamingRangeColor;
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);

            Gizmos.color = attackRangeColor;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = detectionRangeColor;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = chaseRangeColor;
            Gizmos.DrawWireSphere(transform.position, chaseRange);

            Gizmos.color = Color.white;
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);

            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            {
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(transform.position, spawnPoint);
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                    $"{baseEnemy.GetType().Name}\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");

                if (baseEnemy.TargetPlayer != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, baseEnemy.TargetPlayer.transform.position);
                }
            }
#endif
        }

        // ─── Elite_SandGolem ─────────────────────────────────────────────────
        var eliteSandGolem = GetComponent<Elite_SandGolem>();
        if (eliteSandGolem != null)
        {
            Vector2 spawnPoint   = eliteSandGolem.SpawnPosition;
            float patrolRadius   = eliteSandGolem.PatrolRadius;
            float attackRange    = eliteSandGolem.AttackRange;
            float detectionRange = eliteSandGolem.DetectionRange;
            float chaseRange     = eliteSandGolem.ChaseRange;

            Gizmos.color = roamingRangeColor;  Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            Gizmos.color = attackRangeColor;   Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = detectionRangeColor; Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = chaseRangeColor;    Gizmos.DrawWireSphere(transform.position, chaseRange);
            Gizmos.color = Color.white; Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            Gizmos.color = Color.black; Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            { Gizmos.color = Color.gray; Gizmos.DrawLine(transform.position, spawnPoint); }

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                    $"Elite SandGolem\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
#endif
        }

        // ─── Elite_Orc ───────────────────────────────────────────────────────
        var eliteOrc = GetComponent<Elite_Orc>();
        if (eliteOrc != null)
        {
            Vector2 spawnPoint   = eliteOrc.SpawnPosition;
            float patrolRadius   = eliteOrc.PatrolRadius;
            float attackRange    = eliteOrc.AttackRange;
            float detectionRange = eliteOrc.DetectionRange;
            float chaseRange     = eliteOrc.ChaseRange;

            Gizmos.color = roamingRangeColor;  Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            Gizmos.color = attackRangeColor;   Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = detectionRangeColor; Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = chaseRangeColor;    Gizmos.DrawWireSphere(transform.position, chaseRange);
            Gizmos.color = Color.white; Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            Gizmos.color = Color.black; Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            { Gizmos.color = Color.gray; Gizmos.DrawLine(transform.position, spawnPoint); }

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                    $"Elite Orc\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
#endif
        }

        // ─── Elite_Boar ──────────────────────────────────────────────────────
        var eliteBoar = GetComponent<Elite_Boar>();
        if (eliteBoar != null)
        {
            Vector2 spawnPoint   = eliteBoar.SpawnPosition;
            float patrolRadius   = eliteBoar.PatrolRadius;
            float attackRange    = eliteBoar.AttackRange;
            float detectionRange = eliteBoar.DetectionRange;
            float chaseRange     = eliteBoar.ChaseRange;

            Gizmos.color = roamingRangeColor;  Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            Gizmos.color = attackRangeColor;   Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = detectionRangeColor; Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = chaseRangeColor;    Gizmos.DrawWireSphere(transform.position, chaseRange);
            Gizmos.color = Color.white; Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
            Gizmos.color = Color.black; Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.5f);
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            { Gizmos.color = Color.gray; Gizmos.DrawLine(transform.position, spawnPoint); }

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                    $"Elite Boar\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}");
#endif
        }

        // ─── Boss_SandElemental ──────────────────────────────────────────────
        var bossSandElemental = GetComponent<Boss_SandElemental>();
        if (bossSandElemental != null)
        {
            Vector2 spawnPoint   = bossSandElemental.SpawnPosition;
            float patrolRadius   = bossSandElemental.PatrolRadius;
            float attackRange    = bossSandElemental.AttackRange;
            float detectionRange = bossSandElemental.DetectionRange;
            float chaseRange     = bossSandElemental.ChaseRange;

            Gizmos.color = roamingRangeColor;  Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            Gizmos.color = attackRangeColor;   Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = detectionRangeColor; Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = chaseRangeColor;    Gizmos.DrawWireSphere(transform.position, chaseRange);
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, 6f);
            Gizmos.color = Color.white; Gizmos.DrawCube(spawnPoint, Vector3.one * 0.8f);
            Gizmos.color = Color.yellow; Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.8f);
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            { Gizmos.color = Color.gray; Gizmos.DrawLine(transform.position, spawnPoint); }

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEditor.Handles.Label(transform.position + Vector3.up * 3,
                    $"BOSS SandElemental\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}\n근접구분: 6.0f (시안색)");
#endif
        }

        // ─── Boss_ForestElemental ────────────────────────────────────────────
        var bossForestElemental = GetComponent<Boss_ForestElemental>();
        if (bossForestElemental != null)
        {
            Vector2 spawnPoint   = bossForestElemental.SpawnPosition;
            float patrolRadius   = bossForestElemental.PatrolRadius;
            float attackRange    = bossForestElemental.AttackRange;
            float detectionRange = bossForestElemental.DetectionRange;
            float chaseRange     = bossForestElemental.ChaseRange;

            Gizmos.color = roamingRangeColor;  Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
            Gizmos.color = attackRangeColor;   Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.color = detectionRangeColor; Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = chaseRangeColor;    Gizmos.DrawWireSphere(transform.position, chaseRange);
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, 6f);
            Gizmos.color = Color.white; Gizmos.DrawCube(spawnPoint, Vector3.one * 0.8f);
            Gizmos.color = Color.yellow; Gizmos.DrawWireCube(spawnPoint, Vector3.one * 0.8f);
            if (Vector2.Distance(transform.position, spawnPoint) > 0.1f)
            { Gizmos.color = Color.gray; Gizmos.DrawLine(transform.position, spawnPoint); }

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEditor.Handles.Label(transform.position + Vector3.up * 3,
                    $"BOSS ForestElemental\n공격: {attackRange:F1} | 감지: {detectionRange:F1}\n추격: {chaseRange:F1} | 순찰: {patrolRadius:F1}\n근접구분: 6.0f (시안색)");
#endif
        }

        // ─── RangedAttack 발사 지점 시각화 ────────────────────────────────────
        var rangedAttack = GetComponent<RangedAttack>();
        if (rangedAttack != null)
        {
            var spawnPt = rangedAttack.GetProjectileSpawnPoint();
            if (spawnPt != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(spawnPt.position, 0.3f);

                if (Application.isPlaying)
                {
                    var predictedPos = rangedAttack.GetPredictedPlayerPosition();
                    if (predictedPos != Vector3.zero)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(spawnPt.position, predictedPos);
                        Gizmos.DrawCube(predictedPos, Vector3.one * 0.5f);
                    }
                }
            }
        }

        // ─── MeleeAttack 폴백 (BaseEnemy 없는 오브젝트 전용) ─────────────────
        var meleeAttackComp = GetComponent<MeleeAttack>();
        if (meleeAttackComp != null && baseEnemy == null)
        {
            Gizmos.color = attackRangeColor;
            float atkRange = meleeAttackComp.GetAttackRange();
            Gizmos.DrawWireSphere(transform.position, atkRange);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f,
                $"MeleeAttack\nRange: {atkRange:F1}");
#endif
        }
    }

    void DrawWireCircle(Vector2 center, float radius, int segments = 32)
    {
        float angle = 0f;
        Vector3 prevPoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            angle = i * Mathf.PI * 2f / segments;
            Vector3 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    void DrawWireCapsule2D(CapsuleCollider2D capsule, int segments = 16)
    {
        Vector2 center = capsule.offset;
        Vector2 size = capsule.size;
        float radius;
        float height;
        bool isVertical = (capsule.direction == CapsuleDirection2D.Vertical);

        if (isVertical)
        {
            radius = size.x / 2f;
            height = size.y - 2 * radius;
        }
        else
        {
            radius = size.y / 2f;
            height = size.x - 2 * radius;
        }

        if (height > 0)
        {
            if (isVertical)
            {
                Vector2 top = center + Vector2.up * height / 2f;
                Vector2 bottom = center - Vector2.up * height / 2f;
                Gizmos.DrawLine(top + Vector2.left * radius, bottom + Vector2.left * radius);
                Gizmos.DrawLine(top + Vector2.right * radius, bottom + Vector2.right * radius);
            }
            else
            {
                Vector2 right = center + Vector2.right * height / 2f;
                Vector2 left  = center - Vector2.right * height / 2f;
                Gizmos.DrawLine(right + Vector2.up * radius, left + Vector2.up * radius);
                Gizmos.DrawLine(right + Vector2.down * radius, left + Vector2.down * radius);
            }
        }

        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.PI * i / segments;
            float angle2 = Mathf.PI * (i + 1) / segments;

            if (isVertical)
            {
                Vector2 topCenter    = center + Vector2.up * height / 2f;
                Vector2 bottomCenter = center - Vector2.up * height / 2f;
                Gizmos.DrawLine(
                    topCenter + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius,
                    topCenter + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius);
                Gizmos.DrawLine(
                    bottomCenter + new Vector2(Mathf.Cos(angle1 + Mathf.PI), Mathf.Sin(angle1 + Mathf.PI)) * radius,
                    bottomCenter + new Vector2(Mathf.Cos(angle2 + Mathf.PI), Mathf.Sin(angle2 + Mathf.PI)) * radius);
            }
            else
            {
                Vector2 rightCenter = center + Vector2.right * height / 2f;
                Vector2 leftCenter  = center - Vector2.right * height / 2f;
                Gizmos.DrawLine(
                    rightCenter + new Vector2(Mathf.Cos(angle1 - Mathf.PI / 2f), Mathf.Sin(angle1 - Mathf.PI / 2f)) * radius,
                    rightCenter + new Vector2(Mathf.Cos(angle2 - Mathf.PI / 2f), Mathf.Sin(angle2 - Mathf.PI / 2f)) * radius);
                Gizmos.DrawLine(
                    leftCenter + new Vector2(Mathf.Cos(angle1 + Mathf.PI / 2f), Mathf.Sin(angle1 + Mathf.PI / 2f)) * radius,
                    leftCenter + new Vector2(Mathf.Cos(angle2 + Mathf.PI / 2f), Mathf.Sin(angle2 + Mathf.PI / 2f)) * radius);
            }
        }
    }
    
    void DrawCompositeCollider2D(CompositeCollider2D composite)
    {
        for (int pathIndex = 0; pathIndex < composite.pathCount; pathIndex++)
        {
            Vector2[] pathPoints = new Vector2[composite.GetPathPointCount(pathIndex)];
            composite.GetPath(pathIndex, pathPoints);
            for (int i = 0; i < pathPoints.Length; i++)
                Gizmos.DrawLine(pathPoints[i], pathPoints[(i + 1) % pathPoints.Length]);
        }
    }
    
    void DrawPolygonCollider2D(PolygonCollider2D polygon)
    {
        for (int pathIndex = 0; pathIndex < polygon.pathCount; pathIndex++)
        {
            Vector2[] pathPoints = polygon.GetPath(pathIndex);
            if (pathPoints.Length < 3) continue;
            for (int i = 0; i < pathPoints.Length; i++)
                Gizmos.DrawLine(pathPoints[i], pathPoints[(i + 1) % pathPoints.Length]);
        }
    }
}
