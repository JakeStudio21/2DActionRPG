# 🎮 ScenePoolConfig 업데이트 가이드

## 🎯 목표
기존 개별 아이템 풀 (38개) → 범용 프리팹 풀 (6개)로 전환

---

## ❌ 삭제할 풀 (각 씬마다)

### **재화 아이템 풀 (2개)**
```
- ITEM_HEALTH_POTION
- ITEM_GOLD_COIN
```

### **장비 아이템 풀 (36개)**

**무기 풀 (12개):**
```
- Bow_A, Bow_B, Bow_C, Bow_S
- Sword_A, Sword_B, Sword_C, Sword_S
- Staff_A, Staff_B, Staff_C, Staff_S
```

**방어구 풀 (24개):**
```
Warrior:
- ITEM_ARMOR_WARRIOR_A/B/C/S
- ITEM_BOOTS_WARRIOR_A/B/C/S

Assasin:
- ITEM_ARMOR_ASSASIN_A/B/C/S
- ITEM_BOOTS_ASSASIN_A/B/C/S

Wizard:
- ITEM_ARMOR_WIZARD_A/B/C/S
- ITEM_BOOTS_WIZARD_A/B/C/S
```

---

## ✅ 추가할 풀 (각 씬마다)

### **신규 풀 설정 (6개)**

#### **1. Drop_Currency**
```
Tag: Drop_Currency
Prefab: Assets/Prefabs/Pickup/Drop_Currency.prefab
Size: 10
Preload On Scene Start: ✅ 체크
Clear On Scene Exit: ✅ 체크
Max Instances Per Frame: 20
```

#### **2. Drop_Equipment**
```
Tag: Drop_Equipment
Prefab: Assets/Prefabs/Pickup/Drop_Equipment.prefab
Size: 20
Preload On Scene Start: ✅ 체크
Clear On Scene Exit: ✅ 체크
Max Instances Per Frame: 20
```

#### **3. EquipmentFX_D (White)**
```
Tag: EquipmentFX_D
Prefab: Assets/Prefabs/VFX/Equipment/EquipmentFX_D.prefab
Size: 5
Preload On Scene Start: ✅ 체크
Clear On Scene Exit: ✅ 체크
Max Instances Per Frame: 10
```

#### **4. EquipmentFX_C (Green)**
```
Tag: EquipmentFX_C
Prefab: Assets/Prefabs/VFX/Equipment/EquipmentFX_C.prefab
Size: 5
Preload On Scene Start: ✅ 체크
Clear On Scene Exit: ✅ 체크
Max Instances Per Frame: 10
```

#### **5. EquipmentFX_B (Blue)**
```
Tag: EquipmentFX_B
Prefab: Assets/Prefabs/VFX/Equipment/EquipmentFX_B.prefab
Size: 5
Preload On Scene Start: ✅ 체크
Clear On Scene Exit: ✅ 체크
Max Instances Per Frame: 10
```

#### **6. EquipmentFX_A (Purple)**
```
Tag: EquipmentFX_A
Prefab: Assets/Prefabs/VFX/Equipment/EquipmentFX_A.prefab
Size: 5
Preload On Scene Start: ✅ 체크
Clear On Scene Exit: ✅ 체크
Max Instances Per Frame: 10
```

---

## 📋 Unity Editor 작업 순서

### **Step 1: ScenePoolConfig 에셋 찾기**

```
Project 창:
Assets/Resources/Stages/ScenePools/

파일 목록:
- CH01_ST01_PoolConfig.asset
- CH01_ST02_PoolConfig.asset
- CH01_ST03_PoolConfig.asset
- (기타 스테이지 풀 설정)
```

---

### **Step 2: 각 ScenePoolConfig 에셋 열기**

#### **2-1. CH01_ST01_PoolConfig.asset 선택**
```
Inspector 창에서 Required Pools 섹션 확인
```

#### **2-2. 기존 아이템 풀 삭제**
```
Required Pools 목록에서:
1. ITEM_HEALTH_POTION 찾기 → 우클릭 → Delete
2. ITEM_GOLD_COIN 찾기 → 우클릭 → Delete
3. Bow_A/B/C/S 찾기 → 우클릭 → Delete
4. Sword_A/B/C/S 찾기 → 우클릭 → Delete
5. (기타 장비 풀들 모두 삭제)
```

#### **2-3. 신규 풀 6개 추가**
```
Required Pools 섹션에서:
1. 리스트 하단 + 버튼 클릭
2. 새 항목이 생성됨

항목 1:
- Tag: Drop_Currency
- Prefab: Drop_Currency 프리팹 드래그 앤 드롭
- Size: 10
- Preload On Scene Start: ✅
- Clear On Scene Exit: ✅
- Max Instances Per Frame: 20

항목 2~6: 위 "추가할 풀" 섹션 참고하여 입력
```

#### **2-4. 저장**
```
Ctrl+S (또는 File → Save)
```

---

### **Step 3: 다른 씬 PoolConfig도 동일하게 작업**

```
같은 작업을 모든 스테이지 PoolConfig에 반복:
- CH01_ST02_PoolConfig.asset
- CH01_ST03_PoolConfig.asset
- CH01_ST04_PoolConfig.asset
- ...
```

---

## 🔧 자동화 스크립트 (선택사항)

대량의 ScenePoolConfig를 수정해야 한다면 Unity Editor 스크립트를 사용할 수 있습니다.

