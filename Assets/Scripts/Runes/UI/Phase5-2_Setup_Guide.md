# Phase 5-2: 장착 및 강화 UI 설정 가이드

⚙️ **Phase 5-2: Drag & Drop + Enhance UI**

---

## 🎯 구현 목표

1. **드래그 앤 드롭**: 인벤토리 → 3개 장착 슬롯으로 룬 장착
2. **장착 해제**: 우클릭/더블클릭으로 슬롯에서 제거
3. **레벨업 UI**: 스탯 미리보기 + 강화 버튼
4. **한계돌파 UI**: 재료 선택 + 한계돌파 버튼

---

## 📦 컴포넌트 개요

### 1. RuneDragHandler.cs
- **역할**: 인벤토리 룬을 드래그 가능하게 만듦
- **부착 위치**: `RuneSlotPrefab` (자동 추가)
- **핵심 기능**:
  - `OnBeginDrag`: 임시 아이콘 생성, 원본 반투명
  - `OnDrag`: 마우스 커서 따라다님
  - `OnEndDrag`: 원본 복구, 임시 아이콘 제거

### 2. RuneEquipSlotUI.cs
- **역할**: 3개의 장착 슬롯 (드롭 타겟)
- **부착 위치**: `RuneEquipSlot[0/1/2]` GameObject
- **핵심 기능**:
  - `OnDrop`: 드롭된 룬을 `RuneManager.EquipRune()` 호출하여 장착
  - `OnPointerClick`: 우클릭/더블클릭 시 `RuneManager.UnequipRune()` 호출
  - `RefreshSlot`: 현재 장착된 룬 표시

### 3. RuneEnhanceUI.cs
- **역할**: 레벨업 & 한계돌파 UI
- **부착 위치**: `RuneEnhancePanel` GameObject
- **핵심 기능**:
  - **레벨업 탭**: 현재/다음 스탯 비교, `TryLevelUp()` 호출
  - **한계돌파 탭**: 재료 필터링, `TryLimitBreak()` 호출

---

## 🛠️ Unity 에디터 설정

### Step 1: RuneEquipSlot 프리팹 생성

#### 1.1 빈 GameObject 생성
- 이름: `RuneEquipSlot`
- Canvas 하위에 배치

#### 1.2 하이어라키 구조
```
RuneEquipSlot
├── BackgroundImage (Image)
├── RuneIconImage (Image)
├── LevelText (TextMeshProUGUI)
├── EmptySlotIndicator (GameObject)
│   └── EmptyText (TextMeshProUGUI) - "빈 슬롯"
├── ErrorMessageText (TextMeshProUGUI)
└── LimitBreakStarsContainer
    ├── Star1 (Image)
    ├── Star2 (Image)
    ├── Star3 (Image)
    ├── Star4 (Image)
    └── Star5 (Image)
```

#### 1.3 컴포넌트 추가
- `RuneEquipSlot`에 **RuneEquipSlotUI** 스크립트 추가

#### 1.4 SerializeField 매핑

| 필드 이름 | 컴포넌트 | 설명 |
|---------|---------|------|
| `slotIndex` | - | **0, 1, 2** (Inspector에서 직접 입력) |
| `runeIconImage` | `RuneIconImage` (Image) | 룬 아이콘 |
| `backgroundImage` | `BackgroundImage` (Image) | 배경 이미지 |
| `levelText` | `LevelText` (TMP) | "Lv.10" |
| `emptySlotIndicator` | `EmptySlotIndicator` (GameObject) | 빈 슬롯 표시 |
| `limitBreakStars` | Array[5] | Star1~5 (Image) |
| `errorMessageText` | `ErrorMessageText` (TMP) | 에러 메시지 |

#### 1.5 프리팹으로 저장
- `Assets/Resources/Prefabs/UI/RuneEquipSlot.prefab`

#### 1.6 씬에 3개 배치
- `RuneEquipSlot0` (slotIndex = 0)
- `RuneEquipSlot1` (slotIndex = 1)
- `RuneEquipSlot2` (slotIndex = 2)

---

### Step 2: RuneEnhancePanel 생성

#### 2.1 빈 GameObject 생성
- 이름: `RuneEnhancePanel`
- Canvas 하위에 배치

