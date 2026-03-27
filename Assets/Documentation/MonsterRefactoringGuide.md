# 몬스터 범용화 리팩토링 — 에디터 작업 가이드

> 코드 작업(Phase 1)이 완료된 후 이 문서를 따라 에디터 작업을 진행합니다.

---

## 완료된 코드 작업 (Phase 1)

| 파일 | 변경 내용 |
|------|-----------|
| `EnemyFSMController.cs` | `CurrentState` 프로퍼티 추가 (1줄) |
| `GenericMeleeEnemy.cs` | 신규 — Melee형 전체 대체 스크립트 |
| `GenericRangedEnemy.cs` | 신규 — Ranged형 전체 대체 스크립트 |
| `EnemyDebugGizmosDrawer.cs` | 신규 — ColliderGizmosDrawer 대체 |

---

## Phase 2 — Base Animator Controller 생성 (에디터)

> 기존 사용 중인 몬스터의 Animator Controller를 참고해서 만드세요.
> **파라미터 이름은 반드시 아래와 동일하게** 설정해야 EnemyAnimationController와 호환됩니다.

### 공통 파라미터 (8방향 / 2방향 모두)

| 이름 | 타입 | 용도 |
|------|------|------|
| `speed` | Float | 이동 속도 크기 |
| `isMoving` | Bool | Idle ↔ Walk 전환 |
| `Attack` | Trigger | 공격 State 진입 |
| `Hit` | Trigger | 피격 State 진입 |
| `Die` | Trigger | 사망 State 진입 |

### 8방향 전용 추가 파라미터

| 이름 | 타입 | 용도 |
|------|------|------|
| `moveX` | Float | Blend Tree X축 방향값 |
| `moveY` | Float | Blend Tree Y축 방향값 |

---

### 2-1. `Enemy_Melee_2Dir_Base_AC.controller`

```
State 구성:
  Idle   (기본 State)
  Walk   → isMoving == true 진입 / isMoving == false 복귀
           클립: Walk 단일 클립 (Blend Tree 없음)
  Attack → Attack Trigger 진입 → 종료 시 Idle 복귀
           StateMachineBehaviour: EnemyAttackStateBehaviour 부착
  Hit    → Hit Trigger 진입 → 종료 시 Idle 복귀
  Die    → Die Trigger 진입 (루프 또는 마지막 프레임 고정)
           StateMachineBehaviour: EnemyDieStateBehaviour 부착

EnemyAttackStateBehaviour 설정:
  attackExecuteTime  = 0.4  (공격 히트 발동 시점)
  attackCompleteTime = 0.8  (공격 완료 시점)
```

### 2-2. `Enemy_Melee_8Dir_Base_AC.controller`

```
State 구성: (2Dir와 동일 구조)
  Walk State: 2D Freeform Directional Blend Tree
              파라미터: moveX, moveY
              모션: 방향별 Walk 클립 (E/NE/N/SE/S 최소 5방향)
  Attack State: 2D Blend Tree 또는 단일 클립
                StateMachineBehaviour: EnemyAttackStateBehaviour 부착
  
EnemyAttackStateBehaviour 설정:
  attackExecuteTime  = 0.4
  attackCompleteTime = 0.8
```

### 2-3. `Enemy_Ranged_2Dir_Base_AC.controller`

```
Melee_2Dir와 동일한 State 구조
Attack State의 클립만 원거리 공격 동작으로 교체될 예정 (Override에서 처리)
StateMachineBehaviour: EnemyAttackStateBehaviour 부착
```

### 2-4. `Enemy_Ranged_8Dir_Base_AC.controller`

```
Melee_8Dir와 동일한 State 구조
StateMachineBehaviour: EnemyAttackStateBehaviour 부착
```

### 2-5. `Enemy_Fixed_Base_AC.controller`

```
고정형(TowerMonster, PlantsMonster 등) 전용

State 구성:
  Idle   (기본 State, Loop)
  Attack → Attack Trigger 진입 → 종료 시 Idle 복귀
           StateMachineBehaviour: EnemyAttackStateBehaviour 부착
  Hit    → Hit Trigger 진입 → 종료 시 Idle 복귀
  Die    → Die Trigger 진입
           StateMachineBehaviour: EnemyDieStateBehaviour 부착

Walk State 없음 (이동 안 함)
파라미터: Attack(Trigger), Hit(Trigger), Die(Trigger) 만 필요
         speed/isMoving/moveX/moveY 불필요
```

