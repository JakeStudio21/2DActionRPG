# 자동 타겟팅 시스템 테스트 가이드

---

## 1. 에디터 초기 셋업

### 1-1. 컴포넌트 부착 (Player 오브젝트)

```
[Player GameObject]
  ├─ PlayerController        (기존)
  ├─ PlayerAttackInput       (기존)
  ├─ ActiveWeapon            (기존)
  ├─ AutoTargetResolver      (신규)  ← enemyLayer 설정 필수
  └─ AutoTargetDirectionProvider  (신규)
```

> `AutoTargetDirectionProvider`는 `AutoTargetResolver`와 같은 GameObject에 붙어야 합니다 (`RequireComponent`).

---

### 1-2. AutoTargetResolver 인스펙터 설정

| 항목 | 설정값 |
|------|--------|
| Enemy Layer | `Enemy` (또는 몬스터가 속한 레이어) |
| Show Debug Gizmos | ✅ (개발 중 권장) |
| Show Debug Logs | 필요 시 ✅ |

---

### 1-3. AutoTargetDirectionProvider 인스펙터 설정

| 항목 | 설정값 |
|------|--------|
| Basic Attack Profile | 기본공격용 TargetingProfile 에셋 할당 |
| Player Controller | (비워두면 자동 탐색) |
| Active Weapon | (비워두면 자동 탐색) |

---

### 1-4. PlayerAttackInput 인스펙터 설정

| 항목 | 설정값 |
|------|--------|
| Auto Target Provider | AutoTargetDirectionProvider 드래그 할당 (또는 비워두면 자동 탐색) |
| Skill1 Data | 스킬1 ActiveSkillData 에셋 할당 (선택) |
| Skill2 Data | 스킬2 ActiveSkillData 에셋 할당 (선택) |

---

### 1-5. ActiveWeapon 인스펙터 설정

| 항목 | 설정값 |
|------|--------|
| Auto Target Provider | AutoTargetDirectionProvider 드래그 할당 |
| Attack Joystick Input | (기존 유지, AutoTargetProvider가 없을 때 폴백으로 사용) |

---

### 1-6. TargetingProfile 에셋 생성

`Project 탭` → `우클릭` → `Create > AutoTarget > TargetingProfile`

기본공격용 프로필 예시:

| 항목 | 값 |
|------|----|
| Detection Radius | 6 |
| Distance Weight | 3 |
| Angle Weight | 5 |
| Elite Bonus | 5 |
| MiniBoss Bonus | 15 |
| Boss Bonus | 30 |
| Close Protection Radius | 1.5 |
| Close Protection Bonus | 100 |
| Stickiness Bonus | 10 |
| Target Lost Radius | 10 |

---

### 1-7. 락온 아웃라인 (선택 - TargetOutlineEffect)

각 몬스터 프리팹의 SpriteRenderer가 있는 GameObject에 `TargetOutlineEffect` 컴포넌트를 추가합니다.

**URP 2D Shader Graph 셋업 (요약):**
1. `URP2D_TargetOutline.shadergraph` 파일을 생성 (URP > Shader Graph > 2D Renderer)
2. 다음 Properties 추가:
   - `_OutlineEnabled` (Float, default 0)
   - `_OutlineColor` (Color, HDR 허용)
   - `_OutlineThickness` (Float, default 0.02)
   - `_ThicknessMult` (Float, default 1.0)
   - `_BreatheSpeed` (Float, default 1.5)
   - `_BreatheAmplitude` (Float, default 0.003)
3. `TargetOutlineEffect.cs`를 SpriteRenderer가 있는 GameObject에 부착
4. SpriteRenderer의 Material을 위 Shader Graph 머티리얼로 교체

> TargetOutlineEffect가 없어도 타겟팅 로직(방향, 공격)은 정상 동작합니다. 아웃라인 없이 먼저 테스트해도 됩니다.

---

## 2. 공격 우선순위 (점수 공식)

버튼을 누르는 순간, 범위 내 모든 적에게 아래 점수를 계산하여 가장 높은 적이 타겟이 됩니다.

