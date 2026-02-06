# 📋 Phase 2: ItemDetailPopup Unity Hierarchy 설정 가이드

## 🎯 목표
- Canvas에 ItemDetailPopup GameObject 생성
- 독립 팝업 구조 완성
- Inspector 필드 연결

---

## 📍 Step 1: Hierarchy 구조 생성 (15분)

### **1-1. 최상위 ItemDetailPopup 생성**

```
Canvas (기존)
├── LobbyPanel (기존)
├── ShopPanel (기존)
├── EquipmentPanel (기존)
└── ItemDetailPopup ← 여기에 생성
```

**작업:**
1. `Hierarchy` 우클릭 → `Create Empty`
2. 이름: `ItemDetailPopup`
3. Canvas 직속으로 이동 (다른 패널들과 같은 레벨)
4. **RectTransform 설정:**
   - Anchor Presets: `Stretch (전체)`
   - Left/Right/Top/Bottom: `0`
   - Position Z: `0`

**ItemDetailPopup 컴포넌트 추가:**
- `Add Component` → `ItemDetailPopup` (스크립트)

---

### **1-2. Background 생성 (반투명 배경)**

```
ItemDetailPopup
└── Background ← 생성
```

**작업:**
1. `ItemDetailPopup` 우클릭 → `UI` → `Image`
2. 이름: `Background`
3. **RectTransform 설정:**
   - Anchor Presets: `Stretch (전체)`
   - Left/Right/Top/Bottom: `0`

4. **Image 설정:**
   - Source Image: `None` (또는 기본 스프라이트)
   - Color: `검은색 (R:0, G:0, B:0, A:180)` ← 반투명

5. **Button 컴포넌트 추가:**
   - `Add Component` → `Button`
   - Transition: `None` (배경은 시각적 반응 없음)
   - **중요: On Click() 이벤트는 나중에 Inspector에서 연결**

---

### **1-3. PopupPanel 생성 (실제 팝업 패널)**

```
ItemDetailPopup
├── Background
└── PopupPanel ← 생성
```

**작업:**
1. `ItemDetailPopup` 우클릭 → `UI` → `Image`
2. 이름: `PopupPanel`
3. **RectTransform 설정:**
   - Anchor Presets: `Middle Center`
   - Width: `500`
   - Height: `600`
   - Position: `(0, 0, 0)`

4. **Image 설정:**
   - Source Image: `UI-Default Background` (또는 커스텀 패널 스프라이트)
   - Color: `흰색` 또는 원하는 색상
   - Image Type: `Sliced` (9-slice)

---

## 📍 Step 2: PopupPanel 하위 UI 요소 생성 (20분)

### **2-1. Header 그룹**

```
PopupPanel
├── Header
│   ├── TitleText
│   └── CloseButton
```

**Header:**
1. `PopupPanel` 우클릭 → `Create Empty`
2. 이름: `Header`
3. **RectTransform:**
   - Anchor: `Top Stretch`
   - Height: `60`
   - Position Y: `-30`

**TitleText:**
1. `Header` 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름: `TitleText`
3. **RectTransform:**
   - Anchor: `Stretch`
   - Left: `20`, Right: `80`, Top: `10`, Bottom: `10`
4. **TextMeshProUGUI:**
   - Text: `"아이템 정보"`
   - Font Size: `24`
   - Alignment: `Left, Middle`

**CloseButton:**
1. `Header` 우클릭 → `UI` → `Button - TextMeshPro`
2. 이름: `CloseButton`
3. **RectTransform:**
   - Anchor: `Top Right`
   - Width: `50`, Height: `50`
   - Position: `(-25, -25, 0)`
4. **Text 수정:**
   - Text: `"X"`
   - Font Size: `28`
   - Alignment: `Center`

---

### **2-2. Content 그룹 (아이템 정보)**

```
PopupPanel
├── Header
└── Content
    ├── ItemIcon
    ├── ItemNameText
    ├── ItemGradeText
    └── StatsGroup
        ├── Stat1Text
        ├── Stat2Text
        └── Stat3Text
```

**Content:**
1. `PopupPanel` 우클릭 → `Create Empty`
2. 이름: `Content`
3. **RectTransform:**
   - Anchor: `Top Stretch`
   - Height: `350`
   - Position Y: `-90` (Header 아래)

**ItemIcon:**
1. `Content` 우클릭 → `UI` → `Image`
2. 이름: `ItemIcon`
3. **RectTransform:**
   - Anchor: `Top Center`
   - Width: `120`, Height: `120`
   - Position: `(0, -60, 0)`
