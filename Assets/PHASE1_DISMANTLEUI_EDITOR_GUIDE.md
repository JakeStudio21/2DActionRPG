# Phase 1: DismantleUI 에디터 설정 가이드

## 📋 개요
이 가이드는 Unity Editor에서 **DismantleSubPanel**과 **DismantleUI** 컴포넌트를 설정하는 방법을 단계별로 안내합니다.

---

## 🎯 전체 구조

```
Canvas
└── WorkshopUI
    ├── EnhancementSubPanel (기존)
    ├── DismantleSubPanel (신규) ⭐
    └── FusionSubPanel (Phase 2)
```

---

## Step 1: DismantleSubPanel GameObject 생성

### 1.1 기본 구조 생성

1. **Hierarchy**에서 `WorkshopUI` 선택
2. 우클릭 → `Create Empty`
3. 이름: `DismantleSubPanel`
4. **Inspector**에서:
   - `RectTransform` 설정:
     - Anchor: Stretch-Stretch
     - Left: 0, Top: 0, Right: 0, Bottom: 0
     - Pivot: (0.5, 0.5)
   - 초기 상태: **비활성화** (GameObject 체크박스 해제)

---

## Step 2: Left Section (인벤토리 영역) 생성

### 2.1 LeftSection GameObject

1. `DismantleSubPanel` 우클릭 → `Create Empty`
2. 이름: `LeftSection`
3. **RectTransform**:
   - Anchor: Top-Left
   - Width: 700, Height: 800
   - Pos X: 400, Pos Y: -100

### 2.2 Header (제목 + 버튼)

1. `LeftSection` 우클릭 → `UI → Panel`
2. 이름: `Header`
3. **RectTransform**:
   - Anchor: Top-Stretch
   - Height: 80
   - Left: 0, Top: 0, Right: 0
4. **자식 추가**:
   - `UI → Text - TextMeshPro` 3개 생성:
     - **TitleText**: "분해할 아이템 선택"
       - Font Size: 28, Bold, Color: White
       - Alignment: Left-Center
       - Pos X: 20, Pos Y: 0
     - **SelectAllButton**: Button (TextMeshPro)
       - Text: "전체선택"
       - Width: 120, Height: 50
       - Anchor: Right
       - Pos X: -140, Pos Y: 0
     - **DeselectAllButton**: Button (TextMeshPro)
       - Text: "선택해제"
       - Width: 120, Height: 50
       - Anchor: Right
       - Pos X: -20, Pos Y: 0

### 2.3 InventoryScrollView (스크롤 영역)

1. `LeftSection` 우클릭 → `UI → Scroll View`
2. 이름: `InventoryScrollView`
3. **RectTransform**:
   - Anchor: Stretch-Stretch
   - Left: 0, Top: -80, Right: 0, Bottom: 0
4. **ScrollRect 컴포넌트**:
   - Horizontal: ✅ 체크 해제
   - Vertical: ✅ 체크
   - Movement Type: Elastic
   - Scrollbar Visibility: Auto Hide

### 2.4 InventoryGrid (Content)

1. `InventoryScrollView → Viewport → Content` 선택
2. 이름 변경: `InventoryGrid`
3. **GridLayoutGroup 추가**:
   - Cell Size: (120, 140)
   - Spacing: (10, 10)
   - Start Corner: Upper Left
   - Start Axis: Horizontal
   - Child Alignment: Upper Left
   - Constraint: Fixed Column Count → 5
4. **ContentSizeFitter 추가**:
   - Horizontal Fit: Unconstrained
   - Vertical Fit: Preferred Size

---

## Step 3: Right Section (선택 정보 + 보상) 생성

### 3.1 RightSection GameObject

1. `DismantleSubPanel` 우클릭 → `Create Empty`
2. 이름: `RightSection`
3. **RectTransform**:
   - Anchor: Top-Right
   - Width: 450, Height: 800
   - Pos X: -250, Pos Y: -100

---

## Step 4: B안 - 선택된 아이템 요약 (대표 슬롯 + 개수)

### 4.1 SelectedItemSummary Panel

1. `RightSection` 우클릭 → `UI → Panel`
2. 이름: `SelectedItemSummary`
3. **RectTransform**:
   - Anchor: Top-Stretch
   - Height: 200
   - Left: 0, Top: 0, Right: 0
4. **Image 컴포넌트**:
   - Color: (0.2, 0.2, 0.2, 0.9) - 어두운 회색

### 4.2 TitleText

1. `SelectedItemSummary` 우클릭 → `UI → Text - TextMeshPro`
2. 이름: `TitleText`
3. **RectTransform**:
   - Anchor: Top-Stretch
   - Height: 40
   - Left: 10, Top: -10, Right: -10
4. **TextMeshProUGUI**:
   - Text: "선택된 아이템"
   - Font Size: 20, Bold
   - Color: Yellow
   - Alignment: Left-Center

