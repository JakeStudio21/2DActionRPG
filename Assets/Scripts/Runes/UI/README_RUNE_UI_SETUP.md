# 룬 UI 시스템 설정 가이드 (Phase 5-1)

## 📋 목차

1. [개요](#개요)
2. [UI 구조](#ui-구조)
3. [프리팹 설정](#프리팹-설정)
4. [컴포넌트 매핑](#컴포넌트-매핑)
5. [테스트 방법](#테스트-방법)

---

## 개요

**Phase 5-1 & 5-2**에서는 룬 시스템의 핵심 UI를 구현합니다:

#### Phase 5-1 (인벤토리 & 툴팁)
1. **RuneSlotUI**: 개별 룬 아이콘 (인벤토리 스크롤 뷰용)
2. **RuneTooltipUI**: 선택된 룬의 상세 정보 표시
3. **RuneInventoryUI**: 룬 목록 표시 및 관리

#### Phase 5-2 (장착 & 강화)
4. **RuneEquipSlotUI**: 3개의 장착 슬롯 (드롭 타겟)
5. **RuneDragHandler**: 드래그 앤 드롭 처리
6. **RuneEnhanceUI**: 레벨업 & 한계돌파 UI

---

## UI 구조

```
RuneInventoryPanel (Canvas)
├── RuneInventoryUI (Script)
│   ├── ScrollView
│   │   └── Content (Grid Layout Group)
│   │       └── [RuneSlotUI Prefabs 동적 생성]
│   └── InventoryCountText (TMP)
│
└── RuneTooltipPanel
    └── RuneTooltipUI (Script)
        ├── BasicInfo
        │   ├── RuneIcon (Image)
        │   ├── RuneNameText (TMP)
        │   ├── RuneTypeText (TMP)
        │   └── ObtainMethodText (TMP)
        ├── LevelInfo
        │   ├── LevelText (TMP)
        │   └── LimitBreakText (TMP)
        ├── MainStatContainer
        │   ├── MainStatNameText (TMP)
        │   └── MainStatValueText (TMP)
        └── SubStatsContainer
            └── SubStatsList (Vertical Layout Group)
                └── [SubStatRow Prefabs 동적 생성]
```

---

## 프리팹 설정

### 1. RuneSlotUI 프리팹

**경로**: `Assets/Prefabs/UI/Rune/RuneSlotPrefab.prefab`

**필수 컴포넌트**:
- `Button` (클릭 이벤트)
- `RuneSlotUI` (스크립트)

**UI 요소**:
```
RuneSlot (Button)
├── BackgroundImage (Image)
├── RuneIcon (Image)
├── RuneNameText (TextMeshProUGUI)
├── LevelText (TextMeshProUGUI)
├── LimitBreakStars (Image[] - 5개)
│   ├── Star_0 (Image)
│   ├── Star_1 (Image)
│   ├── Star_2 (Image)
│   ├── Star_3 (Image)
│   └── Star_4 (Image)
├── SelectedBorder (GameObject)
└── LockOverlay (GameObject)
    └── LockIcon (Image)
```

**SerializeField 매핑**:
```csharp
[SerializeField] private Image runeIconImage;
[SerializeField] private Image backgroundImage;
[SerializeField] private TextMeshProUGUI runeNameText;
[SerializeField] private TextMeshProUGUI levelText;
[SerializeField] private GameObject selectedBorder;
[SerializeField] private Image[] limitBreakStars; // 5개
[SerializeField] private GameObject lockOverlay;
[SerializeField] private Image lockIcon;
```

---

### 2. SubStatRow 프리팹

**경로**: `Assets/Prefabs/UI/Rune/SubStatRowPrefab.prefab`

**UI 요소**:
```
SubStatRow (Horizontal Layout Group)
├── StatNameText (TextMeshProUGUI) - "치명타 확률"
└── StatValueText (TextMeshProUGUI) - "+15.0%"
```

**간단한 구조**:
- 가로로 배치 (Horizontal Layout Group)
- 왼쪽: 스탯 이름
- 오른쪽: 스탯 값

---

### 3. RuneTooltipUI 설정

**경로**: 씬 내 Canvas 하위에 직접 배치

**SerializeField 매핑**:
```csharp
[SerializeField] private Image runeIconImage;
[SerializeField] private TextMeshProUGUI runeNameText;
[SerializeField] private TextMeshProUGUI runeTypeText;
[SerializeField] private TextMeshProUGUI obtainMethodText;
[SerializeField] private TextMeshProUGUI levelText;
[SerializeField] private TextMeshProUGUI limitBreakText;
[SerializeField] private GameObject mainStatContainer;
[SerializeField] private TextMeshProUGUI mainStatNameText;
[SerializeField] private TextMeshProUGUI mainStatValueText;
[SerializeField] private GameObject subStatsContainer;
[SerializeField] private Transform subStatsList;
[SerializeField] private GameObject subStatRowPrefab; // 위에서 만든 프리팹
[SerializeField] private GameObject tooltipPanel;
```

---

### 4. RuneInventoryUI 설정

**경로**: 씬 내 Canvas 하위에 직접 배치

**SerializeField 매핑**:
```csharp
[SerializeField] private ScrollRect scrollRect;
[SerializeField] private Transform contentTransform; // ScrollView의 Content
[SerializeField] private GameObject runeSlotPrefab; // RuneSlotPrefab 연결
[SerializeField] private RuneTooltipUI runeTooltipUI; // 위에서 만든 툴팁 연결
[SerializeField] private TextMeshProUGUI inventoryCountText;
```

**중요**: 
- `contentTransform`에는 **Grid Layout Group** 컴포넌트를 추가하세요.
- Grid Layout Group 설정 예시:
  - Cell Size: `(200, 80)`
  - Spacing: `(10, 10)`
  - Constraint: Fixed Column Count `3`

---

## 컴포넌트 매핑

### RuneInventoryUI 예시

1. **Hierarchy**에서 `RuneInventoryPanel` 생성
2. `RuneInventoryUI` 스크립트 추가
3. Inspector에서 다음을 드래그&드롭:
   - Scroll Rect → ScrollView의 ScrollRect 컴포넌트
   - Content Transform → ScrollView의 Content Transform
   - Rune Slot Prefab → `RuneSlotPrefab.prefab`
   - Rune Tooltip UI → 씬의 `RuneTooltipUI` GameObject
   - Inventory Count Text → TMP_Text 컴포넌트

---

## 테스트 방법

### 1. Unity Editor 메뉴에서 테스트

**경로**: `Tools > Rune System > Phase 5 - UI`

#### 테스트 순서:

1. **1. 테스트 룬 생성 (다양한 레벨)**
   - Lv.1 ~ Lv.15까지 다양한 레벨의 룬 생성
   - 부옵션, 한계돌파 상태도 다양하게 설정됨

2. **2. UI 강제 갱신**
   - `RuneInventoryUI.RefreshInventory()` 호출
   - 생성된 룬들이 UI에 표시되는지 확인

3. **3. 첫 번째 룬 장착**
   - 첫 번째 룬을 슬롯 0에 장착
   - UI에서 장착 상태 표시 확인

4. **5. 🚀 풀 시나리오 테스트**
   - 위 1~3 단계를 한 번에 실행
   - 빠른 통합 테스트용

5. **6. 🗑️ 모든 테스트 데이터 삭제**
   - 테스트 완료 후 데이터 정리

---

### 2. 씬 실행 테스트

1. **Play 버튼 클릭**
2. 인벤토리 UI가 자동으로 갱신됨 (`Start()` 호출)
3. **룬 슬롯 클릭** → 툴팁에 상세 정보 표시
4. **주옵션 확인** → 레벨 스케일링 적용된 값 표시
5. **부옵션 확인** → 원본 값 표시, 빈 슬롯은 안내 문구

---

## 핵심 원칙

### ⚠️ UI 스크립트는 절대 데이터를 수정하지 않음!

```csharp
// ❌ 잘못된 예시
void OnLevelUpButtonClicked()
{
    currentRune.currentLevel++; // UI에서 데이터 직접 수정 (절대 금지!)
}

// ✅ 올바른 예시
void OnLevelUpButtonClicked()
{
    RuneEnhanceManager.Instance.TryLevelUp(currentRune); // 매니저를 통해 처리
    RefreshUI(); // UI만 갱신
}
```

### 주옵션 스케일링

```csharp
// Phase 4에서 검증된 로직 활용
float scaledValue = modifier.value * currentRune.GetMainStatMultiplier();
```

### 부옵션 표시

```csharp
// 부옵션은 레벨 스케일링 없이 원본 값 그대로
var modifier = ConditionalModifierDatabase.GetModifierById(subModId);
displayValue = modifier.value; // 원본 값 그대로!
```

---

## 다음 단계

Phase 5-1이 완료되면 다음 단계로 진행합니다:

- ✅ **Phase 5-2**: 룬 장착 UI (장착 슬롯 3개, 드래그&드롭) + 룬 강화 UI (레벨업, 한계돌파)
- **Phase 5-3**: 룬 필터링 & 정렬 UI
- **Phase 5-4**: 룬 잠금 및 일괄 처리 UI

---

## Phase 5-2: 장착 및 강화 UI

### RuneEquipSlotUI (장착 슬롯)

**역할**: 3개의 장착 슬롯을 관리하고, 드래그 앤 드롭으로 룬 장착/해제

#### 하이어라키 구조

```
RuneEquipSlot [0/1/2] (Canvas GameObject)
├── RuneEquipSlotUI (Script)
├── BackgroundImage (Image)
├── RuneIconImage (Image) - 장착된 룬 아이콘
├── LevelText (TMP) - "Lv.10"
├── EmptySlotIndicator (GameObject) - "빈 슬롯" 텍스트
└── LimitBreakStars (5개 Image)
```

#### SerializeField 매핑

```csharp
[SerializeField] private int slotIndex;               // 0, 1, 2
[SerializeField] private Image runeIconImage;
[SerializeField] private Image backgroundImage;
[SerializeField] private TextMeshProUGUI levelText;
[SerializeField] private GameObject emptySlotIndicator;
[SerializeField] private Image[] limitBreakStars;     // 5개
[SerializeField] private TextMeshProUGUI errorMessageText;
```

#### 주요 기능

- **드롭 이벤트**: `OnDrop()` - 드래그된 룬을 `RuneManager.EquipRune()` 호출하여 장착
- **우클릭/더블클릭**: `OnPointerClick()` - `RuneManager.UnequipRune()` 호출하여 해제
- **슬롯 갱신**: `RefreshSlot()` - 현재 슬롯에 장착된 룬 표시

---

### RuneDragHandler (드래그 앤 드롭)

**역할**: 인벤토리 룬 슬롯을 드래그하여 장착 슬롯에 드롭

#### 설정 방법

1. `RuneSlotPrefab`에 `RuneDragHandler` 컴포넌트 추가 (자동 추가됨)
2. `dragParent` 필드에 최상위 Canvas 드래그
3. `draggedSlotAlpha` 조절 (권장: 0.3)

#### 작동 원리

1. **드래그 시작**: 원본 슬롯 반투명 처리, 임시 아이콘 생성
2. **드래그 중**: 임시 아이콘이 마우스 커서 따라다님
3. **드롭**: `RuneEquipSlotUI.OnDrop()` 호출 → `RuneManager.EquipRune()`
4. **드래그 종료**: 원본 슬롯 복구, 임시 아이콘 제거

---

### RuneEnhanceUI (강화 UI)

**역할**: 선택된 룬의 레벨업 및 한계돌파 실행

#### 하이어라키 구조

```
RuneEnhancePanel (Canvas GameObject)
├── RuneEnhanceUI (Script)
├── TabButtons
│   ├── LevelUpTabButton (Button)
│   └── LimitBreakTabButton (Button)
├── CommonInfo
│   ├── RuneIcon (Image)
│   ├── RuneNameText (TMP)
│   └── CurrentLevelText (TMP)
├── LevelUpPanel (GameObject)
│   ├── CurrentStatText (TMP) - "보스 피해 증가 20%"
│   ├── ArrowText (TMP) - "→"
│   ├── NextStatText (TMP) - "보스 피해 증가 21%"
│   ├── LevelUpButton (Button)
│   │   └── ButtonText (TMP) - "강화"
│   └── LevelUpMessageText (TMP) - "최대 레벨 도달 (한계돌파 필요)"
└── LimitBreakPanel (GameObject)
    ├── CurrentMaxLevelText (TMP) - "현재 최대 레벨: 10"
    ├── LimitBreakCountText (TMP) - "한계돌파: 0 / 5"
    ├── MaterialSlotsScrollView
    │   └── MaterialSlotsContainer (Vertical Layout Group)
    │       └── [RuneSlotUI Prefabs 동적 생성]
    ├── LimitBreakButton (Button)
    │   └── ButtonText (TMP) - "한계돌파"
    └── LimitBreakMessageText (TMP) - "재료 룬을 선택하세요"
```

#### SerializeField 매핑

```csharp
[Header("=== 탭 전환 ===")]
[SerializeField] private Button levelUpTabButton;
[SerializeField] private Button limitBreakTabButton;
[SerializeField] private GameObject levelUpPanel;
[SerializeField] private GameObject limitBreakPanel;

[Header("=== 공통 정보 ===")]
[SerializeField] private Image runeIconImage;
[SerializeField] private TextMeshProUGUI runeNameText;
[SerializeField] private TextMeshProUGUI currentLevelText;

[Header("=== 레벨업 UI ===")]
[SerializeField] private TextMeshProUGUI currentStatText;
[SerializeField] private TextMeshProUGUI nextStatText;
[SerializeField] private TextMeshProUGUI arrowText;
[SerializeField] private Button levelUpButton;
[SerializeField] private TextMeshProUGUI levelUpButtonText;
[SerializeField] private TextMeshProUGUI levelUpMessageText;

[Header("=== 한계돌파 UI ===")]
[SerializeField] private TextMeshProUGUI currentMaxLevelText;
[SerializeField] private TextMeshProUGUI limitBreakCountText;
[SerializeField] private Transform materialSlotsContainer;
[SerializeField] private GameObject materialSlotPrefab;
[SerializeField] private Button limitBreakButton;
[SerializeField] private TextMeshProUGUI limitBreakButtonText;
[SerializeField] private TextMeshProUGUI limitBreakMessageText;
```

#### 주요 기능

- **레벨업 탭**:
  - 현재/다음 레벨의 주옵션 스케일링 수치 미리보기
  - [강화] 버튼 클릭 → `RuneEnhanceManager.TryLevelUp()` 호출
  - 최대 레벨 도달 시 버튼 비활성화 및 메시지 표시

- **한계돌파 탭**:
  - 동일 `runeId`를 가진 1레벨 룬을 재료 목록에 표시
  - 재료 선택 후 [한계돌파] 버튼 클릭 → `RuneEnhanceManager.TryLimitBreak()` 호출
  - 5한돌(15레벨) 달성 시 버튼 비활성화 및 완료 메시지 표시

---

## 문제 해결

### Q1. "RuneInventoryManager를 찾을 수 없습니다"

**원인**: 씬에 매니저 오브젝트가 없음

**해결**:
1. 빈 GameObject 생성
2. `RuneInventoryManager` 스크립트 추가
3. 씬 실행

### Q2. "룬 슬롯이 생성되지 않음"

**원인**: `runeSlotPrefab`이 연결되지 않음

**해결**:
1. `RuneInventoryUI` Inspector 확인
2. `Rune Slot Prefab` 필드에 프리팹 드래그

### Q3. "툴팁이 표시되지 않음"

**원인**: `runeTooltipUI`가 연결되지 않음

**해결**:
1. `RuneInventoryUI` Inspector 확인
2. `Rune Tooltip UI` 필드에 씬의 툴팁 GameObject 드래그

---

## 참고 자료

- **기존 UI 시스템**: `Assets/Scripts/UI/Skills/SkillListItemUI.cs` 참고
- **Phase 4 스케일링 로직**: `Assets/Scripts/Runes/RuneManager.cs` 참고
- **ConditionalModifier 구조**: `Assets/Scripts/Combat/Conditional/ConditionalModifier.cs` 참고

---

**작성일**: 2026-02-27  
**버전**: Phase 5-1 & 5-2  
**작성자**: AI Assistant
