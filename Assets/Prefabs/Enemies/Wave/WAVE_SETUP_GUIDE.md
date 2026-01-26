# 🌊 SimpleMob 웨이브 시스템 설정 가이드

## 📋 목차
1. [프리팹 설정](#프리팹-설정)
2. [WaveData 생성](#wavedata-생성)
3. [씬 설정](#씬-설정)
4. [테스트](#테스트)

---

## 프리팹 설정

### SimpleMob 기본 프리팹 생성

**1. GameObject 생성**
```
Hierarchy 우클릭 → Create Empty
이름: SimpleMob
```

**2. 필수 컴포넌트 추가**
```
Inspector → Add Component:
├─ Sprite Renderer (몬스터 스프라이트)
├─ Rigidbody2D (Dynamic, Gravity Scale = 0, Freeze Rotation)
├─ Circle Collider 2D × 2개 (Trigger용 + 충돌용)
├─ Animator (옵션)
└─ SimpleMob.cs
```

**3. Circle Collider2D 설정 (중요!)**

**Collider 1 (Trigger - 플레이어 데미지용)**
```
Radius: 0.5
Is Trigger: ✅ TRUE
```

**Collider 2 (충돌 - 몬스터 간 충돌용)**
```
Radius: 0.3
Is Trigger: ❌ FALSE
```

**4. SimpleMob 컴포넌트 설정**
```
기본 스탯:
├─ Max Health: 10
├─ Move Speed: 2
├─ Contact Damage: 5
└─ Detection Range: 15

공격 설정:
└─ Attack Cooldown: 0.5

사운드/이펙트: (옵션)
├─ Hit Sound
├─ Death Sound
└─ Hit Effect Prefab
```

**5. 태그 설정**
```
Inspector 상단 → Tag → SimpleMob (새로 생성)
```

**6. 프리팹 저장**
```
Hierarchy의 SimpleMob을
Project 창의 Assets/Prefabs/Enemies/Wave/ 폴더로 드래그
```

---

### SimpleMobExploder 프리팹 생성

**SimpleMob 프리팹 복제 후:**

**1. SimpleMob.cs 제거 → SimpleMobExploder.cs 추가**

**2. SimpleMobExploder 설정**
```
자폭 설정:
├─ Explosion Radius: 3
├─ Explosion Damage: 20
├─ Explosion Trigger Distance: 1.5
└─ Explosion Delay: 0.5

자폭 이펙트:
├─ Explosion Effect Prefab
├─ Explosion Sound
└─ Glow Color: Red
```

**3. 태그: SimpleMobExploder**

---

### SimpleMobRusher 프리팹 생성

**SimpleMob 프리팹 복제 후:**

**1. SimpleMob.cs 제거 → SimpleMobRusher.cs 추가**

**2. SimpleMobRusher 설정**
```
돌진 설정:
├─ Rush Speed: 8
├─ Rush Duration: 1.5
├─ Rush Cooldown: 3
└─ Rush Damage Multiplier: 2

돌진 이펙트:
├─ Rush Color: Cyan
├─ Rush Effect (ParticleSystem)
└─ Rush Sound
```

**3. 태그: SimpleMobRusher**

---

## WaveData 생성

### WaveData ScriptableObject 생성

**경로**: `Assets/Resources/Data/Waves/`

**1. Project 창 우클릭**
```
Create → 2DActionRPG → Wave System → Wave Data
이름: Wave_Test_30
```

**2. Inspector 설정**

```
웨이브 정보:
├─ Wave Number: 1
└─ Total Waves: 1

스폰 설정:
└─ Spawn Configs (List):
   └─ Element 0:
      ├─ Mob Prefab: SimpleMob (드래그)
      ├─ Spawn Count: 30
      ├─ Spawn Interval: 0.1
      ├─ Spawn Pattern: Circle
      └─ Spawn Radius: 10

클리어 조건:
├─ Clear Condition: KillAll
└─ Time Limit Seconds: 60 (TimeLimit용)

보상:
├─ Reward Gold: 100
└─ Reward Exp: 50
```

---

### 다양한 스폰 패턴 예제

#### Circle 패턴 (원형 배치)
```
Spawn Pattern: Circle
Spawn Radius: 10
→ 플레이어를 중심으로 원형 배치
```

#### Random 패턴 (랜덤 위치)
```
Spawn Pattern: Random
Spawn Radius: 15
→ 반경 내 랜덤 위치
```

#### Line 패턴 (직선 배치)
```
Spawn Pattern: Line
Line Start: (-5, 0)
Line End: (5, 0)
→ 좌→우 직선 배치
```

#### Grid 패턴 (그리드 배치)
```
Spawn Pattern: Grid
Grid Rows: 3
Grid Columns: 3
Grid Spacing: 2
→ 3×3 그리드 배치
```

---

### 혼합 스폰 예제

**Element 0: SimpleMob 20마리 (Circle)**
```
Mob Prefab: SimpleMob
Spawn Count: 20
Spawn Pattern: Circle
Spawn Radius: 10
```

**Element 1: SimpleMobExploder 5마리 (Random)**
```
Mob Prefab: SimpleMobExploder
Spawn Count: 5
Spawn Pattern: Random
Spawn Radius: 12
```

**Element 2: SimpleMobRusher 5마리 (Line)**
```
Mob Prefab: SimpleMobRusher
Spawn Count: 5
Spawn Pattern: Line
Line Start: (-8, 5)
Line End: (8, 5)
```

→ 총 30마리 혼합 스폰!

---

## 씬 설정

### 필수 GameObject 배치

**Hierarchy 구성:**

```
씬 이름 (예: TestStage)
├─ [Managers]
│  ├─ GameManager (기존)
│  ├─ SimpleMobManager  ← 🆕 추가!
│  └─ WaveSpawner  ← 🆕 추가!
│
├─ Player
└─ Map
```

---

### SimpleMobManager 설정

**1. GameObject 생성**
```
Hierarchy → [Managers] 우클릭 → Create Empty
이름: SimpleMobManager
```

**2. SimpleMobManager.cs 추가**
```
Inspector → Add Component → SimpleMobManager
```

**3. Inspector 설정**
```
설정:
├─ AI Update Interval: 0.1
├─ Skip Off Screen Mobs: ✅ TRUE
└─ Screen Padding: 2

디버그:
├─ Enable Debug Logs: ❌
└─ Show Gizmos: ❌
```

---

### WaveSpawner 설정

**1. GameObject 생성**
```
Hierarchy → [Managers] 우클릭 → Create Empty
이름: WaveSpawner
```

**2. WaveSpawner.cs 추가**
```
Inspector → Add Component → WaveSpawner
```

**3. Inspector 설정**
```
Wave Configuration:
├─ Current Wave Data: Wave_Test_30 (드래그)
├─ Player Transform: Player (자동 할당)
└─ Custom Spawn Center: (None)

Spawn Settings:
├─ Auto Start Wave: ✅ TRUE  ← 자동 시작
└─ Wave Start Delay: 1

Debug:
├─ Enable Debug Logs: ✅ (테스트 시)
└─ Show Spawn Gizmos: ✅ (테스트 시)
```

---

### ScenePoolConfig 설정

**경로**: `Assets/Resources/Pool/ScenePoolConfigs/`

**기존 또는 새 ScenePoolConfig에 추가:**

```
Pool Configs:
└─ Element 0:
   ├─ Pool Tag: SimpleMob
   ├─ Prefab: SimpleMob
   ├─ Initial Size: 50  ← 대량 스폰 대비
   └─ Max Size: 100

└─ Element 1:
   ├─ Pool Tag: SimpleMobExploder
   ├─ Prefab: SimpleMobExploder
   ├─ Initial Size: 20
   └─ Max Size: 50

└─ Element 2:
   ├─ Pool Tag: SimpleMobRusher
   ├─ Prefab: SimpleMobRusher
   ├─ Initial Size: 20
   └─ Max Size: 50
```

---

## 테스트

### 기본 테스트 순서

**1. Play 버튼 클릭**

**2. 1초 후 자동 스폰 확인**
- SimpleMob 30마리가 Circle 패턴으로 스폰
- 플레이어를 중심으로 원형 배치

**3. SimpleMob AI 확인**
- 플레이어를 향해 직선 이동
- 접촉 시 0.5초마다 데미지
- 몬스터끼리 겹치지 않고 밀어냄

**4. 전투 테스트**
- 플레이어 공격으로 SimpleMob 처치
- 데미지 숫자 표시 확인
- 죽음 애니메이션 후 풀로 반환

**5. 웨이브 클리어**
- 모두 처치 시 "Wave 1 완료!" 로그
- 골드 100, 경험치 50 획득

---

### 고급 테스트

#### Exploder 테스트
1. SimpleMobExploder 프리팹 사용
2. 플레이어 근접 시 자폭 확인
3. 폭발 범위 데미지 확인

#### Rusher 테스트
1. SimpleMobRusher 프리팹 사용
2. 돌진 패턴 확인 (빠른 속도)
3. 돌진 중 데미지 2배 확인

#### 대량 스폰 테스트
1. Spawn Count: 100으로 설정
2. SimpleMobManager가 효율적으로 관리하는지 확인
3. FPS 드롭 없는지 확인

---

## 트러블슈팅

### ❌ SimpleMob이 스폰되지 않음

**원인 1**: WaveSpawner가 없음
- **해결**: Hierarchy에 WaveSpawner GameObject 추가

**원인 2**: Current Wave Data가 null
- **해결**: WaveData를 Inspector에 할당

**원인 3**: GamePoolManager에 SimpleMob 풀 없음
- **해결**: ScenePoolConfig에 SimpleMob 풀 추가

---

### ❌ SimpleMob이 움직이지 않음

**원인 1**: SimpleMobManager가 없음
- **해결**: Hierarchy에 SimpleMobManager GameObject 추가

**원인 2**: Player Transform이 null
- **해결**: 플레이어에 "Player" 태그 설정

---

### ❌ 플레이어가 SimpleMob을 공격할 수 없음

**원인**: DamageSource.cs에 SimpleMob 체크 없음
- **해결**: 이미 수정됨 (OnTriggerEnter2D에서 SimpleMob 체크)

---

### ❌ SimpleMob끼리 겹침

**원인**: Circle Collider2D (Is Trigger = FALSE) 없음
- **해결**: 
  1. 프리팹에 Circle Collider2D 2개 필요
  2. 하나는 Trigger (플레이어 데미지)
  3. 하나는 Non-Trigger (몬스터 충돌)

---

### ❌ SimpleMob이 연속 공격 안 함

**원인**: OnTriggerEnter2D 사용 (1회만 호출)
- **해결**: 이미 수정됨 (OnTriggerStay2D 사용)

---

## 📊 체크리스트

### 프리팹 준비
- [ ] SimpleMob 프리팹 생성
- [ ] Circle Collider2D 2개 설정 (Trigger + Non-Trigger)
- [ ] SimpleMob.cs 컴포넌트 설정
- [ ] 태그 "SimpleMob" 설정
- [ ] (옵션) SimpleMobExploder, SimpleMobRusher 생성

### WaveData 생성
- [ ] WaveData ScriptableObject 생성
- [ ] Spawn Configs 설정 (Mob Prefab, Count, Pattern)
- [ ] Clear Condition 설정 (KillAll)
- [ ] Reward 설정 (Gold, Exp)

### 씬 설정
- [ ] SimpleMobManager GameObject 추가
- [ ] WaveSpawner GameObject 추가
- [ ] WaveSpawner에 WaveData 할당
- [ ] ScenePoolConfig에 SimpleMob 풀 추가

### 테스트
- [ ] Play 모드 실행
- [ ] SimpleMob 스폰 확인
- [ ] AI 이동 확인
- [ ] 전투 테스트 (공격, 피격, 죽음)
- [ ] 웨이브 클리어 확인 (보상 획득)

---

## 🎯 요약

**핵심 3단계**:
1. **프리팹 생성** → SimpleMob (Collider 2개!)
2. **WaveData 생성** → Spawn Config 설정
3. **씬 배치** → SimpleMobManager + WaveSpawner

**가장 중요한 것**:
- ✅ Circle Collider2D **2개** (Trigger + Non-Trigger)
- ✅ SimpleMobManager **필수** (AI 관리)
- ✅ ScenePoolConfig에 **풀 등록** (대량 스폰)

---

## 🚀 완료!

이제 **대량 SimpleMob 웨이브 시스템**이 완벽하게 작동합니다!

**다음 단계**:
- 다양한 스폰 패턴 실험
- Exploder, Rusher 추가
- 보스방 웨이브 구성

