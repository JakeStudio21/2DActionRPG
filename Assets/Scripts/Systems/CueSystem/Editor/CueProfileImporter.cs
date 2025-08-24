using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System;

namespace CueSystem.Editor
{
    /// <summary>
    /// 📥 Cue Profile Importer (CSV → ScriptableObject 변환 엔진)
    /// 메타/엔트리 분리 구조로 CueProfile 자동 생성
    /// </summary>
    public static class CueProfileImporter
    {
        /// <summary>
        /// 📊 임포트 결과 클래스
        /// </summary>
        public class ImportResult
        {
            public bool Success => Errors.Count == 0;
            public List<string> CreatedAssets = new List<string>();
            public List<string> UpdatedAssets = new List<string>();
            public List<string> Errors = new List<string>();
            public List<string> Warnings = new List<string>();
            
            public string GetSummary()
            {
                return $"임포트 완료: 생성 {CreatedAssets.Count}개, 업데이트 {UpdatedAssets.Count}개, 에러 {Errors.Count}개, 경고 {Warnings.Count}개";
            }
        }
        
        /// <summary>
        /// 📥 메인 임포트 API - CSV 파일들을 CueProfile로 변환
        /// </summary>
        public static ImportResult ImportFromCSV(string metaCSVPath, string entriesCSVPath, string vfxCSVPath = null, string sfxCSVPath = null)
        {
            var result = new ImportResult();
            
            try
            {
                // 1. CSV 파일 존재 확인
                if (!File.Exists(metaCSVPath))
                {
                    result.Errors.Add($"메타 CSV 파일을 찾을 수 없습니다: {metaCSVPath}");
                    return result;
                }
                
                if (!File.Exists(entriesCSVPath))
                {
                    result.Errors.Add($"엔트리 CSV 파일을 찾을 수 없습니다: {entriesCSVPath}");
                    return result;
                }
                
                // 2. CSV 파싱
                var metaData = ParseCSV(metaCSVPath);
                var entriesData = ParseCSV(entriesCSVPath);
                var vfxData = !string.IsNullOrEmpty(vfxCSVPath) && File.Exists(vfxCSVPath) ? ParseCSV(vfxCSVPath) : new List<Dictionary<string, string>>();
                var sfxData = !string.IsNullOrEmpty(sfxCSVPath) && File.Exists(sfxCSVPath) ? ParseCSV(sfxCSVPath) : new List<Dictionary<string, string>>();
                
                // 3. 유효성 검사
                var metaValidation = CueDataValidator.ValidateMetaCSV(metaData, metaCSVPath);
                var entriesValidation = CueDataValidator.ValidateEntriesCSV(entriesData, entriesCSVPath);
                
                // 에러가 있으면 중단
                if (!metaValidation.IsValid)
                {
                    result.Errors.AddRange(metaValidation.Errors.Select(e => $"메타 CSV: {e}"));
                }
                
                if (!entriesValidation.IsValid)
                {
                    result.Errors.AddRange(entriesValidation.Errors.Select(e => $"엔트리 CSV: {e}"));
                }
                
                if (!result.Success)
                {
                    return result;
                }
                
                // 4. VFX/SFX 카탈로그 빌드
                var vfxCatalog = BuildVFXCatalog(vfxData);
                var sfxCatalog = BuildSFXCatalog(sfxData);
                
                // 5. 프로필별로 그룹화
                var profileGroups = GroupEntriesByProfile(entriesData);
                
                // 6. CueProfile 생성/업데이트
                foreach (var metaRow in metaData)
                {
                    string domain = metaRow["domain"];
                    string profileId = metaRow["profile_id"];
                    string profileKey = $"{domain}.{profileId}";
                    
                    // 해당 프로필의 엔트리들 찾기
                    var profileEntries = profileGroups.ContainsKey(profileKey) ? profileGroups[profileKey] : new List<Dictionary<string, string>>();
                    
                    // CueProfile 생성/업데이트
                    var profile = CreateOrUpdateProfile(metaRow, profileEntries, vfxCatalog, sfxCatalog, result);
                    
                    if (profile != null)
                    {
                        // 에셋 저장
                        string assetPath = GetProfileAssetPath(domain, profileId);
                        bool isNewAsset = !File.Exists(assetPath);
                        
                        if (isNewAsset)
                        {
                            AssetDatabase.CreateAsset(profile, assetPath);
                            result.CreatedAssets.Add(assetPath);
                        }
                        else
                        {
                            EditorUtility.SetDirty(profile);
                            result.UpdatedAssets.Add(assetPath);
                        }
                    }
                }
                
                // 7. 에셋 데이터베이스 새로고침
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
            }
            catch (System.Exception ex)
            {
                result.Errors.Add($"임포트 중 예외 발생: {ex.Message}");
                Debug.LogException(ex);
            }
            
            return result;
        }
        
