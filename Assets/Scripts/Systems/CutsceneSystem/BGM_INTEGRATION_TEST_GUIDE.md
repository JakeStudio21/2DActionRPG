# 🎵 BGM 시스템 고도화 테스트 가이드

## ✅ 완료된 구현 내역

### **High Priority (완료)**
1. ✅ **BGM Fade In/Out** - CuePlayer에 구현
2. ✅ **자동 폴백 시스템** - CueRegistry + BGMController 연동
3. ✅ **전투/보스 BGM 자동 전환** - StageManager 연동
4. ✅ **컷신 우선순위 BGM** - CutsceneManager + CutsceneData 통합

### **Phase 3: BGM+컷신 통합 (완료)**
5. ✅ **BGM Step 타입 추가** - 컷신 중 BGM 동적 전환
6. ✅ **BGMStepExecutor 구현** - BGM Step 실행 로직
7. ✅ **Callback을 통한 BGM 제어** - 고급 BGM 제어

---

## 📋 테스트 준비

### **1. Unity 콘솔 설정**
```
Window > General > Console (Ctrl + Shift + C)
- Clear on Play: 체크 해제
- Collapse: 체크 해제 (상세 로그 확인용)
- Error Pause: 체크 (오류 발생 시 일시정지)
```

### **2. 디버그 로그 활성화 확인**
다음 컴포넌트들의 디버그 로그가 활성화되어 있는지 확인:
- `BGMController` → Inspector에서 `Enable Debug Logs` 체크
- `CuePlayer` → Inspector에서 `Show Debug Logs` 체크
- `CueRegistry` → Inspector에서 `Show Debug Logs` 체크
- `CutsceneManager` → Inspector에서 `Enable Debug Logs` 체크
- `StageManager` → Inspector에서 `Enable Debug Logs` 체크

---

## 🧪 테스트 1: BGM Fade In/Out

### **목표**
씬 전환 시 BGM이 부드럽게 페이드 아웃/인되는지 확인

### **테스트 단계**

#### **1-1. 로그인 → 로비 씬 전환**
```
1. 로그인 씬 시작
2. 로그인 완료 후 로비 씬으로 이동
3. Console 확인
```

**예상 로그**:
```
🎵 [BGMController] BGM Fade 시작: Loop_BGM_login → Loop_BGM_lobby (1초)
✅ [CuePlayer] BGM Fade 완료: Loop_BGM_lobby
```

**확인 사항**:
- [ ] BGM이 즉시 바뀌지 않고 부드럽게 전환됨
- [ ] 페이드 시간 동안 두 BGM이 중첩되어 들림 (Crossfade)
- [ ] 페이드 완료 후 이전 BGM이 완전히 정지됨

#### **1-2. 로비 → 인게임 씬 전환**
```
1. 로비에서 스테이지 선택
2. 인게임 씬으로 이동
3. Console 확인
```

**예상 로그**:
```
🎵 [BGMController] BGM Fade 시작: Loop_BGM_lobby → Loop_BGM_stage_default (1초)
✅ [CuePlayer] BGM Fade 완료: Loop_BGM_stage_default
```

**확인 사항**:
- [ ] 로비 BGM이 페이드 아웃됨
- [ ] 스테이지 BGM이 페이드 인됨
- [ ] 전환이 자연스러움

---

## 🧪 테스트 2: 자동 폴백 시스템

### **목표**
스테이지 전용 BGM이 없을 때 공용 BGM으로 자동 폴백되는지 확인

### **테스트 단계**

#### **2-1. 전용 BGM이 없는 스테이지 (예: STAGE_001)**

**준비**:
```
1. Stage_001 씬 열기
2. SceneBGMStarter 컴포넌트 확인:
   - BGM Key: "bgm.stage.default"
   - Stage Id: "STAGE_001"
```

**실행**:
```
1. Play 버튼 클릭
2. Console에서 BGM 키 해석 과정 확인
```

