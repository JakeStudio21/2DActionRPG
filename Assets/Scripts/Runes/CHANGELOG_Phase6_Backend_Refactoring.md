# Phase 6: 백엔드 리팩토링 - 룬 파편 시스템

**날짜**: 2026-02-28  
**버전**: Phase 6 Backend Refactoring  
**작성자**: AI Assistant

---

## 📋 기획 변경 요약

### 이전 시스템 (Phase 1~5)
- ❌ **가챠(뽑기) 방식**: 룬을 랜덤으로 획득
- ❌ **중복 룬 개념**: 동일한 룬을 여러 개 보유 가능
- ❌ **재료 룬 소모**: 한계돌파 시 동일 종류의 1레벨 룬을 재료로 소모

### 새로운 시스템 (Phase 6)
- ✅ **확정 해금 방식**: 스킬 시스템처럼 룬을 확정적으로 해금
- ✅ **단일 룬 보유**: 각 룬 종류당 1개만 보유 (중복 제거)
- ✅ **룬 파편 재화**: 해금/레벨업/한계돌파 모두 룬 파편(Rune Fragment) 소모

---

## 🔧 변경된 코드

### 1. RuneInventoryManager.cs

#### 추가된 필드
```csharp
/// <summary>
/// 룬 파편 (Rune Fragment) - 룬 해금 및 한계돌파에 사용되는 전용 재화
/// ⚠️ TODO: 추후 실제 재화 시스템(CurrencyManager 등)과 연동 필요
/// ⚠️ 현재는 테스트를 위한 임시 변수
/// </summary>
[Header("재화 시스템 (임시)")]
[Tooltip("룬 해금 및 한계돌파에 사용되는 룬 파편 (테스트용)")]
public int currentRuneFragments = 1000;
```

**설명:**
- 테스트를 위해 기본값 1000개로 설정
- Inspector에서 수정 가능
- 추후 실제 재화 시스템과 연동 필요

---

### 2. RuneEnhanceManager.cs

#### 2.1 재화 비용 상수 추가
```csharp
/// <summary>
/// 룬 해금 비용 (룬 파편)
/// </summary>
private const int UNLOCK_COST = 100;

/// <summary>
/// 레벨업 비용 (룬 파편)
/// </summary>
private const int LEVELUP_COST = 10;

/// <summary>
/// 한계돌파 비용 (룬 파편)
/// </summary>
private const int LIMIT_BREAK_COST = 200;
```

**비용 설정:**
- 해금: 100 파편
- 레벨업: 10 파편
- 한계돌파: 200 파편

---

#### 2.2 룬 해금 시스템 추가

**새로운 메서드:**
```csharp
public UnlockResult TryUnlockRune(string runeId)
```

**로직:**
1. RuneData 유효성 검증
2. 이미 보유 중인지 확인 (종류별 1개만 허용)
3. 파편 100개 소지 확인
4. 파편 차감
5. Lv.1 룬 생성 (RuneInventoryManager.AddRune 호출)

**예시:**
```csharp
var result = enhanceManager.TryUnlockRune("RUNE_BOSS_HUNTER");

if (result == UnlockResult.Success)
{
    // 해금 성공: 파편 -100, Lv.1 룬 생성
}
```

---

#### 2.3 레벨업 로직 수정

**변경 사항:**
- 파편 10개 소모 로직 추가
- 기존 스탯 계산 로직은 그대로 유지

**Before:**
```csharp
// [TODO] 재화 소모 로직 (골드, 강화석 등)
// if (!HasEnoughResources())
// {
//     return LevelUpResult.InsufficientGold;
// }
```

**After:**
```csharp
// 파편 소지 확인
var inventoryManager = RuneInventoryManager.Instance;
if (inventoryManager.currentRuneFragments < LEVELUP_COST)
{
    return LevelUpResult.InsufficientGold;
}

// 파편 차감
inventoryManager.currentRuneFragments -= LEVELUP_COST;
```

---

#### 2.4 한계돌파 로직 전면 수정

**API 변경:**
```csharp
// Before (Phase 5)
public LimitBreakResult TryLimitBreak(string baseRuneUID, string materialRuneUID)

// After (Phase 6)
public LimitBreakResult TryLimitBreak(string baseRuneUID)
```

**제거된 로직:**
- ❌ 재료 룬 찾기
- ❌ 재료 룬 검증 (동일 UID 체크, runeId 일치 체크, 잠금 체크)
- ❌ 재료 룬 삭제 (RuneInventoryManager.RemoveRune)

**추가된 로직:**
- ✅ 파편 200개 소지 확인
- ✅ 파편 차감

**예시:**
```csharp
// Before (Phase 5)
var materialRune = inventoryManager.AddRune(baseRune.baseData);
var result = enhanceManager.TryLimitBreak(baseRune.instanceUID, materialRune.instanceUID);

// After (Phase 6)
var result = enhanceManager.TryLimitBreak(baseRune.instanceUID);
```

---

### 3. RunePhase5TestMenu.cs (테스트 코드)

#### 3.1 CreateTestRune 수정

**변경 사항:**
- 재료 룬 생성 로직 제거
- 파편 소모 방식으로 변경

**Before:**
```csharp
// 한계돌파용 재료 룬 생성
var materialRune = inventoryManager.AddRune(materialData);
enhanceManager.TryLimitBreak(rune.instanceUID, materialRune.instanceUID);
```

