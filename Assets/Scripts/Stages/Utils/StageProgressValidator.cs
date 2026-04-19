using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 진행도 데이터 검증 유틸리티
    /// </summary>
    public static class StageProgressValidator
    {
        /// <summary>
        /// 진행도 데이터 무결성 검사
        /// </summary>
        public static ValidationResult ValidateProgresses(List<StageProgress> progresses, List<StageConfig> configs)
        {
            var result = new ValidationResult();
            
            // 중복 스테이지 ID 검사
            var duplicateIds = progresses.GroupBy(p => p.stageId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            
            foreach (var duplicateId in duplicateIds)
            {
                result.AddError($"중복된 스테이지 진행도: {duplicateId}");
            }
            
            // 존재하지 않는 스테이지 ID 검사
            var configIds = configs.Select(c => c.StageID).ToHashSet();
            foreach (var progress in progresses)
            {
                if (!configIds.Contains(progress.stageId))
                {
                    result.AddWarning($"존재하지 않는 스테이지 ID: {progress.stageId}");
                }
            }
            
            // 순환 참조 검사
            foreach (var config in configs)
            {
                if (HasCircularDependency(config, configs))
                {
                    result.AddError($"순환 참조 발견: {config.StageID}");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 순환 참조 검사
        /// </summary>
        private static bool HasCircularDependency(StageConfig config, List<StageConfig> allConfigs, HashSet<string> visited = null)
        {
            if (visited == null) visited = new HashSet<string>();
            
            if (visited.Contains(config.StageID))
                return true;
            
            visited.Add(config.StageID);
            
            var unlockData = config.UnlockData;
            if (!unlockData.IsAlwaysUnlocked && !string.IsNullOrEmpty(unlockData.RequiredStageId))
            {
                var requiredConfig = allConfigs.Find(c => c.StageID == unlockData.RequiredStageId);
                if (requiredConfig != null)
                {
                    return HasCircularDependency(requiredConfig, allConfigs, new HashSet<string>(visited));
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 검증 결과 클래스
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid => errors.Count == 0;
            public bool HasWarnings => warnings.Count > 0;
            
            private List<string> errors = new List<string>();
            private List<string> warnings = new List<string>();
            
            public void AddError(string error) => errors.Add(error);
            public void AddWarning(string warning) => warnings.Add(warning);
            
            public string[] GetErrors() => errors.ToArray();
            public string[] GetWarnings() => warnings.ToArray();
            
            public void LogResults()
            {
                if (IsValid && !HasWarnings)
                {
                }
                else
                {
                    if (errors.Count > 0)
                    {
                        Debug.LogError($"❌ [StageProgressValidator] {errors.Count}개 오류:");
                        foreach (var error in errors)
                            Debug.LogError($"  • {error}");
                    }
                    
                    if (warnings.Count > 0)
                    {
                        Debug.LogWarning($"⚠️ [StageProgressValidator] {warnings.Count}개 경고:");
                        foreach (var warning in warnings)
                            Debug.LogWarning($"  • {warning}");
                    }
                }
            }
        }
    }
}
