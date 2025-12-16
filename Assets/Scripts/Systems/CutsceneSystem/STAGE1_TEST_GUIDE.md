# 🧪 Stage 1: BGM 확장 테스트 가이드

## 📋 테스트 개요

**완료 날짜**: 2024년 12월 16일  
**구현 내용**:
- ✅ Phase 1.1: BGM Fade In/Out
- ✅ Phase 1.2: 자동 폴백 시스템
- ✅ Phase 1.3: 전투/보스 BGM 자동 전환

---

## 🎯 테스트 목표

1. **BGM Fade**: 씬 전환 시 부드러운 페이드 인/아웃
2. **자동 폴백**: 스테이지 전용 BGM이 없으면 공용 BGM 사용
3. **전투/보스 BGM**: 전투 시작/종료, 보스 등장/처치 시 자동 전환

---

## 🔧 사전 준비

### 1. CueProfile 설정 확인

**위치**: `Resources/CueProfiles/BGM_*.asset`

#### **필수 공용 BGM (반드시 필요)**
```
bgm.stage.default   (탐험 BGM)
bgm.stage.battle    (전투 BGM)
bgm.stage.boss      (보스 BGM)
bgm.login           (로그인 화면)
bgm.lobby           (로비)
```

#### **선택적 스테이지 전용 BGM (테스트용)**
```
bgm.stage.STAGE_003.default  (STAGE_003 전용 탐험)
bgm.stage.STAGE_003.battle   (STAGE_003 전용 전투)
bgm.stage.STAGE_003.boss     (STAGE_003 전용 보스)
```

### 2. SceneBGMStarter 설정 확인

**각 인게임 씬의 SceneBGMStarter 컴포넌트**:
```
Event Key: "bgm.stage.default"  (모든 씬 동일)
Stage Id: "STAGE_001", "STAGE_002", "STAGE_003", ...
```

### 3. BGMController 설정 확인

**BGMController Inspector**:
- BGM Domain: "BGM"
- Fade Time: **0.5** (초) ← 테스트용 (실제 게임에서는 1.0~2.0 권장)
- Enable Debug Logs: **true** (테스트용)

---

## 🧪 테스트 시나리오

### **Test Case 1: BGM Fade In/Out**

#### **목표**: 씬 전환 시 부드러운 페이드

#### **절차**:
1. **로그인씬 → 로비씬 이동**
   - 로그인씬 BGM이 **0.5초 동안 Fade Out**
   - 로비씬 BGM이 **0.5초 동안 Fade In**
   
2. **로비씬 → 인게임씬 (STAGE_001) 이동**
   - 로비씬 BGM이 **0.5초 동안 Fade Out**
   - STAGE_001 BGM이 **0.5초 동안 Fade In**

#### **예상 결과**:
- ✅ BGM이 급격하게 끊기지 않음
- ✅ 페이드 시간 동안 두 BGM이 동시에 들림 (크로스페이드)
- ✅ 콘솔에 Fade 로그 출력:
  ```
  🎵 [CuePlayer] BGM Fade 시작: Loop_BGM_XXX → Loop_BGM_YYY (0.5초)
  ✅ [CuePlayer] 이전 BGM 정리 완료: Loop_BGM_XXX
  ✅ [CuePlayer] BGM Fade 완료: Loop_BGM_YYY
  ```

#### **실패 시 체크**:
- BGMController의 `fadeTime` 값 확인 (0보다 커야 함)
- CuePlayer의 `PlayBGMWithFade()` 호출 여부 확인

---

### **Test Case 2: 자동 폴백 시스템**

#### **목표**: 스테이지 전용 BGM이 없으면 공용 BGM 사용

#### **절차**:

**Case 2-1: 전용 BGM이 없는 스테이지 (STAGE_001)**
1. STAGE_001 씬 로드
2. 콘솔 로그 확인

**예상 로그**:
```
🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.default', StageId: 'STAGE_001'
   → 스테이지 전용 키 생성: 'bgm.stage.STAGE_001.default'
🔍 [CueRegistry] HasKey(BGM.bgm.stage.STAGE_001.default) = false
   ⚠️ 스테이지 전용 키 없음: 'bgm.stage.STAGE_001.default'
   🔄 폴백: 공용 키 사용 'bgm.stage.default'
✅ [BGMController] 🎵 BGM 재생 완료 (Fade 0.5s): bgm.stage.default
```

**Case 2-2: 전용 BGM이 있는 스테이지 (STAGE_003)**
1. **CueProfile에 `bgm.stage.STAGE_003.default` 추가** (사전 준비)
2. STAGE_003 씬 로드
3. 콘솔 로그 확인

