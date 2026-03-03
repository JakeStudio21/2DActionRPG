# 💾 Phase 9: 실제 게임 데이터 동기화 가이드

**작성일**: 2026-02-28  
**목적**: 룬 시스템을 임시 저장에서 실제 계정/캐릭터 데이터와 완전 동기화

---

## 🎯 개요

**Before (Phase 8)**: 
- 룬 조각: `Dictionary<string, int> runeFragments` (임시)
- 룬 인스턴스: `PlayerPrefs` (임시)

**After (Phase 9)**:
- 룬 조각: `AccountData.materials` (계정 공용) ✅ **이미 완료!**
- 룬 인스턴스: `PlayerSlotData.runes` (캐릭터 전용)

---

## ✅ Task 1: PlayerSlotData 확장 (완료!)

이미 적용된 내용:

```csharp
// PlayerSlotData.cs (Line 211~220)
[Header("💎 룬 시스템 (캐릭터별)")]
[Tooltip("해금된 룬 인스턴스 (레벨, 한계돌파 상태)")]
public List<RuneInstanceSaveData> runes = new List<RuneInstanceSaveData>();

[Tooltip("장착된 룬 슬롯 (3개, runeUID 저장)")]
public string[] equippedRuneUids = new string[3];
```

---

## 🔧 Task 2: StageEndItemTransfer 정리

**문제**: Phase 8-1에서 룬 조각을 특수 처리했던 `TransferRuneFragments()` 제거

**해결**: 룬 조각도 일반 재료처럼 `TransferMaterials()`에서 자동 처리

### 수정 파일: `StageEndItemTransfer.cs`

#### 변경 1: TransferRuneFragments() 메서드 **삭제**

```csharp
// ❌ 삭제할 메서드 (Line 173~228)
private void TransferRuneFragments(PlayerSlotData slotData, TransferResult result)
{
    // ... 전체 삭제
}
```

#### 변경 2: ExecuteTransfer()에서 호출 **제거**

```csharp
// Before (Line 148)
TransferMaterials(slotData, account, result);
TransferRuneFragments(slotData, result); // ❌ 이 줄 삭제

// After
TransferMaterials(slotData, account, result); // 룬 조각도 여기서 자동 처리됨
```

#### 변경 3: TransferResult 클래스 **단순화**

```csharp
// Before
public class TransferResult
{
    public int materialTypesTransferred = 0;
    public int runeFragmentsTransferred = 0; // ❌ 삭제
}

// After
public class TransferResult
{
    public int materialTypesTransferred = 0; // 룬 조각 포함
}
```

**완료 후 동작**:
- `TransferMaterials()`가 **모든 재료 (일반 + 룬 조각)**를 `AccountData.materials`로 전송
- 룬 조각이 `characterBagMaterials`에서 `accountData.materials`로 자동 이동 ✅

---

## 🔧 Task 3: RuneInventoryManager 리팩토링

**핵심**: `Dictionary<string, int> runeFragments` 완전 제거 → `AccountData.materials` 연동

### 수정 파일: `RuneInventoryManager.cs`

#### 변경 1: Dictionary 필드 **삭제** (Line 77~80)

```csharp
// ❌ 삭제
[SerializeField]
private Dictionary<string, int> runeFragments = new Dictionary<string, int>();
```

#### 변경 2: InitializeFragments() **삭제** (Line 128~147)

```csharp
// ❌ 메서드 전체 삭제
private void InitializeFragments() { ... }
```

#### 변경 3: GetFragmentCount() **수정** (Line 545~558)

```csharp
// ❌ Before
public int GetFragmentCount(string runeId)
{
    if (runeFragments == null) { ... }
    if (runeFragments.TryGetValue(runeId, out int count)) { return count; }
    return 0;
}

// ✅ After
public int GetFragmentCount(string runeId)
{
    // runeId → MaterialType 변환
    string materialId = $"RUNE_FRAG_{runeId}";
    MaterialType materialType = MaterialTypeExtensions.FromItemId(materialId);
    
    if (materialType == MaterialType.None)
    {
        Debug.LogWarning($"[RuneInventoryManager] MaterialType 변환 실패: {materialId}");
        return 0;
    }
    
    // AccountData에서 실제 보유량 조회
    return AccountDataManager.Instance?.GetMaterialCount(materialType) ?? 0;
}
```

