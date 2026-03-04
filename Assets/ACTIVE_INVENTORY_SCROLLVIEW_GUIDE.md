# 🎒 인게임 캐릭터 가방 ScrollView 적용 가이드

> **목적**: 48칸 확장에 따른 스크롤뷰 추가  
> **대상 UI**: ActiveInventory (인게임 I키 가방)  
> **참고 UI**: LobbyInventoryUI (이미 ScrollView 구현됨)

---

## 📋 1단계: Unity Hierarchy 구조 변경

### 현재 구조 (ScrollView 없음):
```
ActiveInventory (GameObject)
├─ InventorySlot_0 (슬롯들이 직접 배치)
├─ InventorySlot_1
├─ InventorySlot_2
└─ ... (16개 슬롯)
```

### 목표 구조 (ScrollView 적용):
```
ActiveInventory (GameObject)
├─ ScrollView (GameObject) ⭐ 신규 추가
│   ├─ Viewport (RectTransform) ⭐ 신규 추가
│   │   └─ Content (RectTransform) ⭐ 슬롯 컨테이너
│   │       ├─ InventorySlot_0
│   │       ├─ InventorySlot_1
│   │       └─ ... (48개 슬롯)
│   └─ Scrollbar Vertical (선택 사항)
└─ (다른 UI 요소들)
```

---

## 🔧 2단계: Unity Editor 작업 (상세 가이드)

### Step 1: ScrollView 생성
1. **Hierarchy**에서 `ActiveInventory` GameObject 우클릭
2. **UI → Scroll View** 클릭
3. 생성된 `Scroll View` GameObject 이름 확인 (그대로 사용)

### Step 2: 자동 생성된 구조 확인
Unity가 자동으로 생성한 구조:
- `Scroll View` (ScrollRect 컴포넌트 포함)
- `└─ Viewport` (Mask 컴포넌트 포함)
- `    └─ Content` (Grid Layout Group 추가 예정)
- `└─ Scrollbar Vertical` (선택 사항)

### Step 3: ScrollRect 설정
**Scroll View** GameObject 선택 → Inspector:

1. **Scroll Rect 컴포넌트**:
   - `Content`: **Viewport → Content** 드래그 앤 드롭
   - `Horizontal`: ✅ 체크 해제 (세로 스크롤만)
   - `Vertical`: ✅ 체크
   - `Movement Type`: `Elastic` (부드러운 스크롤)
   - `Elasticity`: `0.1`
   - `Inertia`: ✅ 체크
   - `Scroll Sensitivity`: `15` (마우스 휠 속도)
   - `Viewport`: **Viewport** 드래그 앤 드롭
   - `Horizontal Scrollbar`: None
   - `Vertical Scrollbar`: **Scrollbar Vertical** 드래그 앤 드롭 (선택 사항)

### Step 4: Viewport 설정
**Viewport** GameObject 선택 → Inspector:

1. **Rect Transform**:
   - Anchor: **Stretch-Stretch** (상하좌우 끝까지)
   - Left: `0`, Right: `0`, Top: `0`, Bottom: `0`

2. **Mask 컴포넌트**:
   - `Show Mask Graphic`: ✅ 체크 해제 (배경 안 보이게)

### Step 5: Content 설정 (슬롯 컨테이너)
**Content** GameObject 선택 → Inspector:

1. **Rect Transform**:
   - Anchor: **Top-Left** (상단 좌측 기준)
   - Pivot: `(0.5, 1)` (상단 중앙 기준)
   - Pos X: `0`, Pos Y: `0`
   - Width: `800` (슬롯 6개 × 100px + 여백)
   - Height: **자동 조절** (Grid Layout Group이 처리)

2. **Grid Layout Group 추가** (Add Component):
   - `Cell Size`: `(100, 100)` (슬롯 크기)
   - `Spacing`: `(10, 10)` (슬롯 간격)
   - `Start Corner`: `Upper Left`
   - `Start Axis`: `Horizontal`
   - `Child Alignment`: `Upper Center`
   - `Constraint`: **Fixed Column Count** 선택
   - `Constraint Count`: `6` (6열 고정)
   