4. **Image:**
   - Preserve Aspect: `✓`

**ItemNameText:**
1. `Content` 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름: `ItemNameText`
3. **RectTransform:**
   - Anchor: `Top Stretch`
   - Height: `40`
   - Position Y: `-140`
   - Left: `20`, Right: `20`
4. **TextMeshProUGUI:**
   - Text: `"아이템 이름"`
   - Font Size: `22`
   - Alignment: `Center`
   - Font Style: `Bold`

**ItemGradeText:**
1. `Content` 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름: `ItemGradeText`
3. **RectTransform:**
   - Anchor: `Top Stretch`
   - Height: `30`
   - Position Y: `-180`
   - Left: `20`, Right: `20`
4. **TextMeshProUGUI:**
   - Text: `"등급: S"`
   - Font Size: `18`
   - Alignment: `Center`

**StatsGroup:**
1. `Content` 우클릭 → `Create Empty`
2. 이름: `StatsGroup`
3. **RectTransform:**
   - Anchor: `Top Stretch`
   - Height: `120`
   - Position Y: `-230`

**Stat1Text, Stat2Text, Stat3Text:**
- 각각 `StatsGroup` 하위에 `UI` → `Text - TextMeshPro` 생성
- **RectTransform:** (Anchor: Top Stretch, Height: 30)
  - Stat1Text: Position Y: `-15`, Left/Right: `20`
  - Stat2Text: Position Y: `-50`, Left/Right: `20`
  - Stat3Text: Position Y: `-85`, Left/Right: `20`
- **TextMeshProUGUI:**
  - Font Size: `16`
  - Alignment: `Left, Middle`
  - Text: `"공격력: +10"`, `"방어력: +5"`, `"이동속도: +1.0"`

---

### **2-3. PrimaryActionGroup (착용/해제 버튼)**

```
PopupPanel
├── Header
├── Content
└── PrimaryActionGroup
    ├── PrimaryActionButton
    │   └── ButtonText
    └── WarningText
```

**PrimaryActionGroup:**
1. `PopupPanel` 우클릭 → `Create Empty`
2. 이름: `PrimaryActionGroup`
3. **RectTransform:**
   - Anchor: `Bottom Stretch`
   - Height: `150`
   - Position Y: `75`

**PrimaryActionButton:**
1. `PrimaryActionGroup` 우클릭 → `UI` → `Button - TextMeshPro`
2. 이름: `PrimaryActionButton`
3. **RectTransform:**
   - Anchor: `Middle Center`
   - Width: `200`, Height: `50`
   - Position: `(0, 45, 0)`

**ButtonText:**
- 자동 생성된 Text 수정
- 이름: `ButtonText`
- **TextMeshProUGUI:**
  - Text: `"착용"` (기본값)
  - Font Size: `20`
  - Alignment: `Center`
  - Font Style: `Bold`

**WarningText:** ⭐ 새로 추가
1. `PrimaryActionGroup` 우클릭 → `UI` → `Text - TextMeshPro`
2. 이름: `WarningText`
3. **RectTransform:**
   - Anchor: `Middle Center`
   - Width: `360`, Height: `40`
   - Position: `(0, -15, 0)` ← 버튼 아래
4. **TextMeshProUGUI:**
   - Text: `"이 아이템은 전사 전용입니다"` (기본값, 초기 비활성화)
   - Font Size: `16`
   - Color: `주황색 (255, 128, 0, 255)`
   - Alignment: `Center`
   - Font Style: `Bold`
5. **초기 상태:**
   - GameObject 체크박스: `☐` (비활성화) ← 스크립트가 필요할 때만 표시

---

### **2-4. AdvancedActionGroup (고급 액션 버튼)**

```
PopupPanel
└── AdvancedActionGroup
    ├── DismantleButton
    ├── EnhanceButton
    └── FusionButton
```

**AdvancedActionGroup:**
1. `PopupPanel` 우클릭 → `Create Empty`
2. 이름: `AdvancedActionGroup`
3. **RectTransform:**
   - Anchor: `Bottom Stretch`
   - Height: `60`
   - Position Y: `30`

**DismantleButton:**
1. `AdvancedActionGroup` 우클릭 → `UI` → `Button - TextMeshPro`
2. 이름: `DismantleButton`
3. **RectTransform:**
   - Anchor: `Middle Center`
   - Width: `140`, Height: `40`
   - Position: `(-150, 0, 0)` ← 왼쪽
4. **Text:** `"분해하기"`, Font Size: `16`

**EnhanceButton:**
1. 동일하게 생성
2. Position: `(0, 0, 0)` ← 중앙
3. **Text:** `"강화하기"`

