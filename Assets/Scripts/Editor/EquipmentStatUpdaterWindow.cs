using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 📊 장비 스탯 자동 업데이트 에디터 창
/// 드래그 앤 드롭으로 장비를 선택하고 스탯을 자동 계산/업데이트
/// </summary>
public class EquipmentStatUpdaterWindow : EditorWindow
{
    #region Fields

    private StatBudgetSettings budgetSettings;
    private EquipmentData targetEquipment;
    
    // 스크롤
    private Vector2 scrollPosition;
    
    // 스탯 배분 (Key: StatId, Value: 할당 예산)
    private Dictionary<string, float> statAllocations = new Dictionary<string, float>();
    
    // 허용 스탯 리스트
    private List<string> allowedStats = new List<string>();
    
    // 계산된 정보
    private float totalBudget = 0f;
    private float usedBudget = 0f;
    private float remainingBudget = 0f;

    #endregion

    #region Window Setup

    [MenuItem("Tools/Equipment Stat Updater")]
    public static void ShowWindow()
    {
        var window = GetWindow<EquipmentStatUpdaterWindow>("장비 스탯 업데이트");
        window.minSize = new Vector2(500, 600);
    }

    #endregion

    #region GUI

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawSettingsSection();
        DrawEquipmentSection();
        
        if (targetEquipment != null && budgetSettings != null)
        {
            DrawBudgetInfo();
            DrawStatAllocationSection();
            DrawActionButtons();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 헤더
    /// </summary>
    private void DrawHeader()
    {
        GUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("📊 장비 스탯 자동 업데이트 툴", headerStyle);
        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. StatBudgetSettings를 할당하세요.\n" +
            "2. 업데이트할 EquipmentData를 드래그하세요.\n" +
            "3. 스탯 배분을 조정하고 [업데이트] 버튼을 누르세요.",
            MessageType.Info
        );
        GUILayout.Space(10);
    }