**예상 로그**:
```
📥 [BGMController] PlayDefaultBGM() - Key: 'bgm.stage.default', StageId: 'STAGE_001'
🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.default', StageId: 'STAGE_001'
   → StageId 있음 + 'bgm.stage.' 포함 - 스테이지 전용 키 생성 시도
   → 스테이지 전용 키 생성: 'bgm.stage.STAGE_001.default'
🔍 [CueRegistry] HasKey(BGM.bgm.stage.STAGE_001.default) = false
   ⚠️ 스테이지 전용 키 없음: 'bgm.stage.STAGE_001.default'
   🔄 폴백: 공용 키 사용 'bgm.stage.default'
✅ [BGMController] 🎵 BGM 재생 완료: bgm.stage.default
```

**확인 사항**:
- [ ] 스테이지 전용 BGM을 먼저 찾음 (`bgm.stage.STAGE_001.default`)
- [ ] 전용 BGM이 없으면 공용 BGM으로 폴백 (`bgm.stage.default`)
- [ ] BGM이 정상 재생됨

#### **2-2. 전용 BGM이 있는 스테이지 (예: STAGE_003)**

**준비**:
```
1. CueProfile에서 'bgm.stage.STAGE_003.default' 키 생성
   (테스트용으로 동일한 AudioClip 할당 가능)
2. Stage_003 씬 열기
```

**실행**:
```
1. Play 버튼 클릭
2. Console에서 BGM 키 해석 과정 확인
```

**예상 로그**:
```
🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.default', StageId: 'STAGE_003'
   → 스테이지 전용 키 생성: 'bgm.stage.STAGE_003.default'
🔍 [CueRegistry] HasKey(BGM.bgm.stage.STAGE_003.default) = true
   ✅ 스테이지 전용 키 발견: 'bgm.stage.STAGE_003.default'
✅ [BGMController] 🎵 BGM 재생 완료: bgm.stage.STAGE_003.default
```

**확인 사항**:
- [ ] 스테이지 전용 BGM을 찾았음
- [ ] 전용 BGM이 재생됨 (공용 BGM이 아님)

---

## 🧪 테스트 3: 전투/보스 BGM 자동 전환

### **목표**
전투/보스 시작 시 BGM이 자동으로 전환되고, 종료 시 이전 BGM으로 복귀하는지 확인

### **테스트 단계**

#### **3-1. 일반 웨이브 전투 BGM**

**실행**:
```
1. Stage_001 씬 플레이
2. 첫 웨이브 시작 대기
3. Console 확인
```

**예상 로그**:
```
🌊 [StageManager] 웨이브 1/3 시작: STAGE_001_WAVE_01
🎵 [StageManager] 전투 BGM 시작: STAGE_001
📌 [BGMController] AddState() - Priority: Battle, Key: 'bgm.stage.battle'
🎵 [BGMController] BGM 전환: 'bgm.stage.default' → 'bgm.stage.battle'
```

**확인 사항**:
- [ ] 웨이브 시작 시 전투 BGM으로 전환됨
- [ ] 탐험 BGM에서 전투 BGM으로 자연스럽게 전환됨

**웨이브 완료 후**:
```
✅ [StageManager] 웨이브 완료: STAGE_001_WAVE_01
🎵 [StageManager] 전투 BGM 종료
[BGMController] 상태 제거: Battle (bgm.stage.battle)
🎵 [BGMController] BGM 전환: 'bgm.stage.battle' → 'bgm.stage.default'
```

**확인 사항**:
- [ ] 웨이브 완료 시 탐험 BGM으로 복귀됨
- [ ] BGM 전환이 부드러움 (Fade 적용)

#### **3-2. 보스 웨이브 BGM**

**실행**:
```
1. 보스가 있는 스테이지 플레이 (예: STAGE_001의 보스 웨이브)
2. 보스 웨이브 시작 대기
3. Console 확인
```

