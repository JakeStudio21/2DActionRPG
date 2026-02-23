using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase 3 장비 시스템 통합 테스트
/// Unity Editor에서 실행 가능한 테스트 스크립트
/// </summary>
public class Phase3_EquipmentSystemTest : MonoBehaviour
{
    [Header("테스트 대상")]
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private PlayerRuntimeStats playerRuntimeStats;
    
    [Header("테스트 설정")]
    [SerializeField] private bool autoRunOnStart = false;
    [SerializeField] private bool detailedLogs = true;
    
    [Header("테스트용 장비 데이터 (Inspector에서 할당)")]
    [SerializeField] private EquipmentData testWeapon;
    [SerializeField] private EquipmentData testArmor;
    [SerializeField] private EquipmentData testAccessory;
    
    private void Start()
    {
        if (autoRunOnStart)
        {
            RunAllTests();
        }
    }
    
    /// <summary>
    /// 전체 테스트 실행 (Inspector 버튼용)
    /// </summary>
    [ContextMenu("🧪 Run All Tests")]
    public void RunAllTests()
    {
        Debug.Log("=================================================");
        Debug.Log("🧪 Phase 3 장비 시스템 통합 테스트 시작");
        Debug.Log("=================================================\n");
        
        // 초기화 확인
        if (!ValidateTestSetup())
        {
            Debug.LogError("❌ 테스트 설정이 올바르지 않습니다. 테스트 중단.");
            return;
        }
        
        // 테스트 실행
        Test1_ItemDatabaseInitialization();
        Test2_EquipmentInstanceCreation();
        Test3_StatModifierGeneration();
        Test4_EquipmentEquip();
        Test5_EnhancementScaling();
        Test6_EquipmentUnequip();
        Test7_BindingSystem();
        
        Debug.Log("\n=================================================");
        Debug.Log("✅ Phase 3 장비 시스템 통합 테스트 완료!");
        Debug.Log("=================================================");
    }
    
    #region Setup Validation
    
    /// <summary>
    /// 테스트 설정 검증
    /// </summary>
    private bool ValidateTestSetup()
    {
        bool isValid = true;
        
        // EquipmentManager 확인
        if (equipmentManager == null)
        {
            equipmentManager = FindObjectOfType<EquipmentManager>();
            if (equipmentManager == null)
            {
                Debug.LogError("❌ EquipmentManager를 찾을 수 없습니다!");
                isValid = false;
            }
        }
        
        // PlayerRuntimeStats 확인
        if (playerRuntimeStats == null)
        {
            playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
            if (playerRuntimeStats == null)
            {
                Debug.LogError("❌ PlayerRuntimeStats를 찾을 수 없습니다!");
                isValid = false;
            }
        }
        
        // ItemDatabase 초기화 확인
        int equipmentCount = ItemDatabase.EquipmentCount;
        if (equipmentCount == 0)
        {
            Debug.LogWarning("⚠️ ItemDatabase에 장비 데이터가 없습니다. Resources/Data/EquipmentData/ 폴더를 확인하세요.");
        }
        else
        {
            Debug.Log($"✅ ItemDatabase 초기화 완료 - {equipmentCount}개 장비 데이터");
        }
        
        return isValid;
    }
    
    #endregion
    
    #region Test Cases
    
    /// <summary>
    /// Test 1: ItemDatabase 초기화 및 로딩
    /// </summary>
    [ContextMenu("Test 1: ItemDatabase 초기화")]
    private void Test1_ItemDatabaseInitialization()
    {
        Debug.Log("\n--- Test 1: ItemDatabase 초기화 ---");
        
        int count = ItemDatabase.EquipmentCount;
        Debug.Log($"총 {count}개 EquipmentData 로드됨");
        
        if (detailedLogs)
        {
            foreach (var equipment in ItemDatabase.GetAllEquipment())
            {
                Debug.Log($"  - {equipment.equipmentName} (Grade: {equipment.itemGrade}, Type: {equipment.equipmentType})");
            }
        }
        
        Debug.Log("✅ Test 1 완료\n");
    }
    
