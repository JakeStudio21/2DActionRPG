# 📋 RightPanel (우측 스킬 리스트) 단계별 제작 가이드

## ActiveSkillSection과 PassiveSkillSection 만들기

---

## 🎯 **최종 결과물**

```
RightPanel (우측 400px)
├─ ActiveSkillSection (250px 높이)
│   ├─ TitleText: "⚔️ 액티브 스킬"
│   └─ Scroll View
│       └─ Content (스킬 아이템 동적 생성)
│
└─ PassiveSkillSection (250px 높이)
    ├─ TitleText: "🛡️ 패시브 스킬"
    └─ Scroll View
        └─ Content (스킬 아이템 동적 생성)
```

---

## 📦 **Step 1: RightPanel 생성**

### **1-1. GameObject 생성**

```
Hierarchy에서:
SkillSubPanel 우클릭 → Create Empty

이름 변경: RightPanel
```

### **1-2. RectTransform 설정**

```
Inspector → RectTransform:

┌─────────────────────────────────────┐
│ Anchor: Center-Stretch              │
│   (중앙의 세로 막대 아이콘)         │
│   [Shift 누른 상태로 클릭]         │
├─────────────────────────────────────┤
│ Pos X: 300                          │
│   (LeftPanel 옆에 위치)             │
│ Pos Y: 0                            │
│ Pos Z: 0                            │
├─────────────────────────────────────┤
│ Width: 400                          │
│ Top: 10                             │
│ Bottom: 10                          │
└─────────────────────────────────────┘
```

### **1-3. Vertical Layout Group 추가**

```
Inspector 하단:
Add Component → Vertical Layout Group

설정:
┌─────────────────────────────────────┐
│ Padding:                            │
│   Left: 10                          │
│   Right: 10                         │
│   Top: 10                           │
│   Bottom: 10                        │
├─────────────────────────────────────┤
│ Spacing: 20                         │
│   (섹션 간 간격)                    │
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

### **1-4. Image 배경 추가 (선택사항)**

```
Inspector 하단:
Add Component → Image

설정:
┌─────────────────────────────────────┐
│ Source Image: None                  │
│ Color: RGB(60, 60, 60, 100)        │
│   (반투명 배경)                     │
└─────────────────────────────────────┘
```

---

## 🎮 **Step 2: ActiveSkillSection 생성**

### **2-1. Empty GameObject 생성 ⭐**

```
RightPanel 우클릭 → Create Empty

이름 변경: ActiveSkillSection
```

**중요:** Empty GameObject = 컨테이너 역할!

### **2-2. RectTransform 설정**

```
Inspector → RectTransform:

┌─────────────────────────────────────┐
│ Anchor: Top-Stretch                 │
│   (상단 가로 막대 아이콘)           │
├─────────────────────────────────────┤
│ Left: 0                             │
│ Right: 0                            │
│ Top: 0                              │
│ Height: 250                         │
│   (액티브 섹션 높이)                │
└─────────────────────────────────────┘
```

### **2-3. Vertical Layout Group 추가**

```
Inspector 하단:
Add Component → Vertical Layout Group

설정:
┌─────────────────────────────────────┐
│ Padding: 모두 0                     │
├─────────────────────────────────────┤
│ Spacing: 5                          │
│   (Title과 Scroll View 간격)       │
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

---

## 📝 **Step 3: TitleText 추가**

### **3-1. TextMeshPro 생성**

```
ActiveSkillSection 우클릭
→ UI → Text - TextMeshPro

이름 변경: TitleText
```

### **3-2. RectTransform 설정**

```
Height: 30
(Width는 Vertical Layout Group이 자동 조절)
```

### **3-3. TextMeshProUGUI 설정**

```
Inspector → Text Mesh Pro - Text (UI):

┌─────────────────────────────────────┐
│ Text Input:                         │
│   "⚔️ 액티브 스킬"                │
│   (또는 "액티브 스킬")              │
├─────────────────────────────────────┤
│ Font Size: 16                       │
│ Font Style: Bold                    │
├─────────────────────────────────────┤
│ Vertex Color: RGB(100, 200, 255)   │
│   (연한 파란색)                     │
├─────────────────────────────────────┤
│ Alignment:                          │
│   [Left] [Middle]                   │
│   (좌측 중앙 정렬)                  │
├─────────────────────────────────────┤
│ Wrapping: Disabled                  │
│ Overflow: Overflow                  │
└─────────────────────────────────────┘
```

