using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class CueDataValidator : MonoBehaviour
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

namespace CueSystem.Editor
{
    /// <summary>
    /// 🔍 Cue Data Validator (CSV 데이터 유효성 검사 엔진)
    /// CSV 구조, 데이터 타입, 참조 무결성 등 종합 검증
    /// </summary>
    public static class CueDataValidator
    {
        // 필수 컬럼 정의
        private static readonly string[] REQUIRED_META_COLUMNS = { "domain", "profile_id", "base_profile_id", "description" };
        private static readonly string[] REQUIRED_ENTRY_COLUMNS = { "domain", "profile_id", "event_key", "vfx_ids", "sfx_ids" };
        private static readonly string[] REQUIRED_VFX_COLUMNS = { "vfx_id", "pool_key", "duration", "scale" };
        private static readonly string[] REQUIRED_SFX_COLUMNS = { "sfx_id", "audio_clip_path", "volume", "pitch" };
        
        // 유효한 도메인 목록
        private static readonly string[] VALID_DOMAINS = { "Player", "Enemy", "Stage", "UI", "Global" };
        
        // 이벤트 키 네이밍 규칙 (소문자, 점, 언더스코어만 허용)
        private static readonly Regex EVENT_KEY_PATTERN = new Regex(@"^[a-z0-9_.]+$");
        
        /// <summary>
        /// 📋 검증 결과 클래스
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid => Errors.Count == 0;
            public List<ValidationError> Errors = new List<ValidationError>();
            public List<ValidationWarning> Warnings = new List<ValidationWarning>();
            
            public void AddError(int line, string column, string message, string suggestion = null)
            {
                Errors.Add(new ValidationError { Line = line, Column = column, Message = message, Suggestion = suggestion });
            }
            
            public void AddWarning(int line, string column, string message, string suggestion = null)
            {
                Warnings.Add(new ValidationWarning { Line = line, Column = column, Message = message, Suggestion = suggestion });
            }
            
            public string GetSummary()
            {
                return $"검증 완료: 에러 {Errors.Count}개, 경고 {Warnings.Count}개";
            }
        }
        
        public class ValidationError
        {
            public int Line;
            public string Column;
            public string Message;
            public string Suggestion;
            
            public override string ToString()
            {
                string result = $"라인 {Line}, 컬럼 '{Column}': {Message}";
                if (!string.IsNullOrEmpty(Suggestion))
                    result += $" (제안: {Suggestion})";
                return result;
            }
        }
        
        public class ValidationWarning
        {
            public int Line;
            public string Column;
            public string Message;
            public string Suggestion;
            
            public override string ToString()
            {
                string result = $"라인 {Line}, 컬럼 '{Column}': {Message}";
                if (!string.IsNullOrEmpty(Suggestion))
                    result += $" (제안: {Suggestion})";
                return result;
            }
        }
        
        /// <summary>
        /// 🔍 메타 CSV 검증
        /// </summary>
        public static ValidationResult ValidateMetaCSV(List<Dictionary<string, string>> data, string filePath)
        {
            var result = new ValidationResult();
            
            if (data == null || data.Count == 0)
            {
                result.AddError(0, "", "CSV 파일이 비어있거나 읽을 수 없습니다.");
                return result;
            }
            
            // 헤더 검증
            var headers = data[0].Keys.ToList();
            
            foreach (var requiredColumn in REQUIRED_META_COLUMNS)
            {
                if (!headers.Contains(requiredColumn))
                {
                    result.AddError(1, requiredColumn, $"필수 컬럼 '{requiredColumn}'이 없습니다.");
                }
            }
            
            // 데이터 행 검증
            for (int i = 0; i < data.Count; i++)
            {
                int lineNumber = i + 2; // CSV 라인 번호 (헤더 + 1)
                var row = data[i];
                
                // 도메인 검증
                if (row.ContainsKey("domain"))
                {
                    string domain = row["domain"].Trim();
                    if (string.IsNullOrEmpty(domain))
                    {
                        result.AddError(lineNumber, "domain", "도메인이 비어있습니다.");
                    }
                    else if (!VALID_DOMAINS.Contains(domain))
                    {
                        result.AddError(lineNumber, "domain", $"유효하지 않은 도메인: {domain}", 
                            $"유효한 도메인: {string.Join(", ", VALID_DOMAINS)}");
                    }
                }
                
                // 프로필 ID 검증
                if (row.ContainsKey("profile_id"))
                {
                    string profileId = row["profile_id"].Trim();
                    if (string.IsNullOrEmpty(profileId))
                    {
                        result.AddError(lineNumber, "profile_id", "프로필 ID가 비어있습니다.");
                    }
                    else if (!IsValidIdentifier(profileId))
                    {
                        result.AddError(lineNumber, "profile_id", "프로필 ID는 영문자, 숫자, 언더스코어만 사용 가능합니다.", 
                            $"예: {profileId.ToLower().Replace(" ", "_")}");
                    }
                }
            }
            
            // 중복 프로필 ID 검증
            ValidateDuplicateProfileIds(data, result);
            
            return result;
        }
        
