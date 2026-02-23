# ⚔️ 전투 공식 설계 (Combat Formula Design)

**프로젝트**: Unity 2D Action RPG  
**작성일**: 2026-02-13  
**버전**: 1.0 - 최소 확장 가능 뼈대  
**목적**: 이후 스킬/룬/고급 옵션 추가를 위한 표준 데미지 계산 구조

---

## 📋 목차

1. [전투 공식 전체 흐름](#1-전투-공식-전체-흐름)
2. [Phase 1: 현재 구현 (기본)](#2-phase-1-현재-구현-기본)
3. [Phase 2: 확장 단계 (미구현)](#3-phase-2-확장-단계-미구현)
4. [단계별 계산 순서](#4-단계별-계산-순서)
5. [확장 포인트](#5-확장-포인트)
6. [구현 가이드](#6-구현-가이드)

---

## 1. 전투 공식 전체 흐름

### 1.1 최종 데미지 계산 다이어그램 (Full)

```
┌─────────────────────────────────────────────────────────────┐
│                    공격자 단계 (Attacker Phase)              │
└─────────────────────────────────────────────────────────────┘

Step 1: 기본 공격력 (Base Attack)
┌──────────────────────────────────────┐
│  기본 공격력 = 무기 공격력 + 레벨 보너스 + 장비 추가 공격력  │
│  예: 100 = 50(무기) + 30(레벨) + 20(장비)                    │
└──────────────────────────────────────┘
                ↓
Step 2: 스킬 배율 적용 (Skill Multiplier) [🔮 확장 예정]
┌──────────────────────────────────────┐
│  공격력 = 기본 공격력 × 스킬 배율     │
│  예: 150 = 100 × 1.5 (스킬 150%)      │
│  ⚠️ 기본 공격: 스킬 배율 = 1.0       │
└──────────────────────────────────────┘
                ↓
Step 3: 증가 보너스 (Additive Bonus) [🔮 확장 예정]
┌──────────────────────────────────────┐
│  공격력 = 공격력 + 고정 추가 데미지   │
│  예: 170 = 150 + 20 (버프/룬 옵션)    │
│  • 버프: +10                          │
│  • 룬 옵션: +10                       │
└──────────────────────────────────────┘
                ↓
Step 4: 증폭 배율 (Multiplicative Multiplier) [✅ 부분 구현]
┌──────────────────────────────────────┐
│  공격력 = 공격력 × (1 + 증폭률 합계) │
│  예: 204 = 170 × (1 + 0.2)            │
│  • 클래스 배율: +10%                  │
│  • 버서커 모드: +10% [Warrior]        │
│  • [🔮 확장] 룬 옵션: +5%            │
└──────────────────────────────────────┘
                ↓
Step 5: 백어택 보너스 (Back Attack) [✅ 구현 - Assasin]
┌──────────────────────────────────────┐
│  공격력 = 공격력 × 백어택 배율        │
│  예: 306 = 204 × 1.5 (뒤에서 공격)    │
│  • Assasin 전용                       │
└──────────────────────────────────────┘
                ↓
Step 6: 크리티컬 판정 (Critical Hit) [✅ 부분 구현]
┌──────────────────────────────────────┐
│  if (Random < 크리티컬 확률)          │
│    공격력 = 공격력 × 크리티컬 배율    │
│  예: 459 = 306 × 1.5 (크리티컬!)      │
│  • 크리티컬 확률: 장비 기반            │
│  • 크리티컬 배율: 1.5 (기본)          │
└──────────────────────────────────────┘
                ↓
Step 7: 최종 공격력 확정 (Final Attack Power)
┌──────────────────────────────────────┐
│  최종 공격력 = 459                    │
└──────────────────────────────────────┘

                ↓ ↓ ↓

┌─────────────────────────────────────────────────────────────┐
│                    방어자 단계 (Defender Phase)              │
└─────────────────────────────────────────────────────────────┘

Step 8: 방어력 계산 (Defense Calculation) [⚠️ 미적용]
┌──────────────────────────────────────┐
│  방어율 = 방어력 / (방어력 + 100)     │
│  예: 0.2 = 20 / (20 + 100)            │
│  • 방어력 20 → 20% 감소               │
│  • 방어력 100 → 50% 감소              │
│  • 방어력 400 → 80% 감소              │
└──────────────────────────────────────┘
                ↓
Step 9: 관통 적용 (Penetration) [🔮 확장 예정]
┌──────────────────────────────────────┐
│  실제 방어율 = 방어율 × (1 - 관통률) │
│  예: 0.1 = 0.2 × (1 - 0.5)            │
│  • 관통률 50% → 방어력 절반 무시      │
│  • [🔮 확장] 룬 옵션: 관통 +10%      │
└──────────────────────────────────────┘
                ↓
Step 10: 방어력 감소 적용 (Defense Reduction)
┌──────────────────────────────────────┐
│  데미지 = 최종 공격력 × (1 - 실제 방어율) │
│  예: 413 = 459 × (1 - 0.1)            │
└──────────────────────────────────────┘
                ↓
Step 11: 블록 판정 (Block) [✅ 구현 - Warrior]
┌──────────────────────────────────────┐
│  if (Random < 블록 확률)              │
│    데미지 = 데미지 × (1 - 블록 감소율) │
│  예: 207 = 413 × (1 - 0.5)            │
│  • Warrior 전용                       │
│  • 블록 확률: 20%                     │
│  • 블록 감소: 50%                     │
└──────────────────────────────────────┘
                ↓
Step 12: 회피 판정 (Evasion) [🔮 확장 예정]
┌──────────────────────────────────────┐
│  if (Random < 회피 확률)              │
│    데미지 = 0 (회피!)                 │
│  • [🔮 확장] Assasin 회피: 10%       │
└──────────────────────────────────────┘
                ↓
Step 13: 최종 데미지 확정 (Final Damage)
┌──────────────────────────────────────┐
│  최종 데미지 = 207                    │
│  • 반올림 처리                        │
│  • 최소 데미지: 1                     │
└──────────────────────────────────────┘

                ↓ ↓ ↓

┌─────────────────────────────────────────────────────────────┐
│                    추가 효과 단계 (Additional Effects)       │
└─────────────────────────────────────────────────────────────┘

Step 14: 추가 효과 적용 [🔮 확장 예정]
┌──────────────────────────────────────┐
│  • 생명력 흡수 (Life Steal)           │
│  • 상태 이상 (독, 출혈, 둔화 등)      │
│  • 연쇄 공격 (Chain Attack)           │
│  • 속성 데미지 (화염, 얼음 등)        │
│  • 반사 데미지 (Reflect)              │
│  • [🔮 확장] 룬 옵션 효과            │
└──────────────────────────────────────┘

                ↓ ↓ ↓

┌─────────────────────────────────────────────────────────────┐
│                    피드백 (Feedback)                         │
└─────────────────────────────────────────────────────────────┘

• 데미지 넘버 표시
• 이펙트 (크리티컬, 회피, 블록)
• 사운드
• 카메라 쉐이크
```

---

## 2. Phase 1: 현재 구현 (기본)

### 2.1 현재 적용된 공식 (✅ 구현 완료)

```
┌─────────────────────────────────────────────────────────────┐
│  플레이어 → 몬스터 데미지                                    │
└─────────────────────────────────────────────────────────────┘

최종 데미지 = PlayerRuntimeStats.FinalAttackDamage
            × ClassMultiplier (클래스 배율)
            × BackAttackBonus (백어택, Assasin)
            × CriticalMultiplier (크리티컬)

예시:
기본 공격력: 100
  ↓ Assasin 클래스 배율 (×1.2)
120
  ↓ 백어택 보너스 (×1.5)
180
  ↓ 크리티컬 판정 (×1.5)
270 (최종 데미지)

⚠️ 방어력 미적용 (현재 구현되지 않음)
```

```
┌─────────────────────────────────────────────────────────────┐
│  몬스터 → 플레이어 데미지                                    │
└─────────────────────────────────────────────────────────────┘

최종 데미지 = AttackData.GetScaledDamage(level)
            × CriticalMultiplier (크리티컬)
            × BlockReduction (블록, Warrior)

예시:
기본 공격력: 15 (Lv.5 몬스터)
  ↓ 크리티컬 판정 (×2.0)
30
  ↓ Warrior 블록 판정 (×0.5)
15 (최종 데미지)

⚠️ 방어력 미적용 (현재 구현되지 않음)
```

### 2.2 현재 구현 위치 (Combat Logic Layer)

**파일: `DamageSource.cs`**
```csharp
private void OnTriggerEnter2D(Collider2D other) 
{
    // Step 1: 기본 공격력
    float baseDamage = GetCurrentBaseDamage(); // PlayerRuntimeStats
    
    // Step 4: 클래스 특수 효과 (증폭 배율 + 백어택 + 크리티컬)
    float finalDamage = ApplyClassSpecialEffects(baseDamage, baseDamage, other);
    
    // Step 13: 최종 데미지 적용 (방어력 미적용!)
    int roundedDamage = Mathf.RoundToInt(finalDamage);
    enemyHealth.TakeDamage(roundedDamage);
}
```

**파일: `MeleeAttack.cs` (몬스터 → 플레이어)**
```csharp
private void ApplyDamageToPlayer(Collider2D playerCollider)
{
    // Step 1: 기본 공격력 (레벨 스케일링)
    int damage = GetScaledDamage(); // AttackData
    
    // Step 6: 크리티컬 판정
    if (RollCriticalHit())
        damage = GetCriticalDamage(damage);
    
    // Step 11: 블록 판정 (PlayerHealth에서 처리)
    playerHealth.TakeDamage(damage, transform);
}
```

---

## 3. Phase 2: 확장 단계 (미구현)

### 3.1 확장 예정 요소

| 단계 | 요소 | 상태 | 우선순위 | 예상 구현 위치 |
|------|------|------|----------|---------------|
| Step 2 | **스킬 배율** | 🔮 미구현 | P1 | SkillData.damageMultiplier |
| Step 3 | **증가 보너스** | 🔮 미구현 | P2 | BuffSystem, RuneSystem |
| Step 8 | **방어력 감소** | ⚠️ 미적용 | P0 | PlayerRuntimeStats.FinalDefense |
| Step 9 | **관통** | 🔮 미구현 | P2 | RuneSystem.penetration |
| Step 12 | **회피** | 🔮 미구현 | P2 | Assasin.dodgeChance |
| Step 14 | **추가 효과** | 🔮 미구현 | P3 | RuneSystem, StatusEffectSystem |

**우선순위:**
- **P0 (긴급)**: 방어력 시스템 (데이터는 있으나 미적용)
- **P1 (높음)**: 스킬 배율 (스킬 데미지 조정)
- **P2 (중간)**: 관통, 회피, 증가 보너스
- **P3 (낮음)**: 추가 효과 (룬 시스템 연동)

---

## 4. 단계별 계산 순서

### 4.1 전체 계산 순서 표

| 단계 | 이름 | 공식 | 현재 구현 | 확장 예정 |
|------|------|------|----------|----------|
| **공격자 단계** | | | | |
| 1 | 기본 공격력 | 무기 + 레벨 + 장비 | ✅ PlayerRuntimeStats | - |
| 2 | 스킬 배율 | 공격력 × 배율 | ❌ | 🔮 SkillData |
| 3 | 증가 보너스 | 공격력 + 고정값 | ❌ | 🔮 BuffSystem, RuneSystem |
| 4 | 증폭 배율 | 공격력 × (1 + %합계) | ✅ 클래스 배율 | 🔮 룬 옵션 추가 |
| 5 | 백어택 | 공격력 × 배율 | ✅ Assasin | - |
| 6 | 크리티컬 | 공격력 × 배율 | ✅ 장비 기반 | - |
| 7 | 최종 공격력 확정 | - | ✅ | - |
| **방어자 단계** | | | | |
| 8 | 방어력 계산 | 방어력 / (방어력 + 100) | ⚠️ 미적용 | 🔧 적용 필요 |
| 9 | 관통 | 방어율 × (1 - 관통률) | ❌ | 🔮 RuneSystem |
| 10 | 방어력 감소 | 공격력 × (1 - 방어율) | ⚠️ 미적용 | 🔧 적용 필요 |
| 11 | 블록 판정 | 데미지 × (1 - 블록률) | ✅ Warrior | - |
| 12 | 회피 판정 | 데미지 = 0 (회피 시) | ❌ | 🔮 Assasin |
| 13 | 최종 데미지 확정 | 반올림 + 최소값 1 | ✅ | - |
| **추가 효과** | | | | |
| 14 | 추가 효과 | 다양한 효과 | ❌ | 🔮 RuneSystem |

**범례:**
- ✅ 구현 완료
- ⚠️ 데이터 존재하나 미적용
- ❌ 미구현
- 🔮 확장 예정
- 🔧 적용 필요

---

## 5. 확장 포인트

### 5.1 단계별 확장 포인트 상세

#### **Step 2: 스킬 배율 (🔮 확장 예정)**

**목적:** 스킬마다 다른 데미지 배율 적용

**확장 방법:**
```csharp
// SkillData.cs에 추가
[Header("스킬 데미지")]
public float damageMultiplier = 1.5f; // 기본 공격의 150%

// DamageSource.cs에서 적용
float baseDamage = GetCurrentBaseDamage();

// 🔮 Step 2: 스킬 배율 적용 (확장 예정)
if (isSkillAttack && skillData != null)
{
    baseDamage *= skillData.damageMultiplier;
}

float finalDamage = ApplyClassSpecialEffects(baseDamage, ...);
```

**예상 사용처:**
- AssasinSkill1 (Multi Arrow): 기본 공격의 80% × 5발
- AssasinSkill2 (Power Arrow): 기본 공격의 200%
- WarriorSkill1 (Dash): 기본 공격의 150%
- WarriorSkill2 (Ground Slam): 기본 공격의 180%

---

#### **Step 3: 증가 보너스 (🔮 확장 예정)**

**목적:** 고정값 추가 데미지 (버프, 룬 옵션)

**확장 방법:**
```csharp
// BuffSystem.cs 신규 생성
public class BuffSystem
{
    public int GetAdditiveAttackBonus()
    {
        int bonus = 0;
        // 버프 합산
        foreach (var buff in activeBuffs)
        {
            bonus += buff.additiveAttack;
        }
        return bonus;
    }
}

// RuneSystem.cs 신규 생성
public class RuneSystem
{
    public int GetAdditiveAttackBonus()
    {
        int bonus = 0;
        // 룬 옵션 합산
        foreach (var rune in equippedRunes)
        {
            bonus += rune.additiveAttack;
        }
        return bonus;
    }
}

// DamageSource.cs에서 적용
float baseDamage = GetCurrentBaseDamage();
baseDamage *= skillMultiplier; // Step 2

// 🔮 Step 3: 증가 보너스 (확장 예정)
int additiveBonus = 0;
if (BuffSystem.Instance != null)
    additiveBonus += BuffSystem.Instance.GetAdditiveAttackBonus();
if (RuneSystem.Instance != null)
    additiveBonus += RuneSystem.Instance.GetAdditiveAttackBonus();

baseDamage += additiveBonus;
```

**예상 사용처:**
- 버프 아이템: 공격력 +20 (30초)
- 룬 옵션: "칼날 룬" - 공격력 +15
- 파티 버프: 공격력 +10% (고정값)

---

#### **Step 8~10: 방어력 시스템 (🔧 적용 필요 - 최우선)**

**목적:** 현재 미적용된 방어력을 실제 데미지 감소에 적용

**적용 방법:**
```csharp
// DamageSource.cs 수정
private void OnTriggerEnter2D(Collider2D other) 
{
    // ... Step 1~7 (공격력 계산)
    float finalAttackPower = ApplyClassSpecialEffects(baseDamage, ...);
    
    // 🔧 Step 8~10: 방어력 감소 적용 (신규 추가)
    float finalDamage = ApplyDefenseReduction(finalAttackPower, other);
    
    int roundedDamage = Mathf.RoundToInt(finalDamage);
    enemyHealth.TakeDamage(roundedDamage);
}

// 신규 메서드
private float ApplyDefenseReduction(float attackPower, Collider2D target)
{
    // Step 8: 방어력 가져오기
    float defense = 0f;
    
    var enemyHealth = target.GetComponent<EnemyHealth>();
    if (enemyHealth != null && enemyHealth.EnemyData != null)
    {
        defense = enemyHealth.EnemyData.defense; // EnemyData에 추가 필요
    }
    
    // Step 9: 관통 적용 (🔮 확장 예정)
    float penetrationRate = 0f; // 나중에 RuneSystem에서 가져옴
    float effectiveDefense = defense * (1f - penetrationRate);
    
    // Step 10: 방어력 감소 공식
    // 방어율 = 방어력 / (방어력 + 100)
    float defenseRate = effectiveDefense / (effectiveDefense + 100f);
    float finalDamage = attackPower * (1f - defenseRate);
    
    return finalDamage;
}
```

**방어력 공식 예시:**
```
방어력 0   → 0% 감소
방어력 20  → 16.7% 감소
방어력 50  → 33.3% 감소
방어력 100 → 50% 감소
방어력 200 → 66.7% 감소
방어력 400 → 80% 감소
```

**몬스터 → 플레이어 방어력 적용:**
```csharp
// PlayerHealth.cs 수정
public void TakeDamage(int damageAmount, Transform hitTransform)
{
    // 🔧 방어력 감소 적용 (신규 추가)
    float defense = playerRuntimeStats != null ? 
                    playerRuntimeStats.FinalDefense : 0f;
    
    float defenseRate = defense / (defense + 100f);
    float reducedDamage = damageAmount * (1f - defenseRate);
    
    int finalDamage = Mathf.RoundToInt(reducedDamage);
    finalDamage = Mathf.Max(1, finalDamage); // 최소 1 데미지
    
    // Step 11: Warrior 블록 판정
    if (warrior != null && warrior.TryBlock())
    {
        finalDamage = Mathf.RoundToInt(finalDamage * 0.5f);
    }
    
    currentHealth -= finalDamage;
    // ... 피드백
}
```

---

#### **Step 9: 관통 (🔮 확장 예정)**

**목적:** 방어력 무시 옵션 (룬 시스템)

**확장 방법:**
```csharp
// RuneSystem.cs에 추가
public class RuneSystem
{
    public float GetPenetrationRate()
    {
        float penetration = 0f;
        foreach (var rune in equippedRunes)
        {
            penetration += rune.penetrationRate; // 0.1 = 10% 관통
        }
        return Mathf.Clamp01(penetration); // 최대 100%
    }
}

// DamageSource.cs에서 적용
private float ApplyDefenseReduction(float attackPower, Collider2D target)
{
    float defense = GetTargetDefense(target);
    
    // 🔮 Step 9: 관통 적용 (확장 예정)
    float penetrationRate = 0f;
    if (RuneSystem.Instance != null)
        penetrationRate = RuneSystem.Instance.GetPenetrationRate();
    
    float effectiveDefense = defense * (1f - penetrationRate);
    float defenseRate = effectiveDefense / (effectiveDefense + 100f);
    
    return attackPower * (1f - defenseRate);
}
```

**예상 사용처:**
- 룬 옵션: "파쇄 룬" - 관통 +10%
- 스킬 옵션: "진격 스킬" - 관통 +20% (일시)
- 장비 옵션: "용검" - 관통 +15%

---

#### **Step 12: 회피 (🔮 확장 예정)**

**목적:** 확률 기반 데미지 완전 회피

**확장 방법:**
```csharp
// PlayerHealth.cs에 추가
public void TakeDamage(int damageAmount, Transform hitTransform)
{
    // 🔮 Step 12: 회피 판정 (확장 예정)
    if (assasin != null)
    {
        float dodgeChance = assasin.DodgeChance; // 0.1 = 10%
        if (Random.Range(0f, 1f) < dodgeChance)
        {
            // 회피 성공!
            if (showDebugLogs)
                Debug.Log("✨ [PlayerHealth] 회피 성공!");
            
            // 회피 이펙트
            CueEmitter.Emit("player.dodge", "Player", new CueContext { ... });
            
            return; // 데미지 0
        }
    }
    
    // ... 방어력, 블록 판정 진행
}
```

**예상 사용처:**
- Assasin 클래스: 기본 회피 10%
- 장비 옵션: "그림자 망토" - 회피 +5%
- 버프: "민첩성 포션" - 회피 +10% (30초)

---

#### **Step 14: 추가 효과 (🔮 확장 예정)**

**목적:** 데미지 외 다양한 효과 적용

**확장 방법:**
```csharp
// DamageSource.cs에 추가
private void OnTriggerEnter2D(Collider2D other) 
{
    // ... Step 1~13 (데미지 적용)
    enemyHealth.TakeDamage(roundedDamage);
    
    // 🔮 Step 14: 추가 효과 적용 (확장 예정)
    ApplyAdditionalEffects(roundedDamage, other);
}

private void ApplyAdditionalEffects(int damage, Collider2D target)
{
    // 생명력 흡수 (Life Steal)
    if (RuneSystem.Instance != null)
    {
        float lifeStealRate = RuneSystem.Instance.GetLifeStealRate();
        if (lifeStealRate > 0f)
        {
            int healAmount = Mathf.RoundToInt(damage * lifeStealRate);
            PlayerHealth.Instance.Heal(healAmount);
        }
    }
    
    // 상태 이상 적용 (Poison, Bleed, Slow)
    if (RuneSystem.Instance != null)
    {
        var statusEffects = RuneSystem.Instance.GetOnHitStatusEffects();
        foreach (var effect in statusEffects)
        {
            if (Random.Range(0f, 1f) < effect.chance)
            {
                target.GetComponent<StatusEffectManager>()?.ApplyEffect(effect);
            }
        }
    }
    
    // 연쇄 공격 (Chain Attack)
    if (RuneSystem.Instance != null && RuneSystem.Instance.HasChainAttack())
    {
        var nearbyEnemies = GetNearbyEnemies(target.transform.position, 5f);
        if (nearbyEnemies.Count > 0)
        {
            int chainDamage = Mathf.RoundToInt(damage * 0.5f);
            nearbyEnemies[0].TakeDamage(chainDamage);
        }
    }
}
```

**예상 사용처:**
- 생명력 흡수: "흡혈 룬" - 데미지의 5% 회복
- 상태 이상: "독 룬" - 30% 확률로 독 부여 (3초간 10데미지)
- 연쇄 공격: "번개 룬" - 50% 데미지로 주변 1명 추가 타격
- 속성 데미지: "화염 룬" - 화염 속성 20% 추가 데미지

---

### 5.2 확장 포인트 요약표

| 확장 포인트 | 위치 | 추가 필요 클래스 | 우선순위 |
|-----------|------|-----------------|---------|
| **Step 2: 스킬 배율** | DamageSource.cs | SkillData.damageMultiplier | P1 |
| **Step 3: 증가 보너스** | DamageSource.cs | BuffSystem, RuneSystem | P2 |
| **Step 8~10: 방어력** | DamageSource.cs, PlayerHealth.cs | - (데이터 존재) | P0 |
| **Step 9: 관통** | DamageSource.cs | RuneSystem.penetration | P2 |
| **Step 12: 회피** | PlayerHealth.cs | - (Assasin 데이터 존재) | P2 |
| **Step 14: 추가 효과** | DamageSource.cs | RuneSystem, StatusEffectManager | thompsonP3 |

---

## 6. 구현 가이드

### 6.1 Phase 0: 방어력 시스템 적용 (즉시 구현 권장)

**목표:** 현재 미적용된 방어력을 실제 전투에 적용

**작업 목록:**
1. ✅ `EnemyData.cs`에 `defense` 필드 추가 (이미 존재할 수도 있음)
2. 🔧 `DamageSource.cs`에 `ApplyDefenseReduction()` 메서드 추가
3. 🔧 `PlayerHealth.cs`에 방어력 감소 로직 추가
4. ✅ 테스트: 방어력 0/20/50/100 몬스터 데미지 차이 확인

**예상 소요 시간:** 1~2시간

---

### 6.2 Phase 1: 스킬 배율 시스템 (다음 구현 권장)

**목표:** 스킬마다 다른 데미지 배율 적용

**작업 목록:**
1. 🔮 `SkillData.cs`에 `damageMultiplier` 필드 추가
2. 🔮 `DamageSource.cs`에 `isSkillAttack` 플래그 추가
3. 🔮 `DamageSource.cs`에서 Step 2 스킬 배율 적용
4. ✅ 각 스킬에 배율 설정 (80%~200%)
5. ✅ 테스트: 기본 공격 vs 스킬 데미지 차이 확인

**예상 소요 시간:** 2~3시간

---

### 6.3 Phase 2: 룬 시스템 기반 구조 (장기 프로젝트)

**목표:** 룬 시스템을 위한 확장 포인트 준비

**작업 목록:**
1. 🔮 `RuneData.cs` 생성 (ScriptableObject)
   - `additiveAttack`: 고정 공격력 증가
   - `multiplicativeAttack`: % 공격력 증가
   - `penetration`: 관통률
   - `lifeSteal`: 생명력 흡수
   - `onHitEffects`: 타격 시 효과
2. 🔮 `RuneSystem.cs` 싱글톤 생성
   - `equippedRunes`: 장착된 룬 목록
   - `GetAdditiveAttackBonus()`
   - `GetMultiplicativeBonus()`
   - `GetPenetrationRate()`
3. 🔮 `DamageSource.cs`에서 Step 3, 9, 14 확장 포인트 활성화
4. ✅ 테스트: 룬 장착 전/후 데미지 차이 확인

**예상 소요 시간:** 1~2주

---

### 6.4 표준 데미지 계산 클래스 (권장)

**목적:** 데미지 계산 로직을 재사용 가능한 표준 클래스로 분리

```csharp
/// <summary>
/// 표준 데미지 계산 시스템
/// Combat Logic Layer에서 사용
/// </summary>
public static class CombatFormula
{
    /// <summary>
    /// 최종 데미지 계산 (플레이어 → 몬스터)
    /// </summary>
    public static int CalculateFinalDamage(
        float baseAttack,
        IPlayerClass playerClass,
        Collider2D target,
        bool isSkillAttack = false,
        SkillData skillData = null
    )
    {
        // Step 1: 기본 공격력 (이미 제공됨)
        float damage = baseAttack;
        
        // Step 2: 스킬 배율 (🔮 확장 예정)
        if (isSkillAttack && skillData != null)
        {
            damage *= skillData.damageMultiplier;
        }
        
        // Step 3: 증가 보너스 (🔮 확장 예정)
        // damage += GetAdditiveBonus();
        
        // Step 4: 증폭 배율
        damage *= GetMultiplicativeBonus(playerClass);
        
        // Step 5: 백어택 (Assasin)
        damage *= GetBackAttackMultiplier(playerClass, target);
        
        // Step 6: 크리티컬
        damage *= GetCriticalMultiplier(playerClass);
        
        // Step 7: 최종 공격력 확정
        float finalAttackPower = damage;
        
        // Step 8~10: 방어력 감소 (🔧 적용 필요)
        float finalDamage = ApplyDefenseReduction(finalAttackPower, target);
        
        // Step 13: 최종 데미지 확정
        int roundedDamage = Mathf.RoundToInt(finalDamage);
        roundedDamage = Mathf.Max(1, roundedDamage); // 최소 1
        
        return roundedDamage;
    }
    
    /// <summary>
    /// 방어력 감소 계산
    /// </summary>
    private static float ApplyDefenseReduction(float attackPower, Collider2D target)
    {
        // Step 8: 방어력 가져오기
        float defense = GetTargetDefense(target);
        
        // Step 9: 관통 적용 (🔮 확장 예정)
        float penetration = 0f; // GetPenetrationRate();
        float effectiveDefense = defense * (1f - penetration);
        
        // Step 10: 방어력 감소 공식
        float defenseRate = effectiveDefense / (effectiveDefense + 100f);
        float finalDamage = attackPower * (1f - defenseRate);
        
        return finalDamage;
    }
    
    /// <summary>
    /// 증폭 배율 계산
    /// </summary>
    private static float GetMultiplicativeBonus(IPlayerClass playerClass)
    {
        float multiplier = 1f;
        
        // 클래스 배율
        multiplier *= playerClass.AttackPowerMultiplier;
        
        // Warrior 버서커 모드
        if (playerClass is Warrior warrior && warrior.IsInBerserkerMode())
        {
            multiplier *= warrior.BerserkerDamageBonus;
        }
        
        // 🔮 확장 예정: 룬 옵션, 버프
        // multiplier *= GetRuneMultiplier();
        
        return multiplier;
    }
    
    /// <summary>
    /// 백어택 배율 계산
    /// </summary>
    private static float GetBackAttackMultiplier(IPlayerClass playerClass, Collider2D target)
    {
        if (playerClass is Assasin assasin)
        {
            Vector3 playerPos = playerClass.transform.position;
            Vector3 enemyPos = target.transform.position;
            Vector3 enemyFacing = target.transform.right;
            
            return assasin.GetBackAttackMultiplier(playerPos, enemyPos, enemyFacing);
        }
        
        return 1f;
    }
    
    /// <summary>
    /// 크리티컬 배율 계산
    /// </summary>
    private static float GetCriticalMultiplier(IPlayerClass playerClass)
    {
        var playerRuntimeStats = Object.FindObjectOfType<PlayerRuntimeStats>();
        if (playerRuntimeStats == null)
            return 1f;
        
        float critChance = playerRuntimeStats.FinalCriticalChance;
        float critDamage = playerRuntimeStats.FinalCriticalDamage;
        
        if (Random.Range(0f, 1f) < critChance)
        {
            Debug.Log($"💥 크리티컬 히트! (×{critDamage})");
            return critDamage;
        }
        
        return 1f;
    }
    
    /// <summary>
    /// 대상 방어력 가져오기
    /// </summary>
    private static float GetTargetDefense(Collider2D target)
    {
        var enemyHealth = target.GetComponent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.EnemyData != null)
        {
            return enemyHealth.EnemyData.defense; // 추가 필요
        }
        
        return 0f;
    }
}
```

**사용 예시:**
```csharp
// DamageSource.cs에서 사용
private void OnTriggerEnter2D(Collider2D other) 
{
    float baseDamage = GetCurrentBaseDamage();
    
    // 표준 공식 사용
    int finalDamage = CombatFormula.CalculateFinalDamage(
        baseDamage,
        currentClass,
        other,
        isSkillAttack: false,
        skillData: null
    );
    
    enemyHealth.TakeDamage(finalDamage);
}
```

---

## 7. 최종 요약

### 7.1 핵심 구조

```
┌────────────────────────────────────────────────────────┐
│  Combat Logic Layer (Layer 5)                          │
│                                                        │
│  CombatFormula.CalculateFinalDamage()                  │
│  ├─ Step 1~7: 공격력 계산                              │
│  │  ├─ ✅ 기본 공격력 (구현됨)                        │
│  │  ├─ 🔮 스킬 배율 (확장 예정)                      │
│  │  ├─ 🔮 증가 보너스 (확장 예정)                    │
│  │  ├─ ✅ 증폭 배율 (부분 구현)                      │
│  │  ├─ ✅ 백어택 (구현됨)                            │
│  │  └─ ✅ 크리티컬 (구현됨)                          │
│  │                                                    │
│  ├─ Step 8~13: 방어력 및 특수 판정                    │
│  │  ├─ 🔧 방어력 감소 (적용 필요)                    │
│  │  ├─ 🔮 관통 (확장 예정)                          │
│  │  ├─ ✅ 블록 (구현됨)                              │
│  │  └─ 🔮 회피 (확장 예정)                          │
│  │                                                    │
│  └─ Step 14: 추가 효과                                │
│     └─ 🔮 생명력 흡수, 상태이상, 연쇄 (확장 예정)    │
└────────────────────────────────────────────────────────┘
```

### 7.2 구현 우선순위

| 우선순위 | 작업 | 상태 | 예상 시간 |
|---------|------|------|----------|
| **P0** | 방어력 시스템 적용 | 🔧 필요 | 1~2시간 |
| **P1** | 스킬 배율 시스템 | 🔮 계획 | 2~3시간 |
| **P2** | 회피/관통 시스템 | 🔮 계획 | 4~6시간 |
| **P3** | 룬 시스템 기반 | 🔮 계획 | 1~2주 |

### 7.3 확장 가능성

이 구조는 다음 요소들을 자연스럽게 추가할 수 있습니다:
- ✅ 스킬 배율 (Step 2)
- ✅ 버프/디버프 (Step 3)
- ✅ 룬 옵션 (Step 3, 9, 14)
- ✅ 속성 시스템 (Step 14)
- ✅ 상태 이상 (Step 14)
- ✅ 특수 효과 (Step 14)
- ✅ 장비 세트 효과 (Step 4)
- ✅ 파티 버프 (Step 3, 4)

---

**작성일**: 2026-02-13  
**버전**: 1.0 - 최소 확장 가능 뼈대  
**작성자**: AI Assistant  
**문서 위치**: `Assets/COMBAT_FORMULA_DESIGN.md`

---

## 📌 다음 단계

1. **즉시 작업 (P0)**: 방어력 시스템 적용
   - `DamageSource.cs`에 `ApplyDefenseReduction()` 추가
   - `PlayerHealth.cs`에 방어력 감소 로직 추가
   - 테스트 및 밸런스 조정

2. **다음 작업 (P1)**: 스킬 배율 시스템
   - `SkillData.cs`에 `damageMultiplier` 추가
   - 각 스킬별 배율 설정
   - 테스트 및 밸런스 조정

3. **장기 계획 (P2~P3)**: 룬 시스템 기반 구조
   - `RuneSystem.cs` 설계
   - 확장 포인트 활성화
   - 추가 효과 구현

---

**이 문서는 전투 공식의 '최소 확장 가능한 뼈대'입니다.**  
**나중에 추가될 시스템들이 이 구조에 자연스럽게 통합될 수 있도록 설계되었습니다.**