3. **Content Size Fitter 추가** (Add Component):
   - `Horizontal Fit`: `Unconstrained`
   - `Vertical Fit`: **Preferred Size** ⭐ (높이 자동 조절)

### Step 6: 기존 슬롯 이동
1. **Hierarchy**에서 기존 `InventorySlot_0 ~ 15` 선택 (Shift + 클릭)
2. **Content** GameObject로 드래그 앤 드롭 (부모 변경)
3. 슬롯들이 Content 하위로 이동되면 **Grid Layout Group이 자동 정렬**

### Step 7: Scrollbar 설정 (선택 사항)
**Scrollbar Vertical** GameObject 선택 → Inspector:

1. **Rect Transform**:
   - Anchor: **Right-Stretch** (우측 고정)
   - Width: `20` (스크롤바 너비)

2. **Scrollbar 컴포넌트**:
   - `Direction`: `Bottom To Top`
   - `Handle Rect`: **Handle** 자식 GameObject 드래그

---

## 💻 3단계: 코드 수정 (ActiveInventory.cs)

### 수정 1: 필드 추가 (Line 17~19 근처)
```csharp
[Header("🎒 인벤토리 연동")]
[SerializeField] private bool useDynamicInventory = true;
[SerializeField] private int maxDisplaySlots = 48; // ⭐ 16 → 48 변경
[SerializeField] private ScrollRect scrollRect; // ⭐ 신규 추가
[SerializeField] private Transform slotContainer; // ⭐ 신규 추가 (Content)
```

### 수정 2: RefreshInventoryUI() 메서드 수정 (Line 200 근처)
**기존 코드**:
```csharp
for (int i = 0; i < transform.childCount && i < maxDisplaySlots; i++)
{
    Transform slotTransform = transform.GetChild(i);
    InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
```

**변경 코드**:
```csharp
// ⭐ slotContainer가 설정되어 있으면 사용, 없으면 기존 방식 (하위 호환)
Transform container = slotContainer != null ? slotContainer : transform;

for (int i = 0; i < container.childCount && i < maxDisplaySlots; i++)
{
    Transform slotTransform = container.GetChild(i);
    InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
```

---

## 🎯 4단계: Unity Inspector 연결

### ActiveInventory GameObject 선택 → Inspector:

1. **Active Inventory (Script)** 컴포넌트:
   - `Max Display Slots`: **48** (이미 코드에서 수정됨)
   - `Scroll Rect`: **ScrollView** GameObject 드래그 앤 드롭 ⭐
   - `Slot Container`: **Viewport → Content** GameObject 드래그 앤 드롭 ⭐

2. **저장**: `Ctrl + S` 또는 **File → Save**

---

## 📐 5단계: 레이아웃 최적화 (48칸 대응)

### 권장 설정 (6열 × 8행):
```
Grid Layout Group:
- Cell Size: (100, 100)
- Spacing: (10, 10)
- Constraint: Fixed Column Count = 6

계산:
- 총 48칸 = 6열 × 8행
- Content Width: 6 × 100 + 5 × 10 = 650px
- Content Height: 8 × 100 + 7 × 10 = 870px (자동 조절)
- ScrollView Height: 400~600px (화면 크기에 맞게)
```

### 대안 설정 (8열 × 6행):
```
Grid Layout Group:
- Cell Size: (90, 90) (슬롯 약간 작게)
- Spacing: (8, 8)
- Constraint: Fixed Column Count = 8

계산:
- 총 48칸 = 8열 × 6행
- Content Width: 8 × 90 + 7 × 8 = 776px
- Content Height: 6 × 90 + 5 × 8 = 580px (자동 조절)
```

---

## ✅ 6단계: 테스트 체크리스트

### 필수 확인 사항:
- [ ] **스크롤 동작**: 마우스 휠로 위아래 스크롤 가능
- [ ] **슬롯 표시**: 48개 슬롯이 정상 표시됨
- [ ] **아이템 배치**: 장비 → 재료 순서로 정렬
- [ ] **클릭 이벤트**: 슬롯 클릭 시 상세 패널 정상 작동
- [ ] **가방 가득 참**: 48개 아이템 모두 표시 가능
- [ ] **스크롤바**: 아이템 많을 때 스크롤바 표시 (선택 사항)

