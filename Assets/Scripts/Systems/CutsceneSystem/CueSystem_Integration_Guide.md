# CueSystem 연동 가이드

## 개요

컷신 시스템은 CueSystem을 통해 효과음을 재생합니다. SFX Step에서 `CueEmitter.Emit(eventKey, domain)`을 호출하여 효과음을 재생합니다.

## 기본 설정

### 1. 컷신용 CueProfile 생성

1. Unity 에디터에서 `Assets` → `Create` → `CueSystem` → `Cue Profile`
2. 프로필 이름: `Cutscene_CutsceneProfile` (또는 원하는 이름)
3. Inspector에서 설정:
   - **Profile ID**: `cutscene_profile` (또는 원하는 ID)
   - **Domain**: `Cutscene` (중요!)
   - **Description**: 컷신용 효과음 프로필

### 2. SFX 카탈로그 설정

CueProfile의 `SFX Catalog`에 효과음 추가:

#### 기본 이벤트 키 4개 (MVP 권장)

1. **bgm_intro**
   - 컷신 시작 BGM
   - AudioClip: 인트로 BGM 클립 할당

2. **image_appear**
   - 이미지 등장 효과음
   - AudioClip: 이미지 등장 시 재생할 효과음

3. **dialogue_type**
   - 대사 타이핑 효과음
   - AudioClip: 타이핑 시 재생할 효과음 (선택사항)

4. **scene_end**
   - 컷신 종료 효과음
   - AudioClip: 컷신 종료 시 재생할 효과음

#### SFX Cue 설정 예시

```
SFX ID: bgm_intro
AudioClip: [BGM_Intro 클립 드래그]
Volume: 1.0
Pitch: 1.0
Is 3D: false
```

### 3. 이벤트 매핑 설정

CueProfile의 `Entries` 리스트에 이벤트 키 매핑 추가:

#### Entry 추가 방법

1. `Entries` 리스트에서 `+` 버튼 클릭
2. **Event Key**: `bgm_intro` (또는 다른 키)
3. **SFX IDs**: `bgm_intro` (SFX Catalog의 ID와 일치)
4. **VFX IDs**: (필요시)

#### 예시 Entry 설정

```
Entry 1:
- Event Key: bgm_intro
- SFX IDs: bgm_intro

Entry 2:
- Event Key: image_appear
- SFX IDs: image_appear

Entry 3:
- Event Key: dialogue_type
- SFX IDs: dialogue_type

Entry 4:
- Event Key: scene_end
- SFX IDs: scene_end
```

### 4. CueRegistry에 프로필 등록

1. 씬에서 `CueRegistry` GameObject 찾기 (또는 생성)
2. Inspector에서 `CueRegistry` 컴포넌트 확인
3. **Cutscene Profile** 필드에 생성한 CueProfile 할당
   - 현재 CueRegistry에는 Cutscene 필드가 없을 수 있음
   - 이 경우 CueRegistry 스크립트 수정 필요 (선택사항)
   - 또는 런타임에 `RegisterProfile`로 등록

#### 런타임 등록 (선택사항)

컷신 시작 시 자동 등록하려면 `CutsceneManager`에 다음 코드 추가:

```csharp
// CutsceneManager.Awake() 또는 Start()에 추가
if (CueRegistry.Instance != null)
{
    // 컷신용 CueProfile 로드
    CueProfile cutsceneProfile = Resources.Load<CueProfile>("CueProfiles/Cutscene_CutsceneProfile");
    if (cutsceneProfile != null)
    {
        CueRegistry.Instance.RegisterProfile("Cutscene", cutsceneProfile);
    }
}
```

## 사용 방법

### CutsceneData에서 SFX Step 설정

1. CutsceneData의 Step 목록에서 SFX Step 추가
2. Inspector에서 설정:
   - **Step Type**: `SFX`
   - **SFX Event Key**: `bgm_intro` (CueProfile의 Event Key와 일치)
   - **SFX Domain**: `Cutscene` (기본값)

### 예시 컷신 데이터

```
Step 1: Image
- 배경 이미지 표시

Step 2: SFX
- Event Key: bgm_intro
- Domain: Cutscene

Step 3: Dialogue
- 대사 표시

Step 4: SFX
- Event Key: dialogue_type
- Domain: Cutscene

Step 5: Wait
- 2초 대기

Step 6: SFX
- Event Key: scene_end
- Domain: Cutscene
```

## 테스트

1. CueProfile 생성 및 설정 완료
2. CueRegistry에 프로필 등록
3. CutsceneData에 SFX Step 추가
4. 컷신 재생 시 효과음 재생 확인

## 문제 해결

### 효과음이 재생되지 않는 경우

1. **CueProfile이 CueRegistry에 등록되었는지 확인**
   - CueRegistry Inspector에서 확인
   - 또는 런타임 등록 코드 확인

2. **Event Key가 일치하는지 확인**
   - CutsceneData의 SFX Event Key
   - CueProfile의 Entry Event Key
   - 두 값이 정확히 일치해야 함

3. **SFX Catalog에 AudioClip이 할당되었는지 확인**
   - SFX ID와 Entry의 SFX IDs가 일치하는지 확인
   - AudioClip이 null이 아닌지 확인

4. **CueSystem 초기화 확인**
   - CuePlayer, CueRegistry가 씬에 있는지 확인
   - SoundManager가 초기화되었는지 확인

5. **Console 로그 확인**
   - CueEmitter.Emit 호출 시 로그 확인
   - CuePlayer 재생 로그 확인

## 확장

### 추가 이벤트 키

필요에 따라 더 많은 이벤트 키를 추가할 수 있습니다:

- `bgm_loop`: 루프 BGM
- `image_fade`: 이미지 페이드 효과음
- `dialogue_skip`: 대사 스킵 효과음
- `choice_select`: 선택지 선택 효과음

각 이벤트 키는 CueProfile의 Entry에 추가하고, CutsceneData의 SFX Step에서 사용합니다.
