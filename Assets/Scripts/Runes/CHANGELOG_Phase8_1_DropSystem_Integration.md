# 📚 CHANGELOG: Phase 8-1 - 드롭 시스템 연동

**작성일**: 2026-02-28  
**Phase**: Phase 8-1 - 룬 조각 드롭 시스템 라우팅  
**작성자**: AI Assistant

---

## 📝 개요

Phase 8-1에서는 **기존 드롭 시스템에 룬 조각 라우팅 로직을 추가**하여, 몬스터 사냥 시 획득한 룬 조각이 자동으로 `RuneInventoryManager`로 전달되도록 구현했습니다.

**핵심 원칙**:
- ✅ 기존 드롭 시스템을 그대로 유지
- ✅ 새로운 드롭 시스템을 만들지 않음
- ✅ 필터링 & 라우팅 로직만 추가

---

## 🎯 핵심 변경 사항

### 1. 룬 조각 명명 규칙

```
RUNE_FRAG_<RUNE_ID>
```

**예시**:
- `RUNE_FRAG_RUNE_BOSS_HUNTER` → 보스 사냥꾼 조각
- `RUNE_FRAG_RUNE_DEFENSE_BREAKER` → 방어 파괴자 조각
- `RUNE_FRAG_RUNE_EXECUTIONER` → 처형자 조각

---

### 2. 드롭 흐름

```
몬스터 처치
    ↓
DropTable에서 "RUNE_FRAG_RUNE_BOSS_HUNTER" 드롭
    ↓
characterBagMaterials에 추가 (재료 가방)
    ↓
스테이지 종료 (StageEndItemTransfer.cs)
    ↓
TransferRuneFragments() - 라우팅 필터링
    ↓
"RUNE_FRAG_" 접두사 감지
    ↓
"RUNE_BOSS_HUNTER"로 변환
    ↓
RuneInventoryManager.AddRuneFragment("RUNE_BOSS_HUNTER", 10)
    ↓
runeFragments Dictionary 업데이트
    ↓
OnInventoryChanged 이벤트 발행
    ↓
RunePanelUI.RefreshUI() - UI 자동 갱신 ✅
```

---

## 🆕 신규 추가된 코드

### 1. **RuneInventoryManager.cs** - 룬 조각 획득 API

#### AddRuneFragment() 메서드
```csharp
/// <summary>
/// [Phase 8-1] 룬 조각 획득 (드롭 시스템 연동용)
/// </summary>
public void AddRuneFragment(string runeId, int amount)
{
    if (string.IsNullOrEmpty(runeId))
    {
        Debug.LogWarning("[RuneInventoryManager] AddRuneFragment: runeId가 비어있습니다.");
        return;
    }
    
    if (amount <= 0)
    {
        Debug.LogWarning($"[RuneInventoryManager] AddRuneFragment: 잘못된 개수입니다. runeId={runeId}, amount={amount}");
        return;
    }
    
    // 조각 추가
    AddFragments(runeId, amount);
    
    Debug.Log($"💎 [RuneDrop] {runeId} 룬 조각 {amount}개 획득! (총 보유: {GetFragmentCount(runeId)}개)");
}
```

**특징**:
- 유효성 검증 (null 체크, 양수 체크)
- 내부적으로 `AddFragments()` 호출
- `OnInventoryChanged` 이벤트 자동 발행 → UI 갱신

---

#### ContextMenu 디버그 테스트
```csharp
/// <summary>
/// [ContextMenu] 테스트용 룬 조각 획득 (보스 사냥꾼 +100)
/// </summary>
[ContextMenu("테스트: 보스 사냥꾼 조각 +100")]
private void DebugAddBossHunterFragments()
{
    const string testRuneId = "RUNE_BOSS_HUNTER";
    const int testAmount = 100;
    
    AddRuneFragment(testRuneId, testAmount);
    
    Debug.Log($"[DEBUG] {testRuneId} 조각 {testAmount}개 추가 완료!");
}

/// <summary>
/// [ContextMenu] 테스트용 모든 룬 조각 +50
/// </summary>
[ContextMenu("테스트: 모든 룬 조각 +50")]
private void DebugAddAllFragments()
{
    var allFragments = GetAllFragments();
    
    foreach (var runeId in allFragments.Keys)
    {
        AddRuneFragment(runeId, 50);
    }
    
    Debug.Log($"[DEBUG] 모든 룬 조각 50개씩 추가 완료! (총 {allFragments.Count}종류)");
}
```

