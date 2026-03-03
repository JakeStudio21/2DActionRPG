# 📦 룬 조각 DropTable 설정 가이드 (Phase 8-1)

**작성일**: 2026-02-28  
**Phase**: Phase 8-1 - 드롭 시스템 연동 완료  
**작성자**: AI Assistant

---

## 🎯 완료된 작업

### ✅ 1. MaterialType Enum 확장
- 8종류 룬 조각 추가 (200~207번)
- `GetDisplayName()` 확장 메서드 추가
- `ToItemId()` / `FromItemId()` 변환 지원

### ✅ 2. MaterialPickup.cs 룬 조각 지원
- `UpdateIcon()` 메서드에 룬 조각 처리 추가
- RuneDatabase에서 아이콘 로드
- 기존 재료 드롭 기능 유지

### ✅ 3. 라우팅 시스템
- `StageEndItemTransfer.cs`에서 룬 조각 필터링
- `RuneInventoryManager`로 자동 전달
- UI 자동 갱신

---

## 📋 DropTable 설정 방법

### Step 1: DropTable ScriptableObject 생성

**위치**: `Assets/ScriptableObjects/DropTables/`

**예시**: `DropTable_BossStage01.asset`

---

### Step 2: Inspector 설정

#### **일반 몬스터용 DropTable**

```
Drop Table ID: NORMAL_MONSTER_01
Drop Policy: RollEachWithChance (권장)

Drop Items (Size: 3)
├─ Element 0 (골드)
│  ├─ Item Id: "ITEM_GOLD"
│  ├─ Rarity: Common
│  ├─ Chance: 1.0 (100%)
│  ├─ Min Quantity: 10
│  └─ Max Quantity: 30
│
├─ Element 1 (무기 파편)
│  ├─ Item Id: "MAT_WEAPON_FRAGMENT"
│  ├─ Rarity: Common
│  ├─ Chance: 0.3 (30%)
│  ├─ Min Quantity: 1
│  └─ Max Quantity: 3
│
└─ Element 2 (룬 조각) ★NEW★
   ├─ Item Id: "RUNE_FRAG_RUNE_BOSS_HUNTER" ★정확히 입력!★
   ├─ Rarity: Common
   ├─ Chance: 0.2 (20%)
   ├─ Min Quantity: 1
   └─ Max Quantity: 5
```

**⚠️ 중요**: DropTable에는 `Material Type` 필드가 없습니다!  
**Item Id** 필드에 직접 문자열을 입력해야 합니다.

---

#### **보스 몬스터용 DropTable**

```
Drop Table ID: BOSS_STAGE_01
Drop Policy: RollEachWithChance

Drop Items (Size: 5)
├─ Element 0 (골드)
│  ├─ Item Id: "ITEM_GOLD"
│  ├─ Chance: 1.0
│  ├─ Min Quantity: 100
│  └─ Max Quantity: 200
│
├─ Element 1 (장비)
│  ├─ Item Id: "EQUIP_SWORD_01" (예시)
│  ├─ Rarity: Epic
│  ├─ Chance: 0.8
│  └─ ... (기존 설정)
│
├─ Element 2 (보스 사냥꾼 조각) ★확정 드롭★
│  ├─ Item Id: "RUNE_FRAG_RUNE_BOSS_HUNTER" ★
│  ├─ Rarity: Common
│  ├─ Chance: 1.0 (100%)
│  ├─ Is Guaranteed: ✅ (선택)
│  ├─ Min Quantity: 10
│  └─ Max Quantity: 20
│
├─ Element 3 (처형자 조각)
│  ├─ Item Id: "RUNE_FRAG_RUNE_EXECUTIONER" ★
│  ├─ Rarity: Common
│  ├─ Chance: 0.5 (50%)
│  ├─ Min Quantity: 5
│  └─ Max Quantity: 10
│
└─ Element 4 (고체력 사냥꾼 조각)
   ├─ Item Id: "RUNE_FRAG_RUNE_HIGH_HP_HUNTER" ★
   ├─ Rarity: Common
   ├─ Chance: 0.3 (30%)
   ├─ Min Quantity: 3
   └─ Max Quantity: 8
```

---

## 🎮 권장 드롭율 설정

### 난이도별 가이드

