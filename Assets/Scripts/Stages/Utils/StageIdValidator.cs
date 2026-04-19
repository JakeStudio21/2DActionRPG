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
    // ✅ Phase 0: 챕터 스테이지 패턴 추가 (CH01_ST01 ~ CH05_ST10)
    private static readonly Regex stageIdPattern = new Regex(@"^(STAGE_\d{3}|CH\d{2}_ST\d{2})$");
    private static readonly Regex waveIdPattern = new Regex(@"^(STAGE_\d{3}_WAVE_\d{2}|CH\d{2}_ST\d{2}_WAVE_\d{2})$");
    private static readonly Regex groupIdPattern = new Regex(@"^(STAGE_\d{3}_G\d{2}|CH\d{2}_ST\d{2}_G\d{2})$");
    private static readonly Regex dropIdPattern = new Regex(@"^DROP_(STG\d{3}|CH\d{2}_ST\d{2})_CLEAR_");
        
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
        /// ✅ Phase 0: 챕터 스테이지 ID 유효성 검사 (CH01_ST01)
        /// </summary>
        public static bool IsValidChapterStageId(string stageId)
        {
            return !string.IsNullOrEmpty(stageId) && 
                   Regex.IsMatch(stageId, @"^CH\d{2}_ST\d{2}$");
        }
        
        /// <summary>
        /// ✅ Phase 0: 챕터 ID 추출 (CH01_ST05 → 1)
        /// </summary>
        public static int ExtractChapterId(string stageId)
        {
            if (!IsValidChapterStageId(stageId)) return -1;
            
            string chapterPart = stageId.Substring(2, 2); // "CH01" → "01"
            if (int.TryParse(chapterPart, out int chapterId))
            {
                return chapterId;
            }
            return -1;
        }
        
        /// <summary>
        /// ✅ Phase 0: 스테이지 번호 추출 (CH01_ST05 → 5)
        /// </summary>
        public static int ExtractStageIndex(string stageId)
        {
            if (!IsValidChapterStageId(stageId)) return -1;
            
            string stagePart = stageId.Substring(7, 2); // "CH01_ST05" → "05"
            if (int.TryParse(stagePart, out int stageIndex))
            {
                return stageIndex;
            }
            return -1;
        }
        
        /// <summary>
        /// ✅ Phase 0: WaveID에서 챕터 ID 추출 (CH01_ST01_WAVE_01 → 1)
        /// </summary>
        public static int ExtractChapterIdFromWaveId(string waveId)
        {
            if (string.IsNullOrEmpty(waveId)) return -1;
            
            // CH01_ST01_WAVE_01 형식인지 확인
            if (waveId.StartsWith("CH") && waveId.Contains("_ST"))
            {
                string stageId = ExtractStageIdFromWave(waveId); // CH01_ST01
                return ExtractChapterId(stageId); // 1
            }
            return -1;
        }
        
        /// <summary>
        /// ✅ Phase 0: GroupID에서 챕터 ID 추출 (CH01_ST01_G01 → 1)
        /// </summary>
        public static int ExtractChapterIdFromGroupId(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return -1;
            
            // CH01_ST01_G01 형식인지 확인
            if (groupId.StartsWith("CH") && groupId.Contains("_ST"))
            {
                string stageId = ExtractStageIdFromGroup(groupId); // CH01_ST01
                return ExtractChapterId(stageId); // 1
            }
            return -1;
        }
        
        // ========================================
        // 🏰 Phase 1: 던전 ID 검증 시스템
        // ========================================
        
        /// <summary>
        /// 던전 ID 유효성 검사 (DG01, DG01_SB01, DG_DAILY_FOREST_BIND 등)
        /// </summary>
        public static bool IsValidDungeonId(string dungeonId)
        {
            if (string.IsNullOrEmpty(dungeonId)) return false;
            if (!dungeonId.StartsWith("DG")) return false;
            
            // 최소 길이 체크 (DG01 = 4자)
            if (dungeonId.Length < 4) return false;
            
            // DG01, DG02 형식 (간단한 번호)
            if (Regex.IsMatch(dungeonId, @"^DG\d{2}$"))
            {
                return true;
            }
            
            // DG01_SB01 또는 DG01_SB01_BIND 형식 (던전번호_서브타입번호_속성) ⭐ 확장
            // 예: DG01_SB01, DG01_SB01_BIND, DG01_SB02_POISON, DG01_SB03_SLOW, DG01_SB04_BURN
            if (Regex.IsMatch(dungeonId, @"^DG\d{2}_[A-Z]{2}\d{2}(_[A-Z]+)?$"))
            {
                return true;
            }
            
            // DG_DAILY_FOREST_BIND 형식 (의미있는 이름)
            if (Regex.IsMatch(dungeonId, @"^DG_[A-Z_]+$"))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 던전인지 확인 (ID 앞부분이 DG로 시작)
        /// </summary>
        public static bool IsDungeon(string stageId)
        {
            return !string.IsNullOrEmpty(stageId) && stageId.StartsWith("DG");
        }
        
        /// <summary>
        /// 던전 번호 추출 (DG01 → 1, DG99 → 99)
        /// </summary>
        public static int ExtractDungeonNumber(string dungeonId)
        {
            if (!IsValidDungeonId(dungeonId)) return -1;
            
            // DG01 형식인 경우
            if (Regex.IsMatch(dungeonId, @"^DG\d{2}$"))
            {
                string numberPart = dungeonId.Substring(2, 2);
                if (int.TryParse(numberPart, out int number))
                {
                    return number;
                }
            }
            
            return -1;
        }
        
        /// <summary>
        /// 던전 카테고리 추출 (DG_DAILY_FOREST_BIND → "DAILY")
        /// </summary>
        public static string ExtractDungeonCategory(string dungeonId)
        {
            if (!IsValidDungeonId(dungeonId)) return null;
            
            // DG_DAILY_FOREST_BIND 형식인 경우
            if (dungeonId.StartsWith("DG_"))
            {
                string[] parts = dungeonId.Split('_');
                if (parts.Length >= 2)
                {
                    return parts[1]; // "DAILY"
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 던전 Wave ID 유효성 검사 (DG01_WAVE_01)
        /// </summary>
        public static bool IsValidDungeonWaveId(string waveId)
        {
            if (string.IsNullOrEmpty(waveId)) return false;
            if (!waveId.StartsWith("DG")) return false;
            
            // DG01_WAVE_01 형식
            if (Regex.IsMatch(waveId, @"^DG\d{2}_WAVE_\d{2}$"))
            {
                return true;
            }
            
            // DG_DAILY_FOREST_BIND_WAVE_01 형식
            if (Regex.IsMatch(waveId, @"^DG_[A-Z_]+_WAVE_\d{2}$"))
            {
                return true;
            }
            
            return false;
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