**예상 로그**:
```
🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.default', StageId: 'STAGE_003'
   → 스테이지 전용 키 생성: 'bgm.stage.STAGE_003.default'
🔍 [CueRegistry] HasKey(BGM.bgm.stage.STAGE_003.default) = true
   ✅ 스테이지 전용 키 발견: 'bgm.stage.STAGE_003.default'
✅ [BGMController] 🎵 BGM 재생 완료 (Fade 0.5s): bgm.stage.STAGE_003.default
```

#### **예상 결과**:
- ✅ STAGE_001: 공용 탐험 BGM 재생
- ✅ STAGE_003: 전용 탐험 BGM 재생
- ✅ 콘솔에 폴백 여부 명확히 표시

#### **실패 시 체크**:
- CueRegistry의 `HasKey()` 메서드가 제대로 구현되었는지 확인
- BGMController의 `ResolveKey()`에서 HasKey() 호출 여부 확인
- CueProfile에 키가 올바르게 등록되었는지 확인

---

### **Test Case 3: 전투/보스 BGM 자동 전환**

#### **목표**: 전투 시작/종료, 보스 등장/처치 시 자동 BGM 전환

#### **절차**:

**Step 1: 스테이지 입장 (탐험)**
1. STAGE_001 씬 로드
2. 예상 BGM: `bgm.stage.default` (공용 탐험)
3. 콘솔 로그:
   ```
   📥 [BGMController] PlayDefaultBGM() 호출됨 - Key: 'bgm.stage.default', StageId: 'STAGE_001'
   ✅ [BGMController] 🎵 BGM 재생 완료: bgm.stage.default
   ```

**Step 2: 일반 웨이브 시작 (전투)**
1. 첫 번째 웨이브 시작 (보스 없는 웨이브)
2. 예상 BGM: `bgm.stage.battle` (공용 전투)
3. 콘솔 로그:
   ```
   🌊 [StageManager] 웨이브 1/3 시작: STAGE_001_WAVE_01
   🎵 [StageManager] 전투 BGM 시작: STAGE_001
   🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.battle', StageId: 'STAGE_001'
   ✅ [BGMController] 🎵 BGM 재생 완료 (Fade 0.5s): bgm.stage.battle
   ```
4. **예상 효과**: 탐험 BGM → 전투 BGM (0.5초 페이드)

**Step 3: 웨이브 완료 (전투 종료)**
1. 모든 적 처치
2. 예상 BGM: `bgm.stage.default` (탐험 복귀)
3. 콘솔 로그:
   ```
   ✅ [StageManager] 웨이브 완료: STAGE_001_WAVE_01
   🎵 [StageManager] 전투 BGM 종료
   ✅ [BGMController] 🎵 BGM 재생 완료 (Fade 0.5s): bgm.stage.default
   ```
4. **예상 효과**: 전투 BGM → 탐험 BGM (0.5초 페이드)

**Step 4: 보스 웨이브 시작**
1. 보스가 있는 웨이브 시작
2. 예상 BGM: `bgm.stage.boss` (공용 보스)
3. 콘솔 로그:
   ```
   🌊 [StageManager] 웨이브 3/3 시작: STAGE_001_WAVE_03
   🎵 [StageManager] 보스 BGM 시작: STAGE_001
   🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.boss', StageId: 'STAGE_001'
   ✅ [BGMController] 🎵 BGM 재생 완료 (Fade 0.5s): bgm.stage.boss
   ```
4. **예상 효과**: 탐험 BGM → 보스 BGM (0.5초 페이드)

**Step 5: 보스 처치**
1. 보스 체력 0
2. 예상 BGM: `bgm.stage.default` (탐험 복귀)
3. 콘솔 로그:
   ```
   🐲 [StageManager] 보스 처치됨: Boss_XXX - 승리 조건 달성!
   ✅ [StageManager] 웨이브 완료: STAGE_001_WAVE_03
   🎵 [StageManager] 보스 BGM 종료
   ✅ [BGMController] 🎵 BGM 재생 완료 (Fade 0.5s): bgm.stage.default
   ```
4. **예상 효과**: 보스 BGM → 탐험 BGM (0.5초 페이드)

#### **예상 결과**:
- ✅ 전투 시작: 탐험 → 전투 (페이드)
- ✅ 전투 종료: 전투 → 탐험 (페이드)
- ✅ 보스 등장: 탐험 → 보스 (페이드)
- ✅ 보스 처치: 보스 → 탐험 (페이드)
- ✅ 모든 전환이 자동으로 발생
- ✅ 우선순위 스택 정상 작동 (Boss > Battle > Default)

