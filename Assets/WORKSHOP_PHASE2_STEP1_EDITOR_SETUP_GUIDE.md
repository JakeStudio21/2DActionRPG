# 🏭 공방(Workshop) UI Phase 2 - Step 2-1: 공통 인프라 에디터 설정 가이드

이 문서는 공방 UI의 공통 인프라(인벤토리 + 제작 전후 슬롯)를 Unity Editor에서 설정하는 방법을 안내합니다.

---

## 📋 **Step 2-1 완료 파일 목록**

✅ `InventorySlot.cs` - 다중 선택 기능 추가 (기존 파일 수정)  
✅ `WorkshopInventoryUI.cs` - 좌측 인벤토리 UI (신규)  
✅ `BeforeAfterComparisonUI.cs` - 제작 전후 슬롯 비교 (신규)

---

## 1️⃣ **InventorySlot 프리팹 수정 (다중 선택 UI 추가)**

### **위치**
`Assets/Prefabs/UI/InventorySlot.prefab`

### **추가할 UI 요소**

```
InventorySlot (기존)
├── EffectTarget (기존)
│   ├── ItemIcon (기존)
│   ├── BindIcon (기존)
│   └── ... (기존 요소들)
│
└── SelectionUI (🆕 신규)
    ├── SelectionCheckbox (GameObject)
    │   └── Checkmark (Image) ☑
    └── SelectionHighlight (Image) - 골드 테두리
```

### **작업 순서**

#### **1-1. SelectionUI 그룹 생성**

1. `InventorySlot` 프리팹을 엽니다
2. 최상위에 `Empty GameObject` 생성
   - 이름: `SelectionUI`
   - RectTransform 설정:
     - AnchorMin: (0, 0)
     - AnchorMax: (1, 1)
     - Pivot: (0.5, 0.5)
     - OffsetMin: (0, 0)
     - OffsetMax: (0, 0)

#### **1-2. SelectionCheckbox 생성**

1. `SelectionUI` 아래에 `GameObject` 생성
   - 이름: `SelectionCheckbox`
   - RectTransform 설정:
     - AnchorMin: (0, 1) (좌상단)
     - AnchorMax: (0, 1) (좌상단)
     - Pivot: (0, 1)
     - AnchoredPosition: (5, -5) // 좌상단에서 5px 여백
     - SizeDelta: (30, 30)
   - **초기 상태**: `GameObject` 비활성화 (체크 해제)

2. `SelectionCheckbox` 아래에 `Image` 생성
   - 이름: `Checkmark`
   - 이미지: UI 기본 체크마크 또는 "☑" 텍스트 이미지
   - Color: #FFEB3B (노란색)
   - RectTransform:
     - Stretch 전체 (부모 크기에 맞춤)
   - **초기 상태**: `Image` 컴포넌트의 `enabled` 체크 해제

#### **1-3. SelectionHighlight 생성**

1. `SelectionUI` 아래에 `Image` 생성
   - 이름: `SelectionHighlight`
   - Image Type: `Sliced` (테두리용)
   - Sprite: UI 기본 테두리 또는 `UI/Skin/UISprite`
   - Color: #FFD700 (골드, Alpha: 0) // 초기에는 투명
   - RectTransform:
     - AnchorMin: (0, 0)
     - AnchorMax: (1, 1)
     - Pivot: (0.5, 0.5)
     - OffsetMin: (-2, -2) // 테두리 두께
     - OffsetMax: (2, 2)
   - **Raycast Target**: ❌ 체크 해제 (클릭 방해 방지)

#### **1-4. InventorySlot 컴포넌트 연결**

`InventorySlot` 컴포넌트의 Inspector에서:

```
📌 Selection UI (다중 선택) 섹션:
- Selection Checkbox: SelectionCheckbox (GameObject)
- Selection Checkmark: SelectionCheckbox/Checkmark (Image)
- Selection Highlight: SelectionUI/SelectionHighlight (Image)
```

---

## 2️⃣ **WorkshopInventoryUI 생성 (좌측 인벤토리)**

### **Hierarchy 구조**

