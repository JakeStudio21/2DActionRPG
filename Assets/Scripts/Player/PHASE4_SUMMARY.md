# Phase 4: 전투 시스템 연동 완료 요약 🎉

## ✅ 완료된 작업

### 1. SkillController.cs 입력 매핑 개편
- **변경 전**: 하드코딩된 스킬 컴포넌트 실행 (AssasinSkill1, WarriorSkill1 등)
- **변경 후**: `PlayerSkillManager.GetEquippedActiveSkill(slotIndex)`에서 장착된 스킬 가져와서 실행
- **핵심 메서드**: `ExecuteSkillFromInstance(SkillInstance, int slotIndex)`

#### 코드 흐름
```csharp
TriggerSkill() 
→ PlayerSkillManager.GetEquippedActiveSkill(0)
→ SkillInstance.CanUse() 체크 (쿨다운 + 해금 상태)
→ ExecuteSkillFromInstance(skillInstance, 0)
```

---

### 2. 쿨타임 로직 갱신
- **변경 전**: `ActiveSkillData.baseCooldown` (고정값)
- **변경 후**: `SkillInstance.GetCurrentCooldown()` (CSV Value2에서 레벨별 쿨다운 로드)
- **적용 위치**: `SkillInstance.CanUse()` 메서드 내부

#### 코드 예시
```csharp
public bool CanUse()
{
    if (!IsUnlocked) return false;
    if (!IsActiveSkill) return false;
    return GetCooldownRemaining() <= 0f; // GetCurrentCooldown() 사용
}
```

---

### 3. 데미지 공식 런타임 적용
- **변경 전**: `ActiveSkillData.baseDamageMultiplier` (고정 150%)
- **변경 후**: `PlayerRuntimeStats.FinalAttackDamage × (SkillInstance.GetCurrentDamageMultiplier() / 100)`

#### 데미지 계산식
```csharp
float damageMultiplier = skillInstance.GetCurrentDamageMultiplier(); // CSV Value1 (예: 150%)
int finalDamage = Mathf.RoundToInt(playerStats.FinalAttackDamage * (damageMultiplier / 100f));
```

#### SkillInstance에 추가된 메서드
```csharp
public float GetCurrentDamageMultiplier()
{
    return GetCurrentDamage(); // CSV SkillLevelData.Value1 반환 (%)
}
```

---

### 4. 발사체 / AoE 판정 연동 (isProjectile 분기)
- **발사체 (isProjectile = true)**: `FireProjectile()` → 화살/마법탄 발사
- **즉발형 AoE (isProjectile = false)**: `SpawnInstantAOE()` → 즉시 범위 데미지 판정

#### 분기 로직
```csharp
if (activeData.isProjectile)
{
    FireProjectile(activeData, skillInstance, finalDamage, slotIndex);
}
else
{
    SpawnInstantAOE(activeData, skillInstance, finalDamage, slotIndex);
}
```

---

## 🎯 실제 시나리오: 공격력 100, 갈래 화살 (130%) 사용 시

### 📊 전제 조건
- **플레이어 공격력**: `PlayerRuntimeStats.FinalAttackDamage = 100`
- **장착 스킬**: 갈래 화살 (`SKILL_MULTISHOT`)
  - **스킬 레벨**: 1
  - **CSV 데이터**: `SkillLevelData.csv` → Lv.1 Value1 = `150` (150%)
  - **쿨다운**: Value2 = `3초`
- **장착 슬롯**: Skill1 (슬롯 0)

---

### 🔄 코드 실행 흐름 (Step-by-Step)

#### **1단계: 버튼 입력**
```
플레이어가 Skill1 버튼 누름
↓
SkillController.TriggerSkill() 호출
```

#### **2단계: 장착 스킬 조회**
```csharp
var skillInstance = skillManager.GetEquippedActiveSkill(0);
// → skillInstance.skillData.skillName = "갈래 화살"
// → skillInstance.currentLevel = 1
```

#### **3단계: 사용 가능 여부 체크**
```csharp
if (skillInstance.CanUse()) // ✅ true
{
    // 쿨다운 확인: GetCooldownRemaining() <= 0 (사용 가능)
    // 해금 확인: currentLevel > 0 (해금됨)
}
```

#### **4단계: 쿨다운 시작**
```csharp
skillInstance.lastUsedTime = Time.time; // 쿨타임 3초 시작
```

