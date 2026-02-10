# 📘 Phase 0: ResultFeedbackPopup 에디터 설정 가이드

## 🎯 목표
**분해/합성 결과 표시 팝업** 프리팹 생성 및 컴포넌트 연결

---

## 📋 작업 순서

### **Step 1: Canvas 하위에 ResultFeedbackPopup 생성**

#### **1-1. 기존 Canvas 확인**

```
Hierarchy 경로:
Lobby/UI/Canvas (또는 Lobby Scene의 메인 Canvas)
```

⚠️ **주의**: Workshop과 **독립적인 전역 팝업**이므로 WorkshopCanvas가 아닌 **Lobby의 메인 Canvas** 하위에 생성

#### **1-2. ResultFeedbackPopup 루트 생성**

```
1. Canvas 우클릭 → Create Empty
2. 이름: "ResultFeedbackPopup"
3. RectTransform:
   - Anchors: Stretch (전체 화면)
   - Left: 0, Right: 0, Top: 0, Bottom: 0
4. Add Component → ResultFeedbackPopup.cs
```

---

### **Step 2: BackgroundDim 생성 (어두운 배경)**

```
1. ResultFeedbackPopup 우클릭 → UI → Image
2. 이름: "BackgroundDim"
3. RectTransform:
   - Anchors: Stretch (전체 화면)
   - Left: 0, Right: 0, Top: 0, Bottom: 0
4. Image 컴포넌트:
   - Color: 검은색 (R:0, G:0, B:0, A:180) // 반투명
   - Raycast Target: ✅ 체크 (배경 클릭 차단)
```

---

### **Step 3: PopupPanel 생성 (메인 팝업 창)**

```
1. ResultFeedbackPopup 우클릭 → UI → Panel
2. 이름: "PopupPanel"
3. RectTransform:
   - Anchors: Center (중앙 고정)
   - Width: 600
   - Height: 400
   - PosX: 0, PosY: 0
4. Image 컴포넌트:
   - Color: 흰색 (R:255, G:255, B:255, A:230) // 반투명
   - Raycast Target: ✅ 체크
```

---

### **Step 4: TitleText 생성 (팝업 제목)**

```
1. PopupPanel 우클릭 → UI → Text - TextMeshPro
2. 이름: "TitleText"
3. RectTransform:
   - Anchors: Top (상단 중앙)
   - PosX: 0, PosY: -30
   - Width: 500, Height: 50
4. TextMeshProUGUI 설정:
   - Text: "결과 표시"
   - Font Size: 32
   - Alignment: Center Middle
   - Color: 흰색
   - Font Style: Bold
```

---

### **Step 5: MessageText 생성 (메시지)**

```
1. PopupPanel 우클릭 → UI → Text - TextMeshPro
2. 이름: "MessageText"
3. RectTransform:
   - Anchors: Top (TitleText 아래)
   - PosX: 0, PosY: -90
   - Width: 500, Height: 40
4. TextMeshProUGUI 설정:
   - Text: "메시지 표시"
   - Font Size: 20
   - Alignment: Center Middle
   - Color: 회색 (R:200, G:200, B:200)
```

---

### **Step 6: RewardPanel 생성 (분해 결과용)**

#### **6-1. RewardPanel 루트**

```
1. PopupPanel 우클릭 → UI → Panel
2. 이름: "RewardPanel"
3. RectTransform:
   - Anchors: Center (중앙)
   - PosX: 0, PosY: -20
   - Width: 550
   - Height: 200
4. Image:
   - Color: 투명 (A:0) // 배경 없음
```

#### **6-2. RewardSlotsContainer 생성**

```
1. RewardPanel 우클릭 → UI → Horizontal Layout Group
2. 이름: "RewardSlotsContainer"
3. RectTransform:
   - Anchors: Stretch
   - Left: 20, Right: 20, Top: 20, Bottom: 20
4. Horizontal Layout Group 설정:
   - Child Alignment: Middle Center
   - Spacing: 20
   - Child Force Expand: Width ✅, Height ✅
   - Child Control Size: Width ✅, Height ✅
```