---

## Phase 3 — Base Prefab 생성 (에디터)

> **이 베이스 프리팹을 기준으로 각 몬스터 Prefab Variant를 만듭니다.**
> 공통 컴포넌트를 여기서 설정해두면 Variant에서 자동 상속됩니다.

### 3-1. `Enemy_MeleeBase.prefab`

**오브젝트 이름:** `Enemy_MeleeBase`

**추가할 컴포넌트 목록:**

| 컴포넌트 | 설정 | 비고 |
|---------|------|------|
| `GenericMeleeEnemy` | 데이터는 비워둠 (Variant에서 설정) | |
| `Animator` | Controller: 비워둠 (Variant에서 Override Controller 할당) | |
| `EnemyAnimationController` | Animator/SpriteRenderer 자동 참조 | |
| `EnemyFSMController` | 설정 없음 | |
| `EnemyHealth` | 기본값 | |
| `NavMeshAgent` | 기본값 (BaseEnemy가 Awake에서 2D 설정 자동 적용) | 현재 모든 몬스터 필수 |
| `MeleeAttack` | AttackData: 비워둠 (Variant에서 설정) | |
| `SpriteRenderer` | Sprite: 비워둠 | |
| `Rigidbody2D` | Body Type: **Kinematic** (NavMesh 사용 시 BaseEnemy가 자동 전환) | Dynamic으로 놓아도 자동 변경됨 |
| `Collider2D` | 기본 BoxCollider2D 또는 CapsuleCollider2D | |
| `AudioSource` | 기본값 | 공격/피격 사운드용 (BaseAttackBehaviour가 사용) |
| `Flash` | `whiteFlashMat` 머티리얼 할당 필요 | 피격 시 흰색 플래시 (NavMesh 넉백 대체) |
| `MinimapMarker` | `markerType`: Enemy, 아이콘 스프라이트 할당 | 모든 몬스터 필수 |
| `EnemyDebugGizmosDrawer` | 설정 기본값 유지 | 에디터 전용 |

**GenericMeleeEnemy 인스펙터 비워두는 필드:**
- `EnemyData` → Variant에서 할당
- `GrowthProfile` → Variant에서 할당
- `MeleeAttack` 필드 → 자동 탐색 (같은 오브젝트의 MeleeAttack 컴포넌트)

**포함하지 않는 컴포넌트:**
- `EnemyPathfinding` → 레거시, 현재 미사용
- `Knockback` → NavMesh 몬스터는 물리 넉백 없음 (Flash가 시각적 효과 대체)

---

### 3-2. `Enemy_RangedBase.prefab`

**오브젝트 이름:** `Enemy_RangedBase`

| 컴포넌트 | 설정 | 비고 |
|---------|------|------|
| `GenericRangedEnemy` | 데이터 비워둠 | |
| `Animator` | Controller: 비워둠 | |
| `EnemyAnimationController` | 자동 참조 | |
| `EnemyFSMController` | 설정 없음 | |
| `EnemyHealth` | 기본값 | |
| `NavMeshAgent` | 기본값 (BaseEnemy가 Awake에서 2D 설정 자동 적용) | 현재 모든 몬스터 필수 |
| `RangedAttack` | AttackData: 비워둠 | Ghost → MultiShotRangedAttack으로 교체, CrystalGolem/WaterGolem → AOEAttack으로 교체 |
| `SpriteRenderer` | Sprite: 비워둠 | |
| `Rigidbody2D` | Body Type: **Kinematic** | NavMesh 사용 시 BaseEnemy가 자동 전환 |
| `Collider2D` | 기본 BoxCollider2D | |
| `AudioSource` | 기본값 | 공격/피격 사운드용 |
| `Flash` | `whiteFlashMat` 머티리얼 할당 필요 | 피격 플래시 |
| `MinimapMarker` | `markerType`: Enemy, 아이콘 스프라이트 할당 | |
| `EnemyDebugGizmosDrawer` | 기본값 | 에디터 전용 |

