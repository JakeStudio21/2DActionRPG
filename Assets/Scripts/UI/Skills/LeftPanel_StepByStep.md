# 📦 LeftPanel 단계별 제작 가이드

## SkillSubPanel의 좌측 장착 슬롯 만들기

---

## 🎯 **최종 결과물**

```
LeftPanel (좌측 200px)
├─ TitleText: "장착 스킬"
├─ ActiveSlotsTitle: "━━ 액티브 ━━"
├─ ActiveSlot_0: [스킬 아이콘]
├─ ActiveSlot_1: [스킬 아이콘]
├─ PassiveSlotsTitle: "━━ 패시브 ━━"
├─ PassiveSlot_0: [스킬 아이콘]
├─ PassiveSlot_1: [스킬 아이콘]
└─ PassiveSlot_2: [스킬 아이콘]
```

---

## 📋 **Step 1: LeftPanel 생성**

### **1-1. GameObject 생성**

```
Hierarchy에서:
SkillSubPanel 우클릭 → Create Empty

이름 변경: LeftPanel
```

### **1-2. RectTransform 설정**

```
Inspector → RectTransform 클릭:

┌─────────────────────────────────────┐
│ 1. Anchor Preset 클릭               │
│    (왼쪽 상단의 십자 아이콘)        │
│                                     │
│ 2. Left-Stretch 선택                │
│    (왼쪽 중앙의 세로 막대 모양)    │
│    [Shift + Alt 누른 상태로 클릭]  │
└─────────────────────────────────────┘

그러면 자동으로:
- Anchor Min: X=0, Y=0
- Anchor Max: X=0, Y=1
- Pivot: X=0, Y=0.5

수동 입력 값:
┌─────────────────────────────────────┐
│ Pos X: 100                          │
│ Pos Y: 0                            │
│ Pos Z: 0                            │
├─────────────────────────────────────┤
│ Width: 200                          │
│ Left: 10      (왼쪽 여백)          │
│ Right: 210                          │
│ Top: 10       (위쪽 여백)          │
│ Bottom: 10    (아래쪽 여백)        │
└─────────────────────────────────────┘
```

### **1-3. Vertical Layout Group 추가**

```
Inspector 하단:
Add Component 버튼 클릭
→ "Vertical" 검색
→ Vertical Layout Group 선택

설정:
┌─────────────────────────────────────┐
│ Padding:                            │
│   Left: 10                          │
│   Right: 10                         │
│   Top: 10                           │
│   Bottom: 10                        │
├─────────────────────────────────────┤
│ Spacing: 10                         │
├─────────────────────────────────────┤
│ Child Alignment: Upper Center       │
├─────────────────────────────────────┤
│ Control Child Size:                 │
│   ✅ Width                          │
│   ❌ Height                         │
├─────────────────────────────────────┤
│ Child Force Expand:                 │
│   ✅ Width                          │
│   ❌ Height                         │
└─────────────────────────────────────┘
```

### **1-4. Image 배경 추가**

```
Inspector 하단:
Add Component 버튼 클릭
→ "Image" 검색
→ Image (UnityEngine.UI) 선택

설정:
┌─────────────────────────────────────┐
│ Source Image: None (단색 배경)      │
├─────────────────────────────────────┤
│ Color: 클릭하여 색상 선택           │
│   R: 50                             │
│   G: 50                             │
│   B: 50                             │
│   A: 200 (슬라이더로 조절)         │
│   결과: 반투명 어두운 회색          │
├─────────────────────────────────────┤
│ Material: None                      │
│ Raycast Target: ✅                  │
└─────────────────────────────────────┘
```

---

## 📝 **Step 2: TitleText 추가**

### **2-1. TextMeshPro 생성**

```
Hierarchy에서:
LeftPanel 우클릭
→ UI → Text - TextMeshPro

이름 변경: TitleText
```

