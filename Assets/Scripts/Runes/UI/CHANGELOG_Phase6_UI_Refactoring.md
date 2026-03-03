# 📚 CHANGELOG: Phase 6 UI Refactoring

**작성일**: 2026-02-28  
**Phase**: Phase 6 - UI 리팩토링  
**작성자**: AI Assistant

---

## 📝 개요

Phase 6에서는 **복잡한 가챠/재료 선택 UI를 폐기**하고, **스킬북 형태의 단일 패널 구조**로 전면 리팩토링했습니다.

### 🎯 핵심 변경 사항

1. **단일 패널 구조 (좌측: 목록 / 우측: 상세)**
2. **컨텍스트 기반 액션 버튼** (미보유→해금 / 만렙미달→강화 / 만렙→한계돌파)
3. **미보유 룬 시각화** (흑백 아이콘, 레벨 숨김)
4. **룬 파편 실시간 표시**

---

## 🆕 신규 추가된 파일

### 1. **RunePanelUI.cs** (메인 컨트롤러)
**경로**: `Assets/Scripts/Runes/UI/RunePanelUI.cs`

**주요 기능**:
- 전체 룬 UI 관리 (좌측 목록 + 우측 상세 패널)
- Resources/Runes 폴더에서 모든 RuneData 로드
- 보유/미보유 룬 구분하여 슬롯 생성
- 상단 룬 파편 개수 실시간 갱신

**주요 메서드**:
```csharp
public void RefreshUI()                              // 전체 UI 갱신
private void LoadAllRuneData()                        // 전체 룬 데이터 로드
private void CreateOwnedRuneSlot(RuneInstance)       // 보유 룬 슬롯 생성
private void CreateLockedRuneSlot(RuneData)          // 미보유 룬 슬롯 생성
private void OnRuneSlotClicked(RuneInstance)         // 보유 룬 선택
private void OnLockedRuneSlotClicked(RuneData)       // 미보유 룬 선택
```

**이벤트 구독**:
- `RuneInventoryManager.OnInventoryChanged` → `RefreshUI()` 호출

---

### 2. **RuneDetailUI.cs** (상세 패널 + 액션)
**경로**: `Assets/Scripts/Runes/UI/RuneDetailUI.cs`

**주요 기능**:
- 선택한 룬의 상세 정보 표시 (스탯, 부옵션, 조건)
- 컨텍스트 기반 액션 버튼 제공
- 해금/강화/한계돌파 실행

**주요 메서드**:
```csharp
public void ShowDetail(RuneInstance)                 // 보유 룬 상세 표시
public void ShowDetail(RuneData)                     // 미보유 룬 상세 표시
public void Hide()                                   // 패널 숨김
private ActionType DetermineActionType()             // 액션 타입 결정
private void OnActionButtonClicked()                 // 액션 실행
private bool ExecuteUnlock(out string message)       // 룬 해금
private bool ExecuteLevelUp(out string message)      // 레벨업
private bool ExecuteLimitBreak(out string message)   // 한계돌파
```

**액션 타입**:
```csharp
enum ActionType
{
    None,           // 액션 없음
    Unlock,         // 해금 (파편 100개)
    LevelUp,        // 레벨업 (파편 10개)
    LimitBreak,     // 한계돌파 (파편 200개)
    MaxLevel        // 최고 레벨 도달 (비활성화)
}
```

---

### 3. **Phase6_UI_Setup_Guide.md** (설정 가이드)
**경로**: `Assets/Scripts/Runes/UI/Phase6_UI_Setup_Guide.md`

**내용**:
- Unity Editor 설정 방법
- RunePanelUI Inspector 설정
- RuneDetailUI Inspector 설정
- RuneSlotPrefab 수정 사항
- 테스트 방법 (Editor 테스트 + 플레이 모드 테스트)
- UI/UX 개선 사항
- 트러블슈팅

---

### 4. **CHANGELOG_Phase6_UI_Refactoring.md** (변경 사항 문서)
**경로**: `Assets/Scripts/Runes/UI/CHANGELOG_Phase6_UI_Refactoring.md`