```
EnhancementSubPanel (기존)
└── LeftSection (🆕 신규, RectTransform)
    ├── InventoryHeader (VerticalLayoutGroup)
    │   ├── TitleText ("보관창고", TMP_Text)
    │   └── TabGroup (HorizontalLayoutGroup)
    │       ├── EquipmentTabButton (Button + TMP_Text "장비")
    │       ├── ConsumableTabButton (Button + TMP_Text "소모품")
    │       └── MaterialTabButton (Button + TMP_Text "재료")
    │
    ├── FilterArea (VerticalLayoutGroup)
    │   ├── MultiSelectToggle (Toggle + TMP_Text "☑ 다중선택 모드")
    │   ├── SelectionInfoRow (HorizontalLayoutGroup)
    │   │   ├── SelectionCountText (TMP_Text "선택: 0개")
    │   │   └── ClearSelectionButton (Button "초기화")
    │   │
    │   └── QuickFilterRow (HorizontalLayoutGroup)
    │       ├── SelectDGradeButton (Button "D등급 전체")
    │       ├── SelectCGradeButton (Button "C등급 전체")
    │       ├── SelectBGradeButton (Button "B등급 전체")
    │       ├── SelectAGradeButton (Button "A등급 전체")
    │       ├── SelectSGradeButton (Button "S등급 전체")
    │       └── ExcludeEquippedToggle (Toggle "장착 아이템 제외")
    │
    ├── InventoryScrollView (Scroll View)
    │   ├── Viewport
    │   │   └── Content (GridLayoutGroup)
    │   │       ├── (동적 생성: InventorySlot_001)
    │   │       ├── (동적 생성: InventorySlot_002)
    │   │       └── ...
    │   └── Scrollbar Vertical
    │
    └── InventoryFooter (HorizontalLayoutGroup)
        └── ItemCountText (TMP_Text "보유 아이템: 0/100")
```

### **작업 순서**

#### **2-1. LeftSection 생성**

1. `EnhancementSubPanel` 아래에 `Empty GameObject` 생성
   - 이름: `LeftSection`
   - RectTransform 설정:
     - AnchorMin: (0, 0)
     - AnchorMax: (0.4, 1) // 좌측 40%
     - Pivot: (0, 0.5)
     - OffsetMin: (20, 20) // 좌하단 여백
     - OffsetMax: (0, -20) // 우상단 여백

2. `LeftSection`에 `WorkshopInventoryUI.cs` 컴포넌트 추가

#### **2-2. InventoryHeader 생성**

1. `LeftSection` 아래에 `Empty GameObject` 생성
   - 이름: `InventoryHeader`
   - RectTransform:
     - AnchorMin: (0, 1)
     - AnchorMax: (1, 1)
     - Pivot: (0.5, 1)
     - AnchoredPosition: (0, 0)
     - SizeDelta: (0, 80) // 높이 80px
   - VerticalLayoutGroup 추가:
     - Padding: Top=10, Bottom=10, Left=10, Right=10
     - Spacing: 10
     - ChildAlignment: UpperCenter

2. **TitleText 생성**
   - `InventoryHeader` 아래에 `TextMeshProUGUI` 생성
   - 이름: `TitleText`
   - Text: "보관창고"
   - Font Size: 20
   - Bold: ✅
   - Alignment: Center
   - Color: #FFFFFF

3. **TabGroup 생성**
   - `InventoryHeader` 아래에 `Empty GameObject` 생성
   - 이름: `TabGroup`
   - HorizontalLayoutGroup:
     - Spacing: 5
     - ChildAlignment: MiddleCenter
     - ChildControlSize: Width ✅, Height ✅
     - ChildForceExpand: Width ✅, Height ❌

4. **탭 버튼 3개 생성**
   - `TabGroup` 아래에 `Button` 3개 생성
   - 이름: `EquipmentTabButton`, `ConsumableTabButton`, `MaterialTabButton`
   - 각 버튼:
     - 자식으로 `TMP_Text` 추가
     - Text: "장비", "소모품", "재료"
     - Font Size: 16
     - LayoutElement 추가:
       - PreferredWidth: 80
       - PreferredHeight: 40

#### **2-3. FilterArea 생성**

1. `LeftSection` 아래에 `Empty GameObject` 생성
   - 이름: `FilterArea`
   - RectTransform:
     - AnchorMin: (0, 1)
     - AnchorMax: (1, 1)
     - Pivot: (0.5, 1)
     - AnchoredPosition: (0, -80) // InventoryHeader 아래
     - SizeDelta: (0, 120)
   - VerticalLayoutGroup:
     - Padding: 10
     - Spacing: 10

2. **MultiSelectToggle 생성**
   - `FilterArea` 아래에 `Toggle` 생성
   - 이름: `MultiSelectToggle`
   - Label Text: "☑ 다중선택 모드"
   - LayoutElement:
     - PreferredHeight: 30

3. **SelectionInfoRow 생성**
   - `FilterArea` 아래에 `Empty GameObject` 생성
   - 이름: `SelectionInfoRow`
   - HorizontalLayoutGroup:
     - Spacing: 10
     - ChildAlignment: MiddleLeft
   - 자식 요소:
     - `SelectionCountText` (TMP_Text "선택: 0개", Font Size: 14)
     - `ClearSelectionButton` (Button "초기화", Width: 60, Height: 30)

