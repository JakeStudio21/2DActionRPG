using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Barricade의 NavMeshObstacle들을 Scene View에서 시각화
/// (선택사항)
/// </summary>
public class BarricadeNavMeshGizmos : MonoBehaviour
{
    [SerializeField] private Color obstacleColor = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
    [SerializeField] private bool showGizmos = true;
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // 자식 NavMeshObstacle들을 시각화
        NavMeshObstacle[] obstacles = GetComponentsInChildren<NavMeshObstacle>();
        
        Gizmos.color = obstacleColor;
        
        foreach (var obstacle in obstacles)
        {
            if (obstacle != null && obstacle.enabled)
            {
                Vector3 center = obstacle.transform.position + obstacle.center;
                
                switch (obstacle.shape)
                {
                    case NavMeshObstacleShape.Box:
                        Gizmos.DrawCube(center, obstacle.size);
                        Gizmos.DrawWireCube(center, obstacle.size);
                        break;
                        
                    case NavMeshObstacleShape.Capsule:
                        // Capsule은 간단히 Sphere로 표시
                        Gizmos.DrawSphere(center, obstacle.size.x * 0.5f);
                        Gizmos.DrawWireSphere(center, obstacle.size.x * 0.5f);
                        break;
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // 선택 시 더 밝게 표시
        NavMeshObstacle[] obstacles = GetComponentsInChildren<NavMeshObstacle>();
        
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f); // 밝은 주황색
        
        foreach (var obstacle in obstacles)
        {
            if (obstacle != null)
            {
                Vector3 center = obstacle.transform.position + obstacle.center;
                
                // 이름 표시 (Unity Editor에서만)
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(center, obstacle.gameObject.name);
                #endif
            }
        }
    }
}

