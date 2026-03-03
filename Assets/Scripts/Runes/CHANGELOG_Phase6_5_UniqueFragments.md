# 📚 CHANGELOG: Phase 6.5 - 8종류 고유 조각 시스템

**작성일**: 2026-02-28  
**Phase**: Phase 6.5 - 8종류 고유 조각 시스템  
**작성자**: AI Assistant

---

## 📝 개요

Phase 6.5에서는 **공용 룬 파편(currentRuneFragments)**을 제거하고, **8종류의 룬이 각각 자신만의 고유한 조각(Fragment)**을 사용하도록 시스템을 개편했습니다.

이는 스킬 시스템의 공용 SP와 차별화된 룬 시스템만의 고유한 재화 구조입니다.

---

## 🎯 핵심 변경 사항

### Before (Phase 6)
```csharp
// 공용 파편 (모든 룬이 공유)
public int currentRuneFragments = 1000;

// 해금 시
inventoryManager.currentRuneFragments -= 100;
```

### After (Phase 6.5)
```csharp
// 룬별 고유 조각 (8종류)
private Dictionary<string, int> runeFragments = new Dictionary<string, int>();

// 해금 시 (해당 룬의 고유 조각만 차감)
inventoryManager.TryConsumeFragments("RUNE_BOSS_HUNTER", 100);
```

---

## 🆕 신규 추가된 파일

### 1. **RuneListItemUI.cs** (우측 목록 아이템)
**경로**: `Assets/Scripts/Runes/UI/RuneListItemUI.cs`

**주요 기능**:
- ✅ 고유 조각 표시: `"보유: X / 필요"`
- ✅ 액션 버튼: 해금/강화/한계돌파 (조각 부족 시 비활성화)
- ✅ 장착 버튼: 장착하기/해제하기 토글
- ✅ 상태별 색상: 미보유(회색), 보유(흰색), 장착(초록), 선택(노랑)

**조각 표시 로직**:
| 상태 | 표시 | 필요 조각 |
|------|------|-----------|
| 미보유 | `보유: X / 100` | 100개 (해금) |
| 만렙 미달 | `보유: X / 10` | 10개 (레벨업) |
| 만렙 도달 | `보유: X / 200` | 200개 (한계돌파) |
| Lv.15 도달 | `MAX` | - |

---

### 2. **RunePanelUI.cs** (4분할 메인 패널) ★업데이트★
**경로**: `Assets/Scripts/Runes/UI/RunePanelUI.cs`

**위치**: `SkillBookPanel > RuneSubPanel > RunePanelUI (컴포넌트)`

**4분할 구조**:
```
[RunePanelUI] (스킬의 SkillTabController와 동일한 역할)
├── TopPanel: 공용 재화 (골드, 크리스탈)
├── LeftPanel: 장착 슬롯 (3개) ★RuneEquipSlotUI 프리팹★
├── RightPanel: 룬 목록 (RuneListItemUI)
└── BottomPanel: 스탯 변화량 & 부옵션 개방 안내
```

**주요 메서드**:
```csharp
public void RefreshUI()                                 // 전체 UI 갱신
private void UpdateTopPanel()                           // 재화 표시
private void UpdateEquipSlots()                         // 장착 슬롯 갱신
private void UpdateRuneList()                           // 룬 목록 갱신
private void UpdateBottomPanel()                        // 스탯 변화량 갱신
```

**스킬 시스템과 일관성**:
- `SkillTabController`: 스킬 탭 전용 UI 관리
- `RunePanelUI`: 룬 탭 전용 UI 관리
- 동일한 계층 구조로 유지

---

### 3. **RunePhase6_5TestMenu.cs** (전용 테스트 메뉴)
**경로**: `Assets/Scripts/Runes/Editor/RunePhase6_5TestMenu.cs`

**메뉴 경로**: `Tools/Rune System/Phase 6.5 - 고유 조각/`

