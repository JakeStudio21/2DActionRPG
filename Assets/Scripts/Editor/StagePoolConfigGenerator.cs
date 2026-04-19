using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace StageSystem
{
    /// <summary>
    /// CSV 데이터 기반 스테이지별 ScenePoolConfig 자동 생성기
    /// 자동 생성 우선, 실패 시 수동 매핑 fallback
    /// </summary>
    public static class StagePoolConfigGenerator
    {
        private const string SCENE_POOLS_PATH = "Assets/Resources/Stages/ScenePools/";
        
        /// <summary>
        /// 모든 스테이지의 풀 설정 자동 생성
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Generate All Stage Pool Configs")]
        public static void GenerateAllStagePoolConfigs()
        {
            // Resources/Stages/Configs에서 모든 스테이지 찾기
            StageConfig[] allStageConfigs = Resources.LoadAll<StageConfig>("Stages/Configs");
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var stageConfig in allStageConfigs)
            {
                try
                {
                    bool success = GenerateStagePoolConfig(stageConfig.StageID);
                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        Debug.LogWarning($"⚠️ [StagePoolConfigGenerator] {stageConfig.StageID} 생성 실패");
                    }
                }
                catch (System.Exception ex)
                {
                    failCount++;
                    Debug.LogError($"❌ [StagePoolConfigGenerator] {stageConfig.StageID} 생성 오류: {ex.Message}");
                }
            }
            // 에셋 데이터베이스 새로고침
            UnityEditor.AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// 특정 스테이지의 풀 설정 생성
        /// </summary>
        public static bool GenerateStagePoolConfig(string stageId)
        {
            try
            {
                // 1단계: 풀 요구사항 계산
                var requirements = StagePoolCalculator.CalculateStageRequirements(stageId);
                if (requirements.Count == 0)
                {
                    Debug.LogWarning($"[StagePoolConfigGenerator] {stageId}: 풀 요구사항이 없음");
                    return false;
                }
                
                // 2단계: 난이도 기반 조정
                StagePoolCalculator.AdjustForDifficulty(requirements, stageId);
                
                // 3단계: ScenePoolConfig 생성
                ScenePoolConfig poolConfig = CreateScenePoolConfig(stageId, requirements);
                
                // 4단계: 에셋 파일로 저장
                string assetPath = $"{SCENE_POOLS_PATH}{stageId}_PoolConfig.asset";
                SavePoolConfigAsset(poolConfig, assetPath);
                
                // 5단계: 메모리 사용량 체크
                float memoryMB = StagePoolCalculator.EstimateMemoryUsage(requirements);
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[StagePoolConfigGenerator] {stageId} 생성 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// ScenePoolConfig 객체 생성
        /// </summary>
        private static ScenePoolConfig CreateScenePoolConfig(string stageId, 
                                                            List<StagePoolCalculator.StagePoolRequirement> requirements)
        {
            var poolConfig = ScriptableObject.CreateInstance<ScenePoolConfig>();
            // poolConfig.sceneName = $"Stage_{stageId}";
            poolConfig.sceneName = stageId.Replace("STAGE_", "Stage_");
            poolConfig.requiredPools = new List<ScenePoolConfig.PoolSettings>();
            
            // 각 요구사항을 PoolSettings으로 변환
            foreach (var requirement in requirements)
            {
                var poolSetting = CreatePoolSetting(requirement);
                if (poolSetting != null)
                {
                    poolConfig.requiredPools.Add(poolSetting);
                }
            }
            
            // 기본 발사체 풀들 추가 (모든 스테이지 공통)
            AddCommonProjectilePools(poolConfig);
            
            // 기본 이펙트 풀들 추가
            AddCommonEffectPools(poolConfig);
            
            return poolConfig;
        }
        
        /// <summary>
        /// 개별 풀 설정 생성
        /// </summary>
        private static ScenePoolConfig.PoolSettings CreatePoolSetting(StagePoolCalculator.StagePoolRequirement requirement)
        {
            // 프리팹 로드 시도
            string prefabPath = MonsterIdMapper.GetPrefabPath(requirement.poolTag);
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            
            if (prefab == null)
            {
                // 다른 경로들 시도
                string[] alternatePaths = {
                    $"Prefabs/{prefabPath}",
                    $"Enemies/{prefabPath}",
                    $"Prefabs/Enemies/{prefabPath}"
                };
                
                foreach (string altPath in alternatePaths)
                {
                    prefab = Resources.Load<GameObject>(altPath);
                    if (prefab != null)
                    {
                        break;
                    }
                }
            }
            
            if (prefab == null)
            {
                Debug.LogWarning($"[StagePoolConfigGenerator] 프리팹을 찾을 수 없음: {requirement.poolTag} (경로: {prefabPath})");
                return null;
            }
            
            var poolSetting = new ScenePoolConfig.PoolSettings
            {
                tag = requirement.poolTag,
                prefab = prefab,
                size = requirement.recommendedSize,
                preloadOnSceneStart = true,
                clearOnSceneExit = false,
                maxInstancesPerFrame = requirement.isBoss ? 1 : 5
            };
            return poolSetting;
        }
        
        /// <summary>
        /// 공통 발사체 풀 추가
        /// </summary>
        private static void AddCommonProjectilePools(ScenePoolConfig poolConfig)
        {
            var commonProjectiles = new Dictionary<string, int>
            {
                { "Arrow", 15 },
                { "Ghost Bullet", 20 },
                { "Grape Projectile", 10 },
                { "Grape Projectile Splatter", 10 },
                { "GrapeShadow", 10 }
            };
            
            foreach (var projectile in commonProjectiles)
            {
                var poolSetting = CreateCommonPoolSetting(projectile.Key, projectile.Value);
                if (poolSetting != null)
                {
                    poolConfig.requiredPools.Add(poolSetting);
                }
            }
        }
        
        /// <summary>
        /// 공통 이펙트 풀 추가
        /// </summary>
        private static void AddCommonEffectPools(ScenePoolConfig poolConfig)
        {
            var commonEffects = new Dictionary<string, int>
            {
                { "Death VFX", 10 },
                { "Hit Effect", 15 },
                { "Explosion Effect", 5 }
            };
            
            foreach (var effect in commonEffects)
            {
                var poolSetting = CreateCommonPoolSetting(effect.Key, effect.Value);
                if (poolSetting != null)
                {
                    poolConfig.requiredPools.Add(poolSetting);
                }
            }
        }
        
        /// <summary>
        /// 공통 풀 설정 생성 헬퍼
        /// </summary>
        private static ScenePoolConfig.PoolSettings CreateCommonPoolSetting(string tag, int size)
        {
            GameObject prefab = Resources.Load<GameObject>(tag);
            if (prefab == null)
            {
                // 대체 경로들 시도
                string[] alternatePaths = {
                    $"Prefabs/{tag}",
                    $"Effects/{tag}",
                    $"Projectiles/{tag}"
                };
                
                foreach (string altPath in alternatePaths)
                {
                    prefab = Resources.Load<GameObject>(altPath);
                    if (prefab != null) break;
                }
            }
            
            if (prefab == null)
            {
                Debug.LogWarning($"[StagePoolConfigGenerator] 공통 프리팹을 찾을 수 없음: {tag}");
                return null;
            }
            
            return new ScenePoolConfig.PoolSettings
            {
                tag = tag,
                prefab = prefab,
                size = size,
                preloadOnSceneStart = true,
                clearOnSceneExit = false,
                maxInstancesPerFrame = 10
            };
        }
        
        /// <summary>
        /// 풀 설정을 에셋 파일로 저장
        /// </summary>
        private static void SavePoolConfigAsset(ScenePoolConfig poolConfig, string assetPath)
        {
            // 디렉토리 생성
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 기존 에셋이 있으면 업데이트, 없으면 새로 생성
            ScenePoolConfig existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<ScenePoolConfig>(assetPath);
            if (existingAsset != null)
            {
                // 기존 에셋 업데이트
                existingAsset.requiredPools = poolConfig.requiredPools;
                existingAsset.sceneName = poolConfig.sceneName;
                UnityEditor.EditorUtility.SetDirty(existingAsset);
            }
            else
            {
                // 새 에셋 생성
                UnityEditor.AssetDatabase.CreateAsset(poolConfig, assetPath);
            }
            
            UnityEditor.AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// 특정 스테이지 풀 설정 삭제
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Clear All Stage Pool Configs")]
        public static void ClearAllStagePoolConfigs()
        {
            string[] assetPaths = Directory.GetFiles(SCENE_POOLS_PATH, "*_PoolConfig.asset");
            
            foreach (string assetPath in assetPaths)
            {
                UnityEditor.AssetDatabase.DeleteAsset(assetPath);
            }
            
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