        /// <summary>
        /// 📄 CSV 파싱 (UTF-8 지원)
        /// </summary>
        public static List<Dictionary<string, string>> ParseCSV(string filePath)
        {
            var result = new List<Dictionary<string, string>>();
            
            try
            {
                // 고급 파일 읽기 (BOM 처리 포함)
                string content = ReadFileWithAdvancedEncoding(filePath);
                
                var lines = content.Split('\n').Select(line => line.Trim('\r')).Where(line => !string.IsNullOrEmpty(line)).ToArray();
                
                if (lines.Length < 2)
                {
                    Debug.LogWarning($"CSV 파일에 데이터가 부족합니다: {filePath}");
                    return result;
                }
                
                // 헤더 파싱
                var headers = ParseCSVLine(lines[0]);
                
                // 데이터 행 파싱
                for (int i = 1; i < lines.Length; i++)
                {
                    var values = ParseCSVLine(lines[i]);
                    
                    if (values.Count != headers.Count)
                    {
                        Debug.LogWarning($"CSV 라인 {i + 1}의 컬럼 수가 헤더와 다릅니다: {filePath}");
                        continue;
                    }
                    
                    var row = new Dictionary<string, string>();
                    for (int j = 0; j < headers.Count; j++)
                    {
                        row[headers[j].Trim()] = values[j].Trim();
                    }
                    
                    result.Add(row);
                }
                
                Debug.Log($"✅ [ParseCSV] 파싱 완료: {result.Count}개 행, 파일: {Path.GetFileName(filePath)}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"🔴 [ParseCSV] 파싱 오류: {ex.Message}, 파일: {filePath}");
            }
            
            return result;
        }
        
        /// <summary>
        /// 📄 CSV 라인 파싱 (따옴표 처리)
        /// </summary>
        private static List<string> ParseCSVLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // 이스케이프된 따옴표
                        current.Append('"');
                        i++; // 다음 따옴표 건너뛰기
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString().Trim());
            return result;
        }
        
        /// <summary>
        /// 🔧 고급 인코딩 처리 파일 읽기
        /// </summary>
        private static string ReadFileWithAdvancedEncoding(string filePath)
        {
            try
            {
                // 바이트 레벨에서 읽기
                byte[] bytes = File.ReadAllBytes(filePath);
                
                if (bytes.Length == 0)
                {
                    Debug.LogError($"🔴 [ReadFile] 빈 파일: {filePath}");
                    return "";
                }
                
                // BOM 감지 및 제거
                int startIndex = 0;
                Encoding encoding = Encoding.UTF8;
                
                // UTF-8 BOM (EF BB BF)
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    startIndex = 3;
                    encoding = Encoding.UTF8;
                }
                // UTF-16 LE BOM (FF FE)
                else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                {
                    startIndex = 2;
                    encoding = Encoding.Unicode;
                }
                // UTF-16 BE BOM (FE FF)
                else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                {
                    startIndex = 2;
                    encoding = Encoding.BigEndianUnicode;
                }
                
                // 실제 데이터 디코딩
                string content = encoding.GetString(bytes, startIndex, bytes.Length - startIndex);
                
                // 추가 보이지 않는 문자 제거
                content = content.TrimStart('\uFEFF', '\uFFFE', '\u200B', '\u00A0');
                
                return content;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"🔴 [ReadFile] 파일 읽기 실패: {ex.Message}");
                
