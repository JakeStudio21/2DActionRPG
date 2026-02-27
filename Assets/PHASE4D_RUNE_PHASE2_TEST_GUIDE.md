# 🧪 룬 시스템 Phase 2 테스트 가이드

## ✅ Phase 2 완료 항목
- **RuneDatabase**: Resources/Runes/ 경로의 모든 RuneData 자동 캐싱
- **RuneSaveData**: JSON 직렬화 전용 클래스
- **RuneInventoryManager**: 룬 인벤토리 CRUD + 세이브/로드

---

## 🎯 테스트 시나리오

### 1단계: 테스트 룬 생성
```
Unity Editor 메뉴:
Tools → Rune System → Generate Test Runes

결과:
- Resources/Runes/ 폴더에 3개 룬 ScriptableObject 생성
- RUNE_BOSS_HUNTER, RUNE_BOSS_DEFENDER, RUNE_LIFESTEAL
```

---

### 2단계: RuneDatabase 자동 초기화 확인
```
Unity Play 모드 진입 시 자동 실행:
[RuneDatabase] 초기화 완료!
  ✅ 성공: 3개

콘솔 로그:
[RuneDatabase] 캐시 목록
총 3개의 룬 데이터:
  [RUNE_BOSS_HUNTER] 보스 사냥꾼 (Attack1)
  [RUNE_BOSS_DEFENDER] 보스 철벽 (Survival1)
  [RUNE_LIFESTEAL] 흡혈 룬 (Utility1)
```

---

### 3단계: RuneInventoryManager 테스트 (C# 코드)

#### 테스트 1: 룬 추가
```csharp
// 1. RuneData 가져오기
RuneData bossHunterData = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");

// 2. 인벤토리에 추가 (새로운 RuneInstance 생성됨)
RuneInstance newRune = RuneInventoryManager.Instance.AddRune(bossHunterData);

// 3. 확인
Debug.Log($"추가된 룬: {newRune}");
Debug.Log($"총 룬 개수: {RuneInventoryManager.Instance.GetRuneCount()}");
```

**예상 출력:**
```
[RuneInventoryManager] 룬 추가: [보스 사냥꾼] Lv.1 (한돌 0) | 총 1개
추가된 룬: [보스 사냥꾼] Lv.1 (한돌 0)
총 룬 개수: 1
```

---

#### 테스트 2: 여러 개 추가
```csharp
// 동일한 종류의 룬 3개 추가 (한계돌파 재료용)
for (int i = 0; i < 3; i++)
{
    RuneData runeData = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
    RuneInventoryManager.Instance.AddRune(runeData);
}

// 다른 종류의 룬도 추가
RuneData defenderData = RuneDatabase.GetRuneData("RUNE_BOSS_DEFENDER");
RuneInventoryManager.Instance.AddRune(defenderData);

// 전체 출력
RuneInventoryManager.Instance.PrintInventory();
```

**예상 출력:**
```
========== [RuneInventoryManager] 인벤토리 ==========
총 4개의 룬:
  [1] [보스 사냥꾼] Lv.1 (한돌 0)
  [2] [보스 사냥꾼] Lv.1 (한돌 0)
  [3] [보스 사냥꾼] Lv.1 (한돌 0)
  [4] [보스 철벽] Lv.1 (한돌 0)
====================================================
```

---

#### 테스트 3: 특정 종류의 룬 검색 (한계돌파 재료 찾기)
```csharp
// 같은 종류의 룬 전체 검색
List<RuneInstance> bossHunterRunes = RuneInventoryManager.Instance.GetRunesByDataId("RUNE_BOSS_HUNTER");

Debug.Log($"보스 사냥꾼 룬 개수: {bossHunterRunes.Count}");

foreach (var rune in bossHunterRunes)
{
    Debug.Log($"  - UID: {rune.instanceUID}, 레벨: {rune.currentLevel}");
}
```

**예상 출력:**
```
보스 사냥꾼 룬 개수: 3
  - UID: 12345-67890-abcde, 레벨: 1
  - UID: 98765-43210-fghij, 레벨: 1
  - UID: 11111-22222-klmno, 레벨: 1
```

---

#### 테스트 4: 룬 삭제 (잠금 테스트 포함)
```csharp
// 1. 삭제할 룬 UID 가져오기
List<RuneInstance> allRunes = RuneInventoryManager.Instance.GetAllRunes();
string targetUID = allRunes[0].instanceUID;

// 2. 잠금 설정
RuneInventoryManager.Instance.SetLock(targetUID, true);

// 3. 삭제 시도 (실패)
bool removed1 = RuneInventoryManager.Instance.RemoveRune(targetUID);
Debug.Log($"잠금 상태 삭제 시도: {removed1}"); // false

// 4. 잠금 해제 후 삭제 (성공)
RuneInventoryManager.Instance.SetLock(targetUID, false);
bool removed2 = RuneInventoryManager.Instance.RemoveRune(targetUID);
Debug.Log($"잠금 해제 후 삭제: {removed2}"); // true
```