**내용**: (이 문서)

---

## 🔧 수정된 파일

### 1. **RuneSlotUI.cs** (버그 수정)
**경로**: `Assets/Scripts/Runes/UI/RuneSlotUI.cs`

**변경 사항**:
- ✅ `RequireComponent(typeof(Button))` 추가로 Button 누락 경고 해결
- 기존 Phase 6 기능 유지 (보유/미보유 상태 시각화)

**주요 메서드**:
```csharp
public void Setup(RuneInstance, Action<RuneInstance>)       // 보유 룬 슬롯 초기화
public void SetupAsLocked(RuneData, Action<RuneData>)       // 미보유 룬 슬롯 초기화 (흑백)
public void SetSelected(bool)                                // 선택 상태 설정
public void SetEquipped(bool)                                // 장착 상태 설정
```

---

## 🚫 Deprecated 파일 (더 이상 사용하지 않음)

Phase 6에서는 다음 파일들이 더 이상 사용되지 않습니다.  
각 파일에 `[System.Obsolete]` 속성과 경고 주석이 추가되었습니다.

### 1. **RuneInventoryUI.cs** → `RunePanelUI.cs`로 대체
**경로**: `Assets/Scripts/Runes/UI/RuneInventoryUI.cs`

**Deprecated 이유**:
- 기존: 분리된 Inventory + Enhance + Tooltip
- 신규: 통합된 RunePanelUI + RuneDetailUI

**추가된 경고**:
```csharp
[System.Obsolete("Phase 6에서 RunePanelUI로 대체됨. 이 클래스는 더 이상 사용되지 않습니다.", false)]
```

---

### 2. **RuneEnhanceUI.cs** → `RuneDetailUI.cs`로 통합
**경로**: `Assets/Scripts/Runes/UI/RuneEnhanceUI.cs`

**Deprecated 이유**:
- 기존: 분리된 탭 방식 (레벨업 탭 / 한계돌파 탭)
- 신규: 컨텍스트 기반 단일 액션 버튼 (상태에 따라 자동 전환)

**추가된 경고**:
```csharp
[System.Obsolete("Phase 6에서 RuneDetailUI로 대체됨. 이 클래스는 더 이상 사용되지 않습니다.", false)]
```

---

### 3. **RuneDragHandler.cs** → Phase 7에서 버튼 방식으로 재구현 예정
**경로**: `Assets/Scripts/Runes/UI/RuneDragHandler.cs`

**Deprecated 이유**:
- Phase 6에서는 드래그 앤 드롭 방식 제거
- 스킬 시스템처럼 "장착" 버튼 방식으로 재구현 예정 (Phase 7)

**추가된 경고**:
```csharp
[System.Obsolete("Phase 6에서 제거됨. 드래그 앤 드롭은 Phase 7에서 버튼 방식으로 재구현 예정.", false)]
```

---

### 4. **RuneTooltipUI.cs** → `RuneDetailUI.cs`로 통합
**경로**: `Assets/Scripts/Runes/UI/RuneTooltipUI.cs`

**Deprecated 이유**:
- 기존: 분리된 Tooltip (상세 정보만)
- 신규: 통합된 RuneDetailUI (상세 정보 + 액션 버튼)

**추가된 경고**:
```csharp
[System.Obsolete("Phase 6에서 RuneDetailUI로 대체됨. 이 클래스는 더 이상 사용되지 않습니다.", false)]
```

---

### 5. **RuneEquipSlotUI.cs** → Phase 7에서 버튼 방식으로 재구현 예정
**경로**: `Assets/Scripts/Runes/UI/RuneEquipSlotUI.cs`

**Deprecated 이유**:
- Phase 6에서는 드래그 앤 드롭 방식 제거
- 스킬 시스템처럼 "장착" 버튼 방식으로 재구현 예정 (Phase 7)

**추가된 경고**:
```csharp
[System.Obsolete("Phase 6에서 제거됨. 장착은 Phase 7에서 버튼 방식으로 재구현 예정.", false)]
```

---

## 🎨 UI/UX 개선 사항