---

## 📜 **Step 4: Scroll View 추가**

### **4-1. Scroll View 생성**

```
ActiveSkillSection 우클릭
→ UI → Scroll View

이름: Scroll View (기본값 유지)
```

**자동으로 생성되는 구조:**
```
Scroll View
├─ Viewport (Mask)
│   └─ Content
└─ Scrollbar Vertical
```

### **4-2. Scroll View RectTransform 설정**

```
Inspector → RectTransform:

┌─────────────────────────────────────┐
│ Anchor: Stretch-Stretch             │
│   [Alt 누른 상태로 우측 하단 클릭] │
├─────────────────────────────────────┤
│ Left: 0                             │
│ Right: 0                            │
│ Top: 35                             │
│   (TitleText 아래)                  │
│ Bottom: 0                           │
└─────────────────────────────────────┘
```

### **4-3. ScrollRect 컴포넌트 설정**

```
Inspector → Scroll Rect:

┌─────────────────────────────────────┐
│ Content: Content (자동 연결됨)      │
│ Horizontal: ❌                      │
│ Vertical: ✅                        │
├─────────────────────────────────────┤
│ Movement Type: Elastic              │
│ Scroll Sensitivity: 20              │
├─────────────────────────────────────┤
│ Viewport: Viewport (자동 연결됨)    │
│ Horizontal Scrollbar: None          │
│ Vertical Scrollbar: Scrollbar Vert  │
│   (또는 None - 스크롤바 안 보임)   │
└─────────────────────────────────────┘
```

### **4-4. Scrollbar 삭제 (선택사항)**

```
스크롤바를 숨기려면:

1. Hierarchy에서:
   Scroll View → Scrollbar Vertical 삭제

2. Scroll Rect 컴포넌트:
   Vertical Scrollbar: None 설정
```

---

## 🎯 **Step 5: Content 설정 (중요!)**

### **5-1. Content GameObject 선택**

```
Hierarchy에서:
Scroll View → Viewport → Content 클릭
```

### **5-2. RectTransform 설정**

```
Inspector → RectTransform:

┌─────────────────────────────────────┐
│ Anchor: Top-Center ⭐                │
│   (상단 중앙의 점 아이콘)           │
├─────────────────────────────────────┤
│ Pivot: X=0.5, Y=1 ⭐                 │
│   (피벗을 상단 중앙으로)            │
├─────────────────────────────────────┤
│ Pos X: 0                            │
│ Pos Y: 0                            │
│ Pos Z: 0                            │
├─────────────────────────────────────┤
│ Width: 380                          │
│   (부모보다 약간 작게)              │
│ Height: (자동 조절됨)               │
└─────────────────────────────────────┘
```

### **5-3. Vertical Layout Group 추가 ⭐**

```
Inspector 하단:
Add Component → Vertical Layout Group

설정:
┌─────────────────────────────────────┐
│ Padding:                            │
│   Left: 5                           │
│   Right: 5                          │
│   Top: 5                            │
│   Bottom: 5                         │
├─────────────────────────────────────┤
│ Spacing: 5                          │
│   (스킬 아이템 간 간격)             │
├─────────────────────────────────────┤
│ Child Alignment: Upper Center       │
├─────────────────────────────────────┤
│ Control Child Size:                 │
│   ✅ Width  (자식 폭 제어)          │
│   ✅ Height (자식 높이 제어)        │
├─────────────────────────────────────┤
│ Use Child Scale: ❌                 │
├─────────────────────────────────────┤
│ Child Force Expand:                 │
│   ✅ Width  (폭 확장)               │
│   ❌ Height (높이는 고정)           │
└─────────────────────────────────────┘
```

### **5-4. Content Size Fitter 추가 ⭐⭐⭐**

```
Inspector 하단:
Add Component → Content Size Fitter

설정:
┌─────────────────────────────────────┐
│ Horizontal Fit: Unconstrained       │
├─────────────────────────────────────┤
│ Vertical Fit: Preferred Size ⭐⭐⭐ │
│   (자동 높이 조절!)                 │
└─────────────────────────────────────┘
```

