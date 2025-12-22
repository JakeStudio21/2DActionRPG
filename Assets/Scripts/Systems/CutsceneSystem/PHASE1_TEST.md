# 🧪 Phase 1 테스트 가이드 (최종)

## ✅ Phase 1 구현 내역
- Executor 패턴 적용 (5개 Executor)
- Factory 패턴
- Context 확장
- MVP 100% 호환

---

## 🎮 테스트 1: 기존 컷신 호환성 (필수)

### **목표**
기존 MVP 컷신이 Executor 패턴으로 리팩토링 후에도 정상 작동하는지 확인

### **단계**

#### **Step 1: Unity 에디터에서 Console 열기**
```
메뉴: Window > General > Console
단축키: Ctrl + Shift + C

Console 설정:
- Collapse: 체크 해제 (모든 로그 보기)
- Clear on Play: 체크 (Play 시 자동 클리어)
- Error Pause: 체크 해제
```

#### **Step 2: 테스트 씬 열기**
```
Project 창에서:
Scenes/Stage_001.unity (또는 컷신이 있는 씬)
더블클릭하여 열기
```

#### **Step 3: Hierarchy 확인**
```
Hierarchy 창에서 "CutsceneStartTrigger" 검색

있으면:
- Inspector에서 Cutscene Data 확인
- Intro_001_CutsceneData 할당되어 있는지 확인

없으면:
- 다른 트리거 방식 사용 중
- 또는 코드로 직접 호출하는 구조
```

#### **Step 4: Play 버튼 클릭**
```
상단 툴바의 ▶️ Play 버튼 클릭
또는 단축키: Ctrl + P
```

#### **Step 5: Console 로그 확인**

**✅ 정상 동작 시 로그:**
```
[CutsceneStepExecutor] ✅ Executor 초기화 완료: 5개 타입
[CutsceneManager] ✅ 초기화 완료 (확장 버전)

[컷신 시작 시]
[CutsceneManager] 컷신 재생 시작: Intro_001
[SequenceBuilder] ✅ Sequence 생성 완료: Intro_001 (X개 Step)

[각 Step 실행 시]
[ImageStepExecutor] 🖼️ Image Step 시작 - sprite: Background_001, layer: 배경
[DialogueStepExecutor] 💬 Dialogue Step 시작 - text: 어둠의 마법사가...
[WaitStepExecutor] ⏱️ Wait Step 시작 - duration: 1초
[SFXOneShotStepExecutor] 🔊 SFX Step 시작 - key: image_appear, domain: Cutscene

[컷신 종료 시]
[CutsceneManager] ✅ 컷신 완료: Intro_001
```

**❌ 에러 발생 시:**
- Console에 빨간색 에러 메시지
- 에러를 더블클릭하면 해당 코드로 이동
- 에러 메시지를 복사해서 알려주세요

---

## 🎯 체크포인트

### **필수 확인 사항**

#### **1. Executor 초기화**
```
로그 확인:
[CutsceneStepExecutor] ✅ Executor 초기화 완료: 5개 타입

문제:
- "5개 타입"이 아닌 다른 숫자 → Executor 개수 문제
- 로그가 안 나옴 → CutsceneManager Awake 문제
```

#### **2. 컷신 재생**
```
확인:
- 배경 이미지가 화면에 표시됨
- 대사 텍스트가 타이핑 효과로 나타남
- 효과음이 들림
- 컷신이 끝까지 진행됨

문제:
- 화면에 아무것도 안 나옴 → Canvas 로딩 문제
- 대사가 안 나옴 → DialoguePanel 문제
- 효과음 안 들림 → CueSystem 문제
```