**테스트 항목**:
1. 💎 모든 룬 조각 확인
2. 🔄 모든 룬 조각 초기화 (1000개씩)
3. 🚀 전체 시나리오 (해금→강화→한계돌파)
4. 🔬 특정 룬 조각 추가 (보스 사냥꾼 +500)
5. 🧹 모든 룬 조각 소진 테스트

---

## 🔧 수정된 파일

### 1. **RuneInventoryManager.cs** (재화 시스템 개편)
**경로**: `Assets/Scripts/Runes/RuneInventoryManager.cs`

**변경 사항**:
```csharp
// ❌ 삭제
public int currentRuneFragments = 1000;

// ✅ 추가
private Dictionary<string, int> runeFragments = new Dictionary<string, int>();
```

**신규 메서드**:
```csharp
public int GetFragmentCount(string runeId)             // 조각 조회
public void AddFragments(string runeId, int amount)    // 조각 추가
public bool TryConsumeFragments(string runeId, int)    // 조각 차감 (성공/실패)
public Dictionary<string, int> GetAllFragments()       // 전체 조각 정보
```

**초기화 로직**:
```csharp
private void InitializeFragments()
{
    // Resources/Runes 폴더에서 모든 RuneData 로드
    var allRunes = Resources.LoadAll<RuneData>("Runes");
    
    foreach (var runeData in allRunes)
    {
        runeFragments[runeData.runeId] = 1000; // 각 1000개씩
    }
}
```

---

### 2. **RuneEnhanceManager.cs** (고유 조각 소모)
**경로**: `Assets/Scripts/Runes/RuneEnhanceManager.cs`

**변경 사항**:

#### 해금 (TryUnlockRune)
```csharp
// Before:
if (inventoryManager.currentRuneFragments < 100) { ... }
inventoryManager.currentRuneFragments -= 100;

// After:
int currentFragments = inventoryManager.GetFragmentCount(runeId);
if (currentFragments < 100) { ... }
inventoryManager.TryConsumeFragments(runeId, 100);
```

#### 레벨업 (TryLevelUp)
```csharp
// Before:
if (inventoryManager.currentRuneFragments < 10) { ... }
inventoryManager.currentRuneFragments -= 10;

// After:
string runeId = targetRune.baseData.runeId;
int currentFragments = inventoryManager.GetFragmentCount(runeId);
if (currentFragments < 10) { ... }
inventoryManager.TryConsumeFragments(runeId, 10);
```

#### 한계돌파 (TryLimitBreak)
```csharp
// Before:
if (inventoryManager.currentRuneFragments < 200) { ... }
inventoryManager.currentRuneFragments -= 200;

// After:
string runeId = baseRune.baseData.runeId;
int currentFragments = inventoryManager.GetFragmentCount(runeId);
if (currentFragments < 200) { ... }
inventoryManager.TryConsumeFragments(runeId, 200);
```

---

### 3. **테스트 파일 업데이트**
**파일**:
- `RunePhase5TestMenu.cs` - Deprecated 표시, 헬퍼 메서드 추가
- `RunePhase3TestMenu.cs` - Deprecated 표시, 헬퍼 메서드 추가

**헬퍼 메서드**:
```csharp
private const string DEFAULT_TEST_RUNE_ID = "RUNE_BOSS_HUNTER";

private static int GetTestFragments()
{
    return RuneInventoryManager.Instance.GetFragmentCount(DEFAULT_TEST_RUNE_ID);
}

private static void SetTestFragments(int amount)
{
    var inventory = RuneInventoryManager.Instance;
    int current = inventory.GetFragmentCount(DEFAULT_TEST_RUNE_ID);
    
    if (current < amount)
        inventory.AddFragments(DEFAULT_TEST_RUNE_ID, amount - current);
    else if (current > amount)
        inventory.TryConsumeFragments(DEFAULT_TEST_RUNE_ID, current - amount);
}
```

---

## 🧪 테스트 방법

### 1. Editor 테스트 (자동)
**메뉴**: `Tools/Rune System/Phase 6.5 - 고유 조각/3. 🚀 전체 시나리오`

