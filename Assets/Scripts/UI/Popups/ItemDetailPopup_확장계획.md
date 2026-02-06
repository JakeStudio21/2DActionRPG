# 🔧 ItemDetailPopup 다른 UI 확장 계획

## 📋 현재 상황

### ✅ 구현 완료
- **보관창고 (LobbyInventoryUI)**:
  - InventorySlot 클릭 → `PlayerDataManager.OnSlotClicked` 이벤트
  - ItemDetailPopup.Show(data, **ItemDetailContext.Inventory**, slotIndex, instanceId)
  - 컨텍스트: `Inventory` (착용 버튼)

### 🔄 구현 필요
1. **상점 (ShopInventoryUI)**:
   - 현재: `OnInventoryItemClicked` 이벤트만 있음
   - 필요: ItemDetailPopup 연동 (판매 가격 표시, "판매" 버튼)
   - 컨텍스트: `Shop_Sell`

2. **캐릭터 정보창 (CharacterInfoUI)**:
   - 현재: LobbyEquippedItemsUI로 장비만 표시
   - 필요: ItemDetailPopup 연동 (읽기 전용, 버튼 없음)
   - 컨텍스트: `ReadOnly`

---

## 🎯 전체 구조 설계

### **ItemDetailContext 기반 UI 변화**

| 컨텍스트 | 호출 위치 | 주요 버튼 | 고급 기능 | 설명 |
|---------|---------|----------|---------|------|
| **Inventory** | 보관창고 | "착용" | ✅ 분해/강화/합성 | 보관창고 아이템 → 장비창으로 착용 |
| **Equipment** | 장비창 | "해제" | ✅ 분해/강화/합성 | 장비창 아이템 → 보관창고로 해제 |
| **Shop_Sell** | 상점 (인벤토리) | "판매" | ❌ | 보관창고 아이템 → 상점에 판매 |
| **Shop_Buy** | 상점 (상품 목록) | "구매" | ❌ | 상점 아이템 → 보관창고로 구매 |
| **ReadOnly** | 캐릭터 정보창 | 없음 | ❌ | 읽기 전용 (정보만 표시) |

---

## 📍 Phase 1: ShopInventoryUI 연동 (30분)

### **목표:**
상점에서 판매할 아이템 클릭 시 ItemDetailPopup 표시 (판매 가격, "판매" 버튼)

### **1-1. ShopInventoryUI.cs 수정 (15분)**

**수정 내용:**
```csharp
// 1. OnInventoryItemClicked 이벤트를 PlayerDataManager.OnSlotClicked 이벤트로 변경
// 2. ItemDetailPopup 구독 추가

private void SetupEventListeners()
{
    if (PlayerDataManager.Instance != null)
    {
        // ⭐ 기존 이벤트 대신 PlayerDataManager.OnSlotClicked 구독
        PlayerDataManager.Instance.OnSlotClicked += HandleInventorySlotClicked;
    }
}

private void HandleInventorySlotClicked(EquipmentData data, int slotIndex, ItemInstanceId instanceId)
{
    // 상점 패널이 활성화된 상태에서만 처리
    if (!gameObject.activeInHierarchy)
        return;
    
    // ⭐ ItemDetailPopup 호출
    var popup = FindObjectOfType<ItemDetailPopup>();
    if (popup != null)
    {
        popup.Show(data, ItemDetailContext.Shop_Sell, slotIndex, instanceId);
    }
}
```

**파일:**
- `Assets/Scripts/UI/Shop/ShopInventoryUI.cs`

**라인:**
- `SetupEventListeners()` 메서드 수정
- `HandleInventorySlotClicked()` 메서드 추가 (신규)

---

### **1-2. ItemDetailPopup.cs - Shop_Sell 컨텍스트 처리 (10분)**