**이게 가장 중요합니다!**
```
Vertical Fit: Preferred Size 없으면:
❌ 스크롤이 작동 안 함
❌ 아이템이 잘림

있으면:
✅ Content 높이 자동 조절
✅ 스크롤 완벽 작동
```

---

## 🔄 **Step 6: PassiveSkillSection 생성**

### **6-1. ActiveSkillSection 복제**

```
Hierarchy에서:
ActiveSkillSection 우클릭 → Duplicate

이름 변경: PassiveSkillSection
```

### **6-2. TitleText 수정**

```
PassiveSkillSection → TitleText 클릭

Inspector → TextMeshProUGUI:
┌─────────────────────────────────────┐
│ Text: "🛡️ 패시브 스킬"            │
│ Color: RGB(100, 255, 150)          │
│   (연한 초록색)                     │
└─────────────────────────────────────┘
```

### **6-3. 나머지는 동일**

---

## ✅ **Step 7: 최종 확인**

### **7-1. Hierarchy 구조 확인**

```
RightPanel
├─ ActiveSkillSection
│   ├─ TitleText ("⚔️ 액티브 스킬")
│   └─ Scroll View
│       ├─ Viewport
│       │   └─ Content
│       │       (Vertical Layout + Content Size Fitter)
│       └─ Scrollbar Vertical (선택사항)
│
└─ PassiveSkillSection
    ├─ TitleText ("🛡️ 패시브 스킬")
    └─ Scroll View
        ├─ Viewport
        │   └─ Content
        │       (Vertical Layout + Content Size Fitter)
        └─ Scrollbar Vertical (선택사항)
```

### **7-2. Component 체크리스트**

**RightPanel:**
```
✅ Vertical Layout Group (Spacing: 20)
✅ (선택) Image 배경
```

**ActiveSkillSection:**
```
✅ Empty GameObject
✅ Height: 250
✅ Vertical Layout Group (Spacing: 5)
```

**TitleText:**
```
✅ TextMeshProUGUI
✅ Height: 30
✅ Text: "⚔️ 액티브 스킬"
```

**Scroll View:**
```
✅ ScrollRect (Horizontal ❌, Vertical ✅)
```

**Content:**
```
✅ Anchor: Top-Center
✅ Pivot: Y=1
✅ Vertical Layout Group ⭐
✅ Content Size Fitter (Vertical: Preferred Size) ⭐⭐⭐
```

---

## 🧪 **Step 8: 테스트**

### **8-1. 임시 아이템으로 테스트**

```
1. SkillListItemPrefab 프리팹 제작 (별도 가이드 참고)

2. Content에 프리팹 드래그 (3개 정도)

3. Scene View 확인:
   ✅ 아이템들이 세로로 정렬되어 있는가?
   ✅ Spacing(5px) 간격이 보이는가?
   ✅ Content 높이가 자동으로 늘어났는가?

4. Game View 확인:
   ✅ 스크롤 휠로 스크롤이 되는가?
   ✅ ActiveSkillSection 내부만 스크롤되는가?

5. 테스트 완료 후 임시 아이템 삭제
```

### **8-2. Play 모드 테스트**

```
1. Play 모드 진입

2. 스킬북 패널 열기

3. Scene View에서 Content 선택:
   - Height 값이 자동으로 변하는지 확인

4. Game View에서:
   - 액티브 스킬 목록이 보이는지 확인
   - 패시브 스킬 목록이 보이는지 확인
   - 각 섹션이 독립적으로 스크롤되는지 확인
```

---

## 💡 **왜 이런 구조인가?**

### **Empty GameObject (ActiveSkillSection)를 사용하는 이유:**

```
1. 컨테이너 역할
   ✅ TitleText + Scroll View를 하나로 묶음
   ✅ 독립적인 섹션 관리

2. 높이 제어
   ✅ Height: 250px로 고정
   ✅ 액티브/패시브 섹션 크기 조절 가능

3. 레이아웃 자동화
   ✅ Vertical Layout Group으로 자식 정렬
   ✅ Spacing으로 간격 조절

4. 재사용성
   ✅ 복제해서 PassiveSkillSection 만들기 쉬움
   ✅ 일관된 UI 패턴
```

### **Scroll View를 각 섹션에 배치하는 이유:**

```
✅ 액티브/패시브 스킬을 독립적으로 스크롤
✅ 한 섹션을 스크롤해도 다른 섹션은 고정
✅ 사용자 경험 향상
```