        /// <summary>
        /// 🔍 엔트리 CSV 검증
        /// </summary>
        public static ValidationResult ValidateEntriesCSV(List<Dictionary<string, string>> data, string filePath)
        {
            var result = new ValidationResult();
            
            if (data == null || data.Count == 0)
            {
                result.AddError(0, "", "CSV 파일이 비어있거나 읽을 수 없습니다.");
                return result;
            }
            
            // 헤더 검증
            var headers = data[0].Keys.ToList();
            
            foreach (var requiredColumn in REQUIRED_ENTRY_COLUMNS)
            {
                if (!headers.Contains(requiredColumn))
                {
                    result.AddError(1, requiredColumn, $"필수 컬럼 '{requiredColumn}'이 없습니다.");
                }
            }
            
            // 데이터 행 검증
            for (int i = 0; i < data.Count; i++)
            {
                int lineNumber = i + 2;
                var row = data[i];
                
                // 이벤트 키 검증
                if (row.ContainsKey("event_key"))
                {
                    string eventKey = row["event_key"].Trim();
                    if (string.IsNullOrEmpty(eventKey))
                    {
                        result.AddError(lineNumber, "event_key", "이벤트 키가 비어있습니다.");
                    }
                    else if (!EVENT_KEY_PATTERN.IsMatch(eventKey))
                    {
                        result.AddError(lineNumber, "event_key", "이벤트 키는 소문자, 숫자, 점, 언더스코어만 사용 가능합니다.", 
                            $"예: {eventKey.ToLower().Replace(" ", "_")}");
                    }
                }
                
                // Priority 검증
                if (row.ContainsKey("priority") && !string.IsNullOrEmpty(row["priority"]))
                {
                    if (!int.TryParse(row["priority"], out int priority) || priority < 0 || priority > 100)
                    {
                        result.AddError(lineNumber, "priority", "우선순위는 0~100 사이의 정수여야 합니다.");
                    }
                }
                
                // Time Stop 검증
                if (row.ContainsKey("time_stop_ms") && !string.IsNullOrEmpty(row["time_stop_ms"]))
                {
                    if (!int.TryParse(row["time_stop_ms"], out int timeStop) || timeStop < 0)
                    {
                        result.AddError(lineNumber, "time_stop_ms", "히트스톱 시간은 0 이상의 정수여야 합니다.");
                    }
                }
            }
            
            // 중복 키 검증
            ValidateDuplicateEventKeys(data, result);
            
            return result;
        }
        
