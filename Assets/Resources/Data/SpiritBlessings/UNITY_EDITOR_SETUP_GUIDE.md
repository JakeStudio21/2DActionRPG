# 📐 정령의 가호 UI - Unity Editor 설정 가이드

## 🎯 전체 구조 (최종)

```
SkillBookPanel (GameObject)
├─ TopPanel (탭 버튼)
├─ SkillSubPanel (GameObject)
│   └─ SkillTabController (컴포넌트)
├─ SpiritStoneSubPanel (GameObject)
│   └─ RunePanelUI (컴포넌트)
└─ SpiritBlessingSubPanel (GameObject) 🆕
    └─ SpiritBlessingTabController (컴포넌트) 🆕

SkillBookPanelUI (컴포넌트 on SkillBookPanel)
├─ skillTabController ────────────> SkillTabController
├─ runePanelUI ───────────────────> RunePanelUI
└─ spiritBlessingTabController ──> SpiritBlessingTabController 🆕
```

---

## 📋 **Step 1: TopPanel 탭 버튼 추가**

### **작업 경로:**
`SkillBookPanel > TopPanel > TabButtons`

### **작업 순서:**

1. **RuneTabButton 복제**
   - `TabButtons`에서 `RuneTabButton` 우클릭 → Duplicate
   
2. **이름 변경**
   - 복제된 버튼 이름: `SpiritBlessingTabButton`
   
3. **텍스트 수정**
   - `SpiritBlessingTabButton > Text (TMP)` 선택
   - Text: "정령의 가호"
   - Font Size: 기존 버튼과 동일 (약 16~18pt)

4. **위치 조정**
   - Horizontal Layout Group이 자동으로 배치함
   - 순서: [스킬] [정령 수호석] [정령의 가호]

---

## 📋 **Step 2: SpiritBlessingSubPanel 생성**

### **작업 경로:**
`SkillBookPanel` 하위

### **작업 순서:**

1. **빈 GameObject 생성**
   - `SkillBookPanel` 우클릭 → Create Empty
   - 이름: `SpiritBlessingSubPanel`

2. **RectTransform 설정**
   ```
   Anchor Presets: Stretch (전체 화면)
   - Anchor Min: (0, 0)
   - Anchor Max: (1, 1)
   - Left: 0
   - Right: 0
   - Top: 0
   - Bottom: 0
   ```

3. **초기 상태**
   - Active: ❌ false (체크 해제)

---

## 📋 **Step 3: SpiritBlessingSubPanel 내부 구조**

### **3-1. LeftPanel (좌측: 종합 스탯)**

```
SpiritBlessingSubPanel
└─ LeftPanel (Panel, Vertical Layout Group)
    ├─ TitleText (TMP) - "정령의 가호"
    └─ BlessingStatContainer (Empty, Vertical Layout Group)
        ├─ BindResistText (TMP)
        ├─ PoisonResistText (TMP)
        ├─ BurnResistText (TMP)
        └─ SlowResistText (TMP)
```

**LeftPanel 설정:**
- RectTransform: Left 정렬, Width 250px
- Vertical Layout Group:
  - Padding: (10, 10, 10, 10)
  - Spacing: 10
  - Child Alignment: Upper Left

**BlessingStatContainer 설정:**
- Vertical Layout Group:
  - Spacing: 5
  - Child Force Expand: Width ✅

**텍스트 초기값:**
- BindResistText: "🌳 숲의 가호 (속박 내성): 0%"
- PoisonResistText: "☠️ 독의 가호 (독 내성): 0%"
- BurnResistText: "🔥 불의 가호 (화상 내성): 0%"
- SlowResistText: "❄️ 얼음의 가호 (둔화 내성): 0%"

---

### **3-2. RightPanel (우측: 정령 리스트)**