```
Score = (DetectionRadius - 거리) × DistanceWeight   ← 가까울수록 높음
      + (dot(aimDir, toTarget) + 1) × AngleWeight    ← 바라보는 방향과 일치할수록 높음
      + RankBonus                                     ← 등급 보너스 (고정)
      + CloseProtectionBonus                          ← 1.5m 이내 즉시 최우선
      + StickinessBonus                               ← 현재 타겟 유지 보너스
```

### 우선순위 순서 (기본 프로필 기준)

| 순위 | 조건 | 이유 |
|------|------|------|
| 1위 | 1.5m 이내 접촉 직전의 적 | CloseProtectionBonus +100 (모든 조건 압도) |
| 2위 | 보스 등급 적 (범위 내) | BossBonus +30 |
| 3위 | 미니보스 등급 적 | MiniBossBonus +15 |
| 4위 | 엘리트 등급 적 | EliteBonus +5 |
| 5위 | 현재 타겟 유지 | StickinessBonus +10 (쉽게 바뀌지 않음) |
| 6위 | 바라보는 방향에 있는 적 | AngleWeight 기여 |
| 7위 | 가까운 적 | DistanceWeight 기여 |

---

## 3. 테스트 케이스

### Case 1: 정지 상태에서 기본공격 (A키)

**기대 동작:**
- 플레이어가 오른쪽을 바라보고 있으면 `FacingDirection = Vector2.right` 사용
- 오른쪽 방향에 적이 있으면 그 적이 타겟, 없으면 가장 가까운 적
- 플레이어 스프라이트가 타겟 방향으로 즉시 flip
- 타겟 적 아웃라인 즉시 ON + Pop 애니메이션

**확인 방법:**
- Scene Gizmos: Player에서 타겟을 향하는 노란 선이 보여야 함
- Console: `[AutoTargetResolver] EnemyName → score=XX` 로그 확인 (showDebugLogs=true)

---

### Case 2: 이동 중 공격 (카이팅)

**시나리오:** 보스를 피해 뒤로 도망치면서(↓ 방향) 공격 버튼(A키) 연타

**기대 동작:**
- aimDir = 이동 방향 (↓), 이 방향이 **angle 점수에만 영향**
- 보스는 뒤에 있어도 CloseProtectionBonus 또는 BossBonus로 점수가 높아 여전히 타겟됨
- 공격 방향은 타겟(보스) 방향 → 도망치면서 뒤로 화살 발사
- 스프라이트는 보스 방향으로 flip

**잘못된 동작 (발생하면 버그):**
- 도망치는 방향(플레이어 이동 방향)으로 화살이 날아가는 경우

---

### Case 3: 근접 보호망 발동 (CloseProtection)

**시나리오:** 원거리 보스가 있고, 1.5m 이내에 일반 졸개가 달라붙는 상황

**기대 동작:**
- 졸개의 점수 = Normal 기본점 + 100 (CloseProtectionBonus)
- 보스의 점수 = BossBonus 30 + 거리/각도 점수 (최대 ~100 수준)
- 가까운 졸개를 먼저 타겟 (근접 보호망 > 보스 우선 타겟팅)

**확인 방법:**
- 1.5m 반경 내 졸개 위에 아웃라인이 표시되어야 함

---

### Case 4: 타겟 고정 (Stickiness)

**시나리오:** 타겟을 잠근 상태에서 근처에 다른 적이 등장

**기대 동작:**
- 기존 타겟은 StickinessBonus +10 덕분에 웬만하면 유지
- 새 적이 현저히 가깝거나 더 높은 랭크일 때만 타겟 교체
- 타겟 교체 시: 기존 아웃라인 즉시 OFF, 새 타겟 아웃라인 즉시 ON + Pop

---

### Case 5: 느린 스킬 (useLiveTargetOnCast = true)

**시나리오:** 시전 시간이 0.7초인 스킬을 사용하는 동안 적이 이동

**설정:** `ActiveSkillData.useLiveTargetOnCast = true`

**기대 동작:**
1. 스킬 버튼 탭 시 타겟 결정 (스냅샷)
2. 시전 애니메이션 재생 (0.7초)
3. 애님 이벤트 발동 시 `AutoTargetDirectionProvider.GetLiveTargetDirection()` 호출
4. 이 시점의 타겟 현재 위치로 방향 재계산 → 더 정확히 명중