**예상 출력:**
```
[RuneInventoryManager] 잠금 설정: [보스 사냥꾼] Lv.1 → 🔒
잠금 상태 삭제 시도: false
[RuneInventoryManager] 잠금 설정: [보스 사냥꾼] Lv.1 → 🔓
[RuneInventoryManager] 룬 삭제: [보스 사냥꾼] Lv.1 | 남은 룬: 3개
잠금 해제 후 삭제: true
```

---

#### 테스트 5: 저장 (JSON → PlayerPrefs)
```csharp
// 현재 인벤토리 저장
bool saved = RuneInventoryManager.Instance.SaveInventory();

if (saved)
{
    Debug.Log("✅ 저장 성공!");
}
```

**예상 출력:**
```
[RuneInventoryManager] 저장 시작... (총 3개 룬)
[RuneInventoryManager] ✅ 저장 완료! (3개 룬)
  저장 시간: 2026-02-27 15:30:45
✅ 저장 성공!
```

---

#### 테스트 6: 로드 (PlayerPrefs → JSON → RuneInstance 복원)
```csharp
// 1. 인벤토리 비우기 (테스트용)
RuneInventoryManager.Instance.DeleteSaveData();
RuneInventoryManager.Instance.PrintInventory(); // 비어있음

// 2. 다시 저장 (테스트 데이터)
RuneData data = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
RuneInventoryManager.Instance.AddRune(data);
RuneInventoryManager.Instance.SaveInventory();

// 3. Unity 재시작 (또는 Play 모드 재진입)
// → 자동으로 LoadInventory() 호출됨

// 4. 확인
RuneInventoryManager.Instance.PrintInventory();
```

**예상 출력:**
```
[RuneInventoryManager] 로드 시작...
  저장 시간: 2026-02-27 15:30:45
  저장 버전: 1
  룬 개수: 1개
[RuneInventoryManager] ✅ 로드 완료!
  성공: 1개

========== [RuneInventoryManager] 인벤토리 ==========
총 1개의 룬:
  [1] [보스 사냥꾼] Lv.1 (한돌 0)
====================================================
```

---

## 🔧 Inspector 테스트 (Unity Editor)

### 1. RuneInventoryManager GameObject 생성
```
1. Hierarchy에서 빈 GameObject 생성
2. 이름: "RuneInventoryManager"
3. Add Component → RuneInventoryManager
```

### 2. Context Menu 활용
```
RuneInventoryManager 컴포넌트 우클릭:
- Print Inventory: 현재 인벤토리 콘솔 출력
```

---

## 🐛 예외 처리 테스트

### 1. 없는 runeId로 검색
```csharp
RuneData notFound = RuneDatabase.GetRuneData("RUNE_NOT_EXIST");
Debug.Log(notFound == null); // true
```

**예상 출력:**
```
[RuneDatabase] runeId를 찾을 수 없습니다: RUNE_NOT_EXIST
true
```

---

### 2. 빈 인벤토리에서 삭제
```csharp
bool removed = RuneInventoryManager.Instance.RemoveRune("invalid_uid");
Debug.Log(removed); // false
```

**예상 출력:**
```
[RuneInventoryManager] RemoveRune: UID를 찾을 수 없습니다: invalid_uid
false
```

---

### 3. null RuneData 추가
```csharp
RuneInstance nullRune = RuneInventoryManager.Instance.AddRune(null);
Debug.Log(nullRune == null); // true
```

**예상 출력:**
```
[RuneInventoryManager] AddRune: RuneData가 null입니다!
true
```

---

## ✅ 체크리스트

- [ ] RuneDatabase 자동 초기화 확인 (콘솔 로그)
- [ ] 룬 추가 정상 작동 (AddRune)
- [ ] 룬 삭제 정상 작동 (RemoveRune)
- [ ] UID 검색 정상 작동 (GetRuneByUID)
- [ ] 종류별 검색 정상 작동 (GetRunesByDataId)
- [ ] 잠금 기능 정상 작동 (SetLock)
- [ ] JSON 저장 정상 작동 (SaveInventory)
- [ ] JSON 로드 정상 작동 (LoadInventory)
- [ ] 예외 처리 확인 (null 체크, 빈 검색 등)

---

## 📌 다음 단계 (Phase 3 예정)

- [ ] 레벨업 시스템 (3, 6, 9레벨 부옵션 가중치 추첨)
- [ ] 한계돌파 시스템 (중복 룬 소모)
- [ ] 중복 장착 방지 (같은 RuneId)

---

## 🆘 문제 해결

### Q1: "RuneData를 찾을 수 없습니다" 에러
**원인**: Resources/Runes/ 폴더에 ScriptableObject가 없음  
**해결**: `Tools → Rune System → Generate Test Runes` 실행

### Q2: 저장 후 로드해도 빈 인벤토리
**원인**: PlayerPrefs 키가 다르거나 JSON 파싱 실패  
**해결**: 콘솔 로그 확인, `DeleteSaveData()` 후 재저장

### Q3: "초기화되지 않았습니다" 경고
**원인**: RuneDatabase가 자동 초기화되지 않음  
**해결**: Play 모드 재진입 또는 `RuneDatabase.Initialize()` 수동 호출

