using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지별 필요 풀 사이즈 계산기
    /// CSV 데이터 분석하여 최적 풀 사이즈 산출
    /// </summary>
    public static class StagePoolCalculator
    {
        /// <summary>
        /// 스테이지 풀 요구사항 데이터
        /// </summary>
        public class StagePoolRequirement
        {
            public string poolTag;
            public int baseCount;      // 기본 필요 수량
            public int maxSimultaneous; // 최대 동시 스폰 수
            public int recommendedSize; // 권장 풀 사이즈 (여유분 포함)
            public bool isBoss;        // 보스 몬스터 여부
            
            public override string ToString()
            {
                return $"{poolTag}: 기본{baseCount}, 동시{maxSimultaneous}, 권장{recommendedSize}" + 
                       (isBoss ? " (Boss)" : "");
            }
        }
        
        /// <summary>
        /// 스테이지별 풀 요구사항 계산
        /// </summary>
        public static List<StagePoolRequirement> CalculateStageRequirements(string stageId)
        {
            var requirements = new Dictionary<string, StagePoolRequirement>();
            
            
            // 스테이지 설정 로드
            StageConfig stageConfig = LoadStageConfig(stageId);
            if (stageConfig == null)
            {
                Debug.LogError($"[StagePoolCalculator] 스테이지 설정을 찾을 수 없음: {stageId}");
                return new List<StagePoolRequirement>();
            }
            
            
            // 모든 웨이브의 스폰 그룹 분석
            foreach (var waveConfig in stageConfig.WaveConfigs)
            {
                AnalyzeWaveRequirements(waveConfig, requirements);
            }
            
            
            // 최종 권장 사이즈 계산
            CalculateRecommendedSizes(requirements);
            
            var result = requirements.Values.ToList();
            
            foreach (var req in result)
            {
            }
            
            return result;
        }
        
        /// <summary>
        /// 스테이지 설정 로드
        /// </summary>
        private static StageConfig LoadStageConfig(string stageId)
        {
            string configPath = $"Stages/Configs/{stageId}_Config";
            return Resources.Load<StageConfig>(configPath);
        }
        
        /// <summary>
        /// 웨이브별 요구사항 분석
        /// </summary>
        private static void AnalyzeWaveRequirements(WaveConfig waveConfig, 
                                                   Dictionary<string, StagePoolRequirement> requirements)
        {
            foreach (var spawnGroup in waveConfig.SpawnGroups)
            {
                AnalyzeSpawnGroupRequirements(spawnGroup, requirements);
            }
        }
        
        /// <summary>
        /// 스폰 그룹별 요구사항 분석
        /// </summary>
        private static void AnalyzeSpawnGroupRequirements(SpawnGroup spawnGroup, 
                                                         Dictionary<string, StagePoolRequirement> requirements)
        {
            
            foreach (var monsterData in spawnGroup.Monsters)
            {
                
                string poolTag = MonsterIdMapper.GetPoolTag(monsterData.MonsterID);
                
                if (string.IsNullOrEmpty(poolTag))
                {
                    Debug.LogWarning($"⚠️ [StagePoolCalculator] MonsterID를 풀 태그로 변환 실패: {monsterData.MonsterID}");
                    continue;
                }
                
                
                if (!requirements.ContainsKey(poolTag))
                {
                    requirements[poolTag] = new StagePoolRequirement
                    {
                        poolTag = poolTag,
                        baseCount = 0,
                        maxSimultaneous = 0,
                        recommendedSize = 0,
                        isBoss = monsterData.IsBoss
                    };
                    
                }
                
                var requirement = requirements[poolTag];
                
                // 기본 필요 수량 누적
                int totalSpawns = CalculateTotalSpawns(spawnGroup, monsterData);
                requirement.baseCount += totalSpawns;
                
                // 최대 동시 스폰 수 계산
                int simultaneousSpawns = CalculateSimultaneousSpawns(spawnGroup, monsterData);
                requirement.maxSimultaneous = Mathf.Max(requirement.maxSimultaneous, simultaneousSpawns);
                
                // 보스 여부 업데이트
                if (monsterData.IsBoss)
                {
                    requirement.isBoss = true;
                }
                
            }
        }
        
        /// <summary>
        /// 총 스폰 수량 계산 (RepeatCount 포함)
        /// </summary>
        private static int CalculateTotalSpawns(SpawnGroup spawnGroup, MonsterSpawnData monsterData)
        {
            int baseSpawns = monsterData.Count;
            float repeatCount = spawnGroup.RepeatCount;
            
            // RepeatCount가 1 이하면 1회만 스폰
            if (repeatCount <= 1f)
            {
                return baseSpawns;
            }
            
            // RepeatCount 적용
            int totalSpawns = Mathf.RoundToInt(baseSpawns * repeatCount);
            return totalSpawns;
        }
        
        /// <summary>
        /// 최대 동시 스폰 수 계산
        /// </summary>
        private static int CalculateSimultaneousSpawns(SpawnGroup spawnGroup, MonsterSpawnData monsterData)
        {
            // 보스는 보통 1마리씩만 등장
            if (monsterData.IsBoss)
            {
                return 1;
            }
            
            // 일반 몬스터는 그룹 단위로 동시 스폰 가능
            int simultaneousSpawns = monsterData.Count;
            
            // RepeatInterval이 짧으면 동시에 더 많이 존재할 수 있음
            if (spawnGroup.RepeatIntervalSec < 5f && spawnGroup.RepeatCount > 1f)
            {
                simultaneousSpawns = Mathf.RoundToInt(simultaneousSpawns * 1.5f);
            }
            
            return simultaneousSpawns;
        }
        
        /// <summary>
        /// 최종 권장 풀 사이즈 계산
        /// </summary>
        private static void CalculateRecommendedSizes(Dictionary<string, StagePoolRequirement> requirements)
        {
            foreach (var requirement in requirements.Values)
            {
                if (requirement.isBoss)
                {
                    // 보스는 최소한의 풀 사이즈
                    requirement.recommendedSize = Mathf.Max(1, requirement.maxSimultaneous);
                }
                else
                {
                    // 일반 몬스터는 여유분 포함
                    int baseSize = Mathf.Max(requirement.baseCount, requirement.maxSimultaneous);
                    
                    // 여유분 계산 (50% ~ 100% 추가)
                    float bufferMultiplier = 1.5f;
                    if (baseSize > 10)
                    {
                        bufferMultiplier = 1.3f; // 수량이 많으면 여유분 비율 감소
                    }
                    
                    requirement.recommendedSize = Mathf.RoundToInt(baseSize * bufferMultiplier);
                    
                    // 최소/최대 제한
                    requirement.recommendedSize = Mathf.Clamp(requirement.recommendedSize, 2, 50);
                }
            }
        }
        
        /// <summary>
        /// 전체 풀 메모리 사용량 추정
        /// </summary>
        public static float EstimateMemoryUsage(List<StagePoolRequirement> requirements)
        {
            float totalMB = 0f;
            
            foreach (var req in requirements)
            {
                // 몬스터당 대략적인 메모리 사용량 (MB)
                float perMonsterMB = req.isBoss ? 2f : 0.5f;
                totalMB += req.recommendedSize * perMonsterMB;
            }
            
            return totalMB;
        }
        
        /// <summary>
        /// 스테이지 난이도 기반 풀 사이즈 조정
        /// </summary>
        public static void AdjustForDifficulty(List<StagePoolRequirement> requirements, string stageId)
        {
            // 스테이지 번호 추출 (STAGE_001 → 1)
            string stageNumber = stageId.Replace("STAGE_", "").Replace("_", "");
            if (int.TryParse(stageNumber, out int stageNum))
            {
                float difficultyMultiplier = 1f + (stageNum - 1) * 0.1f; // 스테이지마다 10% 증가
                difficultyMultiplier = Mathf.Clamp(difficultyMultiplier, 1f, 2f); // 최대 2배
                
                foreach (var req in requirements)
                {
                    if (!req.isBoss) // 보스는 제외
                    {
                        int adjustedSize = Mathf.RoundToInt(req.recommendedSize * difficultyMultiplier);
                        req.recommendedSize = Mathf.Clamp(adjustedSize, req.recommendedSize, 50);
                    }
                }
                
            }
        }
    }
}