    /// <summary>
    /// Test 2: EquipmentInstance 생성
    /// </summary>
    [ContextMenu("Test 2: EquipmentInstance 생성")]
    private void Test2_EquipmentInstanceCreation()
    {
        Debug.Log("\n--- Test 2: EquipmentInstance 생성 ---");
        
        // 테스트용 장비 선택
        EquipmentData testData = testWeapon != null ? testWeapon : GetFirstEquipmentOfType(EquipmentType.Weapon);
        
        if (testData == null)
        {
            Debug.LogWarning("⚠️ 테스트용 장비 데이터를 찾을 수 없습니다.");
            return;
        }
        
        // EquipmentInstance 생성
        EquipmentInstance instance = new EquipmentInstance(
            ItemInstanceID.Generate(),
            testData,
            enhanceLevel: 0
        );
        
        Debug.Log($"생성된 인스턴스: {instance}");
        Debug.Log($"  - instanceId: {instance.instanceId}");
        Debug.Log($"  - equipmentDataName: {instance.equipmentDataName}");
        Debug.Log($"  - enhanceLevel: {instance.enhanceLevel}");
        Debug.Log($"  - isBound: {instance.isBound}");
        
        Debug.Log("✅ Test 2 완료\n");
    }
    
    /// <summary>
    /// Test 3: StatModifier 생성
    /// </summary>
    [ContextMenu("Test 3: StatModifier 생성")]
    private void Test3_StatModifierGeneration()
    {
        Debug.Log("\n--- Test 3: StatModifier 생성 ---");
        
        EquipmentData testData = testWeapon != null ? testWeapon : GetFirstEquipmentOfType(EquipmentType.Weapon);
        
        if (testData == null)
        {
            Debug.LogWarning("⚠️ 테스트용 장비 데이터를 찾을 수 없습니다.");
            return;
        }
        
        EquipmentInstance instance = new EquipmentInstance(
            ItemInstanceID.Generate(),
            testData,
            enhanceLevel: 0
        );
        
        List<StatModifier> modifiers = instance.GetStatModifiers();
        
        Debug.Log($"{testData.equipmentName}의 StatModifier 목록 (총 {modifiers.Count}개):");
        foreach (var mod in modifiers)
        {
            Debug.Log($"  - {mod.statType}: {mod.value} ({mod.unit}) | Source: {mod.source}");
        }
        
        Debug.Log("✅ Test 3 완료\n");
    }
    
    /// <summary>
    /// Test 4: 장비 착용 및 스탯 적용
    /// </summary>
    [ContextMenu("Test 4: 장비 착용")]
    private void Test4_EquipmentEquip()
    {
        Debug.Log("\n--- Test 4: 장비 착용 및 스탯 적용 ---");
        
        // 초기 스탯 기록
        float initialAttack = playerRuntimeStats.FinalAttackDamage;
        float initialDefense = playerRuntimeStats.FinalDefense;
        float initialCritRate = playerRuntimeStats.FinalCriticalChance;
        
        Debug.Log($"초기 스탯:");
        Debug.Log($"  - 공격력: {initialAttack:F1}");
        Debug.Log($"  - 방어력: {initialDefense:F1}");
        Debug.Log($"  - 치명확률: {initialCritRate:F2}");
        
        // 장비 착용
        EquipmentData testData = testWeapon != null ? testWeapon : GetFirstEquipmentOfType(EquipmentType.Weapon);
        
        if (testData == null)
        {
            Debug.LogWarning("⚠️ 테스트용 장비 데이터를 찾을 수 없습니다.");
            return;
        }
        
        EquipmentInstance instance = new EquipmentInstance(
            ItemInstanceID.Generate(),
            testData,
            enhanceLevel: 0
        );
        
        bool success = equipmentManager.EquipItem(EquipmentSlot.MainWeapon, instance);
        
        if (!success)
        {
            Debug.LogError("❌ 장비 착용 실패");
            return;
        }
        
        // 변경된 스탯 확인
        float newAttack = playerRuntimeStats.FinalAttackDamage;
        float newDefense = playerRuntimeStats.FinalDefense;
        float newCritRate = playerRuntimeStats.FinalCriticalChance;
        
        Debug.Log($"\n장비 착용 후 스탯:");
        Debug.Log($"  - 공격력: {newAttack:F1} (변화: +{newAttack - initialAttack:F1})");
        Debug.Log($"  - 방어력: {newDefense:F1} (변화: +{newDefense - initialDefense:F1})");
        Debug.Log($"  - 치명확률: {newCritRate:F2} (변화: +{newCritRate - initialCritRate:F2})");
        
        Debug.Log("✅ Test 4 완료\n");
    }
    