**현재 상태:**
```csharp
case ItemDetailContext.Shop_Sell:
    primaryActionButtonText.text = "판매";
    primaryActionGroup.SetActive(true);
    advancedActionGroup.SetActive(false); // 분해/강화/합성 숨김
    break;
```

**추가 구현 필요:**
```csharp
private void OnPrimaryActionButtonClicked()
{
    switch (currentContext)
    {
        // ... 기존 코드 ...
        
        case ItemDetailContext.Shop_Sell:
            SellItem(); // ⭐ 구현 필요
            break;
    }
}

private void SellItem()
{
    if (currentItem == null || !currentItemInstanceId.IsValid())
    {
        Debug.LogError("판매할 아이템이 없습니다.");
        return;
    }
    
    // ShopUIController로 판매 처리 위임
    var shopController = FindObjectOfType<ShopUIController>();
    if (shopController != null)
    {
        // ⭐ ShopUIController.SellItemFromInventory() 호출
        // shopController.SellItemFromInventory(currentItemInstanceId);
        
        // 판매 완료 후 팝업 닫기
        Hide();
    }
    else
    {
        Debug.LogError("ShopUIController를 찾을 수 없습니다.");
    }
}
```

**파일:**
- `Assets/Scripts/UI/Popups/ItemDetailPopup.cs`

**수정 위치:**
- `OnPrimaryActionButtonClicked()` 메서드에 `Shop_Sell` case 추가
- `SellItem()` 메서드 신규 추가

---

### **1-3. ShopUIController.cs - 판매 로직 확인 (5분)**

**기존 판매 로직 확인:**
```csharp
// ShopUIController.cs에 이미 판매 로직이 있는지 확인
// 있으면 ItemDetailPopup에서 호출만 하면 됨
// 없으면 추가 구현 필요
```

**예상 메서드:**
- `SellItemFromInventory(ItemInstanceId instanceId)`
- `SellItemFromInventory(EquipmentData item, int slotIndex)`

---

## 📍 Phase 2: CharacterInfoUI 연동 (20분)

### **목표:**
캐릭터 정보창에서 장착 중인 아이템 클릭 시 ItemDetailPopup 표시 (읽기 전용)

### **2-1. LobbyEquippedItemsUI.cs 확인 (5분)**

**현재 상태 파악:**
- LobbyEquippedItemsUI가 장비 표시를 담당
- InventorySlot 또는 유사한 슬롯 사용 여부 확인
- 슬롯 클릭 이벤트 처리 방식 확인

**파일:**
- `Assets/Scripts/UI/Inventory/LobbyEquippedItemsUI.cs`

---

### **2-2. CharacterInfoUI.cs 수정 (10분)**

**수정 내용:**
```csharp
private void Start()
{
    InitializeCharacterInfoUI();
    SetupItemDetailPopupEvents(); // ⭐ 추가
}

/// <summary>
/// ItemDetailPopup 이벤트 설정
/// </summary>
private void SetupItemDetailPopupEvents()
{
    if (PlayerDataManager.Instance != null)
    {
        PlayerDataManager.Instance.OnSlotClicked += HandleEquipmentSlotClicked;
    }
}

/// <summary>
/// 장비 슬롯 클릭 처리 (읽기 전용)
/// </summary>
private void HandleEquipmentSlotClicked(EquipmentData data, int slotIndex, ItemInstanceId instanceId)
{
    // 캐릭터 정보창이 활성화된 상태에서만 처리
    if (!gameObject.activeInHierarchy)
        return;
    
    // ⭐ ItemDetailPopup 호출 (ReadOnly)
    var popup = FindObjectOfType<ItemDetailPopup>();
    if (popup != null)
    {
        popup.Show(data, ItemDetailContext.ReadOnly, slotIndex, instanceId);
    }
}

private void OnDestroy()
{
    // 이벤트 구독 해제
    if (PlayerDataManager.Instance != null)
    {
        PlayerDataManager.Instance.OnSlotClicked -= HandleEquipmentSlotClicked;
    }
}
```