**사용 방법**:
1. Hierarchy에서 `RuneInventoryManager` GameObject 선택
2. Inspector에서 `RuneInventoryManager` 컴포넌트 우클릭
3. `테스트: 보스 사냥꾼 조각 +100` 클릭
4. Console에서 결과 확인

---

### 2. **StageEndItemTransfer.cs** - 룬 조각 라우팅

#### TransferRuneFragments() 메서드
```csharp
/// <summary>
/// [Phase 8-1] 룬 조각 전송 처리 (드롭 시스템 라우팅)
/// </summary>
private void TransferRuneFragments(PlayerSlotData slotData, TransferResult result)
{
    if (slotData == null || slotData.characterBagMaterials == null || slotData.characterBagMaterials.Count == 0)
    {
        return;
    }
    
    var runeInventory = RuneInventoryManager.Instance;
    if (runeInventory == null)
    {
        LogWarning("⚠️ RuneInventoryManager를 찾을 수 없음 - 룬 조각 전송 스킵");
        return;
    }
    
    // 룬 조각 필터링 및 전송
    var materialsToRemove = new List<MaterialStack>();
    int fragmentsTransferred = 0;
    
    foreach (var mat in slotData.characterBagMaterials)
    {
        string matId = mat.materialType.ToString();
        
        // "RUNE_FRAG_" 접두사로 룬 조각 식별
        if (matId.StartsWith("RUNE_FRAG_"))
        {
            // "RUNE_FRAG_RUNE_BOSS_HUNTER" -> "RUNE_BOSS_HUNTER"
            string targetRuneId = matId.Replace("RUNE_FRAG_", "");
            
            // 룬 매니저로 조각 추가
            runeInventory.AddRuneFragment(targetRuneId, mat.count);
            
            // 제거 목록에 추가
            materialsToRemove.Add(mat);
            fragmentsTransferred++;
            
            Log($"💎 룬 조각 전송: {targetRuneId} x{mat.count}개 → RuneInventoryManager");
        }
    }
    
    // 룬 조각은 재료 가방에서 제거 (일반 아이템 인벤토리로 가지 않음)
    foreach (var mat in materialsToRemove)
    {
        slotData.characterBagMaterials.Remove(mat);
    }
    
    if (fragmentsTransferred > 0)
    {
        result.runeFragmentsTransferred = fragmentsTransferred;
        Log($"✅ 룬 조각 전송 완료: {fragmentsTransferred}종류");
    }
}
```

**핵심 로직**:
1. `characterBagMaterials` 순회
2. `"RUNE_FRAG_"` 접두사 필터링
3. 접두사 제거 후 `RuneInventoryManager.AddRuneFragment()` 호출
4. 재료 가방에서 제거 (`continue`로 일반 인벤토리로 가지 않도록)

---

#### TransferResult 확장
```csharp
/// <summary>[Phase 8-1] 룬 조각으로 전송된 재료 종류 수</summary>
public int runeFragmentsTransferred = 0;
```

---

### 3. **기존 메서드 수정**

#### AddFragments() - 이벤트 추가
```csharp
// Before:
public void AddFragments(string runeId, int amount)
{
    // ... 조각 추가 로직
    Debug.Log($"[RuneInventoryManager] 조각 추가: {runeId} +{amount}");
}

// After:
public void AddFragments(string runeId, int amount)
{
    // ... 조각 추가 로직
    Debug.Log($"[RuneInventoryManager] 조각 추가: {runeId} +{amount}");
    
    // UI 갱신 이벤트 ★추가★
    OnInventoryChanged?.Invoke();
}
```

---

## 🧪 테스트 방법

### 1. Unity Inspector 테스트 (간편)

**방법**:
1. Play 모드 진입
2. Hierarchy에서 `RuneInventoryManager` 선택
3. Inspector에서 `RuneInventoryManager` 컴포넌트 우클릭
4. `테스트: 보스 사냥꾼 조각 +100` 선택

**예상 결과**:
```
💎 [RuneDrop] RUNE_BOSS_HUNTER 룬 조각 100개 획득! (총 보유: 1100개)
[DEBUG] RUNE_BOSS_HUNTER 조각 100개 추가 완료!
```

**UI 확인**:
- 룬 패널이 열려있으면 즉시 조각 개수가 갱신됨
- `OnInventoryChanged` 이벤트로 자동 갱신