**처음 TMP를 사용하면 "Import TMP Essentials" 창이 뜹니다:**
```
→ "Import TMP Essentials" 버튼 클릭
→ "Import TMP Examples & Extras" 버튼도 클릭 (선택사항)
```

### **2-2. RectTransform 설정**

```
Inspector → RectTransform:

Height: 30
(Width는 Vertical Layout Group이 자동 조절)
```

### **2-3. TextMeshProUGUI 설정**

```
Inspector → Text Mesh Pro - Text (UI):

┌─────────────────────────────────────┐
│ Text Input:                         │
│   "장착 스킬"                      │
├─────────────────────────────────────┤
│ Font Asset:                         │
│   기본 폰트 또는 한글 폰트          │
├─────────────────────────────────────┤
│ Font Style: Bold                    │
│ Font Size: 18                       │
│ Auto Size: ❌                       │
├─────────────────────────────────────┤
│ Vertex Color: White                 │
│   (색상 박스 클릭 → 흰색 선택)     │
├─────────────────────────────────────┤
│ Alignment:                          │
│   [Center] [Middle] 아이콘 클릭    │
│   (정가운데 정렬)                   │
├─────────────────────────────────────┤
│ Wrapping: Disabled                  │
│ Overflow: Overflow                  │
└─────────────────────────────────────┘
```

---

## 🔷 **Step 3: ActiveSlotsTitle 추가**

### **3-1. TextMeshPro 생성**

```
Hierarchy에서:
LeftPanel 우클릭
→ UI → Text - TextMeshPro

이름 변경: ActiveSlotsTitle
```

### **3-2. RectTransform 설정**

```
Height: 25
```

### **3-3. TextMeshProUGUI 설정**

```
┌─────────────────────────────────────┐
│ Text Input:                         │
│   "━━ 액티브 ━━"                  │
│   (또는 "--- 액티브 ---")          │
├─────────────────────────────────────┤
│ Font Size: 14                       │
├─────────────────────────────────────┤
│ Vertex Color: 연한 파란색           │
│   R: 100                            │
│   G: 200                            │
│   B: 255                            │
│   A: 255                            │
├─────────────────────────────────────┤
│ Alignment: [Center] [Middle]        │
└─────────────────────────────────────┘
```

---

## 🎮 **Step 4: SkillEquipSlotPrefab 제작**

### **4-1. 프리팹 기본 구조 만들기**

```
Hierarchy 빈 공간 우클릭
→ Create Empty

이름 변경: SkillEquipSlotPrefab
```

### **4-2. RectTransform 설정**

```
Width: 100
Height: 120
```

### **4-3. SkillEquipSlotUI 컴포넌트 추가**

```
Inspector 하단:
Add Component
→ "SkillEquipSlotUI" 검색
→ Scripts > SkillEquipSlotUI 선택
```

### **4-4. BackgroundImage (배경) 추가**

```
Inspector 하단:
Add Component
→ "Image" 검색
→ Image 선택

설정:
┌─────────────────────────────────────┐
│ Source Image: None                  │
│ Color: RGB(70, 70, 70, 200)        │
│   R: 70                             │
│   G: 70                             │
│   B: 70                             │
│   A: 200                            │
├─────────────────────────────────────┤
│ Raycast Target: ✅                  │
└─────────────────────────────────────┘
```

---

### **4-5. SkillIcon (스킬 아이콘) 추가**

```
SkillEquipSlotPrefab 우클릭
→ UI → Image

이름 변경: SkillIcon
```

**RectTransform:**
```
┌─────────────────────────────────────┐
│ Anchor: Center-Center               │
│   (중앙의 점 아이콘)                │
├─────────────────────────────────────┤
│ Pos X: 0                            │
│ Pos Y: 5                            │
│ Pos Z: 0                            │
├─────────────────────────────────────┤
│ Width: 80                           │
│ Height: 80                          │
└─────────────────────────────────────┘
```

