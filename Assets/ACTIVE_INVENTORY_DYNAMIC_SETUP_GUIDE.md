# 🎒 ActiveInventory 동적 생성 방식 전환 완료 가이드

> **완료 날짜**: 2026-03-04  
> **목적**: 미리 배치 방식(16칸) → 동적 생성 방식(48칸) 전환  
> **패턴**: LobbyInventoryUI와 동일한 안정적인 구조

---

## ✅ 코드 수정 완료 (자동)

**ActiveInventory.cs 수정 사항**:
1. ✅ `slotPrefab` 필드 추가
2. ✅ `List<InventorySlot> activeSlots` 캐시 추가
3. ✅ `SetupSlots()` 메서드 추가 (48개 슬롯 동적 생성)
4. ✅ `SafeInitialization()`에서 `SetupSlots()` 호출
5. ✅ `RefreshInventoryUI()`에서 `activeSlots` 사용하도록 변경

---

## 🔧 Unity Editor 작업 (필수, 5분)

### Step 1: InventorySlot Prefab 생성

**방법 A: 기존 슬롯을 Prefab으로 저장 (권장)**
1. **인게임 Scene 열기** (Stage 또는 Test Scene)
2. **Hierarchy**에서 `ActiveInventory` 찾기
3. 기존 슬롯 중 하나 선택 (예: `InventorySlot_0`)
4. **Hierarchy**에서 **Project 창으로 드래그** → Prefab 생성
5. 저장 위치: `Assets/Prefabs/UI/InventorySlot.prefab` (또는 원하는 경로)
6. 생성 완료!

**방법 B: LobbyInventoryUI의 슬롯 Prefab 재사용 (빠름)**
1. **Project 창**에서 `LobbyInventorySlot` Prefab 찾기
2. 그대로 사용 (ActiveInventory에서 공유)
3. 끝!

---

### Step 2: ActiveInventory GameObject 설정

**인게임 Scene에서:**

1. **Hierarchy** → `ActiveInventory` GameObject 선택

2. **Inspector** → `Active Inventory (Script)` 컴포넌트:
   - `Max Display Slots`: **48** ✅ (이미 설정됨)
   - `Scroll Rect`: **빈 상태** (Step 3에서 설정)
   - `Slot Container`: **빈 상태** (Step 3에서 설정)
   - `Slot Prefab`: **Step 1에서 생성한 Prefab 드래그** ⭐

3. **저장**: `Ctrl + S`

---

### Step 3: ScrollView 추가 (선택 - 나중에 해도 됨)

**지금 당장 필수는 아닙니다. 일단 48칸 동적 생성만 먼저 확인하세요!**

ScrollView가 없어도 48개 슬롯은 정상 생성되고 작동합니다.  
ScrollView는 나중에 `ACTIVE_INVENTORY_SCROLLVIEW_GUIDE.md` 참고해서 추가하면 됩니다.

**임시 설정 (ScrollView 없이 테스트):**
1. `Slot Container`: **ActiveInventory** 자신을 드래그 (자기 자신)
2. 이렇게 하면 기존처럼 ActiveInventory 직속 하위에 슬롯 생성됨
3. 48개 슬롯이 세로로 길게 나열됨 (스크롤 없음)

---

### Step 4: 기존 슬롯 삭제

**중요: 동적 생성이므로 기존 슬롯은 삭제해야 합니다!**

1. **Hierarchy** → `ActiveInventory` 펼치기
2. 기존 `InventorySlot_0 ~ 15` (또는 모든 자식 슬롯) 선택
3. **Delete 키** 또는 우클릭 → Delete
4. **ActiveInventory 하위가 비어있어야 합니다!** (상세 패널 제외)

---

## 🎮 테스트 방법

### Test 1: 슬롯 동적 생성 확인
1. **Play Mode 진입**
2. **Console 확인**: `✅ [ActiveInventory] 48개 슬롯 동적 생성 완료` 로그
3. **I키 누름**: 가방 UI 열림
4. **Scene View** 또는 **Hierarchy**: ActiveInventory 하위에 48개 슬롯 생성 확인
5. **슬롯 개수 세기**: `ActiveInventorySlot_0 ~ ActiveInventorySlot_47`

### Test 2: 아이템 표시 확인
1. **몬스터 처치** → 아이템 드롭 → 픽업
2. **Console 확인**:
   - `✅ [AddItemV2] 아이템 획득 성공`
   - `🔄 [ActiveInventory] 캐릭터 가방 UI 새로고침 시작`
   - `📊 [ActiveInventory] 통합 가방 상태: 장비 X개`
   - `✅ [ActiveInventory] UI 새로고침 완료`
