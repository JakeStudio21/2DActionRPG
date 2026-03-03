# 📚 Phase 6: 룬 UI 리팩토링 Setup 가이드

## 📝 개요

Phase 6에서는 기존의 복잡한 가챠/재료 선택 UI를 폐기하고, **스킬북 형태의 단일 패널 구조**로 전면 리팩토링합니다.

### 🎯 핵심 변경 사항

1. **단일 패널 구조**
   - 좌측: 룬 목록 (보유/미보유 전체 표시)
   - 우측: 상세 정보 + 컨텍스트 기반 액션 버튼
   - 상단: 룬 파편 보유량 실시간 표시

2. **컨텍스트 기반 액션 버튼**
   - 미보유 → "해금 (파편 100개)"
   - 보유 & 만렙 미달 → "강화 (파편 10개)"
   - 보유 & 만렙 도달 → "한계돌파 (파편 200개)"
   - 최종 Lv.15 → "최고 레벨 도달" (비활성화)

3. **미보유 룬 시각화**
   - 아이콘 흑백 처리
   - 레벨 텍스트 숨김
   - 잠금 오버레이 표시

---

## 🗂️ 새로 추가된 스크립트

### 1. **RunePanelUI.cs** (메인 컨트롤러)
**경로**: `Assets/Scripts/Runes/UI/RunePanelUI.cs`

**역할**:
- 전체 룬 UI 관리
- 좌측 룬 목록 생성 (보유/미보유 전체)
- 우측 상세 패널 연동
- 상단 파편 개수 실시간 갱신

**주요 메서드**:
- `RefreshUI()` - 전체 UI 갱신 (목록 + 상세 패널 + 파편 표시)
- `OnRuneSlotClicked(RuneInstance)` - 보유 룬 선택
- `OnLockedRuneSlotClicked(RuneData)` - 미보유 룬 선택

---

### 2. **RuneDetailUI.cs** (상세 패널 + 액션)
**경로**: `Assets/Scripts/Runes/UI/RuneDetailUI.cs`

**역할**:
- 선택한 룬의 상세 정보 표시
- 컨텍스트 기반 액션 버튼 제공
- 해금/강화/한계돌파 실행

**주요 메서드**:
- `ShowDetail(RuneInstance)` - 보유 룬 상세 정보 표시
- `ShowDetail(RuneData)` - 미보유 룬 상세 정보 표시
- `OnActionButtonClicked()` - 액션 실행 (해금/강화/한계돌파)

---

### 3. **RuneSlotUI.cs** (목록 아이템)
**경로**: `Assets/Scripts/Runes/UI/RuneSlotUI.cs`

**역할**:
- 개별 룬 슬롯 UI 표시
- 보유/미보유 상태 시각화
- 클릭 시 상세 패널 표시

**주요 메서드**:
- `Setup(RuneInstance, callback)` - 보유 룬 슬롯 초기화
- `SetupAsLocked(RuneData, callback)` - 미보유 룬 슬롯 초기화 (흑백)
- `SetSelected(bool)` - 선택 상태 설정

**버그 수정**:
- ✅ `RequireComponent(typeof(Button))` 추가로 Button 누락 경고 해결

---

## 🛠️ Unity Editor 설정

### Step 1: RunePanelUI 게임 오브젝트 생성

1. **Canvas > SkillBookPanel > RuneSubPanel** 아래에 새로운 GameObject 생성
   - 이름: `RunePanelUI`

2. **RunePanelUI 컴포넌트 추가**
   - `Add Component` → `RunePanelUI`

3. **UI 구조 설정**

```
RunePanelUI (Canvas, RectTransform)
├── Header (상단 영역)
│   ├── TitleText (TextMeshProUGUI) - "룬 시스템"
│   └── FragmentText (TextMeshProUGUI) - "룬 파편: 1000개"
├── LeftPanel (좌측 룬 목록)
│   └── ScrollView
│       └── Viewport
│           └── Content (Vertical Layout Group)
│               └── (RuneSlotPrefab 동적 생성)
└── RightPanel (우측 상세 패널)
    └── RuneDetailUI
        ├── BasicInfo (아이콘, 이름, 레벨)
        ├── MainStat (주 효과)
        ├── SubStats (부옵션 컨테이너)
        ├── Condition (조건)
        ├── LimitBreakInfo (한계돌파 정보)
        └── ActionButton (컨텍스트 액션 버튼)
```

---

### Step 2: RunePanelUI Inspector 설정

#### **=== 상단: 재화 표시 ===**
- `Rune Fragment Text`: FragmentText (TextMeshProUGUI)
- `Title Text`: TitleText (TextMeshProUGUI)

#### **=== 좌측: 룬 목록 ===**
- `Rune List Container`: ScrollView > Viewport > Content
- `Rune Slot Prefab`: `Assets/Prefabs/UI/Rune/RuneSlotPrefab.prefab`
- `Scroll Rect`: ScrollView (ScrollRect)

