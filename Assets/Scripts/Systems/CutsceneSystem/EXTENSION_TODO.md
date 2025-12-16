# 🚀 컷신 시스템 확장 버전 TODO

## 📌 시작하기 전에

**MVP 완료 상태 확인**: `MVP_COMPLETE.md` 참조

**이 문서는 확장 버전 구현을 위한 TODO 리스트입니다.**

---

## 🎯 확장 버전 목표

### **1. BGM 시스템 고도화**:
- [ ] BGM Fade In/Out 구현
- [ ] 자동 폴백 시스템 구현 (스테이지 전용 → 공용)
- [ ] 전투/보스 BGM 자동 전환
- [ ] 컷신 우선순위 BGM 지원
- [ ] BGM Volume 개별 제어

### **2. 컷신 시스템 개선**:
- [ ] 컷신 중 BGM 제어 (Step 추가 또는 Callback)
- [ ] 카메라 애니메이션 Step
- [ ] 선택지 Step (Branching)
- [ ] 변수 시스템 (플래그, 카운터)
- [ ] 컷신 편집기 (Custom Editor)

### **3. AreaTrigger 고도화**:
- [ ] 조건부 트리거 (플래그 체크)
- [ ] 복수 컷신 지원 (순차 재생)
- [ ] Visual Element 애니메이션
- [ ] 재진입 정책 (N회 재생, 시간 제한)

### **4. 성능 최적화**:
- [ ] Image Pool 시스템
- [ ] Dialogue Panel Pool
- [ ] Canvas 미리 로딩
- [ ] 메모리 사용량 체크

---

## 🎵 1. BGM 시스템 고도화

### **1.1 BGM Fade In/Out 구현**

#### **목표**:
- BGM 전환 시 부드럽게 페이드 인/아웃
- Fade 시간 설정 가능

#### **구현 내용**:

**CuePlayer.cs**:
```
- FadeBGM() 코루틴 추가:
  - 현재 BGM Fade Out (fadeTime)
  - 새 BGM Fade In (fadeTime)
  - DOTween 사용 (audioSource.DOFade())

- PlayLoopingSFX() 수정:
  - 즉시 재생 대신 FadeBGM() 호출
```

**BGMController.cs**:
```
- fadeTime 필드 활용
- PlayBGM()에서 Fade 요청
```

---

### **1.2 자동 폴백 시스템 구현**

#### **목표**:
- 스테이지 전용 BGM이 없으면 자동으로 공용 BGM 사용
- SceneBGMStarter 설정 통일 (모든 씬에서 Stage Id 입력)

#### **구현 내용**:

**CueRegistry.cs**:
```
- HasKey() 메서드 추가:
  public bool HasKey(string domain, string eventKey)
  {
      // CueProfile에서 키 존재 여부 확인
      return profile.Resolve(eventKey) != null;
  }
```

**BGMController.cs**:
```
- ResolveKey() 메서드 개선:
  1. 스테이지 전용 키 생성: bgm.stage.STAGE_001.default
  2. CueRegistry.HasKey() 체크
  3. 있으면 → 스테이지 전용 키 반환
  4. 없으면 → 공용 키 반환 (폴백)
```

#### **테스트**:
```
Stage_001 (전용 BGM 없음):
- 요청: bgm.stage.default + Stage Id: STAGE_001
- 변환: bgm.stage.STAGE_001.default
- 체크: CueProfile에 키 없음
- 폴백: bgm.stage.default 사용 ✅

Stage_003 (전용 BGM 있음):
- 요청: bgm.stage.default + Stage Id: STAGE_003
- 변환: bgm.stage.STAGE_003.default
- 체크: CueProfile에 키 있음 ✅
- 사용: bgm.stage.STAGE_003.default ✅
```

---

### **1.3 전투/보스 BGM 자동 전환**

#### **목표**:
- 전투 시작 → Battle BGM
- 전투 종료 → Default BGM 복귀
- 보스 등장 → Boss BGM (최우선)
- 보스 처치 → Battle 또는 Default 복귀

