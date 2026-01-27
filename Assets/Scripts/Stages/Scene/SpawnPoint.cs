using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // NavMesh 검증용

namespace StageSystem
{
    /// <summary>
    /// 적 스폰 포인트 컴포넌트
    /// 기존 PlayerSpawnPoint와 구분되는 적 전용 스폰 시스템
    /// NavMesh 검증 시스템 포함
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("스폰 포인트 설정")]
        public string spawnPointID = "SP_01";
        
        [Header("🎯 스폰 그룹 연결 (디자이너 설정)")]
        [Tooltip("이 SpawnPoint에서 스폰될 SpawnGroupID")]
        public string assignedSpawnGroupID = "";  // 디자이너가 직접 입력
        
        public SpawnType spawnType = SpawnType.Point;
        public bool isActive = true;
        
        [Header("영역 설정")]
        public float radius = 1f;
        public Vector2 rectangleSize = new Vector2(2f, 2f);
        
        [Header("스폰 제한")]
        public LayerMask spawnLayers = -1;
        public LayerMask obstacleLayerMask = 1; // 장애물 레이어
        public float minDistanceFromPlayer = 3f;
        public float minDistanceFromOtherEnemies = 1f;
        
        [Header("기즈모 설정")]
        public Color gizmoColor = Color.red;
        public bool showGizmos = true;
        public bool showSpawnableArea = true;
        
        // 기존 SpawnPoint 클래스에 추가할 필드들
        [Header("몬스터 배치 설정")]
        [SerializeField] private float spawnSpread = 2f; // 스폰 분산 범위
        [SerializeField] private float patrolRadius = 0f; // 순찰 반경 (0 = EnemyData 사용)
        [SerializeField] private int maxMonstersPerPoint = 10; // 포인트당 최대 몬스터 수
        [SerializeField] private SpawnPattern spawnPattern = SpawnPattern.Random; // 배치 패턴

        public enum SpawnPattern
        {
            Random,    // 랜덤 배치
            Circle,    // 원형 배치
            Grid       // 격자 배치
        }

        // 프로퍼티 추가
        public float SpawnSpread => spawnSpread;
        public float PatrolRadius => patrolRadius;
        public int MaxMonstersPerPoint => maxMonstersPerPoint;
        public SpawnPattern Pattern => spawnPattern;

        /// <summary>
        /// 스폰 위치 계산
        /// </summary>
        public Vector3 GetSpawnPosition()
        {
            Vector3 basePosition = transform.position;
            Vector3 spawnPosition = basePosition;
            
            switch (spawnType)
            {
                case SpawnType.Point:
                    spawnPosition = basePosition;
                    break;
                    
                case SpawnType.Area:
                case SpawnType.Circle:
                    Vector2 randomPoint = Random.insideUnitCircle * radius;
                    spawnPosition = basePosition + new Vector3(randomPoint.x, randomPoint.y, 0);
                    break;
                    
                case SpawnType.Rectangle:
                    float randomX = Random.Range(-rectangleSize.x / 2f, rectangleSize.x / 2f);
                    float randomY = Random.Range(-rectangleSize.y / 2f, rectangleSize.y / 2f);
                    spawnPosition = basePosition + new Vector3(randomX, randomY, 0);
                    break;
            }
            
            return spawnPosition;
        }
        
        /// <summary>
        /// 스폰 위치 유효성 검증 (NavMesh 체크 포함)
        /// </summary>
        public bool IsValidSpawnPosition(Vector3 position)
        {
            // ⭐⭐⭐ NavMesh 위치 체크 (최우선 검증!)
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
            {
                // NavMesh 위가 아님 → 무효
                return false;
            }
            
            // NavMesh 위의 유효한 위치로 보정
            position = hit.position;
            
            // 장애물과 겹침 체크
            Collider2D obstacle = Physics2D.OverlapCircle(position, 0.5f, obstacleLayerMask);
            if (obstacle != null)
            {
                return false;
            }
            
            // 플레이어와의 거리 체크
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(position, player.transform.position);
                if (distanceToPlayer < minDistanceFromPlayer)
                {
                    return false;
                }
            }
            
            // 다른 적들과의 거리 체크
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies)
            {
                float distanceToEnemy = Vector3.Distance(position, enemy.transform.position);
                if (distanceToEnemy < minDistanceFromOtherEnemies)
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 안전한 스폰 위치 찾기 (모바일 최적화: 재시도 5회, 경계 이격 보장)
        /// </summary>
        public Vector3 GetSafeSpawnPosition(int maxAttempts = 5)
        {
            Vector3 bestPosition = transform.position;
            float maxEdgeDistance = 0f;
            bool foundValidPosition = false;
            
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 candidatePosition = GetSpawnPosition();
                
                // ⭐ NavMesh 위치 보정
                NavMeshHit hit;
                if (NavMesh.SamplePosition(candidatePosition, out hit, 5f, NavMesh.AllAreas))
                {
                    candidatePosition = hit.position;
                    
                    // 기본 유효성 검증
                    if (IsValidSpawnPosition(candidatePosition))
                    {
                        // ⭐ 경계까지 거리 측정 (경계에서 가장 먼 위치 선택)
                        NavMeshHit edgeHit;
                        if (NavMesh.FindClosestEdge(candidatePosition, out edgeHit, NavMesh.AllAreas))
                        {
                            if (edgeHit.distance > maxEdgeDistance)
                            {
                                maxEdgeDistance = edgeHit.distance;
                                bestPosition = candidatePosition;
                                foundValidPosition = true;
                            }
                        }
                        else
                        {
                            // 경계 찾기 실패해도 유효한 위치면 사용
                            bestPosition = candidatePosition;
                            foundValidPosition = true;
                        }
                    }
                }
            }
            
            if (foundValidPosition)
            {
                // ⭐ 경계에서 최소 1.5m 이격 보장
                return EnsureDistanceFromEdge(bestPosition, 1.5f);
            }
            
            // 안전한 위치를 찾지 못한 경우 NavMesh 위의 기본 위치 반환
            Debug.LogWarning($"[SpawnPoint] {spawnPointID}: 안전한 스폰 위치를 찾지 못함. 기본 위치 사용.");
            
            // ⭐⭐⭐ 기본 위치도 NavMesh 위로 보정 + 경계 이격
            NavMeshHit fallbackHit;
            if (NavMesh.SamplePosition(transform.position, out fallbackHit, 10f, NavMesh.AllAreas))
            {
                return EnsureDistanceFromEdge(fallbackHit.position, 1.5f);
            }
            
            return transform.position;
        }
        
        /// <summary>
        /// ⭐ NavMesh 경계에서 안쪽으로 이격 보장
        /// </summary>
        private Vector3 EnsureDistanceFromEdge(Vector3 position, float minDistanceFromEdge)
        {
            NavMeshHit edgeHit;
            if (!NavMesh.FindClosestEdge(position, out edgeHit, NavMesh.AllAreas))
            {
                return position;
            }
            
            // 경계에 충분히 멀면 그대로 사용
            if (edgeHit.distance >= minDistanceFromEdge)
            {
                return position;
            }
            
            // 경계에 너무 가까움 → 안쪽으로 밀기
            float pushDistance = minDistanceFromEdge - edgeHit.distance + 0.3f;
            Vector3 safePosition = position + edgeHit.normal * pushDistance;
            
            // 이동된 위치가 NavMesh 위인지 검증
            NavMeshHit hit;
            if (NavMesh.SamplePosition(safePosition, out hit, 2f, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return position; // 보정 실패 시 원래 위치 유지
        }
        
        /// <summary>
        /// 분산된 스폰 위치 계산 (NavMesh + 경계 이격 + 몬스터 간 거리 체크)
        /// </summary>
        public Vector3 GetDistributedSpawnPosition(int monsterIndex, int totalMonsters)
        {
            Vector3 basePosition = transform.position;
            int maxAttempts = 5; // 모바일 최적화: 5회 재시도
            
            // ⭐ 다중 후보 중 최선 선택 (몬스터 간 거리 체크 포함)
            Vector3 bestPosition = basePosition;
            float maxEdgeDistance = 0f;
            bool foundValidPosition = false;
            
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                Vector3 calculatedPosition;
                
                // 패턴별 위치 계산
                switch (spawnPattern)
                {
                    case SpawnPattern.Circle:
                        calculatedPosition = GetCirclePosition(basePosition, monsterIndex, totalMonsters);
                        // Random 오프셋 추가 (겹침 방지)
                        if (attempt > 0)
                        {
                            Vector2 randomOffset = Random.insideUnitCircle * (spawnSpread * 0.3f);
                            calculatedPosition += new Vector3(randomOffset.x, randomOffset.y, 0f);
                        }
                        break;
                    case SpawnPattern.Grid:
                        calculatedPosition = GetGridPosition(basePosition, monsterIndex);
                        // Random 오프셋 추가 (겹침 방지)
                        if (attempt > 0)
                        {
                            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
                            calculatedPosition += new Vector3(randomOffset.x, randomOffset.y, 0f);
                        }
                        break;
                    case SpawnPattern.Random:
                    default:
                        calculatedPosition = GetRandomPosition(basePosition);
                        break;
                }
                
                // ⭐⭐⭐ NavMesh 위치 보정
                NavMeshHit hit;
                if (NavMesh.SamplePosition(calculatedPosition, out hit, 5f, NavMesh.AllAreas))
                {
                    Vector3 candidate = EnsureDistanceFromEdge(hit.position, 1.5f);
                    
                    // ⭐⭐⭐ 핵심 수정: 다른 몬스터와의 거리 체크!
                    if (IsValidSpawnPosition(candidate))
                    {
                        // 경계까지 거리 측정 (경계에서 가장 먼 위치 선택)
                        NavMeshHit edgeHit;
                        if (NavMesh.FindClosestEdge(candidate, out edgeHit, NavMesh.AllAreas))
                        {
                            if (edgeHit.distance > maxEdgeDistance)
                            {
                                maxEdgeDistance = edgeHit.distance;
                                bestPosition = candidate;
                                foundValidPosition = true;
                            }
                        }
                        else
                        {
                            // 경계 찾기 실패해도 유효한 위치면 사용
                            bestPosition = candidate;
                            foundValidPosition = true;
                        }
                    }
                }
            }
            
            // 유효한 위치를 찾았으면 반환
            if (foundValidPosition)
            {
                return bestPosition;
            }
            
            // ⚠️ 유효한 위치를 찾지 못한 경우: fallback 로직
            // 기본 위치 근처에서 NavMesh 위치 찾기 (거리 체크 없이)
            NavMeshHit fallbackHit;
            if (NavMesh.SamplePosition(basePosition, out fallbackHit, 10f, NavMesh.AllAreas))
            {
                Vector3 fallbackPos = EnsureDistanceFromEdge(fallbackHit.position, 1.5f);
                
                Debug.LogWarning($"⚠️ [SpawnPoint] {spawnPointID}: 몬스터 간 거리 조건을 만족하는 위치를 찾지 못함. Fallback 사용.");
                
                return fallbackPos;
            }
            
            // 최악의 경우: 계산된 위치 반환
            Debug.LogWarning($"❌ [SpawnPoint] {spawnPointID}: NavMesh 위치를 찾지 못함. 기본 위치 사용.");
            return basePosition;
        }

        /// <summary>
        /// 원형 배치
        /// </summary>
        private Vector3 GetCirclePosition(Vector3 center, int index, int total)
        {
            if (total == 1) return center;
            
            float angle = (360f / total) * index * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * spawnSpread;
            float y = center.y + Mathf.Sin(angle) * spawnSpread;
            
            return new Vector3(x, y, center.z);
        }

        /// <summary>
        /// 격자 배치
        /// </summary>
        private Vector3 GetGridPosition(Vector3 center, int index)
        {
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(maxMonstersPerPoint));
            int row = index / gridSize;
            int col = index % gridSize;
            
            float spacing = spawnSpread / gridSize;
            float offsetX = (col - gridSize / 2f) * spacing;
            float offsetY = (row - gridSize / 2f) * spacing;
            
            return center + new Vector3(offsetX, offsetY, 0);
        }

        /// <summary>
        /// 랜덤 배치
        /// </summary>
        private Vector3 GetRandomPosition(Vector3 center)
        {
            Vector2 randomOffset = Random.insideUnitCircle * spawnSpread;
            return center + new Vector3(randomOffset.x, randomOffset.y, 0);
        }
        
        /// <summary>
        /// 기즈모 그리기
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // 활성/비활성에 따른 색상 조정
            Color drawColor = isActive ? gizmoColor : Color.gray;
            drawColor.a = 0.3f;
            Gizmos.color = drawColor;
            
            Vector3 position = transform.position;
            
            switch (spawnType)
            {
                case SpawnType.Point:
                    Gizmos.DrawWireSphere(position, 0.5f);
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(position, 0.2f);
                    break;
                    
                case SpawnType.Area:
                case SpawnType.Circle:
                    Gizmos.DrawWireSphere(position, radius);
                    if (showSpawnableArea)
                    {
                        drawColor.a = 0.1f;
                        Gizmos.color = drawColor;
                        Gizmos.DrawSphere(position, radius);
                    }
                    break;
                    
                case SpawnType.Rectangle:
                    Vector3 size = new Vector3(rectangleSize.x, rectangleSize.y, 0.1f);
                    Gizmos.DrawWireCube(position, size);
                    if (showSpawnableArea)
                    {
                        drawColor.a = 0.1f;
                        Gizmos.color = drawColor;
                        Gizmos.DrawCube(position, size);
                    }
                    break;
            }
            
            // ID 텍스트 표시 (에디터에서만)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position + Vector3.up * 1f, spawnPointID);
            #endif
        }
        
        /// <summary>
        /// 선택된 상태에서 기즈모 그리기
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            
            // 플레이어와의 최소 거리 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minDistanceFromPlayer);
            
            // 다른 적과의 최소 거리 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, minDistanceFromOtherEnemies);
        }
        
        /// <summary>
        /// 유효성 검사
        /// </summary>
        private void OnValidate()
        {
            if (radius < 0.1f) radius = 0.1f;
            if (rectangleSize.x < 0.1f) rectangleSize.x = 0.1f;
            if (rectangleSize.y < 0.1f) rectangleSize.y = 0.1f;
            if (minDistanceFromPlayer < 0f) minDistanceFromPlayer = 0f;
            if (minDistanceFromOtherEnemies < 0f) minDistanceFromOtherEnemies = 0f;
            
            // 스폰 타입에 따른 기즈모 색상 자동 설정
            switch (spawnType)
            {
                case SpawnType.Point:
                    gizmoColor = Color.red;
                    break;
                case SpawnType.Area:
                case SpawnType.Circle:
                    gizmoColor = Color.blue;
                    break;
                case SpawnType.Rectangle:
                    gizmoColor = Color.green;
                    break;
            }
        }
    }
}
