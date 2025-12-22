# 🎮 Phase 1 빠른 테스트 가이드

## ✅ Phase 1 확장 버전 재구현 완료!

**완료된 작업:**
- ✅ SFXLoop 타입 추가 (Step 타입 6종)
- ✅ ICutsceneStepExecutor 인터페이스
- ✅ 6개 Executor 클래스 (Executors 폴더)
- ✅ Factory 패턴 적용
- ✅ CutsceneContext Loop 관리 기능
- ✅ Unity 컴파일 성공

---

## 🧪 테스트 1: 기존 컷신 호환성 (2분)

### **목표**: MVP 컷신이 확장 버전에서도 정상 작동하는지 확인

### **실행 방법**
```
1. Console 창 열기 (Ctrl + Shift + C)
2. Stage_001 씬 열기
3. Play 버튼 클릭
```

### **확인할 로그**
```
[CutsceneStepExecutor] ✅ Executor 초기화 완료: 6개 타입  ← 중요!
[CutsceneManager] ✅ 초기화 완료 (확장 버전)

[SequenceBuilder] ✅ Sequence 생성 완료: Intro_001 (X개 Step)

[ImageStepExecutor] 🖼️ Image Step 시작 - ...
[DialogueStepExecutor] 💬 Dialogue Step 시작 - ...
[WaitStepExecutor] ⏱️ Wait Step 시작 - ...
[SFXOneShotStepExecutor] 🔊 SFX Step 시작 - ...

[CutsceneManager] ✅ 컷신 완료: Intro_001
```

### **체크리스트**
- [ ] Executor 초기화 로그 (6개 타입)
- [ ] 컷신 정상 재생
- [ ] 이미지/대사/효과음 모두 작동
- [ ] 1차 클릭: 타이핑 스킵
- [ ] 2차 클릭: 다음 Step
- [ ] ESC: 컷신 종료

---

## 🔊 테스트 2: Loop SFX 기능 (15분) - 선택사항

### **준비 작업**

#### **1. 테스트 컷신 생성**
```
Project 창:
1. Intro_001_CutsceneData.asset 선택
2. Ctrl + D (복사)
3. 이름: LoopSFX_Test_CutsceneData
4. Inspector:
   - Cutscene Id: "LoopSFX_Test"
```

#### **2. Steps 구성**

**기존 Steps 모두 삭제 후 새로 추가:**

| # | Step Type | 설정 내용 |
|---|-----------|---------|
| 0 | Image | 배경 이미지 표시 |
| 1 | Dialogue | "환경음을 시작합니다..." |
| **2** | **SFXLoop** | **sfxEventKey: "sfx.rain"**, **loopMode: Start** ⭐ |
| 3 | Wait | duration: 3초 |
| 4 | Dialogue | "환경음이 들리고 있습니다..." |
| 5 | Wait | duration: 2초 |
| **6** | **SFXLoop** | **sfxEventKey: "sfx.rain"**, **loopMode: Stop** ⭐ |
| 7 | Dialogue | "환경음이 멈췄습니다." |
| 8 | Image | Fade Out |

**⚠️ 주의**: 
- Step 2, 6의 `sfxEventKey`는 동일해야 함 (쌍으로 작동)
- `loopMode`가 **Start/Stop**으로 제대로 설정되어 있는지 확인!

#### **3. CueProfile 확인 (필수!)**
```
Assets/Resources/CueProfiles/Cutscene_CutsceneProfile.asset

Cues 리스트에서 "sfx.rain" 찾기:
- Event Key: "sfx.rain"
- Is Looping: ✅ 반드시 체크!
- Audio Clip: 빗소리 등 할당

없으면 기존 Loop SFX 키 사용 또는 추가
```

#### **4. SceneStartTrigger 설정**
```
Hierarchy > CutsceneStartTrigger

Inspector:
- Cutscene Data: LoopSFX_Test_CutsceneData 드래그
- Auto Play On Start: ✅
```

---

### **실행 및 확인**

#### **정상 동작 시 로그**
```
[Step 2: Loop 시작]
[SFXLoopStepExecutor] 🔊 Loop SFX 시작: sfx.rain (domain: Cutscene)
[CutsceneContext] ➕ Loop 추가: CuePlayer_XXX (총 1개)
← Loop 효과음이 들리기 시작

[Step 6: Loop 종료]
[SFXLoopStepExecutor] 🔇 Loop SFX 종료: sfx.rain
[CutsceneContext] ➖ Loop 제거 (총 0개 남음)
← Loop 효과음이 멈춤

[컷신 종료]
[CutsceneManager] ✅ 컷신 완료: LoopSFX_Test
[CutsceneContext] 🔇 Loop SFX 정리: 0개
```

#### **체크리스트**
- [ ] Loop 시작 로그 확인
- [ ] Loop 추가 로그 (총 1개)
- [ ] **Loop 효과음이 들림** ← 중요!
- [ ] Loop 종료 로그 확인
- [ ] Loop 제거 로그 (총 0개)
- [ ] **Loop 효과음이 멈춤** ← 중요!

---

### **ESC 스킵 테스트 (Loop 정리 확인)**

#### **테스트 방법**
```
1. Play 버튼 클릭
2. Step 2 (Loop 시작) 이후 즉시 ESC 키 누름
3. Console 로그 확인
```

#### **예상 로그**
```
[SFXLoopStepExecutor] 🔊 Loop SFX 시작: sfx.rain
[CutsceneContext] ➕ Loop 추가: ... (총 1개)

[사용자 ESC 입력]

[CutsceneManager] ⏭️ 컷신 강제 종료 (ESC 스킵)
[CutsceneStepExecutor] ⏭️ 모든 Executor 스킵 처리
[CutsceneContext] 🔇 Loop SFX 정리: 1개  ← 중요! (1개 정리됨)
```

#### **확인 사항**
- [ ] ESC 후 Loop 효과음 즉시 멈춤
- [ ] Console: `Loop SFX 정리: 1개`
- [ ] 게임 정상 재개

---

## 🚨 문제 해결

### **Loop가 시작 안 됨**
```
원인: CueProfile에 키 없음 또는 Is Looping 체크 안 됨

해결:
1. CueProfile 열기
2. "sfx.rain" 찾기
3. Is Looping: ✅ 체크
4. Audio Clip 할당 확인
```

### **Loop가 정리 안 됨**
```
원인: Loop 추가 실패 또는 CleanupAllLoops() 미호출

확인:
1. Console에 "Loop 추가" 로그 있는지
2. ESC 후 "Loop SFX 정리: X개" 로그 확인
```

---

## ✅ Phase 1 완료 기준

### **필수 테스트**
- [ ] Unity 컴파일 성공
- [ ] Executor 초기화 (6개 타입)
- [ ] 기존 컷신 정상 작동
- [ ] 스킵 기능 정상

### **Loop SFX 테스트 (선택)**
- [ ] Loop 시작/종료 로그
- [ ] Loop 효과음 재생/멈춤
- [ ] ESC 스킵 시 정리 확인

---

## 🚀 다음 단계

**테스트 완료 후**:
- ✅ 성공: Phase 2 (고급 타이핑) 또는 Phase 3 (BGM 통합) 진행
- ❌ 문제 발생: Console 에러 메시지 알려주세요

---

**테스트를 시작하세요!** 🎮