**테스트 내용**:
1. 초기 조각 확인 (1000개)
2. 룬 해금 (-100개)
3. 레벨업 Lv.1→Lv.10 (-90개)
4. 한계돌파 (+1) (-200개)
5. 추가 레벨업 Lv.10→Lv.11 (-10개)
6. 최종 조각: 600개 (1000 - 400 = 600)

**예상 결과**:
```
[최종 결과]
  룬: [보스 사냥꾼] Lv.11 (한돌 1) | 부옵션 3개
  레벨: Lv.11/11
  한계돌파: +1
  부옵션: 3개
  💎 최종 조각: 600개
  💰 소모량: 400개 (해금 100 + 레벨업 100 + 한계돌파 200)
```

---

### 2. 조각 확인 테스트
**메뉴**: `Tools/Rune System/Phase 6.5 - 고유 조각/1. 💎 모든 룬 조각 확인`

**예상 출력**:
```
총 8종류의 룬 조각:
  💎 [보스 사냥꾼] 1000개
  💎 [보스 철벽] 1000개
  💎 [방어 파괴자] 1000개
  💎 [고체력 사냥꾼] 1000개
  💎 [처형자] 1000개
  💎 [불굴의 생존자] 1000개
  💎 [장판 철벽] 1000개
  💎 [흡혈 룬] 1000개
```

---

## 📊 고유 조각 비용표

| 액션 | 비용 | 대상 조각 |
|------|------|-----------|
| **해금** | 100개 | 해당 룬의 고유 조각 |
| **레벨업** | 10개 | 해당 룬의 고유 조각 |
| **한계돌파** | 200개 | 해당 룬의 고유 조각 |

**예시**:
- 보스 사냥꾼 해금 → 보스 사냥꾼 조각 100개 소모
- 보스 사냥꾼 레벨업 → 보스 사냥꾼 조각 10개 소모
- 보스 철벽 해금 → 보스 철벽 조각 100개 소모 (독립적)

---

## 🎨 UI 구조 (4분할)

```
RuneSubPanel (GameObject)
└─ RunePanelUI (Component)
│
├─ TopPanel (상단)
│  ├─ TitleText: "룬 시스템"
│  ├─ GoldText: "골드: -" (추후 연동)
│  └─ CrystalText: "크리스탈: -" (추후 연동)
│
├─ LeftPanel (좌측) ★스킬과 동일한 구조★
│  └─ EquipSlots[3] (RuneEquipSlotUI[])
│     ├─ RuneEquipSlot0 (GameObject + RuneEquipSlotUI Component)
│     ├─ RuneEquipSlot1 (GameObject + RuneEquipSlotUI Component)
│     └─ RuneEquipSlot2 (GameObject + RuneEquipSlotUI Component)
│
├─ RightPanel (우측)
│  └─ ScrollView
│     └─ Content (Vertical Layout Group)
│        └─ RuneListItemUI (동적 생성)
│           ├─ Icon + Name + Level + Stars
│           ├─ FragmentText: "보유: X / 필요"
│           ├─ ActionButton: 해금/강화/한계돌파
│           └─ EquipButton: 장착하기/해제하기
│
└─ BottomPanel (하단) ★Phase 7-2 업데이트★
   ├─ SelectedRuneIconImage: 선택된 룬 아이콘
   ├─ SelectedRuneNameText: "보스 사냥꾼 (Lv.10)"
   ├─ MainStatText: "보스 피해 증가: 20% -> 22%" (한 줄 포맷)
   ├─ DescriptionText: "보스 몬스터에게 추가 피해를 입힙니다"
   ├─ SubStat1Text: "• 치명타 확률: 5%" 또는 "• Lv.3 개방"
   ├─ SubStat2Text: "• 공격 속도: 10%" 또는 "• Lv.6 개방"
   └─ SubStat3Text: "• 최대 HP: 100" 또는 "• Lv.9 개방"
```

