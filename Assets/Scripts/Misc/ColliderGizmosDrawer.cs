using UnityEngine;

[ExecuteAlways]
public class ColliderGizmosDrawer : MonoBehaviour
{
    public Color gizmoColor = Color.green;
    public static bool showGizmos = true;

    void Update()
    {
        // Shift+F1 단축키로 토글
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F1))
        {
            showGizmos = !showGizmos;
            Debug.Log("콜라이더 표시: " + (showGizmos ? "ON" : "OFF") + " (Shift+F1로 토글)");
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;

        // BoxCollider2D
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }

        // CircleCollider2D
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCircle(circle.offset, circle.radius);
        }

        // CapsuleCollider2D
        var capsule = GetComponent<CapsuleCollider2D>();
        if (capsule != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            DrawWireCapsule2D(capsule);
        }
    }

    // 원을 선으로 그려주는 함수
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

    // 캡슐을 선으로 그려주는 함수 (CapsuleCollider2D)
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

        // Draw rectangle part
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
                Vector2 left = center - Vector2.right * height / 2f;
                Gizmos.DrawLine(right + Vector2.up * radius, left + Vector2.up * radius);
                Gizmos.DrawLine(right + Vector2.down * radius, left + Vector2.down * radius);
            }
        }

        // Draw semicircle ends
        float startAngle = isVertical ? 0 : Mathf.PI / 2f;
        float endAngle = isVertical ? Mathf.PI : Mathf.PI * 3f / 2f;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = Mathf.PI * i / segments;
            float angle2 = Mathf.PI * (i + 1) / segments;

            if (isVertical)
            {
                // Top semicircle
                Vector2 topCenter = center + Vector2.up * height / 2f;
                Vector2 p1 = topCenter + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
                Vector2 p2 = topCenter + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;
                Gizmos.DrawLine(p1, p2);

                // Bottom semicircle
                Vector2 bottomCenter = center - Vector2.up * height / 2f;
                Vector2 p3 = bottomCenter + new Vector2(Mathf.Cos(angle1 + Mathf.PI), Mathf.Sin(angle1 + Mathf.PI)) * radius;
                Vector2 p4 = bottomCenter + new Vector2(Mathf.Cos(angle2 + Mathf.PI), Mathf.Sin(angle2 + Mathf.PI)) * radius;
                Gizmos.DrawLine(p3, p4);
            }
            else
            {
                // Right semicircle
                Vector2 rightCenter = center + Vector2.right * height / 2f;
                Vector2 p1 = rightCenter + new Vector2(Mathf.Cos(angle1 - Mathf.PI / 2f), Mathf.Sin(angle1 - Mathf.PI / 2f)) * radius;
                Vector2 p2 = rightCenter + new Vector2(Mathf.Cos(angle2 - Mathf.PI / 2f), Mathf.Sin(angle2 - Mathf.PI / 2f)) * radius;
                Gizmos.DrawLine(p1, p2);

                // Left semicircle
                Vector2 leftCenter = center - Vector2.right * height / 2f;
                Vector2 p3 = leftCenter + new Vector2(Mathf.Cos(angle1 + Mathf.PI / 2f), Mathf.Sin(angle1 + Mathf.PI / 2f)) * radius;
                Vector2 p4 = leftCenter + new Vector2(Mathf.Cos(angle2 + Mathf.PI / 2f), Mathf.Sin(angle2 + Mathf.PI / 2f)) * radius;
                Gizmos.DrawLine(p3, p4);
            }
        }
    }
}