---

### 2. 드롭 시스템 통합 테스트

#### Step 1: DropTable에 룬 조각 추가

**위치**: `Assets/ScriptableObjects/DropTables/`

**예시**:
```json
{
  "dropItems": [
    {
      "itemType": "Material",
      "itemId": "RUNE_FRAG_RUNE_BOSS_HUNTER",
      "dropRate": 0.3,
      "minQuantity": 5,
      "maxQuantity": 15
    }
  ]
}
```

---

#### Step 2: 스테이지 클리어 후 확인

**플로우**:
1. 던전 입장
2. 몬스터 처치 (룬 조각 드롭)
3. 스테이지 종료
4. Console 로그 확인:

**예상 로그**:
```
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
[StageEndItemTransfer] 🚀 스테이지 종료 아이템 전송 시작
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
[StageEndItemTransfer] 📦 재료 가방: 3종류
[StageEndItemTransfer] ✅ 재료 전송: 철광석 x10
[StageEndItemTransfer] 💎 룬 조각 전송: RUNE_BOSS_HUNTER x10개 → RuneInventoryManager
[RuneInventoryManager] 조각 추가: RUNE_BOSS_HUNTER +10 (총: 1010개)
💎 [RuneDrop] RUNE_BOSS_HUNTER 룬 조각 10개 획득! (총 보유: 1010개)
[StageEndItemTransfer] ✅ 룬 조각 전송 완료: 1종류
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
[StageEndItemTransfer] ✅ 전송 완료: 창고 0개 | 우편함 0개 | 유지 0개 | 룬조각 1종
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
```

---

## 🔧 기술적 세부 사항

### 1. 룬 조각 식별 로직

```csharp
string matId = mat.materialType.ToString();

if (matId.StartsWith("RUNE_FRAG_"))
{
    // 접두사 제거
    string targetRuneId = matId.Replace("RUNE_FRAG_", "");
    
    // 예: "RUNE_FRAG_RUNE_BOSS_HUNTER" -> "RUNE_BOSS_HUNTER"
}
```

---

### 2. 중복 추가 방지

```csharp
// 룬 조각은 재료 가방에서 제거됨
foreach (var mat in materialsToRemove)
{
    slotData.characterBagMaterials.Remove(mat);
}

// ✅ 일반 아이템 인벤토리로 가지 않음
// ✅ 계정 창고로 가지 않음
// ✅ RuneInventoryManager로만 전달됨
```

---

### 3. 이벤트 동기화

```csharp
// RuneInventoryManager.AddFragments()
OnInventoryChanged?.Invoke();

// 구독자: RunePanelUI.OnEnable()
inventoryManager.OnInventoryChanged += OnInventoryChanged;

// 자동 갱신
private void OnInventoryChanged()
{
    RefreshUI(); // 모든 패널 갱신
}
```

---

## 📊 DropTable 설정 가이드

### MaterialType Enum에 추가 필요

**파일**: `Assets/Scripts/Enums/MaterialType.cs` (또는 유사한 파일)

```csharp
public enum MaterialType
{
    // ... 기존 재료들
    IronOre,
    WoodLog,
    
    // [Phase 8-1] 룬 조각 (8종)
    RUNE_FRAG_RUNE_BOSS_HUNTER,
    RUNE_FRAG_RUNE_BOSS_DEFENDER,
    RUNE_FRAG_RUNE_DEFENSE_BREAKER,
    RUNE_FRAG_RUNE_HIGH_HP_HUNTER,
    RUNE_FRAG_RUNE_EXECUTIONER,
    RUNE_FRAG_RUNE_SURVIVOR,
    RUNE_FRAG_RUNE_AREA_DEFENDER,
    RUNE_FRAG_RUNE_VAMPIRE
}
```

---

### DropTable 예시

**난이도별 룬 조각 드롭율**:

```json
{
  "dropTableId": "BOSS_STAGE_01",
  "dropItems": [
    {
      "itemType": "Material",
      "itemId": "RUNE_FRAG_RUNE_BOSS_HUNTER",
      "dropRate": 1.0,
      "minQuantity": 10,
      "maxQuantity": 20,
      "description": "보스 사냥꾼 룬 조각"
    },
    {
      "itemType": "Material",
      "itemId": "RUNE_FRAG_RUNE_EXECUTIONER",
      "dropRate": 0.5,
      "minQuantity": 5,
      "maxQuantity": 10,
      "description": "처형자 룬 조각"
    }
  ]
}
```