### 4.3 RepresentativeSlot (대표 슬롯)

1. `SelectedItemSummary` 우클릭
2. **기존 InventorySlot Prefab을 Drag & Drop** (또는 복사)
3. 이름: `RepresentativeSlot`
4. **RectTransform**:
   - Anchor: Middle-Left
   - Width: 120, Height: 140
   - Pos X: 80, Pos Y: -20

> **중요**: `RepresentativeSlot`에는 **InventorySlot.cs** 컴포넌트가 반드시 있어야 합니다!

### 4.4 CountText (개수 표시)

1. `SelectedItemSummary` 우클릭 → `UI → Text - TextMeshPro`
2. 이름: `CountText`
3. **RectTransform**:
   - Anchor: Middle-Left
   - Width: 200, Height: 60
   - Pos X: 250, Pos Y: -20
4. **TextMeshProUGUI**:
   - Text: "외 15개"
   - Font Size: 32, Bold
   - Color: White
   - Alignment: Left-Center

---

## Step 5: 보상 미리보기 (자동 크기 조절)

### 5.1 RewardPreview Panel

1. `RightSection` 우클릭 → `UI → Panel`
2. 이름: `RewardPreview`
3. **RectTransform**:
   - Anchor: Top-Stretch
   - Height: 400
   - Left: 0, Top: -220, Right: 0
4. **Image 컴포넌트**:
   - Color: (0.15, 0.15, 0.15, 0.9) - 더 어두운 회색

### 5.2 RewardTitleText

1. `RewardPreview` 우클릭 → `UI → Text - TextMeshPro`
2. 이름: `RewardTitleText`
3. **RectTransform**:
   - Anchor: Top-Stretch
   - Height: 40
   - Left: 10, Top: -10, Right: -10
4. **TextMeshProUGUI**:
   - Text: "획득 가능 재료"
   - Font Size: 20, Bold
   - Color: Cyan
   - Alignment: Left-Center

### 5.3 RewardSlotsContainer (HorizontalLayoutGroup)

1. `RewardPreview` 우클릭 → `Create Empty`
2. 이름: `RewardSlotsContainer`
3. **RectTransform**:
   - Anchor: Top-Stretch
   - Height: 180
   - Left: 20, Top: -60, Right: -20
4. **HorizontalLayoutGroup 추가**: ⭐ 핵심
   - Child Alignment: Middle Center
   - Spacing: 10
   - Child Force Expand: Width ✅, Height ❌
   - Child Control Size: Width ❌, Height ❌ (중요!)
5. **ContentSizeFitter 추가**:
   - Horizontal Fit: Unconstrained
   - Vertical Fit: Preferred Size

> **중요**: `Child Control Size`를 모두 해제해야 스크립트에서 `sizeDelta`로 크기 조절 가능!

---

## Step 6: 실행 버튼

### 6.1 DismantleButton

1. `RightSection` 우클릭 → `UI → Button - TextMeshPro`
2. 이름: `DismantleButton`
3. **RectTransform**:
   - Anchor: Bottom-Stretch
   - Height: 80
   - Left: 20, Bottom: 20, Right: -20
4. **Button 컴포넌트**:
   - Transition: Color Tint
   - Normal: (0.2, 0.6, 1, 1) - 파란색
   - Highlighted: (0.3, 0.7, 1, 1)
   - Pressed: (0.1, 0.5, 0.9, 1)
   - Disabled: (0.5, 0.5, 0.5, 0.5)
5. **자식 Text**:
   - Text: "분해하기"
   - Font Size: 28, Bold
   - Color: White
   - Alignment: Center

---

## Step 7: DismantleUI 컴포넌트 추가 및 연결

### 7.1 컴포넌트 추가

1. `DismantleSubPanel` 선택
2. **Inspector** → `Add Component`
3. 검색: `DismantleUI`
4. 추가

### 7.2 필드 연결 (Inspector)

**Left Section (인벤토리):**
- `Left Section`: DismantleSubPanel > LeftSection
- `Inventory Grid`: LeftSection > InventoryScrollView > Viewport > InventoryGrid
- `Inventory Slot Prefab`: **InventorySlot Prefab** (Project 창에서 드래그)
- `Select All Button`: LeftSection > Header > SelectAllButton
- `Deselect All Button`: LeftSection > Header > DeselectAllButton

**Right Section (선택 정보):**
- `Right Section`: DismantleSubPanel > RightSection
- `Selected Item Summary`: RightSection > SelectedItemSummary
- `Representative Slot`: SelectedItemSummary > RepresentativeSlot
- `Count Text`: SelectedItemSummary > CountText