**FusionButton:**
1. 동일하게 생성
2. Position: `(150, 0, 0)` ← 오른쪽
3. **Text:** `"합성하기"`

---

## 📍 Step 3: Inspector 필드 연결 (10분)

### **ItemDetailPopup 스크립트 필드 연결**

`ItemDetailPopup` GameObject 선택 → Inspector에서:

**🎯 팝업 컨테이너:**
- `Popup Panel`: `PopupPanel` GameObject 드래그
- `Background Panel`: `Background` GameObject 드래그

**🎨 아이템 정보 UI:**
- `Item Icon`: `Content/ItemIcon` 드래그
- `Item Name Text`: `Content/ItemNameText` 드래그
- `Item Grade Text`: `Content/ItemGradeText` 드래그
- `Stat1 Text`: `Content/StatsGroup/Stat1Text` 드래그
- `Stat2 Text`: `Content/StatsGroup/Stat2Text` 드래그
- `Stat3 Text`: `Content/StatsGroup/Stat3Text` 드래그

**🔘 기본 액션 버튼:**
- `Primary Action Group`: `PrimaryActionGroup` GameObject 드래그
- `Primary Action Button`: `PrimaryActionGroup/PrimaryActionButton` 드래그
- `Primary Action Button Text`: `PrimaryActionGroup/PrimaryActionButton/ButtonText` 드래그
- `Warning Text`: `PrimaryActionGroup/WarningText` 드래그 ⭐ 새로 추가

**⚡ 고급 액션 버튼:**
- `Advanced Action Group`: `AdvancedActionGroup` GameObject 드래그
- `Dismantle Button`: `AdvancedActionGroup/DismantleButton` 드래그
- `Enhance Button`: `AdvancedActionGroup/EnhanceButton` 드래그
- `Fusion Button`: `AdvancedActionGroup/FusionButton` 드래그

**🔧 제어 버튼:**
- `Close Button`: `Header/CloseButton` 드래그
- `Background Button`: `Background` 드래그

**📊 디버그:**
- `Show Debug Logs`: `✓` (체크)

---

## 📍 Step 4: 초기 상태 설정 (5분)

### **ItemDetailPopup GameObject 설정:**
1. `ItemDetailPopup` GameObject 선택
2. Inspector 최상단 체크박스: `✓` (활성화 상태 유지)
3. **PopupPanel 비활성화:**
   - `PopupPanel` GameObject 선택
   - Inspector 최상단 체크박스: `☐` (비활성화)
   - → 스크립트가 필요할 때만 활성화

---

## ✅ 최종 확인 체크리스트

- [ ] Canvas 직속에 `ItemDetailPopup` 존재
- [ ] `Background` + `PopupPanel` 구조 완성
- [ ] Header (Title + CloseButton) 생성
- [ ] Content (Icon + Name + Grade + Stats) 생성
- [ ] PrimaryActionGroup (착용/해제 버튼 + WarningText) 생성 ⭐
- [ ] AdvancedActionGroup (분해/강화/합성 버튼) 생성
- [ ] Inspector 필드 17개 모두 연결 ⭐ (WarningText 추가)
- [ ] PopupPanel 초기 비활성화 ✅
- [ ] WarningText 초기 비활성화 ✅ ⭐
- [ ] Background Button 컴포넌트 추가 ✅

---

## 🎨 레이아웃 참고

```
┌─────────────────────────────────────┐
│ Background (반투명 검은색)           │
│  ┌───────────────────────────────┐  │
│  │ 아이템 정보           [X]     │  │ ← Header
│  ├───────────────────────────────┤  │
│  │                               │  │
│  │        [아이템 아이콘]        │  │
│  │                               │  │
│  │      검은 그림자 검           │  │ ← Content
│  │          등급: S              │  │
│  │                               │  │
│  │  공격력: +10                  │  │
│  │  방어력: +5                   │  │
│  │  이동속도: +1.0               │  │
│  │                               │  │
│  ├───────────────────────────────┤  │
│  │        [ 착용 ]               │  │ ← PrimaryActionGroup
│  │ 이 아이템은 전사 전용입니다   │  │ ← WarningText ⭐
│  ├───────────────────────────────┤  │
│  │ [분해] [강화] [합성]          │  │ ← AdvancedActionGroup
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘
```

---

## 🚀 다음 단계

**Phase 3: LobbyInventoryUI.cs 정리**
- DetailPanel 관련 코드 제거
- 약 300줄 감소 예상

설정 완료 후 Phase 3 진행 요청해주세요! 🎉