                // 폴백: 기본 방식으로 시도
                try
                {
                    return File.ReadAllText(filePath, Encoding.UTF8);
                }
                catch
                {
                    Debug.LogError($"🔴 [ReadFile] 폴백도 실패");
                    return "";
                }
            }
        }
        
        /// <summary>
        /// 🎨 VFX 카탈로그 빌드
        /// </summary>
        private static List<VFXCue> BuildVFXCatalog(List<Dictionary<string, string>> vfxData)
        {
            var catalog = new List<VFXCue>();
            
            foreach (var row in vfxData)
            {
                var vfx = new VFXCue();
                
                vfx.vfxId = GetStringValue(row, "vfx_id");
                vfx.poolKey = GetStringValue(row, "pool_key", vfx.vfxId); // 기본값: vfx_id와 동일
                vfx.duration = GetFloatValue(row, "duration", -1f);
                vfx.scale = Vector3.one * GetFloatValue(row, "scale", 1f);
                vfx.offset = new Vector3(
                    GetFloatValue(row, "offset_x", 0f),
                    GetFloatValue(row, "offset_y", 0f),
                    GetFloatValue(row, "offset_z", 0f)
                );
                vfx.followTarget = GetBoolValue(row, "follow_target", false);
                vfx.cooldown = GetFloatValue(row, "cooldown", 0f);
                vfx.priority = GetIntValue(row, "priority", 50);
                vfx.note = GetStringValue(row, "note");
                
                catalog.Add(vfx);
            }
            
            return catalog;
        }
        
        /// <summary>
        /// 🔊 SFX 카탈로그 빌드
        /// </summary>
        private static List<SFXCue> BuildSFXCatalog(List<Dictionary<string, string>> sfxData)
        {
            var catalog = new List<SFXCue>();
            
            foreach (var row in sfxData)
            {
                var sfx = new SFXCue();
                
                sfx.sfxId = GetStringValue(row, "sfx_id");
                
                // AudioClip 로드 시도
                string clipPath = GetStringValue(row, "audio_clip_path");
                if (!string.IsNullOrEmpty(clipPath))
                {
                    sfx.audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    if (sfx.audioClip == null)
                    {
                        // Resources 폴더에서 시도
                        sfx.audioClip = Resources.Load<AudioClip>(clipPath);
                    }
                }
                
                sfx.volume = GetFloatValue(row, "volume", 1f);
                sfx.pitch = GetFloatValue(row, "pitch", 1f);
                sfx.is3D = GetBoolValue(row, "is_3d", false);
                sfx.maxDistance = GetFloatValue(row, "max_distance", 50f);
                sfx.cooldown = GetFloatValue(row, "cooldown", 0f);
                sfx.priority = GetIntValue(row, "priority", 50);
                sfx.note = GetStringValue(row, "note");
                
                catalog.Add(sfx);
            }
            
            return catalog;
        }
        
        /// <summary>
        /// 📋 엔트리를 프로필별로 그룹화
        /// </summary>
        private static Dictionary<string, List<Dictionary<string, string>>> GroupEntriesByProfile(List<Dictionary<string, string>> entriesData)
        {
            var groups = new Dictionary<string, List<Dictionary<string, string>>>();
            
            foreach (var entry in entriesData)
            {
                string domain = GetStringValue(entry, "domain");
                string profileId = GetStringValue(entry, "profile_id");
                string key = $"{domain}.{profileId}";
                
                if (!groups.ContainsKey(key))
                {
                    groups[key] = new List<Dictionary<string, string>>();
                }
                
                groups[key].Add(entry);
            }
            
            return groups;
        }
        
        /// <summary>
        /// 🎭 CueProfile 생성 또는 업데이트
        /// </summary>
        private static CueProfile CreateOrUpdateProfile(
            Dictionary<string, string> metaRow,
            List<Dictionary<string, string>> entries,
            List<VFXCue> vfxCatalog,
            List<SFXCue> sfxCatalog,
            ImportResult result)
        {
            string domain = GetStringValue(metaRow, "domain");
            string profileId = GetStringValue(metaRow, "profile_id");
            string assetPath = GetProfileAssetPath(domain, profileId);
            
            // 기존 에셋 로드 시도
            CueProfile profile = AssetDatabase.LoadAssetAtPath<CueProfile>(assetPath);
            
            if (profile == null)
            {
                // 새 프로필 생성
                profile = ScriptableObject.CreateInstance<CueProfile>();
            }
            
            // 메타데이터 설정
            profile.profileId = profileId;
            profile.domain = domain;
            profile.description = GetStringValue(metaRow, "description");
            
            // Base Profile 설정
            string baseProfileId = GetStringValue(metaRow, "base_profile_id");
            if (!string.IsNullOrEmpty(baseProfileId))
            {
                string baseAssetPath = GetProfileAssetPath(domain, baseProfileId);
                profile.baseProfile = AssetDatabase.LoadAssetAtPath<CueProfile>(baseAssetPath);
                
                if (profile.baseProfile == null)
                {
                    result.Warnings.Add($"Base Profile을 찾을 수 없습니다: {baseProfileId} (프로필: {profileId})");
                }
            }
            
            // 카탈로그 설정 (해당 프로필에서 사용하는 VFX/SFX만)
            var usedVFXIds = new HashSet<string>();
            var usedSFXIds = new HashSet<string>();
            
            foreach (var entry in entries)
            {
                var vfxIds = GetStringValue(entry, "vfx_ids").Split(';').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id));
                var sfxIds = GetStringValue(entry, "sfx_ids").Split(';').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id));
                
                foreach (var id in vfxIds) usedVFXIds.Add(id);
                foreach (var id in sfxIds) usedSFXIds.Add(id);
            }
            
            profile.vfxCatalog = vfxCatalog.Where(vfx => usedVFXIds.Contains(vfx.vfxId)).ToList();
            profile.sfxCatalog = sfxCatalog.Where(sfx => usedSFXIds.Contains(sfx.sfxId)).ToList();
            
            // 엔트리 설정
            profile.entries.Clear();
            foreach (var entryRow in entries)
            {
                var entry = new CueEntry();
                
                entry.eventKey = GetStringValue(entryRow, "event_key");
                entry.vfxIds = GetStringValue(entryRow, "vfx_ids").Split(';').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id)).ToList();
                entry.sfxIds = GetStringValue(entryRow, "sfx_ids").Split(';').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id)).ToList();
                entry.priority = GetIntValue(entryRow, "priority", 50);
                entry.cameraShakePreset = GetStringValue(entryRow, "camera_shake_preset");
                entry.timeStopMs = GetIntValue(entryRow, "time_stop_ms", 0);
                entry.note = GetStringValue(entryRow, "note");
                
                profile.entries.Add(entry);
            }
            
            return profile;
        }
        
        #region Helper Methods
        
        /// <summary>
        /// 프로필 에셋 경로 생성
        /// </summary>
        private static string GetProfileAssetPath(string domain, string profileId)
        {
            return $"Assets/Resources/CueProfiles/{domain}_{profileId}.asset";
        }
        
        /// <summary>
        /// 문자열 값 가져오기
        /// </summary>
        private static string GetStringValue(Dictionary<string, string> row, string key, string defaultValue = "")
        {
            return row.ContainsKey(key) ? row[key].Trim() : defaultValue;
        }
        
        /// <summary>
        /// 정수 값 가져오기
        /// </summary>
        private static int GetIntValue(Dictionary<string, string> row, string key, int defaultValue = 0)
        {
            if (row.ContainsKey(key) && int.TryParse(row[key], out int value))
                return value;
            return defaultValue;
        }
        
        /// <summary>
        /// 실수 값 가져오기
        /// </summary>
        private static float GetFloatValue(Dictionary<string, string> row, string key, float defaultValue = 0f)
        {
            if (row.ContainsKey(key) && float.TryParse(row[key], out float value))
                return value;
            return defaultValue;
        }
        
        /// <summary>
        /// 불린 값 가져오기
        /// </summary>
        private static bool GetBoolValue(Dictionary<string, string> row, string key, bool defaultValue = false)
        {
            if (row.ContainsKey(key))
            {
                string value = row[key].ToLower().Trim();
                return value == "true" || value == "1" || value == "yes";
            }
            return defaultValue;
        }
        
        #endregion

        // 기존 Test Import를 완전한 버전으로 업그레이드
        [MenuItem("Tools/Cue System/Test Import")]
        public static void TestImport()
        {
            string metaPath = "Assets/StreamingAssets/CueData/CueProfile_Meta.csv";
            string entriesPath = "Assets/StreamingAssets/CueData/CueProfile_Entries.csv";
            string vfxPath = "Assets/StreamingAssets/CueData/VFX_Catalog.csv";
            string sfxPath = "Assets/StreamingAssets/CueData/SFX_Catalog.csv";
            
            // VFX/SFX 파일이 있으면 포함, 없으면 null
            string vfxPathToUse = File.Exists(vfxPath) ? vfxPath : null;
            string sfxPathToUse = File.Exists(sfxPath) ? sfxPath : null;
            
            var result = ImportFromCSV(metaPath, entriesPath, vfxPathToUse, sfxPathToUse);
            
            Debug.Log($"📊 임포트 결과: {result.GetSummary()}");
            
            if (vfxPathToUse == null)
                Debug.LogWarning("⚠️ VFX_Catalog.csv를 찾을 수 없습니다. VFX 카탈로그가 비어있을 수 있습니다.");
            if (sfxPathToUse == null)
                Debug.LogWarning("⚠️ SFX_Catalog.csv를 찾을 수 없습니다. SFX 카탈로그가 비어있을 수 있습니다.");
            
            foreach (var error in result.Errors)
            {
                Debug.LogError($"🔴 에러: {error}");
            }
            
            foreach (var created in result.CreatedAssets)
            {
                Debug.Log($"✅ 생성됨: {created}");
            }
        }

        // 기존 Import CSV Profiles 창도 업그레이드
        [MenuItem("Tools/Cue System/Import CSV Profiles")]
        public static void OpenImporterWindow()
        {
            CueProfileEditorWindow.ShowWindow();
        }
    }
}
