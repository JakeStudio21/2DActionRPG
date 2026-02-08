# 🎒 캐릭터 가방 통합 시스템 가이드

> **완료 날짜**: 2026-02-08  
> **목적**: 인게임 임시 저장소 (장비 + 재료 통합 표시)

---

## 🎯 시스템 개요

### **캐릭터 가방의 역할**
```
인게임 임시 저장소
├─ 목적: "이번 스테이지에서 뭘 획득했지?" 한눈에 확인
├─ 표시: 장비 + 재료 통합 (탭 없이)
├─ 정렬: 장비 먼저 → 재료 나중 → 이름순
└─ 생명주기: 스테이지 입장 ~ 클리어 (로비 복귀 시 초기화)

로비 보관창고 (영구 저장소)
├─ 장비 탭: sharedInventoryIds (계정 공유)
├─ 재료 탭: AccountDataManager.materials (계정 공유)
└─ 자동 분류: 스테이지 클리어 시 타입별로 자동 저장
```

---

## 📊 데이터 흐름

### **인게임 플레이**
```
몬스터 처치
  ↓
아이템 드롭
  ├─ 장비 → PlayerDataManager.characterBagInstanceIds
  └─ 재료 → PlayerDataManager.characterBagMaterials
  ↓
OnCharacterBagChanged 이벤트 발생
  ↓
ActiveInventory.RefreshInventoryUI()
  ↓
✅ 한눈에 확인: [무기 A] [방어구 B] [파편 x3] [결정 x1]
```

### **스테이지 클리어**
```
클리어 UI
  ↓
PlayerDataManager.TransferCharacterBagToStorage() 호출
  ├─ 장비 → AccountDataManager.sharedInventoryIds
  └─ 재료 → AccountDataManager.materials
  ↓
characterBag 초기화
  ↓
로비 복귀
```

### **로비 보관창고**
```
가방 버튼 클릭
  ↓
LobbyInventoryUI 열림
  ├─ 장비 탭: sharedInventoryIds 표시
  └─ 재료 탭: AccountDataManager.materials 표시
  ↓
✅ 타입별로 나누어진 영구 저장소!
```

---

## 🛠️ 구현된 기능

### **Phase 1: 데이터 구조** ✅

**PlayerSlotData.cs**:
```csharp
public List<ItemInstanceId> characterBagInstanceIds; // 장비 임시
public List<MaterialStack> characterBagMaterials;    // 재료 임시
```

**PlayerDataManager.cs**:
```csharp
// 재료 추가
public void AddMaterialToCharacterBag(MaterialType type, int amount);

// 장비 가져오기
public List<EquipmentData> GetCharacterBagItems();

// 가방 초기화
public void ClearCharacterBag();

// 자동 전송
public void TransferCharacterBagToStorage();

// 이벤트
public event Action OnCharacterBagChanged;
```

---

### **Phase 2: 재료 획득** ✅

**MaterialPickup.cs**:
```csharp
// ❌ 이전 (계정 저장소로 직접)
AccountDataManager.Instance.AddMaterial(materialType, amount);

// ✅ 변경 (캐릭터 가방으로)
PlayerDataManager.Instance.AddMaterialToCharacterBag(materialType, amount);
```

**결과**:
- 재료 획득 시 캐릭터 가방에 임시 저장
- OnCharacterBagChanged 이벤트 발생
- UI 자동 갱신

---

### **Phase 3: 통합 표시** ✅

**ActiveInventory.cs**:
```csharp
private void RefreshInventoryUI()
{
    // 1. 장비 + 재료 통합 목록 생성
    var displayItems = new List<InventoryDisplayItem>();
    
    // 장비 추가
    var equipments = PlayerDataManager.Instance.GetCharacterBagItems();
    foreach (var equip in equipments)
    {
        displayItems.Add(new InventoryDisplayItem
        {
            type = ItemDisplayType.Equipment,
            equipmentData = equip,
            sortOrder = 1 // 장비가 먼저
        });
    }
    
    // 재료 추가
    var materials = slotData.characterBagMaterials;
    foreach (var mat in materials)
    {
        displayItems.Add(new InventoryDisplayItem
        {
            type = ItemDisplayType.Material,
            materialStack = mat,
            sortOrder = 2 // 재료가 그 다음
        });
    }
    
    // 2. 정렬 (타입별 → 이름순)
    displayItems.Sort(...);
    
    // 3. 슬롯에 표시
    for (int i = 0; i < maxDisplaySlots; i++)
    {
        if (i < displayItems.Count)
        {
            if (item.type == ItemDisplayType.Equipment)
                slot.SetEquipmentData(item.equipmentData);
            else if (item.type == ItemDisplayType.Material)
                slot.SetupMaterial(item.materialStack);
        }
        else
        {
            slot.ClearSlot();
        }
    }
}
```

**결과**:
- 장비와 재료가 한 화면에 통합 표시
- 탭 없이 한눈에 확인 가능
- 정렬: 장비 → 재료 → 이름순

---

### **Phase 4: 자동 전송** ✅

**PlayerDataManager.cs**:
```csharp
public void TransferCharacterBagToStorage()
{
    // 1. 장비 전송
    foreach (var instanceId in characterBagInstanceIds)
    {
        AccountDataManager.Instance.AddToSharedInventory(instanceId);
    }
    
    // 2. 재료 전송
    foreach (var mat in characterBagMaterials)
    {
        AccountDataManager.Instance.AddMaterial(mat.materialType, mat.count);
    }
    
    // 3. 가방 초기화
    characterBagInstanceIds.Clear();
    characterBagMaterials.Clear();
    
    // 4. 저장
    SaveOnMeaningfulEvent("CharacterBagTransferred");
}
```