```
SpiritBlessingSubPanel
└─ RightPanel (Panel)
    ├─ TitleText (TMP) - "정령 선택"
    └─ Scroll View
        ├─ Viewport
        │   └─ Content (Vertical Layout Group)
        └─ Scrollbar Vertical
```

**RightPanel 설정:**
- RectTransform: Center 정렬, Width 400px

**Scroll View 설정:**
- Movement Type: Elastic
- Scroll Sensitivity: 20
- Viewport: Viewport
- Content: Content
- Vertical Scrollbar: Scrollbar Vertical

**Content 설정:**
- Vertical Layout Group:
  - Spacing: 10
  - Child Force Expand: Width ✅
- Content Size Fitter:
  - Vertical Fit: Preferred Size

---

### **3-3. BottomPanel (하단: 상세 정보)**

```
SpiritBlessingSubPanel
└─ BottomPanel (Panel)
    ├─ BlessingIcon (Image, 80×80px)
    ├─ BlessingNameText (TMP)
    ├─ DescriptionText (TMP)
    ├─ EffectText (TMP)
    ├─ CostText (TMP)
    └─ ReceiveButton (Button)
        └─ Text (TMP) - "가호 받기"
```

**BottomPanel 설정:**
- RectTransform: Bottom 정렬, Height 150px
- Horizontal Layout Group (Optional)

**초기 상태:**
- Active: ❌ false (선택 전까지 숨김)

---

## 📋 **Step 4: SpiritBlessingItemUI 프리팹 생성**

### **프리팹 경로:**
`Assets/Prefabs/UI/SpiritBlessing/SpiritBlessingItemUI.prefab`

### **프리팹 구조 (300px × 80px):**

```
SpiritBlessingItemUI (GameObject + SpiritBlessingItemUI.cs)
├─ BackgroundImage (Image)
├─ ItemButton (Button) - 전체 클릭용
├─ ContentPanel (Horizontal Layout Group)
│   ├─ BlessingIcon (Image, 60×60px)
│   ├─ InfoPanel (Vertical Layout Group, Width 140px)
│   │   ├─ BlessingNameText (TMP, 14pt)
│   │   ├─ EffectTypeText (TMP, 12pt, 회색)
│   │   └─ CurrentValueText (TMP, 12pt)
│   └─ ReceiveButton (Button, Width 80px)
│       └─ Text (TMP) - "가호 받기"
└─ MaxLevelOverlay (Image, 반투명 회색, Active: false)
    └─ MaxLevelText (TMP) - "최대치 달성"
```

### **컴포넌트 연결 (SpiritBlessingItemUI.cs):**

```
=== UI 참조 ===
Blessing Icon: BlessingIcon
Blessing Name Text: BlessingNameText
Effect Type Text: EffectTypeText
Current Value Text: CurrentValueText
Item Button: ItemButton
Receive Button: ReceiveButton
Receive Button Text: ReceiveButton/Text
Max Level Overlay: MaxLevelOverlay
Max Level Text: MaxLevelText
```

---

## 📋 **Step 5: SpiritBlessingTabController 컴포넌트 추가**

### **작업 경로:**
`SpiritBlessingSubPanel` GameObject 선택

### **작업 순서:**

1. **컴포넌트 추가**
   - Inspector → Add Component
   - 검색: "SpiritBlessingTabController"
   - 추가

2. **필드 연결:**