4. **QuickFilterRow 생성**
   - `FilterArea` 아래에 `Empty GameObject` 생성
   - 이름: `QuickFilterRow`
   - HorizontalLayoutGroup:
     - Spacing: 5
     - ChildAlignment: MiddleLeft
   - 자식 요소 (버튼 5개 + 토글 1개):
     - `SelectDGradeButton` ~ `SelectSGradeButton` (각 Button, Width: 70, Height: 30)
     - `ExcludeEquippedToggle` (Toggle "장착 아이템 제외")

#### **2-4. InventoryScrollView 생성**

1. `LeftSection` 아래에 `Scroll View` 생성 (UI → Scroll View)
   - 이름: `InventoryScrollView`
   - RectTransform:
     - AnchorMin: (0, 0)
     - AnchorMax: (1, 1)
     - Pivot: (0.5, 0.5)
     - OffsetMin: (0, 50) // 하단 여백 (Footer용)
     - OffsetMax: (0, -200) // 상단 여백 (Header + Filter용)

2. **Content 설정**
   - `InventoryScrollView/Viewport/Content` 선택
   - GridLayoutGroup 추가:
     - Cell Size: (80, 80)
     - Spacing: (10, 10)
     - Start Corner: Upper Left
     - Start Axis: Horizontal
     - Child Alignment: Upper Left
     - Constraint: Fixed Column Count = 4

3. **Scrollbar 설정**
   - Vertical Scrollbar만 활성화
   - Horizontal Scrollbar 삭제

#### **2-5. InventoryFooter 생성**

1. `LeftSection` 아래에 `Empty GameObject` 생성
   - 이름: `InventoryFooter`
   - RectTransform:
     - AnchorMin: (0, 0)
     - AnchorMax: (1, 0)
     - Pivot: (0.5, 0)
     - AnchoredPosition: (0, 0)
     - SizeDelta: (0, 50)
   - HorizontalLayoutGroup:
     - Padding: 10
     - ChildAlignment: MiddleCenter

2. **ItemCountText 생성**
   - `InventoryFooter` 아래에 `TMP_Text` 생성
   - 이름: `ItemCountText`
   - Text: "보유 아이템: 0/100"
   - Font Size: 16
   - Color: #AAAAAA
   - Alignment: Center

#### **2-6. WorkshopInventoryUI 컴포넌트 연결**

`LeftSection`의 `WorkshopInventoryUI` 컴포넌트 Inspector에서 모든 필드 연결:

```
📑 탭 시스템:
- Equipment Tab Button: TabGroup/EquipmentTabButton
- Consumable Tab Button: TabGroup/ConsumableTabButton
- Material Tab Button: TabGroup/MaterialTabButton
- Equipment Tab Text: EquipmentTabButton/Text
- Consumable Tab Text: ConsumableTabButton/Text
- Material Tab Text: MaterialTabButton/Text

🔘 다중 선택 시스템:
- Multi Select Mode Toggle: FilterArea/MultiSelectToggle
- Selection Count Text: SelectionInfoRow/SelectionCountText
- Clear Selection Button: SelectionInfoRow/ClearSelectionButton

🎯 빠른 필터:
- Select D Grade Button: QuickFilterRow/SelectDGradeButton
- Select C Grade Button: QuickFilterRow/SelectCGradeButton
- Select B Grade Button: QuickFilterRow/SelectBGradeButton
- Select A Grade Button: QuickFilterRow/SelectAGradeButton
- Select S Grade Button: QuickFilterRow/SelectSGradeButton
- Exclude Equipped Toggle: QuickFilterRow/ExcludeEquippedToggle

📦 인벤토리:
- Inventory Scroll View: InventoryScrollView
- Inventory Content: InventoryScrollView/Viewport/Content
- Inventory Slot Prefab: Assets/Prefabs/UI/InventorySlot (프리팹 드래그)
- Max Slots: 100

📊 하단 정보:
- Item Count Text: InventoryFooter/ItemCountText
```

---

## 3️⃣ **BeforeAfterComparisonUI 생성 (제작 전후 슬롯)**

### **Hierarchy 구조**

