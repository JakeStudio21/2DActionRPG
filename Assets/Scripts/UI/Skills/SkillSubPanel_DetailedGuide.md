# 📚 SkillSubPanel 상세 제작 가이드

## Unity Editor에서 LeftPanel과 RightPanel 만들기

---

## 🎯 **1. SkillSubPanel 기본 구조**

### **전체 레이아웃**

```
SkillSubPanel (GameObject)
├─ LeftPanel (좌측 20%, 세로 배치)
├─ RightPanel (우측 50%, 세로 배치)
└─ BottomPanel (하단 30%, 가로 전체)
```

### **SkillSubPanel 설정**

1. **GameObject 생성**
   - Hierarchy 우클릭 → UI → Panel
   - 이름: `SkillSubPanel`

2. **RectTransform 설정**
   - Anchor: Stretch-Stretch (좌우상하 전체)
   - Left: 0, Right: 0, Top: 50 (TopPanel 아래), Bottom: 0

3. **Component 추가**
   - Add Component → `SkillTabController` (C# 스크립트)

---

## 📦 **2. LeftPanel (좌측 장착 슬롯) 상세**

### **2-1. LeftPanel GameObject 생성**

1. **SkillSubPanel 우클릭 → Create Empty**
   - 이름: `LeftPanel`

2. **RectTransform 설정**
   ```
   Anchor Preset: Left-Stretch (좌측 고정, 세로 확장)
   Position: X=0, Y=0, Z=0
   Width: 200 (픽셀)
   Left: 10, Right: 210, Top: 10, Bottom: 10
   ```

3. **Component 추가**
   - Add Component → Vertical Layout Group
     - Child Force Expand: Width ✅, Height ❌
     - Child Control Size: Width ✅, Height ❌
     - Spacing: 10
     - Padding: Left 10, Right 10, Top 10, Bottom 10
   
   - Add Component → Image (배경색)
     - Color: RGB(50, 50, 50, 100) - 반투명 어두운 배경

### **2-2. LeftPanel 내부 구조**

```
LeftPanel
├─ TitleText (TextMeshProUGUI)
├─ ActiveSlotsTitle (TextMeshProUGUI)
├─ ActiveSlot_0 (GameObject + SkillEquipSlotUI)
├─ ActiveSlot_1 (GameObject + SkillEquipSlotUI)
├─ PassiveSlotsTitle (TextMeshProUGUI)
├─ PassiveSlot_0 (GameObject + SkillEquipSlotUI)
├─ PassiveSlot_1 (GameObject + SkillEquipSlotUI)
└─ PassiveSlot_2 (GameObject + SkillEquipSlotUI)
```

### **2-3. TitleText 생성**

1. **LeftPanel 우클릭 → UI → Text - TextMeshPro**
   - 이름: `TitleText`

2. **RectTransform 설정**
   ```
   Height: 30
   Anchor: Top-Center
   ```

3. **TextMeshProUGUI 설정**
   ```
   Text: "장착 스킬"
   Font Size: 18
   Color: White
   Alignment: Center-Middle
   Font Style: Bold
   ```

### **2-4. ActiveSlotsTitle 생성**

1. **LeftPanel 우클릭 → UI → Text - TextMeshPro**
   - 이름: `ActiveSlotsTitle`

2. **RectTransform 설정**
   ```
   Height: 25
   ```

3. **TextMeshProUGUI 설정**
   ```
   Text: "━━ 액티브 ━━"
   Font Size: 14
   Color: RGB(100, 200, 255) - 연한 파란색
   Alignment: Center-Middle
   ```

### **2-5. ActiveSlot_0 생성 (프리팹)**

**먼저 프리팹 제작 (한 번만):**

1. **Scene에 임시 GameObject 생성**
   - Hierarchy 우클릭 → Create Empty
   - 이름: `SkillEquipSlotPrefab`

2. **RectTransform 설정**
   ```
   Width: 100
   Height: 120
   ```

3. **Component 추가**
   - Add Component → `SkillEquipSlotUI` (C# 스크립트)
   - Add Component → Image (배경)
     - Color: RGB(70, 70, 70, 200)
     - Raycast Target: ✅ (클릭 가능)

4. **자식 오브젝트 추가**

**a. BackgroundImage** (이미 추가됨, Image 컴포넌트)

**b. SkillIcon 생성**
   - 우클릭 → UI → Image
   - 이름: `SkillIcon`
   - RectTransform:
     ```
     Anchor: Center-Center
     Width: 80, Height: 80
     Position: X=0, Y=5, Z=0
     ```
   - Image:
     ```
     Sprite: None (비어있음)
     Preserve Aspect: ✅
     ```

**c. SlotNumberText 생성**
   - 우클릭 → UI → Text - TextMeshPro
   - 이름: `SlotNumberText`
   - RectTransform:
     ```
     Anchor: Top-Center
     Width: 100, Height: 20
     Position: X=0, Y=-10, Z=0
     ```
   - TextMeshProUGUI:
     ```
     Text: "액티브 1"
     Font Size: 12
     Color: RGB(200, 200, 200)
     Alignment: Center-Middle
     ```

**d. SkillLevelText 생성**
   - 우클릭 → UI → Text - TextMeshPro
   - 이름: `SkillLevelText`
   - RectTransform:
     ```
     Anchor: Bottom-Right
     Width: 40, Height: 20
     Position: X=-5, Y=5, Z=0
     ```
   - TextMeshProUGUI:
     ```
     Text: "Lv.2"
     Font Size: 14
     Color: RGB(255, 220, 100) - 황금색
     Alignment: Right-Middle
     Font Style: Bold
     ```

**e. EmptyOverlay 생성**
   - 우클릭 → UI → Image
   - 이름: `EmptyOverlay`
   - RectTransform:
     ```
     Anchor: Stretch-Stretch
     Left/Right/Top/Bottom: 0
     ```
   - Image:
     ```
     Color: RGB(30, 30, 30, 150) - 반투명 어두운색
     ```

**f. EmptyText 생성 (EmptyOverlay 자식)**
   - EmptyOverlay 우클릭 → UI → Text - TextMeshPro
   - 이름: `EmptyText`
   - RectTransform:
     ```
     Anchor: Center-Center
     Width: 80, Height: 30
     ```
   - TextMeshProUGUI:
     ```
     Text: "빈 슬롯"
     Font Size: 12
     Color: RGB(150, 150, 150)
     Alignment: Center-Middle
     ```

5. **SkillEquipSlotUI Inspector 연결**
   - Skill Icon: SkillIcon 드래그
   - Background Image: SkillEquipSlotPrefab의 Image 컴포넌트 드래그
   - Slot Number Text: SlotNumberText 드래그
   - Skill Level Text: SkillLevelText 드래그
   - Empty Overlay: EmptyOverlay 드래그

6. **프리팹으로 저장**
   - SkillEquipSlotPrefab을 Project 창 `Assets/Prefabs/UI/Skills/` 폴더로 드래그
   - Hierarchy에서 원본 삭제

**이제 LeftPanel에 배치:**

1. **LeftPanel에 프리팹 배치**
   - SkillEquipSlotPrefab을 LeftPanel으로 드래그
   - 이름: `ActiveSlot_0`

2. **SlotNumberText 수정**
   - Text: "액티브 1"

3. **SkillEquipSlotUI 설정**
   - Slot Index: 0
   - Is Active Slot: ✅ true
   - Show Debug Logs: ❌ (필요 시 체크)

4. **같은 방법으로 ActiveSlot_1 생성**
   - SlotNumberText: "액티브 2"
   - Slot Index: 1
   - Is Active Slot: ✅ true

### **2-6. PassiveSlotsTitle 생성**

1. **ActiveSlotsTitle 복제**
   - ActiveSlotsTitle 우클릭 → Duplicate
   - 이름: `PassiveSlotsTitle`

2. **TextMeshProUGUI 수정**
   ```
   Text: "━━ 패시브 ━━"
   Color: RGB(100, 255, 150) - 연한 초록색
   ```

### **2-7. PassiveSlot_0, 1, 2 생성**

1. **프리팹 배치 (3개)**
   - SkillEquipSlotPrefab을 LeftPanel으로 드래그 (3번)
   - 이름:
     - PassiveSlot_0
     - PassiveSlot_1
     - PassiveSlot_2

2. **각 슬롯 설정**

**PassiveSlot_0:**
   - SlotNumberText: "패시브 1"
   - Slot Index: 0
   - Is Active Slot: ❌ false

**PassiveSlot_1:**
   - SlotNumberText: "패시브 2"
   - Slot Index: 1
   - Is Active Slot: ❌ false

**PassiveSlot_2:**
   - SlotNumberText: "패시브 3"
   - Slot Index: 2
   - Is Active Slot: ❌ false

---

## 📋 **3. RightPanel (우측 스킬 리스트) 상세**

### **3-1. RightPanel GameObject 생성**

1. **SkillSubPanel 우클릭 → Create Empty**
   - 이름: `RightPanel`

2. **RectTransform 설정**
   ```
   Anchor Preset: Center-Stretch (중앙, 세로 확장)
   Position: X=300, Y=0, Z=0
   Width: 400 (픽셀)
   Top: 10, Bottom: 10
   ```

3. **Component 추가**
   - Add Component → Vertical Layout Group
     - Child Force Expand: Width ✅, Height ❌
     - Child Control Size: Width ✅, Height ❌
     - Spacing: 20
     - Padding: Left 10, Right 10, Top 10, Bottom 10
   
   - Add Component → Image (배경색)
     - Color: RGB(60, 60, 60, 100) - 반투명 배경

### **3-2. RightPanel 내부 구조**

```
RightPanel
├─ ActiveSkillSection (GameObject)
│   ├─ TitleText (TextMeshProUGUI)
│   └─ Scroll View (ScrollRect)
│       └─ Viewport (Mask)
│           └─ Content (Vertical Layout Group)
│               ├─ SkillListItemUI (프리팹, 동적 생성)
│               ├─ SkillListItemUI (프리팹, 동적 생성)
│               └─ ...
│
└─ PassiveSkillSection (GameObject)
    ├─ TitleText (TextMeshProUGUI)
    └─ Scroll View (ScrollRect)
        └─ Viewport (Mask)
            └─ Content (Vertical Layout Group)
                ├─ SkillListItemUI (프리팹, 동적 생성)
                └─ ...
```

### **3-3. ActiveSkillSection 생성**

1. **RightPanel 우클릭 → Create Empty**
   - 이름: `ActiveSkillSection`

2. **RectTransform 설정**
   ```
   Height: 250 (픽셀)
   ```

3. **Component 추가**
   - Add Component → Vertical Layout Group
     - Child Force Expand: Width ✅, Height ❌
     - Child Control Size: Width ✅, Height ❌
     - Spacing: 5

### **3-4. ActiveSkillSection TitleText 생성**

1. **ActiveSkillSection 우클릭 → UI → Text - TextMeshPro**
   - 이름: `TitleText`

2. **RectTransform 설정**
   ```
   Height: 30
   ```

3. **TextMeshProUGUI 설정**
   ```
   Text: "⚔️ 액티브 스킬"
   Font Size: 16
   Color: RGB(100, 200, 255)
   Alignment: Left-Middle
   Font Style: Bold
   ```

### **3-5. ActiveSkillSection Scroll View 생성**

1. **ActiveSkillSection 우클릭 → UI → Scroll View**
   - 이름: `Scroll View`

2. **RectTransform 설정**
   ```
   Anchor: Stretch-Stretch
   Left: 0, Right: 0, Top: 35, Bottom: 0
   ```

3. **Scroll Rect 컴포넌트 설정**
   ```
   Horizontal: ❌ (가로 스크롤 없음)
   Vertical: ✅
   Movement Type: Elastic
   Scroll Sensitivity: 20
   ```

4. **Scrollbar 삭제 (선택 사항)**
   - Scroll View 하위의 `Scrollbar Vertical` 삭제
   - Scroll Rect의 Vertical Scrollbar: None

### **3-6. Content 설정 (중요!)**

1. **Scroll View → Viewport → Content 선택**

2. **RectTransform 설정**
   ```
   Anchor: Top-Center
   Pivot: X=0.5, Y=1 (상단 중앙)
   Width: 380 (부모보다 약간 작게)
   ```

3. **Component 추가**
   - Add Component → Vertical Layout Group
     ```
     Child Force Expand: Width ✅, Height ❌
     Child Control Size: Width ✅, Height ✅
     Spacing: 5
     Padding: Left 5, Right 5, Top 5, Bottom 5
     ```
   
   - Add Component → Content Size Fitter
     ```
     Horizontal Fit: Unconstrained
     Vertical Fit: Preferred Size (중요! 자동으로 높이 조절)
     ```

### **3-7. PassiveSkillSection 생성**

1. **ActiveSkillSection 복제**
   - ActiveSkillSection 우클릭 → Duplicate
   - 이름: `PassiveSkillSection`

2. **TitleText 수정**
   ```
   Text: "🛡️ 패시브 스킬"
   Color: RGB(100, 255, 150)
   ```

3. **나머지 설정은 동일**

---

## 🎨 **4. SkillListItemUI 프리팹 제작**

### **4-1. 프리팹 기본 구조**

1. **Scene에 임시 GameObject 생성**
   - Hierarchy 우클릭 → Create Empty
   - 이름: `SkillListItemPrefab`

2. **RectTransform 설정**
   ```
   Width: 380
   Height: 80
   ```

3. **Component 추가**
   - Add Component → `SkillListItemUI` (C# 스크립트)
   - Add Component → Button (전체 클릭용)
     - Transition: Color Tint
     - Normal: RGB(255, 255, 255, 255)
     - Highlighted: RGB(245, 245, 245, 255)
     - Pressed: RGB(200, 200, 200, 255)
     - Selected: RGB(230, 240, 255, 255)
   - Add Component → Image (배경)
     - Color: RGB(80, 80, 80, 200)

### **4-2. ContentPanel 생성**

1. **SkillListItemPrefab 우클릭 → Create Empty**
   - 이름: `ContentPanel`

2. **RectTransform 설정**
   ```
   Anchor: Stretch-Stretch
   Left: 5, Right: 5, Top: 5, Bottom: 5
   ```

3. **Component 추가**
   - Add Component → Horizontal Layout Group
     ```
     Child Force Expand: Width ❌, Height ✅
     Child Control Size: Width ❌, Height ✅
     Spacing: 10
     Padding: Left 5, Right 5, Top 5, Bottom 5
     Child Alignment: Middle-Left
     ```

### **4-3. SkillIcon 생성 (ContentPanel 자식)**

1. **ContentPanel 우클릭 → UI → Image**
   - 이름: `SkillIcon`

2. **RectTransform 설정**
   ```
   Width: 70
   Height: 70
   ```

3. **Image 설정**
   ```
   Sprite: None
   Preserve Aspect: ✅
   ```

4. **Component 추가**
   - Add Component → Layout Element
     ```
     Min Width: 70
     Min Height: 70
     Preferred Width: 70
     Preferred Height: 70
     ```

### **4-4. InfoPanel 생성 (ContentPanel 자식)**

1. **ContentPanel 우클릭 → Create Empty**
   - 이름: `InfoPanel`

2. **RectTransform 설정**
   ```
   Width: 150
   ```

3. **Component 추가**
   - Add Component → Vertical Layout Group
     ```
     Child Force Expand: Width ✅, Height ❌
     Child Control Size: Width ✅, Height ❌
     Spacing: 2
     Child Alignment: Upper-Left
     ```
   
   - Add Component → Layout Element
     ```
     Min Width: 120
     Flexible Width: 1
     ```

**InfoPanel 내부 구조:**

**a. SkillNameText**
   - InfoPanel 우클릭 → UI → Text - TextMeshPro
   - 이름: `SkillNameText`
   - RectTransform: Height 22
   - TextMeshProUGUI:
     ```
     Text: "정령의 공명"
     Font Size: 16
     Color: White
     Alignment: Left-Middle
     Font Style: Bold
     Overflow: Ellipsis
     ```

**b. SkillTypeText**
   - InfoPanel 우클릭 → UI → Text - TextMeshPro
   - 이름: `SkillTypeText`
   - RectTransform: Height 18
   - TextMeshProUGUI:
     ```
     Text: "Combat"
     Font Size: 12
     Color: RGB(180, 180, 180)
     Alignment: Left-Middle
     ```

**c. SkillLevelText**
   - InfoPanel 우클릭 → UI → Text - TextMeshPro
   - 이름: `SkillLevelText`
   - RectTransform: Height 20
   - TextMeshProUGUI:
     ```
     Text: "Lv.2/5"
     Font Size: 14
     Color: RGB(255, 220, 100)
     Alignment: Left-Middle
     ```

### **4-5. ButtonPanel 생성 (ContentPanel 자식)**

1. **ContentPanel 우클릭 → Create Empty**
   - 이름: `ButtonPanel`

2. **RectTransform 설정**
   ```
   Width: 100
   ```

3. **Component 추가**
   - Add Component → Horizontal Layout Group
     ```
     Child Force Expand: Width ✅, Height ✅
     Child Control Size: Width ✅, Height ✅
     Spacing: 5
     ```
   
   - Add Component → Layout Element
     ```
     Min Width: 100
     Preferred Width: 100
     ```

**ButtonPanel 내부 구조:**

**a. UpgradeButton**
   - ButtonPanel 우클릭 → UI → Button - TextMeshPro
   - 이름: `UpgradeButton`
   - Button:
     ```
     Transition: Color Tint
     Normal: RGB(100, 200, 100, 255) - 초록색
     Highlighted: RGB(120, 220, 120, 255)
     Pressed: RGB(80, 160, 80, 255)
     Disabled: RGB(150, 150, 150, 255)
     ```
   - Text (TMP):
     ```
     Text: "레벨업"
     Font Size: 12
     Color: White
     Alignment: Center-Middle
     Font Style: Bold
     ```

**b. EquipButton**
   - ButtonPanel 우클릭 → UI → Button - TextMeshPro
   - 이름: `EquipButton`
   - Button:
     ```
     Transition: Color Tint
     Normal: RGB(100, 150, 255, 255) - 파란색
     Highlighted: RGB(120, 170, 255, 255)
     Pressed: RGB(80, 120, 200, 255)
     Disabled: RGB(150, 150, 150, 255)
     ```
   - Text (TMP):
     ```
     Text: "장착"
     Font Size: 12
     Color: White
     Alignment: Center-Middle
     Font Style: Bold
     ```

### **4-6. LockOverlay 생성 (SkillListItemPrefab 자식)**

1. **SkillListItemPrefab 우클릭 → UI → Image**
   - 이름: `LockOverlay`

2. **RectTransform 설정**
   ```
   Anchor: Stretch-Stretch
   Left/Right/Top/Bottom: 0
   ```

3. **Image 설정**
   ```
   Color: RGB(20, 20, 20, 200) - 어두운 반투명
   Raycast Target: ❌ (클릭 막지 않음)
   ```

**LockIcon 추가 (LockOverlay 자식):**
   - LockOverlay 우클릭 → UI → Image
   - 이름: `LockIcon`
   - RectTransform:
     ```
     Anchor: Center-Center
     Width: 40, Height: 40
     ```
   - Image:
     ```
     Sprite: Unity 기본 자물쇠 아이콘 또는 커스텀
     Color: RGB(255, 100, 100)
     ```

### **4-7. SkillListItemUI Inspector 연결**

**프리팹 Inspector에서 연결:**

```
🎨 기본 UI:
- Skill Icon: ContentPanel/SkillIcon
- Background Image: SkillListItemPrefab의 Image 컴포넌트
- Skill Name Text: ContentPanel/InfoPanel/SkillNameText
- Skill Level Text: ContentPanel/InfoPanel/SkillLevelText
- Skill Type Text: ContentPanel/InfoPanel/SkillTypeText
- Lock Overlay: LockOverlay

🎮 버튼:
- Upgrade Button: ContentPanel/ButtonPanel/UpgradeButton
- Upgrade Button Text: UpgradeButton/Text (TMP)
- Equip Button: ContentPanel/ButtonPanel/EquipButton
- Equip Button Text: EquipButton/Text (TMP)

🎨 상태별 색상: (기본값 유지 또는 조정)
- Locked Color: RGB(77, 77, 77, 255)
- Unlocked Color: RGB(255, 255, 255, 255)
- Equipped Color: RGB(255, 204, 77, 255)
- Selected Color: RGB(200, 230, 255, 255)
```

**Button 컴포넌트 OnClick 연결:**
   - SkillListItemPrefab의 Button 컴포넌트
   - OnClick() → SkillListItemUI.OnItemClickedFromButton()

### **4-8. 프리팹 저장**

1. **SkillListItemPrefab을 Project 창으로 드래그**
   - 경로: `Assets/Prefabs/UI/Skills/SkillListItemPrefab.prefab`

2. **Hierarchy에서 원본 삭제**

---

## 🔗 **5. SkillTabController Inspector 연결**

### **SkillSubPanel의 SkillTabController 설정**

```
📊 SP 표시:
- SP Text: SkillBookPanel/TopPanel/SPText
- Player Level Text: SkillBookPanel/TopPanel/PlayerLevelText

🎯 좌측 장착 슬롯:
- Active Equip Slots: 크기 2
  [0]: LeftPanel/ActiveSlot_0
  [1]: LeftPanel/ActiveSlot_1
  
- Passive Equip Slots: 크기 3
  [0]: LeftPanel/PassiveSlot_0
  [1]: LeftPanel/PassiveSlot_1
  [2]: LeftPanel/PassiveSlot_2

📋 우측 스킬 리스트:
- Active Skill List Parent: RightPanel/ActiveSkillSection/Scroll View/Viewport/Content
- Passive Skill List Parent: RightPanel/PassiveSkillSection/Scroll View/Viewport/Content
- Skill List Item Prefab: SkillListItemPrefab 드래그

📖 하단 상세 패널:
- Skill Detail Panel: BottomPanel/SkillDetailPanelObject (SkillDetailPanel 컴포넌트)

🔧 디버그:
- Show Debug Logs: ✅ (테스트 중 체크)
```

---

## 💡 **6. 레이아웃 팁**

### **Layout Group 우선순위**

```
부모: Vertical/Horizontal Layout Group
자식: Layout Element (크기 제어)
손자: 자유롭게 배치
```

### **Content Size Fitter 활용**

```
ScrollView의 Content에 Content Size Fitter 필수!
- Vertical Fit: Preferred Size
→ 자동으로 스크롤 영역 높이 조절
```

### **Anchor Preset 추천**

```
LeftPanel: Left-Stretch (좌측 고정)
RightPanel: Center-Stretch (중앙, 세로 확장)
BottomPanel: Bottom-Stretch (하단 고정)
```

---

## 🎨 **7. 시각적 배치 확인**

### **완성된 모습 (Game View)**

```
┌─────────────────────────────────────────────────┐
│  SP: 8/10   Lv.5  [스킬][룬]           [X]    │
├───────────┬─────────────────────────────────────┤
│           │  ⚔️ 액티브 스킬                    │
│ 장착 스킬 │  ┌────────────────────────────────┐│
│           │  │[아이콘] 갈래화살  Lv.5/10      ││
│━━액티브━━│  │         Combat   [레벨업][장착] ││
│ ┌───────┐│  └────────────────────────────────┘│
│ │액티브1││  ┌────────────────────────────────┐│
│ └───────┘│  │[아이콘] 화살의비  Lv.10/10     ││
│ ┌───────┐│  │         BossBurst [만렙][장착] ││
│ │액티브2││  └────────────────────────────────┘│
│ └───────┘│                                      │
│          │  🛡️ 패시브 스킬                    │
│━━패시브━━│  ┌────────────────────────────────┐│
│ ┌───────┐│  │[아이콘] 정령의공명 Lv.2/5      ││
│ │패시브1││  │         Combat   [레벨업][장착] ││
│ └───────┘│  └────────────────────────────────┘│
│ ┌───────┐│                                      │
│ │패시브2││  (스크롤...)                        │
│ └───────┘│                                      │
│ ┌───────┐│                                      │
│ │패시브3││                                      │
│ └───────┘│                                      │
├───────────┴─────────────────────────────────────┤
│ 하단: 상세 정보 패널 (BottomPanel)            │
└─────────────────────────────────────────────────┘
```

---

## 🧪 **8. 테스트 체크리스트**

### **LeftPanel 테스트**
- ✅ 슬롯이 세로로 5개 정렬되어 있는가?
- ✅ "액티브" / "패시브" 구분선이 보이는가?
- ✅ 빈 슬롯에 "빈 슬롯" 텍스트가 보이는가?

### **RightPanel 테스트**
- ✅ 액티브/패시브 섹션이 각각 독립적으로 스크롤되는가?
- ✅ 스킬 아이템이 세로로 나열되는가?
- ✅ [레벨업], [장착] 버튼이 각 아이템에 보이는가?

### **프리팹 테스트**
- ✅ SkillListItemPrefab 클릭 시 하단에 상세 정보 표시되는가?
- ✅ [레벨업] 버튼 클릭 시 SP가 차감되는가?
- ✅ [장착] 버튼 클릭 시 좌측 슬롯에 아이콘이 표시되는가?

---

## 📋 **요약**

1. **LeftPanel**: 장착 슬롯 5개, Vertical Layout Group
2. **RightPanel**: 2개 섹션(액티브/패시브), 각각 Scroll View
3. **Content**: Content Size Fitter (Vertical Fit: Preferred Size)
4. **프리팹 2개**: SkillEquipSlotPrefab, SkillListItemPrefab
5. **SkillTabController**: 모든 UI 요소 연결

이제 Unity Editor에서 정확히 구현하실 수 있습니다! 🚀
