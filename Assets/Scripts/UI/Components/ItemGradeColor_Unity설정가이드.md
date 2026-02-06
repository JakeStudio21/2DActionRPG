# 🎨 아이템 등급별 배경 색상 시스템 Unity 설정 가이드

## 📋 목차
1. [ItemGradeColorConfig 생성](#step-1-itemgradecolorconfig-생성-5분)
2. [ItemGradeColorManager 설정](#step-2-itemgradecolormanager-설정-3분)
3. [ItemDetailPopup 설정](#step-3-itemdetailpopup-설정-5분)
4. [InventorySlot 설정](#step-4-inventoryslot-설정-10분)
5. [테스트 및 색상 조정](#step-5-테스트-및-색상-조정-5분)

**총 예상 시간: 30분**

---

## 📍 Step 1: ItemGradeColorConfig 생성 (5분)

### **1-1. ScriptableObject 생성**

1. **Project 창에서 폴더 생성:**
   - `Assets/Resources/Config/` 폴더 생성 (없으면)

2. **ScriptableObject 생성:**
   - `Assets/Resources/Config/` 폴더 우클릭
   - `Create` → `Config` → `Item Grade Colors`
   - 이름: `ItemGradeColorConfig`

### **1-2. 등급별 색상 설정**

`ItemGradeColorConfig` 선택 → Inspector에서 8등급 색상 설정:

#### **기본 권장 색상:**

| 등급 | 색상 이름 | RGB 값 | Hex |
|------|-----------|--------|-----|
| **D** | 회색 | (128, 128, 128) | #808080 |
| **C** | 흰색 | (255, 255, 255) | #FFFFFF |
| **B** | 초록색 | (0, 255, 0) | #00FF00 |
| **A** | 파란색 | (0, 102, 255) | #0066FF |
| **S** | 노란색 | (255, 220, 0) | #FFDC00 |
| **SS** | 주황색 | (255, 128, 0) | #FF8000 |
| **EX** | 보라색 | (255, 0, 255) | #FF00FF |
| **TR** | 빨간색 | (255, 0, 0) | #FF0000 |

#### **Inspector 설정 방법:**

```
Grade Colors (Array, Size: 8)
├── Element 0
│   ├── Grade: D
│   └── Color: RGB(128, 128, 128) ← 색상 클릭하여 변경
├── Element 1
│   ├── Grade: C
│   └── Color: RGB(255, 255, 255)
├── Element 2
│   ├── Grade: B
│   └── Color: RGB(0, 255, 0)
... (8개 등급 모두 설정)
```

---

## 📍 Step 2: ItemGradeColorManager 설정 (3분)

### **2-1. Managers GameObject 찾기/생성**

1. **Hierarchy에서 확인:**
   - `Managers` GameObject 찾기
   - 없으면: Hierarchy 우클릭 → `Create Empty` → 이름: `Managers`

### **2-2. ItemGradeColorManager 추가**

1. `Managers` GameObject 선택
2. Inspector → `Add Component`
3. `ItemGradeColorManager` 검색하여 추가

### **2-3. Config 연결**

1. `ItemGradeColorManager` 컴포넌트에서:
   - `Config` 필드에 `ItemGradeColorConfig.asset` 드래그 ✅
   - `Show Debug Logs`: ☐ (체크 해제) ← 나중에 필요시 활성화

---

## 📍 Step 3: ItemDetailPopup 설정 (5분)

### **3-1. ItemIcon_Background 찾기**

**Hierarchy 경로:**
```
PopupCanvas
└── ItemDetailPopup
    └── PopupPanel
        └── Content
            └── ItemIcon_Background ← 여기에 컴포넌트 추가
```

### **3-2. ItemIconGradeFrame 컴포넌트 추가**

1. `ItemIcon_Background` GameObject 선택
2. Inspector → `Add Component`
3. `ItemIconGradeFrame` 검색하여 추가

### **3-3. 컴포넌트 설정**

**ItemIconGradeFrame 컴포넌트:**
```
Background Image: ItemIcon_Background (자기 자신 - Image)
Current Grade: D (기본값)
Default Color: White (255, 255, 255)
Show Debug Logs: ☐
```

### **3-4. ItemDetailPopup 스크립트 연결**

1. `ItemDetailPopup` GameObject 선택
2. Inspector에서 `ItemDetailPopup` 컴포넌트 찾기
3. **🎨 아이템 정보 UI** 섹션에서:
   - `Item Icon Grade Frame`: `ItemIcon_Background` 드래그 ✅

---

## 📍 Step 4: InventorySlot 설정 (10분)

### **4-1. InventorySlot 프리팹 찾기**

**위치:**
- `Assets/Prefabs/UI/InventorySlot.prefab` (또는 유사 경로)
- Hierarchy에서 `LobbyInventoryPanel` → `SlotContainer` 하위 슬롯들

### **4-2. 프리팹 구조 확인**

```
InventorySlot (Prefab)
├── SlotBackground
├── ItemIcon
└── ItemIcon_Background ← 여기에 컴포넌트 추가
```

**ItemIcon_Background가 없으면:**
1. `InventorySlot` 하위에 `Create Empty` → 이름: `ItemIcon_Background`
2. `Add Component` → `Image`
3. **RectTransform:**
   - Anchor: `Stretch`
   - Left/Right/Top/Bottom: `0`
4. **Image:**
   - Color: `White (255, 255, 255, 255)`
   - Sprite: (선택사항, 프레임 이미지)

### **4-3. ItemIconGradeFrame 컴포넌트 추가**

1. `ItemIcon_Background` GameObject 선택
2. Inspector → `Add Component`
3. `ItemIconGradeFrame` 검색하여 추가
4. **설정:**
   - `Background Image`: 자기 자신 (Image) ← 자동으로 찾음
   - `Current Grade`: D
   - `Show Debug Logs`: ☐

### **4-4. InventorySlot 스크립트 연결**

1. `InventorySlot` GameObject 선택
2. Inspector에서 `InventorySlot` 컴포넌트 찾기
3. **🎨 UI 컴포넌트** 섹션에서:
   - `Item Icon Grade Frame`: `ItemIcon_Background` 드래그 ✅

### **4-5. 프리팹 적용**

1. 프리팹 편집 완료 후 저장
2. Hierarchy의 모든 InventorySlot에 자동 적용됨

---

## 📍 Step 5: 테스트 및 색상 조정 (5분)

### **5-1. 플레이 모드 테스트**

1. **Unity Play 버튼 클릭**
2. **로비에서 보관창고 열기**
3. **다양한 등급 아이템 클릭:**
   - D등급 → 회색 배경 ✅
   - S등급 → 노란색 배경 ✅
   - SS등급 → 주황색 배경 ✅
   - TR등급 → 빨간색 배경 ✅

### **5-2. 색상 조정 (원하는 색상으로 변경)**

**실시간 조정:**
1. **플레이 모드 중지**
2. `ItemGradeColorConfig.asset` 선택
3. Inspector에서 등급별 색상 변경:
   - 예: S등급을 더 밝은 노란색으로 변경
   - 예: TR등급을 더 진한 빨간색으로 변경
4. **재생하여 확인**

**색상 추천 팁:**
- 낮은 등급(D~B): 차분한 색상 (회색, 초록, 파랑)
- 중간 등급(A~S): 눈에 띄는 색상 (밝은 파랑, 노랑)
- 최고 등급(SS~TR): 화려한 색상 (주황, 보라, 빨강)

### **5-3. 디버그 로그 확인 (선택사항)**

**문제 발생 시:**
1. `ItemGradeColorManager` 선택
2. `Show Debug Logs`: ✓ (체크)
3. Console 창에서 로그 확인:
   ```
   🎨 [ItemGradeColorManager] S 등급 색상 반환: RGBA(1.00, 0.86, 0.00, 1.00)
   ```

---

## ✅ 최종 확인 체크리스트

- [ ] `ItemGradeColorConfig.asset` 생성 및 8등급 색상 설정 완료
- [ ] `ItemGradeColorManager` Hierarchy에 추가 및 Config 연결
- [ ] `ItemDetailPopup/ItemIcon_Background`에 `ItemIconGradeFrame` 추가
- [ ] `ItemDetailPopup` 스크립트에서 `itemIconGradeFrame` 필드 연결
- [ ] `InventorySlot/ItemIcon_Background`에 `ItemIconGradeFrame` 추가
- [ ] `InventorySlot` 스크립트에서 `itemIconGradeFrame` 필드 연결
- [ ] 플레이 모드에서 다양한 등급 아이템 테스트
- [ ] 원하는 색상으로 조정 완료

---

## 🎨 추가 UI 적용 (선택사항)

### **동일한 방법으로 적용 가능:**

**1. ShopSlot (상점)**
- `ShopSlot/ItemIcon_Background`에 `ItemIconGradeFrame` 추가
- `ShopSlot` 스크립트에 필드 연결

**2. EquipmentSlot (장비창)**
- `EquipmentSlot/ItemIcon_Background`에 `ItemIconGradeFrame` 추가
- `EquipmentSlot` 스크립트에 필드 연결

**3. ActiveInventory (인게임 가방)**
- 동일한 방식으로 적용

**패턴:**
```csharp
// 스크립트에 필드 추가
[SerializeField] private ItemIconGradeFrame itemIconGradeFrame;

// SetEquipmentData() 메서드에서 호출
if (itemIconGradeFrame != null && data != null)
{
    itemIconGradeFrame.SetGrade(data.itemGrade);
}
```

---

## 🚀 예상 결과

**ItemDetailPopup:**
```
┌─────────────────┐
│  [아이템 아이콘] │ ← 배경: SS등급 주황색
│   (SS 등급)     │
└─────────────────┘
```

**보관창고:**
```
[슬롯1] [슬롯2] [슬롯3] [슬롯4]
 D등급   S등급   SS등급  TR등급
 회색    노란색  주황색  빨간색
```

---

## 🐛 문제 해결

### **문제 1: 색상이 적용되지 않음**

**원인:** ItemGradeColorManager가 Config를 찾지 못함

**해결:**
1. `ItemGradeColorConfig.asset`가 `Assets/Resources/Config/` 경로에 있는지 확인
2. `ItemGradeColorManager` Inspector에서 `Config` 필드 수동 연결
3. Console에서 에러 메시지 확인

### **문제 2: 모든 아이템이 흰색**

**원인:** `itemIconGradeFrame` 필드가 연결되지 않음

**해결:**
1. `ItemDetailPopup` 또는 `InventorySlot` Inspector 확인
2. `Item Icon Grade Frame` 필드에 `ItemIcon_Background` 연결
3. `ItemIcon_Background`에 `ItemIconGradeFrame` 컴포넌트 있는지 확인

### **문제 3: NullReferenceException 발생**

**원인:** ItemGradeColorManager.Instance가 null

**해결:**
1. Hierarchy에 `ItemGradeColorManager` 있는지 확인
2. DontDestroyOnLoad 설정 확인
3. 씬 시작 시 자동 생성되도록 대기

---

## 🎉 완료!

모든 설정이 완료되었습니다! 

이제 모든 아이템 UI에서 등급별 배경 색상이 자동으로 적용됩니다.

색상이 마음에 들지 않으면 언제든 `ItemGradeColorConfig.asset`에서 변경하세요! 🎨

