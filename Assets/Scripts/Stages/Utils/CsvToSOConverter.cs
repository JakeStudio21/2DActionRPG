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
            Debug.Log("🔄 [CsvToSOConverter] CSV→SO 변환 시작...");
            
            // CSV 폴더 존재 여부 확인
            if (!Directory.Exists(CSV_PATH))
            {
                Debug.LogError($"❌ [CsvToSOConverter] CSV 폴더가 존재하지 않습니다: {CSV_PATH}");
                Debug.Log($"💡 [CsvToSOConverter] 폴더를 생성하거나 CSV 파일들을 이동해주세요.");
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
            
            Debug.Log("✅ [CsvToSOConverter] 변환 완료!");
        }
        
        /// <summary>
        /// StageConfig.csv → StageConfig ScriptableObject
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
                
                Debug.Log($"📄 [StageConfig] 생성: {stageId}");
            }
            
            return configs;
        }
        
        /// <summary>
        /// WaveConfig.csv → WaveConfig ScriptableObject
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
                
                Debug.Log($"🌊 [WaveConfig] 생성: {waveId}");
            }
            
            return configs;
        }
        
        /// <summary>
        /// SpawnGroup.csv + SpawnGroupMonster.csv → SpawnGroup ScriptableObject
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
                            
                            groupDict[groupId].AddMonsterData(monsterId, count, spawnCount, isBoss);
                        }
                    }
                }
            }
            
            // 3단계: 에셋 생성
            foreach (var group in groups)
            {
                string assetPath = $"{STAGES_RESOURCE_PATH}Spawns/{group.SpawnGroupID}_Config.asset";
                UnityEditor.AssetDatabase.CreateAsset(group, assetPath);
                Debug.Log($"👾 [SpawnGroup] 생성: {group.SpawnGroupID} (몬스터 {group.Monsters.Count}종)");
            }
            
            return groups;
        }
        
        /// <summary>
        /// DropGroup.csv + DropItem.csv → DropTable ScriptableObject
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
                Debug.Log($"💎 [DropTable] 생성: {table.DropGroupID} (아이템 {table.Items.Count}개)");
            }
            
            return tables;
        }
        
        /// <summary>
        /// 참조 연결
        /// </summary>
        private static void LinkReferences(List<StageConfig> stages, List<WaveConfig> waves, 
                                         List<SpawnGroup> groups, List<DropTable> drops)
        {
            Debug.Log("🔗 [CsvToSOConverter] 참조 연결 시작...");
            
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
            }
            
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("✅ [CsvToSOConverter] 참조 연결 완료");
        }
        
        /// <summary>
        /// 무결성 검사
        /// </summary>
        private static void ValidateAllData(List<StageConfig> stages, List<WaveConfig> waves,
                                          List<SpawnGroup> groups, List<DropTable> drops)
        {
            Debug.Log("🔍 [CsvToSOConverter] 무결성 검사 시작...");
            
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
                Debug.Log("✅ [CsvToSOConverter] 무결성 검사 완료 - 오류 없음");
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
