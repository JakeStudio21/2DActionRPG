# 📘 Workshop Phase 2 Step 2-2: EnhancementUI 에디터 설정 가이드

## 🎯 목표
**강화 UI (우측 제작 패널)** 구조 생성 및 `EnhancementUI.cs` 컴포넌트 연결

---

## 📋 작업 순서

### **Step 1: RightSection UI 계층 구조 생성**

#### **1-1. EnhancementSubPanel > RightSection 선택**
```
Hierarchy 경로:
WorkshopPanel > EnhancementSubPanel > RightSection
```

#### **1-2. 하위 UI 요소 생성**

##### **A. SelectedItemPanel (선택된 아이템 정보)**
```
1. RightSection 우클릭 → UI → Panel
2. 이름: "SelectedItemPanel"
3. RectTransform:
   - Anchor: Top (상단 고정)
   - PosY: -50 (상단에서 50픽셀 아래)
   - Width: 400
   - Height: 120

4. 하위 요소 생성:

   a) ItemIcon (Image)
      - 위치: 좌측 상단
      - Width: 80, Height: 80
      - Image 컴포넌트: Preserve Aspect = true
      
   b) ItemNameText (TextMeshProUGUI)
      - Anchor: Top-Left
      - 위치: ItemIcon 우측
      - Font Size: 20
      - Color: White
      - Text: "아이템 이름 +3"
      
   c) EnhancementLevelText (TextMeshProUGUI)
      - Anchor: Middle-Left
      - 위치: ItemNameText 아래
      - Font Size: 16
      - Color: Yellow
      - Text: "+3 → +4"
```

##### **B. SuccessRatePanel (성공 확률)**
```
1. RightSection 우클릭 → UI → Panel
2. 이름: "SuccessRatePanel"
3. RectTransform:
   - Anchor: Top (SelectedItemPanel 아래)
   - PosY: -200
   - Width: 400
   - Height: 100

4. 하위 요소 생성:

   a) SuccessRateText (TextMeshProUGUI)
      - Anchor: Top-Center
      - Font Size: 18
      - Color: Green
      - Text: "성공 확률: 85%"
      
   b) SuccessRateBarBG (Image)
      - Anchor: Bottom-Stretch
      - Height: 30
      - Color: Dark Gray (0.2, 0.2, 0.2, 1)
      
   c) SuccessRateBar (Image)
      - Parent: SuccessRateBarBG
      - Anchor: Left-Stretch
      - Image Type: Filled
      - Fill Method: Horizontal
      - Fill Amount: 0.85 (85%)
      - Color: Green
```

##### **C. MaterialCostPanel (필요 재료)**
```
1. RightSection 우클릭 → UI → Panel
2. 이름: "MaterialCostPanel"
3. RectTransform:
   - Anchor: Top
   - PosY: -330
   - Width: 400
   - Height: 200

4. 하위 요소 생성:

   a) MaterialSlotsContainer (HorizontalLayoutGroup)
      - Spacing: 10
      - Child Alignment: Middle Center
      
      하위에 3개 슬롯 생성:
      - MaterialSlot1 (InventorySlot 프리팹 인스턴스)
      - MaterialSlot2 (InventorySlot 프리팹 인스턴스)
      - MaterialSlot3 (InventorySlot 프리팹 인스턴스)
      
      각 슬롯 크기: 80x80
      
   b) GoldCostText (TextMeshProUGUI)
      - Anchor: Bottom-Center
      - Font Size: 16
      - Color: Gold (1, 0.84, 0, 1)
      - Text: "필요 골드: 5000G"
```

##### **D. WarningPanel (실패 경고)**
```
1. RightSection 우클릭 → UI → Panel
2. 이름: "WarningPanel"
3. RectTransform:
   - Anchor: Top
   - PosY: -560
   - Width: 400
   - Height: 80

4. 하위 요소 생성:

   a) WarningIcon (Image)
      - Anchor: Left
      - Width: 40, Height: 40
      - Sprite: 경고 아이콘 (없으면 Yellow Square)
      
   b) WarningText (TextMeshProUGUI)
      - Anchor: Right-Stretch
      - Font Size: 14
      - Color: Yellow
      - Text: "실패 시: 강화 수치 유지"
      - Alignment: Middle Left
```

##### **E. EnhanceButton (강화 버튼)**
```
1. RightSection 우클릭 → UI → Button
2. 이름: "EnhanceButton"
3. RectTransform:
   - Anchor: Bottom-Center
   - PosY: 50 (하단에서 50픽셀 위)
   - Width: 300
   - Height: 60

4. 버튼 설정:
   - Image Color: Green (0.2, 0.8, 0.2, 1)
   - Transition: Color Tint
   - Highlighted Color: Bright Green
   - Pressed Color: Dark Green
   - Disabled Color: Gray

5. 하위 Text (ButtonText):
   - Font Size: 20
   - Color: White
   - Text: "강화하기"
   - Alignment: Center
```