### 테스트 방법:
1. **인게임 진입**: Lobby → Stage 선택 → 플레이
2. **I키 누름**: 가방 UI 열림
3. **아이템 드롭**: 몬스터 처치 → 아이템 획득
4. **스크롤 테스트**: 마우스 휠로 위아래 스크롤
5. **48칸 테스트**: 치트로 48개 아이템 추가 후 스크롤 확인

---

## 🎨 7단계: 시각적 개선 (선택 사항)

### Scrollbar 스타일링:
1. **Scrollbar Vertical** 선택 → Inspector
2. **Image 컴포넌트**: 
   - Color: `(1, 1, 1, 0.3)` (반투명)
3. **Handle → Image**:
   - Color: `(0.8, 0.8, 0.8, 0.8)` (약간 불투명)

### Viewport 배경 (선택):
1. **Viewport** 선택 → **Add Component → Image**
2. **Image 컴포넌트**:
   - Color: `(0, 0, 0, 0.2)` (어두운 반투명 배경)
   - Raycast Target: ✅ 체크 해제

---

## 📝 참고: LobbyInventoryUI 구조 (이미 구현됨)

**LobbyInventoryUI.cs 필드**:
```csharp
[SerializeField] private ScrollRect scrollRect;       // ScrollView의 ScrollRect
[SerializeField] private Transform slotContainer;     // ScrollView → Viewport → Content
```

**Hierarchy 구조**:
```
LobbyInventoryPanel
└─ InventoryScrollView
    ├─ Viewport (Mask)
    │   └─ SlotContainer (Content)
    │       └─ (슬롯들 동적 생성)
    └─ Scrollbar Vertical
```

**동작 원리**:
- `slotContainer`에 Grid Layout Group + Content Size Fitter
- 아이템 개수에 따라 Content 높이 자동 조절
- ScrollRect가 Content 크기 > Viewport 크기일 때 스크롤 활성화

---

## 🚨 주의사항

### 1. 기존 슬롯 참조 방식 변경:
- **기존**: `transform.GetChild(i)` (ActiveInventory 직접 하위)
- **변경**: `slotContainer.GetChild(i)` (Content 하위)

### 2. maxDisplaySlots 업데이트:
- **16 → 48** 변경 (코드에서 이미 수정)
- Inspector에서도 확인 필요

### 3. ScrollRect 할당:
- Inspector에서 **ScrollView GameObject** 드래그 필수
- **slotContainer**는 **Viewport → Content** 드래그 필수

### 4. Grid Layout Group 설정:
- **Constraint: Fixed Column Count** 필수
- **Constraint Count**: 6 또는 8 선택
- **Content Size Fitter: Vertical Fit = Preferred Size** 필수

### 5. Anchor 설정:
- **ScrollView**: Stretch-Stretch (부모 크기에 맞춤)
- **Viewport**: Stretch-Stretch
- **Content**: Top-Left (상단 정렬)

---

## 🎯 완료 후 기대 효과

### Before (16칸, 스크롤 없음):
- ❌ 16개 이상 아이템 획득 시 표시 안 됨
- ❌ 가방 가득 참 경고
- ❌ 추가 아이템 획득 불가

### After (48칸, 스크롤 있음):
- ✅ 48개까지 아이템 획득 가능
- ✅ 마우스 휠로 스크롤하여 전체 확인
- ✅ 장비 + 재료 통합 표시로 편리한 관리
- ✅ 로비 보관창고(64칸)와 통일된 UX

---

## 🔗 관련 파일

**스크립트**:
- `Assets/Scripts/UI/Inventory/ActiveInventory.cs` (코드 수정 필요)
- `Assets/Scripts/Player/SelectedPlayerData.cs` (maxInventorySize = 48 완료)
- `Assets/Scripts/Systems/PlayerSlotData.cs` (maxInventorySize = 48 완료)