**Image 설정:**
```
┌─────────────────────────────────────┐
│ Source Image: None                  │
│   (스킬 장착 시 코드로 변경됨)     │
├─────────────────────────────────────┤
│ Color: White                        │
├─────────────────────────────────────┤
│ Preserve Aspect: ✅                 │
│   (아이콘 비율 유지)                │
└─────────────────────────────────────┘
```

---

### **4-6. SlotNumberText (슬롯 번호) 추가**

```
SkillEquipSlotPrefab 우클릭
→ UI → Text - TextMeshPro

이름 변경: SlotNumberText
```

**RectTransform:**
```
┌─────────────────────────────────────┐
│ Anchor: Top-Center                  │
│   (상단 중앙의 점 아이콘)           │
├─────────────────────────────────────┤
│ Pos X: 0                            │
│ Pos Y: -10                          │
│ Pos Z: 0                            │
├─────────────────────────────────────┤
│ Width: 100                          │
│ Height: 20                          │
└─────────────────────────────────────┘
```

**TextMeshProUGUI:**
```
┌─────────────────────────────────────┐
│ Text: "액티브 1"                   │
│   (나중에 각 슬롯별로 변경)         │
├─────────────────────────────────────┤
│ Font Size: 12                       │
│ Color: RGB(200, 200, 200)          │
├─────────────────────────────────────┤
│ Alignment: [Center] [Middle]        │
└─────────────────────────────────────┘
```

---

### **4-7. SkillLevelText (레벨 표시) 추가**

```
SkillEquipSlotPrefab 우클릭
→ UI → Text - TextMeshPro

이름 변경: SkillLevelText
```

**RectTransform:**
```
┌─────────────────────────────────────┐
│ Anchor: Bottom-Right                │
│   (우측 하단의 점 아이콘)           │
├─────────────────────────────────────┤
│ Pos X: -5                           │
│ Pos Y: 5                            │
│ Pos Z: 0                            │
├─────────────────────────────────────┤
│ Width: 40                           │
│ Height: 20                          │
└─────────────────────────────────────┘
```

**TextMeshProUGUI:**
```
┌─────────────────────────────────────┐
│ Text: "Lv.2"                        │
│   (장착된 스킬 레벨)                │
├─────────────────────────────────────┤
│ Font Size: 14                       │
│ Font Style: Bold                    │
│ Color: RGB(255, 220, 100) - 황금색 │
├─────────────────────────────────────┤
│ Alignment: [Right] [Middle]         │
└─────────────────────────────────────┘
```

---

### **4-8. EmptyOverlay (빈 슬롯 표시) 추가**

```
SkillEquipSlotPrefab 우클릭
→ UI → Image

이름 변경: EmptyOverlay
```

**RectTransform:**
```
┌─────────────────────────────────────┐
│ Anchor: Stretch-Stretch             │
│   (오른쪽 하단의 확장 아이콘)       │
│   [Alt 누른 상태로 클릭]           │
├─────────────────────────────────────┤
│ Left: 0                             │
│ Right: 0                            │
│ Top: 0                              │
│ Bottom: 0                           │
│   (부모 크기에 꽉 참)               │
└─────────────────────────────────────┘
```

**Image 설정:**
```
┌─────────────────────────────────────┐
│ Source Image: None                  │
│ Color: RGB(30, 30, 30, 150)        │
│   (반투명 어두운색)                 │
└─────────────────────────────────────┘
```

---

### **4-9. EmptyText (EmptyOverlay 자식) 추가**

```
EmptyOverlay 우클릭
→ UI → Text - TextMeshPro

이름 변경: EmptyText
```

**RectTransform:**
```
┌─────────────────────────────────────┐
│ Anchor: Center-Center               │
├─────────────────────────────────────┤
│ Pos X: 0                            │
│ Pos Y: 0                            │
├─────────────────────────────────────┤
│ Width: 80                           │
│ Height: 30                          │
└─────────────────────────────────────┘
```