---

### **Step 2: EnhancementUI 컴포넌트 추가 및 연결**

#### **2-1. EnhancementSubPanel에 EnhancementUI 컴포넌트 추가**
```
1. Hierarchy: WorkshopPanel > EnhancementSubPanel > RightSection 선택
2. Inspector → Add Component
3. 검색: "EnhancementUI"
4. EnhancementUI 컴포넌트 추가
```

#### **2-2. 컴포넌트 필드 연결**

##### **📊 디버그**
```
- Show Debug Logs: ✅ 체크 (테스트 중)
```

##### **📌 선택된 아이템 정보**
```
Hierarchy에서 다음 요소를 드래그하여 연결:

- Item Name Text: SelectedItemPanel > ItemNameText
- Item Icon Image: SelectedItemPanel > ItemIcon (Image 컴포넌트)
- Enhancement Level Text: SelectedItemPanel > EnhancementLevelText
```

##### **📊 성공 확률 표시**
```
- Success Rate Text: SuccessRatePanel > SuccessRateText
- Success Rate Bar: SuccessRatePanel > SuccessRateBarBG > SuccessRateBar (Image 컴포넌트)
- Success Rate Panel: SuccessRatePanel (GameObject)
```

##### **📦 필요 재료 슬롯**
```
- Material Slot1: MaterialCostPanel > MaterialSlotsContainer > MaterialSlot1
- Material Slot2: MaterialCostPanel > MaterialSlotsContainer > MaterialSlot2
- Material Slot3: MaterialCostPanel > MaterialSlotsContainer > MaterialSlot3
- Gold Cost Text: MaterialCostPanel > GoldCostText (필요 골드)
- Player Gold Text: PlayerGoldText (플레이어 보유 골드) ⭐ 새로 추가!
- Material Cost Panel: MaterialCostPanel (GameObject)
```

**⭐ PlayerGoldText 추가 방법 (상점/보관창고와 동일한 방식):**
```
1. RightSection 하위에 새 TextMeshProUGUI 오브젝트 생성
   - 이름: PlayerGoldText
   - 위치: MaterialCostPanel 위 또는 옆 (UI 디자인에 맞게 배치)
   - 기본 텍스트: "0" (⭐ 숫자만 표시, 상점과 동일)
   - Font Size: 20~24 (상점 골드 텍스트 참고)
   - 색상: 밝은 노란색 (#FFD700) 권장
   - Alignment: Left 또는 Right (디자인에 맞게)

2. (선택) 골드 아이콘 추가
   - PlayerGoldText 옆에 골드 아이콘 Image 추가 (상점 UI 참고)

3. EnhancementUI 컴포넌트에 연결
   - Inspector > EnhancementUI > 📦 필요 재료 슬롯
   - Player Gold Text: PlayerGoldText 드래그 & 드롭
```

##### **⚠️ 실패 경고 패널**
```
- Warning Text: WarningPanel > WarningText
- Warning Icon: WarningPanel > WarningIcon (Image 컴포넌트)
- Warning Panel: WarningPanel (GameObject)
```

##### **🔘 강화 버튼**
```
- Enhance Button: EnhanceButton (Button 컴포넌트)
- Enhance Button Text: EnhanceButton > Text (TextMeshProUGUI)
```

##### **🔗 연동 컴포넌트**
```
- Comparison UI: ComparisonArea (BeforeAfterComparisonUI 컴포넌트)
- Workshop Inventory UI: LeftSection (WorkshopInventoryUI 컴포넌트)
```

---

### **Step 3: WorkshopInventoryUI와 EnhancementUI 연동**

#### **3-1. WorkshopInventoryUI에서 이벤트 발생 추가**

이 부분은 코드 수정이 필요합니다. `WorkshopInventoryUI.cs`에서 아이템 선택 시 `EnhancementUI.OnSelectedItemChanged()` 호출하도록 수정해야 합니다.

**수정 위치:** `WorkshopInventoryUI.cs`
```csharp
// 아이템 슬롯 클릭 시
private void OnSlotSelectionChanged(ItemInstanceId itemId, bool selected)
{
    // ... 기존 로직 ...
    
    // ⭐ EnhancementUI에 선택된 아이템 전달
    if (enhancementUI != null && selected)
    {
        enhancementUI.OnSelectedItemChanged(itemId);
    }
}
```