**파일:**
- `Assets/Scripts/UI/PlayerUI/CharacterInfoUI.cs`

**수정 위치:**
- `Start()` 메서드에 `SetupItemDetailPopupEvents()` 호출 추가
- `SetupItemDetailPopupEvents()` 메서드 신규 추가
- `HandleEquipmentSlotClicked()` 메서드 신규 추가
- `OnDestroy()` 메서드에 이벤트 구독 해제 추가

---

### **2-3. ItemDetailPopup.cs - ReadOnly 컨텍스트 확인 (5분)**

**현재 상태:**
```csharp
case ItemDetailContext.ReadOnly:
    primaryActionGroup.SetActive(false);  // 버튼 숨김
    advancedActionGroup.SetActive(false); // 분해/강화/합성 숨김
    break;
```

**확인 사항:**
- ✅ 이미 구현되어 있음 (버튼 모두 숨김)
- ✅ 정보만 표시되는 읽기 전용 모드

---

## 📍 Phase 3: 통합 테스트 (15분)

### **3-1. 보관창고 테스트**
- [x] 아이템 클릭 → ItemDetailPopup 표시
- [x] "착용" 버튼 → 장비창으로 이동
- [x] 분해/강화/합성 버튼 표시 ✅
- [x] 등급별 배경 색상 적용 ✅

### **3-2. 상점 테스트**
- [ ] 상점 열기 → 보관창고 아이템 클릭
- [ ] ItemDetailPopup 표시 (Shop_Sell)
- [ ] "판매" 버튼 → 판매 처리
- [ ] 분해/강화/합성 버튼 숨김 ✅
- [ ] 판매 가격 표시 확인

### **3-3. 캐릭터 정보창 테스트**
- [ ] 캐릭터 정보창 열기 → 장착 아이템 클릭
- [ ] ItemDetailPopup 표시 (ReadOnly)
- [ ] 모든 버튼 숨김 ✅
- [ ] 정보만 표시 확인

### **3-4. 공통 기능 테스트**
- [ ] 배경 클릭 시 팝업 닫기
- [ ] 등급별 배경 색상 모든 UI에서 정상 표시
- [ ] 팝업 전환 시 데이터 정상 표시

---

## 🔑 핵심 아키텍처

### **이벤트 기반 통합 구조**

```
┌─────────────────────────────────────────────────────┐
│           PlayerDataManager (중앙 이벤트)            │
│  OnSlotClicked(EquipmentData, int, ItemInstanceId)  │
└─────────────────────────────────────────────────────┘
                         ▲
                         │ (구독)
         ┌───────────────┼───────────────┐
         │               │               │
┌─────────────┐  ┌──────────────┐  ┌────────────────┐
│LobbyInventory│  │ShopInventory │  │CharacterInfoUI │
│     UI       │  │      UI      │  │                │
└─────────────┘  └──────────────┘  └────────────────┘
         │               │               │
         │ (호출)        │ (호출)        │ (호출)
         └───────────────┼───────────────┘
                         ▼
         ┌───────────────────────────────┐
         │      ItemDetailPopup          │
         │  - Show(data, context, ...)   │
         │  - 컨텍스트별 UI 자동 변경     │
         └───────────────────────────────┘
```

### **컨텍스트별 흐름**

**1. 보관창고 → 착용:**
```
InventorySlot 클릭
  → PlayerDataManager.OnSlotClicked 발생
  → LobbyInventoryUI.HandleSlotClicked()
  → ItemDetailPopup.Show(data, Inventory, ...)
  → "착용" 버튼 클릭
  → EquipItem() → PlayerDataManager.EquipItem()
```

**2. 상점 → 판매:**
```
InventorySlot 클릭 (상점 내)
  → PlayerDataManager.OnSlotClicked 발생
  → ShopInventoryUI.HandleInventorySlotClicked()
  → ItemDetailPopup.Show(data, Shop_Sell, ...)
  → "판매" 버튼 클릭
  → SellItem() → ShopUIController.SellItemFromInventory()
```