### 1. 상태 시각화
| 상태 | 아이콘 | 레벨 텍스트 | 잠금 오버레이 | 한계돌파 별 |
|------|--------|-------------|---------------|-------------|
| **미보유** | 흑백 | 숨김 | 표시 | 숨김 |
| **보유** | 컬러 | 표시 | 숨김 | 표시 |
| **선택** | 컬러 | 표시 | 숨김 | 표시 + 테두리 강조 |

---

### 2. 컨텍스트 기반 액션 버튼

| 룬 상태 | 버튼 텍스트 | 소모 파편 | 실행 메서드 |
|---------|-------------|-----------|-------------|
| 미보유 | "해금 (파편 100개)" | 100개 | `ExecuteUnlock()` |
| 보유 & 만렙 미달 | "강화 (파편 10개)" | 10개 | `ExecuteLevelUp()` |
| 보유 & 만렙 도달 | "한계돌파 (파편 200개)" | 200개 | `ExecuteLimitBreak()` |
| 최종 Lv.15 | "최고 레벨 도달" | - | (비활성화) |
| 파편 부족 | "파편 부족 (X/필요량)" | - | (비활성화) |

---

### 3. 실시간 피드백
- ✅ 액션 실행 후 즉각 UI 갱신 (`panelUI.RefreshUI()` 호출)
- ✅ 성공/실패 메시지 2초간 표시 (초록색/빨간색)
- ✅ 파편 개수 실시간 업데이트 (`OnInventoryChanged` 이벤트)

---

## 🧪 테스트 방법

### 1. Editor 테스트 (자동)
**메뉴**: `Tools/Rune System/Phase 5 - UI/Phase 6 - 해금/Test_FullScenarioUnlockAndEnhance`

**테스트 내용**:
1. 데이터 초기화
2. 룬 파편 1000개 지급
3. 룬 해금 (RUNE_EXECUTIONER)
4. 레벨업 (Lv.1 → Lv.10)
5. 한계돌파 (1회)
6. 추가 레벨업 (Lv.10 → Lv.11)

---

### 2. 플레이 모드 테스트 (수동)

#### ✅ 미보유 룬 해금 테스트
1. Lobby 씬 실행
2. SkillBook 패널 열기
3. Rune 탭 클릭
4. 미보유 룬 선택 (흑백 표시 확인)
5. 우측 상세 패널 확인 ("해금 (파편 100개)" 버튼)
6. 해금 버튼 클릭
7. 확인 사항:
   - ✅ 파편 100개 차감
   - ✅ 룬 컬러로 전환
   - ✅ "강화 (파편 10개)" 버튼으로 변경

#### ✅ 레벨업 테스트
1. 보유 룬 선택
2. "강화 (파편 10개)" 버튼 클릭
3. 확인 사항:
   - ✅ 파편 10개 차감
   - ✅ 레벨 증가
   - ✅ 상세 패널 스탯 갱신
   - ✅ 3, 6, 9레벨 시 부옵션 추가

#### ✅ 한계돌파 테스트
1. 보유 룬을 만렙(Lv.10)까지 레벨업
2. "한계돌파 (파편 200개)" 버튼 클릭
3. 확인 사항:
   - ✅ 파편 200개 차감
   - ✅ 한계돌파 +1 증가
   - ✅ 최대 레벨 11로 증가
   - ✅ 별 표시 1개 활성화
   - ✅ "강화 (파편 10개)" 버튼으로 변경

#### ✅ 최고 레벨 도달 테스트
1. 보유 룬을 Lv.15까지 성장
2. 확인 사항:
   - ✅ "최고 레벨 도달" 버튼 (비활성화)
   - ✅ 별 5개 모두 활성화

#### ✅ 파편 부족 테스트
1. 파편을 10개 미만으로 소진
2. 확인 사항:
   - ✅ "파편 부족 (X/10)" 버튼 (비활성화)
   - ✅ 빨간색 에러 메시지 표시

---

## 📊 기술적 세부 사항

