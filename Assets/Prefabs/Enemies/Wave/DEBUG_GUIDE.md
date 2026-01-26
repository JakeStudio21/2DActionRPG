# 🐛 SimpleMob 웨이브 디버그 가이드

## 📋 디버그 로그 활성화

웨이브가 클리어되지 않는 문제를 추적하기 위해 디버그 로그를 활성화하세요.

---

## ✅ 로그 활성화 체크리스트

### **1. WaveSpawner 로그 활성화**

**Hierarchy → WaveSpawner 선택**

```
Inspector → WaveSpawner 컴포넌트
└─ Debug
   └─ Enable Debug Logs: ✅ TRUE
```

---

### **2. WaveController 로그 활성화**

**Hierarchy → WaveController 선택**

```
Inspector → WaveController 컴포넌트
└─ Debug
   └─ Enable Debug Logs: ✅ TRUE
```

---

### **3. SimpleMobManager 로그 활성화**

**Hierarchy → SimpleMobManager 선택**

```
Inspector → SimpleMobManager 컴포넌트
└─ Debug
   └─ Enable Debug Logs: ✅ TRUE
```

---

## 🔍 예상되는 로그 순서

### **정상 플로우**

```
1. 트리거 진입:
🎯 [WaveTriggerZone] WaveTrigger_BossRoom 발동! Trigger Id: PlayerReachedPoint

2. WaveController 시작:
🌊 [WaveController] SimpleMob 웨이브 시작: CH01_ST01_WAVE_03
🔗 [WaveController] WaveSpawner 이벤트 구독 완료
🚀 [WaveController] SimpleMob 웨이브 스폰 시작 명령 전송!

3. WaveSpawner 스폰:
📞 [WaveSpawner] StartWaveExternal() 호출됨 - WaveData: Wave_Test_30
🌊 [WaveSpawner] Wave 1 시작!
📍 [WaveSpawner] 스폰: SimpleMob at (10, 5, 0)
📍 [WaveSpawner] 스폰: SimpleMob at (12, 5, 0)
... (30번 반복)
✅ [WaveSpawner] 스폰 완료: 30마리
🔄 [WaveSpawner] 클리어 조건 체크 시작 - 조건: KillAll, 총 스폰: 30마리

4. 몬스터 처치 중:
🔍 [WaveSpawner] 클리어 체크 - 스폰된 몬스터: 30→28, 살아있는 몬스터: 28
🔍 [WaveSpawner] 클리어 체크 - 스폰된 몬스터: 28→25, 살아있는 몬스터: 25
...

5. 웨이브 클리어:
🔍 [WaveSpawner] 클리어 체크 - 스폰된 몬스터: 3→0, 살아있는 몬스터: 0
✅ [WaveSpawner] 모든 몬스터 처치 완료! 웨이브 클리어!
🏆 [WaveSpawner] Wave 1 완료!
💰 [WaveSpawner] 골드 획득: 100
⭐ [WaveSpawner] 경험치 획득: 50
📣 [WaveSpawner] OnWaveComplete 이벤트 발동 - 구독자 1명
✅ [WaveSpawner] 웨이브 완료 처리 끝!

6. WaveController 완료:
🏆 [WaveController] SimpleMob 웨이브 완료 콜백 받음: CH01_ST01_WAVE_03 (Wave 1)
✅ [WaveController] WaveSpawner 이벤트 구독 해제 완료
📣 [WaveController] OnWaveCompleted 이벤트 발동 - 구독자 1명 (StageManager 등)
✅ [WaveController] SimpleMob 웨이브 완료 처리 끝!

7. 다음 웨이브로 진행!
```

---

## 🐛 문제 진단

### **Case 1: 클리어 체크가 안 됨**

**증상:**
```
✅ [WaveSpawner] 스폰 완료: 30마리
(이후 로그 없음)
```

**원인**: `CheckClearConditionCoroutine()`이 시작 안 됨

**해결**: WaveSpawner 코루틴 문제

---