#### 변경 4: TryConsumeFragments() **수정** (Line 623~647)

```csharp
// ❌ Before
public bool TryConsumeFragments(string runeId, int amount)
{
    if (runeFragments == null) { ... }
    if (!runeFragments.ContainsKey(runeId)) { return false; }
    runeFragments[runeId] -= amount;
    return true;
}

// ✅ After
public bool TryConsumeFragments(string runeId, int amount)
{
    // runeId → MaterialType 변환
    string materialId = $"RUNE_FRAG_{runeId}";
    MaterialType materialType = MaterialTypeExtensions.FromItemId(materialId);
    
    if (materialType == MaterialType.None)
    {
        Debug.LogWarning($"[RuneInventoryManager] MaterialType 변환 실패: {materialId}");
        return false;
    }
    
    // AccountData에서 실제 소모
    bool success = AccountDataManager.Instance?.TryRemoveMaterial(materialType, amount) ?? false;
    
    if (success)
    {
        Debug.Log($"[RuneInventoryManager] 조각 소모: {runeId} -{amount}개");
        OnInventoryChanged?.Invoke(); // UI 갱신
    }
    else
    {
        int currentCount = GetFragmentCount(runeId);
        Debug.LogWarning($"[RuneInventoryManager] 조각 부족: {runeId} (보유: {currentCount}개, 필요: {amount}개)");
    }
    
    return success;
}
```

#### 변경 5: AddRuneFragment() **Obsolete 처리** (Line 586~618)

```csharp
// ✅ Obsolete로 변경 (삭제하지 않고 경고만)
[System.Obsolete("Phase 9: 드롭 시스템이 AccountData Material로 직접 저장함")]
public void AddRuneFragment(string runeId, int amount)
{
    Debug.LogWarning($"[RuneInventoryManager] AddRuneFragment는 Obsolete입니다. 드롭 시스템이 AccountData.materials로 직접 저장합니다.");
}
```

#### 변경 6: GetAllFragments() **수정** (Line 652~660)

```csharp
// ✅ After
public Dictionary<string, int> GetAllFragments()
{
    var result = new Dictionary<string, int>();
    
    // Resources/Runes 폴더에서 모든 RuneData 로드
    var allRunes = Resources.LoadAll<RuneData>("Runes");
    
    foreach (var runeData in allRunes)
    {
        int count = GetFragmentCount(runeData.runeId);
        result[runeData.runeId] = count;
    }
    
    return result;
}
```

---

## 🔧 Task 4: RuneManager Save/Load 로직 추가

### 목적
- 캐릭터 접속 시: `PlayerSlotData.runes` → `RuneInventoryManager.runeInventory` 복원
- 게임 저장 시: `RuneInventoryManager.runeInventory` → `PlayerSlotData.runes` 저장

### 수정 파일: `RuneManager.cs`

#### 추가 1: SaveRunes() 메서드

```csharp
/// <summary>
/// [Phase 9] 현재 룬 데이터를 PlayerSlotData에 저장
/// </summary>
public void SaveRunes(PlayerSlotData slotData)
{
    if (slotData == null)
    {
        Debug.LogWarning("[RuneManager] SaveRunes: slotData가 null입니다.");
        return;
    }
    
    var inventory = RuneInventoryManager.Instance;
    if (inventory == null)
    {
        Debug.LogWarning("[RuneManager] SaveRunes: RuneInventoryManager를 찾을 수 없습니다.");
        return;
    }
    
    // 1. 모든 룬 인스턴스 저장
    slotData.runes = new List<RuneInstanceSaveData>();
    
    foreach (var rune in inventory.GetAllRunes())
    {
        var saveData = new RuneInstanceSaveData
        {
            runeId = rune.runeId,
            instanceUID = rune.instanceUID,
            currentLevel = rune.currentLevel,
            currentLimitBreak = rune.currentLimitBreak,
            isLocked = rune.isLocked,
            allocatedSubStatModifierIds = new List<string>(rune.allocatedSubStatModifierIds)
        };
        
        slotData.runes.Add(saveData);
    }
    
    // 2. 장착된 룬 UID 저장
    slotData.equippedRuneUids = new string[3];
    for (int i = 0; i < 3; i++)
    {
        var equipped = GetEquippedRune(i);
        slotData.equippedRuneUids[i] = equipped?.instanceUID ?? "";
    }
    
    Debug.Log($"[RuneManager] SaveRunes 완료: {slotData.runes.Count}개 룬, 장착 {slotData.equippedRuneUids.Count(s => !string.IsNullOrEmpty(s))}개");
}
```

