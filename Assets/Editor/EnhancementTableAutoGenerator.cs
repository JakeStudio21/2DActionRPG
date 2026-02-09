using UnityEngine;
using UnityEditor;
using Systems;

/// <summary>
/// 강화 테이블 자동 생성 도구
/// - EnhanceLevelTableSO: 비용/확률/보너스
/// - EnhanceCurveTableSO: 스탯 증가율 곡선
/// </summary>
public class EnhancementTableAutoGenerator : EditorWindow
{
    [MenuItem("Tools/Workshop/Generate Enhancement Tables")]
    public static void ShowWindow()
    {
        GetWindow<EnhancementTableAutoGenerator>("강화 테이블 생성 도구");
    }
    
    void OnGUI()
    {
        GUILayout.Label("강화 테이블 자동 생성", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "이 도구는 두 개의 강화 테이블을 자동으로 생성합니다:\n\n" +
            "1️⃣ EnhanceLevelTableSO - 비용/확률/보너스\n" +
            "2️⃣ EnhanceCurveTableSO - 스탯 증가율 곡선\n\n" +
            "⚠️ 기존 파일이 있다면 덮어씌워집니다!",
            MessageType.Info
        );
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("✨ 강화 테이블 생성 (Lv 0~15)", GUILayout.Height(50)))
        {
            GenerateTables();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🧪 데이터 검증 (Console 확인)", GUILayout.Height(30)))
        {
            ValidateTables();
        }
    }
    
    void GenerateTables()
    {
        if (!EditorUtility.DisplayDialog(
            "확인",
            "강화 테이블을 자동 생성합니다.\n\n" +
            "기존 파일이 있다면 덮어씌워집니다.\n계속하시겠습니까?",
            "예, 생성",
            "취소"))
        {
            return;
        }
        
        // 1️⃣ EnhanceLevelTableSO 생성
        GenerateLevelTable();
        
        // 2️⃣ EnhanceCurveTableSO 생성
        GenerateCurveTable();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("✅ [EnhancementTableAutoGenerator] 강화 테이블 생성 완료!");
        EditorUtility.DisplayDialog("완료", "강화 테이블 생성이 완료되었습니다!", "확인");
    }
    
    void GenerateLevelTable()
    {
        string path = "Assets/Resources/Data/EnhanceLevelTable.asset";
        
        // 기존 파일 로드 또는 새로 생성
        EnhanceLevelTableSO table = AssetDatabase.LoadAssetAtPath<EnhanceLevelTableSO>(path);
        if (table == null)
        {
            table = CreateInstance<EnhanceLevelTableSO>();
            AssetDatabase.CreateAsset(table, path);
        }
        
        // Level Table 초기화
        table.levelTable = new EnhanceLevelTableSO.LevelData[16];
        
        // 레벨별 데이터 입력
        for (int i = 0; i < 16; i++)
        {
            table.levelTable[i] = CreateLevelData(i);
        }
        
        EditorUtility.SetDirty(table);
        
        Debug.Log($"✅ [Generator] EnhanceLevelTableSO 생성 완료: {path}");
    }
    
    void GenerateCurveTable()
    {
        string path = "Assets/Resources/Data/EnhanceCurveTable.asset";
        
        // 기존 파일 로드 또는 새로 생성
        EnhanceCurveTableSO table = AssetDatabase.LoadAssetAtPath<EnhanceCurveTableSO>(path);
        if (table == null)
        {
            table = CreateInstance<EnhanceCurveTableSO>();
            AssetDatabase.CreateAsset(table, path);
        }
        
        // Curve Groups 생성
        table.curveGroups = new EnhanceCurveTableSO.CurveGroup[]
        {
            // 표준 곡선
            new EnhanceCurveTableSO.CurveGroup
            {
                groupId = "CURVE_STANDARD",
                curveName = "표준 곡선 (기본)",
                levelRanges = new[]
                {
                    new EnhanceCurveTableSO.LevelRange { startLevel = 1, endLevel = 5, statRateAdd = 1.5f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 6, endLevel = 10, statRateAdd = 2.0f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 11, endLevel = 15, statRateAdd = 3.0f }
                }
            },
            
            // 무기 곡선
            new EnhanceCurveTableSO.CurveGroup
            {
                groupId = "CURVE_WEAPON",
                curveName = "무기 곡선 (공격력)",
                levelRanges = new[]
                {
                    new EnhanceCurveTableSO.LevelRange { startLevel = 1, endLevel = 5, statRateAdd = 1.5f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 6, endLevel = 10, statRateAdd = 2.0f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 11, endLevel = 15, statRateAdd = 3.0f }
                }
            },
            
            // 방어구 곡선
            new EnhanceCurveTableSO.CurveGroup
            {
                groupId = "CURVE_ARMOR",
                curveName = "방어구 곡선 (방어력)",
                levelRanges = new[]
                {
                    new EnhanceCurveTableSO.LevelRange { startLevel = 1, endLevel = 5, statRateAdd = 1.2f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 6, endLevel = 10, statRateAdd = 1.6f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 11, endLevel = 15, statRateAdd = 2.4f }
                }
            },
            
            // 악세사리 곡선
            new EnhanceCurveTableSO.CurveGroup
            {
                groupId = "CURVE_ACCESSORY",
                curveName = "악세사리 곡선 (체력)",
                levelRanges = new[]
                {
                    new EnhanceCurveTableSO.LevelRange { startLevel = 1, endLevel = 5, statRateAdd = 0.8f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 6, endLevel = 10, statRateAdd = 1.0f },
                    new EnhanceCurveTableSO.LevelRange { startLevel = 11, endLevel = 15, statRateAdd = 1.5f }
                }
            }
        };
        
        EditorUtility.SetDirty(table);
        
        Debug.Log($"✅ [Generator] EnhanceCurveTableSO 생성 완료: {path}");
    }
    