| 몬스터 타입 | 드롭율 | 개수 | 비고 |
|-------------|--------|------|------|
| **일반 몬스터** | 10~30% | 1~5개 | 낮은 확률, 소량 |
| **엘리트 몬스터** | 50~80% | 5~15개 | 중간 확률, 중량 |
| **보스 몬스터** | 100% | 10~30개 | 확정 드롭, 대량 |
| **던전 완료 보상** | 100% | 50~100개 | 클리어 보상 |

---

### 룬별 드롭 난이도 (권장)

#### **공격형 룬** (Attack)
- **보스 사냥꾼**: 보스 몬스터 확정 드롭 (★추천★)
- **방어 파괴자**: 엘리트 몬스터 50%
- **고체력 사냥꾼**: 보스 몬스터 30%

#### **생존형 룬** (Survival)
- **보스 철벽**: 보스 몬스터 확정 드롭
- **불굴의 생존자**: 엘리트 몬스터 50%

#### **유틸리티 룬** (Utility)
- **처형자**: 보스 몬스터 50%
- **장판 철벽**: 특수 패턴 몬스터 50%
- **흡혈 룬**: 혈귀 타입 몬스터 50%

---

## 🧪 테스트 방법

### Test 1: Inspector ContextMenu (간편)

**단계**:
1. Play 모드 진입
2. Hierarchy에서 `RuneInventoryManager` 선택
3. Inspector > `RuneInventoryManager` 컴포넌트 우클릭
4. `테스트: 보스 사냥꾼 조각 +100` 클릭

**예상 결과**:
```
💎 [RuneDrop] RUNE_BOSS_HUNTER 룬 조각 100개 획득! (1000개 → 1100개)
[DEBUG] RUNE_BOSS_HUNTER 조각 100개 추가 완료!
[RunePanelUI] 인벤토리 변경 감지 → UI 갱신
```

**UI 확인**:
- 룬 패널이 열려있으면 즉시 `보유: 1100 / 100` 으로 갱신됨

---

### Test 2: 실제 드롭 테스트 (완전한 플로우)

#### Step 1: DropTable 설정
1. `Assets/ScriptableObjects/DropTables/` 폴더로 이동
2. 기존 DropTable 선택 (예: `DropTable_TestMonster`)
3. Inspector에서 `Drop Items` 배열에 새 요소 추가:
   - `Item Type`: Material
   - `Material Type`: `RUNE_FRAG_RUNE_BOSS_HUNTER`
   - `Drop Rate`: 1.0 (테스트용)
   - `Min Quantity`: 10
   - `Max Quantity`: 10

#### Step 2: 몬스터에 DropTable 연결
1. Hierarchy에서 테스트용 몬스터 선택
2. `EnemyHealth` 컴포넌트 확인
3. `Drop Table` 필드에 위에서 만든 DropTable 드래그

#### Step 3: 게임 플레이
1. Play 모드 진입
2. 몬스터 처치
3. **필드에 룬 조각 드롭 확인** ✅
4. 플레이어가 다가가서 자동 픽업
5. Console 로그 확인:

**예상 로그**:
```
📦 [MaterialPickup] 스폰: 보스 사냥꾼 룬 조각 x10 at (5.2, 3.1, 0)
💎 [MaterialPickup] 룬 조각 아이콘 로드: RUNE_BOSS_HUNTER
✅ [MaterialPickup] 재료 획득 → 캐릭터 가방: 보스 사냥꾼 룬 조각 x10
```

#### Step 4: 스테이지 종료
1. 던전 클리어 (또는 ESC → 나가기)
2. `StageEndItemTransfer` 자동 실행
3. Console 로그 확인:

**예상 로그**:
```
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
[StageEndItemTransfer] 🚀 스테이지 종료 아이템 전송 시작
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
[StageEndItemTransfer] 📦 재료 가방: 2종류
[StageEndItemTransfer] ✅ 재료 전송: 무기 강화 파편 x3
[StageEndItemTransfer] 💎 룬 조각 전송: RUNE_BOSS_HUNTER x10개 → RuneInventoryManager
[RuneInventoryManager] 조각 추가: RUNE_BOSS_HUNTER +10 (현재: 1010개)
💎 [RuneDrop] RUNE_BOSS_HUNTER 룬 조각 10개 획득! (1000개 → 1010개)
[StageEndItemTransfer] ✅ 룬 조각 전송 완료: 1종류
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
[StageEndItemTransfer] ✅ 전송 완료: 창고 0개 | 우편함 0개 | 유지 0개 | 룬조각 1종
[StageEndItemTransfer] ═══════════════════════════════════════════════════════
```