---

## 🔧 기술적 세부 사항

### 1. Dictionary 구조
```csharp
Dictionary<string, int> runeFragments = new Dictionary<string, int>()
{
    { "RUNE_BOSS_HUNTER", 1000 },
    { "RUNE_DEFENSE_BREAKER", 1000 },
    { "RUNE_EXECUTIONER", 1000 },
    { "RUNE_HIGH_HP_HUNTER", 1000 },
    { "RUNE_SURVIVOR", 1000 },
    { "RUNE_AREA_DEFENDER", 1000 },
    { "RUNE_VAMPIRE", 1000 },
    { "RUNE_BOSS_DEFENDER", 1000 }
};
```

---

### 2. 조각 소모 흐름
```
사용자 액션 (해금/강화/한계돌파 버튼 클릭)
    ↓
RuneListItemUI.OnActionButtonClicked()
    ↓
RuneEnhanceManager.TryUnlockRune(runeId) / TryLevelUp / TryLimitBreak
    ↓
1. inventoryManager.GetFragmentCount(runeId) - 조각 확인
    ↓
2. inventoryManager.TryConsumeFragments(runeId, cost) - 조각 차감
    ↓
3. 액션 실행 (해금/레벨업/한계돌파)
    ↓
4. 이벤트 발행 (OnInventoryChanged / OnLevelUp / OnLimitBreak)
    ↓
5. UI 갱신 (RuneMainPanelUI.RefreshUI())
    ↓
    UpdateTopPanel() - 재화 표시
    UpdateEquipSlots() - 장착 슬롯
    UpdateRuneList() - 룬 목록 (조각 개수 갱신)
    UpdateBottomPanel() - 스탯 변화량
```

---

### 3. 조각 부족 시 UI 처리
```csharp
private void UpdateFragmentDisplay()
{
    string runeId = runeData.runeId;
    int currentFragments = inventoryManager.GetFragmentCount(runeId);
    int requiredFragments = GetRequiredFragments();
    
    bool isEnough = currentFragments >= requiredFragments;
    fragmentText.text = $"보유: {currentFragments} / {requiredFragments}";
    fragmentText.color = isEnough ? Color.white : Color.red;
    
    // 액션 버튼 비활성화
    actionButton.interactable = isEnough;
}
```

---

## 🚫 Deprecated 파일

### 1. **RunePhase5TestMenu.cs** (일부 기능 Deprecated)
- 공용 파편 참조 제거
- 헬퍼 메서드로 호환성 유지
- Phase 6.5 테스트 메뉴 사용 권장

### 2. **RunePhase3TestMenu.cs** (일부 기능 Deprecated)
- 공용 파편 참조 제거
- 헬퍼 메서드로 호환성 유지
- Phase 6.5 테스트 메뉴 사용 권장

### 3. **Phase 6 UI 파일들** (일부 Deprecated)
- `RunePanelUI.cs` - ✅ Phase 6.5로 전면 개편 완료 (4분할 구조)
- `RuneSlotUI.cs` - Deprecated: `RuneListItemUI`로 대체
- `RuneDetailUI.cs` - Deprecated: `RunePanelUI` BottomPanel로 통합
- `RuneEnhanceUI.cs` - Deprecated: `RuneListItemUI` 액션 버튼으로 통합

---

## 🎯 Unity Editor 설정 (4분할 구조)

### Step 0: 구조 이해
**스킬 시스템과 동일한 계층 구조**:
```
SkillBookPanel (GameObject)
├─ SkillBookPanelUI (Component) - 탭 전환 관리
├─ SkillSubPanel (GameObject)
│  └─ SkillTabController (Component) - 스킬 전용 UI
└─ RuneSubPanel (GameObject)
   └─ RunePanelUI (Component) - 룬 전용 UI ★여기★
```

### Step 1: RuneSubPanel 내부 구성

**중요**: `RuneSubPanel`에 `RunePanelUI` 컴포넌트가 이미 부착되어 있습니다!

