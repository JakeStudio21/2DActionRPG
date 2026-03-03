# 📦 Phase 8-2: MaterialDatabase 통합 가이드

**작성일**: 2026-02-28  
**목적**: 룬 조각을 MaterialDatabase에 추가하여 일반 재료와 동일하게 처리

---

## 🎯 목표

**현재 상태** (Phase 8-1):
```csharp
// ❌ 분기 처리 필요
if (itemId.StartsWith("RUNE_FRAG_"))
{
    // 룬 조각: MaterialType Enum에서 직접 변환
    materialType = MaterialTypeExtensions.FromItemId(itemId);
}
else
{
    // 일반 재료: MaterialDatabase 사용
    MaterialData materialData = MaterialDatabase.Instance?.GetDataById(itemId);
    materialType = materialData.materialType;
}
```

**목표 상태** (Phase 8-2):
```csharp
// ✅ 일관된 처리
MaterialData materialData = MaterialDatabase.Instance?.GetDataById(itemId);
materialType = materialData.materialType;
```

---

## 📋 작업 단계

### Step 1: MaterialData ScriptableObject 8개 생성

#### **Unity Editor 작업**

1. **Project 창에서 폴더 이동**:
   ```
   Assets/Resources/Data/Materials/
   ```
   (폴더가 없으면 생성)

2. **첫 번째 룬 조각 생성**:
   - 우클릭 → `Create` → `Data` → `Material`
   - 이름: `RUNE_FRAG_RUNE_BOSS_HUNTER`

3. **Inspector 설정** (보스 사냥꾼 조각):
   ```
   🔑 기본 정보
   ├─ Material Id: "RUNE_FRAG_RUNE_BOSS_HUNTER"
   ├─ Material Type: RUNE_FRAG_RUNE_BOSS_HUNTER (드롭다운에서 선택)
   ├─ Display Name: "보스 사냥꾼 룬 조각"
   └─ Icon: (RuneData의 아이콘과 동일한 것 설정)

   📝 설명
   ├─ Description: "보스 사냥꾼 룬을 해금하고 강화할 때 사용하는 조각입니다.\n보스 몬스터를 처치하면 획득할 수 있습니다."
   ├─ Usage Hint: "보스 사냥꾼 룬 해금/강화 시 사용"
   └─ Obtain Hint: "보스 몬스터 처치"

   ⚙️ 게임 설정
   ├─ Max Stack Size: 9999
   ├─ Can Drop: ✅
   ├─ Rarity: Rare (파란색 - 룬 조각은 희귀)
   └─ Sort Order: 100 (일반 재료 다음에 표시)
   ```

4. **나머지 7개 복사**:
   - `RUNE_FRAG_RUNE_BOSS_HUNTER` 선택
   - `Ctrl+D` (Duplicate) 7번
   - 각각 이름 변경 및 설정 수정

---

### Step 2: 8개 룬 조각 설정표

| 파일 이름 | Material Id | Display Name | Icon | Sort Order |
|-----------|-------------|--------------|------|------------|
| `RUNE_FRAG_RUNE_BOSS_HUNTER` | `RUNE_FRAG_RUNE_BOSS_HUNTER` | 보스 사냥꾼 룬 조각 | (RuneData 아이콘) | 100 |
| `RUNE_FRAG_RUNE_BOSS_DEFENDER` | `RUNE_FRAG_RUNE_BOSS_DEFENDER` | 보스 철벽 룬 조각 | (RuneData 아이콘) | 101 |
| `RUNE_FRAG_RUNE_DEFENSE_BREAKER` | `RUNE_FRAG_RUNE_DEFENSE_BREAKER` | 방어 파괴자 룬 조각 | (RuneData 아이콘) | 102 |
| `RUNE_FRAG_RUNE_HIGH_HP_HUNTER` | `RUNE_FRAG_RUNE_HIGH_HP_HUNTER` | 고체력 사냥꾼 룬 조각 | (RuneData 아이콘) | 103 |
| `RUNE_FRAG_RUNE_EXECUTIONER` | `RUNE_FRAG_RUNE_EXECUTIONER` | 처형자 룬 조각 | (RuneData 아이콘) | 104 |
| `RUNE_FRAG_RUNE_SURVIVOR` | `RUNE_FRAG_RUNE_SURVIVOR` | 불굴의 생존자 룬 조각 | (RuneData 아이콘) | 105 |
| `RUNE_FRAG_RUNE_AREA_DEFENDER` | `RUNE_FRAG_RUNE_AREA_DEFENDER` | 장판 철벽 룬 조각 | (RuneData 아이콘) | 106 |
| `RUNE_FRAG_RUNE_VAMPIRE` | `RUNE_FRAG_RUNE_VAMPIRE` | 흡혈 룬 조각 | (RuneData 아이콘) | 107 |