**보상 미리보기 (자동 크기):**
- `Reward Preview`: RightSection > RewardPreview
- `Reward Slots Container`: RewardPreview > RewardSlotsContainer
- `Material Slot Prefab`: **MaterialSlot Prefab** (InventorySlot Prefab과 동일)
- `Reward Title Text`: RewardPreview > RewardTitleText

**실행 버튼:**
- `Dismantle Button`: RightSection > DismantleButton
- `Dismantle Button Text`: DismantleButton > Text (TextMeshPro)

**팝업 참조:**
- `Confirmation Popup`: Canvas > ConfirmationPopup
- `Result Feedback Popup`: Canvas > ResultFeedbackPopup

**필터 설정:**
- `Min Grade Filter`: D (기본값)
- `Exclude Enhanced Items`: ❌ 체크 해제 (기본값)

---

## Step 8: WorkshopUIController 연동

### 8.1 WorkshopUIController.cs 수정

`WorkshopUIController.cs` 파일에 다음 필드 추가:

```csharp
[Header("=== Sub Panels ===")]
[SerializeField] private GameObject enhancementSubPanel;
[SerializeField] private GameObject dismantleSubPanel; // ⭐ 신규
[SerializeField] private GameObject fusionSubPanel;

[Header("=== UI Controllers ===")]
[SerializeField] private EnhancementUI enhancementUI;
[SerializeField] private DismantleUI dismantleUI; // ⭐ 신규
// private FusionUI fusionUI; // Phase 2
```

### 8.2 탭 전환 메서드 수정

```csharp
private void ShowEnhancementPanel()
{
    enhancementSubPanel.SetActive(true);
    dismantleSubPanel.SetActive(false);
    fusionSubPanel.SetActive(false);
    
    enhancementUI.Initialize();
}

private void ShowDismantlePanel() // ⭐ 신규
{
    enhancementSubPanel.SetActive(false);
    dismantleSubPanel.SetActive(true);
    fusionSubPanel.SetActive(false);
    
    dismantleUI.Initialize();
}

private void ShowFusionPanel() // Phase 2
{
    enhancementSubPanel.SetActive(false);
    dismantleSubPanel.SetActive(false);
    fusionSubPanel.SetActive(true);
    
    // fusionUI.Initialize();
}
```

### 8.3 WorkshopUIController Inspector 연결

1. `Canvas > WorkshopUI` 선택
2. **WorkshopUIController** 컴포넌트에서:
   - `Dismantle Sub Panel`: DismantleSubPanel 드래그
   - `Dismantle UI`: DismantleSubPanel (DismantleUI 컴포넌트) 드래그

---

## ✅ 최종 검증 체크리스트

### GameObject 계층 구조

- [ ] `DismantleSubPanel` (초기 비활성화)
  - [ ] `LeftSection`
    - [ ] `Header` (TitleText, SelectAllButton, DeselectAllButton)
    - [ ] `InventoryScrollView`
      - [ ] `Viewport`
        - [ ] `InventoryGrid` (GridLayoutGroup + ContentSizeFitter)
  - [ ] `RightSection`
    - [ ] `SelectedItemSummary` (B안)
      - [ ] `TitleText`
      - [ ] `RepresentativeSlot` (InventorySlot 컴포넌트)
      - [ ] `CountText`
    - [ ] `RewardPreview`
      - [ ] `RewardTitleText`
      - [ ] `RewardSlotsContainer` (HorizontalLayoutGroup)
    - [ ] `DismantleButton`

### 컴포넌트 연결

- [ ] `DismantleUI` 컴포넌트 추가됨
- [ ] 모든 필드 연결 완료 (15개)
- [ ] Prefab 참조 연결 (InventorySlot, MaterialSlot)
- [ ] 팝업 참조 연결 (ConfirmationPopup, ResultFeedbackPopup)

### WorkshopUIController

- [ ] `dismantleSubPanel`, `dismantleUI` 필드 추가
- [ ] `ShowDismantlePanel()` 메서드 구현
- [ ] Inspector에서 연결 완료

---

## 🎯 다음 단계

에디터 설정이 완료되면:
1. **Play 모드 진입**
2. **Phase 1 테스트 체크리스트** 실행
3. 문제 발견 시 디버그 로그 확인

---

## 📌 참고 사항

### 자동 크기 조절 동작 방식

- **재료 3개 이하**: 슬롯 크기 150px (큰 슬롯)
- **재료 4~6개**: 슬롯 크기 75px (작은 슬롯)
- **재료 7~9개**: 슬롯 크기 60px (최소 슬롯)

### Phase 0의 교훈 적용

- `rewardPreview.SetActive(true)` 후 **1프레임 대기**
- `Instantiate()` 후 **1프레임 대기**
- 2-stage delay 시스템으로 Unity UI Layout 버그 방지

### B안 표시 규칙

- 선택 1개: 대표 슬롯만 표시
- 선택 2개 이상: 대표 슬롯 + "외 N개" 표시