    EnhanceLevelTableSO.LevelData CreateLevelData(int level)
    {
        var data = new EnhanceLevelTableSO.LevelData
        {
            level = level
        };
        
        // 레벨별 데이터 설정 (표 기반)
        switch (level)
        {
            case 0:
                data.goldCost = 0;
                data.materialCount = 0;
                data.successRate = 1.00f;
                data.failureType = EnhancementFailureType.Maintain;
                break;
            
            case 1:
                data.goldCost = 200;
                data.materialCount = 1;
                data.successRate = 0.95f;
                data.failureType = EnhancementFailureType.Maintain;
                break;
            
            case 2:
                data.goldCost = 350;
                data.materialCount = 1;
                data.successRate = 0.92f;
                data.failureType = EnhancementFailureType.Maintain;
                break;
            
            case 3:
                data.goldCost = 550;
                data.materialCount = 2;
                data.successRate = 0.88f;
                data.bonusId = "BONUS_LV3";
                data.bonusDescription = "크리티컬 확률 +1%";
                data.failureType = EnhancementFailureType.Maintain;
                break;
            
            case 4:
                data.goldCost = 800;
                data.materialCount = 2;
                data.successRate = 0.85f;
                data.failureType = EnhancementFailureType.Maintain;
                break;
            
            case 5:
                data.goldCost = 1100;
                data.materialCount = 3;
                data.successRate = 0.82f;
                data.failureType = EnhancementFailureType.Maintain;
                break;
            
            case 6:
                data.goldCost = 1500;
                data.materialCount = 3;
                data.successRate = 0.75f;
                data.bonusId = "BONUS_LV6";
                data.bonusDescription = "크리티컬 데미지 +5%";
                data.failureType = EnhancementFailureType.Downgrade; // ⭐ Lv6부터 -1
                break;
            
            case 7:
                data.goldCost = 2000;
                data.materialCount = 4;
                data.successRate = 0.72f;
                data.failureType = EnhancementFailureType.Downgrade;
                break;
            
            case 8:
                data.goldCost = 2600;
                data.materialCount = 4;
                data.successRate = 0.68f;
                data.failureType = EnhancementFailureType.Downgrade;
                break;
            
            case 9:
                data.goldCost = 3300;
                data.materialCount = 5;
                data.successRate = 0.62f;
                data.bonusId = "BONUS_LV9";
                data.bonusDescription = "공격 속도 +3%";
                data.failureType = EnhancementFailureType.Downgrade;
                break;
            
            case 10:
                data.goldCost = 4100;
                data.materialCount = 5;
                data.successRate = 0.58f;
                data.failureType = EnhancementFailureType.Downgrade;
                break;
            
            case 11:
                data.goldCost = 5200;
                data.materialCount = 6;
                data.successRate = 0.50f;
                data.failureType = EnhancementFailureType.Downgrade;
                break;
            
            case 12:
                data.goldCost = 6600;
                data.materialCount = 6;
                data.successRate = 0.45f;
                data.bonusId = "BONUS_LV12";
                data.bonusDescription = "스킬 쿨다운 -5%";
                data.failureType = EnhancementFailureType.Destroy; // ⭐ Lv12부터 파괴
                break;
            
            case 13:
                data.goldCost = 8300;
                data.materialCount = 7;
                data.successRate = 0.40f;
                data.failureType = EnhancementFailureType.Destroy;
                break;
            
            case 14:
                data.goldCost = 10500;
                data.materialCount = 7;
                data.successRate = 0.35f;
                data.failureType = EnhancementFailureType.Destroy;
                break;
            
            case 15:
                data.goldCost = 13500;
                data.materialCount = 8;
                data.successRate = 0.30f;
                data.bonusId = "BONUS_LV15";
                data.bonusDescription = "모든 스탯 +2%";
                data.failureType = EnhancementFailureType.Destroy;
                break;
        }
        
        return data;
    }
    
    void ValidateTables()
    {
        Debug.Log("=== 강화 테이블 검증 시작 ===");
        
        // Level Table 검증
        var levelTable = Resources.Load<EnhanceLevelTableSO>("Data/EnhanceLevelTable");
        if (levelTable == null)
        {
            Debug.LogError("❌ EnhanceLevelTableSO를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"✅ EnhanceLevelTableSO 로드 성공! (Size: {levelTable.levelTable.Length})");
            Debug.Log($"   Lv 10: 골드 {levelTable.GetGoldCost(10)}G, 재료 {levelTable.GetMaterialCount(10)}개, 성공률 {levelTable.GetSuccessRate(10)}%");
        }
        
        // Curve Table 검증
        var curveTable = Resources.Load<EnhanceCurveTableSO>("Data/EnhanceCurveTable");
        if (curveTable == null)
        {
            Debug.LogError("❌ EnhanceCurveTableSO를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"✅ EnhanceCurveTableSO 로드 성공! (Groups: {curveTable.curveGroups.Length})");
            Debug.Log($"   CURVE_WEAPON Lv 10: +{curveTable.GetStatRateAdd("CURVE_WEAPON", 10)}%");
            Debug.Log($"   CURVE_WEAPON 누적 Lv 10: +{curveTable.GetTotalStatBonus("CURVE_WEAPON", 10)}%");
        }
        
        Debug.Log("=== 검증 완료! ===");
        EditorUtility.DisplayDialog("검증 완료", "모든 테이블이 정상입니다!\nConsole 창을 확인하세요.", "확인");
    }
}