**AnimEvent에서 호출 예시:**
```csharp
// PlayerAnimationController 또는 스킬 발사 스크립트에서
public void OnSkillCastAnimEvent()
{
    if (skillData.useLiveTargetOnCast && autoTargetProvider != null)
        attackDir = autoTargetProvider.GetLiveTargetDirection();
    else
        attackDir = autoTargetProvider?.LastResolvedDirection ?? facingDir;
    
    FireProjectile(attackDir);
}
```

---

### Case 6: 범위 내 적 없음

**기대 동작:**
- `FindBestTarget()` → null 반환
- 방향 = `GetAimDir()` (이동 중이면 이동 방향, 정지 중이면 FacingDirection)
- 아웃라인 없음, 기존 타겟 있었으면 아웃라인 OFF

---

### Case 7: 타겟이 사망

**기대 동작:**
- 다음 `FindBestTarget()` 호출 시 `IsAlive() = false` 감지 → 타겟 해제
- 아웃라인 OFF
- 다음으로 높은 점수의 적으로 자동 전환

---

## 4. 클래스별 TargetingProfile 튜닝 가이드

### 기본공격 (BasciAttack_Profile)
```
DetectionRadius = 5
DistanceWeight  = 4   ← 가까운 적 중심
AngleWeight     = 4
BossBonus       = 30
CloseProtRadius = 1.5
StickinessBonus = 10
```

### 돌진/찌르기 스킬 (RushSkill_Profile)
```
DetectionRadius = 8   ← 앞에 있는 먼 적도 확인
DistanceWeight  = 2
AngleWeight     = 8   ← 바라보는 방향 강화 (찌르는 방향 중요)
BossBonus       = 20
CloseProtRadius = 0.5
StickinessBonus = 15  ← 한번 타겟 정하면 유지
```

### 원거리/필살기 스킬 (SniperSkill_Profile)
```
DetectionRadius = 15  ← 넓은 범위
DistanceWeight  = 1
AngleWeight     = 3
BossBonus       = 50  ← 보스 최우선
MiniBossBonus   = 30
EliteBonus      = 15
CloseProtRadius = 0.3
StickinessBonus = 20
```

---

## 5. 디버그 방법

### 5-1. Gizmos (Scene 뷰)
- `AutoTargetResolver.showDebugGizmos = true`
- 초록 원: 탐지 반경
- 노란 선 + 원: 현재 락온 타겟

### 5-2. Console 로그
- `AutoTargetResolver.showDebugLogs = true` → 각 적의 점수 출력
- `AutoTargetDirectionProvider.showDebugLogs = true` → Resolve 결과 출력

### 5-3. 런타임 확인 포인트
```csharp
// Inspector 런타임 확인용
var provider = FindObjectOfType<AutoTargetDirectionProvider>();
Debug.Log($"Last Dir: {provider.LastResolvedDirection}");
Debug.Log($"Last Target: {provider.LastResolvedTarget?.GetTransform()?.name}");
```

---

## 6. 자주 발생하는 문제

| 증상 | 원인 | 해결 |
|------|------|------|
| 타겟팅이 전혀 안 됨 | enemyLayer 설정 오류 | AutoTargetResolver의 enemyLayer를 Enemy 레이어로 설정 |
| 아웃라인이 안 보임 | TargetOutlineEffect 없음 또는 머티리얼 미설정 | 몬스터 프리팹에 컴포넌트 추가 + 셰이더 머티리얼 적용 |
| 항상 가장 가까운 적만 타겟 | AngleWeight = 0 | TargetingProfile 값 조정 |
| 보스보다 졸개 타겟 | CloseProtectionBonus가 너무 높음 | CloseProtectionRadius를 줄이거나 Bonus를 낮춤 |
| 타겟이 너무 자주 바뀜 | StickinessBonus가 너무 낮음 | 10 → 20으로 증가 |
| 카이팅 중 이동 방향으로 공격 | AutoTargetProvider가 연결 안 됨 | PlayerAttackInput의 autoTargetProvider 할당 확인 |