```
=== 좌측: 종합 스탯 ===
Bind Resist Text: LeftPanel/BlessingStatContainer/BindResistText
Poison Resist Text: LeftPanel/BlessingStatContainer/PoisonResistText
Burn Resist Text: LeftPanel/BlessingStatContainer/BurnResistText
Slow Resist Text: LeftPanel/BlessingStatContainer/SlowResistText

=== 우측: 정령 리스트 ===
Blessing List Parent: RightPanel/Scroll View/Viewport/Content
Blessing Item Prefab: SpiritBlessingItemUI.prefab (드래그)
Blessing Database: Size 4
  [0]: SpiritBlessing_Bind.asset
  [1]: SpiritBlessing_Poison.asset
  [2]: SpiritBlessing_Burn.asset
  [3]: SpiritBlessing_Slow.asset

=== 하단: 상세 정보 ===
Bottom Panel: BottomPanel (GameObject)
Blessing Icon: BottomPanel/BlessingIcon
Blessing Name Text: BottomPanel/BlessingNameText
Description Text: BottomPanel/DescriptionText
Effect Text: BottomPanel/EffectText
Cost Text: BottomPanel/CostText
Receive Button: BottomPanel/ReceiveButton
Receive Button Text: BottomPanel/ReceiveButton/Text

=== 디버그 ===
Enable Debug Logs: ✅ true (테스트 시)
```

---

## 📋 **Step 6: SkillBookPanelUI Inspector 연결**

### **작업 경로:**
`SkillBookPanel` GameObject 선택 → SkillBookPanelUI 컴포넌트

### **필드 연결:**

```
=== 📑 탭 버튼 ===
Skill Tab Button: TopPanel/TabButtons/SkillTabButton
Spirit Stone Tab Button: TopPanel/TabButtons/RuneTabButton
Spirit Blessing Tab Button: TopPanel/TabButtons/SpiritBlessingTabButton 🆕

=== 📑 탭 텍스트 ===
Skill Tab Text: SkillTabButton/Text
Spirit Stone Tab Text: RuneTabButton/Text
Spirit Blessing Tab Text: SpiritBlessingTabButton/Text 🆕

=== 📦 서브 패널 ===
Skill Sub Panel: SkillSubPanel
Spirit Stone Sub Panel: RuneSubPanel (또는 SpiritStoneSubPanel)
Spirit Blessing Sub Panel: SpiritBlessingSubPanel 🆕

=== 🔘 공통 버튼 ===
Close Button: TopPanel/CloseButton

=== 🎮 탭 컨트롤러 ===
Skill Tab Controller: SkillSubPanel의 SkillTabController
Rune Panel UI: SpiritStoneSubPanel의 RunePanelUI 🆕
Spirit Blessing Tab Controller: SpiritBlessingSubPanel의 SpiritBlessingTabController 🆕

=== 📊 디버그 ===
Show Debug Logs: ✅ true (테스트 시)
```

---

## 📋 **Step 7: ScriptableObject 에셋 생성**

### **생성 경로:**
`Assets/Resources/Data/SpiritBlessings/`

### **생성 방법:**

1. **폴더 우클릭** → Create → Data → Spirit Blessing
2. **파일명 변경 및 데이터 입력**

### **생성할 에셋 (4개):**

#### **1. SpiritBlessing_Bind.asset**
```
Target Effect Type: Bind
Blessing Name: 숲의 정령의 가호
Blessing Icon: (숲/나무 아이콘)
Description: 깊은 숲의 정령이 내리는 가호입니다.
Increment Per Level: 0.03
Max Resistance: 1.0
Cost Per Level: 10
Currency Name: 정화의 이슬
```

#### **2. SpiritBlessing_Poison.asset**
```
Target Effect Type: Poison
Blessing Name: 독의 정령의 가호
Blessing Icon: (독/해골 아이콘)
Description: 어둠 속에서 깨어난 정령이 내리는 가호입니다.
Increment Per Level: 0.03
Max Resistance: 1.0
Cost Per Level: 10
Currency Name: 정화의 이슬
```

#### **3. SpiritBlessing_Burn.asset**
```
Target Effect Type: Burn
Blessing Name: 불의 정령의 가호
Blessing Icon: (불꽃 아이콘)
Description: 작열하는 불꽃의 정령이 내리는 가호입니다.
Increment Per Level: 0.03
Max Resistance: 1.0
Cost Per Level: 10
Currency Name: 정화의 이슬
```