### **Case 2: 클리어 체크는 되는데 카운트가 안 줄어듦**

**증상:**
```
🔍 [WaveSpawner] 클리어 체크 - 스폰된 몬스터: 30→30, 살아있는 몬스터: 30
(몬스터를 처치해도 카운트 변화 없음)
```

**원인**: SimpleMob이 죽어도 `activeInHierarchy`가 false가 안 됨

**해결**: 
1. SimpleMob이 풀로 제대로 반환되는지 확인
2. `GamePoolManager.ReturnToPool()`이 오브젝트를 비활성화하는지 확인

---

### **Case 3: 클리어는 되는데 다음 웨이브로 안 넘어감**

**증상:**
```
✅ [WaveSpawner] 모든 몬스터 처치 완료! 웨이브 클리어!
🏆 [WaveSpawner] Wave 1 완료!
📣 [WaveSpawner] OnWaveComplete 이벤트 발동 - 구독자 0명 ❌
```

**원인**: WaveController가 이벤트를 구독하지 않음

**해결**:
1. WaveController의 `ExecuteWaveWithDelay()`에서 구독 확인
2. `waveSpawner.OnWaveComplete += OnSimpleMobWaveComplete;` 실행 여부 확인

---

### **Case 4: WaveController는 받았는데 StageManager가 안 받음**

**증상:**
```
🏆 [WaveController] SimpleMob 웨이브 완료 콜백 받음
📣 [WaveController] OnWaveCompleted 이벤트 발동 - 구독자 0명 ❌
```

**원인**: StageManager가 WaveController의 이벤트를 구독하지 않음

**해결**:
1. StageManager가 씬에 있는지 확인
2. StageManager가 WaveController의 `OnWaveCompleted` 이벤트를 구독하는지 확인

---

## 📊 로그 분석 방법

### **Step 1: 어디까지 진행되는지 확인**

로그를 위에서부터 읽으면서 어느 단계에서 멈추는지 확인하세요.

---

### **Step 2: 구독자 수 확인**

```
📣 [WaveSpawner] OnWaveComplete 이벤트 발동 - 구독자 ?명
📣 [WaveController] OnWaveCompleted 이벤트 발동 - 구독자 ?명
```

**구독자가 0명이면 문제!**
- WaveSpawner → WaveController 구독 실패
- WaveController → StageManager 구독 실패

---

### **Step 3: 몬스터 카운트 확인**

```
🔍 [WaveSpawner] 클리어 체크 - 스폰된 몬스터: ?→?, 살아있는 몬스터: ?
```

**살아있는 몬스터 수가 줄어들지 않으면 문제!**
- SimpleMob이 죽어도 풀로 반환 안 됨
- `activeInHierarchy`가 false가 안 됨

---

## 🔧 임시 해결 방법

### **수동 다음 웨이브 트리거**

만약 웨이브가 클리어되었는데도 다음으로 안 넘어간다면:

**Console 창에서 실행:**

```csharp
FindObjectOfType<StageManager>().TriggerNextWave();
```

(이건 임시 방편이고, 근본 원인을 찾아야 합니다)

---

## 📞 도움 요청 시 제공할 정보

1. **Console 로그 전체** (트리거 진입부터 끝까지)
2. **구독자 수** (📣 로그의 숫자)
3. **몬스터 카운트 변화** (🔍 로그의 숫자들)
4. **마지막 로그 메시지** (어디서 멈췄는지)

---

## 🎯 요약

**핵심 확인 사항:**
1. ✅ 디버그 로그 활성화 (WaveSpawner, WaveController, SimpleMobManager)
2. ✅ Console 로그 전체 복사
3. ✅ 구독자 수 확인 (0명이면 문제!)
4. ✅ 몬스터 카운트 변화 확인

**가장 중요한 로그:**
```
📣 [WaveSpawner] OnWaveComplete 이벤트 발동 - 구독자 ?명
📣 [WaveController] OnWaveCompleted 이벤트 발동 - 구독자 ?명
```

구독자가 0명이면 이벤트 연결 문제입니다!