    /// <summary>
    /// Test 5: 강화 레벨별 스탯 스케일링
    /// </summary>
    [ContextMenu("Test 5: 강화 스케일링")]
    private void Test5_EnhancementScaling()
    {
        Debug.Log("\n--- Test 5: 강화 레벨별 스탯 스케일링 ---");
        
        EquipmentData testData = testWeapon != null ? testWeapon : GetFirstEquipmentOfType(EquipmentType.Weapon);
        
        if (testData == null)
        {
            Debug.LogWarning("⚠️ 테스트용 장비 데이터를 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log($"장비: {testData.equipmentName}");
        Debug.Log($"기본 공격력: {testData.attackDamage}");
        Debug.Log($"\n강화 레벨별 스탯:");
        
        for (int enhanceLevel = 0; enhanceLevel <= 10; enhanceLevel += 2)
        {
            EquipmentInstance instance = new EquipmentInstance(
                ItemInstanceID.Generate(),
                testData,
                enhanceLevel: enhanceLevel
            );
            
            List<StatModifier> modifiers = instance.GetStatModifiers();
            StatModifier atkMod = modifiers.Find(m => m.statType == EStatType.ATK_FLAT);
            
            if (atkMod != null)
            {
                float expectedValue = testData.attackDamage * (1f + enhanceLevel * 0.1f);
                Debug.Log($"  +{enhanceLevel}강: 공격력 {atkMod.value:F1} (예상: {expectedValue:F1})");
            }
        }
        
        Debug.Log("✅ Test 5 완료\n");
    }
    
    /// <summary>
    /// Test 6: 장비 해제
    /// </summary>
    [ContextMenu("Test 6: 장비 해제")]
    private void Test6_EquipmentUnequip()
    {
        Debug.Log("\n--- Test 6: 장비 해제 ---");
        
        // 현재 장착된 장비 확인
        EquipmentInstance equipped = equipmentManager.GetEquippedItem(EquipmentSlot.MainWeapon);
        
        if (equipped == null)
        {
            Debug.LogWarning("⚠️ MainWeapon 슬롯에 장착된 장비가 없습니다. Test 4를 먼저 실행하세요.");
            return;
        }
        
        // 해제 전 스탯
        float beforeAttack = playerRuntimeStats.FinalAttackDamage;
        
        // 장비 해제
        bool success = equipmentManager.UnequipItem(EquipmentSlot.MainWeapon);
        
        if (!success)
        {
            Debug.LogError("❌ 장비 해제 실패");
            return;
        }
        
        // 해제 후 스탯
        float afterAttack = playerRuntimeStats.FinalAttackDamage;
        
        Debug.Log($"장비 해제 완료");
        Debug.Log($"  - 해제 전 공격력: {beforeAttack:F1}");
        Debug.Log($"  - 해제 후 공격력: {afterAttack:F1}");
        Debug.Log($"  - 변화: {afterAttack - beforeAttack:F1}");
        
        Debug.Log("✅ Test 6 완료\n");
    }
    
    /// <summary>
    /// Test 7: 귀속 시스템 (Grade 6 이상)
    /// </summary>
    [ContextMenu("Test 7: 귀속 시스템")]
    private void Test7_BindingSystem()
    {
        Debug.Log("\n--- Test 7: 귀속 시스템 테스트 ---");
        
        // Grade S 이상 장비 찾기 (귀속 대상)
        EquipmentData highGradeEquipment = null;
        foreach (var equipment in ItemDatabase.GetAllEquipment())
        {
            if (equipment.itemGrade >= ItemGrade.S)
            {
                highGradeEquipment = equipment;
                break;
            }
        }
        
        if (highGradeEquipment == null)
        {
            Debug.LogWarning("⚠️ Grade S 이상 장비를 찾을 수 없습니다. 테스트용 장비를 사용합니다.");
            
            // 테스트용으로 임의 장비 사용
            highGradeEquipment = testWeapon != null ? testWeapon : GetFirstEquipmentOfType(EquipmentType.Weapon);
            
            if (highGradeEquipment == null)
            {
                Debug.LogError("❌ 테스트용 장비를 찾을 수 없습니다.");
                return;
            }
        }
        
        Debug.Log($"테스트 장비: {highGradeEquipment.equipmentName} (Grade: {highGradeEquipment.itemGrade})");
        
        // 귀속 전 인스턴스
        EquipmentInstance instance = new EquipmentInstance(
            ItemInstanceID.Generate(),
            highGradeEquipment,
            enhanceLevel: 0
        );
        
        Debug.Log($"초기 귀속 상태: {instance.isBound}");
        
        // Grade S 미만이면 경고 (테스트용)
        if (highGradeEquipment.itemGrade < ItemGrade.S)
        {
            Debug.Log("⚠️ 테스트 장비의 Grade가 S 미만입니다. 귀속이 발생하지 않을 수 있습니다.");
            // Note: EquipmentManager에서 Grade S 이상만 귀속 처리
        }
        
        // 장착 (귀속 발생)
        // Note: EquipmentManager.EquipItem()에서 Grade 6 이상이면 자동으로 귀속됨
        bool equipped = equipmentManager.EquipItem(EquipmentSlot.MainWeapon, instance);
        
        if (equipped)
        {
            Debug.Log($"장착 후 귀속 상태: {instance.isBound}");
            
            if (instance.isBound)
            {
                Debug.Log("✅ 귀속 시스템 정상 작동!");
            }
            else
            {
                Debug.LogWarning("⚠️ 장착했지만 귀속되지 않음 (Grade S 미만일 수 있음)");
            }
        }
        
        Debug.Log("✅ Test 7 완료\n");
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 특정 타입의 첫 번째 장비 가져오기
    /// </summary>
    private EquipmentData GetFirstEquipmentOfType(EquipmentType type)
    {
        foreach (var equipment in ItemDatabase.GetAllEquipment())
        {
            if (equipment.equipmentType == type)
            {
                return equipment;
            }
        }
        return null;
    }
    
    /// <summary>
    /// 현재 PlayerRuntimeStats 상태 출력
    /// </summary>
    [ContextMenu("📊 현재 스탯 출력")]
    public void PrintCurrentStats()
    {
        if (playerRuntimeStats == null)
        {
            Debug.LogWarning("⚠️ PlayerRuntimeStats를 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log("=== 현재 플레이어 스탯 ===");
        Debug.Log($"공격력: {playerRuntimeStats.FinalAttackDamage:F1}");
        Debug.Log($"방어력: {playerRuntimeStats.FinalDefense:F1}");
        Debug.Log($"체력: {playerRuntimeStats.FinalMaxHealth:F0}");
        Debug.Log($"치명확률: {playerRuntimeStats.FinalCriticalChance:F2}");
        Debug.Log($"치명데미지: {playerRuntimeStats.FinalCriticalDamage:F2}");
        Debug.Log($"이동속도: {playerRuntimeStats.FinalMoveSpeed:F2}");
    }
    
    /// <summary>
    /// 장착된 모든 장비 출력
    /// </summary>
    [ContextMenu("🎒 장착 장비 목록")]
    public void PrintEquippedItems()
    {
        if (equipmentManager == null)
        {
            Debug.LogWarning("⚠️ EquipmentManager를 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log("=== 현재 장착 중인 장비 ===");
        
        var equipped = equipmentManager.GetAllEquippedItems();
        
        if (equipped.Count == 0)
        {
            Debug.Log("장착된 장비가 없습니다.");
            return;
        }
        
        foreach (var kvp in equipped)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
    }
    
    #endregion
}

