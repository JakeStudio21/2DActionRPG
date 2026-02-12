# 🏭 FusionUI Inspector 설정 가이드

## 📋 목차
1. [필수 프리팹 생성](#1-필수-프리팹-생성)
2. [UI 구조 이해](#2-ui-구조-이해)
3. [FusionUI 컴포넌트 설정](#3-fusionui-컴포넌트-설정)
4. [WorkshopUI 연동](#4-workshopui-연동)
5. [테스트 체크리스트](#5-테스트-체크리스트)

---

## 1. 필수 프리팹 생성

### 1-1. GradeGroupPrefab 생성

**목적**: 등급별 선택 아이템 **요약 정보 표시** (텍스트 UI)

**⚠️ 주의**: 아이템 슬롯이 아닙니다! 선택된 아이템의 통계 정보만 표시합니다.

**생성 방법**:
1. Hierarchy에서 우클릭 → `UI → Panel` 생성
2. 이름: `GradeGroupPrefab`
3. 하위에 다음 UI 요소 추가:

```
GradeGroupPrefab (Panel)
├─ GradeText (TextMeshProUGUI)
│  └─ Text: "D등급"
│  └─ Font Size: 18
│  └─ Alignment: Left
│
├─ CountText (TextMeshProUGUI)
│  └─ Text: "3개 선택"
│  └─ Font Size: 16
│  └─ Color: White
│
└─ StatusText (TextMeshProUGUI)
   └─ Text: "✅ 1회 합성 가능"
   └─ Font Size: 14
   └─ Color: Green
```

**레이아웃 설정**:
- GradeGroupPrefab:
  - Width: 400px
  - Height: 60px
  - HorizontalLayoutGroup 추가 (Spacing: 10px)
  
- GradeText: Preferred Width: 80px
- CountText: Preferred Width: 120px
- StatusText: Flexible Width: 1

**프리팹 저장**:
- `Assets/Prefabs/UI/Workshop/` 폴더에 저장
- 이름: `GradeGroupPrefab.prefab`

---

### 1-2. MaterialSlotPrefab 확인

**위치**: `Assets/Prefabs/UI/InventorySlot.prefab`

**확인 사항**:
- ✅ InventorySlot 컴포넌트 있음
- ✅ SetupMaterial() 메서드 지원
- ✅ SetEquipmentData() 메서드 지원

---

## 2. UI 구조 이해

### 2-1. FusionUI의 두 가지 표시 영역

```
┌─────────────────────────────────────────────┐
│  SelectedItemsPanel (상단)                   │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  📊 선택된 아이템 요약 정보                    │
│  ┌─────────────────────────────────────┐   │
│  │ D등급   9개 선택   ✅ 3회 합성 가능   │   │
│  ├─────────────────────────────────────┤   │
│  │ C등급   5개 선택   ✅ 1회 합성 가능   │   │
│  └─────────────────────────────────────┘   │
│  ↑ GradeGroupPrefab (텍스트만)              │
├─────────────────────────────────────────────┤
│  RewardPreview (하단)                        │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━  │
│  🎁 합성 결과 미리보기                        │
│  ┌────┐ ┌────┐ ┌────┐                      │
│  │ C  │ │ C  │ │ B  │                      │
│  │ 검 │ │ 활 │ │ 검 │                      │
│  └────┘ └────┘ └────┘                      │
│  ↑ LobbyInventorySlot (아이템 슬롯)          │
└─────────────────────────────────────────────┘
```

### 2-2. 프리팹 비교표

| 영역 | 프리팹 | 타입 | 표시 내용 | 동적 생성 |
|------|--------|------|-----------|-----------|
| **SelectedItemsPanel** | GradeGroupPrefab | 텍스트 UI | 등급별 요약 정보 | CreateGradeGroup() |
| **RewardPreview** | LobbyInventorySlot | 아이템 슬롯 | 합성 결과 아이템 | CreateResultSlot() |

### 2-3. 사용자가 보는 화면 예시

```
선택된 아이템 (WorkshopInventoryUI - 왼쪽)
[✓ D검1] [✓ D검2] [✓ D검3]
[✓ D검4] [✓ D검5] [✓ D검6]
[✓ C활1] [✓ C활2] [✓ C활3]

↓ 요약 정보 (SelectedItemsPanel - 우측 상단)
━━━━━━━━━━━━━━━━━━━━━━━━━
D등급   6개 선택   ✅ 2회 합성 가능
C등급   3개 선택   ✅ 1회 합성 가능
━━━━━━━━━━━━━━━━━━━━━━━━━

↓ 결과 미리보기 (RewardPreview - 우측 하단)
━━━━━━━━━━━━━━━━━━━━━━━━━
[C검] [C검] [B활]
━━━━━━━━━━━━━━━━━━━━━━━━━

[합성하기 (3회)]
```

---

## 3. FusionUI 컴포넌트 설정

### 3-1. 연동 컴포넌트

#### WorkshopInventoryUI
```
Hierarchy 경로: WorkshopPanel/LeftSection/WorkshopInventoryUI
드래그 앤 드롭 → FusionUI.workshopInventoryUI
```

---

### 3-2. Right Section (선택 정보)

#### SelectedItemsPanel
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/SelectedItemsPanel
드래그 앤 드롭 → FusionUI.selectedItemsPanel
```
**역할**: 등급별 선택 아이템 **요약 정보** 표시  
**사용 프리팹**: GradeGroupPrefab (아이템 슬롯 아님!)  
**표시 내용**: "D등급 9개 선택 (3회 합성 가능)" 형태의 텍스트

#### SelectedGroupsContainer
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/SelectedItemsPanel/ScrollView/Viewport/Content
드래그 앤 드롭 → FusionUI.selectedGroupsContainer
```
- ⭐ VerticalLayoutGroup 추가
  - Spacing: 10px
  - Child Force Expand: Width ✅, Height ❌
- **동적 생성**: 등급별 GradeGroupPrefab

#### GradeGroupPrefab
```
드래그 앤 드롭 → FusionUI.gradeGroupPrefab
(1-1에서 생성한 프리팹)
```
**⚠️ 중요**: 텍스트 요약 UI입니다. 아이템 슬롯 프리팹이 아닙니다!

---

### 3-3. 보상 미리보기

#### RewardPreview
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/RewardPreview
드래그 앤 드롭 → FusionUI.rewardPreview
```
**역할**: 합성 결과 아이템 **미리보기** 표시  
**사용 프리팹**: LobbyInventorySlot  
**표시 내용**: 합성 후 획득할 아이템 슬롯 (최대 10개)

#### RewardSlotsContainer
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/RewardPreview/ScrollView/Viewport/Content
드래그 앤 드롭 → FusionUI.rewardSlotsContainer
```
- ⭐ GridLayoutGroup 추가
  - Cell Size: 90x90 (동적으로 변경됨)
  - Spacing: 10x10
  - Constraint: Fixed Column Count = 5
- **동적 생성**: 합성 결과별 LobbyInventorySlot

#### MaterialSlotPrefab
```
드래그 앤 드롭 → FusionUI.materialSlotPrefab
```
**⚠️ 프리팹 지정**: `Assets/Prefabs/UI/LobbyInventorySlot.prefab`  
**역할**: 합성 결과 아이템 슬롯 표시 (InventorySlot 컴포넌트 필요)

---

### 3-4. 실행 버튼

#### FusionButton
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/ButtonArea/FusionButton
드래그 앤 드롭 → FusionUI.fusionButton
```

#### FusionButtonText
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/ButtonArea/FusionButton/Text
드래그 앤 드롭 → FusionUI.fusionButtonText
```
- 기본 텍스트: "아이템 선택"

#### FusionWarningText
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/ButtonArea/WarningText
드래그 앤 드롭 → FusionUI.fusionWarningText
```
- 색상: Red (255, 76, 76)
- 초기 상태: 비활성화

#### CancelButton
```
Hierarchy 경로: WorkshopPanel/RightSection/FusionSubPanel/ButtonArea/CancelButton
드래그 앤 드롭 → FusionUI.cancelButton
```

---

### 3-5. 팝업 참조

#### ConfirmationPopup
```
Hierarchy 경로: Canvas/Popups/ConfirmationPopup
드래그 앤 드롭 → FusionUI.confirmationPopup
```

#### ResultFeedbackPopup
```
Hierarchy 경로: Canvas/Popups/ResultFeedbackPopup
드래그 앤 드롭 → FusionUI.resultFeedbackPopup
```

---

### 3-6. 디버그 설정

#### Show Debug Logs
```
FusionUI.showDebugLogs = true (개발 중)
FusionUI.showDebugLogs = false (릴리즈)
```

---

## 4. WorkshopUI 연동

### 4-1. WorkshopUI 컴포넌트 설정

```
Hierarchy: WorkshopPanel/WorkshopUI (컴포넌트)
```

#### FusionUI 참조
```
드래그 앤 드롭 → WorkshopUI.fusionUI
(FusionSubPanel의 FusionUI 컴포넌트)
```

#### FusionSubPanel 참조
```
드래그 앤 드롭 → WorkshopUI.fusionSubPanel
(Hierarchy: WorkshopPanel/RightSection/FusionSubPanel)
```

---

### 4-2. 탭 버튼 설정

#### FusionTabButton
```
Hierarchy: WorkshopPanel/TopSection/TabButtons/FusionTabButton
드래그 앤 드롭 → WorkshopUI.fusionTabButton
```

#### FusionTabText
```
Hierarchy: WorkshopPanel/TopSection/TabButtons/FusionTabButton/Text
드래그 앤 드롭 → WorkshopUI.fusionTabText
```
- 텍스트: "합성"

---

## 5. 테스트 체크리스트

### 5-1. 기본 기능 테스트

- [ ] **탭 전환**
  - [ ] 강화 → 합성 탭 전환 시 FusionSubPanel 활성화
  - [ ] 합성 → 분해 탭 전환 시 FusionSubPanel 비활성화
  - [ ] 탭 전환 시 선택 초기화됨

- [ ] **아이템 선택**
  - [ ] 아이템 1개 클릭 → 같은 등급만 밝게, 나머지 반투명
  - [ ] 같은 등급 개별 선택 가능
  - [ ] 다른 등급 클릭 → 이전 선택 해제 + 새 등급 선택

- [ ] **등급 일괄 선택**
  - [ ] D등급 일괄 버튼 → 배수만큼 선택 (예: 10개 → 9개)
  - [ ] 강화 +0 우선 선택
  - [ ] 같은 버튼 재클릭 → 토글 (해제)

---

### 5-2. 혼합 등급 선택 테스트

- [ ] **혼합 선택**
  - [ ] D등급 일괄 + C등급 일괄 → 두 등급 동시 선택
  - [ ] 선택된 아이템 그룹핑 표시 (등급별)
  - [ ] 각 등급별 필요 개수 표시

- [ ] **부분 선택 해제**
  - [ ] 개별 아이템 선택 해제 가능
  - [ ] 배수가 맞지 않으면 합성 버튼 비활성화
  - [ ] 경고 메시지 표시

---

### 5-3. 보상 미리보기 테스트

- [ ] **단일 등급**
  - [ ] D등급 3개 선택 → C등급 1개 미리보기
  - [ ] C등급 5개 선택 → B등급 1개 미리보기

- [ ] **혼합 등급**
  - [ ] D등급 6개 + C등급 5개 → C등급 2개 + B등급 1개 미리보기
  - [ ] 최대 10개 제한 확인

- [ ] **슬롯 크기 자동 조절**
  - [ ] 1~3개: 120px
  - [ ] 4~6개: 90px
  - [ ] 7~10개: 75px

---

### 5-4. 합성 실행 테스트

- [ ] **기본 합성**
  - [ ] D등급 3개 합성 → C등급 1개 생성
  - [ ] 골드 소모 정상
  - [ ] 재료 아이템 삭제 정상

- [ ] **강화 아이템 경고**
  - [ ] 강화 +1 이상 포함 시 경고 팝업 표시
  - [ ] "합성 시 강화 수치 초기화" 메시지 확인
  - [ ] 확인 후 합성 진행

- [ ] **결과 팝업**
  - [ ] ResultFeedbackPopup 표시
  - [ ] 합성 횟수 표시 (예: "3회 합성 완료")
  - [ ] 결과 아이템 슬롯 표시 (여러 등급 혼합)
  - [ ] 3초 후 자동 닫기

---

### 5-5. 에러 처리 테스트

- [ ] **조건 불만족**
  - [ ] 필요 개수 부족 시 버튼 비활성화
  - [ ] 경고 메시지 표시
  - [ ] 배수가 맞지 않으면 경고

- [ ] **골드 부족**
  - [ ] FusionSystem.CanFuse() 실패
  - [ ] 에러 메시지 표시

- [ ] **장착 아이템**
  - [ ] 장착 중인 아이템 합성 시도 → 실패
  - [ ] "장착 중인 아이템은 합성할 수 없습니다" 메시지

---

## 6. 주의사항

### 6-1. Phase 0 교훈 적용

**⚠️ Unity UI Layout 버그 대응**:
```csharp
// Panel 활성화 후 1프레임 대기
if (!panel.activeSelf) {
    panel.SetActive(true);
    StartCoroutine(UpdateUIDelayed());
    return;
}

// Instantiate 후 1프레임 대기
StartCoroutine(SetupSlotDelayed(slotObj));
```

### 6-2. 데이터 무결성

**FusionRule 필수**:
- 위치: `Resources/Data/FusionRule.asset`
- 없으면 FusionUI 작동 안 함!

**ItemTemplateResolver 사용**:
```csharp
// ❌ Resources.Load<EquipmentData>() 직접 사용 금지
// ✅ ItemTemplateResolver.Load(templateName) 사용
```

### 6-3. 골드 차감

**PlayerDataManager.SpendGold() 필수**:
```csharp
// ❌ AccountDataManager.SpendGold() 직접 호출 금지
// ✅ PlayerDataManager.SpendGold() 사용
```

---

## 7. 트러블슈팅

### 문제 1: "FusionRule을 찾을 수 없습니다"
**원인**: Resources/Data/FusionRule.asset 누락  
**해결**: ScriptableObject 생성 (Create → Systems → FusionRule)

### 문제 2: 이미지가 표시되지 않음
**원인**: Panel 활성화/Instantiate 직후 Image 설정  
**해결**: 1프레임 대기 후 설정 (Phase 0 교훈)

### 문제 3: 등급 일괄 선택 시 개수가 맞지 않음
**원인**: 배수 계산 로직 누락  
**해결**: WorkshopInventoryUI.SelectAllByGrade() 확인

### 문제 4: 골드 UI가 갱신되지 않음
**원인**: PlayerDataManager.OnGoldChanged 이벤트 미구독  
**해결**: WorkshopInventoryUI.OnEnable()에서 이벤트 구독 확인

---

## 8. 완료 확인

### 최종 체크리스트

- [ ] GradeGroupPrefab 생성 및 저장
- [ ] FusionUI 모든 SerializeField 연결
- [ ] WorkshopUI에 FusionUI 연동
- [ ] 탭 전환 정상 작동
- [ ] 배수 선택 로직 작동
- [ ] 혼합 등급 합성 작동
- [ ] 강화 경고 팝업 작동
- [ ] 결과 팝업 표시 정상
- [ ] 골드 실시간 갱신
- [ ] 인벤토리 UI 즉시 갱신

**✅ 모든 항목 체크 완료 시 FusionUI 구현 완료!**

---

## 9. 참고 자료

### 관련 파일
- `Assets/Scripts/UI/Workshop/FusionUI.cs`
- `Assets/Scripts/UI/Workshop/WorkshopUI.cs`
- `Assets/Scripts/UI/Workshop/WorkshopInventoryUI.cs`
- `Assets/Scripts/UI/Workshop/ResultFeedbackPopup.cs`
- `Assets/Scripts/Systems/FusionSystem.cs`

### 메모리 참고
- [[memory:14341626]] - 골드 차감 패턴
- [[memory:14333684]] - Phase 0 Image 버그 교훈
- [[memory:14280973]] - 골드 체크 방식
- [[memory:14280755]] - ItemTemplateResolver 사용

---

**📅 작성일**: 2026-02-11  
**📝 버전**: 1.0  
**👤 작성자**: AI Assistant