#### Step 5: UI 확인
1. ESC → 스킬북 열기 → 룬 탭 선택
2. 우측 리스트에서 "보스 사냥꾼" 확인
3. **`보유: 1010 / 100`** 으로 갱신되어 있는지 확인 ✅

---

## 🔄 전체 드롭 흐름 (완전 통합)

```
1. 몬스터 처치
    ↓
2. DropTable 확인: RUNE_FRAG_RUNE_BOSS_HUNTER (확률: 100%)
    ↓
3. GamePoolManager에서 "Drop_Material" 프리팹 스폰
    ↓
4. MaterialPickup.Initialize(MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER, 10, position)
    ↓
5. UpdateIcon() 호출
   - MaterialDatabase.GetData() → null (룬 조각은 MaterialDatabase에 없음)
   - "RUNE_FRAG_" 감지 ✅
   - RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER") ✅
   - runeData.icon 사용 ✅
    ↓
6. 필드에 룬 아이콘으로 표시됨 ✅
    ↓
7. 플레이어 픽업 (OnTriggerEnter2D)
    ↓
8. PlayerDataManager.AddMaterialToCharacterBag(RUNE_FRAG_RUNE_BOSS_HUNTER, 10)
    ↓
9. characterBagMaterials에 추가
    ↓
10. 스테이지 종료
    ↓
11. StageEndItemTransfer.TransferRuneFragments()
    - "RUNE_FRAG_" 감지 ✅
    - RuneInventoryManager.AddRuneFragment("RUNE_BOSS_HUNTER", 10) ✅
    ↓
12. OnInventoryChanged 이벤트 발행
    ↓
13. RunePanelUI.RefreshUI() - UI 자동 갱신 ✅
```

---

## 🎨 시각적 확인

### 필드 드롭 시

**일반 재료 (무기 파편)**:
- 회색 아이콘 (MaterialData.icon)

**룬 조각 (보스 사냥꾼)**:
- 보스 사냥꾼 룬 아이콘 (RuneData.icon) ✅
- 색상: 흰색 (Color.white)

---

## 🧪 빠른 테스트 시나리오

### 시나리오 A: Inspector 테스트 (30초)

```
1. Play 모드 진입
2. RuneInventoryManager 우클릭
3. "테스트: 보스 사냥꾼 조각 +100"
4. 룬 패널 열기 → 조각 개수 확인 (1100개)
```

---

### 시나리오 B: DropTable 테스트 (2분)

```
1. DropTable 설정:
   - Material Type: RUNE_FRAG_RUNE_BOSS_HUNTER
   - Drop Rate: 1.0
   - Quantity: 10

2. Play 모드 → 몬스터 처치
3. 필드에 룬 아이콘 확인 ✅
4. 픽업 → Console 로그 확인
5. 스테이지 종료
6. 룬 패널 → 조각 개수 확인 (1010개)
```

---

### 시나리오 C: 복수 룬 조각 테스트 (3분)

```
DropTable 설정:
- RUNE_FRAG_RUNE_BOSS_HUNTER x10 (100%)
- RUNE_FRAG_RUNE_EXECUTIONER x5 (100%)
- RUNE_FRAG_RUNE_DEFENSE_BREAKER x8 (100%)

결과:
- 3종류 룬 아이콘이 필드에 드롭됨
- 스테이지 종료 시 3종류 모두 자동 전송
- 룬 패널에서 3개 룬의 조각 개수 증가 확인
```

---

## 📊 8종 룬 조각 매핑표