#### **4. SpiritBlessing_Slow.asset**
```
Target Effect Type: Slow
Blessing Name: 얼음의 정령의 가호
Blessing Icon: (눈송이/얼음 아이콘)
Description: 얼어붙은 땅의 정령이 내리는 가호입니다.
Increment Per Level: 0.03
Max Resistance: 1.0
Cost Per Level: 10
Currency Name: 정화의 이슬
```

---

## 🧪 **Step 8: 테스트**

### **테스트 시나리오:**

```
1. Play 모드 진입
2. 캐릭터 선택
3. "스킬&룬" 버튼 클릭
4. [정령의 가호] 탭 클릭
   ✅ 좌측: 종합 스탯 4개 표시 (모두 0%)
   ✅ 우측: 정령 리스트 4개 표시
   ✅ 하단: 비활성화 상태
5. 정령 클릭 (예: 숲의 정령의 가호)
   ✅ 하단: 상세 정보 표시
   ✅ "다음 가호 받기 시: 속박 내성 0% → 3%"
6. [가호 받기] 버튼 클릭
   ✅ 좌측: 속박 내성 0% → 3%
   ✅ 우측: 현재값 0% → 3%
   ✅ 하단: "0% → 3%" → "3% → 6%"
   ✅ Console: "✅ [SpiritBlessingTabController] 저항 증가: 속박 내성 0% → 3%"
   ✅ Console: "✅ [SpiritBlessingTabController] 데이터 저장 완료"
7. 게임 종료 후 재시작
   ✅ 저항값 유지 (JSON 로드)
   ✅ 좌측: 속박 내성 3%
8. 30번 클릭 (3% × 30 = 90%)
9. 4번 더 클릭 (90% + 12% = 100%)
   ✅ 버튼 비활성화: "최대치 달성"
   ✅ MaxLevelOverlay 표시
```

---

## 🚨 **문제 해결**

### **Q1: 리스트가 비어있음**
- SpiritBlessingTabController의 Blessing Database 확인
- 4개 에셋이 모두 연결되었는지 확인
- Console에서 에러 메시지 확인

### **Q2: 탭 전환이 안 됨**
- SkillBookPanelUI의 필드 연결 확인:
  - Spirit Blessing Tab Button
  - Spirit Blessing Sub Panel
  - Spirit Blessing Tab Controller
- 모두 null이 아닌지 확인

### **Q3: [가호 받기] 버튼이 안 눌림**
- PlayerResistanceStats가 Player GameObject에 있는지 확인
- Console에서 "PlayerResistanceStats를 찾을 수 없습니다" 에러 확인

### **Q4: 저항값이 증가하지 않음**
- PlayerResistanceStats.AddResistance() 호출 확인
- Console 로그 확인 (Enable Debug Logs = true)
- Inspector에서 PlayerResistanceStats 값 실시간 확인

### **Q5: 저장이 안 됨**
- PlayerDataManager.SaveOnMeaningfulEvent() 호출 확인
- JSON 파일 경로: StreamingAssets/SaveData/PlayerSlot{N}.json
- resistanceStats 필드 존재 여부 확인

---

## ✅ **최종 체크리스트**

- [ ] TopPanel에 "정령의 가호" 탭 버튼 추가
- [ ] SpiritBlessingSubPanel Hierarchy 구조 완성
- [ ] SpiritBlessingItemUI 프리팹 제작
- [ ] SpiritBlessingTabController 컴포넌트 추가 및 필드 연결
- [ ] SkillBookPanelUI 필드 연결 (3개 컨트롤러)
- [ ] ScriptableObject 에셋 4개 생성
- [ ] 에셋을 Blessing Database에 연결
- [ ] Play 모드 테스트 (리스트 표시)
- [ ] [가호 받기] 버튼 테스트 (저항 증가)
- [ ] 저장/로드 테스트 (게임 재시작)

---

## 🎉 **완료!**

모든 설정이 완료되면 정령의 가호 시스템이 정상 작동합니다! 🌟