```
EnhancementSubPanel (기존)
└── RightSection (🆕 신규, RectTransform)
    └── ComparisonArea (🆕 신규)
        ├── TitleText ("제작 전 → 제작 후 비교", TMP_Text)
        │
        ├── BeforeGroup (HorizontalLayoutGroup)
        │   ├── BeforeLabel (TMP_Text "제작 전")
        │   └── BeforeSlotsContainer (HorizontalLayoutGroup)
        │       ├── BeforeSlot1 (InventorySlot 프리팹 인스턴스)
        │       ├── BeforeSlot2 (InventorySlot 프리팹 인스턴스)
        │       └── BeforeSlot3 (InventorySlot 프리팹 인스턴스)
        │
        ├── ArrowIcon (Image "═══►")
        │
        └── AfterGroup (HorizontalLayoutGroup)
            ├── AfterLabel (TMP_Text "제작 후")
            └── AfterSlotsContainer (HorizontalLayoutGroup)
                ├── AfterSlot1 (InventorySlot 프리팹 인스턴스)
                ├── AfterSlot2 (InventorySlot 프리팹 인스턴스)
                └── AfterSlot3 (InventorySlot 프리팹 인스턴스)
```

### **작업 순서**

#### **3-1. RightSection 생성**

1. `EnhancementSubPanel` 아래에 `Empty GameObject` 생성
   - 이름: `RightSection`
   - RectTransform:
     - AnchorMin: (0.4, 0)
     - AnchorMax: (1, 1) // 우측 60%
     - Pivot: (0, 0.5)
     - OffsetMin: (20, 20) // 좌하단 여백
     - OffsetMax: (-20, -20) // 우상단 여백

#### **3-2. ComparisonArea 생성**

1. `RightSection` 아래에 `Empty GameObject` 생성
   - 이름: `ComparisonArea`
   - RectTransform:
     - AnchorMin: (0, 1)
     - AnchorMax: (1, 1)
     - Pivot: (0.5, 1)
     - AnchoredPosition: (0, 0)
     - SizeDelta: (0, 300) // 높이 300px
   - `BeforeAfterComparisonUI.cs` 컴포넌트 추가

2. **TitleText 생성**
   - `ComparisonArea` 아래에 `TMP_Text` 생성
   - 이름: `TitleText`
   - Text: "제작 전 → 제작 후 비교"
   - Font Size: 20
   - Bold: ✅
   - Alignment: Center
   - RectTransform:
     - AnchorMin: (0, 1)
     - AnchorMax: (1, 1)
     - Pivot: (0.5, 1)
     - AnchoredPosition: (0, 0)
     - SizeDelta: (0, 40)

#### **3-3. BeforeGroup 생성**

1. `ComparisonArea` 아래에 `Empty GameObject` 생성
   - 이름: `BeforeGroup`
   - RectTransform:
     - AnchorMin: (0, 0.5)
     - AnchorMax: (0.35, 1) // 좌측 35%
     - Pivot: (0.5, 0.5)
     - AnchoredPosition: (0, -20)
   - VerticalLayoutGroup:
     - Padding: 10
     - Spacing: 10
     - ChildAlignment: MiddleCenter

2. **BeforeLabel 생성**
   - `BeforeGroup` 아래에 `TMP_Text` 생성
   - 이름: `BeforeLabel`
   - Text: "제작 전"
   - Font Size: 16
   - Alignment: Center
   - Color: #FFEB3B (노란색)

3. **BeforeSlotsContainer 생성**
   - `BeforeGroup` 아래에 `Empty GameObject` 생성
   - 이름: `BeforeSlotsContainer`
   - HorizontalLayoutGroup:
     - Spacing: 10
     - ChildAlignment: MiddleCenter

4. **슬롯 3개 생성**
   - `InventorySlot` 프리팹을 `BeforeSlotsContainer`에 드래그하여 3개 인스턴스 생성
   - 이름: `BeforeSlot1`, `BeforeSlot2`, `BeforeSlot3`
   - 각 슬롯:
     - RectTransform SizeDelta: (100, 100)
     - **초기 상태**: `GameObject` 비활성화 (체크 해제)

#### **3-4. ArrowIcon 생성**

1. `ComparisonArea` 아래에 `Image` 생성
   - 이름: `ArrowIcon`
   - RectTransform:
     - AnchorMin: (0.35, 0.5)
     - AnchorMax: (0.45, 0.5) // 중앙 10%
     - Pivot: (0.5, 0.5)
     - SizeDelta: (80, 40)
   - Image:
     - Sprite: 화살표 이미지 또는 없으면 `TMP_Text`로 대체 ("═══►")
     - Color: #FFFFFF
   - **초기 상태**: `GameObject` 비활성화 (체크 해제)

#### **3-5. AfterGroup 생성**