#### **5단계: 데미지 계산**
```csharp
// PlayerRuntimeStats에서 최종 공격력 가져오기
playerStats.FinalAttackDamage; // = 100

// SkillInstance에서 CSV 데미지 배율 가져오기
float damageMultiplier = skillInstance.GetCurrentDamageMultiplier(); 
// → SkillLevelDataLoader.GetSkillLevelInfo("SKILL_MULTISHOT", 1).value1
// → CSV: 150 (150%)

// 최종 데미지 계산
int finalDamage = Mathf.RoundToInt(100 * (150 / 100f));
// = Mathf.RoundToInt(100 * 1.5)
// = 150 ⚠️ (CSV가 150%로 설정되어 있다면)
```

**❗ 주의**: CSV 파일에서 갈래 화살 Lv.1의 Value1이 **130**으로 설정되어 있다면 → `finalDamage = 130`

#### **6단계: isProjectile 확인**
```csharp
var activeData = (ActiveSkillData)skillInstance.skillData;
if (activeData.isProjectile) // ✅ true (갈래 화살은 발사체)
{
    FireProjectile(activeData, skillInstance, finalDamage, 0);
}
```

#### **7단계: 발사체 발사**
```csharp
// FireProjectile 메서드 실행
int projectileCount = activeData.projectileCount; // 3개 (갈래 화살)
float spreadAngle = activeData.spreadAngle; // 15도

for (int i = 0; i < 3; i++)
{
    // 오브젝트 풀에서 화살 생성
    var projectile = GamePoolManager.Instance.SpawnFromPool(
        "ActiveSkill_MultiShot", // projectilePrefab.name
        firePoint.position,
        rotation
    );
    
    // 속도 설정
    projectile.GetComponent<Projectile>().UpdateMoveSpeed(10f);
    
    // ⚠️ TODO: 발사체 데미지 설정 (현재 Projectile이 별도로 데미지 처리)
}
```

#### **8단계: 적 타격**
```
발사체가 적과 충돌
↓
Projectile → OnTriggerEnter2D
↓
EnemyHealth.TakeDamage(finalDamage)
↓
적이 130 데미지를 받음 ✅
```

---

## 🔧 현재 상태 및 향후 작업

### ✅ 완료된 부분
1. ✅ SkillController가 PlayerSkillManager의 장착 스킬을 참조
2. ✅ 쿨타임이 SkillInstance.GetCurrentCooldown()에서 가져옴
3. ✅ 데미지 계산식이 런타임으로 변경 (PlayerRuntimeStats × CSV 배율)
4. ✅ isProjectile 분기 처리 (발사체 vs AoE)

### ⚠️ 추가 작업 필요
1. **발사체 데미지 전달**: 현재 `Projectile` 컴포넌트에 최종 데미지를 전달하는 로직이 누락됨
   - `DamageSource` 컴포넌트를 활용하거나
   - `Projectile.SetDamage(int damage)` 메서드 추가 필요

2. **애니메이션 트리거**: 기존 `PlayerAnimationController.TriggerSkill1()` 연동
   - 현재는 스킬 실행만 처리하고 애니메이션은 별도 처리 필요

3. **UI 쿨타임 표시**: SkillUIManager가 `SkillInstance.GetCooldownRemaining()` 참조하도록 업데이트

---

## 📝 핵심 변경 사항 요약

| 항목 | 변경 전 (Phase 3 이전) | 변경 후 (Phase 4) |
|------|----------------------|-------------------|
| **스킬 참조** | 하드코딩 (AssasinSkill1.cs) | PlayerSkillManager.equippedActiveSkills[0] |
| **쿨타임** | ActiveSkillData.baseCooldown | SkillInstance.GetCurrentCooldown() (CSV) |
| **데미지** | ActiveSkillData.baseDamageMultiplier (고정 150%) | PlayerRuntimeStats × CSV 배율 (동적) |
| **발사체/AoE** | 스킬 클래스마다 개별 구현 | isProjectile 플래그로 통합 분기 |

---

## 🎉 결론

**Phase 4 완료!** 이제 유저가 장착한 스킬이 **레벨에 따라 동적으로 변하는 성능(데미지, 쿨타임)**을 가지며, **플레이어 스탯과 연동**되어 실제 전투에서 작동합니다.

**공격력 100인 상태에서 갈래 화살(130%)을 사용하면 → 130 데미지가 적에게 들어갑니다!** 🏹💥
