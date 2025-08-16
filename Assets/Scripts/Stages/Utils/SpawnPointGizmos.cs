using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스폰 포인트 기즈모 확장 시각화
    /// SpawnPoint.cs의 기즈모 기능을 보완
    /// </summary>
    public class SpawnPointGizmos : MonoBehaviour
    {
        [Header("고급 기즈모 설정")]
        public bool showSpawnRadius = true;
        public bool showPlayerMinDistance = true;
        public bool showEnemyMinDistance = true;
        public bool showSpawnPath = false;
        
        [Header("색상 설정")]
        public Color spawnRadiusColor = Color.blue;
        public Color playerDistanceColor = Color.yellow;
        public Color enemyDistanceColor = Color.cyan;
        public Color pathColor = Color.white;
        
        private SpawnPoint spawnPoint;
        
        private void Awake()
        {
            spawnPoint = GetComponent<SpawnPoint>();
        }
        
        /// <summary>
        /// 고급 기즈모 그리기
        /// </summary>
        private void OnDrawGizmos()
        {
            if (spawnPoint == null) return;
            
            Vector3 position = transform.position;
            
            // 스폰 반경 표시
            if (showSpawnRadius)
            {
                Gizmos.color = spawnRadiusColor;
                switch (spawnPoint.spawnType)
                {
                    case SpawnType.Circle:
                    case SpawnType.Area:
                        Gizmos.DrawWireSphere(position, spawnPoint.radius);
                        break;
                    case SpawnType.Rectangle:
                        Vector3 size = new Vector3(spawnPoint.rectangleSize.x, spawnPoint.rectangleSize.y, 0.1f);
                        Gizmos.DrawWireCube(position, size);
                        break;
                }
            }
            
            // 플레이어 최소 거리 표시
            if (showPlayerMinDistance)
            {
                Gizmos.color = playerDistanceColor;
                Gizmos.DrawWireSphere(position, spawnPoint.minDistanceFromPlayer);
            }
            
            // 적 최소 거리 표시
            if (showEnemyMinDistance)
            {
                Gizmos.color = enemyDistanceColor;
                Gizmos.DrawWireSphere(position, spawnPoint.minDistanceFromOtherEnemies);
            }
            
            // 스폰 경로 표시 (옵션)
            if (showSpawnPath)
            {
                DrawSpawnPath(position);
            }
        }
        
        /// <summary>
        /// 스폰 경로 시각화
        /// </summary>
        private void DrawSpawnPath(Vector3 center)
        {
            Gizmos.color = pathColor;
            
            // 여러 샘플 위치 표시
            for (int i = 0; i < 8; i++)
            {
                Vector3 samplePos = GetSampleSpawnPosition(center, i);
                Gizmos.DrawWireSphere(samplePos, 0.1f);
                Gizmos.DrawLine(center, samplePos);
            }
        }
        
        /// <summary>
        /// 샘플 스폰 위치 계산
        /// </summary>
        private Vector3 GetSampleSpawnPosition(Vector3 center, int index)
        {
            switch (spawnPoint.spawnType)
            {
                case SpawnType.Circle:
                case SpawnType.Area:
                    float angle = (360f / 8f) * index * Mathf.Deg2Rad;
                    return center + new Vector3(
                        Mathf.Cos(angle) * spawnPoint.radius,
                        Mathf.Sin(angle) * spawnPoint.radius,
                        0
                    );
                
                case SpawnType.Rectangle:
                    int x = index % 3 - 1; // -1, 0, 1
                    int y = index / 3 - 1; // -1, 0, 1
                    return center + new Vector3(
                        x * spawnPoint.rectangleSize.x * 0.4f,
                        y * spawnPoint.rectangleSize.y * 0.4f,
                        0
                    );
                
                default:
                    return center;
            }
        }
    }
}