| MaterialType | Rune ID | 한글 이름 | 권장 드롭 위치 |
|--------------|---------|-----------|----------------|
| `RUNE_FRAG_RUNE_BOSS_HUNTER` | RUNE_BOSS_HUNTER | 보스 사냥꾼 | 보스 몬스터 |
| `RUNE_FRAG_RUNE_BOSS_DEFENDER` | RUNE_BOSS_DEFENDER | 보스 철벽 | 보스 몬스터 |
| `RUNE_FRAG_RUNE_DEFENSE_BREAKER` | RUNE_DEFENSE_BREAKER | 방어 파괴자 | 엘리트 몬스터 |
| `RUNE_FRAG_RUNE_HIGH_HP_HUNTER` | RUNE_HIGH_HP_HUNTER | 고체력 사냥꾼 | 보스 몬스터 |
| `RUNE_FRAG_RUNE_EXECUTIONER` | RUNE_EXECUTIONER | 처형자 | 보스 몬스터 |
| `RUNE_FRAG_RUNE_SURVIVOR` | RUNE_SURVIVOR | 불굴의 생존자 | 엘리트 몬스터 |
| `RUNE_FRAG_RUNE_AREA_DEFENDER` | RUNE_AREA_DEFENDER | 장판 철벽 | 특수 패턴 몬스터 |
| `RUNE_FRAG_RUNE_VAMPIRE` | RUNE_VAMPIRE | 흡혈 룬 | 혈귀 타입 몬스터 |

---

## 🔧 기술적 세부 사항

### MaterialPickup.cs 수정 내역

```csharp
private void UpdateIcon()
{
    // 1. 일반 재료: MaterialDatabase에서 로드 (기존)
    var materialData = MaterialDatabase.Instance?.GetData(materialType);
    if (materialData != null && materialData.icon != null)
    {
        spriteRenderer.sprite = materialData.icon;
        return;
    }
    
    // 2. 룬 조각: RuneDatabase에서 로드 (Phase 8-1)
    string matTypeName = materialType.ToString();
    if (matTypeName.StartsWith("RUNE_FRAG_"))
    {
        string runeId = matTypeName.Replace("RUNE_FRAG_", "");
        var runeData = RuneDatabase.GetRuneData(runeId);
        
        if (runeData != null && runeData.icon != null)
        {
            spriteRenderer.sprite = runeData.icon; // ★룬 아이콘 사용★
            return;
        }
    }
    
    // 3. 기본 색상 (폴백)
    spriteRenderer.color = GetDefaultColor();
}
```

**핵심**:
- ✅ MaterialDatabase에서 못 찾으면 RuneDatabase 검색
- ✅ 룬 데이터의 `icon` 사용
- ✅ 시각적 연출 코드 없음 (SO 데이터 그대로 사용)
- ✅ 기존 재료 드롭 기능 유지

---

## 🎯 DropTable 설정 예시 (복사용)

### 보스 몬스터 DropTable

```
Element 0:
- Item Type: Material
- Material Type: RUNE_FRAG_RUNE_BOSS_HUNTER
- Drop Rate: 1.0
- Min Quantity: 15
- Max Quantity: 25

Element 1:
- Item Type: Material
- Material Type: RUNE_FRAG_RUNE_EXECUTIONER
- Drop Rate: 0.5
- Min Quantity: 5
- Max Quantity: 10

Element 2:
- Item Type: Material
- Material Type: RUNE_FRAG_RUNE_DEFENSE_BREAKER
- Drop Rate: 0.3
- Min Quantity: 3
- Max Quantity: 8
```

---

### 일반 몬스터 DropTable

```
Element 0:
- Item Type: Currency
- Item ID: "ITEM_GOLD"
- Drop Rate: 1.0
- Min Quantity: 10
- Max Quantity: 30

Element 1:
- Item Type: Material
- Material Type: RUNE_FRAG_RUNE_BOSS_HUNTER
- Drop Rate: 0.15
- Min Quantity: 1
- Max Quantity: 3
```

---

## 🐛 트러블슈팅

### ⚠️ 문제 1: 룬 조각이 드롭되지 않음 (가장 흔한 문제!)
**증상**: 일반 재료는 드롭되는데 룬 조각만 드롭 안됨  
**원인**: DropTable의 itemId를 잘못 입력했거나, EnemyHealth.cs가 Phase 8-1 수정 전 버전  
**해결**:
1. **DropTable Inspector 확인**:
   - itemId 필드에 정확히 `RUNE_FRAG_RUNE_BOSS_HUNTER` 입력 (대소문자 구분!)
   - ❌ 잘못된 예: `RUNE_BOSS_HUNTER`, `MAT_RUNE_FRAG_`, `rune_frag_`
   - ✅ 올바른 예: `RUNE_FRAG_RUNE_BOSS_HUNTER`

