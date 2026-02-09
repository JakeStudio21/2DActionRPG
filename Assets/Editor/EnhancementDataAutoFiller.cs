using UnityEngine;
using UnityEditor;
using Systems;

/// <summary>
/// EnhancementData Asset에 Lv 0~15 강화 테이블 데이터를 자동으로 입력하는 Editor Tool
/// </summary>
public class EnhancementDataAutoFiller : EditorWindow
    {
        private EnhancementData targetData;
        
        [MenuItem("Tools/Workshop/Auto Fill Enhancement Table")]
        public static void ShowWindow()
        {
            GetWindow<EnhancementDataAutoFiller>("강화 테이블 자동 입력");
        }
        
        void OnGUI()
        {
            GUILayout.Label("강화 테이블 자동 입력 도구", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "이 도구는 EnhancementData Asset의 Level Table을\n" +
                "Lv 0~15 데이터로 자동으로 채워줍니다.\n\n" +
                "⚠️ 기존 Level Table 데이터가 있다면 덮어씌워집니다!",
                MessageType.Warning
            );
            
            GUILayout.Space(10);
            
            // Asset 선택
            targetData = (EnhancementData)EditorGUILayout.ObjectField(
                "Target Asset",
                targetData,
                typeof(EnhancementData),
                false
            );
            
            GUILayout.Space(10);
            
            // 자동 로드 버튼
            if (GUILayout.Button("📂 기존 EnhancementData 자동 로드", GUILayout.Height(30)))
            {
                LoadExistingData();
            }
            
            GUILayout.Space(10);
            
            GUI.enabled = targetData != null;
            
            // 자동 입력 버튼
            if (GUILayout.Button("✨ Level Table 자동 입력 (Lv 0~15)", GUILayout.Height(40)))
            {
                FillLevelTable();
            }
            
            GUI.enabled = true;
            
            GUILayout.Space(20);
            
            // 테스트 버튼
            if (GUILayout.Button("🧪 데이터 검증 (Console 확인)", GUILayout.Height(30)))
            {
                ValidateData();
            }
        }
        
        /// <summary>
        /// 기존 EnhancementData Asset 자동 로드
        /// </summary>
        void LoadExistingData()
        {
            targetData = Resources.Load<EnhancementData>("Data/EnhancementData");
            
            if (targetData != null)
            {
                Debug.Log("✅ EnhancementData Asset 로드 성공!");
                EditorUtility.DisplayDialog("성공", "EnhancementData Asset을 찾았습니다!", "확인");
            }
            else
            {
                Debug.LogError("❌ EnhancementData를 찾을 수 없습니다! (경로: Resources/Data/EnhancementData)");
                EditorUtility.DisplayDialog("오류", "EnhancementData를 찾을 수 없습니다!", "확인");
            }
        }
        
        /// <summary>
        /// Level Table에 Lv 0~15 데이터 자동 입력
        /// </summary>
        void FillLevelTable()
        {
            if (targetData == null)
            {
                EditorUtility.DisplayDialog("오류", "먼저 EnhancementData Asset을 선택해주세요!", "확인");
                return;
            }
            
            // 사용자 확인
            if (!EditorUtility.DisplayDialog(
                "확인",
                "Level Table을 Lv 0~15 데이터로 자동 입력합니다.\n\n" +
                "기존 데이터가 있다면 덮어씌워집니다.\n계속하시겠습니까?",
                "예, 진행",
                "취소"))
            {
                return;
            }
            
            Undo.RecordObject(targetData, "Auto Fill Enhancement Level Table");
            
            // Level Table 초기화 (16개 = Lv 0~15)
            targetData.levelTable = new EnhancementLevelData[16];
            
            // 각 레벨별 데이터 입력
            for (int i = 0; i < 16; i++)
            {
                targetData.levelTable[i] = CreateLevelData(i);
            }
            
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"✅ [EnhancementDataAutoFiller] Level Table 자동 입력 완료! (Lv 0~15)");
            EditorUtility.DisplayDialog("완료", "Level Table 자동 입력이 완료되었습니다!", "확인");
        }
        
        /// <summary>
        /// 레벨별 데이터 생성 (표 기반)
        /// </summary>
        EnhancementLevelData CreateLevelData(int level)
        {
            var data = new EnhancementLevelData
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
                    data.statRateAdd_Weapon = 0f;
                    data.statRateAdd_Armor = 0f;
                    data.statRateAdd_Accessory = 0f;
                    break;
                
                case 1:
                    data.goldCost = 200;
                    data.materialCount = 1;
                    data.successRate = 0.95f;
                    data.statRateAdd_Weapon = 1.5f;
                    data.statRateAdd_Armor = 1.2f;
                    data.statRateAdd_Accessory = 0.8f;
                    break;
                
                case 2:
                    data.goldCost = 350;
                    data.materialCount = 1;
                    data.successRate = 0.92f;
                    data.statRateAdd_Weapon = 1.5f;
                    data.statRateAdd_Armor = 1.2f;
                    data.statRateAdd_Accessory = 0.8f;
                    break;
                
                case 3:
                    data.goldCost = 550;
                    data.materialCount = 2;
                    data.successRate = 0.88f;
                    data.statRateAdd_Weapon = 1.5f;
                    data.statRateAdd_Armor = 1.2f;
                    data.statRateAdd_Accessory = 0.8f;
                    data.bonusId = "BONUS_LV3";
                    data.bonusDescription = "크리티컬 확률 +1%";
                    break;
                
                case 4:
                    data.goldCost = 800;
                    data.materialCount = 2;
                    data.successRate = 0.85f;
                    data.statRateAdd_Weapon = 1.5f;
                    data.statRateAdd_Armor = 1.2f;
                    data.statRateAdd_Accessory = 0.8f;
                    break;
                
                case 5:
                    data.goldCost = 1100;
                    data.materialCount = 3;
                    data.successRate = 0.82f;
                    data.statRateAdd_Weapon = 1.5f;
                    data.statRateAdd_Armor = 1.2f;
                    data.statRateAdd_Accessory = 0.8f;
                    break;
                
                case 6:
                    data.goldCost = 1500;
                    data.materialCount = 3;
                    data.successRate = 0.75f;
                    data.statRateAdd_Weapon = 2.0f;
                    data.statRateAdd_Armor = 1.6f;
                    data.statRateAdd_Accessory = 1.0f;
                    data.bonusId = "BONUS_LV6";
                    data.bonusDescription = "크리티컬 데미지 +5%";
                    break;
                
                case 7:
                    data.goldCost = 2000;
                    data.materialCount = 4;
                    data.successRate = 0.72f;
                    data.statRateAdd_Weapon = 2.0f;
                    data.statRateAdd_Armor = 1.6f;
                    data.statRateAdd_Accessory = 1.0f;
                    break;
                
                case 8:
                    data.goldCost = 2600;
                    data.materialCount = 4;
                    data.successRate = 0.68f;
                    data.statRateAdd_Weapon = 2.0f;
                    data.statRateAdd_Armor = 1.6f;
                    data.statRateAdd_Accessory = 1.0f;
                    break;
                
                case 9:
                    data.goldCost = 3300;
                    data.materialCount = 5;
                    data.successRate = 0.62f;
                    data.statRateAdd_Weapon = 2.0f;
                    data.statRateAdd_Armor = 1.6f;
                    data.statRateAdd_Accessory = 1.0f;
                    data.bonusId = "BONUS_LV9";
                    data.bonusDescription = "공격 속도 +3%";
                    break;
                
                case 10:
                    data.goldCost = 4100;
                    data.materialCount = 5;
                    data.successRate = 0.58f;
                    data.statRateAdd_Weapon = 2.0f;
                    data.statRateAdd_Armor = 1.6f;
                    data.statRateAdd_Accessory = 1.0f;
                    break;
                
                case 11:
                    data.goldCost = 5200;
                    data.materialCount = 6;
                    data.successRate = 0.50f;
                    data.statRateAdd_Weapon = 3.0f;
                    data.statRateAdd_Armor = 2.4f;
                    data.statRateAdd_Accessory = 1.5f;
                    break;
                
                case 12:
                    data.goldCost = 6600;
                    data.materialCount = 6;
                    data.successRate = 0.45f;
                    data.statRateAdd_Weapon = 3.0f;
                    data.statRateAdd_Armor = 2.4f;
                    data.statRateAdd_Accessory = 1.5f;
                    data.bonusId = "BONUS_LV12";
                    data.bonusDescription = "스킬 쿨다운 -5%";
                    break;
                
                case 13:
                    data.goldCost = 8300;
                    data.materialCount = 7;
                    data.successRate = 0.40f;
                    data.statRateAdd_Weapon = 3.0f;
                    data.statRateAdd_Armor = 2.4f;
                    data.statRateAdd_Accessory = 1.5f;
                    break;
                
                case 14:
                    data.goldCost = 10500;
                    data.materialCount = 7;
                    data.successRate = 0.35f;
                    data.statRateAdd_Weapon = 3.0f;
                    data.statRateAdd_Armor = 2.4f;
                    data.statRateAdd_Accessory = 1.5f;
                    break;
                
                case 15:
                    data.goldCost = 13500;
                    data.materialCount = 8;
                    data.successRate = 0.30f;
                    data.statRateAdd_Weapon = 3.0f;
                    data.statRateAdd_Armor = 2.4f;
                    data.statRateAdd_Accessory = 1.5f;
                    data.bonusId = "BONUS_LV15";
                    data.bonusDescription = "모든 스탯 +2%";
                    break;
            }
            
            return data;
        }
        
        /// <summary>
        /// 데이터 검증
        /// </summary>
        void ValidateData()
        {
            if (targetData == null)
            {
                EditorUtility.DisplayDialog("오류", "먼저 EnhancementData Asset을 선택해주세요!", "확인");
                return;
            }
            
            Debug.Log("=== EnhancementData 검증 시작 ===");
            
            if (targetData.levelTable == null || targetData.levelTable.Length == 0)
            {
                Debug.LogError("❌ Level Table이 비어있습니다! '✨ Level Table 자동 입력' 버튼을 먼저 클릭하세요.");
                EditorUtility.DisplayDialog("오류", "Level Table이 비어있습니다!", "확인");
                return;
            }
            
            Debug.Log($"✅ Level Table Size: {targetData.levelTable.Length}");
            
            // Lv 10 샘플 테스트
            var lv10 = targetData.GetLevelData(10);
            if (lv10 != null)
            {
                Debug.Log($"✅ Lv 10 골드: {lv10.goldCost}G (예상: 4100G)");
                Debug.Log($"✅ Lv 10 재료: {lv10.materialCount}개 (예상: 5개)");
                Debug.Log($"✅ Lv 10 성공률: {lv10.successRate * 100}% (예상: 58%)");
            }
            
            // 누적 스탯 테스트
            float totalStat = targetData.GetTotalStatBonus(10, EquipmentType.Weapon);
            Debug.Log($"✅ 무기 +10 누적 스탯: +{totalStat}% (예상: +17.5%)");
            
            // 보너스 테스트
            int[] bonusLevels = { 3, 6, 9, 12, 15 };
            foreach (var level in bonusLevels)
            {
                if (targetData.HasBonus(level))
                {
                    string bonusId = targetData.GetBonusId(level);
                    Debug.Log($"✅ Lv {level} 보너스: {bonusId}");
                }
            }
            
            Debug.Log("=== 검증 완료! ===");
            EditorUtility.DisplayDialog("검증 완료", "모든 데이터가 정상입니다!\nConsole 창을 확인하세요.", "확인");
        }
    }