#### **실패 시 체크**:
- StageManager의 `HandleWaveBGM()` 호출 여부 확인
- `CheckIfWaveHasBoss()` 로직 확인 (MonsterID에 "BOSS" 포함 여부)
- BGMController의 `OnBattleStart()`, `OnBossStart()` 등 호출 여부 확인

---

### **Test Case 4: 우선순위 스택 테스트**

#### **목표**: BGM 우선순위 시스템 작동 확인

#### **절차**:
1. 스테이지 입장 (Default)
2. 전투 시작 (Battle 추가)
3. **전투 중 보스 등장** (Boss 추가) ← 핵심
4. 보스 처치 (Boss 제거)
5. 전투 종료 (Battle 제거)

#### **예상 BGM 흐름**:
```
1. 탐험 BGM (Default)
   activeStates = { Default: "bgm.stage.default" }

2. 전투 시작 → 전투 BGM (Battle)
   activeStates = { Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
   → Battle > Default 이므로 전투 BGM 재생

3. 보스 등장 → 보스 BGM (Boss)
   activeStates = { Default: "bgm.stage.default", Battle: "bgm.stage.battle", Boss: "bgm.stage.boss" }
   → Boss > Battle > Default 이므로 보스 BGM 재생

4. 보스 처치 → 전투 BGM 복귀
   activeStates = { Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
   → Battle > Default 이므로 전투 BGM 재생 ✅

5. 전투 종료 → 탐험 BGM 복귀
   activeStates = { Default: "bgm.stage.default" }
   → 탐험 BGM 재생 ✅
```

#### **예상 결과**:
- ✅ 보스 처치 후 **전투 BGM으로 복귀** (탐험이 아님!)
- ✅ 전투 종료 후 **탐험 BGM으로 복귀**
- ✅ 우선순위 스택이 정상 작동

#### **실패 시 체크**:
- BGMController의 `activeStates` Dictionary 상태 확인
- `UpdateBGM()`에서 최고 우선순위 선택 로직 확인

---

### **Test Case 5: 폴백 + 전투 통합**

#### **목표**: 폴백과 전투 BGM이 함께 작동하는지 확인

#### **절차**:
1. **STAGE_001 씬 로드** (전용 BGM 없음)
   - 예상: 공용 탐험 BGM
2. **전투 시작**
   - 예상: 공용 전투 BGM (폴백)
3. **보스 등장**
   - 예상: 공용 보스 BGM (폴백)
4. **보스 처치**
   - 예상: 공용 전투 BGM 복귀
5. **전투 종료**
   - 예상: 공용 탐험 BGM 복귀

#### **예상 결과**:
- ✅ 모든 단계에서 공용 BGM 사용 (폴백)
- ✅ BGM 전환은 정상 작동 (페이드 포함)
- ✅ 콘솔에 폴백 로그 출력

---

## 🐛 트러블슈팅

### **문제 1: BGM이 페이드 없이 즉시 전환됨**

**원인**:
- `fadeTime`이 0으로 설정됨
- `PlayBGMWithFade()`가 호출되지 않음

**해결**:
1. BGMController Inspector에서 `Fade Time` 확인 (0.5 이상 권장)
2. 콘솔에서 `PlayBGMWithFade()` 호출 여부 확인
3. CuePlayer의 `FadeBGM()` 코루틴이 실행되는지 확인

---

### **문제 2: 스테이지 전용 BGM이 재생되지 않음**

**원인**:
- CueProfile에 키가 없음
- 키 이름이 잘못됨
- `HasKey()`가 false 반환

**해결**:
1. CueProfile에서 키 확인:
   ```
   bgm.stage.STAGE_003.default
   bgm.stage.STAGE_003.battle
   bgm.stage.STAGE_003.boss
   ```
2. 키 이름에 오타가 없는지 확인 (대소문자 구분)
3. 콘솔에서 `HasKey()` 결과 확인:
   ```
   🔍 [CueRegistry] HasKey(BGM.bgm.stage.STAGE_003.default) = ???
   ```

---

### **문제 3: 전투/보스 BGM이 자동 전환되지 않음**

**원인**:
- StageManager에서 `HandleWaveBGM()` 호출 안됨
- BGMController가 씬에 없음
- `CheckIfWaveHasBoss()` 로직 오류

**해결**:
1. 콘솔에서 BGMController 호출 로그 확인:
   ```
   🎵 [StageManager] 전투 BGM 시작: STAGE_001
   ```