2. **Console 로그 확인**:
   - 드롭 시도: `📦 [EnemyHealth] 재료 드롭 성공: 보스 사냥꾼 룬 조각 x10`
   - 드롭 실패: `❌ [EnemyHealth] MaterialData를 찾을 수 없습니다: RUNE_FRAG_...`
   
3. **EnemyHealth.cs 버전 확인**:
   - `SpawnSingleItem()` 메서드에 `|| itemId.StartsWith("RUNE_FRAG_")` 포함 확인
   - `SpawnMaterialItem()` 메서드에 룬 조각 분기 처리 확인

---

### 문제 2: 필드에 아이콘이 표시되지 않음
**원인**: RuneData의 icon이 null  
**해결**: RuneData ScriptableObject에서 icon 필드 설정

---

### 문제 3: 흰색 네모가 표시됨
**원인**: MaterialDatabase와 RuneDatabase 모두에서 못 찾음  
**해결**: Console 로그 확인 (`⚠️ 룬 데이터를 찾을 수 없음`)

---

### 문제 4: 픽업 후 조각이 추가되지 않음
**원인**: MaterialType Enum에 값이 없음  
**해결**: `MaterialType.cs` 200~207번 확인

---

### 문제 5: 스테이지 종료 후에도 조각 증가 안됨
**원인**: `StageEndItemTransfer.TransferRuneFragments()` 미호출  
**해결**: Phase 8-1 라우팅 로직 확인

---

## 📈 예상 플레이 경험

### 유저 입장에서의 플로우

```
1. 보스 처치
   → "룬 조각이 바닥에 떨어졌다!" (시각 확인)

2. 다가가서 자동 픽업
   → 특별한 UI 없음 (일반 재료처럼)

3. 던전 클리어
   → "전송 중..." (기존 UI)

4. 로비 복귀
   → 스킬북 > 룬 탭 열기
   → "보유: 1010 / 100" (자동 갱신됨) ✅
   → [해금] 버튼 활성화 (조각 충분)
```

---

## 🎮 밸런싱 가이드

### 해금까지 필요한 플레이 타임 (추정)

**보스 사냥꾼 해금 (100개 필요)**:

| 드롭 설정 | 몬스터 수 | 예상 시간 |
|-----------|-----------|-----------|
| 일반 몬스터 (20%, 1~3개) | 약 50마리 | 10~15분 |
| 엘리트 몬스터 (50%, 5~10개) | 약 15마리 | 5~10분 |
| 보스 몬스터 (100%, 15~25개) | 약 5마리 | 10~15분 |
| **혼합 (권장)** | 보스 3 + 일반 20 | 8~10분 |

**레벨 15까지 필요한 조각**:
- 해금: 100개
- 레벨업 (1→10): 90개 (10개 × 9)
- 한계돌파 (5회): 1000개 (200개 × 5)
- 레벨업 (10→15): 50개 (10개 × 5)
- **합계**: 1240개

**추정 플레이 타임**: 약 2~3시간 (보스 클리어 기준)

---

## 🎯 다음 단계 (Phase 8-2 예정)

1. **DropTable 실전 배치**
   - 각 던전별로 적절한 룬 조각 설정
   - 난이도별 드롭율 밸런싱

2. **획득 UI 연출**
   - 화면 중앙에 "보스 사냥꾼 조각 +10" 알림
   - 룬 아이콘 + 개수 표시

3. **상점 시스템 연동**
   - 골드로 룬 조각 구매
   - 크리스탈로 대량 구매

4. **퀘스트 보상 연동**
   - 일일 퀘스트 보상으로 룬 조각
   - 업적 보상

---

## ✅ Phase 8-1 완료!

**구현된 기능**:
- ✅ MaterialType Enum 확장 (8종 룬 조각)
- ✅ MaterialPickup 룬 조각 지원 (아이콘 로드)
- ✅ 드롭 → 픽업 → 라우팅 → UI 갱신 (완전 자동화)
- ✅ 기존 재료 시스템 정상 작동 유지

**다음 작업**: DropTable 설정 → 게임플레이 테스트 → 밸런싱! 🚀
