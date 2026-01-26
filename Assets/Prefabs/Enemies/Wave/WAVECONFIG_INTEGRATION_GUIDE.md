# 🌊 WaveConfig + SimpleMob 통합 가이드

## 📋 목차
1. [개요](#개요)
2. [시스템 구조](#시스템-구조)
3. [Unity Editor 설정](#unity-editor-설정)
4. [예제 시나리오](#예제-시나리오)
5. [트러블슈팅](#트러블슈팅)

---

## 개요

이 가이드는 **기존 WaveConfig 시스템**에 **SimpleMob 웨이브**를 통합하는 방법을 설명합니다.

**핵심 기능**:
- ✅ 트리거 기반 SimpleMob 스폰 (PlayerReachedPoint, BossDefeated 등)
- ✅ 기존 WaveConfig 시스템과 완전 통합
- ✅ 특정 위치 또는 플레이어 기준 스폰
- ✅ 기존 몬스터와 SimpleMob 혼합 가능

---

## 시스템 구조

### **데이터 플로우**

```
StageConfig.asset
    ↓
WaveConfig.asset {
    UseSimpleMobWave: true  ← SimpleMob 사용 플래그
    SimpleMobWaveData: Wave_01.asset  ← SimpleMob 웨이브 데이터
    SimpleMobSpawnCenter: SpawnPoint_A  ← 스폰 위치 (옵션)
}
    ↓
플레이어가 트리거 진입 (예: Boss_Room_Enter)
    ↓
WaveController → WaveSpawner.StartWaveExternal()
    ↓
SimpleMob 30마리 스폰!
```

### **기존 시스템과의 차이**

| 항목 | 기존 WaveConfig | SimpleMob 통합 |
|------|----------------|----------------|
| **몬스터 타입** | BlueSlime, Grape, Ghost 등 | SimpleMob, Exploder, Rusher |
| **스폰 방식** | SpawnGroup (고정 위치) | 동적 패턴 (Circle, Line, Grid) |
| **수량** | 5~10마리 | 30~100마리 |
| **트리거** | WaveConfig 설정 | ✅ 동일하게 사용 가능 |

---

## Unity Editor 설정

### **Step 1: WaveData 생성 (SimpleMob 웨이브)**

이미 완료되어 있다면 스킵하세요.

**경로**: `Assets/Resources/Data/Waves/`

1. **Project 창** 우클릭
2. `Create → 2DActionRPG → Wave System → Wave Data`
3. 이름: `Wave_Boss_Room_SimpleMob`

**Inspector 설정**:
```
Wave Info:
├─ Wave Number: 1
└─ Total Waves: 1

Spawn Configuration:
└─ Spawn Configs:
   └─ Element 0:
      ├─ Mob Prefab: SimpleMob
      ├─ Spawn Count: 30
      ├─ Spawn Interval: 0.1
      ├─ Spawn Pattern: Circle
      └─ Spawn Radius: 10

Clear Condition: KillAll
Rewards:
├─ Reward Gold: 100
└─ Reward Exp: 50
```

---

### **Step 2: WaveConfig 수정 (SimpleMob 연동)**

**경로**: `Assets/Resources/Stages/Waves/`

**예**: `CH01_ST01_WAVE_03_Config.asset` (보스방 웨이브)

**Inspector에서 설정**:

```
기본 정보:
├─ Wave ID: CH01_ST01_WAVE_03
├─ Stage ID: CH01_ST01
└─ Wave Index: 3

시작 조건:
├─ Start Condition: OnTrigger  ← 트리거 기반!
├─ Wave Delay Sec: 0
└─ Trigger Id: PlayerReachedPoint  ← 트리거 ID

🌊 SimpleMob 웨이브 설정:
├─ Use SimpleMob Wave: ✅ TRUE  ← 체크!
├─ Simple Mob Wave Data: Wave_Boss_Room_SimpleMob  ← 위에서 만든 WaveData
└─ Simple Mob Spawn Center: (None 또는 SpawnPoint)  ← 아래 설명 참고

런타임 참조:
└─ Spawn Groups: (비워두기 - SimpleMob은 SpawnGroup 사용 안 함)
```

#### **SimpleMob Spawn Center 옵션**

**옵션 1: None (플레이어 기준 스폰)** ⭐ 권장
- SimpleMobSpawnCenter = **None**
- → SimpleMob이 플레이어 주변에 스폰됨
- → Circle 패턴: 플레이어를 포위 공격
- **사용 시나리오**: 보스방 진입, 특정 구역 진입

**옵션 2: Transform 설정 (고정 위치 스폰)**
- SimpleMobSpawnCenter = **SpawnPoint_BossRoom** (씬 내 GameObject)
- → SimpleMob이 지정된 위치 기준으로 스폰됨
- → Circle 패턴: 해당 위치를 중심으로 원형 스폰
- **사용 시나리오**: 특정 지점에서 몬스터 출현

---

### **Step 3: 씬 설정 (WaveSpawner 배치)**

**Hierarchy 구성**:

```
CH01_ST01 씬
├─ [Managers]
│  ├─ GameManager
│  ├─ StageManager
│  ├─ WaveController
│  ├─ SimpleMobManager  ← 🆕 추가!
│  └─ WaveSpawner  ← 🆕 추가!
│     └─ Inspector 설정:
│        ├─ Current Wave Data: (None) ← autoStartWave = false
│        ├─ Player Transform: Player (자동 할당)
│        ├─ Custom Spawn Center: (None)
│        ├─ Auto Start Wave: ❌ FALSE  ← WaveConfig에서 제어
│        └─ Wave Start Delay: 0
│
├─ [Triggers]
│  └─ Boss_Room_Trigger
│     ├─ Tag: PlayerReachedPoint
│     └─ Collider2D (Is Trigger)
│
└─ [SpawnPoints] (옵션)
   └─ SpawnPoint_BossRoom
      └─ Transform (위치 설정)
```

#### **WaveSpawner 설정 중요 사항**

1. **Auto Start Wave**: ❌ **FALSE로 설정!**
   - WaveConfig에서 트리거 기반으로 제어하므로
   - 자동 시작하면 안 됨

2. **Current Wave Data**: **None으로 둠**
   - WaveConfig에서 SimpleMobWaveData를 전달하므로
   - 미리 할당하지 않음

3. **Player Transform**: 자동으로 찾아짐
   - 수동 할당도 가능

---

### **Step 4: 트리거 설정 (옵션)**

SimpleMobSpawnCenter를 사용하려면:

**GameObject 생성**:
```
Hierarchy 우클릭 → Create Empty
이름: SpawnPoint_BossRoom
Transform:
├─ Position: (보스방 중앙 좌표)
├─ Rotation: (0, 0, 0)
└─ Scale: (1, 1, 1)
```

**WaveConfig에 할당**:
- Inspector → SimpleMob Spawn Center → **SpawnPoint_BossRoom** 드래그

---

## 예제 시나리오

### **시나리오 1: 보스방 진입 시 SimpleMob 대량 스폰**

**목표**: 플레이어가 보스방에 진입하면 SimpleMob 50마리 스폰

**WaveConfig 설정**:
```
CH01_ST01_WAVE_03_Config.asset:
├─ Start Condition: OnTrigger
├─ Trigger Id: PlayerReachedPoint
├─ Use SimpleMob Wave: ✅
├─ Simple Mob Wave Data: Wave_BossRoom_50
└─ Simple Mob Spawn Center: None (플레이어 주변)
```

**WaveData 설정**:
```
Wave_BossRoom_50.asset:
├─ Spawn Configs:
│  └─ Element 0:
│     ├─ SimpleMob × 50마리
│     ├─ Spawn Interval: 0.1초
│     └─ Spawn Pattern: Circle (반경 12f)
└─ Clear Condition: KillAll
```

**결과**:
- 플레이어가 Boss_Room_Trigger 진입
- 5초 동안 SimpleMob 50마리 스폰 (Circle 패턴)
- 플레이어를 포위 공격
- 모두 처치 시 웨이브 클리어

---

### **시나리오 2: 보스 처치 후 보상 웨이브**

**목표**: 보스 처치 후 약한 SimpleMob 20마리 + 골드 대량 획득

**WaveConfig 설정**:
```
CH01_ST01_WAVE_04_Config.asset:
├─ Start Condition: OnTrigger
├─ Trigger Id: BossDefeated  ← 보스 처치 트리거
├─ Use SimpleMob Wave: ✅
├─ Simple Mob Wave Data: Wave_Reward_Easy
└─ Simple Mob Spawn Center: Boss_Center (보스 위치)
```

**WaveData 설정**:
```
Wave_Reward_Easy.asset:
├─ Spawn Configs:
│  └─ Element 0:
│     ├─ SimpleMob × 20마리
│     ├─ Spawn Interval: 0.05초 (빠른 스폰)
│     └─ Spawn Pattern: Random
├─ Clear Condition: KillAll
└─ Reward Gold: 500 (대량 보상!)
```

**결과**:
- 보스 처치 → BossDefeated 트리거
- 1초 안에 SimpleMob 20마리 스폰 (Random 패턴)
- 쉽게 처치 → 골드 500 획득
- 보너스 웨이브 느낌

---

### **시나리오 3: 기존 몬스터 + SimpleMob 혼합**

**목표**: Wave 1은 일반 몬스터, Wave 2는 SimpleMob

**WaveConfig 1 (일반 몬스터)**:
```
CH01_ST01_WAVE_01_Config.asset:
├─ Start Condition: AutoAfterDelay
├─ Use SimpleMob Wave: ❌ FALSE
└─ Spawn Groups: CH01_ST01_G01 (BlueSlime, Grape)
```

**WaveConfig 2 (SimpleMob)**:
```
CH01_ST01_WAVE_02_Config.asset:
├─ Start Condition: OnClearPrev  ← Wave 1 클리어 후
├─ Use SimpleMob Wave: ✅ TRUE
└─ Simple Mob Wave Data: Wave_02_SimpleMob
```

**결과**:
- Wave 1: BlueSlime, Grape 스폰 (기존 시스템)
- Wave 1 클리어 → Wave 2 자동 시작
- Wave 2: SimpleMob 30마리 스폰 (SimpleMob 시스템)
- 두 시스템 완벽 통합!

---

## 트러블슈팅

### ❌ SimpleMob이 스폰되지 않음

**원인 1**: WaveSpawner가 씬에 없음
- **해결**: Hierarchy에 WaveSpawner GameObject 추가

**원인 2**: Use SimpleMob Wave = false
- **해결**: WaveConfig Inspector에서 체크박스 활성화

**원인 3**: SimpleMobWaveData = null
- **해결**: WaveData.asset을 SimpleMobWaveData 필드에 할당

**원인 4**: GamePoolManager에 SimpleMob 풀 없음
- **해결**: ScenePoolConfig에 SimpleMob 풀 추가

---

### ❌ SimpleMob이 이상한 위치에 스폰됨

**원인 1**: SimpleMobSpawnCenter 잘못 설정
- **해결**: 
  - 플레이어 주변 스폰 → **None**
  - 특정 위치 스폰 → 올바른 **Transform** 할당

**원인 2**: Player Transform이 null
- **해결**: WaveSpawner Inspector → Player Transform 할당

---

### ❌ 웨이브가 클리어되지 않음

**원인 1**: SimpleMob이 죽어도 카운트 안됨
- **해결**: 현재 자동 처리됨 (SimpleMobManager가 관리)

**원인 2**: Clear Condition이 잘못됨
- **해결**: WaveData → Clear Condition = **KillAll** 설정

**원인 3**: SimpleMob이 씬 밖으로 이탈
- **해결**: 맵에 Collider 벽 설치

---

### ❌ 기존 몬스터 웨이브가 작동 안 함

**원인**: Use SimpleMob Wave = true인데 SpawnGroups 있음
- **해결**: 
  - SimpleMob만: Use SimpleMob Wave = true + SpawnGroups 비우기
  - 기존 몬스터만: Use SimpleMob Wave = false + SpawnGroups 설정

---

## 📊 체크리스트

### **SimpleMob 웨이브 설정 전**
- [ ] SimpleMob 프리팹 3종 생성 완료
- [ ] ScenePoolConfig에 SimpleMob 풀 추가 완료
- [ ] WaveData.asset 생성 완료

### **WaveConfig 설정**
- [ ] WaveConfig.asset 열기
- [ ] Use SimpleMob Wave = ✅ 체크
- [ ] Simple Mob Wave Data = WaveData.asset 할당
- [ ] Simple Mob Spawn Center = None 또는 Transform 설정
- [ ] Spawn Groups = 비우기 (SimpleMob 전용)
- [ ] Start Condition = 트리거 조건 설정

### **씬 설정**
- [ ] SimpleMobManager GameObject 배치
- [ ] WaveSpawner GameObject 배치
- [ ] Auto Start Wave = ❌ FALSE
- [ ] Current Wave Data = None
- [ ] (옵션) SpawnPoint GameObject 생성 및 할당

### **테스트**
- [ ] Play 모드 진입
- [ ] 트리거 조건 충족 (PlayerReachedPoint 등)
- [ ] SimpleMob 스폰 확인
- [ ] 모두 처치 후 웨이브 클리어 확인

---

## 🎯 요약

**핵심 3단계**:
1. **WaveData 생성** → SimpleMob 웨이브 정의
2. **WaveConfig 수정** → Use SimpleMob Wave = true + SimpleMobWaveData 할당
3. **씬에 WaveSpawner 배치** → Auto Start Wave = false

**트리거 시스템 활용**:
- PlayerReachedPoint: 특정 위치 도달
- BossDefeated: 보스 처치
- OnClearPrev: 이전 웨이브 클리어
- AutoAfterDelay: 자동 시작 (트리거 불필요)

**기존 시스템과 완벽 통합**:
- ✅ WaveConfig 그대로 사용
- ✅ 트리거 시스템 그대로 사용
- ✅ 기존 몬스터와 혼합 가능
- ✅ StageManager가 자동 관리

---

## 🚀 완료!

이제 **기존 WaveConfig 시스템**에서 **SimpleMob 대량 스폰**을 자유롭게 사용할 수 있습니다!

**다음 단계**:
- 다양한 트리거 조건 테스트
- 여러 WaveData 조합 실험
- 보스방, 보너스방 등 특수 상황에 활용

**문제 발생 시**: 위 트러블슈팅 섹션 참고 또는 로그 확인 (🌊 [WaveController], 🌊 [WaveSpawner])