**예상 로그**:
```
🌊 [StageManager] 웨이브 시작: STAGE_001_WAVE_BOSS
🐲 [StageManager] 보스 발견: MON_BLUESLIME_BOSS_001 (EnemyType: Boss)
🎵 [StageManager] 보스 BGM 시작: STAGE_001
📌 [BGMController] AddState() - Priority: Boss, Key: 'bgm.stage.boss'
🎵 [BGMController] BGM 전환: 'bgm.stage.battle' → 'bgm.stage.boss'
```

**확인 사항**:
- [ ] 보스 웨이브 시작 시 보스 BGM으로 전환됨
- [ ] 보스 BGM이 전투 BGM보다 우선순위가 높음 (Boss > Battle)

**보스 처치 후**:
```
🐲 [StageManager] 보스 처치됨: MON_BLUESLIME_BOSS_001
🎵 [StageManager] 보스 BGM 종료
[BGMController] 상태 제거: Boss (bgm.stage.boss)
🎵 [BGMController] BGM 전환: 'bgm.stage.boss' → 'bgm.stage.battle'
```

**확인 사항**:
- [ ] 보스 처치 시 이전 BGM(전투 또는 탐험)으로 복귀됨
- [ ] BGM 스택 시스템이 정상 작동함

#### **3-3. 전투 → 보스 → 전투 종료 시퀀스 (전체 플로우)**

**시나리오**:
```
1. 탐험 BGM (Default) 재생 중
2. 일반 전투 시작 → 전투 BGM
3. 보스 등장 → 보스 BGM
4. 보스 처치 → 전투 BGM으로 복귀
5. 전투 종료 → 탐험 BGM으로 복귀
```

**예상 BGM 스택 변화**:
```
1. { Default: "bgm.stage.default" }
   → 탐험 BGM 재생

2. { Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
   → 전투 BGM 재생 (Battle > Default)

3. { Default: "bgm.stage.default", Battle: "bgm.stage.battle", Boss: "bgm.stage.boss" }
   → 보스 BGM 재생 (Boss > Battle > Default)

4. { Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
   → 전투 BGM 복귀 ✅

5. { Default: "bgm.stage.default" }
   → 탐험 BGM 복귀 ✅
```

**확인 사항**:
- [ ] 각 단계에서 가장 높은 우선순위 BGM이 재생됨
- [ ] 상위 우선순위가 제거되면 자동으로 하위 우선순위로 복귀됨
- [ ] BGM 중복 재생이 발생하지 않음

---

## 🧪 테스트 4: 컷신 우선순위 BGM

### **목표**
컷신 시작 시 전용 BGM이 재생되고, 종료 시 이전 BGM으로 복귀하는지 확인

### **테스트 단계**

#### **4-1. 컷신 전용 BGM 설정**

**준비**:
```
1. Resources/CutsceneData/Intro_001_CutsceneData.asset 열기
2. Inspector에서 설정:
   - BGM Event Key: "bgm.intro" (또는 테스트용 다른 키)
   - Resume Previous BGM: ✅ 체크
```

**CueProfile 설정 확인**:
```
1. BGM CueProfile 열기 (Resources/CueProfiles/BGM_bgm_base.asset)
2. "bgm.intro" 키가 존재하는지 확인
   (없으면 테스트용으로 생성)
```

#### **4-2. 로비에서 컷신 재생**

**실행**:
```
1. 로비 씬 플레이
2. 컷신 트리거 (AreaTrigger 또는 코드로 호출)
3. Console 확인
```

**예상 로그**:
```
[CutsceneManager] 컷신 재생 시작: Intro_001
🎵 [CutsceneManager] 컷신 BGM 재생: bgm.intro
📌 [BGMController] AddState() (Public) - Priority: Cutscene, Key: 'bgm.intro'
🎵 [BGMController] BGM 전환: 'bgm.lobby' → 'bgm.intro'
```

**확인 사항**:
- [ ] 컷신 시작 시 컷신 전용 BGM으로 전환됨
- [ ] 로비 BGM이 일시적으로 중단됨
- [ ] 컷신 BGM이 최우선순위로 재생됨 (Cutscene > Boss > Battle > Default)

