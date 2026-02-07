using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase 0: 기반 데이터 구조 테스트
/// Unity Editor에서 [Tools → Phase 0 Test] 메뉴로 실행
/// </summary>
public class Phase0_DataStructureTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool showDetailedLogs = true;
    
    [ContextMenu("Phase 0 전체 테스트 실행")]
    public void RunAllTests()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧪 Phase 0: 기반 데이터 구조 테스트 시작");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        int passCount = 0;
        int totalCount = 9;
        
        // Test 1: ItemInstanceId
        if (Test1_ItemInstanceId()) passCount++;
        
        // Test 2: ItemInstanceId Equals/GetHashCode
        if (Test2_ItemInstanceIdEquality()) passCount++;
        
        // Test 3: ItemInstanceData
        if (Test3_ItemInstanceData()) passCount++;
        
        // Test 4: EquippedRecord
        if (Test4_EquippedRecord()) passCount++;
        
        // Test 5: ItemBindRecord
        if (Test5_ItemBindRecord()) passCount++;
        
        // Test 6: MaterialStack
        if (Test6_MaterialStack()) passCount++;
        
        // Test 7: AccountData JSON 직렬화
        if (Test7_AccountDataSerialization()) passCount++;
        
        // Test 8: PlayerSlotData V2 필드
        if (Test8_PlayerSlotDataV2Fields()) passCount++;
        
        // Test 9: Dictionary 키 사용
        if (Test9_DictionaryKeyUsage()) passCount++;
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"🎯 테스트 결과: {passCount}/{totalCount} 통과");
        if (passCount == totalCount)
            Debug.Log("✅ Phase 0 테스트 100% 성공!");
        else
            Debug.LogError($"❌ Phase 0 테스트 실패: {totalCount - passCount}개 실패");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    // ========================================
    // Test 1: ItemInstanceId 생성/유효성
    // ========================================
    private bool Test1_ItemInstanceId()
    {
        Debug.Log("\n--- Test 1: ItemInstanceId 생성/유효성 ---");
        
        try
        {
            // 새 ID 생성
            var id1 = ItemInstanceId.NewId();
            var id2 = ItemInstanceId.NewId();
            
            // 유효성 확인
            if (!id1.IsValid())
            {
                Debug.LogError("❌ Test 1 실패: 생성된 ID가 유효하지 않음");
                return false;
            }
            
            // 고유성 확인
            if (id1.id == id2.id)
            {
                Debug.LogError("❌ Test 1 실패: 생성된 ID가 고유하지 않음");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ ID1: {id1}");
                Debug.Log($"✅ ID2: {id2}");
            }
            
            Debug.Log("✅ Test 1 통과: ItemInstanceId 생성/유효성");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 1 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 2: ItemInstanceId Equals/GetHashCode
    // ========================================
    private bool Test2_ItemInstanceIdEquality()
    {
        Debug.Log("\n--- Test 2: ItemInstanceId Equals/GetHashCode ---");
        
        try
        {
            var id1 = ItemInstanceId.NewId();
            var id2 = new ItemInstanceId { id = id1.id }; // 동일한 ID
            var id3 = ItemInstanceId.NewId(); // 다른 ID
            
            // Equals 테스트
            if (!id1.Equals(id2))
            {
                Debug.LogError("❌ Test 2 실패: 동일한 ID가 Equals에서 false 반환");
                return false;
            }
            
            if (id1.Equals(id3))
            {
                Debug.LogError("❌ Test 2 실패: 다른 ID가 Equals에서 true 반환");
                return false;
            }
            
            // GetHashCode 테스트
            if (id1.GetHashCode() != id2.GetHashCode())
            {
                Debug.LogError("❌ Test 2 실패: 동일한 ID의 HashCode가 다름");
                return false;
            }
            
            // 연산자 오버로딩 테스트
            if (!(id1 == id2))
            {
                Debug.LogError("❌ Test 2 실패: == 연산자 오버로딩 실패");
                return false;
            }
            
            if (!(id1 != id3))
            {
                Debug.LogError("❌ Test 2 실패: != 연산자 오버로딩 실패");
                return false;
            }
            
            Debug.Log("✅ Test 2 통과: Equals/GetHashCode 정상 작동");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 2 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 3: ItemInstanceData
    // ========================================
    private bool Test3_ItemInstanceData()
    {
        Debug.Log("\n--- Test 3: ItemInstanceData ---");
        
        try
        {
            var id = ItemInstanceId.NewId();
            var instanceData = new ItemInstanceData
            {
                instanceId = id,
                templateName = "Sword_S_Equipment",
                enhancementLevel = 5,
                enhancementAttempts = 10,
                customName = "전설의 검",
                acquiredTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                craftedFromRecipeId = -1
            };
            
            // ToString 확인
            string description = instanceData.ToString();
            if (!description.Contains("전설의 검") || !description.Contains("+5"))
            {
                Debug.LogError("❌ Test 3 실패: ToString 출력 오류");
                return false;
            }
            
            if (showDetailedLogs)
                Debug.Log($"✅ ItemInstanceData: {description}");
            
            Debug.Log("✅ Test 3 통과: ItemInstanceData 정상 작동");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 3 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 4: EquippedRecord
    // ========================================
    private bool Test4_EquippedRecord()
    {
        Debug.Log("\n--- Test 4: EquippedRecord ---");
        
        try
        {
            var id = ItemInstanceId.NewId();
            var record = new EquippedRecord
            {
                slot = EquipmentSlot.MainWeapon,
                instanceId = id
            };
            
            // ToString 확인
            string description = record.ToString();
            if (!description.Contains("MainWeapon"))
            {
                Debug.LogError("❌ Test 4 실패: ToString 출력 오류");
                return false;
            }
            
            if (showDetailedLogs)
                Debug.Log($"✅ EquippedRecord: {description}");
            
            Debug.Log("✅ Test 4 통과: EquippedRecord 정상 작동");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 4 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 5: ItemBindRecord
    // ========================================
    private bool Test5_ItemBindRecord()
    {
        Debug.Log("\n--- Test 5: ItemBindRecord ---");
        
        try
        {
            var id = ItemInstanceId.NewId();
            var bindRecord = new ItemBindRecord
            {
                instanceId = id,
                characterSlotIndex = 0,
                bindTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            // ToString 확인
            string description = bindRecord.ToString();
            if (!description.Contains("Slot 0"))
            {
                Debug.LogError("❌ Test 5 실패: ToString 출력 오류");
                return false;
            }
            
            if (showDetailedLogs)
                Debug.Log($"✅ ItemBindRecord: {description}");
            
            Debug.Log("✅ Test 5 통과: ItemBindRecord 정상 작동");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 5 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 6: MaterialStack
    // ========================================
    private bool Test6_MaterialStack()
    {
        Debug.Log("\n--- Test 6: MaterialStack ---");
        
        try
        {
            var material = new MaterialStack
            {
                materialType = MaterialType.WeaponFragment,
                count = 100
            };
            
            // ToString 확인
            string description = material.ToString();
            if (!description.Contains("FRAGMENT_ENHANCE") || !description.Contains("100"))
            {
                Debug.LogError("❌ Test 6 실패: ToString 출력 오류");
                return false;
            }
            
            if (showDetailedLogs)
                Debug.Log($"✅ MaterialStack: {description}");
            
            Debug.Log("✅ Test 6 통과: MaterialStack 정상 작동");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 6 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 7: AccountData JSON 직렬화
    // ========================================
    private bool Test7_AccountDataSerialization()
    {
        Debug.Log("\n--- Test 7: AccountData JSON 직렬화 ---");
        
        try
        {
            // AccountData 생성
            var accountData = new AccountData();
            
            // 테스트 데이터 추가
            var id1 = ItemInstanceId.NewId();
            var id2 = ItemInstanceId.NewId();
            
            accountData.sharedInventoryIds.Add(id1);
            accountData.mailboxIds.Add(id2);
            
            var instanceData = new ItemInstanceData
            {
                instanceId = id1,
                templateName = "Sword_A_Equipment",
                enhancementLevel = 3
            };
            accountData.itemInstances.Add(instanceData);
            
            var bindRecord = new ItemBindRecord
            {
                instanceId = id1,
                characterSlotIndex = 0,
                bindTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            accountData.binds.Add(bindRecord);
            
            var material = new MaterialStack
            {
                materialType = MaterialType.ArmorCrystal,
                count = 50
            };
            accountData.materials.Add(material);
            
            // JSON 직렬화
            string json = JsonUtility.ToJson(accountData, true);
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("❌ Test 7 실패: JSON 직렬화 실패");
                return false;
            }
            
            // JSON 역직렬화
            var loadedData = JsonUtility.FromJson<AccountData>(json);
            
            if (loadedData.itemInstances.Count != 1 ||
                loadedData.binds.Count != 1 ||
                loadedData.materials.Count != 1)
            {
                Debug.LogError("❌ Test 7 실패: JSON 역직렬화 후 데이터 불일치");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ AccountData JSON:\n{json}");
                Debug.Log($"✅ Loaded: {loadedData}");
            }
            
            Debug.Log("✅ Test 7 통과: AccountData JSON 직렬화/역직렬화 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 7 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 8: PlayerSlotData V2 필드
    // ========================================
    private bool Test8_PlayerSlotDataV2Fields()
    {
        Debug.Log("\n--- Test 8: PlayerSlotData V2 필드 ---");
        
        try
        {
            var slotData = new PlayerSlotData
            {
                slotIndex = 0,
                playerName = "TestPlayer",
                playerType = PlayerType.Warrior
            };
            
            // V2 필드 추가
            var id1 = ItemInstanceId.NewId();
            var id2 = ItemInstanceId.NewId();
            
            slotData.characterBagInstanceIds.Add(id1);
            slotData.characterBagInstanceIds.Add(id2);
            
            var record = new EquippedRecord
            {
                slot = EquipmentSlot.MainWeapon,
                instanceId = id1
            };
            slotData.equippedRecords.Add(record);
            
            // JSON 직렬화
            string json = slotData.ToJson();
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("❌ Test 8 실패: PlayerSlotData JSON 직렬화 실패");
                return false;
            }
            
            // JSON 역직렬화
            var loadedSlot = PlayerSlotData.FromJson(json);
            
            if (loadedSlot.characterBagInstanceIds.Count != 2 ||
                loadedSlot.equippedRecords.Count != 1)
            {
                Debug.LogError("❌ Test 8 실패: V2 필드 데이터 불일치");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ PlayerSlotData V2: Bag={loadedSlot.characterBagInstanceIds.Count}, Equipped={loadedSlot.equippedRecords.Count}");
            }
            
            Debug.Log("✅ Test 8 통과: PlayerSlotData V2 필드 정상 작동");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 8 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 9: Dictionary 키 사용
    // ========================================
    private bool Test9_DictionaryKeyUsage()
    {
        Debug.Log("\n--- Test 9: Dictionary 키 사용 ---");
        
        try
        {
            var dict = new Dictionary<ItemInstanceId, ItemInstanceData>();
            
            var id1 = ItemInstanceId.NewId();
            var id2 = ItemInstanceId.NewId();
            
            var data1 = new ItemInstanceData { instanceId = id1, templateName = "Item1" };
            var data2 = new ItemInstanceData { instanceId = id2, templateName = "Item2" };
            
            // Dictionary 추가
            dict[id1] = data1;
            dict[id2] = data2;
            
            // Dictionary 조회
            if (!dict.TryGetValue(id1, out var retrievedData))
            {
                Debug.LogError("❌ Test 9 실패: Dictionary 조회 실패");
                return false;
            }
            
            if (retrievedData.templateName != "Item1")
            {
                Debug.LogError("❌ Test 9 실패: Dictionary 데이터 불일치");
                return false;
            }
            
            // 동일 ID로 재생성하여 조회
            var id1Copy = new ItemInstanceId { id = id1.id };
            if (!dict.ContainsKey(id1Copy))
            {
                Debug.LogError("❌ Test 9 실패: 동일 ID로 Dictionary 조회 실패 (Equals/GetHashCode 문제)");
                return false;
            }
            
            if (showDetailedLogs)
                Debug.Log($"✅ Dictionary 크기: {dict.Count}, 조회 성공: {retrievedData.templateName}");
            
            Debug.Log("✅ Test 9 통과: Dictionary 키로 정상 작동");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 9 실패: {e.Message}");
            return false;
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Unity Editor 메뉴 추가
/// </summary>
public static class Phase0TestMenu
{
    [UnityEditor.MenuItem("Tools/Phase 0 Test")]
    public static void RunPhase0Test()
    {
        // 씬에서 테스트 오브젝트 찾기
        var tester = Object.FindObjectOfType<Phase0_DataStructureTest>();
        
        if (tester == null)
        {
            // 없으면 생성
            var go = new GameObject("Phase0_Tester");
            tester = go.AddComponent<Phase0_DataStructureTest>();
        }
        
        tester.RunAllTests();
    }
}
#endif

