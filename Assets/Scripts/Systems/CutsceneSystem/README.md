# Cutscene System Design

## 핵심 개념

### 1. 컷신은 "연출(보이기)" 시스템이다
- 컷신의 목적: 스토리를 시각적으로 전달하는 것
- 게임플레이와 분리된 독립적인 연출 시스템
- 이미지, 대사, 효과음을 조합하여 스토리 전달

### 2. 게임 규칙/판정/AI는 컷신 밖에서 처리
- 컷신은 순수하게 "보여주기"만 담당
- 게임 로직 (전투, AI, 스탯 등)은 컷신 외부에서 관리
- 컷신 종료 후 게임 상태는 컷신 이전으로 복구

### 3. 컷신은 ID로 호출한다
- 각 컷신은 고유한 ID를 가짐
- 예시: `Intro_001`, `CH1_MID_002`, `BOSS_ENTER_001`
- 코드에서 ID로 간단하게 호출 가능: `CutsceneManager.PlayCutscene("Intro_001")`

## 설계 원칙

### 데이터 기반 설계
- 컷신 내용은 모두 ScriptableObject 데이터로 관리
- 코드 수정 없이 기획자가 컷신 제작 가능
- Inspector에서 직관적으로 편집

### 모듈화
- 각 Step(이미지, 대사, 대기 등)은 독립적인 단위
- Step을 조합하여 복잡한 컷신 구성
- 재사용 가능한 컴포넌트 구조

### 명확한 제어 흐름
- 컷신 재생 중 여부를 명확히 관리
- 스킵 정책을 일관성 있게 적용
- 컷신 종료 후 게임 상태 복구 보장

## MVP 범위

### 지원하는 Step 타입 (5종)
1. **ImageStep**: 배경/초상/Fade 이미지 표시
2. **DialogueStep**: 화자 이름과 대사 표시 + 타이핑 효과
3. **WaitStep**: 지정된 시간 대기
4. **SFXStep**: 효과음 재생 (CueSystem 연동) + 대기 시간 설정 가능
5. **CallbackStep**: 컷신 종료 후 처리

### 지원하는 트리거 (3종)
1. **SceneStartTrigger**: 씬 시작 시 자동 재생
2. **AreaTrigger**: 플레이어가 특정 영역 진입 시 재생
   - 지연 시간 설정 (delay)
   - 시각적 요소 자동 관리 (컷신 완료 후 숨김)
   - Player Tag 커스터마이징
3. **CodeTrigger**: 코드에서 직접 호출

### BGM 시스템 (새로 추가)
1. **BGMController**: 스택 기반 우선순위 시스템
   - 자동 복귀 기능 (전투 종료 → 기본 BGM)
   - 우선순위: Cutscene > Boss > Battle > Default
2. **SceneBGMStarter**: 씬별 BGM 자동 재생
3. **폴백 시스템**: 스테이지 전용 BGM 없으면 공용 BGM 사용

### 스킵 정책
- **1차 클릭**: 현재 대사 타이핑 즉시 완료
- **2차 클릭**: 다음 Step으로 이동
- **ESC 키**: 컷신 전체 종료

## 📄 문서 구조

- **README.md** (이 파일): 시스템 개요 및 아키텍처
- **Testing_Guide.md**: 테스트 가이드 및 트러블슈팅
- **MVP_COMPLETE.md**: MVP 완료 내역 및 해결된 이슈
- **EXTENSION_TODO.md**: 확장 버전 구현 계획
- **CueSystem_Integration_Guide.md**: CueSystem 연동 가이드

## 확장 계획

### 추가 예정 기능 (EXTENSION_TODO.md 참조)

**High Priority**:
- BGM Fade In/Out
- 자동 폴백 시스템 (스테이지 전용 → 공용)
- 전투/보스 BGM 자동 전환

**Medium Priority**:
- 선택지 시스템 (ChoiceStep)
- 카메라 애니메이션 Step
- 조건부 AreaTrigger

**Low Priority**:
- 컷신 편집기 도구
- 성능 최적화 (Image Pool)
- 컷신 이력 저장

---

## 아키텍처

```
CutsceneSystem
├── Data
│   ├── CutsceneData.cs (ScriptableObject)
│   └── CutsceneStep.cs (데이터 구조)
├── Core
│   ├── CutsceneManager.cs (메인 컨트롤러)
│   ├── CutsceneCanvasLoader.cs (Canvas 로드 관리)
│   └── SequenceBuilder.cs (DOTween Sequence 생성)
├── UI
│   ├── CutsceneImagePanel.cs (이미지 표시)
│   └── DialoguePanel.cs (대사 표시)
└── Triggers
    ├── SceneStartTrigger.cs
    ├── AreaTrigger.cs
    └── (CodeTrigger는 Manager에 내장)
```

## 사용 예시

```csharp
// 1. 씬 시작 시 자동 재생
// SceneStartTrigger 컴포넌트를 씬에 배치하고 Inspector에서 컷신 ID 설정

// 2. 코드에서 호출
CutsceneManager.Instance.PlayCutscene("Intro_001");

// 3. 트리거 영역
// AreaTrigger 컴포넌트를 Collider와 함께 배치
```