---

## 📋 호출 방법

### **스테이지 클리어 시** (필수 구현)

**방법 1: FSMStageController.cs**
```csharp
private void OnStageCleared()
{
    // 캐릭터 가방 → 보관창고 자동 전송
    PlayerDataManager.Instance.TransferCharacterBagToStorage();
    
    // 로비로 복귀
    LoadLobby();
}
```

**방법 2: StageManager.cs**
```csharp
public void CompleteStage()
{
    // 스테이지 완료 처리
    // ...
    
    // 캐릭터 가방 자동 전송
    PlayerDataManager.Instance.TransferCharacterBagToStorage();
    
    // 클리어 UI 표시
    ShowClearUI();
}
```

**방법 3: 클리어 UI 버튼**
```csharp
public void OnReturnToLobbyButtonClicked()
{
    // 캐릭터 가방 자동 전송
    PlayerDataManager.Instance.TransferCharacterBagToStorage();
    
    // 로비로 이동
    SceneManager.LoadScene("Lobby");
}
```

---

### **로비 진입 시** (선택 구현)

**LobbyController.cs**:
```csharp
private void Start()
{
    // 혹시 모를 잔여 데이터 초기화
    PlayerDataManager.Instance.ClearCharacterBag();
}
```

---

## 🧪 테스트 체크리스트

### **인게임 테스트**
- [ ] 장비 획득 시 캐릭터 가방에 표시됨
- [ ] 재료 획득 시 캐릭터 가방에 표시됨
- [ ] 장비 + 재료가 통합 표시됨 (탭 없이)
- [ ] 정렬 순서: 장비 먼저 → 재료 나중
- [ ] I키로 가방 열기/닫기 정상 작동

### **전송 테스트**
- [ ] 스테이지 클리어 시 TransferCharacterBagToStorage() 호출
- [ ] 로비 복귀 후 캐릭터 가방 비어있음
- [ ] 로비 보관창고 장비 탭에 장비 이동됨
- [ ] 로비 보관창고 재료 탭에 재료 이동됨
- [ ] JSON 파일에 저장 확인

### **로비 테스트**
- [ ] 보관창고 장비 탭에 장비 표시됨
- [ ] 보관창고 재료 탭에 재료 표시됨
- [ ] 재입장 시 데이터 유지됨

---

## 🔍 디버그 가이드

### **문제 1: 재료가 캐릭터 가방에 안 보임**

**확인 사항**:
1. MaterialPickup에서 `AddMaterialToCharacterBag()` 호출하는지 확인
2. `OnCharacterBagChanged` 이벤트가 발생하는지 로그 확인
3. ActiveInventory가 이벤트를 구독했는지 확인

**로그**:
```
📦 [CharacterBag] 재료 추가: 무기 강화 파편 +3 (총: 3개)
🔄 [ActiveInventory] 캐릭터 가방 UI 새로고침 시작
📦 [ActiveInventory] 슬롯 2: 재료 - 무기 강화 파편 x3
```

---

### **문제 2: 스테이지 클리어 후 재료가 사라짐**

**확인 사항**:
1. `TransferCharacterBagToStorage()` 호출되는지 확인
2. AccountDataManager에 재료가 추가되는지 확인
3. 로비 복귀 후 보관창고 재료 탭 확인

**로그**:
```
📦 [TransferCharacterBag] 재료 전송: 무기 강화 파편 x3
✅ [TransferCharacterBag] 전송 완료 - 장비: 2개, 재료: 1개
🧹 [CharacterBag] 초기화 완료 (장비: 2개, 재료: 1개)
```

---

### **문제 3: 로비에서 재료가 안 보임**

**확인 사항**:
1. AccountDataManager에 재료가 저장되어 있는지 확인
2. LobbyInventoryUI의 재료 탭이 정상 작동하는지 확인
3. JSON 파일에 materials 필드가 있는지 확인

**로그**:
```
✅ [AccountDataManager] 재료 추가: WeaponFragment +3 (총: 3개)
📦 [LobbyInventoryUI] 재료 슬롯 설정: 무기 강화 파편 x3
```

---

## ✅ 최종 데이터 구조

### **인게임 (캐릭터 가방)**
```json
{
  "characterBagInstanceIds": [
    "item_abc123", // 장비 1
    "item_def456"  // 장비 2
  ],
  "characterBagMaterials": [
    { "materialType": "WeaponFragment", "count": 3 },
    { "materialType": "ArmorCrystal", "count": 1 }
  ]
}
```

### **로비 (보관창고)**
```json
{
  "sharedInventoryIds": [
    "item_abc123", // 장비 (전송됨)
    "item_def456"
  ],
  "materials": [
    { "materialType": "WeaponFragment", "count": 3 }, // 재료 (전송됨)
    { "materialType": "ArmorCrystal", "count": 1 }
  ]
}
```

---

## 📌 참고 파일

- `Assets/Scripts/Systems/PlayerSlotData.cs` - 데이터 구조
- `Assets/Scripts/Managers/PlayerDataManager.cs` - 가방 관리
- `Assets/Scripts/Items/MaterialPickup.cs` - 재료 획득
- `Assets/Scripts/UI/Inventory/ActiveInventory.cs` - 통합 표시
- `Assets/Scripts/Managers/AccountDataManager.cs` - 영구 저장

---

**🎉 구현 완료!** 이제 캐릭터 가방에서 장비와 재료를 한눈에 확인할 수 있습니다!