#### **=== 우측: 상세 정보 패널 ===**
- `Detail Panel`: RightPanel > RuneDetailUI (RuneDetailUI)

#### **=== 디버그 ===**
- `Show Debug Logs`: ☑️ (테스트 시)

---

### Step 3: RuneDetailUI Inspector 설정

#### **=== 기본 정보 ===**
- `Rune Icon Image`: BasicInfo > Icon (Image)
- `Rune Name Text`: BasicInfo > NameText (TextMeshProUGUI)
- `Rune Level Text`: BasicInfo > LevelText (TextMeshProUGUI)
- `Rune Type Text`: BasicInfo > TypeText (TextMeshProUGUI)

#### **=== 주 효과 ===**
- `Main Stat Text`: MainStat > StatText (TextMeshProUGUI)
- `Main Stat Scaling Text`: MainStat > ScalingText (TextMeshProUGUI)

#### **=== 부옵션 ===**
- `Sub Stat Container`: SubStats > Container (Transform)
- `Sub Stat Row Prefab`: `Assets/Prefabs/UI/Rune/SubStatRowPrefab.prefab`
- `Sub Stat Milestone Text`: SubStats > MilestoneText (TextMeshProUGUI)

#### **=== 조건 ===**
- `Condition Text`: Condition > ConditionText (TextMeshProUGUI)

#### **=== 한계돌파 정보 ===**
- `Limit Break Text`: LimitBreakInfo > LimitBreakText (TextMeshProUGUI)
- `Max Level Text`: LimitBreakInfo > MaxLevelText (TextMeshProUGUI)

#### **=== 컨텍스트 액션 버튼 ===**
- `Action Button`: ActionButton (Button)
- `Action Button Text`: ActionButton > Text (TextMeshProUGUI)
- `Action Message Text`: ActionMessage (TextMeshProUGUI)

---

### Step 4: RuneSlotPrefab 수정

기존 `RuneSlotPrefab.prefab`를 다음과 같이 수정:

1. **Button 컴포넌트 필수 추가**
   - `Add Component` → `Button`
   - ⚠️ `RequireComponent(typeof(Button))`로 보장됨

2. **잠금 오버레이 추가**
   - `LockOverlay` (GameObject)
   - `LockIcon` (Image) - 자물쇠 아이콘

3. **한계돌파 별 표시**
   - `LimitBreakStars` (Image 배열, 최대 5개)

4. **Inspector 설정**
   - `Background Image`: Background (Image)
   - `Rune Icon Image`: Icon (Image)
   - `Rune Name Text`: NameText (TextMeshProUGUI)
   - `Level Text`: LevelText (TextMeshProUGUI)
   - `Selected Border`: SelectedBorder (GameObject)
   - `Lock Overlay`: LockOverlay (GameObject)
   - `Lock Icon`: LockIcon (Image)
   - `Limit Break Stars`: Star1~Star5 (Image[])

---

## 🧪 테스트 방법

### 1. Editor 테스트 (자동)

#### 테스트 메뉴:
- `Tools/Rune System/Phase 5 - UI/Phase 6 - 해금/Test_FullScenarioUnlockAndEnhance`

#### 테스트 내용:
1. 데이터 초기화
2. 룬 파편 1000개 지급
3. 룬 해금 (RUNE_EXECUTIONER)
4. 레벨업 (Lv.1 → Lv.10)
5. 한계돌파 (1회)
6. 추가 레벨업 (Lv.10 → Lv.11)

---

### 2. 플레이 모드 테스트 (수동)

#### 테스트 시나리오:

##### ✅ **미보유 룬 해금 테스트**
1. Lobby 씬 실행
2. SkillBook 패널 열기
3. Rune 탭 클릭
4. 미보유 룬 선택 (흑백 표시)
5. 우측 상세 패널 확인:
   - 아이콘 흑백
   - "해금 (파편 100개)" 버튼
6. 해금 버튼 클릭
7. 확인 사항:
   - ✅ 파편 100개 차감
   - ✅ 룬 컬러로 전환
   - ✅ "강화 (파편 10개)" 버튼으로 변경

---

##### ✅ **레벨업 테스트**
1. 보유 룬 선택
2. "강화 (파편 10개)" 버튼 클릭
3. 확인 사항:
   - ✅ 파편 10개 차감
   - ✅ 레벨 증가
   - ✅ 상세 패널 스탯 갱신
   - ✅ 3, 6, 9레벨 시 부옵션 추가

---

##### ✅ **한계돌파 테스트**
1. 보유 룬을 만렙(Lv.10)까지 레벨업
2. 확인 사항:
   - ✅ "한계돌파 (파편 200개)" 버튼으로 변경
3. 한계돌파 버튼 클릭
4. 확인 사항:
   - ✅ 파편 200개 차감
   - ✅ 한계돌파 +1 증가
   - ✅ 최대 레벨 11로 증가
   - ✅ 별 표시 1개 활성화
   - ✅ "강화 (파편 10개)" 버튼으로 변경