**포함하지 않는 컴포넌트:**
- `EnemyPathfinding` → 레거시, 현재 미사용
- `Knockback` → NavMesh 몬스터는 불필요

---

## Phase 4 — 몬스터별 작업 (20개)

> ⚠️ **중요: 씬 교체 작업 없음**
> 기존 프리팹 파일(.prefab)을 직접 수정합니다.
> Unity는 GUID 기반으로 참조를 유지하므로, 프리팹 내용을 바꿔도
> 50개 씬의 모든 배치 인스턴스에 자동으로 반영됩니다.
> 파일명/경로를 바꾸지 않는 한 씬은 손댈 필요가 없습니다.

---

### Step A. Override Controller 생성

```
1. Project 창에서 우클릭 → Create → Animator Override Controller
2. 이름: [몬스터명]_AC  (예: BlueSlime_AC)
3. Controller 필드: 해당 Base Controller 지정
4. 아래 테이블에서 각 몬스터의 Base Controller 확인
5. 클립 목록에서 Placeholder 클립 → 해당 몬스터 클립으로 교체
```

### Step B. 기존 프리팹 직접 수정

```
Project 창에서 기존 프리팹(예: Blue_slime.prefab) 더블클릭
→ Prefab 편집 모드 진입

[ 스크립트 교체 ]
1. 기존 개별 스크립트(BlueSlime) 컴포넌트 우클릭 → Remove Component
2. Add Component → GenericMeleeEnemy (또는 GenericRangedEnemy)
3. 데이터 재할당:
   - EnemyData      → 기존과 동일한 ScriptableObject
   - GrowthProfile  → 기존과 동일한 ScriptableObject
   - isometricData  → DirectionPreset, FootOffset 설정
   (MeleeAttack 필드는 자동 탐색, 별도 할당 불필요)

[ 애니메이터 교체 ]
4. Animator 컴포넌트의 Controller 필드
   → 기존 개별 컨트롤러 → Step A에서 만든 [몬스터명]_AC

[ 신규 컴포넌트 추가 (없는 경우) ]
5. NavMeshAgent   → Add Component → NavMeshAgent
6. Flash          → Add Component → Flash → whiteFlashMat 할당
7. MinimapMarker  → Add Component → MinimapMarker
                    markerType: Enemy, 아이콘 스프라이트 할당
8. EnemyDebugGizmosDrawer → Add Component

[ 불필요 컴포넌트 제거 ]
9. EnemyPathfinding 있으면 Remove
10. Knockback 있으면 Remove

Ctrl+S 저장 → 모든 씬 자동 반영
```

---

## 몬스터별 Base Controller 지정표

### Melee형 (GenericMeleeEnemy 사용)

| 몬스터 | Base Controller | 비고 |
|-------|----------------|------|
| BlueSlime | Enemy_Melee_2Dir_Base_AC 또는 8Dir | 프리팹 DirectionPreset 확인 |
| GoblinWarrior | Enemy_Melee_8Dir_Base_AC | |
| Bear | Enemy_Melee_8Dir_Base_AC | |
| Beetle | Enemy_Melee_8Dir_Base_AC | |
| Spider | Enemy_Melee_8Dir_Base_AC | |
| LadyBug | Enemy_Melee_8Dir_Base_AC | |
| Cobra | Enemy_Melee_8Dir_Base_AC | |
| Scorpion | Enemy_Melee_8Dir_Base_AC | |
| Sphinx | Enemy_Melee_8Dir_Base_AC | |
| Anubis | Enemy_Melee_8Dir_Base_AC | |
| Mimic | Enemy_Melee_8Dir_Base_AC | |
| StoneGolem | Enemy_Melee_8Dir_Base_AC | |

### Ranged형 (GenericRangedEnemy 사용)

| 몬스터 | Base Controller | 공격 컴포넌트 | 비고 |
|-------|----------------|------------|------|
| Ghost | Enemy_Ranged_8Dir_Base_AC | MultiShotRangedAttack | |
| Grape | Enemy_Ranged_8Dir_Base_AC | RangedAttack | |
| GoblinBomb | Enemy_Ranged_8Dir_Base_AC | RangedAttack | |
| GoblinCart | Enemy_Ranged_8Dir_Base_AC | RangedAttack | |
| CrystalGolem | Enemy_Ranged_8Dir_Base_AC | AOEAttack | |
| WaterGolem | Enemy_Ranged_8Dir_Base_AC | AOEAttack | |
| TowerMonster | Enemy_Fixed_Base_AC | RangedAttack | EnemyData: PatrolRadius=0, ChaseRange=0 |
| PlantsMonster | Enemy_Fixed_Base_AC | RangedAttack | EnemyData: PatrolRadius=0, ChaseRange=0 |

