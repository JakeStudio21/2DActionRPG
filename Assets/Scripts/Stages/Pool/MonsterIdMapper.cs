using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// MonsterID → 풀 태그 매핑 시스템
    /// 자동 매핑 우선, 실패 시 수동 매핑 fallback
    /// </summary>
    public static class MonsterIdMapper
    {
        // 수동 매핑 테이블 (자동 매핑 실패 시 사용)
        private static readonly Dictionary<string, string> manualMappings = new Dictionary<string, string>
        {
            // 기본 몬스터 수동 매핑
            { "MON_BLUESLIME_001", "Blue_slime" },
            { "MON_GRAPE_001", "Enemie1" },
            { "MON_GHOST_001", "Ghost" },
            { "MON_RABBIT_001", "Rabbit" },
            
            // ⭐ 보스 몬스터 매핑 추가
            { "MON_BLUESLIME_001_BOSS", "Blue_slime" }, // 같은 프리팹, 다른 데이터
            { "MON_FINALBOSSA_001", "FinalBossA" },
            { "MON_FINALBOSSB_001", "FinalBossB" },
            { "MON_FINALBOSSC_001", "FinalBossC" }
        };
        
        // 프리팹 경로 매핑 (Resources.Load용)
        private static readonly Dictionary<string, string> prefabPaths = new Dictionary<string, string>
        {
            { "Blue_slime", "Blue_slime" },
            { "Enemie1", "Enemie1" }, 
            { "Ghost", "Ghost" },
            { "Rabbit", "Rabbit" },
            { "FinalBossA", "FinalBossA" },
            { "FinalBossB", "FinalBossB" },
            { "FinalBossC", "FinalBossC" }
        };
        
        /// <summary>
        /// MonsterID를 풀 태그로 변환 (자동 + 수동 매핑)
        /// </summary>
        public static string GetPoolTag(string monsterId)
        {
            if (string.IsNullOrEmpty(monsterId))
            {
                Debug.LogWarning("[MonsterIdMapper] 빈 MonsterID");
                return null;
            }
            
            // 1순위: 자동 매핑 시도
            string autoTag = TryAutoMapping(monsterId);
            if (!string.IsNullOrEmpty(autoTag))
            {
                return autoTag;
            }
            
            // 2순위: 수동 매핑 테이블 확인
            if (manualMappings.TryGetValue(monsterId, out string manualTag))
            {
                Debug.Log($"[MonsterIdMapper] 수동 매핑 사용: {monsterId} → {manualTag}");
                return manualTag;
            }
            
            // 3순위: 기본값 반환
            Debug.LogWarning($"[MonsterIdMapper] 매핑 실패, 기본값 사용: {monsterId} → Blue_slime");
            return "Blue_slime";
        }
        
        /// <summary>
        /// 자동 매핑 로직 (MON_BLUESLIME_001 → Blue_slime)
        /// </summary>
        private static string TryAutoMapping(string monsterId)
        {
            try
            {
                // MON_BLUESLIME_001 형식 파싱
                if (!monsterId.StartsWith("MON_"))
                    return null;
                
                // MON_ 제거 후 처리
                string withoutPrefix = monsterId.Substring(4);
                
                // 마지막 _숫자 부분 제거 (예: _001)
                int lastUnderscoreIndex = withoutPrefix.LastIndexOf('_');
                if (lastUnderscoreIndex > 0)
                {
                    string monsterType = withoutPrefix.Substring(0, lastUnderscoreIndex);
                    
                    // 자동 변환 규칙 적용
                    string poolTag = ConvertToPoolTag(monsterType);
                    
                    // 변환 결과 검증
                    if (IsValidPoolTag(poolTag))
                    {
                        Debug.Log($"[MonsterIdMapper] 자동 매핑 성공: {monsterId} → {poolTag}");
                        return poolTag;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MonsterIdMapper] 자동 매핑 오류: {monsterId} - {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 몬스터 타입을 풀 태그로 변환
        /// </summary>
        private static string ConvertToPoolTag(string monsterType)
        {
            switch (monsterType.ToUpper())
            {
                case "BLUESLIME":
                    return "Blue_slime";
                case "GRAPE":
                    return "Enemie1";
                case "GHOST":
                    return "Ghost";
                case "RABBIT":
                    return "Rabbit";
                case "FINALBOSSA":
                    return "FinalBossA";
                case "FINALBOSSB":
                    return "FinalBossB";
                case "FINALBOSSC":
                    return "FinalBossC";
                default:
                    return null;
            }
        }
        
        /// <summary>
        /// 유효한 풀 태그인지 검증
        /// </summary>
        private static bool IsValidPoolTag(string poolTag)
        {
            if (string.IsNullOrEmpty(poolTag))
                return false;
            
            // GamePoolManager의 기존 풀 태그와 비교하거나
            // Resources 폴더에서 프리팹 존재 여부 확인
            return prefabPaths.ContainsKey(poolTag);
        }
        
        /// <summary>
        /// 풀 태그에 해당하는 프리팹 경로 가져오기
        /// </summary>
        public static string GetPrefabPath(string poolTag)
        {
            if (prefabPaths.TryGetValue(poolTag, out string path))
            {
                return path;
            }
            
            Debug.LogWarning($"[MonsterIdMapper] 프리팹 경로를 찾을 수 없음: {poolTag}");
            return poolTag; // 기본값으로 태그명 자체 반환
        }
        
        /// <summary>
        /// 새로운 수동 매핑 추가
        /// </summary>
        public static void AddManualMapping(string monsterId, string poolTag, string prefabPath = null)
        {
            manualMappings[monsterId] = poolTag;
            
            if (!string.IsNullOrEmpty(prefabPath))
            {
                prefabPaths[poolTag] = prefabPath;
            }
            
            Debug.Log($"[MonsterIdMapper] 새 수동 매핑 추가: {monsterId} → {poolTag}");
        }
        
        /// <summary>
        /// 모든 매핑 정보 출력 (디버그용)
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        public static void PrintAllMappings()
        {
            Debug.Log("🗺️ [MonsterIdMapper] 현재 매핑 테이블:");
            foreach (var mapping in manualMappings)
            {
                Debug.Log($"  {mapping.Key} → {mapping.Value}");
            }
        }
    }
}