**공통 설정**:
- Rarity: `Rare` (파란색)
- Can Drop: `✅`
- Max Stack Size: `9999`
- Description: `"{룬 이름} 룬을 해금하고 강화할 때 사용하는 조각입니다.\n보스 몬스터를 처치하면 획득할 수 있습니다."`

---

### Step 3: MaterialDatabase에 추가

1. **MaterialDatabase.asset 열기**:
   ```
   Assets/Resources/Data/MaterialDatabase.asset
   ```

2. **Inspector에서 Materials 배열 확장**:
   ```
   Materials (Size: 9 → 17)
   ```

3. **8개의 룬 조각 MaterialData 드래그**:
   - Element 9: `RUNE_FRAG_RUNE_BOSS_HUNTER`
   - Element 10: `RUNE_FRAG_RUNE_BOSS_DEFENDER`
   - Element 11: `RUNE_FRAG_RUNE_DEFENSE_BREAKER`
   - Element 12: `RUNE_FRAG_RUNE_HIGH_HP_HUNTER`
   - Element 13: `RUNE_FRAG_RUNE_EXECUTIONER`
   - Element 14: `RUNE_FRAG_RUNE_SURVIVOR`
   - Element 15: `RUNE_FRAG_RUNE_AREA_DEFENDER`
   - Element 16: `RUNE_FRAG_RUNE_VAMPIRE`

4. **저장**: `Ctrl+S`

---

### Step 4: 코드 단순화

#### **4-1. EnemyHealth.cs 수정**

**Before (Phase 8-1)**:
```csharp
private void SpawnMaterialItem(string itemId, Vector3 spawnPosition)
{
    MaterialType materialType;
    
    // 룬 조각: MaterialType Enum에서 직접 변환
    if (itemId.StartsWith("RUNE_FRAG_"))
    {
        materialType = MaterialTypeExtensions.FromItemId(itemId);
        Debug.Log($"💎 [EnemyHealth] 룬 조각 드롭 준비: {itemId}");
    }
    // 일반 재료: MaterialDatabase에서 검색
    else
    {
        MaterialData materialData = MaterialDatabase.Instance?.GetDataById(itemId);
        if (materialData == null) return;
        materialType = materialData.materialType;
    }
    
    // ... 스폰 로직
}
```

**After (Phase 8-2)**:
```csharp
private void SpawnMaterialItem(string itemId, Vector3 spawnPosition)
{
    // ✅ 일반 재료 & 룬 조각 통합 처리 (Phase 8-2)
    MaterialData materialData = MaterialDatabase.Instance?.GetDataById(itemId);
    
    if (materialData == null)
    {
        Debug.LogError($"❌ [EnemyHealth] MaterialData를 찾을 수 없습니다: {itemId}");
        return;
    }
    
    MaterialType materialType = materialData.materialType;
    
    // Drop_Material 프리팹 스폰
    // ... (기존 로직 동일)
}
```

---

#### **4-2. MaterialPickup.cs 수정 (선택)**

**Option A: 완전 통합 (권장)**

MaterialData에 아이콘을 설정했으므로, RuneDatabase 분기 제거 가능:

```csharp
private void UpdateIcon()
{
    // ✅ 모든 재료 (일반 + 룬 조각) 통합 처리
    var materialData = MaterialDatabase.Instance?.GetData(materialType);
    
    if (materialData != null && materialData.icon != null && spriteRenderer != null)
    {
        spriteRenderer.sprite = materialData.icon;
        spriteRenderer.color = Color.white;
        return;
    }
    
    // 폴백: 기본 색상
    if (spriteRenderer != null)
    {
        Color iconColor = materialType.GetMaterialGrade() switch
        {
            "파편" => new Color(0.7f, 0.7f, 0.7f),
            "결정" => new Color(0.3f, 0.9f, 0.3f),
            "코어" => new Color(0.3f, 0.6f, 1f),
            _ => Color.white
        };
        spriteRenderer.color = iconColor;
    }
}
```

**Option B: 하이브리드 (유연성)**

MaterialData를 우선 확인하고, 없으면 RuneDatabase 확인 (현재 로직 유지):
- 장점: 아이콘 변경 시 RuneData만 수정하면 자동 반영
- 단점: 약간의 분기 처리 남음

---

## 🧪 테스트

### Test 1: MaterialDatabase 확인

**Play 모드에서**:
1. `F12` → Console 창
2. 입력:
   ```csharp
   MaterialDatabase.Instance.GetDataById("RUNE_FRAG_RUNE_BOSS_HUNTER")
   ```
