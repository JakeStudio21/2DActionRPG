# 🎯 Wave 트리거 시스템 사용 가이드

## 📋 개요

**WaveTriggerZone**을 사용하면 플레이어가 특정 위치에 도달했을 때 자동으로 SimpleMob 웨이브를 시작할 수 있습니다.

---

## 🚀 빠른 시작 (5단계)

### **Step 1: WaveConfig 설정**

**경로**: `Assets/Resources/Stages/Waves/CH01_ST01_WAVE_03_Config.asset`

```
기본 정보:
├─ Wave ID: CH01_ST01_WAVE_03
└─ Wave Index: 3

시작 조건:
├─ Start Condition: OnTrigger  ← 🔑 중요!
├─ Wave Delay Sec: 0
└─ Trigger Id: PlayerReachedPoint  ← 🔑 중요!

🌊 SimpleMob 웨이브 설정:
├─ Use SimpleMob Wave: ✅ TRUE
├─ Simple Mob Wave Data: Wave_Test_30
└─ Simple Mob Spawn Center: None

런타임 참조:
└─ Spawn Groups: (비워두기)
```

---

### **Step 2: 트리거 존 생성**

**Hierarchy에서:**

```
우클릭 → Create Empty
이름: WaveTrigger_BossRoom
```

---

### **Step 3: Collider2D 추가**

**Inspector에서:**

```
Add Component → Box Collider 2D
└─ Is Trigger: ✅ TRUE  ← 반드시 체크!
└─ Size: 원하는 크기 (예: 5 × 3)
```

**또는 Circle Collider 2D:**

```
Add Component → Circle Collider 2D
└─ Is Trigger: ✅ TRUE
└─ Radius: 3
```

---

### **Step 4: WaveTriggerZone 스크립트 추가**

**Inspector에서:**

```
Add Component → WaveTriggerZone

트리거 설정:
├─ Trigger Id To Activate: PlayerReachedPoint  ← WaveConfig와 일치!
├─ Trigger Once: ✅ TRUE (한 번만 발동)
└─ Disable After Trigger: ✅ TRUE (발동 후 비활성화)

디버그:
└─ Enable Debug Logs: ✅ TRUE (테스트 시)
```

---

### **Step 5: 위치 배치**

**Transform 설정:**

```
Position: (보스방 입구 좌표)
Rotation: (0, 0, 0)
Scale: (1, 1, 1)
```

**Scene View에서 Collider 크기 조절:**
- Scene View에서 Collider가 주황색으로 표시됨
- Collider를 적절한 크기로 조절 (플레이어가 진입할 만한 크기)

---

## 🎮 작동 방식

### **플로우**

```
1. 플레이어가 WaveTriggerZone 진입
   ↓
2. WaveTriggerZone이 StageManager.TriggerWave(PlayerReachedPoint) 호출
   ↓
3. WaveController가 해당 TriggerId를 가진 WaveConfig 찾기
   ↓
4. WaveConfig의 SimpleMobWaveData 확인
   ↓
5. WaveSpawner.StartWaveExternal() 호출
   ↓
6. SimpleMob 30마리 스폰!
```

---

## 🔧 고급 설정

### **트리거 ID 종류**

현재 사용 가능한 트리거:

```csharp
public enum WaveTriggerId
{
    None,
    BossGateOpened,      // 보스 게이트 오픈 시
    PlayerReachedPoint,  // 플레이어 위치 도달 시 ⭐ 권장
    TimerExpired         // 타이머 만료 시
}
```

**PlayerReachedPoint가 가장 범용적!**

---

### **재사용 가능한 트리거**

**설정:**

```
Trigger Once: ❌ FALSE
Disable After Trigger: ❌ FALSE
```

**결과:**
- 플레이어가 재진입할 때마다 트리거 발동
- 무한 웨이브 모드 가능

---

### **여러 웨이브를 순차 트리거**

**예: 보스방에서 3단계 웨이브**

**WaveTrigger_1 (입구)**
```
Trigger Id: PlayerReachedPoint
→ WAVE_01 (SimpleMob 20마리)
```

**WaveTrigger_2 (중앙)**
```
Trigger Id: PlayerReachedPoint
→ WAVE_02 (SimpleMob 30마리)
```

**WaveTrigger_3 (보스 앞)**
```
Trigger Id: PlayerReachedPoint
→ WAVE_03 (SimpleMob 50마리)
```

**주의:** 각 WaveConfig의 **Trigger Id**는 같아도 되지만, **Wave Index**는 달라야 합니다!

---

## 📍 배치 예제

### **예제 1: 보스방 입구**

**목표:** 플레이어가 보스방에 진입하면 SimpleMob 스폰

**Hierarchy:**

```
BossRoom
├─ BossRoom_Entrance (Empty GameObject)
│  ├─ Transform: Position = (10, 5, 0)
│  ├─ Box Collider 2D: Size = (4, 2), Is Trigger = ✅
│  └─ WaveTriggerZone:
│     └─ Trigger Id: PlayerReachedPoint
│
└─ Boss (보스 오브젝트)
```

**WaveConfig:**

```
CH01_ST01_WAVE_BOSS_Config.asset:
├─ Start Condition: OnTrigger
├─ Trigger Id: PlayerReachedPoint
└─ Use SimpleMob Wave: ✅
```