#### 2.2 하이어라키 구조
```
RuneEnhancePanel
├── TabButtons
│   ├── LevelUpTabButton (Button)
│   │   └── Text (TMP) - "레벨업"
│   └── LimitBreakTabButton (Button)
│       └── Text (TMP) - "한계돌파"
├── CommonInfo
│   ├── RuneIcon (Image)
│   ├── RuneNameText (TMP)
│   └── CurrentLevelText (TMP)
├── LevelUpPanel
│   ├── StatsCompareContainer
│   │   ├── CurrentStatText (TMP)
│   │   ├── ArrowText (TMP) - "→"
│   │   └── NextStatText (TMP)
│   ├── LevelUpButton (Button)
│   │   └── ButtonText (TMP) - "강화"
│   └── LevelUpMessageText (TMP)
└── LimitBreakPanel
    ├── InfoTexts
    │   ├── CurrentMaxLevelText (TMP)
    │   └── LimitBreakCountText (TMP)
    ├── MaterialSection
    │   └── MaterialSlotsScrollView
    │       └── Viewport
    │           └── MaterialSlotsContainer (Vertical Layout Group)
    ├── LimitBreakButton (Button)
    │   └── ButtonText (TMP) - "한계돌파"
    └── LimitBreakMessageText (TMP)
```

#### 2.3 컴포넌트 추가
- `RuneEnhancePanel`에 **RuneEnhanceUI** 스크립트 추가

#### 2.4 SerializeField 매핑

##### 탭 전환
| 필드 이름 | 컴포넌트 | 설명 |
|---------|---------|------|
| `levelUpTabButton` | `LevelUpTabButton` (Button) | 레벨업 탭 버튼 |
| `limitBreakTabButton` | `LimitBreakTabButton` (Button) | 한계돌파 탭 버튼 |
| `levelUpPanel` | `LevelUpPanel` (GameObject) | 레벨업 패널 |
| `limitBreakPanel` | `LimitBreakPanel` (GameObject) | 한계돌파 패널 |

##### 공통 정보
| 필드 이름 | 컴포넌트 | 설명 |
|---------|---------|------|
| `runeIconImage` | `RuneIcon` (Image) | 룬 아이콘 |
| `runeNameText` | `RuneNameText` (TMP) | "보스 사냥꾼" |
| `currentLevelText` | `CurrentLevelText` (TMP) | "Lv.10 (한계돌파 +0)" |

##### 레벨업 UI
| 필드 이름 | 컴포넌트 | 설명 |
|---------|---------|------|
| `currentStatText` | `CurrentStatText` (TMP) | "보스 피해 증가 20%" |
| `nextStatText` | `NextStatText` (TMP) | "보스 피해 증가 21%" |
| `arrowText` | `ArrowText` (TMP) | "→" |
| `levelUpButton` | `LevelUpButton` (Button) | 강화 버튼 |
| `levelUpButtonText` | `ButtonText` (TMP) | "강화" |
| `levelUpMessageText` | `LevelUpMessageText` (TMP) | 상태 메시지 |

##### 한계돌파 UI
| 필드 이름 | 컴포넌트 | 설명 |
|---------|---------|------|
| `currentMaxLevelText` | `CurrentMaxLevelText` (TMP) | "현재 최대 레벨: 10" |
| `limitBreakCountText` | `LimitBreakCountText` (TMP) | "한계돌파: 0 / 5" |
| `materialSlotsContainer` | `MaterialSlotsContainer` (Transform) | 재료 슬롯 부모 |
| `materialSlotPrefab` | **RuneSlotPrefab** | 재료 슬롯 프리팹 (재사용) |
| `limitBreakButton` | `LimitBreakButton` (Button) | 한계돌파 버튼 |
| `limitBreakButtonText` | `ButtonText` (TMP) | "한계돌파" |
| `limitBreakMessageText` | `LimitBreakMessageText` (TMP) | 상태 메시지 |

---

## 🧪 테스트 방법

### 에디터 메뉴 테스트 (추천)

1. **Unity 메뉴**: `Runes > Phase 5 - UI > Phase 5-2/5. 풀 시나리오 테스트`
2. **콘솔 확인**:
   ```
   ✅ 데이터 클리어
   ✅ 테스트 룬 생성
   ✅ 첫 번째 룬 장착
   ✅ 레벨업 3회
   ✅ 한계돌파
   ✅ UI 갱신
   ```

### 씬 실행 테스트

#### 1. 드래그 앤 드롭 테스트
- 인벤토리에서 룬 클릭 → 드래그
- 장착 슬롯으로 드롭
- **예상 결과**: 슬롯에 룬 아이콘, 레벨, 별 표시