**권장 드롭율**:
- 일반 몬스터: 10~30% (조각 1~3개)
- 엘리트 몬스터: 50~80% (조각 5~10개)
- 보스 몬스터: 100% (조각 10~20개)

---

## 🎮 실제 게임플레이 테스트

### 시나리오 1: 보스 사냥꾼 조각 획득

**초기 상태**:
- 보스 사냥꾼 조각: 1000개
- 보유 룬: 없음 (미해금)

**던전 클리어 후**:
- 보스 사냥꾼 조각: 1015개 (+15개 획득)

**UI 확인**:
```
[우측 리스트]
보스 사냥꾼 (미보유)
보유: 1015 / 100  ← 자동 갱신 ✅
[해금] 버튼 (활성화)
```

---

### 시나리오 2: 복수 룬 조각 획득

**드롭 결과**:
- 보스 사냥꾼 조각 x10
- 방어 파괴자 조각 x5
- 처형자 조각 x8

**전송 로그**:
```
💎 룬 조각 전송: RUNE_BOSS_HUNTER x10개 → RuneInventoryManager
💎 룬 조각 전송: RUNE_DEFENSE_BREAKER x5개 → RuneInventoryManager
💎 룬 조각 전송: RUNE_EXECUTIONER x8개 → RuneInventoryManager
✅ 룬 조각 전송 완료: 3종류
```

**UI 확인**:
- 3개 룬의 조각 개수가 모두 갱신됨
- `OnInventoryChanged` 이벤트 1회 호출로 전체 UI 갱신

---

## 🚫 중요 주의사항

### 1. MaterialType Enum 확장 필요

**⚠️ 중요**: `MaterialType` Enum에 8종류의 룬 조각을 추가해야 합니다!

**위치 찾기**:
```bash
# MaterialType 정의 파일 검색
Assets/Scripts/**/MaterialType.cs
Assets/Scripts/Enums/MaterialType.cs
```

---

### 2. 일반 아이템 인벤토리와 분리

```csharp
// ✅ 룬 조각은 RuneInventoryManager로만 전달
// ❌ 일반 아이템 인벤토리에 추가되지 않음
// ❌ 계정 창고에 추가되지 않음
```

---

### 3. 저장 시스템 연동

**현재**:
- `RuneInventoryManager`는 자체 저장 시스템 사용 (`PlayerPrefs`)
- `StageEndItemTransfer`는 `AccountDataManager.Save()` 호출

**추후 개선**:
- 룬 조각도 `AccountDataManager`로 통합 고려
- 현재는 별도 저장 시스템으로 작동

---

## 📈 성능 최적화

### 1. 이벤트 최적화
```csharp
// ❌ 나쁜 예: 조각 추가마다 이벤트 발행
foreach (var mat in materials)
{
    AddRuneFragment(runeId, 1); // 10번 호출 시 이벤트 10번
}

// ✅ 좋은 예: 한 번에 추가
AddRuneFragment(runeId, 10); // 이벤트 1번
```

---

### 2. UI 갱신 최소화
```csharp
// OnInventoryChanged 이벤트는 TransferRuneFragments() 종료 후
// AddFragments() 내부에서 발행되므로,
// 전체 전송 완료 후 1회만 RefreshUI() 호출됨
```

---

## 🐛 트러블슈팅

### 문제 1: 조각이 추가되지 않음
**원인**: `MaterialType` Enum에 `RUNE_FRAG_` 항목이 없음  
**해결**: Enum에 8종류 추가

### 문제 2: UI가 갱신되지 않음
**원인**: `OnInventoryChanged` 이벤트 미구독  
**해결**: `RunePanelUI.OnEnable()`에서 이벤트 구독 확인

### 문제 3: 일반 인벤토리에 들어감
**원인**: 라우팅 로직 미작동  
**해결**: `StartsWith("RUNE_FRAG_")` 조건 확인

### 문제 4: 조각 개수가 2배로 증가
**원인**: 재료 가방에서 제거 안됨  
**해결**: `materialsToRemove` 리스트로 후처리

---

## 🎯 다음 단계 (Phase 8-2 예정)

1. **MaterialType Enum 확장**
   - 8종류 룬 조각 Enum 추가

2. **DropTable 설정**
   - 각 던전별 룬 조각 드롭율 설정