#### **구현 내용**:

**StageManager.cs**:
```
- 전투 시작 시:
  BGMController.Instance.OnBattleStart(stageId);

- 전투 종료 시:
  BGMController.Instance.OnBattleEnd();

- 보스 등장 시:
  BGMController.Instance.OnBossStart(stageId);

- 보스 처치 시:
  BGMController.Instance.OnBossEnd();
```

**BGMController.cs**:
```
- OnBattleStart/End 메서드는 이미 구현됨 ✅
- StageManager와 연동만 하면 됨
```

#### **우선순위 스택 작동 예시**:
```
1. 기본 상태:
   activeStates = { Default: "bgm.stage.default" }
   → 탐험 BGM 재생

2. 전투 시작:
   activeStates = { Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
   → 전투 BGM 재생 (Battle > Default)

3. 보스 등장:
   activeStates = { Default: "bgm.stage.default", Battle: "bgm.stage.battle", Boss: "bgm.stage.boss" }
   → 보스 BGM 재생 (Boss > Battle > Default)

4. 보스 처치:
   activeStates = { Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
   → 전투 BGM 복귀 ✅

5. 전투 종료:
   activeStates = { Default: "bgm.stage.default" }
   → 탐험 BGM 복귀 ✅
```

---

### **1.4 컷신 우선순위 BGM**

#### **목표**:
- 컷신 시작 → 컷신 전용 BGM (선택적)
- 컷신 종료 → 이전 BGM 복귀

#### **구현 내용**:

**CutsceneData.cs**:
```
- bgmEventKey 필드 추가 (선택적):
  [Header("=== BGM 설정 (선택사항) ===")]
  public string bgmEventKey = "";
  public bool resumePreviousBGM = true;
```

**CutsceneManager.cs**:
```
- PlayCutscene() 시작 시:
  if (!string.IsNullOrEmpty(data.bgmEventKey))
  {
      BGMController.Instance.AddState(BGMPriority.Cutscene, data.bgmEventKey);
  }

- OnCutsceneEnded() 시:
  if (data.resumePreviousBGM)
  {
      BGMController.Instance.RemoveState(BGMPriority.Cutscene);
  }
```

---

## 🎬 2. 컷신 시스템 개선

### **2.1 카메라 애니메이션 Step**

#### **목표**:
- 카메라 줌 인/아웃
- 카메라 이동
- 카메라 쉐이크

#### **구현 내용**:

**CutsceneStep.cs**:
```
- CameraStep 추가:
  public enum CameraAction { ZoomIn, ZoomOut, MoveTo, Shake }
  public CameraAction cameraAction;
  public float cameraZoom = 1.0f;
  public Vector3 cameraTarget;
  public float cameraDuration = 1.0f;
```

**CutsceneStepExecutor.cs**:
```
- ExecuteCameraStep() 메서드 추가:
  - DOTween으로 카메라 애니메이션
  - Cinemachine 지원 (있으면)
```

---

### **2.2 선택지 Step (Branching)**

#### **목표**:
- 플레이어가 선택지 선택
- 선택에 따라 다른 컷신 분기

#### **구현 내용**:

**CutsceneStep.cs**:
```
- ChoiceStep 추가:
  public string[] choices;
  public CutsceneData[] nextCutscenes;
```

**DialoguePanel.cs**:
```
- ShowChoices() 메서드 추가:
  - 선택지 버튼 생성
  - 선택 시 콜백 호출
```

**CutsceneManager.cs**:
```
- 선택 처리 로직:
  - 현재 컷신 일시정지
  - 선택 완료 후 다음 컷신 재생
```

---

### **2.3 변수 시스템**

#### **목표**:
- 플래그 저장/로드 (예: "인트로 본 적 있음")
- 조건부 컷신 재생

#### **구현 내용**:

**CutsceneVariableManager.cs** (신규):
```
- SetFlag(string key, bool value)
- GetFlag(string key)
- SetInt(string key, int value)
- GetInt(string key)
- SaveToDisk() / LoadFromDisk()
```