**결과:**
- 플레이어가 (10, 5) 부근 진입 → SimpleMob 스폰!

---

### **예제 2: 함정 방**

**목표:** 플레이어가 보물상자 근처에 가면 SimpleMob 포위

**Hierarchy:**

```
TreasureRoom
├─ TreasureChest (보물상자)
├─ WaveTrigger_Trap (Empty GameObject)
│  ├─ Transform: Position = (보물상자 위치)
│  ├─ Circle Collider 2D: Radius = 2, Is Trigger = ✅
│  └─ WaveTriggerZone:
│     └─ Trigger Id: PlayerReachedPoint
```

**WaveData:**

```
Wave_Trap_Ambush.asset:
├─ Spawn Pattern: Circle (플레이어 포위!)
└─ Spawn Count: 40
```

**결과:**
- 플레이어가 보물상자 근처 접근 → 갑자기 SimpleMob 40마리 포위!

---

### **예제 3: 구역별 웨이브**

**목표:** 맵을 3구역으로 나누고 각 구역마다 웨이브

**Hierarchy:**

```
Stage
├─ Zone_1
│  └─ WaveTrigger_Zone1
│     └─ Trigger Id: PlayerReachedPoint
│
├─ Zone_2
│  └─ WaveTrigger_Zone2
│     └─ Trigger Id: PlayerReachedPoint
│
└─ Zone_3
   └─ WaveTrigger_Zone3
      └─ Trigger Id: PlayerReachedPoint
```

**WaveConfig:**

```
WAVE_ZONE1_Config: Trigger Id = PlayerReachedPoint, Wave Index = 1
WAVE_ZONE2_Config: Trigger Id = PlayerReachedPoint, Wave Index = 2
WAVE_ZONE3_Config: Trigger Id = PlayerReachedPoint, Wave Index = 3
```

**중요:** **Wave Index가 다르면** 같은 Trigger Id를 사용해도 됩니다!

---

## 🐛 트러블슈팅

### ❌ 트리거가 발동되지 않음

**원인 1**: Collider2D의 Is Trigger가 체크 안 됨
- **해결**: Inspector → Collider2D → Is Trigger ✅

**원인 2**: WaveConfig의 Start Condition이 OnTrigger가 아님
- **해결**: WaveConfig → Start Condition = **OnTrigger**

**원인 3**: Trigger Id 불일치
- **해결**: WaveTriggerZone의 Trigger Id와 WaveConfig의 Trigger Id가 **정확히 일치**해야 함

**원인 4**: Player Layer가 잘못됨
- **해결**: Player GameObject → Layer = **Player** (대소문자 정확히)

---

### ❌ 트리거가 여러 번 발동됨

**원인**: Trigger Once가 체크 안 됨
- **해결**: WaveTriggerZone → Trigger Once ✅

---

### ❌ SimpleMob이 스폰 안 됨

**원인**: WaveConfig에 SimpleMobWaveData가 할당 안 됨
- **해결**: WaveConfig → Use SimpleMob Wave ✅ + SimpleMobWaveData 할당

---

### ❌ Scene View에서 트리거 영역이 안 보임

**원인**: Gizmos가 꺼져 있음
- **해결**: Scene View 우측 상단 **Gizmos** 버튼 클릭

---

## 📊 체크리스트

### **WaveConfig 설정**
- [ ] Start Condition = **OnTrigger**
- [ ] Trigger Id = **PlayerReachedPoint** (또는 다른 ID)
- [ ] Use SimpleMob Wave = ✅
- [ ] SimpleMobWaveData 할당 완료

### **WaveTriggerZone 설정**
- [ ] GameObject 생성 완료
- [ ] Collider2D 추가 완료
- [ ] Is Trigger = ✅ 체크
- [ ] WaveTriggerZone 스크립트 추가
- [ ] Trigger Id = WaveConfig와 **일치**
- [ ] 위치 배치 완료

### **씬 설정**
- [ ] StageManager 존재 확인
- [ ] Player Layer = "Player" 확인
- [ ] SimpleMobManager 배치
- [ ] WaveSpawner 배치 (Auto Start Wave = FALSE)

### **테스트**
- [ ] Play 모드 실행
- [ ] 트리거 존 진입
- [ ] 트리거 발동 로그 확인
- [ ] SimpleMob 스폰 확인

---

## 🎯 요약

**핵심 3단계:**
1. **WaveConfig 설정** → Start Condition: OnTrigger, Trigger Id: PlayerReachedPoint
2. **WaveTriggerZone 생성** → Collider2D (Is Trigger ✅) + WaveTriggerZone 스크립트
3. **위치 배치** → 플레이어가 진입할 만한 곳에 배치

**가장 중요한 것:**
- ✅ Collider2D의 **Is Trigger** 체크!
- ✅ WaveConfig와 WaveTriggerZone의 **Trigger Id 일치**!
- ✅ WaveConfig의 **Start Condition = OnTrigger**!

---

## 🚀 완료!

이제 **트리거 기반 SimpleMob 웨이브 시스템**이 완벽하게 작동합니다!

**다음 단계:**
- 다양한 위치에 트리거 존 배치
- 보스방, 함정방, 보너스방 등 활용
- 재사용 가능한 무한 웨이브 모드 테스트

**문제 발생 시**: 위 트러블슈팅 섹션 참고 또는 로그 확인 (🎯 [WaveTriggerZone])

