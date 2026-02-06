# 📜 ScrollView 설정 가이드 (LobbyInventoryUI & ShopInventoryUI)

## 🎯 **목표**

로비 보관창고와 상점 인벤토리에서 **스크롤 기능이 정상 작동**하도록 설정합니다.

---

## 🛠️ **1단계: ScrollView 기본 구조 확인**

### **Hierarchy 구조**

```
LobbyInventoryPanel (또는 ShopPanel > InventoryPanel)
  └─ ScrollView (Scroll Rect 컴포넌트)
      ├─ Viewport (Mask 컴포넌트)
      │   └─ Content (Grid Layout Group + Content Size Fitter)
      │       └─ Slot_0, Slot_1, ... (동적 생성 슬롯들)
      └─ Scrollbar Vertical (스크롤바)
```

### **필수 컴포넌트**

| GameObject | 필수 컴포넌트 | 역할 |
|------------|--------------|------|
| **ScrollView** | Scroll Rect | 스크롤 기능 제어 |
| **Viewport** | Rect Transform, Mask | 보이는 영역 제한 (Clipping) |
| **Content** | Rect Transform, Grid Layout Group, **Content Size Fitter** | 슬롯 배치 + 자동 높이 계산 |
| **Scrollbar Vertical** | Scrollbar | 세로 스크롤바 UI |

---

## 🔧 **2단계: Scroll Rect 설정**

### **ScrollView GameObject 선택 → Inspector**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Content** | `Content (Rect Transform)` | 드래그: Viewport 하위의 Content |
| **Horizontal** | ❌ **체크 해제** | 가로 스크롤 비활성화 |
| **Vertical** | ✅ **체크** | 세로 스크롤 활성화 |
| **Movement Type** | `Elastic` 또는 `Clamped` | Elastic 권장 (튕기는 효과) |
| **Elasticity** | `0.1` | 튕김 강도 (Elastic 모드일 때) |
| **Inertia** | ✅ **체크** | 관성 스크롤 활성화 |
| **Deceleration Rate** | `0.135` | 관성 감속 속도 (기본값) |
| **Scroll Sensitivity** | `1` | 스크롤 휠 민감도 |
| **Viewport** | `Viewport (Rect Transform)` | 드래그: ScrollView 하위의 Viewport |
| **Horizontal Scrollbar** | `None (Scrollbar)` | 가로 스크롤바 없음 |
| **Vertical Scrollbar** | `Scrollbar Vertical (Scrollbar)` | 드래그: ScrollView 하위의 Scrollbar Vertical |
| **Visibility** | `Auto Hide And Expand Viewport` | 필요시 자동 표시/숨김 |
| **Spacing** | `-3` | 스크롤바와 Viewport 간격 |

**⚠️ 중요:** `Horizontal`을 **반드시 체크 해제**하세요! (가로 스크롤 방지)

---

## 📐 **3단계: Viewport 설정 (핵심!)**

### **Viewport GameObject 선택 → Inspector**

#### **Rect Transform 설정**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Anchor Presets** | `Stretch-Stretch` (전체 늘어남) | Alt + Shift + 클릭으로 Position도 함께 설정 |
| **Left, Top, Right, Bottom** | `0, 0, 0, 0` | ScrollView 영역 전체 채우기 |
| **Width, Height** | 자동 계산됨 | (Stretch 모드에서는 편집 불가) |

**🔧 Viewport 높이 조정 (스크롤 활성화):**

만약 64개 슬롯이 모두 보여서 스크롤이 필요 없다면:

1. **Anchor Presets를 변경:** `Top-Stretch` (상단 고정, 가로만 늘어남)
2. **Height 값 설정:** `400` ~ `600` (4~7줄 슬롯만 보이도록)
   - 예: 슬롯 크기 80x80, 간격 5 → 4줄 = 80×4 + 5×3 = 335px
   - 여유를 두고 **Height = 400** 정도 권장

#### **Mask 컴포넌트**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Show Mask Graphic** | ❌ 체크 해제 | 배경 이미지 숨김 (Clipping만 사용) |

---

## 📦 **4단계: Content 설정 (자동 높이 계산)**

### **Content GameObject 선택 → Inspector**