#### **3. 스킵 기능**
```
테스트 A: 1차 클릭 (타이핑 스킵)
1. 대사 타이핑 중에 마우스 좌클릭
2. 타이핑이 즉시 완료되는지 확인

예상 로그:
[DialogueStepExecutor] ⏭️ 타이핑 즉시 완료

테스트 B: 2차 클릭 (다음 Step)
1. 타이핑 완료 후 다시 마우스 좌클릭
2. 다음 Step으로 이동하는지 확인

테스트 C: ESC 키 (전체 종료)
1. 컷신 진행 중 ESC 키 누름
2. 컷신이 즉시 종료되는지 확인

예상 로그:
[CutsceneManager] ⏭️ 컷신 강제 종료 (ESC 스킵)
[CutsceneStepExecutor] ⏭️ 모든 Executor 스킵 처리
```

---

## 📋 테스트 결과 보고 양식

### **성공 케이스**
```
✅ Phase 1 테스트 완료

확인 사항:
- Executor 초기화: 5개 타입 ✅
- 컷신 정상 재생: ✅
- 이미지 표시: ✅
- 대사 타이핑: ✅
- 효과음 재생: ✅
- 1차 스킵 (타이핑): ✅
- 2차 스킵 (다음 Step): ✅
- ESC 스킵 (전체 종료): ✅

다음 단계: Phase 2 또는 Phase 3 진행해주세요
```

### **실패 케이스**
```
❌ Phase 1 테스트 실패

문제:
[Console 에러 메시지 복사]
또는
[발생한 문제 상황 설명]

예:
- Executor 초기화 안 됨
- 컷신이 재생 안 됨
- 스킵이 작동 안 함
- 등등
```

---

## 🚨 자주 발생하는 문제

### **문제 1: 컷신이 시작 안 됨**
```
원인:
- SceneStartTrigger가 없거나 비활성화
- CutsceneData가 할당 안 됨
- 씬에 컷신 트리거가 없음

확인:
1. Hierarchy에서 CutsceneStartTrigger 검색
2. Inspector에서 Cutscene Data 확인
3. Auto Play On Start 체크 확인
```

### **문제 2: Console에 로그가 너무 많음**
```
해결:
1. Console 우측 상단 검색창 사용
2. "Executor" 입력 → Executor 관련 로그만 필터링
3. "Cutscene" 입력 → 컷신 관련 로그만 필터링
```

### **문제 3: 에러가 발생했는데 내용을 모르겠음**
```
해결:
1. Console에서 에러 메시지 선택
2. 우클릭 > Copy
3. 전체 메시지를 복사해서 알려주세요
```

---

## 📊 현재 상태 요약

### **구현 완료**
```
✅ ICutsceneStepExecutor 인터페이스
✅ 5개 Executor 클래스
   - ImageStepExecutor
   - DialogueStepExecutor
   - WaitStepExecutor
   - SFXOneShotStepExecutor
   - CallbackStepExecutor
✅ CutsceneStepExecutor (Factory)
✅ CutsceneContext 확장
✅ SequenceBuilder 수정
✅ CutsceneManager 수정
✅ Unity 컴파일 성공
```

### **MVP 호환성**
```
✅ 기존 5가지 Step 타입 유지
✅ 스킵 기능 유지
✅ 타이핑 효과 유지
✅ CueSystem 연동 유지
✅ 기존 CutsceneData 에셋 그대로 사용 가능
```

### **다음 단계 옵션**
```
Phase 2: 고급 타이핑 효과 [1-2일]
- DOText + Fallback
- 리치 텍스트 지원

Phase 3: BGM+컷신 통합 [1-2일]
- 컷신 전용 BGM
- BGM 우선순위
```

---

## 🎯 테스트 시작!

**지금 Unity 에디터에서 테스트를 시작하세요:**

1. Console 열기 (Ctrl + Shift + C)
2. Stage_001 씬 열기
3. Play 버튼 클릭 (Ctrl + P)
4. Console 로그 확인
5. 컷신 작동 확인
6. 스킵 기능 테스트

**테스트 완료 후 결과를 알려주세요!**

---

**테스트 결과에 따라 다음 단계를 진행하겠습니다!** 🚀