#### 2. 중복 장착 테스트
- 동일한 룬을 다른 슬롯에 드래그
- **예상 결과**: "동일한 룬은 장착할 수 없습니다" 메시지

#### 3. 장착 해제 테스트
- 장착된 슬롯 우클릭 또는 더블클릭
- **예상 결과**: 슬롯이 비워지고 인벤토리에 복귀

#### 4. 레벨업 테스트
- 강화 UI에서 룬 선택
- 레벨업 탭 확인: "20% → 21%" 표시
- [강화] 버튼 클릭
- **예상 결과**: 레벨 증가, 스탯 수치 증가, UI 갱신

#### 5. 한계돌파 테스트
- 룬을 최대 레벨까지 성장
- 한계돌파 탭으로 전환
- 재료 목록에서 동일 종류 1레벨 룬 선택
- [한계돌파] 버튼 클릭
- **예상 결과**: 한계돌파 횟수 증가, 최대 레벨 상승, 재료 소모

---

## ⚠️ 주의사항

### 1. CanvasGroup 설정
- `RuneSlotPrefab`에 `CanvasGroup` 컴포넌트가 자동으로 추가됩니다
- `blocksRaycasts`를 조절하여 드롭 타겟이 이벤트를 받을 수 있게 처리

### 2. dragParent 설정
- `RuneDragHandler`의 `dragParent` 필드에 **최상위 Canvas**를 드래그
- 설정하지 않으면 자동으로 부모 Canvas를 찾음

### 3. materialSlotPrefab 재사용
- `RuneEnhanceUI`의 `materialSlotPrefab`은 기존 `RuneSlotPrefab`을 재사용
- 별도로 새 프리팹을 만들 필요 없음

### 4. 슬롯 인덱스
- `RuneEquipSlotUI`의 `slotIndex`는 **0, 1, 2**로 설정
- 각 슬롯은 고유한 인덱스를 가져야 함

---

## 🐛 문제 해결

### Q1. "동일한 룬은 장착할 수 없습니다" 메시지가 표시되지 않음

**원인**: `errorMessageText`가 연결되지 않음

**해결**:
1. `RuneEquipSlotUI` Inspector 확인
2. `Error Message Text` 필드에 TMP 드래그

### Q2. 드래그 중 아이콘이 보이지 않음

**원인**: `dragParent`가 설정되지 않음

**해결**:
1. `RuneDragHandler` Inspector 확인
2. `Drag Parent` 필드에 최상위 Canvas 드래그

### Q3. 드롭이 작동하지 않음

**원인**: `CanvasGroup.blocksRaycasts`가 true로 유지됨

**해결**:
- `RuneDragHandler.OnBeginDrag()`에서 자동으로 처리됨
- 수동 수정 불필요

### Q4. 재료 목록이 비어있음

**원인**: 동일 `runeId`의 1레벨 룬이 없음

**해결**:
1. 에디터 메뉴: `Runes > Phase 5 - UI > Phase 5-2/4. 한계돌파 테스트`
2. 자동으로 재료 룬 생성됨

### Q5. 강화 UI에서 룬 선택이 안 됨

**원인**: `RuneEnhanceUI`가 비활성화되어 있음

**해결**:
1. 에디터 메뉴: `Runes > Phase 5 - UI > Phase 5-2/2. 강화 UI 선택 테스트`
2. 자동으로 패널 활성화 및 룬 선택

---

## 📊 전체 UI 흐름

```
인벤토리 (RuneInventoryUI)
  ↓ 드래그 (RuneDragHandler)
장착 슬롯 (RuneEquipSlotUI)
  ↓ 선택
강화 UI (RuneEnhanceUI)
  ↓ 레벨업/한계돌파
인벤토리 갱신 (RefreshInventory)
```

---

## 🎮 작동 원리

### 드래그 앤 드롭 플로우

1. **유저**: 인벤토리 룬을 클릭 & 드래그
2. **RuneDragHandler**: `OnBeginDrag()` → 원본 반투명, 임시 아이콘 생성
3. **RuneDragHandler**: `OnDrag()` → 임시 아이콘이 마우스 커서 따라다님
4. **유저**: 장착 슬롯에 드롭
5. **RuneEquipSlotUI**: `OnDrop()` → `RuneManager.EquipRune()` 호출
6. **RuneManager**: 중복 체크 → 장착 처리 → 이벤트 발생
7. **RuneEquipSlotUI**: 성공 시 `RefreshSlot()` + `RefreshInventoryUI()` 호출
8. **RuneDragHandler**: `OnEndDrag()` → 원본 복구, 임시 아이콘 제거