3. **예상 결과**: MaterialData 객체 반환

---

### Test 2: 드롭 테스트

**DropTable 설정**:
```
Item Id: "RUNE_FRAG_RUNE_BOSS_HUNTER"
Chance: 1.0
Min/Max Quantity: 10
```

**실행**:
1. Play 모드 진입
2. 몬스터 처치
3. **예상 로그**:
   ```
   📦 [EnemyHealth] 재료 드롭 성공: 보스 사냥꾼 룬 조각 x10
   ```
   (💎 룬 조각 로그가 사라짐 - 일반 재료와 동일 처리)

---

### Test 3: UI 확인

**인벤토리 UI**:
- 일반 재료와 룬 조각이 함께 표시됨
- Sort Order에 따라 정렬 (일반 재료 → 룬 조각)
- 아이콘, 이름, 개수 정상 표시

---

## ✅ 완료 체크리스트

### Unity Editor 작업
- [ ] MaterialData ScriptableObject 8개 생성
- [ ] 각 MaterialData의 Inspector 설정 완료
- [ ] MaterialDatabase.asset에 8개 추가
- [ ] 저장 (Ctrl+S)

### 코드 수정
- [ ] `EnemyHealth.cs` - SpawnMaterialItem() 단순화
- [ ] `MaterialPickup.cs` - UpdateIcon() 단순화 (선택)

### 테스트
- [ ] MaterialDatabase.GetDataById() 테스트
- [ ] 드롭 테스트 (필드에 룬 조각 표시)
- [ ] 픽업 및 스테이지 종료 후 조각 증가 확인
- [ ] 룬 패널에서 조각 개수 확인

---

## 🔄 Before / After 비교

### **드롭 로직**

**Before (Phase 8-1)**:
```
itemId = "RUNE_FRAG_RUNE_BOSS_HUNTER"
  ↓
"RUNE_FRAG_" 감지
  ↓
MaterialTypeExtensions.FromItemId() ★특수 처리★
  ↓
MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER
```

**After (Phase 8-2)**:
```
itemId = "RUNE_FRAG_RUNE_BOSS_HUNTER"
  ↓
MaterialDatabase.GetDataById() ★일반 재료와 동일★
  ↓
MaterialData → MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER
```

---

### **아이콘 로드**

**Before (Phase 8-1)**:
```
MaterialDatabase.GetData() → null
  ↓
"RUNE_FRAG_" 감지
  ↓
RuneDatabase.GetRuneData() ★특수 처리★
  ↓
runeData.icon
```

**After (Phase 8-2)**:
```
MaterialDatabase.GetData()
  ↓
materialData.icon ★일반 재료와 동일★
```

---

## 📊 장단점 비교

### **Phase 8-2 (MaterialDatabase 통합) 장점**
- ✅ 코드 일관성 증가 (분기 제거)
- ✅ 인벤토리 UI에서 자동 표시
- ✅ 재료 관련 기능 자동 지원 (정렬, 필터링 등)
- ✅ 디자이너가 Inspector에서 쉽게 수정 가능

### **Phase 8-2 단점**
- ❌ 초기 작업량 증가 (ScriptableObject 8개 생성)
- ❌ 아이콘 중복 관리 (RuneData와 MaterialData 모두 설정)

### **Phase 8-1 (현재 상태) 장점**
- ✅ 초기 구현 빠름 (Enum만 추가)
- ✅ 아이콘 단일 관리 (RuneData만)

### **Phase 8-1 단점**
- ❌ 코드 분기 처리 필요
- ❌ 재료 관련 기능 수동 구현 필요

---

## 💡 권장 사항

**지금 바로 Phase 8-2 진행 권장!** 🚀

**이유**:
1. **향후 확장성**: 룬 조각이 추가되면 MaterialData만 생성하면 됨
2. **UI 통합**: 인벤토리 재료 탭에서 자동 표시
3. **상점 연동**: MaterialDatabase 기반 상점 시스템과 호환
4. **밸런싱**: Inspector에서 드롭율, 설명 등 쉽게 수정

**작업 시간**: 약 15~20분 (Unity Editor 작업 위주)

---

## 🎯 다음 단계

Phase 8-2 완료 후:
1. **재료 인벤토리 UI 통합**: 일반 재료와 룬 조각 함께 표시
2. **상점 시스템 연동**: 골드로 룬 조각 구매
3. **퀘스트 보상 연동**: 일일 퀘스트 보상으로 룬 조각
4. **Drop Rate 밸런싱**: 실제 게임플레이 기반 조정

---

**✅ Phase 8-2 완료 후 코드가 훨씬 깔끔해집니다!** 🎉