2. BGMController Inspector에서 `Enable Debug Logs` 활성화
3. `CheckIfWaveHasBoss()`에서 MonsterID 체크 로직 확인

---

### **문제 4: 보스 처치 후 탐험 BGM으로 복귀됨 (전투 BGM이 아님)**

**원인**:
- 전투 상태가 스택에서 제거됨
- 우선순위 스택 로직 오류

**해결**:
1. `OnBattleEnd()`가 호출되는 시점 확인
2. 보스 처치 시 `OnBossEnd()`만 호출되는지 확인
3. 콘솔에서 `activeStates` 상태 로그 확인

---

### **문제 5: 씬 전환 시 BGM이 안 나옴**

**원인**:
- CuePlayer가 DontDestroyOnLoad되지 않음
- AudioListener가 없음
- CueRegistry가 초기화되지 않음

**해결**:
1. CuePlayer가 씬 전환 후에도 살아있는지 확인
2. 씬에 AudioListener가 있는지 확인 (Camera에 보통 있음)
3. 콘솔에서 CuePlayer 초기화 로그 확인:
   ```
   🎵 [CuePlayer] 초기화 완료 - 직참조 차단 활성화
   ```

---

## ✅ 테스트 완료 체크리스트

### **Phase 1.1: BGM Fade In/Out**
- [ ] 씬 전환 시 BGM이 0.5초 동안 페이드 인/아웃
- [ ] 급격한 BGM 전환 없음
- [ ] 콘솔에 Fade 로그 출력

### **Phase 1.2: 자동 폴백 시스템**
- [ ] STAGE_001에서 공용 BGM 재생 (폴백)
- [ ] STAGE_003에서 전용 BGM 재생 (전용 키 있는 경우)
- [ ] 콘솔에 폴백 과정 로그 출력
- [ ] 전투/보스 BGM도 폴백 시스템 작동

### **Phase 1.3: 전투/보스 BGM 자동 전환**
- [ ] 전투 시작 시 전투 BGM 자동 전환
- [ ] 전투 종료 시 탐험 BGM 복귀
- [ ] 보스 등장 시 보스 BGM 자동 전환
- [ ] 보스 처치 시 전투 BGM 복귀 (탐험이 아님!)
- [ ] 전투 종료 시 탐험 BGM 복귀
- [ ] 우선순위 스택 정상 작동

### **통합 테스트**
- [ ] 모든 씬 전환이 부드러운 페이드로 처리됨
- [ ] 50개 스테이지를 위한 폴백 시스템 준비 완료
- [ ] 전투/보스 BGM 전환이 자연스러움
- [ ] 콘솔 로그가 명확하고 유용함

---

## 📝 테스트 결과 기록

### **테스트 환경**:
- Unity 버전: _______________
- 테스트 날짜: _______________
- 테스터: _______________

### **Test Case 1: BGM Fade** (통과/실패)
- 결과: _______________
- 메모: _______________

### **Test Case 2: 자동 폴백** (통과/실패)
- 결과: _______________
- 메모: _______________

### **Test Case 3: 전투/보스 BGM** (통과/실패)
- 결과: _______________
- 메모: _______________

### **Test Case 4: 우선순위 스택** (통과/실패)
- 결과: _______________
- 메모: _______________

### **Test Case 5: 폴백 + 전투 통합** (통과/실패)
- 결과: _______________
- 메모: _______________

### **발견된 버그**:
1. _______________
2. _______________
3. _______________

### **개선 사항**:
1. _______________
2. _______________
3. _______________

---

## 🎓 알려진 제약사항

1. **Fade 시간 고정**: 현재는 모든 BGM 전환에 동일한 fadeTime 적용
   - 향후 개선: 상황별로 다른 Fade 시간 적용 (긴급 상황은 짧게, 느긋한 전환은 길게)

2. **보스 감지 로직**: MonsterID에 "BOSS" 문자열 포함 여부로 판단
   - 향후 개선: WaveConfig에 `isBossWave` 플래그 추가

3. **전투 BGM 활성화 조건**: 모든 웨이브에서 전투 BGM 재생
   - 향후 개선: "대기 웨이브"는 탐험 BGM 유지

---

## 🚀 다음 단계

**Stage 1 테스트 완료 후**:
- [ ] 테스트 결과 정리
- [ ] 버그 수정 (있는 경우)
- [ ] **Stage 2: 컷신 확장 기초** 진행
  - Executor 패턴 리팩토링
  - 고급 타이핑 효과

---

**테스트 가이드 작성 날짜**: 2024년 12월 16일  
**다음 문서**: `STAGE2_IMPLEMENTATION_PLAN.md`