3. **I키 누름**: 가방에 아이템 표시 확인 ✅
4. **JSON 확인**: `characterBagInstanceIds` 배열 확인

### Test 3: 48개 아이템 스트레스 테스트
1. 아이템 48개까지 획득
2. 모든 슬롯에 아이템 표시 확인
3. 49번째 아이템: "가방 가득 참 → 우편함 이동" 로그 확인

---

## 🐛 문제 해결

### 문제 1: 슬롯이 하나도 생성 안 됨
- **원인**: `slotPrefab` 미할당
- **해결**: Inspector에서 InventorySlot Prefab 드래그

### 문제 2: 슬롯 생성되는데 아이템 안 보임
- **원인 A**: `slotContainer`가 잘못 할당됨
- **해결**: 임시로 `slotContainer`에 ActiveInventory 자기 자신 드래그
- **원인 B**: `OnCharacterBagChanged` 이벤트 미발생
- **해결**: 이미 코드에서 수정 완료 ✅

### 문제 3: 슬롯이 겹쳐서 생김
- **원인**: Grid Layout Group이 없음
- **해결**: 
  - Content GameObject 선택
  - Add Component → **Grid Layout Group**
  - Cell Size: `(100, 100)`, Spacing: `(10, 10)`
  - Constraint: **Fixed Column Count = 6**

### 문제 4: 기존 슬롯과 새 슬롯이 섞임
- **원인**: Step 4 (기존 슬롯 삭제) 안 함
- **해결**: ActiveInventory 하위 기존 슬롯 전부 삭제

---

## 📊 동적 생성 vs 미리 배치 비교

### ❌ 미리 배치 방식 (기존):
```
장점:
- Scene에서 슬롯 위치 시각적 편집 가능
- Prefab 불필요

단점:
- 슬롯 개수 변경 시 Scene 수동 편집 필수 ⚠️
- 16칸 → 48칸 확장 시 32개 슬롯 복사/붙여넣기 ⚠️
- 코드와 Scene 불일치 위험 (코드 48칸, Scene 16칸) 🔴
- ScrollView 적용 어려움 (32개 슬롯을 Content로 이동) ⚠️
```

### ✅ 동적 생성 방식 (변경 후):
```
장점:
- 슬롯 개수 변경 시 코드 1줄만 수정 ✅
- ScrollView 호환 완벽 (Content Size Fitter 자동 조절) ✅
- 코드 = 진실 (불일치 불가능) ✅
- 로비/상점과 동일한 패턴 (일관성) ✅
- Scene 수정 불필요 ✅

단점:
- slotPrefab 설정 필요 (1회만)
- SetupSlots() 메서드 추가 (이미 완료 ✅)
```

---

## 🎯 최종 체크리스트

### 필수 작업:
- [ ] **Step 1**: InventorySlot Prefab 생성 (기존 슬롯 → Prefab)
- [ ] **Step 2**: ActiveInventory Inspector → `Slot Prefab` 할당
- [ ] **Step 4**: ActiveInventory 하위 기존 슬롯 전부 삭제
- [ ] **Test 1**: Play Mode → Console에서 "48개 슬롯 동적 생성 완료" 확인
- [ ] **Test 2**: 아이템 픽업 → I키 → 슬롯에 아이템 표시 확인

### 선택 작업 (나중에):
- [ ] **Step 3**: ScrollView 추가 (`ACTIVE_INVENTORY_SCROLLVIEW_GUIDE.md` 참고)
- [ ] Grid Layout Group 설정 (6열 × 8행)
- [ ] Content Size Fitter 설정

---

## 💡 빠른 시작 (3분)

1. **InventorySlot_0 → Prefab 생성** (Project 창에 드래그)
2. **ActiveInventory Inspector → Slot Prefab 할당**
3. **ActiveInventory 하위 슬롯 전부 삭제**
4. **Slot Container에 ActiveInventory 자기 자신 드래그** (임시)
5. **Play Mode → 아이템 픽업 테스트**

✅ 이제 48칸 동적 생성 + 아이템 표시 정상 작동!

---

## 🔗 다음 단계

**ScrollView 적용 (선택)**:
- `ACTIVE_INVENTORY_SCROLLVIEW_GUIDE.md` 2단계부터 진행
- ScrollView → Viewport → Content 구조 생성
- slotContainer를 Content로 변경
- Grid Layout + Content Size Fitter 설정

**현재 상태로도 48칸 사용 가능하지만, 세로로 길게 나열되므로 ScrollView 추가 권장!**