**필드 추가:**
```csharp
[Header("🔗 연동 컴포넌트")]
[SerializeField] private EnhancementUI enhancementUI;
```

#### **3-2. WorkshopInventoryUI Inspector에서 EnhancementUI 연결**
```
1. Hierarchy: WorkshopPanel > EnhancementSubPanel > LeftSection 선택
2. Inspector > WorkshopInventoryUI 컴포넌트
3. "연동 컴포넌트" 섹션:
   - Enhancement UI: RightSection (EnhancementUI 컴포넌트)
```

---

### **Step 4: 버튼 이벤트 연결**

#### **4-1. EnhanceButton 이벤트 연결**
```
1. Hierarchy: EnhanceButton 선택
2. Inspector > Button 컴포넌트
3. On Click () 이벤트:
   - "+" 버튼 클릭
   - Object: RightSection (EnhancementUI 컴포넌트)
   - Function: EnhancementUI > OnEnhanceButtonClicked ()
```

---

### **Step 5: InventorySlot 프리팹 Material 슬롯용 설정**

#### **5-1. MaterialSlot1/2/3에 SetupMaterialSlot 지원 확인**

`InventorySlot.cs`에 이미 `SetupMaterialSlot(string materialName, int count)` 메서드가 구현되어 있습니다.

**확인 사항:**
- MaterialSlot1, 2, 3는 모두 InventorySlot 프리팹 인스턴스여야 합니다.
- CountText와 RarityBorder가 연결되어 있어야 합니다.

**이미 구현되어 있으므로 추가 작업 불필요!** ✅

---

## 4️⃣ 최종 확인 체크리스트

### **RightSection 계층 구조**
- [ ] SelectedItemPanel 생성됨
  - [ ] ItemIcon, ItemNameText, EnhancementLevelText 하위 요소 있음
- [ ] SuccessRatePanel 생성됨
  - [ ] SuccessRateText, SuccessRateBar 하위 요소 있음
- [ ] MaterialCostPanel 생성됨
  - [ ] MaterialSlot1, 2, 3 (InventorySlot 프리팹) 있음
  - [ ] GoldCostText 있음
- [ ] WarningPanel 생성됨
  - [ ] WarningIcon, WarningText 있음
- [ ] EnhanceButton 생성됨
  - [ ] ButtonText 하위 요소 있음

### **EnhancementUI 컴포넌트 연결**
- [ ] RightSection에 EnhancementUI 컴포넌트 추가됨
- [ ] 선택된 아이템 정보 필드 (3개) 연결됨
- [ ] 성공 확률 필드 (3개) 연결됨
- [ ] 필요 재료 필드 (6개) 연결됨 ⭐ Player Gold Text 추가!
  - Material Slot1, 2, 3
  - Gold Cost Text (필요 골드)
  - Player Gold Text (보유 골드) 🆕
  - Material Cost Panel
- [ ] 실패 경고 필드 (3개) 연결됨
- [ ] 강화 버튼 필드 (2개) 연결됨
- [ ] 연동 컴포넌트 필드 (2개) 연결됨

### **WorkshopInventoryUI 연동**
- [ ] WorkshopInventoryUI에 enhancementUI 필드 추가됨
- [ ] OnSlotSelectionChanged에서 EnhancementUI.OnSelectedItemChanged 호출됨
- [ ] LeftSection Inspector에서 RightSection 연결됨

### **버튼 이벤트**
- [ ] EnhanceButton.onClick에 OnEnhanceButtonClicked 연결됨

---

## 5️⃣ 테스트 방법

### **테스트 1: UI 초기 상태**
```
1. 플레이 모드 진입
2. 로비 → 공방 → 강화 탭
3. 확인:
   - SuccessRatePanel: 비활성화됨
   - MaterialCostPanel: 비활성화됨
   - WarningPanel: 비활성화됨
   - EnhanceButton: 비활성화됨
   - ButtonText: "아이템을 선택하세요"
```

### **테스트 2: 아이템 선택**
```
1. 좌측 인벤토리에서 장비 아이템 클릭
2. 확인:
   - SelectedItemPanel: 아이템 이름, 아이콘, 강화 레벨 표시됨
   - SuccessRatePanel: 활성화, 성공 확률 표시됨
   - MaterialCostPanel: 활성화, 필요 재료/골드 표시됨
   - WarningPanel: 활성화, 실패 경고 메시지 표시됨
   - ComparisonArea: "제작 전 → 제작 후" 미리보기 표시됨
   - Console: "🎯 [EnhancementUI] 아이템 선택: ..." 로그
```