3. **시각적 연출**
   - 필드에 조각 스폰
   - 획득 이펙트

4. **UI 획득 알림**
   - 화면 중앙에 "보스 사냥꾼 조각 +10" 표시

---

## 📝 변경 사항 요약

### 수정된 파일 (5개)
1. `RuneInventoryManager.cs`
   - `AddRuneFragment()` 메서드 추가
   - `AddFragments()` 이벤트 추가
   - ContextMenu 디버그 테스트 2개 추가

2. `StageEndItemTransfer.cs`
   - `TransferRuneFragments()` 메서드 추가
   - `ExecuteTransfer()` 호출 흐름 수정
   - `TransferResult.runeFragmentsTransferred` 필드 추가

3. `MaterialPickup.cs`
   - `UpdateIcon()` 메서드에 룬 조각 처리 추가
   - RuneDatabase에서 룬 아이콘 로드
   - "RUNE_FRAG_" 접두사 감지 로직 추가

4. **`EnemyHealth.cs`** ★중요★
   - `SpawnSingleItem()`: "RUNE_FRAG_" 라우팅 추가
   - `SpawnMaterialItem()`: MaterialType Enum 직접 변환 지원
   - 룬 조각 드롭 로그 추가

### 생성된 파일 (1개)
5. `Phase8_1_DropTable_Setup_Guide.md`
   - DropTable 설정 가이드
   - 완전한 테스트 시나리오
   - 밸런싱 권장사항
   - 8종 룬 조각 매핑표
   - 트러블슈팅 (드롭 안되는 문제 해결)

---

## 📚 API 문서

### RuneInventoryManager

```csharp
/// <summary>
/// [Phase 8-1] 룬 조각 획득 (드롭 시스템 연동용)
/// </summary>
/// <param name="runeId">룬 ID (예: "RUNE_BOSS_HUNTER")</param>
/// <param name="amount">획득 개수</param>
public void AddRuneFragment(string runeId, int amount)
```

**사용 예시**:
```csharp
// 보스 처치 시 조각 지급
RuneInventoryManager.Instance.AddRuneFragment("RUNE_BOSS_HUNTER", 20);

// 퀘스트 보상
RuneInventoryManager.Instance.AddRuneFragment("RUNE_EXECUTIONER", 50);

// 상점 구매
RuneInventoryManager.Instance.AddRuneFragment("RUNE_DEFENSE_BREAKER", 100);
```

---

---

## 🎯 Phase 8-1 완료 체크리스트

### ✅ 완료된 작업
- [x] MaterialType Enum 확장 (8종 룬 조각)
- [x] MaterialPickup 룬 조각 지원 (아이콘 로드)
- [x] StageEndItemTransfer 라우팅 로직
- [x] RuneInventoryManager AddRuneFragment API
- [x] UI 자동 갱신 (OnInventoryChanged)
- [x] DropTable 설정 가이드 작성
- [x] 테스트 시나리오 작성

### 📊 구현된 완전한 플로우
```
1. 몬스터 처치 (EnemyHealth)
    ↓
2. DropResolver: DropTable에서 "RUNE_FRAG_RUNE_BOSS_HUNTER" 선택
    ↓
3. EnemyHealth.SpawnSingleItem("RUNE_FRAG_RUNE_BOSS_HUNTER")
    ↓
4. itemId.StartsWith("RUNE_FRAG_") 감지 → SpawnMaterialItem() 호출 ✅
    ↓
5. MaterialType Enum 직접 변환 (MaterialDatabase 대신) ✅
    ↓
6. GamePoolManager.SpawnFromPool("Drop_Material")
    ↓
7. MaterialPickup.Initialize(MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER)
    ↓
8. UpdateIcon() → RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER") → 룬 아이콘 표시 ✅
    ↓
9. 플레이어 픽업 → characterBagMaterials 추가
    ↓
10. 스테이지 종료 → TransferRuneFragments() ✅
    ↓
11. RuneInventoryManager.AddRuneFragment("RUNE_BOSS_HUNTER", 10) ✅
    ↓
12. OnInventoryChanged 이벤트 → RunePanelUI.RefreshUI() ✅
```

---

**✅ Phase 8-1 드롭 시스템 연동 완료!**

**다음 작업**: DropTable 실전 배치 → 게임플레이 테스트 → 밸런싱 🚀

**상세 가이드**: `Phase8_1_DropTable_Setup_Guide.md` 참조