### 1. 전체 룬 데이터 로드 방식
```csharp
private void LoadAllRuneData()
{
    allRuneDataList.Clear();
    
    // Resources/Runes 폴더에서 모든 RuneData 로드
    var allRunes = Resources.LoadAll<RuneData>("Runes");
    
    // 정렬 (runeType → runeName)
    allRuneDataList = allRunes
        .OrderBy(r => r.runeType)
        .ThenBy(r => r.runeName)
        .ToList();
}
```

---

### 2. 보유/미보유 구분 로직
```csharp
foreach (var runeData in allRuneDataList)
{
    var ownedRunes = inventoryManager.GetRunesByDataId(runeData.runeId);
    
    if (ownedRunes.Count > 0)
    {
        CreateOwnedRuneSlot(ownedRunes[0]);  // 보유 룬 (컬러)
    }
    else
    {
        CreateLockedRuneSlot(runeData);      // 미보유 룬 (흑백)
    }
}
```

---

### 3. 컨텍스트 액션 결정 로직
```csharp
private ActionType DetermineActionType()
{
    // 1. 미보유 → 해금
    if (!isOwnedRune) return ActionType.Unlock;
    
    // 2. 최종 Lv.15 → 완료
    if (currentLevel >= 15) return ActionType.MaxLevel;
    
    // 3. 만렙 & 한돌 가능 → 한계돌파
    if (currentLevel >= maxLevel && currentLimitBreak < maxLimitBreak)
        return ActionType.LimitBreak;
    
    // 4. 만렙 미달 → 레벨업
    if (currentLevel < maxLevel) return ActionType.LevelUp;
    
    // 5. 그 외 → 완료
    return ActionType.MaxLevel;
}
```

---

### 4. 액션 실행 후 RefreshUI 흐름
```
사용자 클릭 (RuneDetailUI.OnActionButtonClicked)
    ↓
액션 실행 (ExecuteUnlock / ExecuteLevelUp / ExecuteLimitBreak)
    ↓
백엔드 매니저 호출 (enhanceManager.TryUnlockRune 등)
    ↓
성공 시:
    ↓
    RuneDetailUI.RefreshUI() - 상세 패널 갱신
    ↓
    RunePanelUI.RefreshUI() - 전체 UI 갱신
        ↓
        UpdateFragmentDisplay() - 파편 개수 갱신
        ↓
        RefreshRuneList() - 룬 목록 재생성
        ↓
        RefreshDetailPanel() - 상세 패널 동기화
```

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

- [Phase6_UI_Setup_Guide.md](./Phase6_UI_Setup_Guide.md) - Unity Editor 설정 가이드
- [CHANGELOG_Phase6_Backend_Refactoring.md](../CHANGELOG_Phase6_Backend_Refactoring.md) - Phase 6 백엔드 변경 사항
- [SkillBookPanelUI.cs](../../UI/Skills/SkillBookPanelUI.cs) - 스킬 시스템 참고
- [SkillListItemUI.cs](../../UI/Skills/SkillListItemUI.cs) - 스킬 아이템 UI 참고

---

## 📝 변경 사항 요약

### 신규 파일 (4개)
1. `RunePanelUI.cs` - 메인 컨트롤러
2. `RuneDetailUI.cs` - 상세 패널 + 액션
3. `Phase6_UI_Setup_Guide.md` - 설정 가이드
4. `CHANGELOG_Phase6_UI_Refactoring.md` - 변경 사항 문서

### 수정된 파일 (1개)
1. `RuneSlotUI.cs` - Button 컴포넌트 요구 추가

### Deprecated 파일 (5개)
1. `RuneInventoryUI.cs` - RunePanelUI로 대체
2. `RuneEnhanceUI.cs` - RuneDetailUI로 통합
3. `RuneDragHandler.cs` - Phase 7에서 버튼 방식으로 재구현
4. `RuneTooltipUI.cs` - RuneDetailUI로 통합
5. `RuneEquipSlotUI.cs` - Phase 7에서 버튼 방식으로 재구현

---

**✅ Phase 6 UI 리팩토링 완료!**

**다음 작업**: Unity Editor 설정 → 플레이 모드 테스트 → Phase 7 장착 시스템 구현