### **테스트 3: 재료 부족 상태**
```
1. 재료가 부족한 아이템 선택
2. 확인:
   - GoldCostText: 빨간색으로 표시 (부족한 골드 표시)
   - EnhanceButton: 비활성화됨
   - ButtonText: "재료가 부족합니다" 또는 "골드가 부족합니다"
   - Console: "⚠️ [EnhancementUI] 강화 불가: ..." 경고
```

### **테스트 4: 성공 확률 색상**
```
1. 낮은 레벨 아이템 선택 (높은 확률)
2. 확인:
   - SuccessRateText: 초록색
   - SuccessRateBar: 초록색, fillAmount 높음

3. 높은 레벨 아이템 선택 (낮은 확률)
4. 확인:
   - SuccessRateText: 빨간색 또는 노란색
   - SuccessRateBar: 빨간색 또는 노란색, fillAmount 낮음
```

### **테스트 5: 실패 경고 메시지**
```
1. +0~+9 아이템 선택
2. 확인:
   - WarningText: "실패 시: 강화 수치 유지" (흰색)

3. +10~+12 아이템 선택
4. 확인:
   - WarningText: "⚠️ 실패 시: 강화 수치 -1" (노란색)

5. +13~+15 아이템 선택
6. 확인:
   - WarningText: "🔥 실패 시: 아이템 파괴" (빨간색)
```

### **테스트 6: 강화 실행 (기본 기능)**
```
1. 재료가 충분한 아이템 선택
2. EnhanceButton 클릭
3. 확인:
   - Console: "🔨 [EnhancementUI] 강화 실행 시작: ..."
   - Console: "✨ [EnhancementUI] 강화 성공!" 또는 "⚠️ [EnhancementUI] 강화 실패: ..."
   - 좌측 인벤토리: 자동 갱신됨
   - RightSection: UI 갱신됨 (강화 레벨 변경 반영)
```

### **테스트 7: 아이템 파괴 (고레벨)**
```
1. +13 이상 아이템 선택
2. 재료 부족하지 않으면 강화 시도
3. 확인:
   - 실패 시: "💥 [EnhancementUI] 강화 실패 (파괴): ..."
   - RightSection: 선택 해제됨
   - 좌측 인벤토리: 아이템 사라짐
```

---

## 6️⃣ 문제 해결

### **문제 1: "아이템을 선택해도 RightSection에 정보가 표시되지 않음"**
**원인:** WorkshopInventoryUI에서 EnhancementUI.OnSelectedItemChanged() 호출 안 됨

**해결:**
1. WorkshopInventoryUI.cs에 enhancementUI 필드 추가 확인
2. OnSlotSelectionChanged 메서드에서 enhancementUI.OnSelectedItemChanged() 호출 확인
3. LeftSection Inspector에서 RightSection 연결 확인

---

### **문제 2: "재료 슬롯에 아이템이 표시되지 않음"**
**원인:** MaterialSlot1/2/3가 InventorySlot 프리팹 인스턴스가 아님

**해결:**
1. MaterialCostPanel > MaterialSlotsContainer 하위 확인
2. MaterialSlot1, 2, 3를 InventorySlot 프리팹으로 교체
3. InventorySlot.SetupMaterialSlot() 메서드 호출 확인

---

### **문제 3: "성공 확률이 0%로 표시됨"**
**원인:** EnhancementData ScriptableObject가 없음

**해결:**
1. Resources/Data/EnhancementData.asset 존재 확인
2. 없으면 생성: Assets/Resources/Data 폴더 → 우클릭 → Create → Data → Enhancement Data
3. Inspector에서 등급별 성공률 설정 (D: 95%, C: 90%, ...)

---

### **문제 4: "강화 버튼을 눌러도 반응이 없음"**
**원인:** Button.onClick 이벤트 연결 안 됨

**해결:**
1. EnhanceButton Inspector → Button 컴포넌트 확인
2. On Click () 이벤트 목록에 "EnhancementUI.OnEnhanceButtonClicked" 있는지 확인
3. 없으면 Step 4 참고하여 추가

---

### **문제 5: "Console에 'EnhancementData를 찾을 수 없습니다' 에러"**
**원인:** EnhancementData.asset 경로 문제

**해결:**
1. Resources/Data/EnhancementData.asset 경로 확인
2. 대소문자 일치 확인 (EnhancementData, 대문자 E, D)
3. .asset 확장자 확인

---

## 7️⃣ 다음 단계

**Step 2-2 완료 후:**
- **Phase 2 Step 2-3**: EnhancementResultPopup 구현 (강화 결과 팝업)
- 성공/실패/파괴 애니메이션
- 결과 메시지 표시
- 확인 버튼

---

**에디터 작업 완료 후:**
**"Step 2-2 에디터 작업 완료, 테스트 진행합니다"** 라고 알려주세요! 🎉

