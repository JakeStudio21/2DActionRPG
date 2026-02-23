# ⚔️ 전투 시스템 레이어 아키텍처 (Combat System Layer Architecture)

**프로젝트**: Unity 2D Action RPG  
**작성일**: 2026-02-13  
**버전**: 1.0 Final  

---

## 📋 목차

1. [레이어 구조 개요](#1-레이어-구조-개요)
2. [플레이어 전투 플로우](#2-플레이어-전투-플로우)
3. [몬스터 전투 플로우](#3-몬스터-전투-플로우)
4. [전투 상호작용](#4-전투-상호작용)
5. [데이터 흐름](#5-데이터-흐름)
6. [레이어별 책임](#6-레이어별-책임)

---

## 1. 레이어 구조 개요

### 1.1 전체 레이어 구성 (7개 레이어)

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: Input Layer (입력 레이어)                         │
│  - 키보드, 마우스, 조이스틱 입력 처리                       │
│  - 입력 이벤트 발행                                          │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: Control Layer (제어 레이어)                       │
│  - 입력 검증 및 필터링                                       │
│  - 쿨다운, 상태 체크                                         │
│  - 명령 전달                                                 │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: Animation Layer (애니메이션 레이어)                │
│  - Animator 파라미터 설정                                    │
│  - Animation Event 발행                                      │
│  - State Machine 관리                                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 4: Execution Layer (실행 레이어)                     │
│  - 무기/스킬 실행                                            │
│  - 발사체 생성                                               │
│  - 데미지 소스 활성화                                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 5: Combat Logic Layer (전투 로직 레이어)             │
│  - 데미지 계산                                               │
│  - 클래스 효과 적용                                          │
│  - 충돌 감지 및 판정                                         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 6: Data Layer (데이터 레이어)                        │
│  - 스탯 참조 (PlayerRuntimeStats)                           │
│  - 장비 데이터 (EquipmentData)                              │
│  - 공격 데이터 (AttackData)                                 │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  Layer 7: Feedback Layer (피드백 레이어)                    │
│  - 이펙트 (CueSystem)                                       │
│  - 데미지 넘버                                               │
│  - 사운드, 카메라 쉐이크                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 플레이어 전투 플로우

### 2.1 플레이어 공격 전체 플로우

```
┌──────────────────────────────────────────────────────────────┐
│                    LAYER 1: INPUT LAYER                       │
├──────────────────────────────────────────────────────────────┤
│  [PlayerAttackInput.cs]                                      │
│  - Input.GetKeyDown(KeyCode.A)  ← A키 입력 감지             │
│  - Input.GetKeyDown(KeyCode.S)  ← S키 입력 감지 (스킬1)     │
│  - Input.GetKeyDown(KeyCode.D)  ← D키 입력 감지 (스킬2)     │
│                                                              │
│  [GameControl.cs]                                            │
│  - MovementJoystick  ← 이동 입력 (조이스틱)                 │
│  - AttackJoystick    ← 공격 방향 입력 (조이스틱)            │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                   LAYER 2: CONTROL LAYER                      │
├──────────────────────────────────────────────────────────────┤
│  [PlayerAnimationController.cs]                              │
│  - TriggerAttack()      ← A키 → 기본공격 트리거             │
│  - TriggerSkill1()      ← S키 → 스킬1 트리거                │
│  - TriggerSkill2()      ← D키 → 스킬2 트리거                │
│                                                              │
│  ✅ 검증 단계:                                               │
│  - CanPerformAttack()   ← 쿨다운 체크                       │
│  - isAttacking == false ← 공격 중 여부 체크                 │
│  - canAttack == true    ← 공격 가능 여부                    │
│                                                              │
│  ✅ 통과 시:                                                 │
│  - animator.SetTrigger("Attack")                            │
│  - playerController.ApplyAttackMovementRestriction()        │
│  - isAttacking = true                                       │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                  LAYER 3: ANIMATION LAYER                     │
├──────────────────────────────────────────────────────────────┤
│  [Animator (Player_Assasin.controller)]                      │
│  - Idle → Attack_BlendTree (Attack 트리거)                   │
│  - Animation Event: OnAttackStart()                          │
│                                                              │
│  [AttackStateBehaviour.cs] (StateMachineBehaviour)           │
│  - OnStateEnter() → playerAnimController.OnAttackStart()    │
│  - OnStateExit()  → playerAnimController.OnAttackComplete() │
│                                                              │
│  ✅ OnAttackStart() 호출:                                    │
│  - ExecuteWeaponAttack() 실행 명령                          │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                  LAYER 4: EXECUTION LAYER                     │
├──────────────────────────────────────────────────────────────┤
│  [PlayerAnimationController.cs]                              │
│  - ExecuteWeaponAttack()                                     │
│    └→ activeWeapon.Attack()                                  │
│                                                              │
│  [ActiveWeapon.cs]                                           │
│  - Attack()                                                  │
│    ├→ currentWeapon.Attack() (Bow/Sword)                    │
│    └→ CueSystem.Emit("attack.player.weapon")                │
│                                                              │
│  [Bow.cs / Sword.cs]                                         │
│  - Attack()                                                  │
│    ├→ 조이스틱 방향 계산                                     │
│    ├→ GamePoolManager.SpawnFromPool("Arrow")                │
│    └→ Projectile.Initialize() / DamageSource 활성화        │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                LAYER 5: COMBAT LOGIC LAYER                    │
├──────────────────────────────────────────────────────────────┤
│  [DamageSource.cs] (Trigger Collider)                       │
│  - OnTriggerEnter2D(Collider2D other)                       │
│    ├→ GetCurrentBaseDamage() (PlayerRuntimeStats)          │
│    ├→ ApplyClassSpecialEffects()                            │
│    │   ├→ Warrior: 버서커 모드 (체력 30% 이하 → ×1.5)      │
│    │   └→ Assasin: 크리티컬 × 백어택                       │
│    └→ enemyHealth.TakeDamage(finalDamage)                   │
│                                                              │
│  [Projectile.cs] (원거리 전용)                               │
│  - OnTriggerEnter2D()                                       │
│    └→ DamageSource와 동일한 로직                            │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                    LAYER 6: DATA LAYER                        │
├──────────────────────────────────────────────────────────────┤
│  [PlayerRuntimeStats.cs]                                     │
│  - FinalAttackDamage  ← 기본 + 레벨 + 장비 + 클래스         │
│  - FinalCriticalChance                                       │
│  - FinalCriticalDamage                                       │
│                                                              │
│  계산 흐름:                                                   │
│  1. CalculateBaseStats()    ← 레벨 기반                     │
│  2. ApplyEquipmentStats()   ← 장비 추가                     │
│  3. ApplyClassMultipliers() ← 클래스 배율                   │
│  4. ApplyTemporaryBuffs()   ← 버프 효과                     │
│                                                              │
│  최종 공식:                                                   │
│  FinalAttackDamage = (기본값 + 레벨×2 + 장비) × 클래스배율  │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                  LAYER 7: FEEDBACK LAYER                      │
├──────────────────────────────────────────────────────────────┤
│  [EnemyHealth.cs]                                            │
│  - TakeDamage(int damage)                                   │
│    ├→ DamageNumberManager.ShowDamage()  ← 데미지 넘버 표시  │
│    ├→ CueEmitter.Emit("hit.enemy.normal")  ← 히트 이펙트   │
│    ├→ flash.FlashRoutine()  ← 피격 깜빡임                  │
│    └→ knockback.GetKnockedBack()  ← 넉백                   │
│                                                              │
│  [CueSystem]                                                 │
│  - HitEffect Prefab 생성                                     │
│  - Sound 재생                                                │
│  - Camera Shake (옵션)                                       │
└──────────────────────────────────────────────────────────────┘
```

### 2.2 플레이어 스킬 플로우

```
┌─────────────────────────────────────────────────────┐
│  S키 입력 (스킬1) or D키 입력 (스킬2)               │
│          ↓                                          │
│  PlayerAnimationController.TriggerSkill()           │
│          ↓                                          │
│  SkillStateBehaviour → OnStateEnter()               │
│          ↓                                          │
│  SkillController.ExecuteSkill()                     │
│          ↓                                          │
│  AssasinSkill1/2.cs → Cast/AOE/Hit 3단계 이펙트    │
│          ↓                                          │
│  DamageArea.InitializeForPlayer() → AOE 판정       │
│          ↓                                          │
│  EnemyHealth.TakeDamage() × N명                     │
│          ↓                                          │
│  피드백 (이펙트, 데미지 넘버, 사운드)               │
└─────────────────────────────────────────────────────┘
```

---

## 3. 몬스터 전투 플로우

### 3.1 몬스터 공격 전체 플로우

```
┌──────────────────────────────────────────────────────────────┐
│                    LAYER 1: INPUT LAYER                       │
│                         (AI 대체)                             │
├──────────────────────────────────────────────────────────────┤
│  [EnemyFSMController.cs]                                     │
│  - currentState.Execute()  ← 매 프레임 상태 실행            │
│                                                              │
│  [EnemyIdleState → EnemyChaseState → EnemyAttackState]      │
│  - 플레이어 감지 (DetectionRadius)                           │
│  - 거리 계산 (Distance < AttackRange?)                       │
│  - 상태 전환 결정                                            │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                   LAYER 2: CONTROL LAYER                      │
├──────────────────────────────────────────────────────────────┤
│  [EnemyAttackState.cs]                                       │
│  - Enter()                                                   │
│    ├→ 거리 체크 (AttackRange 이내?)                         │
│    ├→ attackBehaviour.CanAttack() 체크                      │
│    └→ attackBehaviour.Attack() 호출                         │
│                                                              │
│  [BaseAttackBehaviour.cs]                                    │
│  - CanAttack()                                               │
│    ├→ Time.time >= lastAttackTime + cooldown                │
│    └→ !animController.IsAttacking()                         │
│                                                              │
│  ✅ 통과 시:                                                 │
│  - OnAttack() 호출 (자식 클래스)                            │
│  - lastAttackTime = Time.time                               │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                  LAYER 3: ANIMATION LAYER                     │
├──────────────────────────────────────────────────────────────┤
│  [EnemyAnimationController.cs]                               │
│  - SetAttackTrigger()                                        │
│    └→ animator.SetTrigger("Attack")                         │
│                                                              │
│  [Animator (BlueSlime.controller / Grape.controller)]        │
│  - Idle → Attack (Attack 트리거)                             │
│  - Animation Event: AttackHit() / SpawnProjectileAnimEvent()│
│                                                              │
│  ✅ Animation Event 시점:                                    │
│  - 근접: AttackHit() → MeleeAttack.AttackHit()              │
│  - 원거리: SpawnProjectileAnimEvent() → RangedAttack        │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                  LAYER 4: EXECUTION LAYER                     │
├──────────────────────────────────────────────────────────────┤
│  [MeleeAttack.cs] (근접)                                     │
│  - AttackHit()                                               │
│    ├→ GetHitTargets() (OverlapCircle)                       │
│    ├→ RollCriticalHit() (크리티컬 판정)                     │
│    └→ playerHealth.TakeDamage()                             │
│                                                              │
│  [RangedAttack.cs] (원거리)                                  │
│  - SpawnProjectileAnimEvent()                                │
│    ├→ 플레이어 이동 예측 (predictionFactor)                 │
│    ├→ GamePoolManager.SpawnFromPool(projectilePrefab)      │
│    └→ Projectile.Initialize(direction, speed)              │
│                                                              │
│  [MultiShotRangedAttack.cs] (복합 원거리)                   │
│  - SpawnProjectileAnimEvent()                                │
│    ├→ Burst 발사 (projectileCount개)                       │
│    ├→ 각도 분산 (multiShotAngle)                            │
│    └→ 여러 발사체 생성                                       │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                LAYER 5: COMBAT LOGIC LAYER                    │
├──────────────────────────────────────────────────────────────┤
│  [MeleeAttack.cs - 근접 데미지 로직]                         │
│  - GetScaledDamage()                                         │
│    └→ AttackData.GetScaledDamage(level)                     │
│        └→ baseDamage × (1.1 ^ (level - 1))                  │
│                                                              │
│  - RollCriticalHit()                                         │
│    └→ AttackData.RollCritical()                             │
│        └→ Random.Range(0f, 1f) < criticalChance             │
│                                                              │
│  - ApplyDamageToPlayer()                                     │
│    ├→ damage = GetScaledDamage()                            │
│    ├→ if (isCritical) damage *= criticalMultiplier          │
│    └→ playerHealth.TakeDamage(damage)                       │
│                                                              │
│  [Projectile.cs - 원거리 데미지 로직]                        │
│  - OnTriggerEnter2D()                                       │
│    ├→ playerHealth = target.GetComponent<PlayerHealth>()   │
│    └→ playerHealth.TakeDamage(projectileDamage)            │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                    LAYER 6: DATA LAYER                        │
├──────────────────────────────────────────────────────────────┤
│  [AttackData.cs] (ScriptableObject)                          │
│  - baseDamage                                                │
│  - criticalChance                                            │
│  - criticalMultiplier                                        │
│  - attackCooldown                                            │
│  - attackRange                                               │
│  - projectilePrefab (원거리 전용)                            │
│  - onHitEffects (상태이상)                                   │
│                                                              │
│  [EnemyData.cs] (ScriptableObject)                           │
│  - maxHealth                                                 │
│  - moveSpeed                                                 │
│  - detectionRadius                                           │
│  - level                                                     │
└──────────────────────────────────────────────────────────────┘
                            ↓
┌──────────────────────────────────────────────────────────────┐
│                  LAYER 7: FEEDBACK LAYER                      │
├──────────────────────────────────────────────────────────────┤
│  [PlayerHealth.cs]                                           │
│  - TakeDamage(int damage, Transform hitTransform)           │
│    ├→ DamageNumberManager.ShowDamage()  ← 빨간색 데미지     │
│    ├→ CueEmitter.Emit("hit.player.normal")  ← 피격 이펙트  │
│    ├→ knockback.GetKnockedBack()  ← 넉백                   │
│    ├→ flash.FlashRoutine()  ← 깜빡임                       │
│    └→ playerAnimController.OnHitStart()  ← 피격 애니메이션  │
│                                                              │
│  [CueSystem]                                                 │
│  - PlayerHitEffect Prefab 생성                               │
│  - Sound 재생                                                │
│  - Screen Shake (옵션)                                       │
└──────────────────────────────────────────────────────────────┘
```

### 3.2 몬스터 FSM 상태 전환

```
┌────────────────────────────────────────────────────┐
│  EnemyIdleState (대기)                             │
│  - 플레이어 감지 (DetectionRadius 내)              │
│          ↓                                         │
│  EnemyChaseState (추격)                            │
│  - 플레이어 방향으로 이동                          │
│  - 거리 < AttackRange? → Attack                   │
│  - 거리 > DetectionRadius × 1.5? → Idle           │
│          ↓                                         │
│  EnemyAttackState (공격)                           │
│  - attackBehaviour.Attack()                       │
│  - 공격 완료 후 Chase로 복귀                       │
│          ↓                                         │
│  EnemyHitState (피격)                              │
│  - 짧은 경직                                       │
│  - 복귀: Chase or Idle                            │
│          ↓                                         │
│  EnemyDieState (사망)                              │
│  - 아이템 드롭                                     │
│  - 풀 반환                                         │
└────────────────────────────────────────────────────┘
```

---

## 4. 전투 상호작용

### 4.1 플레이어 → 몬스터 데미지 흐름

```
Player Input (A키)
     ↓
PlayerAnimationController.TriggerAttack()
     ↓
Animator → Attack Animation
     ↓
Animation Event: OnAttackStart()
     ↓
ActiveWeapon.Attack()
     ↓
Bow/Sword.Attack()
     ↓
DamageSource/Projectile 활성화
     ↓
Trigger Collision (EnemyHealth Collider)
     ↓
DamageSource.OnTriggerEnter2D()
     ├→ PlayerRuntimeStats.FinalAttackDamage
     ├→ ApplyClassSpecialEffects()
     │   ├→ Warrior: 버서커 × 1.5
     │   └→ Assasin: 크리티컬 × 백어택
     └→ finalDamage = baseDamage × classMultipliers
     ↓
EnemyHealth.TakeDamage(finalDamage)
     ├→ currentHealth -= damage
     ├→ DamageNumber 표시
     ├→ HitEffect 생성
     ├→ Flash + Knockback
     └→ FSM → HitState
```

### 4.2 몬스터 → 플레이어 데미지 흐름

```
Enemy FSM (Chase → Attack)
     ↓
EnemyAttackState.Enter()
     ↓
BaseAttackBehaviour.Attack()
     ↓
EnemyAnimationController.SetAttackTrigger()
     ↓
Animator → Attack Animation
     ↓
Animation Event: AttackHit() / SpawnProjectileAnimEvent()
     ↓
MeleeAttack / RangedAttack
     ├→ AttackData.GetScaledDamage(level)
     ├→ RollCriticalHit()
     └→ damage = baseDamage × levelMultiplier × (critical ? 2.0 : 1.0)
     ↓
PlayerHealth.TakeDamage(damage, hitTransform)
     ├→ Warrior.TryBlock() (20% 확률)
     │   └→ damage × 0.5 (50% 감소)
     ├→ Warrior.TriggerCounterAttack() (블록 성공 시 15% 확률)
     │   └→ 반격 데미지 × 1.3
     ├→ currentHealth -= damage
     ├→ DamageNumber 표시 (빨간색)
     ├→ HitEffect 생성
     ├→ Flash + Knockback
     └→ PlayerAnimController.OnHitStart()
```

### 4.3 상호 충돌 판정 (Collision Matrix)

| Layer | Player Weapon | Player Body | Enemy Weapon | Enemy Body |
|-------|--------------|-------------|--------------|------------|
| **Player Weapon** | ❌ | ❌ | ❌ | ✅ 데미지 |
| **Player Body** | ❌ | ❌ | ✅ 데미지 | ❌ |
| **Enemy Weapon** | ❌ | ✅ 데미지 | ❌ | ❌ |
| **Enemy Body** | ✅ 데미지 | ❌ | ❌ | ❌ |

**충돌 레이어 설정:**
- Player: Layer 3
- Enemy: Layer 7
- PlayerWeapon (DamageSource): Layer 6
- EnemyProjectile: Layer 8

---

## 5. 데이터 흐름

### 5.1 플레이어 스탯 계산 흐름

```
┌──────────────────────────────────────────────────┐
│  JSON 파일 (PlayerSlot1.json)                    │
│  - currentLevel: 5                               │
│  - RuntimeEquippedItems: { MainWeapon: "..." }  │
└──────────────────────────────────────────────────┘
                  ↓ Load
┌──────────────────────────────────────────────────┐
│  SelectedPlayerData (런타임 캐시)                 │
│  - classLevel: 5                                 │
│  - RuntimeEquippedItems: Dictionary              │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  PlayerRuntimeStats.RecalculateAllStats()        │
│  ├→ 1. CalculateBaseStats()                     │
│  │   └→ 10 + (레벨-1) × 2 = 18                  │
│  ├→ 2. ApplyEquipmentStats()                    │
│  │   └→ 18 + Sword_A(+5) = 23                   │
│  ├→ 3. ApplyClassMultipliers()                  │
│  │   └→ 23 × Assasin(×1.2) = 27.6              │
│  └→ 4. ApplyTemporaryBuffs()                    │
│      └→ 27.6 + BuffAttack(+5) = 32.6           │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  DamageSource.GetCurrentBaseDamage()             │
│  - playerRuntimeStats.FinalAttackDamage          │
│  - return 32.6                                   │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  ApplyClassSpecialEffects()                      │
│  - Assasin: 크리티컬 판정 (15%)                  │
│    └→ 32.6 × 1.5 = 48.9                         │
│  - Assasin: 백어택 판정 (뒤쪽 공격)             │
│    └→ 48.9 × 1.5 = 73.35                        │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  EnemyHealth.TakeDamage(73)                      │
│  - currentHealth -= 73                           │
│  - 피드백 (데미지 넘버, 이펙트)                  │
└──────────────────────────────────────────────────┘
```

### 5.2 몬스터 스탯 계산 흐름

```
┌──────────────────────────────────────────────────┐
│  AttackData.asset (ScriptableObject)             │
│  - baseDamage: 10                                │
│  - criticalChance: 0.15 (15%)                    │
│  - criticalMultiplier: 2.0                       │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  BaseEnemy (MonoBehaviour)                       │
│  - level: 5                                      │
│  - levelMultiplier: 1.1                          │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  AttackData.GetScaledDamage(level)               │
│  - baseDamage × (levelMultiplier ^ (level - 1)) │
│  - 10 × (1.1 ^ 4) = 10 × 1.4641 = 14.641       │
│  - return 15 (반올림)                            │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  MeleeAttack.RollCriticalHit()                   │
│  - Random.Range(0f, 1f) < 0.15                  │
│  - 15% 확률로 크리티컬 성공                      │
│  - damage = 15 × 2.0 = 30                       │
└──────────────────────────────────────────────────┘
                  ↓
┌──────────────────────────────────────────────────┐
│  PlayerHealth.TakeDamage(30, hitTransform)       │
│  - Warrior.TryBlock() (20% 확률)                 │
│    └→ 블록 실패 → 풀 데미지                      │
│  - currentHealth -= 30                           │
│  - 피드백 (데미지 넘버, 이펙트)                  │
└──────────────────────────────────────────────────┘
```

---

## 6. 레이어별 책임

### 6.1 Layer 1: Input Layer

**책임:**
- 사용자 입력 감지 (키보드, 마우스, 조이스틱)
- AI 입력 대체 (몬스터 FSM)
- 입력 이벤트 발행

**주요 클래스:**
- `PlayerAttackInput.cs` - 플레이어 키보드 입력
- `GameControl.cs` - 조이스틱 입력
- `EnemyFSMController.cs` - AI 상태 관리
- `EnemyIdleState/ChaseState/AttackState` - AI 입력 결정

**원칙:**
- ✅ 입력만 감지, 로직 없음
- ✅ 다음 레이어로 즉시 전달
- ❌ 데미지 계산 금지
- ❌ 애니메이션 직접 제어 금지

---

### 6.2 Layer 2: Control Layer

**책임:**
- 입력 검증 (쿨다운, 상태 체크)
- 명령 필터링
- 애니메이션 레이어로 전달

**주요 클래스:**
- `PlayerAnimationController.cs` - TriggerAttack(), CanPerformAttack()
- `BaseAttackBehaviour.cs` - CanAttack(), 쿨다운 관리
- `EnemyAttackState.cs` - 공격 가능 여부 판단

**원칙:**
- ✅ 쿨다운, 상태 검증
- ✅ Bool 플래그 관리 (isAttacking, canAttack)
- ❌ 직접 데미지 처리 금지
- ❌ UI 업데이트 금지

---

### 6.3 Layer 3: Animation Layer

**책임:**
- Animator 파라미터 설정
- Animation Event 발행
- StateMachineBehaviour 관리

**주요 클래스:**
- `Animator` - Attack_BlendTree, Skill_BlendTree
- `AttackStateBehaviour.cs` - OnAttackStart(), OnAttackComplete()
- `SkillStateBehaviour.cs` - OnSkillCastStart(), OnSkillActionComplete()
- `EnemyAnimationController.cs` - SetAttackTrigger()

**원칙:**
- ✅ 애니메이션 타이밍 제어
- ✅ Animation Event 정확한 시점 발행
- ❌ 데미지 계산 금지
- ❌ 직접 충돌 감지 금지

---

### 6.4 Layer 4: Execution Layer

**책임:**
- 무기/스킬 실행
- 발사체 생성
- DamageSource 활성화

**주요 클래스:**
- `ActiveWeapon.cs` - Attack() 중개
- `Bow.cs / Sword.cs` - 무기별 로직
- `SkillController.cs` - ExecuteSkill()
- `MeleeAttack.cs / RangedAttack.cs` - AttackHit(), SpawnProjectile()

**원칙:**
- ✅ 무기/스킬 실제 실행
- ✅ 오브젝트 풀링 관리
- ❌ 데미지 계산은 다음 레이어
- ❌ 최종 스탯 참조 금지

---

### 6.5 Layer 5: Combat Logic Layer

**책임:**
- 데미지 계산
- 클래스 효과 적용
- 충돌 감지 및 판정

**주요 클래스:**
- `DamageSource.cs` - OnTriggerEnter2D(), ApplyClassSpecialEffects()
- `Projectile.cs` - OnTriggerEnter2D()
- `MeleeAttack.cs` - GetScaledDamage(), RollCriticalHit()

**원칙:**
- ✅ 모든 데미지 계산 집중
- ✅ 클래스/장비 효과 적용
- ✅ Trigger Collision 감지
- ❌ UI 업데이트 금지
- ❌ 사운드/이펙트 직접 재생 금지

**공식:**
```csharp
// 플레이어 → 몬스터
finalDamage = PlayerRuntimeStats.FinalAttackDamage
            × ClassSpecialEffects (버서커/크리티컬/백어택)

// 몬스터 → 플레이어
finalDamage = AttackData.GetScaledDamage(level)
            × CriticalMultiplier (크리티컬 시)
            × BlockReduction (Warrior 블록 시)
```

---

### 6.6 Layer 6: Data Layer

**책임:**
- 스탯 데이터 제공
- ScriptableObject 관리
- 런타임 캐시

**주요 클래스:**
- `PlayerRuntimeStats.cs` - FinalAttackDamage 계산
- `EquipmentData.cs` - 장비 스탯
- `AttackData.cs` - 공격 스탯
- `SelectedPlayerData` - 런타임 캐시

**원칙:**
- ✅ 데이터 읽기 전용 (Read-Only)
- ✅ 계산된 최종 값 제공
- ❌ 직접 데미지 적용 금지
- ❌ 이벤트 발행 금지

**계산 순서:**
```
1. Base Stats (레벨 기반)
2. Equipment Stats (장비 추가)
3. Class Multipliers (클래스 배율)
4. Temporary Buffs (임시 버프)
→ FinalAttackDamage
```

---

### 6.7 Layer 7: Feedback Layer

**책임:**
- 시각/청각 피드백
- 데미지 넘버 표시
- 이펙트, 사운드, 카메라 쉐이크

**주요 클래스:**
- `EnemyHealth.cs` - TakeDamage(), EmitHitCues()
- `PlayerHealth.cs` - TakeDamage(), 피격 피드백
- `CueSystem` - Emit(), 이펙트 통합
- `DamageNumberManager` - ShowDamage()
- `Flash.cs` - FlashRoutine()
- `Knockback.cs` - GetKnockedBack()

**원칙:**
- ✅ 모든 피드백 집중 관리
- ✅ CueSystem 통한 통합 이펙트
- ❌ 데미지 계산 금지
- ❌ 게임 로직 변경 금지

---

## 7. 레이어 간 의존성 규칙

### 7.1 의존성 방향

```
Layer 1 (Input)
    ↓ 단방향
Layer 2 (Control)
    ↓ 단방향
Layer 3 (Animation)
    ↓ 단방향
Layer 4 (Execution)
    ↓ 단방향
Layer 5 (Combat Logic)
    ↓ 읽기 전용 →→→ Layer 6 (Data)
    ↓ 단방향
Layer 7 (Feedback)
```

### 7.2 금지된 의존성

❌ **역방향 의존성 금지:**
```
Layer 7 (Feedback) → Layer 5 (Combat Logic)  // 금지!
Layer 5 (Combat Logic) → Layer 2 (Control)   // 금지!
```

❌ **레이어 건너뛰기 금지:**
```
Layer 1 (Input) → Layer 4 (Execution)  // 금지! Layer 2, 3 거쳐야 함
```

✅ **허용된 읽기 전용 접근:**
```
Layer 5 (Combat Logic) → Layer 6 (Data)  // OK! 읽기 전용
```

---

## 8. 확장 가이드

### 8.1 새로운 무기 추가

```
1. Layer 6: EquipmentData.asset 생성 (데이터)
2. Layer 4: NewWeapon.cs 작성 (실행)
3. Layer 3: Animation Clip 추가
4. Layer 5: DamageSource 설정 (충돌)
5. Layer 7: CueSystem 이벤트 등록
```

### 8.2 새로운 몬스터 추가

```
1. Layer 6: EnemyData.asset, AttackData.asset 생성
2. Layer 1: FSM 상태 정의 (Idle/Chase/Attack)
3. Layer 2: BaseEnemy 상속 클래스 작성
4. Layer 4: AttackBehaviour 작성 (Melee/Ranged)
5. Layer 3: Animation Controller 설정
6. Layer 7: 피격 이펙트 등록
```

### 8.3 새로운 스킬 추가

```
1. Layer 6: SkillData.asset 생성 (데이터)
2. Layer 4: NewSkill.cs 작성 (ISkill 구현)
3. Layer 2: SkillController.SkillSet 등록
4. Layer 3: Animation Clip + StateMachineBehaviour
5. Layer 5: DamageArea 설정 (AOE)
6. Layer 7: CueSystem 3단계 이펙트 등록
```

---

## 9. 디버깅 가이드

### 9.1 레이어별 디버그 로그

```csharp
// Layer 1: Input
Debug.Log($"[Input] A키 입력 #{aKeyPressCount}");

// Layer 2: Control
Debug.Log($"[Control] CanPerformAttack: {canAttack}, isAttacking: {isAttacking}");

// Layer 3: Animation
Debug.Log($"[Animation] OnAttackStart() 호출");

// Layer 4: Execution
Debug.Log($"[Execution] Bow.Attack() 실행");

// Layer 5: Combat Logic
Debug.Log($"[Combat] 최종 데미지: {finalDamage}");

// Layer 6: Data
Debug.Log($"[Data] FinalAttackDamage: {playerRuntimeStats.FinalAttackDamage}");

// Layer 7: Feedback
Debug.Log($"[Feedback] HitEffect 생성");
```

### 9.2 문제 추적 순서

**문제: 공격이 안 나감**
```
1. Layer 1: Input.GetKeyDown() 감지 되는가?
2. Layer 2: CanPerformAttack() true인가?
3. Layer 3: Animator.SetTrigger() 호출되는가?
4. Layer 4: ExecuteWeaponAttack() 실행되는가?
5. Layer 5: OnTriggerEnter2D() 호출되는가?
```

**문제: 데미지가 이상함**
```
1. Layer 6: PlayerRuntimeStats.FinalAttackDamage 값 확인
2. Layer 5: ApplyClassSpecialEffects() 적용 여부
3. Layer 5: enemyHealth.TakeDamage() 호출 확인
```

---

## 10. 성능 최적화

### 10.1 레이어별 최적화 포인트

**Layer 1 (Input):**
- ✅ Input.GetKeyDown() (매 프레임)
- ✅ 불필요한 FindObjectOfType 제거

**Layer 4 (Execution):**
- ✅ 오브젝트 풀링 (GamePoolManager)
- ✅ 발사체 생존 시간 제한

**Layer 5 (Combat Logic):**
- ✅ Physics2D.OverlapCircle 범위 최소화
- ✅ Trigger Collision만 사용 (Rigidbody2D Kinematic)

**Layer 7 (Feedback):**
- ✅ 이펙트 풀링 (CueSystem)
- ✅ 데미지 넘버 제한 (최대 30개)

---

## 📊 최종 요약

### 전투 시스템 레이어 구조

| Layer | 책임 | 주요 클래스 | 의존성 |
|-------|------|-------------|--------|
| **1. Input** | 입력 감지 | PlayerAttackInput, EnemyFSM | → Control |
| **2. Control** | 검증/필터링 | PlayerAnimationController, BaseAttackBehaviour | → Animation |
| **3. Animation** | 애니메이션 | Animator, StateMachineBehaviour | → Execution |
| **4. Execution** | 실행 | ActiveWeapon, Bow/Sword, MeleeAttack | → Combat Logic |
| **5. Combat Logic** | 데미지 계산 | DamageSource, Projectile | ← Data (읽기 전용) |
| **6. Data** | 스탯 제공 | PlayerRuntimeStats, EquipmentData | - |
| **7. Feedback** | 피드백 | EnemyHealth, CueSystem, DamageNumber | - |

### 핵심 원칙

1. **단방향 흐름**: 상위 레이어 → 하위 레이어만 의존
2. **책임 분리**: 각 레이어는 명확한 단일 책임
3. **데이터 중심**: Layer 6은 읽기 전용으로만 참조
4. **확장 용이**: 새 무기/몬스터 추가 시 레이어별로 확장

---

**작성일**: 2026-02-13  
**버전**: 1.0 Final  
**작성자**: AI Assistant  
**문서 위치**: `Assets/COMBAT_SYSTEM_LAYER_ARCHITECTURE.md`