---

##### ✅ **최고 레벨 도달 테스트**
1. 보유 룬을 Lv.15까지 성장
2. 확인 사항:
   - ✅ "최고 레벨 도달" 버튼 (비활성화)
   - ✅ 별 5개 모두 활성화

---

##### ✅ **파편 부족 테스트**
1. 파편을 10개 미만으로 소진
2. 확인 사항:
   - ✅ "파편 부족 (X/10)" 버튼 (비활성화)
   - ✅ 빨간색 에러 메시지 표시

---

## 🎨 UI/UX 개선 사항

### 1. **상태 시각화**
- **미보유 룬**: 흑백 아이콘, 레벨 숨김, 잠금 오버레이
- **보유 룬**: 컬러 아이콘, 레벨 표시, 한계돌파 별
- **선택 룬**: 테두리 강조

### 2. **실시간 피드백**
- 액션 실행 후 즉각 UI 갱신
- 성공/실패 메시지 2초간 표시
- 파편 개수 실시간 업데이트

### 3. **컨텍스트 기반 UI**
- 룬 상태에 따라 버튼 텍스트 자동 변경
- 파편 부족 시 필요량 표시
- 조건부 버튼 활성화/비활성화

---

## 🚫 Deprecated 파일 (더 이상 사용하지 않음)

Phase 6에서는 다음 파일들이 더 이상 사용되지 않습니다:

1. **RuneInventoryUI.cs** (Deprecated)
   - → `RunePanelUI.cs`로 대체

2. **RuneEnhanceUI.cs** (Deprecated)
   - → `RuneDetailUI.cs`로 통합

3. **RuneDragHandler.cs** (Deprecated)
   - → 드래그 앤 드롭 장착은 다음 단계에서 재구현 예정

4. **RuneTooltipUI.cs** (Deprecated)
   - → `RuneDetailUI.cs`로 통합

⚠️ **주의**: 기존 파일들은 삭제하지 말고 참고용으로 보관할 것

---

## 📊 체크리스트

### ✅ 스크립트 작성
- [x] RunePanelUI.cs 작성
- [x] RuneDetailUI.cs 작성
- [x] RuneSlotUI.cs 리팩토링 (RequireComponent 추가)

### ✅ Unity Editor 설정
- [ ] RunePanelUI GameObject 생성
- [ ] RunePanelUI Inspector 설정
- [ ] RuneDetailUI Inspector 설정
- [ ] RuneSlotPrefab 수정 (Button, LockOverlay, LimitBreakStars)

### ✅ 테스트
- [ ] Editor 테스트 실행 (Test_FullScenarioUnlockAndEnhance)
- [ ] 플레이 모드 테스트
  - [ ] 미보유 룬 해금
  - [ ] 레벨업 (부옵션 개방 확인)
  - [ ] 한계돌파 (별 표시 확인)
  - [ ] 최고 레벨 도달
  - [ ] 파편 부족 케이스

---

## 🐛 트러블슈팅

### 문제 1: "Button 컴포넌트가 없습니다" 경고
**해결**: `RuneSlotUI.cs`에 `RequireComponent(typeof(Button))` 추가됨 (✅ 완료)

### 문제 2: 미보유 룬이 목록에 표시되지 않음
**원인**: Resources/Runes 폴더에 RuneData가 없음
**해결**: `Resources/Runes/` 폴더에 모든 RuneData 에셋 배치

### 문제 3: 액션 버튼 클릭 후 UI가 갱신되지 않음
**원인**: RefreshUI() 호출 누락
**해결**: 각 액션 실행 후 `panelUI.RefreshUI()` 호출 확인

### 문제 4: 파편 개수가 갱신되지 않음
**원인**: OnInventoryChanged 이벤트 미구독
**해결**: `OnEnable()`에서 이벤트 구독 확인

---

## 🎯 다음 단계 (Phase 7 예정)

1. **룬 장착 시스템 재구현**
   - 스킬처럼 "장착" 버튼 방식으로 구현
   - 5개 슬롯 (Attack1, Attack2, Survival1, Survival2, Utility)

2. **룬 정렬/필터링**
   - 타입별 필터
   - 레벨순/이름순 정렬

3. **애니메이션 추가**
   - 해금 연출
   - 레벨업 이펙트
   - 한계돌파 이펙트

4. **실제 재화 시스템 연동**
   - `currentRuneFragments` → CurrencyManager 연동

---

## 📚 참고 문서

- [CHANGELOG_Phase6_Backend_Refactoring.md](../CHANGELOG_Phase6_Backend_Refactoring.md)
- [SkillBookPanelUI.cs](../../UI/Skills/SkillBookPanelUI.cs) - 스킬 시스템 참고
- [SkillListItemUI.cs](../../UI/Skills/SkillListItemUI.cs) - 스킬 아이템 UI 참고

---

**작성일**: 2026-02-28  
**작성자**: AI Assistant  
**버전**: Phase 6 - UI Refactoring