**참고**:
- `Assets/Scripts/UI/Inventory/LobbyInventoryUI.cs` (이미 ScrollView 구현됨)
- `Assets/Scripts/UI/Shop/ShopInventoryUI.cs` (이미 ScrollView 구현됨)

**프리팹** (추정):
- `Assets/Prefabs/UI/ActiveInventory.prefab` (또는 Scene에 직접 배치)

---

## 💡 빠른 팁

### 방법 1: 수동 설정 (위 가이드 따라하기)
- 장점: Unity UI 시스템 학습
- 시간: 10~15분

### 방법 2: LobbyInventoryUI 복사
1. **Lobby Scene** 열기
2. **LobbyInventoryPanel → InventoryScrollView** 복사 (Ctrl+C)
3. **Stage Scene** 열기
4. **ActiveInventory** 하위에 붙여넣기 (Ctrl+V)
5. 이름 변경 및 참조 재연결
- 장점: 빠름 (3분)
- 단점: 불필요한 설정까지 복사됨

### 방법 3: Prefab Variant 활용
1. **LobbyInventoryPanel**을 Prefab으로 저장
2. ActiveInventory에서 Prefab Variant 생성
3. 필요한 부분만 Override
- 장점: 일관성 유지
- 단점: 복잡도 증가

---

## 🐛 문제 해결

### 문제 1: 스크롤이 안 됨
- **원인**: Content 높이가 Viewport 높이보다 작음
- **해결**: Content Size Fitter의 Vertical Fit = Preferred Size 확인

### 문제 2: 슬롯이 안 보임
- **원인**: Mask 컴포넌트가 Content를 가림
- **해결**: Viewport의 Rect Transform이 Stretch-Stretch인지 확인

### 문제 3: 슬롯 클릭 안 됨
- **원인**: Viewport에 Graphic Raycaster 없음
- **해결**: Canvas 루트에 Graphic Raycaster 있는지 확인

### 문제 4: 스크롤바가 안 움직임
- **원인**: ScrollRect의 Vertical Scrollbar 할당 안 됨
- **해결**: Inspector에서 Scrollbar Vertical 드래그

### 문제 5: Grid가 깨짐
- **원인**: Grid Layout Group의 Constraint 미설정
- **해결**: Fixed Column Count = 6 설정

---

## 📊 48칸 레이아웃 시뮬레이션

### 6열 × 8행 (권장):
```
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
```
- Content Height: 약 870px (자동 계산)
- Viewport Height: 400~500px 권장
- **스크롤 필요**: 하단 2~3행 보려면 스크롤 ✅

### 8열 × 6행 (대안):
```
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
[슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯] [슬롯]
```
- Content Height: 약 580px (자동 계산)
- Viewport Height: 400~500px 권장
- **스크롤 필요**: 하단 1~2행 보려면 스크롤 ✅

---

## 🎮 최종 확인

### Unity Editor에서:
1. Scene View에서 ActiveInventory 선택
2. Inspector에서 ScrollRect, slotContainer 할당 확인
3. Play Mode 진입
4. I키 눌러 가방 열기
5. 마우스 휠로 스크롤 테스트

### 성공 기준:
- ✅ 48개 슬롯 모두 표시
- ✅ 스크롤 부드럽게 작동
- ✅ 장비 + 재료 통합 표시
- ✅ 슬롯 클릭으로 상세 패널 열림
- ✅ 로비 보관창고와 동일한 UX

---

## 📚 추가 자료

### Unity 공식 문서:
- [Scroll Rect](https://docs.unity3d.com/Manual/script-ScrollRect.html)
- [Grid Layout Group](https://docs.unity3d.com/Manual/script-GridLayoutGroup.html)
- [Content Size Fitter](https://docs.unity3d.com/Manual/script-ContentSizeFitter.html)

### 프로젝트 내 참고:
- `LobbyInventoryUI`: 64칸 ScrollView 구현
- `ShopInventoryUI`: 64칸 ScrollView 구현
- `WorkshopInventoryUI`: 이미 ScrollView 구현 (강화 UI)