**컷신 종료 후**:
```
[CutsceneManager] ✅ 컷신 완료: Intro_001
🎵 [CutsceneManager] 컷신 BGM 종료, 이전 BGM으로 복귀
📌 [BGMController] RemoveState() (Public) - Priority: Cutscene
[BGMController] 상태 제거: Cutscene (bgm.intro)
🎵 [BGMController] BGM 전환: 'bgm.intro' → 'bgm.lobby'
```

**확인 사항**:
- [ ] 컷신 종료 시 이전 BGM(로비)으로 복귀됨
- [ ] BGM 스택이 정상적으로 복원됨

#### **4-3. 전투 중 컷신 재생 (우선순위 테스트)**

**시나리오**:
```
1. 스테이지에서 전투 중 (전투 BGM 재생 중)
2. 컷신 트리거 (특수 이벤트)
3. 컷신 완료 후 전투 재개
```

**예상 BGM 스택 변화**:
```
1. 전투 중: { Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
   → 전투 BGM 재생

2. 컷신 시작: { Default: "...", Battle: "...", Cutscene: "bgm.cutscene.event" }
   → 컷신 BGM 재생 (Cutscene > Battle)

3. 컷신 종료: { Default: "...", Battle: "..." }
   → 전투 BGM 복귀 ✅
```

**확인 사항**:
- [ ] 컷신 BGM이 전투 BGM보다 우선순위가 높음
- [ ] 컷신 종료 후 전투 BGM으로 자동 복귀됨
- [ ] 전투 상태가 유지됨 (BGM 스택에서 Battle 상태가 유지됨)

#### **4-4. BGM 없는 컷신 (선택사항)**

**준비**:
```
1. CutsceneData에서 BGM Event Key를 비워둠 (공백)
2. Resume Previous BGM: 체크 여부 무관
```

**실행**:
```
1. 컷신 재생
2. Console 확인
```

**예상 동작**:
- [ ] BGM 전환이 발생하지 않음
- [ ] 이전 BGM이 계속 재생됨
- [ ] 컷신 BGM 관련 로그가 없음

---

## 🧪 테스트 5: 통합 시나리오

### **목표**
전체 시스템이 복합적으로 작동하는지 확인

### **시나리오: 게임 플레이 플로우**

```
1. [로그인] 로그인 BGM 재생
   ↓ (씬 전환, Fade)
2. [로비] 로비 BGM 재생
   ↓ (스테이지 선택, Fade)
3. [스테이지] 탐험 BGM 재생 (Default)
   ↓ (컷신 트리거)
4. [컷신] 컷신 BGM 재생 (Cutscene 우선순위)
   ↓ (컷신 종료)
5. [스테이지] 탐험 BGM 복귀
   ↓ (몬스터 조우)
6. [전투] 전투 BGM 재생 (Battle 우선순위)
   ↓ (보스 등장)
7. [보스전] 보스 BGM 재생 (Boss 우선순위)
   ↓ (보스 처치)
8. [전투] 전투 BGM 복귀
   ↓ (전투 종료)
9. [스테이지] 탐험 BGM 복귀
   ↓ (스테이지 클리어)
10. [승리] 승리 BGM 재생
```

**확인 사항**:
- [ ] 모든 BGM 전환이 자연스러움 (Fade 적용)
- [ ] 우선순위 시스템이 정상 작동함
- [ ] BGM 중복 재생이 없음
- [ ] 각 상황에서 적절한 BGM이 재생됨
- [ ] BGM이 누락되거나 멈추지 않음

---

## ❌ 문제 해결 가이드

### **문제 1: BGM이 재생되지 않음**

**증상**:
- Console에 "BGM 재생 완료" 로그는 있지만 소리가 안 들림

**확인 사항**:
1. **AudioListener 체크**:
   ```
   Console에서 "AudioListener 발견" 로그 확인
   - 없으면: 메인 카메라에 AudioListener 컴포넌트 추가
   ```

2. **AudioClip 할당 체크**:
   ```
   CueProfile에서 해당 키의 AudioClip이 할당되어 있는지 확인
   ```