```
RuneSubPanel (GameObject)
│
├─ RunePanelUI (Component) ★기존 컴포넌트★
│
├─ TopPanel (GameObject)
│  ├─ TitleText (TextMeshProUGUI)
│  ├─ GoldText (TextMeshProUGUI)
│  └─ CrystalText (TextMeshProUGUI)
│
├─ LeftPanel (GameObject)
│  ├─ RuneEquipSlot0 (GameObject + RuneEquipSlotUI Component)
│  │  ├─ RuneIcon, Level, LimitBreakStars (5개 별) ★
│  ├─ RuneEquipSlot1 (GameObject + RuneEquipSlotUI Component)
│  │  ├─ RuneIcon, Level, LimitBreakStars (5개 별) ★
│  └─ RuneEquipSlot2 (GameObject + RuneEquipSlotUI Component)
│     ├─ RuneIcon, Level, LimitBreakStars (5개 별) ★
│
├─ RightPanel (GameObject)
│  └─ ScrollView
│     └─ Viewport
│        └─ Content (Vertical Layout Group)
│
└─ BottomPanel (GameObject) ★Phase 7-2 업데이트★
   ├─ SelectedRuneIconImage (Image)
   ├─ SelectedRuneNameText (TextMeshProUGUI)
   ├─ MainStatText (TextMeshProUGUI) - 한 줄 포맷
   ├─ DescriptionText (TextMeshProUGUI) - 룬 설명
   ├─ SubStat1Text (TextMeshProUGUI) - 부옵션 슬롯 1
   ├─ SubStat2Text (TextMeshProUGUI) - 부옵션 슬롯 2
   └─ SubStat3Text (TextMeshProUGUI) - 부옵션 슬롯 3
```

---

### Step 2: RuneListItemPrefab 생성

**⚠️ 중요**: 스킬 시스템과 동일한 구조로, **메인 GameObject에는 Button을 추가하지 마세요!**

```
RuneListItemPrefab (GameObject)
├─ RuneListItemUI (Component) ★★★ 반드시 추가! ★★★
├─ BackgroundImage (Image)
├─ IconImage (Image)
├─ NameText (TextMeshProUGUI)
├─ LevelText (TextMeshProUGUI)
├─ LimitBreakStars (Image[] × 5)
├─ FragmentText (TextMeshProUGUI) ★NEW★
├─ ItemSelectButton (Button) ★ 전체 클릭용 (배경에 부착) ★
├─ ActionButton (Button)
│  └─ ActionButtonText (TextMeshProUGUI)
└─ EquipButton (Button)
   └─ EquipButtonText (TextMeshProUGUI)
```

**구조 설명**:
1. **ItemSelectButton**: 전체 아이템 클릭 감지용 (배경 또는 별도 GameObject에 부착)
2. **ActionButton**: 해금/강화/한계돌파 버튼 (하위 GameObject)
3. **EquipButton**: 장착/해제 토글 버튼 (하위 GameObject)

**중요 사항**:
- `RuneListItemUI.cs` 스크립트를 메인 GameObject에 컴포넌트로 추가
- Inspector에서 모든 UI 요소와 **3개의 버튼**을 드래그하여 연결
- `Awake()`에서 자동으로 버튼 이벤트가 연결됩니다

---

### Step 2-2: RuneEquipSlot 프리팹 생성 ★NEW★

**⚠️ 중요**: 스킬의 `SkillEquipSlotUI`와 동일한 구조입니다!

```
RuneEquipSlot (Prefab)
├─ RuneEquipSlotUI (Component) ★★★ 반드시 추가! ★★★
├─ BackgroundImage (Image)
├─ RuneIconImage (Image)
├─ SlotNumberText (TextMeshProUGUI) - "슬롯 1"
├─ RuneLevelText (TextMeshProUGUI) - "Lv.10"
├─ LimitBreakStars (Transform) ★룬 전용★
│  ├─ Star0 (Image)
│  ├─ Star1 (Image)
│  ├─ Star2 (Image)
│  ├─ Star3 (Image)
│  └─ Star4 (Image)
└─ EmptyOverlay (GameObject) - 빈 슬롯 표시
```