⚠️ **중요**: 이 Container에는 **동적으로 MaterialSlot이 생성**됩니다 (프리팹 할당 필요)

---

### **Step 7: ResultItemPanel 생성 (합성 결과용)**

#### **7-1. ResultItemPanel 루트**

```
1. PopupPanel 우클릭 → UI → Panel
2. 이름: "ResultItemPanel"
3. RectTransform:
   - Anchors: Center (중앙)
   - PosX: 0, PosY: -20
   - Width: 400
   - Height: 250
4. Image:
   - Color: 투명 (A:0) // 배경 없음
```

#### **7-2. ResultItemIcon 생성**

```
1. ResultItemPanel 우클릭 → UI → Image
2. 이름: "ResultItemIcon"
3. RectTransform:
   - Anchors: Top (상단 중앙)
   - PosX: 0, PosY: -50
   - Width: 128, Height: 128
4. Image 설정:
   - Preserve Aspect: ✅ 체크
   - Color: 흰색
```

#### **7-2-1. ItemIconGradeFrame 생성 (등급 프레임) ⭐ 중요!**

```
1. ResultItemIcon 우클릭 → UI → Image
2. 이름: "ItemIconGradeFrame"
3. RectTransform:
   - Anchors: Stretch (전체 영역)
   - Left: 0, Right: 0, Top: 0, Bottom: 0
4. Image 설정:
   - Source Image: None (기본)
   - Color: 흰색 (런타임에 등급별 색상으로 변경됨)
5. Add Component → ItemIconGradeFrame (UI.Components)
   - Background Image: 자동 연결됨
   - Current Grade: D (기본값)
```

⚠️ **주의**: ItemIconGradeFrame은 ResultItemIcon의 **자식 오브젝트**로 생성해야 합니다!

#### **7-3. ResultItemNameText 생성**

```
1. ResultItemPanel 우클릭 → UI → Text - TextMeshPro
2. 이름: "ResultItemNameText"
3. RectTransform:
   - Anchors: Top (ResultItemIcon 아래)
   - PosX: 0, PosY: -140
   - Width: 350, Height: 40
4. TextMeshProUGUI 설정:
   - Text: "아이템 이름 +0"
   - Font Size: 24
   - Alignment: Center Middle
   - Color: 흰색
   - Font Style: Bold
```

#### **7-4. ResultItemGradeText 생성**

```
1. ResultItemPanel 우클릭 → UI → Text - TextMeshPro
2. 이름: "ResultItemGradeText"
3. RectTransform:
   - Anchors: Top (ResultItemNameText 아래)
   - PosX: 0, PosY: -190
   - Width: 350, Height: 30
4. TextMeshProUGUI 설정:
   - Text: "B 등급"
   - Font Size: 18
   - Alignment: Center Middle
   - Color: 골드 (R:255, G:215, B:0)
```

---

### **Step 8: CloseButton 생성 (닫기 버튼)**

```
1. PopupPanel 우클릭 → UI → Button - TextMeshPro
2. 이름: "CloseButton"
3. RectTransform:
   - Anchors: Bottom (하단 중앙)
   - PosX: 0, PosY: 40
   - Width: 200, Height: 50
4. Button 설정:
   - Image Color: 초록색 (R:50, G:200, B:50)
   - Transition: Color Tint
   - Highlighted Color: Bright Green
   - Pressed Color: Dark Green
5. 하위 Text (ButtonText):
   - Text: "확인"
   - Font Size: 20
   - Alignment: Center Middle
   - Color: 흰색
```

---

### **Step 9: ResultFeedbackPopup 컴포넌트 필드 연결**

#### **9-1. ResultFeedbackPopup 선택**

```
Hierarchy: ResultFeedbackPopup (루트 오브젝트)
Inspector: ResultFeedbackPopup 컴포넌트
```

#### **9-2. 필드 연결**

##### **📦 팝업 구조**
```
- Popup Panel: PopupPanel (GameObject)
- Background Dim: BackgroundDim (GameObject)
```

##### **🎨 타이틀/메시지**
```
- Title Text: TitleText (TMP_Text)
- Message Text: MessageText (TMP_Text)
```