3. **Volume 체크**:
   ```
   SFXCue의 Volume이 0이 아닌지 확인 (기본값: 1.0)
   ```

4. **AudioSource 상태 체크**:
   ```
   Console에서 "AudioSource 상태 체크" 로그 확인:
   - Is Playing: true 여야 함
   - Volume: 0이 아니어야 함
   - Mute: false 여야 함
   ```

### **문제 2: BGM이 중복 재생됨**

**증상**:
- 두 개 이상의 BGM이 동시에 재생됨

**원인**:
- 이전 BGM이 정리되지 않음

**해결**:
```
1. Console에서 "이전 BGM 정리 완료" 로그 확인
2. CuePlayer의 _currentLoopBGM이 정상적으로 정리되는지 확인
3. Hierarchy에서 "Loop_BGM_" 오브젝트가 여러 개 있는지 확인
   - 있으면: 수동으로 삭제 후 재테스트
```

### **문제 3: 폴백 시스템이 작동하지 않음**

**증상**:
- 스테이지 전용 BGM이 없는데도 오류 발생

**확인 사항**:
```
1. CueRegistry.HasKey() 메서드가 정상 작동하는지 확인
   Console에서 "HasKey(...) = false" 로그 확인

2. 공용 BGM 키가 CueProfile에 등록되어 있는지 확인
   - bgm.stage.default
   - bgm.stage.battle
   - bgm.stage.boss
```

### **문제 4: 컷신 종료 후 BGM이 복귀되지 않음**

**증상**:
- 컷신 종료 후 BGM이 멈춤

**확인 사항**:
```
1. CutsceneData에서 "Resume Previous BGM" 체크 여부 확인
2. Console에서 "컷신 BGM 종료, 이전 BGM으로 복귀" 로그 확인
3. BGMController.RemoveState()가 호출되는지 확인
4. activeStates Dictionary에 이전 상태가 남아있는지 확인
```

### **문제 5: Fade가 작동하지 않음**

**증상**:
- BGM이 즉시 전환됨 (Fade 효과 없음)

**확인 사항**:
```
1. BGMController의 fadeTime 값 확인 (기본값: 1초)
2. CuePlayer.PlayBGMWithFade()가 호출되는지 확인
3. Console에서 "BGM Fade 시작" 로그 확인
4. DOTween이 프로젝트에 설치되어 있는지 확인
```

---

## 📊 예상 로그 전체 예시

### **스테이지 진입 → 전투 → 보스 → 승리**

