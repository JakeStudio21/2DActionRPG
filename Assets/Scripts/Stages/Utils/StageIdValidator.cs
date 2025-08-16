using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

public class StageIdValidator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

namespace StageSystem
{
    /// <summary>
    /// 스테이지 ID 규칙 검증 및 생성 유틸리티
    /// CSV 파일의 ID 무결성 검사용
    /// </summary>
    public static class StageIdValidator
    {
        // ID 패턴 정규식
        private static readonly Regex stageIdPattern = new Regex(@"^STAGE_\d{3}$");
        private static readonly Regex waveIdPattern = new Regex(@"^STAGE_\d{3}_WAVE_\d{2}$");
        private static readonly Regex groupIdPattern = new Regex(@"^STAGE_\d{3}_G\d{2}$");
        private static readonly Regex dropIdPattern = new Regex(@"^DROP_STG\d{3}_");
        
        /// <summary>
        /// 스테이지 ID 유효성 검사 (STAGE_001)
        /// </summary>
        public static bool IsValidStageId(string stageId)
        {
            return !string.IsNullOrEmpty(stageId) && stageIdPattern.IsMatch(stageId);
        }
        
        /// <summary>
        /// 웨이브 ID 유효성 검사 (STAGE_001_WAVE_01)
        /// </summary>
        public static bool IsValidWaveId(string waveId)
        {
            return !string.IsNullOrEmpty(waveId) && waveIdPattern.IsMatch(waveId);
        }
        
        /// <summary>
        /// 스폰 그룹 ID 유효성 검사 (STAGE_001_G01)
        /// </summary>
        public static bool IsValidGroupId(string groupId)
        {
            return !string.IsNullOrEmpty(groupId) && groupIdPattern.IsMatch(groupId);
        }
        
        /// <summary>
        /// 드롭 ID 유효성 검사 (DROP_STG001_CLEAR_FIRST)
        /// </summary>
        public static bool IsValidDropId(string dropId)
        {
            return !string.IsNullOrEmpty(dropId) && dropIdPattern.IsMatch(dropId);
        }
        
        /// <summary>
        /// 웨이브 ID에서 스테이지 ID 추출
        /// </summary>
        public static string ExtractStageIdFromWave(string waveId)
        {
            if (!IsValidWaveId(waveId)) return null;
            
            int waveIndex = waveId.IndexOf("_WAVE_");
            return waveIndex > 0 ? waveId.Substring(0, waveIndex) : null;
        }
        
        /// <summary>
        /// 그룹 ID에서 스테이지 ID 추출
        /// </summary>
        public static string ExtractStageIdFromGroup(string groupId)
        {
            if (!IsValidGroupId(groupId)) return null;
            
            int groupIndex = groupId.IndexOf("_G");
            return groupIndex > 0 ? groupId.Substring(0, groupIndex) : null;
        }
        
        /// <summary>
        /// CSV 데이터 참조 무결성 검증
        /// </summary>
        public static ValidationResult ValidateReferences(
            string[] stageIds, 
            string[] waveIds, 
            string[] groupIds, 
            string[] dropIds)
        {
            var result = new ValidationResult();
            
            // 웨이브 → 스테이지 참조 검증
            foreach (string waveId in waveIds)
            {
                if (string.IsNullOrEmpty(waveId)) continue;
                
                string stageId = ExtractStageIdFromWave(waveId);
                if (stageId != null && System.Array.IndexOf(stageIds, stageId) == -1)
                {
                    result.AddError($"웨이브 '{waveId}'가 참조하는 스테이지 '{stageId}'가 존재하지 않습니다.");
                }
            }
            
            // 그룹 → 스테이지 참조 검증 (간접)
            foreach (string groupId in groupIds)
            {
                if (string.IsNullOrEmpty(groupId)) continue;
                
                string stageId = ExtractStageIdFromGroup(groupId);
                if (stageId != null && System.Array.IndexOf(stageIds, stageId) == -1)
                {
                    result.AddError($"그룹 '{groupId}'가 참조하는 스테이지 '{stageId}'가 존재하지 않습니다.");
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 검증 결과 클래스
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid => errors.Count == 0;
            private System.Collections.Generic.List<string> errors = new System.Collections.Generic.List<string>();
            
            public void AddError(string error)
            {
                errors.Add(error);
            }
            
            public string[] GetErrors()
            {
                return errors.ToArray();
            }
            
            public void LogResults()
            {
                if (IsValid)
                {
                    Debug.Log("✅ [StageIdValidator] 모든 ID 검증 통과");
                }
                else
                {
                    Debug.LogError($"❌ [StageIdValidator] {errors.Count}개 오류 발견:");
                    foreach (string error in errors)
                    {
                        Debug.LogError($"  • {error}");
                    }
                }
            }
        }
    }
}
