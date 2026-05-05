using System.Collections;
using UnityEngine;
using CueSystem;

/// <summary>
/// 기믹 장치의 데미지 판정 영역.
///
/// 구조:
///   BaseTrap 자식 오브젝트 또는 이동하는 공격체(톱날, 철퇴 등)의 자식으로 배치.
///   Collider2D (IsTrigger = true) 필수.
///
/// 사용 패턴:
///   고정형: 특정 위치에 배치. Activate(duration)으로 잠깐 켰다 끔 (망치 착지점, 가시 돌출 등)
///   이동형: 공격 오브젝트 자식으로 배치. 오브젝트가 이동하는 동안 상시 판정 (톱날, 회전 스파이크 등)
///
/// 동작 흐름:
///   Activate(duration) → damageDuration초 판정 ON → 자동 OFF
///   Activate()         → 수동으로 Deactivate() 호출할 때까지 판정 ON
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TrapDamageZone : MonoBehaviour
{
    [Header("데미지 설정")]
    [Tooltip("플레이어에게 가하는 데미지")]
    [SerializeField] private int damage = 1;

    [Tooltip("연속 피격 방지 쿨타임 (초)")]
    [SerializeField] private float damageCooldown = 0.5f;

    [Header("넉백 설정")]
    [Tooltip("넉백 방향의 기준 Transform. null이면 이 오브젝트 자신을 기준으로 사용.")]
    [SerializeField] private Transform knockbackSource;

    [Tooltip("넉백 강도. 0이면 넉백 없음.")]
    [SerializeField] private float knockbackThrust = 8f;

    [Header("레이어")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Cue 이벤트")]
    [Tooltip("데미지 발생 시 발행할 CueKey (비워두면 이벤트 없음)")]
    [SerializeField] private string hitCueKey = "hit.player.normal";

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private Collider2D col;
    private float lastDamageTime = -99f;
    private Coroutine deactivateRoutine;
    private bool isActive = false;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        SetActive(false);
    }

    // ── 활성화 API ────────────────────────────────────────────────────────────

    /// <summary>
    /// duration초 동안 판정을 활성화한 뒤 자동으로 비활성화한다.
    /// 애니메이션 타이밍 기반 기믹(망치, 가시 등)에서 사용.
    /// </summary>
    public void Activate(float duration)
    {
        if (deactivateRoutine != null)
            StopCoroutine(deactivateRoutine);

        SetActive(true);
        deactivateRoutine = StartCoroutine(DeactivateAfter(duration));
    }

    /// <summary>
    /// 수동으로 비활성화할 때까지 판정을 유지한다.
    /// 이동형 기믹(톱날, 회전 스파이크 등)에서 사용.
    /// </summary>
    public void Activate()
    {
        if (deactivateRoutine != null)
        {
            StopCoroutine(deactivateRoutine);
            deactivateRoutine = null;
        }

        SetActive(true);
    }

    /// <summary>판정을 즉시 비활성화한다.</summary>
    public void Deactivate()
    {
        if (deactivateRoutine != null)
        {
            StopCoroutine(deactivateRoutine);
            deactivateRoutine = null;
        }

        SetActive(false);
    }

    private void SetActive(bool value)
    {
        isActive = value;
        col.enabled = value;
    }

    private IEnumerator DeactivateAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        SetActive(false);
        deactivateRoutine = null;
    }

    // ── 피격 처리 ─────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        TryDamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (Time.time - lastDamageTime < damageCooldown) return;

        lastDamageTime = Time.time;

        Transform source = knockbackSource != null ? knockbackSource : transform;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damage, source);

        if (!string.IsNullOrEmpty(hitCueKey))
            CueEmitter.EmitAt(other.transform.position, hitCueKey, "Player");
    }

    // ── 에디터 Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Gizmos.color = isActive
            ? new Color(1f, 0f, 0f, 0.5f)
            : new Color(1f, 0.4f, 0f, 0.2f);

        DrawColliderGizmo(c);

        if (knockbackSource != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);
            Gizmos.DrawLine(transform.position, knockbackSource.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        DrawColliderGizmo(c);

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.3f,
            $"DMG:{damage}  KB:{knockbackThrust}\nCooldown:{damageCooldown}s");
    }

    private void DrawColliderGizmo(Collider2D c)
    {
        if (c is BoxCollider2D box)
        {
            Matrix4x4 m = Matrix4x4.TRS(
                transform.position + (Vector3)(box.offset * transform.lossyScale),
                transform.rotation,
                transform.lossyScale);
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (c is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(
                transform.position + (Vector3)circle.offset,
                circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
        }
        else if (c is CapsuleCollider2D capsule)
        {
            DrawCapsuleGizmo(capsule);
        }
        else if (c is PolygonCollider2D polygon)
        {
            DrawPolygonGizmo(polygon);
        }
    }

    private void DrawCapsuleGizmo(CapsuleCollider2D capsule)
    {
        Vector3 center = transform.position + transform.rotation * (Vector3)(capsule.offset * transform.lossyScale);
        float w = capsule.size.x * transform.lossyScale.x;
        float h = capsule.size.y * transform.lossyScale.y;

        // 캡슐 방향 벡터를 transform.rotation으로 회전시켜서 반영
        if (capsule.direction == CapsuleDirection2D.Horizontal)
        {
            float radius = h * 0.5f;
            float bodyHalf = Mathf.Max(0f, w * 0.5f - radius);

            Vector3 axisDir = transform.rotation * Vector3.right;
            Gizmos.DrawSphere(center + axisDir * bodyHalf, radius);
            Gizmos.DrawSphere(center - axisDir * bodyHalf, radius);

            Matrix4x4 m = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, new Vector3(bodyHalf * 2f, h, 0f));
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            float radius = w * 0.5f;
            float bodyHalf = Mathf.Max(0f, h * 0.5f - radius);

            Vector3 axisDir = transform.rotation * Vector3.up;
            Gizmos.DrawSphere(center + axisDir * bodyHalf, radius);
            Gizmos.DrawSphere(center - axisDir * bodyHalf, radius);

            Matrix4x4 m = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, new Vector3(w, bodyHalf * 2f, 0f));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    private void DrawPolygonGizmo(PolygonCollider2D polygon)
    {
        Color fillColor = Gizmos.color;

        for (int i = 0; i < polygon.pathCount; i++)
        {
            Vector2[] points = polygon.GetPath(i);
            Vector3[] worldPoints = new Vector3[points.Length];
            for (int j = 0; j < points.Length; j++)
                worldPoints[j] = transform.TransformPoint(points[j]);

            // 채워진 면적 표시 (볼록 다각형 기준)
            UnityEditor.Handles.color = fillColor;
            UnityEditor.Handles.DrawAAConvexPolygon(worldPoints);

            // 외곽선 표시
            for (int j = 0; j < worldPoints.Length; j++)
            {
                Gizmos.color = new Color(fillColor.r, fillColor.g, fillColor.b, 1f);
                Gizmos.DrawLine(worldPoints[j], worldPoints[(j + 1) % worldPoints.Length]);
            }

            Gizmos.color = fillColor;
        }
    }
#endif
}