```
=== 스테이지 진입 ===
📥 [BGMController] PlayDefaultBGM() - Key: 'bgm.stage.default', StageId: 'STAGE_001'
🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.default', StageId: 'STAGE_001'
   → 스테이지 전용 키 생성: 'bgm.stage.STAGE_001.default'
🔍 [CueRegistry] HasKey(BGM.bgm.stage.STAGE_001.default) = false
   🔄 폴백: 공용 키 사용 'bgm.stage.default'
🎵 [CuePlayer] BGM Fade 시작: Loop_BGM_lobby → Loop_BGM_stage_default (1초)
✅ [CuePlayer] BGM Fade 완료: Loop_BGM_stage_default
✅ [BGMController] 🎵 BGM 재생 완료 (Fade 1s): bgm.stage.default

=== 전투 시작 ===
🌊 [StageManager] 웨이브 1/3 시작: STAGE_001_WAVE_01
🎵 [StageManager] 전투 BGM 시작: STAGE_001
📌 [BGMController] AddState() - Priority: Battle, Key: 'bgm.stage.battle'
🎵 [BGMController] BGM 전환: 'bgm.stage.default' → 'bgm.stage.battle'
🎵 [CuePlayer] BGM Fade 시작: Loop_BGM_stage_default → Loop_BGM_stage_battle (1초)
✅ [CuePlayer] BGM Fade 완료: Loop_BGM_stage_battle

=== 보스 등장 ===
🌊 [StageManager] 웨이브 시작: STAGE_001_WAVE_BOSS
🐲 [StageManager] 보스 발견: MON_BLUESLIME_BOSS_001 (EnemyType: Boss)
🎵 [StageManager] 보스 BGM 시작: STAGE_001
📌 [BGMController] AddState() - Priority: Boss, Key: 'bgm.stage.boss'
🎵 [BGMController] BGM 전환: 'bgm.stage.battle' → 'bgm.stage.boss'
🎵 [CuePlayer] BGM Fade 시작: Loop_BGM_stage_battle → Loop_BGM_stage_boss (1초)
✅ [CuePlayer] BGM Fade 완료: Loop_BGM_stage_boss

=== 보스 처치 ===
🐲 [StageManager] 보스 처치됨: MON_BLUESLIME_BOSS_001
🎵 [StageManager] 보스 BGM 종료
[BGMController] 상태 제거: Boss (bgm.stage.boss)
🎵 [BGMController] BGM 전환: 'bgm.stage.boss' → 'bgm.stage.battle'
🎵 [CuePlayer] BGM Fade 시작: Loop_BGM_stage_boss → Loop_BGM_stage_battle (1초)

=== 전투 종료 ===
✅ [StageManager] 웨이브 완료: STAGE_001_WAVE_BOSS
🎵 [StageManager] 전투 BGM 종료
[BGMController] 상태 제거: Battle (bgm.stage.battle)
🎵 [BGMController] BGM 전환: 'bgm.stage.battle' → 'bgm.stage.default'
🎵 [CuePlayer] BGM Fade 시작: Loop_BGM_stage_battle → Loop_BGM_stage_default (1초)

=== 스테이지 클리어 ===
🏁 [StageManager] 스테이지 성공: STAGE_001
(승리 BGM 재생...)
```

---

## ✅ 테스트 완료 체크리스트

### **기능별 체크리스트**

#### **BGM Fade In/Out**
- [ ] 씬 전환 시 Fade 적용됨
- [ ] Crossfade 효과가 자연스러움
- [ ] 이전 BGM이 정상적으로 정리됨

#### **자동 폴백 시스템**
- [ ] 스테이지 전용 키를 먼저 찾음
- [ ] 전용 키가 없으면 공용 키로 폴백
- [ ] 폴백 과정이 Console에 출력됨

#### **전투/보스 BGM**
- [ ] 웨이브 시작 시 전투 BGM 재생
- [ ] 보스 등장 시 보스 BGM 재생
- [ ] 각 종료 시 이전 BGM으로 복귀
- [ ] 우선순위 스택이 정상 작동

#### **컷신 BGM**
- [ ] 컷신 시작 시 전용 BGM 재생
- [ ] 컷신 종료 시 이전 BGM 복귀
- [ ] 컷신 BGM이 최우선순위
- [ ] BGM 없는 컷신도 정상 작동

#### **통합 시나리오**
- [ ] 전체 게임 플로우에서 BGM이 자연스럽게 전환됨
- [ ] BGM 중복 재생이 발생하지 않음
- [ ] 우선순위 시스템이 모든 상황에서 정상 작동
- [ ] 성능 문제 없음 (프레임 드랍, 지연 등)

---

## 🧪 테스트 6: BGM Step (Phase 3)

### **목표**
컷신 중간에 BGM Step을 사용하여 BGM을 동적으로 전환하는지 확인

### **테스트 단계**

#### **6-1. 간단한 BGM Step 테스트**

**준비**:
```
1. CutsceneData 열기 (예: Test_BGMStep_CutsceneData)
2. Steps 구성:
   - Step 1 [Dialogue]: "평화로운 시작"
   - Step 2 [BGM]: bgmEventKey = "bgm.cutscene.peaceful", fadeTime = 1초
   - Step 3 [Wait]: 2초
   - Step 4 [Dialogue]: "긴장감 있는 순간..."
   - Step 5 [BGM]: bgmEventKey = "bgm.cutscene.dramatic", fadeTime = 1초
   - Step 6 [Wait]: 2초
   - Step 7 [Dialogue]: "끝!"
```