**AreaTrigger.cs**:
```
- 조건부 재생:
  public string requiredFlag = "";
  public bool requiredFlagValue = true;
  
  if (!string.IsNullOrEmpty(requiredFlag))
  {
      if (CutsceneVariableManager.GetFlag(requiredFlag) != requiredFlagValue)
          return; // 조건 미충족
  }
```

---

### **2.4 컷신 편집기 (Custom Editor)**

#### **목표**:
- Inspector에서 Step 추가/삭제/순서 변경 용이
- Step 미리보기
- 타임라인 시각화

#### **구현 내용**:

**CutsceneDataEditor.cs** (신규):
```
- ReorderableList 사용
- Step 드래그 앤 드롭 순서 변경
- Step 복사/붙여넣기
- Step 미리보기 버튼
- 전체 컷신 duration 표시
```

---

## 🎮 3. AreaTrigger 고도화

### **3.1 조건부 트리거**

#### **목표**:
- 플래그 체크 (예: "퀘스트 완료 시에만 재생")
- 레벨 체크 (예: "레벨 10 이상")

#### **구현 내용**:

**AreaTrigger.cs**:
```
[Header("=== 조건 설정 (선택) ===")]
public bool useCondition = false;
public string requiredFlag = "";
public int requiredLevel = 0;

private bool CheckCondition()
{
    if (!useCondition) return true;
    
    if (!string.IsNullOrEmpty(requiredFlag))
    {
        if (!CutsceneVariableManager.GetFlag(requiredFlag))
            return false;
    }
    
    if (requiredLevel > 0)
    {
        if (PlayerData.Level < requiredLevel)
            return false;
    }
    
    return true;
}
```

---

### **3.2 복수 컷신 지원**

#### **목표**:
- 한 트리거에서 여러 컷신 순차 재생
- 예: 첫 진입 시 인트로, 두 번째 진입 시 힌트

#### **구현 내용**:

**AreaTrigger.cs**:
```
public List<CutsceneData> cutsceneDatas; // 복수 지원
private int currentCutsceneIndex = 0;

private void PlayCutscene()
{
    if (currentCutsceneIndex >= cutsceneDatas.Count)
        return;
    
    var data = cutsceneDatas[currentCutsceneIndex];
    CutsceneManager.Instance.PlayCutscene(data);
    
    currentCutsceneIndex++;
    
    if (!playOnce && currentCutsceneIndex >= cutsceneDatas.Count)
    {
        currentCutsceneIndex = 0; // 루프
    }
}
```

---

### **3.3 Visual Element 애니메이션**

#### **목표**:
- Visual Element가 bounce/pulse 애니메이션
- 플레이어 접근 시 반응

#### **구현 내용**:

**AreaTrigger.cs**:
```
public bool animateVisualElement = true;
public AnimationType animationType = AnimationType.Bounce;

private void Start()
{
    if (animateVisualElement && visualElement != null)
    {
        AnimateVisualElement();
    }
}

private void AnimateVisualElement()
{
    switch (animationType)
    {
        case AnimationType.Bounce:
            // DOTween으로 위아래 움직임
            break;
        case AnimationType.Pulse:
            // DOTween으로 크기 변화
            break;
    }
}
```

---

## ⚡ 4. 성능 최적화

### **4.1 Image Pool 시스템**

#### **목표**:
- 이미지 재사용으로 메모리 절약
- 빈번한 컷신 재생 시 GC 방지

#### **구현 내용**:

**CutsceneImagePanel.cs**:
```
- 이미지 Pool 추가 (배경/초상/Fade 각각)
- ShowImage() 시 Pool에서 가져오기
- HideImage() 시 Pool에 반환
```

---

### **4.2 Canvas 미리 로딩**

#### **목표**:
- 첫 컷신 재생 시 로딩 지연 제거
- 부드러운 컷신 시작

#### **구현 내용**:

**CutsceneCanvasLoader.cs**:
```
- PreloadCanvas() 메서드 추가:
  - 씬 시작 시 Canvas 미리 로드
  - 비활성화 상태로 대기
  - 컷신 재생 시 즉시 활성화
```

---

## 🧪 5. 추가 기능 (선택)

### **5.1 컷신 스킵 설정 확장**

#### **목표**:
- 스킵 불가 구간 설정
- 특정 Step만 스킵 불가

#### **구현 내용**:

**CutsceneStep.cs**:
```
public bool canSkipThisStep = true;
```

**CutsceneManager.cs**:
```
- HandleSkipInput() 수정:
  - 현재 Step의 canSkipThisStep 체크
  - false면 스킵 무시
```

---

### **5.2 컷신 이력 저장**

#### **목표**:
- 본 컷신 기록
- 컷신 갤러리 (재감상 기능)

#### **구현 내용**:

**CutsceneHistoryManager.cs** (신규):
```
- Dictionary<string, CutsceneHistory> histories
- AddHistory(cutsceneId, timestamp)
- GetHistory(cutsceneId)
- SaveToDisk() / LoadFromDisk()
```

---

## 📋 우선순위

### **High Priority** (반드시 구현):
1. **BGM Fade In/Out** (사용자 경험 핵심)
2. **자동 폴백 시스템** (50개 스테이지 관리용)
3. **전투/보스 BGM 자동 전환** (게임플레이 필수)

### **Medium Priority** (선택적 구현):
4. 컷신 우선순위 BGM
5. 조건부 AreaTrigger
6. Visual Element 애니메이션

### **Low Priority** (나중에 고려):
7. 선택지 Step (Branching)
8. 카메라 애니메이션 Step
9. 컷신 편집기
10. 성능 최적화

---

## 🚀 시작하기

### **New Agent 세션에서 전달할 내용**:

```
안녕하세요!

컷신 시스템 MVP 버전이 완료되었습니다.
확장 버전을 구현하고 싶습니다.

📁 관련 파일:
- MVP_COMPLETE.md (완료 내역)
- EXTENSION_TODO.md (이 파일)

🎯 우선 구현할 기능:
1. BGM Fade In/Out
2. 자동 폴백 시스템 (스테이지 전용 → 공용)
3. 전투/보스 BGM 자동 전환

순서대로 구현해주세요.
```

---

## 💡 참고 사항

### **BGM 키 구조 (최종)**:

```
=== 고정 BGM ===
bgm.login
bgm.intro
bgm.lobby
bgm.victory
bgm.gameover

=== 공용 스테이지 BGM ===
bgm.stage.default (탐험)
bgm.stage.battle (전투)
bgm.stage.boss (보스)
bgm.stage.clear (클리어)

=== 스테이지 전용 BGM (폴백 사용) ===
bgm.stage.STAGE_001.default
bgm.stage.STAGE_001.battle
bgm.stage.STAGE_001.boss
bgm.stage.STAGE_002.default
...
bgm.stage.STAGE_050.default
```

### **폴백 체인**:
```
요청: bgm.stage.STAGE_003.boss + Stage Id: STAGE_003
↓
1순위: bgm.stage.STAGE_003.boss (스테이지 전용 보스)
2순위: bgm.stage.boss (공용 보스 BGM)
3순위: bgm.stage.default (최후 폴백)
```

---

## 📊 예상 작업 시간

| 기능 | 예상 시간 | 우선순위 |
|------|---------|---------|
| BGM Fade In/Out | 30분 | High |
| 자동 폴백 시스템 | 40분 | High |
| 전투/보스 BGM 연동 | 20분 | High |
| 컷신 우선순위 BGM | 30분 | Medium |
| 조건부 AreaTrigger | 40분 | Medium |
| 선택지 Step | 2시간 | Low |
| 카메라 Step | 1시간 | Low |
| 컷신 편집기 | 3시간 | Low |

**High Priority 전체**: 약 1.5시간

---

**확장 버전 구현을 시작하시려면, New Agent에게 이 문서를 참조하도록 해주세요!** 🚀