#### **Rect Transform 설정**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Anchor Presets** | `Top-Stretch` (상단 고정, 가로 늘어남) | Alt + Shift + 클릭 |
| **Pivot** | `(0.5, 1)` | X=0.5 (중앙), Y=1 (상단) |
| **Left, Top, Right** | `0, 0, 0` | Viewport 가로 전체 채우기 |
| **Height** | **자동 계산됨** | Content Size Fitter가 계산 |

**⚠️ 중요:** Height는 직접 설정하지 마세요! Content Size Fitter가 자동으로 계산합니다.

#### **Grid Layout Group 설정**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Padding** | `Left: 10, Right: 10, Top: 10, Bottom: 10` | 여백 |
| **Cell Size** | `80 x 80` | 슬롯 크기 (아이콘 + 여백) |
| **Spacing** | `5 x 5` | 슬롯 간격 |
| **Start Corner** | `Upper Left` | 왼쪽 위부터 시작 |
| **Start Axis** | `Horizontal` | 가로로 먼저 채움 |
| **Child Alignment** | `Upper Left` | 왼쪽 위 정렬 |
| **Constraint** | `Fixed Column Count` | 열 개수 고정 |
| **Constraint Count** | `8` | 8열 (8개씩 가로 배치) |

**🔧 Cell Size 계산:**

- **Available Width** = Viewport Width - Left Padding - Right Padding - Right Margin (스크롤바)
- **Cell Width** = (Available Width - Spacing × 7) ÷ 8
- 예: (800 - 20 - 20) ÷ 8 ≈ 90~95px

#### **Content Size Fitter 설정 (필수!)**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Horizontal Fit** | `Unconstrained` | 가로는 Stretch로 고정 |
| **Vertical Fit** | `Preferred Size` | **세로는 자동 계산** ⭐ |

**⚠️ 핵심:** `Vertical Fit = Preferred Size`가 반드시 설정되어야 Grid Layout Group이 계산한 높이가 자동으로 적용됩니다!

**✅ 결과:** 64개 슬롯 (8×8) → Height ≈ 80×8 + 5×7 + 20 = 695px (자동 계산)

---

## 📊 **5단계: Scrollbar Vertical 설정**

### **Scrollbar Vertical GameObject 선택 → Inspector**

#### **Rect Transform 설정**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Anchor Presets** | `Right-Stretch` (우측 고정, 세로 늘어남) | Alt + Shift + 클릭 |
| **Width** | `20` | 스크롤바 너비 |
| **Left, Top, Right, Bottom** | `자동, 0, 0, 0` | 우측에 붙임 |

#### **Scrollbar 컴포넌트**

| 필드 | 설정 값 | 설명 |
|------|--------|------|
| **Direction** | `Bottom To Top` | 아래에서 위로 |
| **Handle Rect** | `Handle (Rect Transform)` | 드래그: Scrollbar > Sliding Area > Handle |
| **Value** | `1` | 초기 위치 (최상단) |
| **Size** | `0.1` ~ `1.0` (자동 계산) | 보이는 영역 비율 |
| **Number Of Steps** | `0` | 연속 스크롤 (단계 없음) |

---

## ✅ **6단계: LobbyInventoryUI 스크립트 연결**

### **LobbyInventoryPanel GameObject 선택 → Inspector**

#### **LobbyInventoryUI 컴포넌트**

| 필드 | 연결할 GameObject | 설명 |
|------|------------------|------|
| **Scroll Rect** | `LobbyInventoryPanel > ScrollView` | ⭐ Scroll Rect 컴포넌트 연결 |
| **Slot Container** | `ScrollView > Viewport > Content` | 슬롯들이 생성될 부모 |
| **Slot Prefab** | `Prefabs/UI/InventorySlot` | 슬롯 프리팹 |
| **Close Panel Button** | `LobbyInventoryPanel > CloseButton` | 닫기 버튼 |
| **Equipped Items UI** | `LobbyInventoryPanel > EquippedItemsUI` | 장비창 UI (선택) |

**⚠️ 중요:** `Scroll Rect` 필드에 **ScrollView GameObject**를 드래그하세요! (Viewport가 아님!)

---

## 🧪 **7단계: 테스트 및 검증**

### **Play 모드에서 확인**

1. ✅ **스크롤바가 활성화되어 있는가?**
   - Viewport Height < Content Height 여야 활성화됨
   - 비활성화(회색)면 → Viewport Height를 줄이세요