### 레벨업 플로우

1. **유저**: 인벤토리에서 룬 클릭
2. **RuneInventoryUI**: `OnSlotClicked()` → 툴팁 표시
3. **유저**: 강화 UI 열기 → 룬 선택
4. **RuneEnhanceUI**: `SelectRune()` → 현재/다음 스탯 계산 및 표시
5. **유저**: [강화] 버튼 클릭
6. **RuneEnhanceUI**: `OnLevelUpButtonClick()` → `RuneEnhanceManager.TryLevelUp()` 호출
7. **RuneEnhanceManager**: 레벨업 처리 → 이벤트 발생
8. **RuneEnhanceUI**: `RefreshAllUI()` → 인벤토리, 툴팁, 장착 슬롯 갱신

### 한계돌파 플로우

1. **유저**: 최대 레벨 룬 선택
2. **RuneEnhanceUI**: 한계돌파 탭으로 전환
3. **RuneEnhanceUI**: `RefreshMaterialSlots()` → 동일 `runeId` 1레벨 룬 필터링
4. **유저**: 재료 룬 선택
5. **RuneEnhanceUI**: `selectedMaterialRune` 저장
6. **유저**: [한계돌파] 버튼 클릭
7. **RuneEnhanceUI**: `OnLimitBreakButtonClick()` → `RuneEnhanceManager.TryLimitBreak()` 호출
8. **RuneEnhanceManager**: 한계돌파 처리 → 재료 소모 → 이벤트 발생
9. **RuneEnhanceUI**: `RefreshAllUI()` → 모든 UI 갱신

---

## 📝 코드 설계 원칙

### 1. View Only (데이터 수정 금지)
```csharp
// ❌ 잘못된 예시
public void OnLevelUpButtonClick()
{
    selectedRune.currentLevel++; // UI에서 데이터 직접 수정 (금지!)
}

// ✅ 올바른 예시
private void OnLevelUpButtonClick()
{
    enhanceManager.TryLevelUp(selectedRune.instanceUID); // 매니저 호출
    RefreshAllUI(); // UI만 갱신
}
```

### 2. 매니저 API 호출
```csharp
// 장착
bool success = runeManager.EquipRune(runeInstance, slotIndex);

// 해제
bool success = runeManager.UnequipRune(slotIndex);

// 레벨업
bool success = enhanceManager.TryLevelUp(runeUID);

// 한계돌파
bool success = enhanceManager.TryLimitBreak(baseRuneUID, materialRuneUID);
```

### 3. UI 갱신 체인
```csharp
// 강화 완료 후 모든 관련 UI 갱신
private void RefreshAllUI()
{
    // 1. 인벤토리 (레벨 표시 업데이트)
    FindObjectOfType<RuneInventoryUI>()?.RefreshInventory();
    
    // 2. 툴팁 (스탯 표시 업데이트)
    FindObjectOfType<RuneTooltipUI>()?.ShowTooltip(selectedRune);
    
    // 3. 장착 슬롯 (장착된 룬 표시 업데이트)
    foreach (var slot in FindObjectsOfType<RuneEquipSlotUI>())
    {
        slot.RefreshSlot();
    }
}
```

---

## 🎨 UI 디자인 팁

### 1. 드래그 시각 피드백
- 원본 슬롯: `alpha = 0.3` (반투명)
- 임시 아이콘: `alpha = 0.6`, `sizeDelta = (100, 100)`

### 2. 슬롯 색상
- 빈 슬롯: `Color(0.2, 0.2, 0.2, 1)`
- 장착된 슬롯: `Color(0.5, 0.8, 1, 1)` (파란색)

### 3. 에러 메시지
- 표시 시간: 2초
- 색상: 빨간색 (`Color.red`)

### 4. 한계돌파 별
- 활성화: `Color.yellow`
- 비활성화: `Color(0.3, 0.3, 0.3, 1)` (회색)

---

## 🚀 다음 단계

Phase 5-2 완료 후:
- **Phase 5-3**: 룬 필터링 & 정렬 UI
- **Phase 5-4**: 룬 잠금 및 일괄 처리 UI
- **Phase 6**: 전투 중 룬 효과 실시간 적용 (CombatEntity 연동)

---

**작성일**: 2026-02-27  
**버전**: Phase 5-2  
**작성자**: AI Assistant