**TextMeshProUGUI:**
```
┌─────────────────────────────────────┐
│ Text: "빈 슬롯"                    │
├─────────────────────────────────────┤
│ Font Size: 12                       │
│ Color: RGB(150, 150, 150) - 회색   │
├─────────────────────────────────────┤
│ Alignment: [Center] [Middle]        │
└─────────────────────────────────────┘
```

---

## 🔗 **Step 5: SkillEquipSlotUI Inspector 연결**

### **5-1. SkillEquipSlotPrefab 선택**

```
Hierarchy에서 SkillEquipSlotPrefab 클릭
```

### **5-2. Inspector에서 연결**

```
SkillEquipSlotUI 컴포넌트 찾기
→ 각 필드에 GameObject 드래그

┌─────────────────────────────────────┐
│ 🎨 UI 요소:                         │
├─────────────────────────────────────┤
│ Skill Icon:                         │
│   → SkillIcon 드래그                │
├─────────────────────────────────────┤
│ Background Image:                   │
│   → SkillEquipSlotPrefab의         │
│      Image 컴포넌트 드래그          │
├─────────────────────────────────────┤
│ Slot Number Text:                   │
│   → SlotNumberText 드래그           │
├─────────────────────────────────────┤
│ Skill Level Text:                   │
│   → SkillLevelText 드래그           │
├─────────────────────────────────────┤
│ Empty Overlay:                      │
│   → EmptyOverlay 드래그             │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 🎨 상태별 색상: (기본값)            │
├─────────────────────────────────────┤
│ Locked Color: RGB(77, 77, 77)      │
│ Unlocked Color: RGB(255, 255, 255) │
│ Equipped Color: RGB(255, 204, 77)  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 🔗 데이터: (비워둠)                 │
│   - 코드에서 자동 설정됨            │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ 🔧 디버그:                          │
│   Show Debug Logs: ❌               │
│   (테스트 시 체크)                  │
└─────────────────────────────────────┘
```

---

## 💾 **Step 6: 프리팹으로 저장**

### **6-1. 폴더 생성**

```
Project 창:
Assets 우클릭 → Create → Folder
이름: Prefabs

Prefabs 우클릭 → Create → Folder
이름: UI

UI 우클릭 → Create → Folder
이름: Skills
```

### **6-2. 프리팹 저장**

```
Hierarchy에서:
SkillEquipSlotPrefab을 드래그
→ Project 창의 Assets/Prefabs/UI/Skills/ 폴더로 드롭

파일 이름: SkillEquipSlotPrefab.prefab
```

### **6-3. Hierarchy에서 원본 삭제**

```
Hierarchy에서:
SkillEquipSlotPrefab 우클릭
→ Delete

(프리팹은 Project 창에 저장되어 있음)
```

---

## 🎯 **Step 7: LeftPanel에 슬롯 배치**

### **7-1. ActiveSlot_0 배치**

```
Project 창에서:
Assets/Prefabs/UI/Skills/SkillEquipSlotPrefab

드래그 → Hierarchy의 LeftPanel으로 드롭
```

**이름 변경:**
```
Hierarchy에서:
SkillEquipSlotPrefab(Clone)
→ 이름 변경: ActiveSlot_0
```

**SlotNumberText 수정:**
```
Hierarchy에서:
ActiveSlot_0 → SlotNumberText 클릭

Inspector → TextMeshProUGUI:
Text: "액티브 1"
```

**SkillEquipSlotUI 설정:**
```
Inspector → SkillEquipSlotUI:

┌─────────────────────────────────────┐
│ Slot Index: 0                       │
│ Is Active Slot: ✅                  │
│ Show Debug Logs: ❌                 │
└─────────────────────────────────────┘
```

---

### **7-2. ActiveSlot_1 배치**

```
같은 방법으로:
1. 프리팹 드래그 → LeftPanel
2. 이름: ActiveSlot_1
3. SlotNumberText: "액티브 2"
4. Slot Index: 1
5. Is Active Slot: ✅
```