**3. 캐릭터 정보창 → 읽기 전용:**
```
EquipmentSlot 클릭 (캐릭터 정보창)
  → PlayerDataManager.OnSlotClicked 발생
  → CharacterInfoUI.HandleEquipmentSlotClicked()
  → ItemDetailPopup.Show(data, ReadOnly, ...)
  → 버튼 없음 (읽기 전용)
```

---

## ✅ 예상 결과

### **상점 UI:**
```
┌─────────────────────────────┐
│   [판매할 아이템 선택]       │
│                              │
│  [아이템1] [아이템2] [아이템3]│
│    (클릭 시 팝업 열림)        │
│                              │
│  ┌────────────────────────┐ │
│  │ ItemDetailPopup        │ │
│  │ ┌──────────────────┐   │ │
│  │ │ 🎨 등급별 배경   │   │ │
│  │ │   아이콘         │   │ │
│  │ └──────────────────┘   │ │
│  │ 이름: 드래곤 소드       │ │
│  │ 판매 가격: 500 골드    │ │
│  │                        │ │
│  │  [판매] 버튼           │ │
│  └────────────────────────┘ │
└─────────────────────────────┘
```

### **캐릭터 정보창 UI:**
```
┌─────────────────────────────┐
│   캐릭터 정보                │
│                              │
│  착용 중인 장비:             │
│  [무기] [방어구] [악세서리]  │
│    (클릭 시 팝업 열림)        │
│                              │
│  ┌────────────────────────┐ │
│  │ ItemDetailPopup        │ │
│  │ ┌──────────────────┐   │ │
│  │ │ 🎨 등급별 배경   │   │ │
│  │ │   아이콘         │   │ │
│  │ └──────────────────┘   │ │
│  │ 이름: 드래곤 소드       │ │
│  │ 등급: SS               │ │
│  │ 공격력: +50            │ │
│  │                        │ │
│  │  (버튼 없음)           │ │
│  └────────────────────────┘ │
└─────────────────────────────┘
```

---

## 📋 작업 체크리스트

### **Phase 1: ShopInventoryUI (30분)**
- [ ] ShopInventoryUI.cs - `HandleInventorySlotClicked()` 추가
- [ ] ShopInventoryUI.cs - `SetupEventListeners()` 수정
- [ ] ItemDetailPopup.cs - `SellItem()` 메서드 추가
- [ ] ShopUIController.cs - 판매 로직 확인/연동
- [ ] 테스트: 상점에서 아이템 클릭 → 팝업 표시

### **Phase 2: CharacterInfoUI (20분)**
- [ ] LobbyEquippedItemsUI.cs - 슬롯 클릭 이벤트 확인
- [ ] CharacterInfoUI.cs - `HandleEquipmentSlotClicked()` 추가
- [ ] CharacterInfoUI.cs - `SetupItemDetailPopupEvents()` 추가
- [ ] 테스트: 캐릭터 정보창에서 아이템 클릭 → 팝업 표시

### **Phase 3: 통합 테스트 (15분)**
- [ ] 모든 UI에서 등급별 배경 색상 확인
- [ ] 컨텍스트별 버튼 표시/숨김 확인
- [ ] 배경 클릭 시 닫기 기능 확인

---

## 🎯 최종 목표

**모든 아이템 UI에서 일관된 상세 정보 표시:**
- ✅ 보관창고: 착용 기능
- 🔄 상점: 판매 기능
- 🔄 캐릭터 정보창: 읽기 전용
- ✅ 등급별 배경 색상 자동 적용
- ✅ 컨텍스트 기반 UI 자동 변경
- ✅ 단일 ItemDetailPopup 재사용

**총 예상 시간: 1시간 5분**