### **BulkUpdateScenePoolConfig.cs (에디터 스크립트)**

```csharp
// Assets/Editor/BulkUpdateScenePoolConfig.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BulkUpdateScenePoolConfig : EditorWindow
{
    [MenuItem("Tools/Item Drop/Bulk Update Scene Pool Configs")]
    public static void ShowWindow()
    {
        GetWindow<BulkUpdateScenePoolConfig>("Bulk Update Pool Configs");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Bulk Update Scene Pool Configs", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Remove Old Item Pools & Add New Pools"))
        {
            UpdateAllPoolConfigs();
        }
    }
    
    private void UpdateAllPoolConfigs()
    {
        // 1. 모든 ScenePoolConfig 찾기
        string[] guids = AssetDatabase.FindAssets("t:ScenePoolConfig", new[] { "Assets/Resources/Stages/ScenePools" });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ScenePoolConfig config = AssetDatabase.LoadAssetAtPath<ScenePoolConfig>(path);
            
            if (config != null)
            {
                Debug.Log($"🔄 [BulkUpdate] 처리 중: {config.sceneName}");
                
                // 2. 기존 아이템 풀 제거
                RemoveOldItemPools(config);
                
                // 3. 신규 풀 추가
                AddNewPools(config);
                
                // 4. 저장
                EditorUtility.SetDirty(config);
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("✅ [BulkUpdate] 모든 ScenePoolConfig 업데이트 완료!");
    }
    
    private void RemoveOldItemPools(ScenePoolConfig config)
    {
        // 삭제할 태그 목록
        List<string> tagsToRemove = new List<string>
        {
            "ITEM_HEALTH_POTION", "ITEM_GOLD_COIN",
            "Bow_A", "Bow_B", "Bow_C", "Bow_S",
            "Sword_A", "Sword_B", "Sword_C", "Sword_S",
            "Staff_A", "Staff_B", "Staff_C", "Staff_S",
            // 방어구 추가...
        };
        
        config.requiredPools = config.requiredPools
            .Where(pool => !tagsToRemove.Contains(pool.tag))
            .ToList();
    }
    
    private void AddNewPools(ScenePoolConfig config)
    {
        // 신규 풀 6개 추가
        AddPool(config, "Drop_Currency", "Prefabs/Pickup/Drop_Currency", 10);
        AddPool(config, "Drop_Equipment", "Prefabs/Pickup/Drop_Equipment", 20);
        AddPool(config, "EquipmentFX_D", "Prefabs/VFX/Equipment/EquipmentFX_D", 5);
        AddPool(config, "EquipmentFX_C", "Prefabs/VFX/Equipment/EquipmentFX_C", 5);
        AddPool(config, "EquipmentFX_B", "Prefabs/VFX/Equipment/EquipmentFX_B", 5);
        AddPool(config, "EquipmentFX_A", "Prefabs/VFX/Equipment/EquipmentFX_A", 5);
    }
    
    private void AddPool(ScenePoolConfig config, string tag, string prefabPath, int size)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            Debug.LogWarning($"⚠️ [BulkUpdate] 프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }
        
        var poolSetting = new ScenePoolConfig.PoolSettings
        {
            tag = tag,
            prefab = prefab,
            size = size,
            preloadOnSceneStart = true,
            clearOnSceneExit = true,
            maxInstancesPerFrame = size * 2
        };
        
        config.requiredPools.Add(poolSetting);
        Debug.Log($"✅ [BulkUpdate] 풀 추가: {tag}");
    }
}
```

---

## ✅ 완료 체크리스트

### **각 씬마다:**
- [ ] 기존 재화 풀 2개 삭제 (ITEM_GOLD_COIN, ITEM_HEALTH_POTION)
- [ ] 기존 장비 풀 36개 삭제
- [ ] Drop_Currency 풀 추가
- [ ] Drop_Equipment 풀 추가
- [ ] EquipmentFX_D 풀 추가
- [ ] EquipmentFX_C 풀 추가
- [ ] EquipmentFX_B 풀 추가
- [ ] EquipmentFX_A 풀 추가
- [ ] 저장 (Ctrl+S)

### **전체:**
- [ ] 모든 스테이지 PoolConfig 업데이트 완료
- [ ] Unity 에디터 재시작 (선택사항)
- [ ] 씬 로드 테스트 (풀 로딩 로그 확인)

---

## 🎯 다음 단계

ScenePoolConfig 업데이트가 완료되면:
1. Unity에서 스테이지 씬 로드
2. Console에서 풀 로딩 로그 확인
3. 몬스터 처치 후 아이템 드롭 테스트
4. Drop_Currency, Drop_Equipment 프리팹 정상 작동 확인

---

## 💡 팁

1. **백업 생성**:
   - 수정 전에 Assets/Resources/Stages/ScenePools/ 폴더를 백업하세요
   - Git 커밋을 먼저 하는 것을 권장합니다

2. **자동화 스크립트 사용**:
   - 스테이지가 많다면 위의 에디터 스크립트를 사용하세요
   - `Tools → Item Drop → Bulk Update Scene Pool Configs` 메뉴

3. **검증**:
   - 각 씬 로드 후 Console에서 "풀 로딩 성공" 로그를 확인하세요
   - 풀 태그 오타가 있으면 런타임 에러가 발생합니다