#### 추가 2: LoadRunes() 메서드

```csharp
/// <summary>
/// [Phase 9] PlayerSlotData에서 룬 데이터 복원
/// </summary>
public void LoadRunes(PlayerSlotData slotData)
{
    if (slotData == null)
    {
        Debug.LogWarning("[RuneManager] LoadRunes: slotData가 null입니다.");
        return;
    }
    
    var inventory = RuneInventoryManager.Instance;
    if (inventory == null)
    {
        Debug.LogWarning("[RuneManager] LoadRunes: RuneInventoryManager를 찾을 수 없습니다.");
        return;
    }
    
    // 1. 기존 인벤토리 초기화
    inventory.ClearInventory();
    
    // 2. 저장된 룬 인스턴스 복원
    if (slotData.runes != null)
    {
        foreach (var saveData in slotData.runes)
        {
            // RuneData 로드
            var runeData = RuneDatabase.GetRuneData(saveData.runeId);
            if (runeData == null)
            {
                Debug.LogWarning($"[RuneManager] RuneData를 찾을 수 없음: {saveData.runeId}");
                continue;
            }
            
            // RuneInstance 복원
            var instance = new RuneInstance(runeData)
            {
                instanceUID = saveData.instanceUID,
                currentLevel = saveData.currentLevel,
                currentLimitBreak = saveData.currentLimitBreak,
                isLocked = saveData.isLocked,
                allocatedSubStatModifierIds = new List<string>(saveData.allocatedSubStatModifierIds)
            };
            
            inventory.AddRuneInstance(instance);
        }
    }
    
    // 3. 장착 상태 복원
    for (int i = 0; i < 3; i++)
    {
        UnequipRune(i); // 기존 장착 해제
    }
    
    if (slotData.equippedRuneUids != null)
    {
        for (int i = 0; i < Mathf.Min(slotData.equippedRuneUids.Length, 3); i++)
        {
            string uid = slotData.equippedRuneUids[i];
            if (!string.IsNullOrEmpty(uid))
            {
                var rune = inventory.GetRuneByUID(uid);
                if (rune != null)
                {
                    EquipRune(rune, i);
                }
            }
        }
    }
    
    Debug.Log($"[RuneManager] LoadRunes 완료: {slotData.runes?.Count ?? 0}개 룬 복원, 장착 {slotData.equippedRuneUids?.Count(s => !string.IsNullOrEmpty(s)) ?? 0}개");
}
```

#### 추가 3: PlayerSlotDataManager에서 호출

```csharp
// PlayerSlotDataManager.cs (또는 SlotLoadManager.cs)

// 캐릭터 접속 시
public void LoadSlot(int slotIndex)
{
    var slotData = LoadSlotData(slotIndex);
    
    // ... (기존 스킬 로드 등)
    
    // 룬 로드 추가
    RuneManager.Instance?.LoadRunes(slotData);
}

// 게임 저장 시
public void SaveCurrentSlot()
{
    var slotData = GetCurrentSlotData();
    
    // ... (기존 스킬 저장 등)
    
    // 룬 저장 추가
    RuneManager.Instance?.SaveRunes(slotData);
    
    SaveSlotData(slotData);
}
```

---

## 🧪 테스트 체크리스트

### Test 1: 룬 조각 획득 (드롭)
- [ ] 몬스터 처치 → 룬 조각 드롭
- [ ] 픽업 → `characterBagMaterials`에 추가
- [ ] 스테이지 종료 → `AccountData.materials`로 자동 이동
- [ ] 룬 패널 → 조각 개수 정상 표시

### Test 2: 룬 해금/강화
- [ ] 룬 해금 → `AccountData.materials`에서 조각 차감
- [ ] 룬 레벨업 → 조각 차감 및 레벨 증가
- [ ] 룬 한계돌파 → 조각 차감 및 한돌 증가