---

## 🎨 **시각적 구조**

```
Game View:

┌─ RightPanel ─────────────────────────┐
│                                       │
│ ┌─ ActiveSkillSection (250px) ─────┐│
│ │ ⚔️ 액티브 스킬                   ││
│ │ ┌─ Scroll View ────────────────┐││
│ │ │ [갈래 화살]  Lv.5/10         │││
│ │ │ [화살의 비]  Lv.10/10        │││
│ │ │ ...                           │││
│ │ └───────────────────────────────┘││
│ └───────────────────────────────────┘│
│           ↕ Spacing: 20               │
│ ┌─ PassiveSkillSection (250px) ────┐│
│ │ 🛡️ 패시브 스킬                  ││
│ │ ┌─ Scroll View ────────────────┐││
│ │ │ [정령의 공명]  Lv.2/5        │││
│ │ │ [생명의 축복]  Lv.1/10       │││
│ │ │ ...                           │││
│ │ └───────────────────────────────┘││
│ └───────────────────────────────────┘│
│                                       │
└───────────────────────────────────────┘
```

---

## ⚠️ **자주 하는 실수**

### **실수 1: ActiveSkillSection을 UI 컴포넌트로 만듦**

```
❌ 잘못:
   RightPanel 우클릭 → UI → Panel

✅ 올바름:
   RightPanel 우클릭 → Create Empty
```

### **실수 2: Content Size Fitter 안 넣음**

```
증상:
- 스크롤이 작동 안 함
- 아이템이 잘림

해결:
→ Content에 Content Size Fitter 추가
→ Vertical Fit: Preferred Size
```

### **실수 3: Scroll View를 RightPanel에 직접 추가**

```
❌ 잘못:
RightPanel
└─ Scroll View (전체 스크롤)

✅ 올바름:
RightPanel
├─ ActiveSkillSection
│   └─ Scroll View (독립 스크롤)
└─ PassiveSkillSection
    └─ Scroll View (독립 스크롤)
```

### **실수 4: Anchor/Pivot 잘못 설정**

```
증상:
- 스크롤 위치가 이상함
- 아이템이 중앙에서 시작

해결:
→ Content Anchor: Top-Center
→ Content Pivot: X=0.5, Y=1
```

---

## 📊 **GameObject 타입 정리**

```
RightPanel ← Empty GameObject
├─ ActiveSkillSection ← Empty GameObject ⭐
│   ├─ TitleText ← TextMeshPro (UI 컴포넌트)
│   └─ Scroll View ← Unity UI (복합 컴포넌트)
│       ├─ Viewport ← Image + Mask (자동 생성)
│       │   └─ Content ← Empty GameObject (자동 생성)
│       └─ Scrollbar Vertical ← Unity UI (자동 생성)
│
└─ PassiveSkillSection ← Empty GameObject ⭐
    ├─ TitleText ← TextMeshPro (UI 컴포넌트)
    └─ Scroll View ← Unity UI (복합 컴포넌트)
```

---

## 🎯 **핵심 정리**

### **만드는 순서:**

```
1. RightPanel (Empty GameObject)
   → Vertical Layout Group 추가

2. ActiveSkillSection (Empty GameObject) ⭐
   → Height: 250
   → Vertical Layout Group 추가

3. TitleText (TextMeshPro)
   → "⚔️ 액티브 스킬"

4. Scroll View (Unity UI)
   → Horizontal ❌, Vertical ✅

5. Content (자동 생성됨)
   → Vertical Layout Group ⭐
   → Content Size Fitter ⭐⭐⭐
   → Anchor: Top-Center, Pivot: Y=1

6. PassiveSkillSection 복제
   → TitleText 수정
```

### **중요 포인트:**

```
✅ ActiveSkillSection = Empty GameObject (컨테이너!)
✅ Content = Vertical Layout + Content Size Fitter
✅ Content Size Fitter의 Vertical Fit: Preferred Size 필수!
✅ 각 섹션이 독립적으로 스크롤됨
```

---

## 🚀 **완성!**

이제 RightPanel이 완성되었습니다!

**다음 단계:**
- BottomPanel (상세 정보) 만들기
- SkillTabController 연결
- SkillListItemPrefab 제작

---

**이 가이드를 따라하면 RightPanel이 완벽하게 만들어집니다!** 🎉
