using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 스킬 시스템 테스트 헬퍼 (Phase 1)
/// Unity Editor에서 스킬 시스템을 빠르게 테스트하기 위한 도구
/// </summary>
public class SkillSystemTestHelper : EditorWindow
{
    private GameObject playerObject;
    private PlayerSkillManager skillManager;
    private PlayerRuntimeStats runtimeStats;
    
    [MenuItem("Tools/Skill System/Test Helper")]
    public static void ShowWindow()
    {
        GetWindow<SkillSystemTestHelper>("스킬 시스템 테스트");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("🧪 스킬 시스템 Phase 1 테스트", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Play 모드 체크
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ Play 모드에서만 테스트 가능합니다!\n▶️ 버튼을 눌러 Play 모드로 진입하세요.", MessageType.Warning);
            
            if (GUILayout.Button("▶️ Play 모드 진입", GUILayout.Height(40)))
            {
                EditorApplication.isPlaying = true;
            }
            return;
        }
        
        // 플레이어 오브젝트 선택
        playerObject = EditorGUILayout.ObjectField("Player GameObject", playerObject, typeof(GameObject), true) as GameObject;
        
        if (playerObject == null)
        {
            EditorGUILayout.HelpBox("테스트할 Player GameObject를 선택해주세요.", MessageType.Info);
            return;
        }
        
        // 컴포넌트 찾기
        if (GUILayout.Button("컴포넌트 찾기"))
        {
            FindComponents();
        }
        
        GUILayout.Space(10);
        
        if (skillManager == null || runtimeStats == null)
        {
            EditorGUILayout.HelpBox("PlayerSkillManager 또는 PlayerRuntimeStats가 없습니다.\n컴포넌트를 추가하거나 '컴포넌트 찾기'를 눌러주세요.", MessageType.Warning);
            
            if (GUILayout.Button("필수 컴포넌트 자동 추가"))
            {
                AddRequiredComponents();
            }
            return;
        }
        
        // 현재 상태 표시
        GUILayout.Label("📊 현재 상태", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("보유 액티브 스킬", skillManager.unlockedActiveSkills.Count.ToString());
        EditorGUILayout.LabelField("보유 패시브 스킬", skillManager.unlockedPassiveSkills.Count.ToString());
        EditorGUILayout.LabelField("사용가능 SP", $"{skillManager.AvailableSP}/{skillManager.totalSP}");
        
        GUILayout.Space(10);
        
        // 테스트 버튼들
        GUILayout.Label("🧪 빠른 테스트", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. '정령의 공명' 패시브 추가 및 장착"))
        {
            TestSpiritResonancePassive();
        }
        
        if (GUILayout.Button("2. SP 10 지급"))
        {
            skillManager.AddSP(10);
            EditorUtility.SetDirty(skillManager);
        }
        
        if (GUILayout.Button("3. 현재 스탯 출력"))
        {
            PrintCurrentStats();
        }
        
        if (GUILayout.Button("4. 모든 패시브 스탯 재적용"))
        {
            skillManager.ApplyAllPassiveStats();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("⚠️ 테스트 데이터 초기화"))
        {
            ResetTestData();
        }
    }
    
    private void FindComponents()
    {
        skillManager = playerObject.GetComponent<PlayerSkillManager>();
        runtimeStats = playerObject.GetComponent<PlayerRuntimeStats>();
        
        if (skillManager == null)
            Debug.LogWarning("❌ PlayerSkillManager 없음");
        
        if (runtimeStats == null)
            Debug.LogWarning("❌ PlayerRuntimeStats 없음");
    }
    
    private void AddRequiredComponents()
    {
        if (skillManager == null)
        {
            skillManager = playerObject.AddComponent<PlayerSkillManager>();
        }
        
        if (runtimeStats == null)
        {
            runtimeStats = playerObject.GetComponent<PlayerRuntimeStats>();
            if (runtimeStats == null)
            {
                Debug.LogWarning("⚠️ PlayerRuntimeStats는 수동으로 추가해주세요.");
            }
        }
        
        EditorUtility.SetDirty(playerObject);
    }
    
    private void TestSpiritResonancePassive()
    {
        // '정령의 공명' SO 로드
        string path = "Assets/Resources/Skills/Passive/PassiveSkill_SpiritResonance";
        PassiveSkillData passiveData = AssetDatabase.LoadAssetAtPath<PassiveSkillData>(path + ".asset");
        
        if (passiveData == null)
        {
            Debug.LogError($"❌ PassiveSkill_SpiritResonance.asset를 찾을 수 없습니다!\n경로: {path}");
            EditorUtility.DisplayDialog("에러", "'정령의 공명' SO를 찾을 수 없습니다.\n수동으로 생성해주세요.", "확인");
            return;
        }
        
        // SkillInstance 생성 (레벨 1)
        var skillInstance = new SkillInstance(passiveData, 1);
        
        // unlockedPassiveSkills에 추가
        if (!skillManager.unlockedPassiveSkills.Exists(s => s.skillData.skillID == passiveData.skillID))
        {
            skillManager.unlockedPassiveSkills.Add(skillInstance);
        }
        
        // 패시브 슬롯 0에 장착
        bool equipped = skillManager.EquipPassiveSkill(skillInstance, 0);
        if (equipped)
        {
        }
        
        EditorUtility.SetDirty(skillManager);
        
        // 스탯 출력
        PrintCurrentStats();
    }
    
    private void PrintCurrentStats()
    {
        if (runtimeStats == null) return;
    }
    
    private void ResetTestData()
    {
        if (!EditorUtility.DisplayDialog("경고", "테스트 데이터를 초기화하시겠습니까?", "초기화", "취소"))
            return;
        
        skillManager.unlockedActiveSkills.Clear();
        skillManager.unlockedPassiveSkills.Clear();
        skillManager.equippedActiveSkills.Clear();
        skillManager.equippedPassiveSkills.Clear();
        skillManager.totalSP = 0;
        skillManager.usedSP = 0;
        
        EditorUtility.SetDirty(skillManager);
    }
}