##### **🎁 보상 표시 (분해용)**
```
- Reward Panel: RewardPanel (GameObject)
- Reward Slots Container: RewardSlotsContainer (Transform)
- Material Slot Prefab: ⭐ InventorySlot 프리팹 할당 필요!
  → Assets/Prefabs/UI/InventorySlot.prefab
```

##### **⭐ 결과 아이템 표시 (합성용)**
```
- Result Item Panel: ResultItemPanel (GameObject)
- Result Item Icon: ResultItemIcon (Image)
- Result Item Grade Frame: ResultItemIcon > ItemIconGradeFrame (ItemIconGradeFrame) ⭐ 중요!
- Result Item Name Text: ResultItemNameText (TMP_Text)
- Result Item Grade Text: ResultItemGradeText (TMP_Text)
```

##### **🔘 버튼**
```
- Close Button: CloseButton (Button)
- Close Button Text: CloseButton > Text (TMP) (TMP_Text)
```

##### **⚙️ 설정**
```
- Auto Close Delay: 3 (3초 후 자동 닫기)
- Enable Debug Logs: ✅ 체크 (테스트 중)
```

---

### **Step 10: 초기 상태 설정**

```
1. ResultFeedbackPopup (루트) 활성화: ✅ 체크
2. PopupPanel 비활성화: ❌ 체크 해제 (초기 숨김)
3. BackgroundDim 비활성화: ❌ 체크 해제 (초기 숨김)
```

⚠️ **중요**: 
- ResultFeedbackPopup (루트)는 **활성화** 상태로 유지
- PopupPanel과 BackgroundDim만 **비활성화**
- 스크립트가 Show() 호출 시 자동으로 표시됨

---

## 🔑 중요 확인 사항

### **1. MaterialSlot 프리팹 할당**

```
Inspector > ResultFeedbackPopup 컴포넌트
→ Material Slot Prefab: Assets/Prefabs/UI/InventorySlot.prefab
```

⚠️ 이 프리팹이 없으면 재료 슬롯 동적 생성 불가!

### **2. 계층 구조 최종 확인**

```
ResultFeedbackPopup (ResultFeedbackPopup.cs)
├── BackgroundDim (Image)
└── PopupPanel (Image)
    ├── TitleText (TMP)
    ├── MessageText (TMP)
    ├── RewardPanel (분해용)
    │   └── RewardSlotsContainer
    ├── ResultItemPanel (합성용)
    │   ├── ResultItemIcon (Image)
    │   │   └── ItemIconGradeFrame (ItemIconGradeFrame) ⭐ 자식!
    │   ├── ResultItemNameText (TMP)
    │   └── ResultItemGradeText (TMP)
    └── CloseButton (Button)
        └── Text (TMP)
```

### **3. Z-Order 확인**

ResultFeedbackPopup은 Canvas 하위에서 **최하위에 배치**합니다.
→ Show() 호출 시 `SetAsLastSibling()`으로 자동으로 최상위로 이동

---

## ✅ 최종 체크리스트

- [ ] ResultFeedbackPopup 루트 생성 (Canvas 하위)
- [ ] BackgroundDim 생성 (검은색 반투명)
- [ ] PopupPanel 생성 (600×400, 중앙)
- [ ] TitleText / MessageText 생성
- [ ] RewardPanel + RewardSlotsContainer 생성
- [ ] ResultItemPanel + Icon/GradeFrame/Name/Grade 생성
- [ ] CloseButton 생성
- [ ] ResultFeedbackPopup 컴포넌트 필드 모두 연결
- [ ] MaterialSlot 프리팹 할당 확인 ⭐
- [ ] PopupPanel / BackgroundDim 초기 비활성화 ✅
- [ ] ResultFeedbackPopup 루트는 활성화 유지 ✅

---

## 🎨 선택 사항 (나중에 추가 가능)

- [ ] 팝업 테두리 (Outline 컴포넌트)
- [ ] 그림자 효과 (Shadow 컴포넌트)
- [ ] 배경 블러 효과 (Shader)
- [ ] 애니메이션 (DOTween - Phase 3)

---

**설정 완료 후 Phase 0 테스트 진행!**