**프리팹 생성 방법**:
1. `Assets/Prefabs/UI/Rune/` 폴더에 `RuneEquipSlot` 프리팹 생성
2. `RuneEquipSlotUI.cs` 컴포넌트 추가
3. Inspector에서 모든 UI 요소를 연결
4. 3개 복사하여 `RuneEquipSlot0`, `RuneEquipSlot1`, `RuneEquipSlot2` 생성
5. RuneSubPanel > LeftPanel에 배치

---

### Step 3: Inspector 설정

#### **RunePanelUI** (RuneSubPanel에 부착된 컴포넌트)
- **Top Panel**:
  - `Gold Text`: TopPanel > GoldText
  - `Crystal Text`: TopPanel > CrystalText
  - `Title Text`: TopPanel > TitleText

- **Left Panel** ★스킬과 동일★:
  - `Equip Slots`: RuneEquipSlot0, RuneEquipSlot1, RuneEquipSlot2 (Array size: 3)
  - 각 슬롯은 `RuneEquipSlotUI` 컴포넌트를 가진 GameObject입니다
  - 드래그하여 배열에 추가하기만 하면 됩니다!

- **Right Panel**:
  - `Rune List Container`: RightPanel > ScrollView > Viewport > Content
  - `Rune List Item Prefab`: `Assets/Prefabs/UI/Rune/RuneListItemPrefab.prefab`
  - `Scroll Rect`: RightPanel > ScrollView

- **Bottom Panel** ★Phase 7-2 업데이트★:
  - `Selected Rune Icon Image`: BottomPanel > SelectedRuneIconImage
  - `Selected Rune Name Text`: BottomPanel > SelectedRuneNameText
  - `Main Stat Text`: BottomPanel > MainStatText (주옵션 한 줄 포맷)
  - `Description Text`: BottomPanel > DescriptionText (룬 설명)
  - `Sub Stat 1 Text`: BottomPanel > SubStat1Text (부옵션 슬롯 1)
  - `Sub Stat 2 Text`: BottomPanel > SubStat2Text (부옵션 슬롯 2)
  - `Sub Stat 3 Text`: BottomPanel > SubStat3Text (부옵션 슬롯 3)
  - `Opened SubStat Color`: 개방된 부옵션 색상 (기본: 흰색)
  - `Locked SubStat Color`: 미개방 부옵션 색상 (기본: 회색)

#### **RuneListItemUI**
**기본 정보**:
- `Rune Icon Image`: IconImage (Image)
- `Background Image`: BackgroundImage (Image)
- `Rune Name Text`: NameText (TextMeshProUGUI)
- `Level Text`: LevelText (TextMeshProUGUI)
- `Limit Break Stars`: LimitBreakStars (Image[] × 5)

**고유 조각 표시** ★NEW★:
- `Fragment Text`: FragmentText (TextMeshProUGUI)

**버튼** ★중요★:
- `Item Select Button`: ItemSelectButton (전체 클릭용 Button)
- `Action Button`: ActionButton (해금/강화/한계돌파 Button)
- `Action Button Text`: ActionButtonText (TextMeshProUGUI)
- `Equip Button`: EquipButton (장착/해제 Button)
- `Equip Button Text`: EquipButtonText (TextMeshProUGUI)

---

#### **RuneEquipSlotUI** (각 슬롯 프리팹) ★NEW★

**UI 요소**:
- `Rune Icon`: RuneIconImage (Image)
- `Background Image`: BackgroundImage (Image)
- `Slot Number Text`: SlotNumberText (TextMeshProUGUI) - "슬롯 1"
- `Rune Level Text`: RuneLevelText (TextMeshProUGUI) - "Lv.10"
- `Limit Break Stars`: Star0~Star4 (Image[] × 5) ★룬 전용★
- `Empty Overlay`: EmptyOverlay (GameObject) - 빈 슬롯 표시
- `Empty Slot Sprite`: 빈 슬롯용 기본 스프라이트