---

## Prefab Variant 인스펙터 설정 체크리스트

각 Variant에서 반드시 오버라이드해야 하는 항목입니다.

```
[ 애니메이션 ]
□ Animator.Controller              → 해당 몬스터 Override Controller (.overrideController)

[ 데이터 ]
□ GenericMeleeEnemy.EnemyData      → 해당 몬스터 EnemyData (ScriptableObject)
□ GenericMeleeEnemy.GrowthProfile  → MonsterGrowthProfile (ScriptableObject)
□ GenericMeleeEnemy.isometricData
    □ DirectionPreset  → E4M 또는 E8 (기존 프리팹 참조)
    □ FootOffset       → 기존 프리팹 참조

[ 공격 ]
□ MeleeAttack.AttackData           → 해당 몬스터 AttackData (ScriptableObject)
  (Ranged형은 RangedAttack / MultiShotRangedAttack / AOEAttack 컴포넌트 교체 후 AttackData 할당)

[ 비주얼 ]
□ SpriteRenderer.Sprite            → 해당 몬스터 스프라이트
□ Collider2D (Size/Offset)         → 해당 몬스터 크기에 맞게 조정

[ 공통 컴포넌트 설정 (Variant별 다른 경우) ]
□ Flash.whiteFlashMat              → 흰색 플래시 머티리얼 (모두 동일)
□ MinimapMarker.defaultSprite      → 해당 몬스터 미니맵 아이콘 스프라이트
□ MinimapMarker.arrowSprite        → 방향 화살표 스프라이트 (공통)
□ MinimapMarker.markerColor        → 색상 (기본 흰색)
```

> **이동 속도 / 체력 / 공격력 / 탐지/추격 범위**는  
> `EnemyData` ScriptableObject에서 설정하므로 Variant에서 별도 조정 불필요.
>
> **NavMeshAgent** 세부 설정(radius, speed 등)은 BaseEnemy.Awake에서  
> EnemyData 기반으로 자동 초기화되므로 개별 조정 불필요.

---

## Phase 5 — 정리

모든 Variant로 교체 완료 후:

```
삭제 대상 스크립트 (BasicEnemies/):
  □ BlueSlime.cs
  □ GoblinWarrior.cs
  □ Bear.cs
  □ Beetle.cs
  □ Spider.cs
  □ LadyBug.cs
  □ Cobra.cs
  □ Scorpion.cs
  □ Sphinx.cs
  □ Anubis.cs
  □ Mimic.cs
  □ StoneGolem.cs
  □ Ghost.cs
  □ Grape.cs
  □ GoblinBomb.cs
  □ GoblinCart.cs
  □ CrystalGolem.cs
  □ WaterGolem.cs
  □ TowerMonster.cs
  □ PlantsMonster.cs

삭제 대상 스크립트 (Misc/):
  □ ColliderGizmosDrawer.cs

삭제 전 확인:
  - 씬에 배치된 기존 개별 프리팹이 모두 Variant로 교체되었는지 확인
  - 삭제 전 스크립트가 다른 곳에서 참조되는지 검색 (Ctrl+Shift+F)
```

---

## 변경하지 않는 파일 (수정 금지)

```
BaseEnemy.cs
IEnemyState.cs
EnemyIdleState.cs / EnemyPatrolState.cs / EnemyChaseState.cs
EnemyAttackState.cs / EnemyHitState.cs / EnemyDieState.cs
EnemyFSMController.cs       (CurrentState 프로퍼티 추가만 완료)
EnemyAnimationController.cs
EnemyAttackStateBehaviour.cs / EnemyDieStateBehaviour.cs
MeleeAttack.cs / RangedAttack.cs / MultiShotRangedAttack.cs / AOEAttack.cs
EnemyData.cs / AttackData.cs / MonsterGrowthProfile.cs
EnemyHealth.cs
```