        /// <summary>
        /// 🔍 VFX 카탈로그 검증
        /// </summary>
        public static ValidationResult ValidateVFXCatalog(List<Dictionary<string, string>> data, string filePath)
        {
            var result = new ValidationResult();
            
            if (data == null || data.Count == 0)
            {
                result.AddWarning(0, "", "VFX 카탈로그가 비어있습니다.");
                return result;
            }
            
            // 헤더 검증
            var headers = data[0].Keys.ToList();
            foreach (var requiredColumn in REQUIRED_VFX_COLUMNS)
            {
                if (!headers.Contains(requiredColumn))
                {
                    result.AddError(1, requiredColumn, $"필수 컬럼 '{requiredColumn}'이 없습니다.");
                }
            }
            
            // 데이터 행 검증
            for (int i = 0; i < data.Count; i++)
            {
                int lineNumber = i + 2;
                var row = data[i];
                
                // Duration 검증
                if (row.ContainsKey("duration") && !string.IsNullOrEmpty(row["duration"]))
                {
                    if (!float.TryParse(row["duration"], out float duration) || duration < -1)
                    {
                        result.AddError(lineNumber, "duration", "지속시간은 -1 이상의 실수여야 합니다. (-1은 자동감지)");
                    }
                }
                
                // Scale 검증
                if (row.ContainsKey("scale") && !string.IsNullOrEmpty(row["scale"]))
                {
                    if (!float.TryParse(row["scale"], out float scale) || scale <= 0)
                    {
                        result.AddError(lineNumber, "scale", "스케일은 0보다 큰 실수여야 합니다.");
                    }
                }
                
                // Cooldown 검증
                if (row.ContainsKey("cooldown") && !string.IsNullOrEmpty(row["cooldown"]))
                {
                    if (!float.TryParse(row["cooldown"], out float cooldown) || cooldown < 0)
                    {
                        result.AddError(lineNumber, "cooldown", "쿨다운은 0 이상의 실수여야 합니다.");
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 🔍 SFX 카탈로그 검증
        /// </summary>
        public static ValidationResult ValidateSFXCatalog(List<Dictionary<string, string>> data, string filePath)
        {
            var result = new ValidationResult();
            
            if (data == null || data.Count == 0)
            {
                result.AddWarning(0, "", "SFX 카탈로그가 비어있습니다.");
                return result;
            }
            
            // 헤더 검증
            var headers = data[0].Keys.ToList();
            foreach (var requiredColumn in REQUIRED_SFX_COLUMNS)
            {
                if (!headers.Contains(requiredColumn))
                {
                    result.AddError(1, requiredColumn, $"필수 컬럼 '{requiredColumn}'이 없습니다.");
                }
            }
            
            // 데이터 행 검증
            for (int i = 0; i < data.Count; i++)
            {
                int lineNumber = i + 2;
                var row = data[i];
                
                // Volume 검증
                if (row.ContainsKey("volume") && !string.IsNullOrEmpty(row["volume"]))
                {
                    if (!float.TryParse(row["volume"], out float volume) || volume < 0 || volume > 1)
                    {
                        result.AddError(lineNumber, "volume", "볼륨은 0.0~1.0 사이의 실수여야 합니다.");
                    }
                }
                
                // Pitch 검증
                if (row.ContainsKey("pitch") && !string.IsNullOrEmpty(row["pitch"]))
                {
                    if (!float.TryParse(row["pitch"], out float pitch) || pitch <= 0)
                    {
                        result.AddError(lineNumber, "pitch", "피치는 0보다 큰 실수여야 합니다.");
                    }
                }
                
                // Max Distance 검증 (3D 사운드용)
                if (row.ContainsKey("max_distance") && !string.IsNullOrEmpty(row["max_distance"]))
                {
                    if (!float.TryParse(row["max_distance"], out float maxDistance) || maxDistance <= 0)
                    {
                        result.AddError(lineNumber, "max_distance", "최대 거리는 0보다 큰 실수여야 합니다.");
                    }
                }
            }
            
            return result;
        }
        
        #region Helper Methods
        
        /// <summary>
        /// 유효한 식별자인지 검증 (영문자, 숫자, 언더스코어만)
        /// </summary>
        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return false;
                
            return Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }
        
        /// <summary>
        /// 중복 프로필 ID 검증
        /// </summary>
        private static void ValidateDuplicateProfileIds(List<Dictionary<string, string>> data, ValidationResult result)
        {
            var profileIds = new Dictionary<string, int>(); // profileId -> first line number
            
            for (int i = 0; i < data.Count; i++)
            {
                int lineNumber = i + 2;
                var row = data[i];
                
                if (row.ContainsKey("domain") && row.ContainsKey("profile_id"))
                {
                    string key = $"{row["domain"]}.{row["profile_id"]}";
                    
                    if (profileIds.ContainsKey(key))
                    {
                        result.AddError(lineNumber, "profile_id", 
                            $"중복된 프로필 ID: {key} (첫 번째 정의: 라인 {profileIds[key]})");
                    }
                    else
                    {
                        profileIds[key] = lineNumber;
                    }
                }
            }
        }
        
        /// <summary>
        /// 중복 이벤트 키 검증
        /// </summary>
        private static void ValidateDuplicateEventKeys(List<Dictionary<string, string>> data, ValidationResult result)
        {
            var eventKeys = new Dictionary<string, int>(); // domain.profile_id.event_key -> first line number
            
            for (int i = 0; i < data.Count; i++)
            {
                int lineNumber = i + 2;
                var row = data[i];
                
                if (row.ContainsKey("domain") && row.ContainsKey("profile_id") && row.ContainsKey("event_key"))
                {
                    string key = $"{row["domain"]}.{row["profile_id"]}.{row["event_key"]}";
                    
                    if (eventKeys.ContainsKey(key))
                    {
                        result.AddError(lineNumber, "event_key", 
                            $"중복된 이벤트 키: {key} (첫 번째 정의: 라인 {eventKeys[key]})");
                    }
                    else
                    {
                        eventKeys[key] = lineNumber;
                    }
                }
            }
        }
        
        #endregion
    }
}
