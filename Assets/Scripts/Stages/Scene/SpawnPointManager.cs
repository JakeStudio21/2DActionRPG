using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 씬별 스폰 포인트 관리자
    /// 기존 PlayerSpawner와 연동하여 통합 스폰 관리
    /// </summary>
    public class SpawnPointManager : MonoBehaviour
    {
        [Header("스폰 포인트 관리")]
        public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        
        [Header("기존 시스템 연동")]
        public PlayerSpawner playerSpawner; // 기존 PlayerSpawner 참조
        public Transform playerSpawnPoint; // 기존 PlayerSpawnPoint 참조
        
        [Header("디버그")]
        public bool showAllGizmos = true;
        
        private void Start()
        {
            InitializeSpawnPoints();
        }
        
        /// <summary>
        /// 스폰 포인트 시스템 초기화
        /// </summary>
        private void InitializeSpawnPoints()
        {
            // 씬에서 모든 SpawnPoint 자동 수집
            CollectSpawnPoints();
            
            // 기존 PlayerSpawner 시스템 연동
            ValidatePlayerSpawnSystem();
            
            {
            }
        }
        
        /// <summary>
        /// 씬의 모든 SpawnPoint 수집
        /// </summary>
        private void CollectSpawnPoints()
        {
            // 기존 리스트 초기화
            spawnPoints.Clear();
            
            // 씬의 모든 SpawnPoint 찾기
            SpawnPoint[] foundSpawnPoints = FindObjectsOfType<SpawnPoint>();
            spawnPoints.AddRange(foundSpawnPoints);
            
            // ID 기준으로 정렬
            spawnPoints = spawnPoints.OrderBy(sp => sp.spawnPointID).ToList();
        }
        
        /// <summary>
        /// 기존 PlayerSpawner 시스템 검증
        /// </summary>
        private void ValidatePlayerSpawnSystem()
        {
            // PlayerSpawner 자동 찾기
            if (playerSpawner == null)
            {
                playerSpawner = FindObjectOfType<PlayerSpawner>();
            }
            
            // PlayerSpawnPoint 자동 찾기
            if (playerSpawnPoint == null)
            {
                GameObject spawnPointObj = GameObject.Find("PlayerSpawnPoint");
                if (spawnPointObj != null)
                {
                    playerSpawnPoint = spawnPointObj.transform;
                }
            }
            
        }
        
        /// <summary>
        /// ID로 스폰 포인트 찾기
        /// </summary>
        public SpawnPoint GetSpawnPointById(string spawnPointId)
        {
            return spawnPoints.Find(sp => sp.spawnPointID == spawnPointId);
        }
        
        /// <summary>
        /// 타입별 스폰 포인트 목록 가져오기
        /// </summary>
        public List<SpawnPoint> GetSpawnPointsByType(SpawnType spawnType)
        {
            return spawnPoints.Where(sp => sp.spawnType == spawnType && sp.isActive).ToList();
        }
        
        /// <summary>
        /// 활성화된 스폰 포인트 목록
        /// </summary>
        public List<SpawnPoint> GetActiveSpawnPoints()
        {
            return spawnPoints.Where(sp => sp.isActive).ToList();
        }
        
        /// <summary>
        /// 플레이어 스폰 위치 가져오기
        /// </summary>
        public Vector3 GetPlayerSpawnPosition()
        {
            if (playerSpawnPoint != null)
            {
                return playerSpawnPoint.position;
            }
            
            Debug.LogWarning("⚠️ [SpawnPointManager] PlayerSpawnPoint가 설정되지 않음. 기본 위치 사용.");
            return Vector3.zero;
        }
        
        /// <summary>
        /// 특정 스폰 포인트에서 안전한 위치 가져오기
        /// </summary>
        public Vector3 GetSafeSpawnPosition(string spawnPointId)
        {
            SpawnPoint spawnPoint = GetSpawnPointById(spawnPointId);
            if (spawnPoint == null)
            {
                Debug.LogWarning($"⚠️ [SpawnPointManager] 스폰 포인트를 찾을 수 없음: {spawnPointId}");
                return Vector3.zero;
            }
            
            return spawnPoint.GetSafeSpawnPosition();
        }
        
        /// <summary>
        /// 랜덤 스폰 포인트에서 위치 가져오기
        /// </summary>
        public Vector3 GetRandomSpawnPosition(SpawnType? filterType = null)
        {
            List<SpawnPoint> availablePoints = GetActiveSpawnPoints();
            
            if (filterType.HasValue)
            {
                availablePoints = availablePoints.Where(sp => sp.spawnType == filterType.Value).ToList();
            }
            
            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("⚠️ [SpawnPointManager] 사용 가능한 스폰 포인트가 없음");
                return Vector3.zero;
            }
            
            SpawnPoint randomPoint = availablePoints[Random.Range(0, availablePoints.Count)];
            return randomPoint.GetSafeSpawnPosition();
        }
        
        /// <summary>
        /// 필요한 풀 사이즈 계산 (티켓 4 연동용)
        /// </summary>
        public Dictionary<string, int> CalculateRequiredPoolSizes()
        {
            var poolSizes = new Dictionary<string, int>();
            
            // 스폰 포인트별 예상 최대 동시 스폰 수 계산
            int maxSimultaneousSpawns = spawnPoints.Count * 2; // 여유분 포함
            
            // 기본 적 타입들의 예상 풀 사이즈
            poolSizes["BlueSlime"] = maxSimultaneousSpawns;
            poolSizes["Grape"] = maxSimultaneousSpawns;
            poolSizes["Ghost"] = maxSimultaneousSpawns;
            
            return poolSizes;
        }
        
        /// <summary>
        /// 디버그용 스폰 포인트 정보 출력
        /// </summary>
        [ContextMenu("Print Spawn Points Info")]
        public void PrintSpawnPointsInfo()
        {
            
            foreach (var sp in spawnPoints)
            {
            }
        }
        
        /// <summary>
        /// 에디터에서 기즈모 표시 제어
        /// </summary>
        private void OnValidate()
        {
            if (spawnPoints != null)
            {
                foreach (var spawnPoint in spawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        spawnPoint.showGizmos = showAllGizmos;
                    }
                }
            }
        }
    }
}