**한계돌파 색상** (선택 사항):
- `Active Star Color`: 활성화된 별 색상 (기본: 노란색)
- `Inactive Star Color`: 비활성화된 별 색상 (기본: 회색)

---

## 📈 성능 최적화

### 1. Dictionary 사용
- 조각 조회: O(1) - 해시 테이블
- 조각 추가/차감: O(1)

### 2. UI 갱신 최소화
- 이벤트 기반 갱신 (`OnInventoryChanged`, `OnRunesChanged`)
- 변경된 부분만 업데이트

### 3. Object Pooling (추후 고려)
- RuneListItemUI 재사용
- 대량의 룬 목록 처리 시 성능 향상

---

## 🐛 트러블슈팅

### 문제 1: "currentRuneFragments does not exist" 에러
**원인**: Phase 6.5에서 공용 파편 제거  
**해결**: ✅ 고유 조각 시스템으로 변경 완료

### 문제 2: 조각 개수가 0으로 표시됨
**원인**: `InitializeFragments()` 미호출  
**해결**: `RuneInventoryManager.Awake()`에서 자동 호출

### 문제 3: 다른 룬 조각이 차감됨
**원인**: 잘못된 runeId 전달  
**해결**: `runeInstance.baseData.runeId` 사용 확인

### 문제 4: UI 버튼이 비활성화되지 않음
**원인**: 조각 확인 로직 누락  
**해결**: `UpdateActionButton()`에서 `GetFragmentCount()` 확인

---

## 🎯 다음 단계 (Phase 7 예정)

1. **CurrencyManager 연동**
   - `runeFragments` → 실제 재화 시스템 통합
   - PlayerPrefs 저장/로드

2. **조각 획득 시스템**
   - 던전 클리어 시 조각 보상
   - 상점에서 조각 구매

3. **UI 개선**
   - 조각 획득 연출
   - 부족 시 상점 이동 버튼

4. **룬 타입별 슬롯 매칭**
   - Attack1 → Slot 0
   - Survival1 → Slot 1
   - Utility1 → Slot 2

---

## 📚 참고 문서

- [Phase6_UI_Setup_Guide.md](../UI/Phase6_UI_Setup_Guide.md)
- [CHANGELOG_Phase6_Backend_Refactoring.md](../CHANGELOG_Phase6_Backend_Refactoring.md)
- [CHANGELOG_Phase6_UI_Refactoring.md](../UI/CHANGELOG_Phase6_UI_Refactoring.md)

---

## 📝 변경 사항 요약

### 신규 파일 (3개)
1. `RuneListItemUI.cs` - 우측 목록 아이템 (조각 표시, 액션 버튼, 장착 버튼)
2. `RuneEquipSlotUI.cs` - 좌측 장착 슬롯 UI (프리팹 형태) ★스킬과 동일★
3. `RunePhase6_5TestMenu.cs` - 전용 테스트 메뉴

### 수정된 파일 (7개)
1. `RuneInventoryManager.cs` - Dictionary 기반 고유 조각
2. `RuneEnhanceManager.cs` - 고유 조각 소모 로직
3. `RunePanelUI.cs` - Phase 6.5 4분할 구조로 전면 개편 ★중요★
4. `RunePhase5TestMenu.cs` - 헬퍼 메서드 추가
5. `RunePhase3TestMenu.cs` - 헬퍼 메서드 추가
6. `RuneEnhanceUI.cs` - 고유 조각 시스템 변경 (Deprecated)
7. `RuneDetailUI.cs` - 고유 조각 시스템 변경 (Deprecated)

---

**✅ Phase 6.5 고유 조각 시스템 완료!**

**다음 작업**: Unity Editor 설정 → 플레이 모드 테스트 → Phase 7 재화 시스템 연동