    /// <summary>
    /// 설정 섹션
    /// </summary>
    private void DrawSettingsSection()
    {
        EditorGUILayout.LabelField("⚙️ 밸런스 설정", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        budgetSettings = (StatBudgetSettings)EditorGUILayout.ObjectField(
            "Budget Settings",
            budgetSettings,
            typeof(StatBudgetSettings),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            RefreshAllowedStats();
        }
        GUILayout.Space(10);
    }

    /// <summary>
    /// 장비 선택 섹션
    /// </summary>
    private void DrawEquipmentSection()
    {
        EditorGUILayout.LabelField("🎒 대상 장비", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        targetEquipment = (EquipmentData)EditorGUILayout.ObjectField(
            "Target Equipment",
            targetEquipment,
            typeof(EquipmentData),
            false
        );
        if (EditorGUI.EndChangeCheck())
        {
            OnEquipmentChanged();
        }

        if (targetEquipment != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"등급: {targetEquipment.itemGrade}");
            
            // 무기 타입 세부 정보 표시
            string typeInfo = targetEquipment.equipmentType.ToString();
            if (targetEquipment.equipmentType == EquipmentType.Weapon)
            {
                typeInfo += $" ({targetEquipment.WeaponType})";
            }
            EditorGUILayout.LabelField($"타입: {typeInfo}");
            
            EditorGUILayout.LabelField($"슬롯: {targetEquipment.equipmentSlot}");
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(10);
    }

    /// <summary>
    /// 예산 정보
    /// </summary>
    private void DrawBudgetInfo()
    {
        EditorGUILayout.LabelField("💰 예산 정보", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"총 예산: {totalBudget:F2}");
        EditorGUILayout.LabelField($"사용 예산: {usedBudget:F2}");
        
        // 남은 예산 색상 표시
        Color originalColor = GUI.contentColor;
        if (remainingBudget < 0)
            GUI.contentColor = Color.red;
        else if (remainingBudget > 0)
            GUI.contentColor = Color.yellow;
        else
            GUI.contentColor = Color.green;
        
        EditorGUILayout.LabelField($"남은 예산: {remainingBudget:F2}");
        GUI.contentColor = originalColor;
        
        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    /// <summary>
    /// 스탯 배분 섹션
    /// </summary>
    private void DrawStatAllocationSection()
    {
        EditorGUILayout.LabelField("📊 스탯 배분", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        if (allowedStats.Count == 0)
        {
            EditorGUILayout.HelpBox("이 장비 타입에 사용 가능한 스탯이 없습니다.", MessageType.Warning);
        }
        else
        {
            foreach (string statId in allowedStats)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 스탯 이름
                EditorGUILayout.LabelField(statId, GUILayout.Width(150));
                
                // 예산 입력
                float currentAllocation = statAllocations.ContainsKey(statId) ? statAllocations[statId] : 0f;
                float newAllocation = EditorGUILayout.FloatField(currentAllocation, GUILayout.Width(100));
                
                if (newAllocation != currentAllocation)
                {
                    statAllocations[statId] = Mathf.Max(0, newAllocation);
                    RecalculateBudget();
                }
                
                // 예상 수치
                if (newAllocation > 0 && budgetSettings != null)
                {
                    float statValue = BalanceCalculator.CalculateStatValue(statId, newAllocation, budgetSettings);
                    EditorGUILayout.LabelField($"→ {statValue:F2}", GUILayout.Width(100));
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(10);
    }

    /// <summary>
    /// 액션 버튼
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        // 자동 배분 버튼
        if (GUILayout.Button("🎲 자동 배분 (균등)", GUILayout.Height(30)))
        {
            AutoAllocateBudget();
        }
        
        // 초기화 버튼
        if (GUILayout.Button("🔄 초기화", GUILayout.Height(30)))
        {
            ResetAllocations();
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // 업데이트 버튼 (큰 버튼)
        GUI.enabled = remainingBudget >= 0;
        if (GUILayout.Button("✅ 장비 스탯 업데이트", GUILayout.Height(50)))
        {
            UpdateEquipmentStats();
        }
        GUI.enabled = true;
        
        if (remainingBudget < 0)
        {
            EditorGUILayout.HelpBox("예산을 초과했습니다! 스탯 배분을 조정하세요.", MessageType.Error);
        }
    }

    #endregion

    #region Logic

    /// <summary>
    /// 장비 변경 시 호출
    /// </summary>
    private void OnEquipmentChanged()
    {
        if (targetEquipment == null || budgetSettings == null) return;

        // 총 예산 계산
        totalBudget = BalanceCalculator.CalculateSlotBudget(
            targetEquipment.itemGrade,
            targetEquipment.equipmentSlot,
            budgetSettings
        );

        // 허용 스탯 갱신
        RefreshAllowedStats();

        // 배분 초기화
        ResetAllocations();
    }

    /// <summary>
    /// 허용 스탯 리스트 갱신
    /// </summary>
    private void RefreshAllowedStats()
    {
        allowedStats.Clear();
        
        if (targetEquipment == null)
        {
            Debug.LogWarning("⚠️ [EquipmentStatUpdater] targetEquipment가 null입니다!");
            return;
        }
        
        if (budgetSettings == null)
        {
            Debug.LogError("❌ [EquipmentStatUpdater] budgetSettings가 null입니다! StatBudgetSettings를 할당해주세요.");
            return;
        }

        // 무기 타입(Bow/Sword/Staff)과 방어구/악세사리를 구분
        string equipmentKey = BalanceCalculator.GetEquipmentKey(targetEquipment);
        Debug.Log($"🔍 [EquipmentStatUpdater] 장비 키: {equipmentKey}");
        
        // AllowedStatsPerType 디버깅
        var allowedStatsDict = budgetSettings.AllowedStatsPerType;
        Debug.Log($"🔍 [EquipmentStatUpdater] AllowedStatsPerType 개수: {allowedStatsDict.Count}");
        foreach (var key in allowedStatsDict.Keys)
        {
            Debug.Log($"   - Key: '{key}', 스탯 개수: {allowedStatsDict[key].Count}");
        }
        
        allowedStats = BalanceCalculator.GetAllowedStats(equipmentKey, budgetSettings);
        Debug.Log($"✅ [EquipmentStatUpdater] '{equipmentKey}' 허용 스탯: {allowedStats.Count}개");
        
        if (allowedStats.Count == 0)
        {
            Debug.LogWarning($"⚠️ [EquipmentStatUpdater] '{equipmentKey}'에 대한 허용 스탯이 없습니다!");
            Debug.LogWarning($"   StatBudgetSettings에서 [Parse CSV Data] 버튼을 눌러 CSV를 파싱해주세요!");
        }
    }

    /// <summary>
    /// 예산 재계산
    /// </summary>
    private void RecalculateBudget()
    {
        usedBudget = 0f;
        foreach (var allocation in statAllocations.Values)
        {
            usedBudget += allocation;
        }
        remainingBudget = totalBudget - usedBudget;
    }

    /// <summary>
    /// 자동 예산 배분
    /// </summary>
    private void AutoAllocateBudget()
    {
        if (allowedStats.Count == 0) return;

        statAllocations = BalanceCalculator.AutoAllocateBudget(totalBudget, allowedStats, 3);
        RecalculateBudget();
    }

    /// <summary>
    /// 배분 초기화
    /// </summary>
    private void ResetAllocations()
    {
        statAllocations.Clear();
        RecalculateBudget();
    }

    /// <summary>
    /// 장비 스탯 업데이트 실행
    /// </summary>
    private void UpdateEquipmentStats()
    {
        if (targetEquipment == null || budgetSettings == null)
        {
            Debug.LogError("❌ [EquipmentStatUpdater] targetEquipment 또는 budgetSettings가 null입니다.");
            return;
        }

        // 배분되지 않은 스탯 제거
        var filteredAllocations = new Dictionary<string, float>();
        foreach (var allocation in statAllocations)
        {
            if (allocation.Value > 0)
                filteredAllocations[allocation.Key] = allocation.Value;
        }

        // 스탯 계산
        List<ItemStat> newStats = BalanceCalculator.CalculateAllStats(filteredAllocations, budgetSettings);

        // EquipmentData에 적용
        targetEquipment.baseStats = newStats;

        // 변경사항 저장
        EditorUtility.SetDirty(targetEquipment);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ [EquipmentStatUpdater] {targetEquipment.equipmentName}의 스탯이 업데이트되었습니다!");
        Debug.Log($"   - 총 예산: {totalBudget}");
        Debug.Log($"   - 사용 예산: {usedBudget}");
        Debug.Log($"   - 생성된 스탯: {newStats.Count}개");

        EditorUtility.DisplayDialog(
            "스탯 업데이트 완료",
            $"{targetEquipment.equipmentName}의 스탯이 업데이트되었습니다!\n\n" +
            $"총 {newStats.Count}개의 스탯이 생성되었습니다.",
            "확인"
        );
    }

    #endregion
}