### Test 3: 세이브/로드
- [ ] 룬 해금 후 게임 저장 → 종료
- [ ] 게임 재시작 → 캐릭터 선택
- [ ] 룬 패널 → 해금된 룬 정상 표시
- [ ] 장착된 룬 → 좌측 패널에 정상 표시

### Test 4: 캐릭터 간 독립성
- [ ] 슬롯1: 룬A 해금
- [ ] 슬롯2 전환 → 룬A 미보유 (⭐캐릭터 전용)
- [ ] 두 캐릭터 모두 → 동일한 룬 조각 개수 (⭐계정 공용)

---

## 📊 Before / After 비교

### 룬 조각 (계정 공용)

**Before**:
```
RuneInventoryManager.runeFragments (Dictionary)
  └─ Phase 8-1: StageEndItemTransfer가 특수 처리
  └─ 임시 저장, 게임 재시작 시 초기화
```

**After**:
```
AccountData.materials (List<MaterialStack>)
  └─ 일반 재료와 동일하게 처리
  └─ 영구 저장, 모든 캐릭터 공유 ✅
```

---

### 룬 인스턴스 (캐릭터 전용)

**Before**:
```
RuneInventoryManager.runeInventory
  └─ PlayerPrefs 저장 (임시)
  └─ 캐릭터 구분 없음
```

**After**:
```
PlayerSlotData.runes (List<RuneInstanceSaveData>)
  └─ JSON 파일 저장 (영구)
  └─ 캐릭터별 독립적인 성장 상태 ✅
```

---

## ✅ 완료 후 장점

1. **데이터 일관성**: 룬 조각이 일반 재료처럼 처리됨
2. **영구 저장**: 게임 재시작해도 데이터 유지
3. **캐릭터 독립성**: 각 캐릭터가 독립적으로 룬 성장
4. **계정 공유**: 룬 조각은 모든 캐릭터가 공유
5. **코드 단순화**: 특수 처리 로직 제거

---

## 🎯 다음 단계 (선택)

1. **UI 개선**: 계정 공용 조각 표시
2. **교환 시스템**: 조각으로 다른 아이템 구매
3. **일일 퀘스트**: 조각 보상
4. **업적 시스템**: 룬 해금 업적

---

---

## 🆕 추가 개선: 장착 슬롯 경고 메시지 (Phase 9+)

### **문제**
- 3개 슬롯이 모두 차있을 때 다른 룬 장착 시도 → 1번 슬롯 자동 교체 (의도하지 않음)

### **해결**
- 슬롯이 모두 찼을 때 경고 메시지 표시 후 장착 차단

### **구현**

#### 1. **RunePanelUI.cs** - 경고 메시지 UI 추가
```csharp
[Header("=== Left Panel: 경고 메시지 ===")]
[SerializeField] private GameObject warningMessageObject;
[SerializeField] private TextMeshProUGUI warningMessageText;
[SerializeField] private float warningDisplayDuration = 3f;

public void ShowWarningMessage(string message)
{
    warningMessageText.text = message;
    warningMessageObject.SetActive(true);
    StartCoroutine(HideWarningMessageAfterDelay());
}
```

#### 2. **RuneListItemUI.cs** - 장착 시도 전 슬롯 체크
```csharp
// 빈 슬롯 찾기
if (emptySlotIndex < 0)
{
    panelUI.ShowWarningMessage("⚠️ 장착 슬롯이 가득 찼습니다!\n먼저 장착된 룬을 해제해주세요.");
    return; // 장착 차단
}
```

### **Inspector 설정**

**상세 가이드**: `RuneEquipSlotFullWarning_Setup.md`

**간략 요약**:
1. LeftPanel 하위에 `WarningMessage` Panel 생성
2. WarningMessage 하위에 `WarningText` TextMeshProUGUI 생성
3. 배경 색상: 빨강 반투명 (255, 100, 100, 200)
4. 텍스트: 중앙 정렬, 흰색
5. RunePanelUI 컴포넌트에 연결

---

**✅ Phase 9 완료 후 룬 시스템이 완전한 영구 저장 시스템으로 작동합니다!** 🎉