2. ✅ **마우스 휠로 스크롤되는가?**
   - Scroll Rect의 `Scroll Sensitivity = 1` 확인

3. ✅ **스크롤바를 드래그할 수 있는가?**
   - Scrollbar Vertical이 올바르게 연결되었는지 확인

4. ✅ **아이템 클릭 후 스크롤 위치가 유지되는가?**
   - Console에서 디버그 로그 확인:
     ```
     💾 [LobbyInventoryUI] 스크롤 위치 저장: (1.00, 0.65)
     🔄 [LobbyInventoryUI] 스크롤 위치 복원 시도:
        - 목표 위치: (1.00, 0.65)
        - 복원 후: (1.00, 0.65)
        - 성공 여부: True
     ```

---

## 🐛 **문제 해결**

### **문제 1: 스크롤바가 비활성화(회색)되어 있음**

**원인:** Content 높이 < Viewport 높이 (스크롤 불필요)

**해결책:**
1. **Viewport Height 줄이기:**
   - Viewport Rect Transform의 Anchor를 `Top-Stretch`로 변경
   - Height = 400으로 설정
2. **Content Size Fitter 확인:**
   - `Vertical Fit = Preferred Size` 설정되었는지 확인

---

### **문제 2: 스크롤 위치가 항상 (0, 0)으로 저장됨**

**원인:** 스크롤이 실제로 작동하지 않음 (문제 1과 동일)

**해결책:** 위의 "문제 1" 해결 후 재테스트

---

### **문제 3: 스크롤은 되는데 위치가 복원 안 됨**

**원인:** Grid Layout Group이 Content Size를 여러 프레임에 걸쳐 재계산

**해결책:** (현재 코드는 1프레임 대기 중, 추가 대기 시간 필요 시 알려주세요)

---

### **문제 4: Content가 Viewport 밖으로 넘어감**

**원인:** Content의 Anchor가 잘못 설정됨

**해결책:**
- Content Rect Transform의 Anchor = `Top-Stretch`
- Pivot = `(0.5, 1)`
- Left/Top/Right = `0, 0, 0`

---

## 🎯 **최종 체크리스트**

- [ ] ScrollView에 Scroll Rect 컴포넌트 있음
- [ ] Scroll Rect의 Horizontal **체크 해제** ✅
- [ ] Scroll Rect의 Vertical **체크** ✅
- [ ] Scroll Rect의 Content에 `Content (Rect Transform)` 연결
- [ ] Scroll Rect의 Viewport에 `Viewport (Rect Transform)` 연결
- [ ] Viewport에 Mask 컴포넌트 있음
- [ ] Viewport의 Anchor = `Stretch-Stretch` 또는 `Top-Stretch` (Height 고정)
- [ ] Content에 Grid Layout Group + **Content Size Fitter** 있음 ⭐
- [ ] Content Size Fitter의 `Vertical Fit = Preferred Size` ⭐
- [ ] Content의 Anchor = `Top-Stretch`, Pivot = `(0.5, 1)`
- [ ] Grid Layout Group의 Constraint = `Fixed Column Count = 8`
- [ ] Scrollbar Vertical이 Scroll Rect에 연결됨
- [ ] LobbyInventoryUI의 `Scroll Rect` 필드에 ScrollView 연결 ⭐

---

## 📌 **ShopInventoryUI에도 동일 설정 적용**

위의 모든 설정을 `ShopPanel > InventoryPanel > ScrollView`에도 동일하게 적용하세요!

---

## 🚀 **다음 단계**

설정 완료 후:
1. Unity Play 모드 시작
2. 로비 보관창고 열기
3. 스크롤을 아래로 내리기
4. 아이템 클릭 (RefreshInventoryUI 트리거)
5. **스크롤 위치가 유지되는지 확인** ✅

---

## 💡 **참고: 64칸 기준 권장 설정**

| 항목 | 권장 값 | 계산 |
|------|--------|------|
| **Viewport Height** | `400` | 4~5줄 슬롯 표시 |
| **Cell Size** | `80 x 80` | 아이콘 + 여백 |
| **Spacing** | `5 x 5` | 슬롯 간격 |
| **Column Count** | `8` | 8열 그리드 |
| **Content Height (자동)** | `≈ 695` | 80×8 + 5×7 + 20 (Padding) |

**결과:** Scrollbar가 활성화되어 4.5줄씩 스크롤 가능! 🎉