---

### **7-3. PassiveSlotsTitle 추가**

```
LeftPanel 우클릭
→ UI → Text - TextMeshPro

이름: PassiveSlotsTitle
```

**설정:**
```
Height: 25

Text: "━━ 패시브 ━━"
Font Size: 14
Color: RGB(100, 255, 150) - 연한 초록색
Alignment: [Center] [Middle]
```

---

### **7-4. PassiveSlot_0, 1, 2 배치**

**PassiveSlot_0:**
```
1. 프리팹 드래그 → LeftPanel
2. 이름: PassiveSlot_0
3. SlotNumberText: "패시브 1"
4. Slot Index: 0
5. Is Active Slot: ❌ (체크 해제!)
```

**PassiveSlot_1:**
```
1. 프리팹 드래그 → LeftPanel
2. 이름: PassiveSlot_1
3. SlotNumberText: "패시브 2"
4. Slot Index: 1
5. Is Active Slot: ❌
```

**PassiveSlot_2:**
```
1. 프리팹 드래그 → LeftPanel
2. 이름: PassiveSlot_2
3. SlotNumberText: "패시브 3"
4. Slot Index: 2
5. Is Active Slot: ❌
```

---

## ✅ **Step 8: 최종 확인**

### **8-1. Hierarchy 구조 확인**

```
LeftPanel
├─ TitleText ("장착 스킬")
├─ ActiveSlotsTitle ("━━ 액티브 ━━")
├─ ActiveSlot_0 (Slot Index: 0, Is Active: ✅)
├─ ActiveSlot_1 (Slot Index: 1, Is Active: ✅)
├─ PassiveSlotsTitle ("━━ 패시브 ━━")
├─ PassiveSlot_0 (Slot Index: 0, Is Active: ❌)
├─ PassiveSlot_1 (Slot Index: 1, Is Active: ❌)
└─ PassiveSlot_2 (Slot Index: 2, Is Active: ❌)
```

### **8-2. Vertical Layout Group 확인**

```
LeftPanel 선택
→ Inspector → Vertical Layout Group

모든 자식이 세로로 자동 정렬되어야 함
```

### **8-3. Scene View에서 확인**

```
Scene 탭에서:
- 좌측에 세로로 슬롯 5개가 보임
- "액티브" / "패시브" 구분선이 보임
- 각 슬롯에 "빈 슬롯" 텍스트가 보임
```

---

## 🎉 **완성!**

이제 LeftPanel이 완성되었습니다!

**다음 단계:**
- RightPanel (스킬 리스트) 만들기
- BottomPanel (상세 정보) 만들기
- SkillTabController 연결

---

## 💡 **자주 하는 실수**

### **1. Layout Group이 작동 안 함**

```
해결:
- 부모에 Vertical Layout Group이 있는지 확인
- Control Child Size: Width ✅
- Child Force Expand: Width ✅
```

### **2. 슬롯이 보이지 않음**

```
해결:
- LeftPanel의 Image 컴포넌트 확인
- Color의 Alpha 값이 0이 아닌지 확인
- Width가 0이 아닌지 확인
```

### **3. Anchor가 이상하게 설정됨**

```
해결:
- Anchor Preset 다시 선택
- Shift + Alt 누르고 클릭
- Position 값 수동 입력
```

### **4. 텍스트가 깨짐 (한글)**

```
해결:
- TextMeshPro 한글 폰트 사용
- 또는 Unity 기본 UI Text 사용
```

---

## 📚 **참고**

- **Vertical Layout Group**: 자식을 세로로 자동 정렬
- **Content Size Fitter**: 자식 크기에 맞춰 부모 크기 조절
- **Layout Element**: 특정 자식의 크기 제어

---

**이 가이드를 따라하면 LeftPanel이 완벽하게 만들어집니다!** 🚀
