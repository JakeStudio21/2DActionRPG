using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace StageSystem
{
    /// <summary>
    /// CSV 파일을 ScriptableObject로 변환하는 시스템
    /// CSV→SO 자동생성→참조연결 파이프라인
    /// </summary>
    public static class CsvToSOConverter
    {
        private const string STAGES_RESOURCE_PATH = "Assets/Resources/Stages/";
        private const string CSV_PATH = "Assets/Editor/Stage/CSV/";  // 변경된 경로
        
        /// <summary>
        /// 모든 CSV 파일을 ScriptableObject로 변환
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Convert All CSV to SO")]
        public static void ConvertAllCsvToSO()
        {
            // CSV 폴더 존재 여부 확인
            if (!Directory.Exists(CSV_PATH))
            {
                Debug.LogError($"❌ [CsvToSOConverter] CSV 폴더가 존재하지 않습니다: {CSV_PATH}");
                return;
            }
            
            // 1단계: 기본 SO 생성
            var stageConfigs = ConvertStageConfigs();
            var waveConfigs = ConvertWaveConfigs();
            var spawnGroups = ConvertSpawnGroups();
            var dropTables = ConvertDropTables();
            
            // 2단계: 참조 연결
            LinkReferences(stageConfigs, waveConfigs, spawnGroups, dropTables);
            
            // 3단계: 무결성 검사
            ValidateAllData(stageConfigs, waveConfigs, spawnGroups, dropTables);
        }
        
        /// <summary>
        /// StageConfig.csv → StageConfig ScriptableObject (개별 변환)
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Convert Select Only/Convert StageConfig Only")]
        public static void ConvertStageConfigsOnly()
        {
            ConvertStageConfigs();
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// StageConfig.csv → StageConfig ScriptableObject (내부 메서드)
        /// </summary>
        private static List<StageConfig> ConvertStageConfigs()
        {
            string csvPath = CSV_PATH + "StageConfig.csv";
            var configs = new List<StageConfig>();
            
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"[CsvToSOConverter] CSV 파일을 찾을 수 없습니다: {csvPath}");
                return configs;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 3) return configs;
            
            string[] headers = lines[0].Split(',');
            
            for (int i = 2; i < lines.Length; i++) // 헤더(0), 타입(1) 제외
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var csvData = CsvDataLoader.ParseCsvLine(headers, lines[i]);
                string stageId = csvData.GetValueOrDefault("StageID", "");
                
                if (string.IsNullOrEmpty(stageId)) continue;
                
                var config = ScriptableObject.CreateInstance<StageConfig>();
                config.InitializeFromCsv(csvData);
                
                string assetPath = $"{STAGES_RESOURCE_PATH}Configs/{stageId}_Config.asset";
                UnityEditor.AssetDatabase.CreateAsset(config, assetPath);
                configs.Add(config);
            }
            
            return configs;
        }
        
        /// <summary>
        /// WaveConfig.csv → WaveConfig ScriptableObject (개별 변환)
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Convert Select Only/Convert WaveConfig Only")]
        public static void ConvertWaveConfigsOnly()
        {
            ConvertWaveConfigs();
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// WaveConfig.csv → WaveConfig ScriptableObject (내부 메서드)
        /// </summary>
        private static List<WaveConfig> ConvertWaveConfigs()
        {
            string csvPath = CSV_PATH + "WaveConfig.csv";
            var configs = new List<WaveConfig>();
            
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"[CsvToSOConverter] CSV 파일을 찾을 수 없습니다: {csvPath}");
                return configs;
            }
            
            string[] lines = File.ReadAllLines(csvPath);
            if (lines.Length < 3) return configs;
            
            string[] headers = lines[0].Split(',');
            
            for (int i = 2; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var csvData = CsvDataLoader.ParseCsvLine(headers, lines[i]);
                string waveId = csvData.GetValueOrDefault("WaveID", "");
                
                if (string.IsNullOrEmpty(waveId)) continue;
                
                var config = ScriptableObject.CreateInstance<WaveConfig>();
                config.InitializeFromCsv(csvData);
                
                string assetPath = $"{STAGES_RESOURCE_PATH}Waves/{waveId}_Config.asset";
                UnityEditor.AssetDatabase.CreateAsset(config, assetPath);
                configs.Add(config);
            }
            
            return configs;
        }
        
        /// <summary>
        /// SpawnGroup.csv + SpawnGroupMonster.csv → SpawnGroup ScriptableObject (개별 변환)
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Convert Select Only/Convert SpawnGroup Only")]
        public static void ConvertSpawnGroupsOnly()
        {
            ConvertSpawnGroups();
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// SpawnGroup.csv + SpawnGroupMonster.csv → SpawnGroup ScriptableObject (내부 메서드)
        /// </summary>
        private static List<SpawnGroup> ConvertSpawnGroups()
        {
            var groups = new List<SpawnGroup>();
            var groupDict = new Dictionary<string, SpawnGroup>();
            
            // 1단계: SpawnGroup.csv 처리
            string groupCsvPath = CSV_PATH + "SpawnGroup.csv";
            if (File.Exists(groupCsvPath))
            {
                string[] lines = File.ReadAllLines(groupCsvPath);
                if (lines.Length >= 3)
                {
                    string[] headers = lines[0].Split(',');
                    
                    for (int i = 2; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        
                        var csvData = CsvDataLoader.ParseCsvLine(headers, lines[i]);
                        string groupId = csvData.GetValueOrDefault("SpawnGroupID", "");
                        
                        if (string.IsNullOrEmpty(groupId)) continue;
                        
                        var group = ScriptableObject.CreateInstance<SpawnGroup>();
                        group.InitializeFromCsv(csvData);
                        groupDict[groupId] = group;
                        groups.Add(group);
                    }
                }
            }
            
            // 2단계: SpawnGroupMonster.csv 처리
            string monsterCsvPath = CSV_PATH + "SpawnGroupMonster.csv";
            if (File.Exists(monsterCsvPath))
            {
                string[] lines = File.ReadAllLines(monsterCsvPath);
                if (lines.Length >= 3)
                {
                    string[] headers = lines[0].Split(',');
                    
                    for (int i = 2; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        
                        var csvData = CsvDataLoader.ParseCsvLine(headers, lines[i]);
                        string groupId = csvData.GetValueOrDefault("SpawnGroupID", "");
                        
                        if (groupDict.ContainsKey(groupId))
                        {
                            string monsterId = csvData.GetValueOrDefault("MonsterID", "");
                            int.TryParse(csvData.GetValueOrDefault("Count", "1"), out int count);
                            float.TryParse(csvData.GetValueOrDefault("Spawn Count", "0"), out float spawnCount);
                            bool.TryParse(csvData.GetValueOrDefault("IsBoss", "FALSE"), out bool isBoss);
                            
                            // ⭐ Phase 1: LevelOffset 파싱 추가 (없으면 기본값 0)
                            int.TryParse(csvData.GetValueOrDefault("LevelOffset", "0"), out int levelOffset);
                            
                            groupDict[groupId].AddMonsterData(monsterId, count, spawnCount, isBoss, levelOffset);
                        }
                    }
                }
            }
            
            // 3단계: 에셋 생성
            foreach (var group in groups)
            {
                string assetPath = $"{STAGES_RESOURCE_PATH}Spawns/{group.SpawnGroupID}_Config.asset";
                UnityEditor.AssetDatabase.CreateAsset(group, assetPath);
            }
            
            return groups;
        }
        
        /// <summary>
        /// DropGroup.csv + DropItem.csv → DropTable ScriptableObject (개별 변환)
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Convert Select Only/Convert DropTable Only")]
        public static void ConvertDropTablesOnly()
        {
            ConvertDropTables();
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// DropGroup.csv + DropItem.csv → DropTable ScriptableObject (내부 메서드)
        /// </summary>
        private static List<DropTable> ConvertDropTables()
        {
            var tables = new List<DropTable>();
            var tableDict = new Dictionary<string, DropTable>();
            
            // 1단계: DropGroup.csv 처리
            string groupCsvPath = CSV_PATH + "DropGroup.csv";
            if (File.Exists(groupCsvPath))
            {
                string[] lines = File.ReadAllLines(groupCsvPath);
                if (lines.Length >= 3)
                {
                    string[] headers = lines[0].Split(',');
                    
                    for (int i = 2; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        
                        var csvData = CsvDataLoader.ParseCsvLine(headers, lines[i]);
                        string dropId = csvData.GetValueOrDefault("DropGroupID", "");
                        
                        if (string.IsNullOrEmpty(dropId)) continue;
                        
                        var table = ScriptableObject.CreateInstance<DropTable>();
                        table.InitializeFromCsv(csvData);
                        tableDict[dropId] = table;
                        tables.Add(table);
                    }
                }
            }
            
            // 2단계: DropItem.csv 처리
            string itemCsvPath = CSV_PATH + "DropItem.csv";
            if (File.Exists(itemCsvPath))
            {
                string[] lines = File.ReadAllLines(itemCsvPath);
                if (lines.Length >= 3)
                {
                    string[] headers = lines[0].Split(',');
                    
                    for (int i = 2; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        
                        var csvData = CsvDataLoader.ParseCsvLine(headers, lines[i]);
                        string dropId = csvData.GetValueOrDefault("DropGroupID", "");
                        
                        if (tableDict.ContainsKey(dropId))
                        {
                            string itemId = csvData.GetValueOrDefault("ItemID", "");
                            int.TryParse(csvData.GetValueOrDefault("Amount", "1"), out int amount);
                            float.TryParse(csvData.GetValueOrDefault("DropRate", "0.3"), out float dropRate);
                            
                            tableDict[dropId].AddItemData(itemId, amount, dropRate);
                        }
                    }
                }
            }
            
            // 3단계: 에셋 생성
            foreach (var table in tables)
            {
                string assetPath = $"{STAGES_RESOURCE_PATH}Drops/{table.DropGroupID}_Config.asset";
                UnityEditor.AssetDatabase.CreateAsset(table, assetPath);
            }
            
            return tables;
        }
        
        /// <summary>
        /// 모든 참조 연결 + 무결성 검사 (개별 변환 후 실행)
        /// </summary>
        [UnityEditor.MenuItem("Tools/Stage System/Link All References")]
        public static void LinkAllReferences()
        {
            // 기존 SO들을 로드
            var stages = LoadAllScriptableObjects<StageConfig>("Assets/Resources/Stages/Configs");
            var waves = LoadAllScriptableObjects<WaveConfig>("Assets/Resources/Stages/Waves");
            var groups = LoadAllScriptableObjects<SpawnGroup>("Assets/Resources/Stages/Spawns");
            var drops = LoadAllScriptableObjects<DropTable>("Assets/Resources/Stages/Drops");
            
            // 참조 연결
            LinkReferences(stages, waves, groups, drops);
            
            // 무결성 검사
            ValidateAllData(stages, waves, groups, drops);
        }
        
        /// <summary>
        /// 특정 폴더의 모든 ScriptableObject 로드
        /// </summary>
        private static List<T> LoadAllScriptableObjects<T>(string folderPath) where T : ScriptableObject
        {
            var results = new List<T>();
            
            if (!Directory.Exists(folderPath))
            {
                Debug.LogWarning($"⚠️ [CsvToSOConverter] 폴더가 존재하지 않습니다: {folderPath}");
                return results;
            }
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }
            return results;
        }
        
        /// <summary>
        /// 참조 연결 (내부 메서드)
        /// </summary>
        private static void LinkReferences(List<StageConfig> stages, List<WaveConfig> waves, 
                                         List<SpawnGroup> groups, List<DropTable> drops)
        {
            // Stage → Wave 연결
            foreach (var stage in stages)
            {
                stage.WaveConfigs.Clear();
                foreach (var wave in waves)
                {
                    if (wave.StageID == stage.StageID)
                    {
                        stage.WaveConfigs.Add(wave);
                    }
                }
                stage.WaveConfigs.Sort((a, b) => a.WaveIndex.CompareTo(b.WaveIndex));
            }
            
            // Wave → SpawnGroup 연결
            foreach (var wave in waves)
            {
                wave.SpawnGroups.Clear();
                foreach (var group in groups)
                {
                    if (group.WaveID == wave.WaveID)
                    {
                        wave.SpawnGroups.Add(group);
                    }
                }
                
                // 🔧 핵심 수정: 에셋 파일에 직접 저장
                string waveAssetPath = $"{STAGES_RESOURCE_PATH}Waves/{wave.WaveID}_Config.asset";
                UnityEditor.EditorUtility.SetDirty(wave);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(wave);
            }
            
            // Stage → DropTable 연결
            foreach (var stage in stages)
            {
                foreach (var drop in drops)
                {
                    if (drop.DropGroupID == stage.FirstClearDropGroupId)
                        stage.FirstClearDropTable = drop;
                    if (drop.DropGroupID == stage.RepeatClearDropGroupId)
                        stage.RepeatClearDropTable = drop;
                }
                
                // 🔧 핵심 수정: 에셋 파일에 직접 저장
                UnityEditor.EditorUtility.SetDirty(stage);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(stage);
            }
            
            // 🔧 추가: 모든 SpawnGroup도 저장
            foreach (var group in groups)
            {
                UnityEditor.EditorUtility.SetDirty(group);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(group);
            }
            
            // 🔧 추가: 모든 DropTable도 저장
            foreach (var drop in drops)
            {
                UnityEditor.EditorUtility.SetDirty(drop);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(drop);
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh(); // 🔧 추가: 강제 새로고침
        }
        
        /// <summary>
        /// 무결성 검사
        /// </summary>
        private static void ValidateAllData(List<StageConfig> stages, List<WaveConfig> waves,
                                          List<SpawnGroup> groups, List<DropTable> drops)
        {
            int errorCount = 0;
            
            // 각 스테이지 검증
            foreach (var stage in stages)
            {
                if (stage.WaveConfigs.Count != stage.WaveCount)
                {
                    Debug.LogWarning($"⚠️ [{stage.StageID}] 웨이브 수 불일치: 설정 {stage.WaveCount} vs 실제 {stage.WaveConfigs.Count}");
                    errorCount++;
                }
                
                if (stage.FirstClearDropTable == null && !string.IsNullOrEmpty(stage.FirstClearDropGroupId))
                {
                    Debug.LogWarning($"⚠️ [{stage.StageID}] FirstClear 드롭테이블 연결 실패: {stage.FirstClearDropGroupId}");
                    errorCount++;
                }
            }
            
            // 드롭 테이블 확률 검증
            foreach (var drop in drops)
            {
                if (!drop.ValidateDropRates())
                    errorCount++;
            }
            
            if (errorCount == 0)
            {
            }
            else
            {
                Debug.LogWarning($"⚠️ [CsvToSOConverter] 무결성 검사 완료 - {errorCount}개 경고 발견");
            }
        }
    }
    
    /// <summary>
    /// Dictionary 확장 메서드
    /// </summary>
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