**After:**
```csharp
// 한계돌파 (파편 소모 방식)
enhanceManager.TryLimitBreak(rune.instanceUID);
```

---

#### 3.2 새로운 테스트 메뉴 추가

**Phase 6 - 해금 메뉴:**
```
Tools > Rune System > Phase 5 - UI > Phase 6 - 해금/
  1. 🔓 룬 해금 테스트
  2. 💎 룬 파편 초기화 (1000개)
  3. 🚀 전체 시나리오 (해금→강화)
```

**테스트 시나리오:**
1. 데이터 초기화
2. 룬 파편 1000개 지급
3. 룬 해금 (파편 -100)
4. 레벨업 (Lv.1 → Lv.10)
5. 한계돌파 (파편 -200)
6. UI 갱신

---

## 🎯 사용 예시

### 1. 룬 해금
```csharp
var enhanceManager = RuneEnhanceManager.Instance;
var result = enhanceManager.TryUnlockRune("RUNE_BOSS_HUNTER");

switch (result)
{
    case UnlockResult.Success:
        Debug.Log("✅ 해금 성공! 파편 -100");
        break;
    case UnlockResult.AlreadyUnlocked:
        Debug.Log("⚠️ 이미 보유 중입니다.");
        break;
    case UnlockResult.InsufficientFragments:
        Debug.Log("❌ 파편이 부족합니다.");
        break;
}
```

### 2. 레벨업
```csharp
var result = enhanceManager.TryLevelUp(runeUID);

// 파편 10개 자동 차감
// Lv.3, 6, 9에서 부옵션 자동 추첨 (기존 로직 유지)
```

### 3. 한계돌파
```csharp
// 재료 룬 불필요! 파편만 있으면 OK
var result = enhanceManager.TryLimitBreak(runeUID);

// 파편 200개 자동 차감
// 최대 레벨 10 → 11 확장
```

---

## ⚠️ 주의사항

### 1. 호환성
- `LimitBreakResult` enum의 Deprecated 값들은 호환성을 위해 유지
- UI 코드가 기존 enum을 참조하는 경우 에러 방지

### 2. 파편 관리
- 현재는 `RuneInventoryManager.currentRuneFragments` 변수로 임시 관리
- 추후 실제 재화 시스템과 연동 필요
- 세이브/로드 시스템에 파편도 포함해야 함

### 3. 기존 세이브 데이터
- Phase 1~5 세이브 데이터와 호환되지 않음
- 테스트 시 `Test_ClearAllData()` 먼저 실행 권장

---

## 🧪 테스트 방법

### 1. 에디터 테스트 (추천)
```
Unity 메뉴 > Tools > Rune System > Phase 5 - UI > Phase 6 - 해금/
  → 3. 🚀 전체 시나리오 (해금→강화) 실행
```

**예상 결과:**
- ✅ 파편 초기화 (1000개)
- ✅ 룬 해금 성공 (-100 파편)
- ✅ 레벨업 성공 (-10 파편 × N회)
- ✅ 한계돌파 성공 (-200 파편)

### 2. 런타임 테스트
```csharp
// 1. 파편 확인
Debug.Log($"현재 파편: {inventoryManager.currentRuneFragments}개");

// 2. 룬 해금
var result = enhanceManager.TryUnlockRune("RUNE_BOSS_HUNTER");

// 3. 강화
var rune = inventoryManager.GetAllRunes()[0];
enhanceManager.TryLevelUp(rune.instanceUID);

// 4. 한계돌파
enhanceManager.TryLimitBreak(rune.instanceUID);
```

---

## 📊 파편 소모량 계산

### 예시: 1개 룬을 Lv.15 (한돌 +5)까지 성장

| 단계 | 레벨 | 파편 소모 | 누적 소모 |
|------|------|-----------|-----------|
| 해금 | - | 100 | 100 |
| 레벨업 | Lv.1 → Lv.10 | 10 × 9 = 90 | 190 |
| 한계돌파 1 | 최대 Lv.10 → 11 | 200 | 390 |
| 레벨업 | Lv.10 → Lv.11 | 10 | 400 |
| 한계돌파 2 | 최대 Lv.11 → 12 | 200 | 600 |
| 레벨업 | Lv.11 → Lv.12 | 10 | 610 |
| 한계돌파 3 | 최대 Lv.12 → 13 | 200 | 810 |
| 레벨업 | Lv.12 → Lv.13 | 10 | 820 |
| 한계돌파 4 | 최대 Lv.13 → 14 | 200 | 1020 |
| 레벨업 | Lv.13 → Lv.14 | 10 | 1030 |
| 한계돌파 5 | 최대 Lv.14 → 15 | 200 | 1230 |
| 레벨업 | Lv.14 → Lv.15 | 10 | 1240 |

**총 소모량: 1240 파편**

---

## 🚀 다음 단계

### Phase 7: UI 리팩토링 (예정)
- 룬 해금 UI 추가
- 파편 표시 UI
- 단일 패널 구조로 통합
- 스킬 시스템과 유사한 UX

### 추후 작업
- 실제 재화 시스템 연동
- 세이브/로드에 파편 포함
- 파편 획득 방법 구현 (던전 보상, 분해 등)

---

**변경 완료일**: 2026-02-28  
**테스트 상태**: ✅ 에디터 테스트 완료