**실행**:
```
1. 컷신 재생
2. Console 확인
```

**예상 로그**:
```
💬 [DialogueStepExecutor] Dialogue Step 시작 - "평화로운 시작"
🎵 [BGMStepExecutor] BGM Step 시작 - Key: 'bgm.cutscene.peaceful', Fade: 1초
📌 [BGMController] AddState() - Priority: Cutscene, Key: 'bgm.cutscene.peaceful'
🎵 [BGMController] BGM 전환: '...' → 'bgm.cutscene.peaceful'
✅ [BGMStepExecutor] BGM Step 완료: bgm.cutscene.peaceful

⏱️ [WaitStepExecutor] Wait Step 시작 - 2초

💬 [DialogueStepExecutor] Dialogue Step 시작 - "긴장감 있는 순간..."
🎵 [BGMStepExecutor] BGM Step 시작 - Key: 'bgm.cutscene.dramatic', Fade: 1초
📌 [BGMController] AddState() - Priority: Cutscene, Key: 'bgm.cutscene.dramatic'
🎵 [BGMController] BGM 전환: 'bgm.cutscene.peaceful' → 'bgm.cutscene.dramatic'
✅ [BGMStepExecutor] BGM Step 완료: bgm.cutscene.dramatic
```

**확인 사항**:
- [ ] BGM Step이 정상 실행됨
- [ ] BGM이 Step에서 지정한 키로 전환됨
- [ ] Fade Time이 올바르게 적용됨
- [ ] 여러 BGM Step이 순차적으로 작동함

#### **6-2. 복잡한 BGM Step 시나리오**

**시나리오**: "평화 → 긴장 → 전투"
```
Step 1 [Image]: 평화로운 마을 배경
Step 2 [BGM]: bgm.cutscene.peaceful (평화)
Step 3 [Dialogue]: "오늘도 평화로운 하루군요."
Step 4 [Wait]: 1초
Step 5 [Image]: 어두운 그림자
Step 6 [BGM]: bgm.cutscene.ominous (불길함)
Step 7 [Dialogue]: "뭔가 이상한데..."
Step 8 [SFX]: sfx.alarm (경보음)
Step 9 [BGM]: bgm.stage.battle (전투)
Step 10 [Dialogue]: "전투 준비!"
```

**확인 사항**:
- [ ] 각 BGM이 순차적으로 전환됨
- [ ] 이미지/대사와 BGM이 자연스럽게 조화됨
- [ ] SFX와 BGM이 충돌하지 않음

---

## 📝 다음 단계

### **BGM 시스템 + Phase 3 테스트 완료 후**:
1. ✅ 모든 테스트 통과 확인
2. 🎮 선택적: Phase 2 (고급 타이핑 효과) 진행
   - DOText + Fallback 구현
   - 리치 텍스트 지원
   - 타이핑 속도 조절
3. 🎮 또는 다른 게임 시스템 개발로 이동

### **추가 학습 자료**:
- `PHASE3_BGM_CONTROL_EXAMPLES.md` - BGM Step 상세 사용법
- `EXTENSION_TODO.md` - 향후 확장 계획

### **버그 발견 시**:
1. 문제 상황을 정확히 기록
2. Console 로그 캡처
3. 문제 해결 가이드 참조
4. 필요 시 에이전트에게 보고

---

## 🎉 최종 완료 체크리스트

### **BGM 시스템 고도화**
- [ ] BGM Fade In/Out 테스트 완료
- [ ] 자동 폴백 시스템 테스트 완료
- [ ] 전투/보스 BGM 테스트 완료
- [ ] 컷신 우선순위 BGM 테스트 완료

### **Phase 3: BGM+컷신 통합**
- [ ] BGM Step 테스트 완료
- [ ] 복잡한 시나리오 테스트 완료
- [ ] 우선순위 스택 테스트 완료

---

**테스트 가이드 최종 완료!**  
모든 테스트가 통과하면 BGM 시스템이 완전히 완성됩니다! 🎉

**Phase 3 완료를 축하합니다!** 🎊