1. `ComparisonArea` 아래에 `Empty GameObject` 생성
   - 이름: `AfterGroup`
   - RectTransform:
     - AnchorMin: (0.45, 0.5)
     - AnchorMax: (1, 1) // 우측 55%
     - Pivot: (0.5, 0.5)
     - AnchoredPosition: (0, -20)
   - VerticalLayoutGroup:
     - Padding: 10
     - Spacing: 10
     - ChildAlignment: MiddleCenter

2. **AfterLabel 생성**
   - `AfterGroup` 아래에 `TMP_Text` 생성
   - 이름: `AfterLabel`
   - Text: "제작 후"
   - Font Size: 16
   - Alignment: Center
   - Color: #4CAF50 (초록색)

3. **AfterSlotsContainer 생성**
   - `AfterGroup` 아래에 `Empty GameObject` 생성
   - 이름: `AfterSlotsContainer`
   - HorizontalLayoutGroup:
     - Spacing: 10
     - ChildAlignment: MiddleCenter

4. **슬롯 3개 생성**
   - `InventorySlot` 프리팹을 `AfterSlotsContainer`에 드래그하여 3개 인스턴스 생성
   - 이름: `AfterSlot1`, `AfterSlot2`, `AfterSlot3`
   - 각 슬롯:
     - RectTransform SizeDelta: (100, 100)
     - **초기 상태**: `GameObject` 비활성화 (체크 해제)

#### **3-6. BeforeAfterComparisonUI 컴포넌트 연결**

`ComparisonArea`의 `BeforeAfterComparisonUI` 컴포넌트 Inspector에서:

```
⬅️ 제작 전 슬롯:
- Before Slot 1: BeforeSlotsContainer/BeforeSlot1
- Before Slot 2: BeforeSlotsContainer/BeforeSlot2
- Before Slot 3: BeforeSlotsContainer/BeforeSlot3
- Before Label: BeforeGroup/BeforeLabel

➡️ 제작 후 슬롯:
- After Slot 1: AfterSlotsContainer/AfterSlot1
- After Slot 2: AfterSlotsContainer/AfterSlot2
- After Slot 3: AfterSlotsContainer/AfterSlot3
- After Label: AfterGroup/AfterLabel

🎨 화살표:
- Arrow Icon: ArrowIcon (GameObject)
- Arrow Image: ArrowIcon (Image 컴포넌트)
```

---

## 4️⃣ **최종 확인 체크리스트**

### **InventorySlot 프리팹**
- [ ] SelectionUI 그룹 생성됨
- [ ] SelectionCheckbox 초기 비활성화됨
- [ ] SelectionCheckmark Image.enabled = false
- [ ] SelectionHighlight Color Alpha = 0
- [ ] InventorySlot 컴포넌트에 3개 필드 연결됨

### **WorkshopInventoryUI (LeftSection)**
- [ ] 탭 버튼 3개 생성 및 연결됨
- [ ] 다중 선택 토글/버튼 생성됨
- [ ] 등급별 필터 버튼 5개 생성됨
- [ ] Scroll View + GridLayoutGroup 설정됨
- [ ] InventorySlot 프리팹 연결됨
- [ ] WorkshopInventoryUI 컴포넌트 모든 필드 연결됨

### **BeforeAfterComparisonUI (ComparisonArea)**
- [ ] 제작 전/후 슬롯 각 3개씩 생성됨
- [ ] 모든 슬롯 초기 비활성화됨
- [ ] 화살표 아이콘 생성 및 초기 비활성화됨
- [ ] BeforeAfterComparisonUI 컴포넌트 모든 필드 연결됨

---

## 5️⃣ **테스트 준비**

에디터 작업 완료 후:

1. Unity Editor에서 `Lobby` 씬 열기
2. Play 모드 실행
3. 로비에서 "공방" 버튼 클릭
4. "강화" 탭 선택
5. 좌측에 보관창고 아이템 목록 표시 확인
6. "다중선택 모드" 토글 → 체크박스 표시 확인
7. 아이템 클릭 → 선택 카운터 증가 확인

**다음 단계:** Step 2-1 테스트 후 Step 2-2 (강화 UI 구현)으로 진행

---

## 💡 **참고 사항**

- **InventorySlot 프리팹 수정 시**: 프리팹 변경사항은 모든 인스턴스에 자동 적용됩니다
- **다중 선택 모드**: 기본적으로 비활성화 상태, 사용자가 토글로 활성화
- **슬롯 크기 조정**: GridLayoutGroup의 Cell Size 조정으로 변경 가능
- **등급별 색상**: ItemIconGradeFrame 컴포넌트가 자동 처리

에디터 작업 중 문제 발생 시 콘솔 로그를 확인하세요. 각 컴포넌트의 `showDebugLogs` 필드를 `true`로 설정하면 상세 로그를 볼 수 있습니다.

