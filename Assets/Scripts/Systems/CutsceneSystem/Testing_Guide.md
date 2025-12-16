# 컷신 시스템 테스트 가이드

## Phase 7: 더미 컷신 및 통합 테스트

### 목표
MVP 버전의 모든 기능이 정상 작동하는지 확인하는 더미 컷신을 만들고 테스트합니다.

---

## 1. 더미 컷신 데이터 생성

### 1.1 CutsceneData 생성

1. Unity 에디터에서 `Assets` → `Create` → `CutsceneSystem` → `Cutscene Data`
2. 파일명: `Intro_001` (또는 원하는 이름)
3. Inspector에서 설정:

#### 기본 정보
- **Cutscene ID**: `Intro_001`
- **Cutscene Name**: `테스트 인트로 컷신`
- **Cutscene Type**: `Intro`

#### 컷신 Step 목록

다음 순서로 Step을 추가합니다:

**Step 1: Image (배경)**
- Step Type: `Image`
- Image Sprite: (배경 이미지 할당)
- Is Portrait: `false`
- Fade In: `true`
- Image Duration: `2f`
- Image Position: `(0, 0)`
- Image Scale: `1f`

**Step 2: SFX (BGM)**
- Step Type: `SFX`
- SFX Event Key: `bgm_intro`
- SFX Domain: `Cutscene`
- Duration: `0.5f` (효과음 재생 후 대기 시간)

**Step 3: Image (초상)**
- Step Type: `Image`
- Image Sprite: (초상 이미지 할당)
- Is Portrait: `true`
- Fade In: `true`
- Image Duration: `3f`
- Image Position: `(-400, 0)` (왼쪽 중앙)
- Image Scale: `1f`

**Step 4: Dialogue (대사 1)**
- Step Type: `Dialogue`
- Speaker Name: `나레이션`
- Dialogue Text: `이것은 테스트 컷신입니다.`
- Typing Speed: `20` (글자/초)

**Step 5: Wait (대기)**
- Step Type: `Wait`
- Duration: `1f`

**Step 6: Dialogue (대사 2)**
- Step Type: `Dialogue`
- Speaker Name: `나레이션`
- Dialogue Text: `모든 기능이 정상적으로 작동하는지 확인합니다.`
- Typing Speed: `20`

**Step 7: SFX (효과음)**
- Step Type: `SFX`
- SFX Event Key: `image_appear`
- SFX Domain: `Cutscene`
- Duration: `0.5f` (효과음 재생 후 대기 시간)

**Step 8: Wait (대기)**
- Step Type: `Wait`
- Duration: `1f`

**Step 9: Image (Fade to Black)**
- Step Type: `Image`
- Image Sprite: (검은색 **불투명** 이미지)
- **Is Fade: `true`** (Fade 레이어 사용 - 배경/초상 위에 표시)
- Is Portrait: `false` (isFade가 true면 무시됨)
- Fade In: `true` (검은색이 서서히 나타남)
- Image Duration: `2f` (최소 2초 이상 권장)

**Step 10: SFX (종료 효과음)**
- Step Type: `SFX`
- SFX Event Key: `scene_end`
- SFX Domain: `Cutscene`
- Duration: `0.5f` (효과음 재생 후 대기 시간)

#### 설정
- Pause Game On Start: `true`
- Can Skip: `true`
- Play Once: `false` (테스트용)

### 1.2 리소스 폴더에 저장

1. `Assets/Resources/CutsceneData/` 폴더 생성 (없으면)
2. 생성한 CutsceneData를 해당 폴더에 저장
3. 파일명: `Intro_001.asset`

---

## 2. 통합 테스트

### 2.1 씬 설정

테스트용 씬을 만들거나 기존 씬을 사용합니다.

#### 필수 GameObject

1. **CutsceneManager**
   - 빈 GameObject 생성
   - `CutsceneManager` 컴포넌트 추가
   - Inspector에서 설정 확인

2. **CueRegistry** (이미 있다면)
   - CueRegistry GameObject 확인
   - Cutscene 도메인 프로필 등록 확인

3. **CuePlayer** (이미 있다면)
   - CuePlayer GameObject 확인

4. **SoundManager** (이미 있다면)
   - SoundManager GameObject 확인

### 2.2 SceneStartTrigger 테스트

1. 씬에 빈 GameObject 생성
2. `SceneStartTrigger` 컴포넌트 추가
3. Inspector에서 설정:
   - Cutscene ID: `Intro_001`
   - Delay: `0.5f`
   - Play Once: `true`
4. Play 모드 실행
5. 씬 로드 후 0.5초 뒤 컷신이 자동 재생되는지 확인

### 2.3 AreaTrigger 테스트

1. 씬에 빈 GameObject 생성
2. `BoxCollider2D` 컴포넌트 추가
   - Is Trigger: `true`
   - Size: 적절한 크기 (예: 5x5)
3. `AreaTrigger` 컴포넌트 추가
4. Inspector에서 설정:
   ```
   컷신 설정:
   - Cutscene ID: "Intro_001"
   - Cutscene Data: (선택사항)
   - Play Once: true
   - Is Active: true
   
   플레이어 인식:
   - Player Tag: "Player" (기본값)
   
   타이밍 설정:
   - Delay: 0f (즉시 재생) 또는 원하는 지연 시간 (예: 0.5초)
   
   시각적 요소:
   - Visual Element: (선택사항 - Sprite나 Effect GameObject)
   - Hide Visual On Complete: true (컷신 후 자동 숨김)
   ```
5. Play 모드 실행
6. 플레이어가 트리거 영역에 진입하면 컷신이 재생되는지 확인
7. **새 기능 테스트**:
   - Delay 설정 시 지연 후 재생되는지 확인
   - Play Once = true 시 두 번째 진입에서 재생 안 되는지 확인
   - Visual Element 설정 시 컷신 후 자동으로 숨겨지는지 확인

### 2.4 CodeTrigger 테스트

코드에서 직접 호출:

```csharp
// 어디서든 호출 가능
CutsceneManager.Instance.PlayCutscene("Intro_001");
```

또는:

```csharp
CutsceneData data = Resources.Load<CutsceneData>("CutsceneData/Intro_001");
CutsceneManager.Instance.PlayCutscene(data);
```

### 2.5 BGM 시스템 테스트

#### **Step 1: BGM CueProfile 생성**

1. `Assets/Resources/CueProfiles` 폴더에 우클릭
2. Create → CueSystem → Cue Profile
3. 파일명: `BGM_bgm_base`
4. Inspector에서 설정:
   ```
   Profile ID: "bgm_base"
   
   📦 Sfx Catalog (먼저 AudioClip 등록):
     [0] SFX Cue:
         Sfx Id: "bgm_lobby_001"
         Audio Clip: [로비 BGM AudioClip]
         Volume: 0.5
         Pitch: 1.0
         Loop: ✅ true (BGM은 필수!)
         Note: "로비 BGM"
     
     [1] SFX Cue:
         Sfx Id: "bgm_stage_default_001"
         Audio Clip: [스테이지 기본 BGM]
         Loop: ✅ true
     
     [2] SFX Cue:
         Sfx Id: "bgm_stage_battle_001"
         Audio Clip: [전투 BGM]
         Loop: ✅ true
   
   🔑 Entries (이벤트 키 매핑):
     [0] Cue Entry:
         Event Key: "bgm.lobby"
         Sfx Ids: [0] "bgm_lobby_001"
     
     [1] Cue Entry:
         Event Key: "bgm.stage.default"
         Sfx Ids: [0] "bgm_stage_default_001"
     
     [2] Cue Entry:
         Event Key: "bgm.stage.battle"
         Sfx Ids: [0] "bgm_stage_battle_001"
   ```

#### **Step 2: CueRegistry에 BGM Profile 등록**

1. Lobby 씬에서 `CueRegistry` GameObject 선택
2. Inspector에서 `Bgm Profile` 필드에 `BGM_bgm_base` 할당

#### **Step 3: BGMController 배치**

1. Lobby 씬에 빈 GameObject 생성 → "BGMController"
2. Add Component → `BGMController`
3. Inspector 설정:
   ```
   Bgm Domain: "BGM"
   Fade Time: 1.0
   Enable Debug Logs: ✅
   ```

#### **Step 4: 씬별 BGM 재생**

로비 씬:
1. 빈 GameObject 생성 → "BGMStarter_Lobby"
2. Add Component → `SceneBGMStarter`
3. Inspector 설정:
   ```
   Bgm Event Key: "bgm.lobby"
   Stage Id: (비워둠)
   Delay: 0.5
   Enable Debug Logs: ✅
   ```

인게임 씬:
1. 빈 GameObject 생성 → "BGMStarter_Stage"
2. Add Component → `SceneBGMStarter`
3. Inspector 설정:
   ```
   Bgm Event Key: "bgm.stage.default"
   Stage Id: "STAGE_001" (선택사항)
   Delay: 0.5
   ```

#### **Step 5: 테스트**

1. Play 모드 진입
2. Console 로그 확인:
   ```
   [BGMController] 초기화 완료
   [SceneBGMStarter] BGM 재생 요청: bgm.lobby
   [BGMController] 🎵 BGM 재생: bgm.lobby
   🎵 [CuePlayer] 루프 BGM 시작: bgm_lobby_001 (Volume: 0.5, Clip: YourAudioClipName)
   ```
3. BGM이 재생되는지 확인 ✅

#### **🔧 BGM 트러블슈팅**

##### **문제 1: BGM이 들리지 않음**

**체크리스트**:
```
[ ] 1. BGM_bgm_base.asset → Domain 필드
    → "BGM" 입력되어 있나요? (대문자 주의!)

[ ] 2. BGM_bgm_base.asset → Sfx Catalog → Volume
    → 0.5 이상으로 설정되어 있나요? (0이면 소리 안 남!)

[ ] 3. BGM_bgm_base.asset → Sfx Catalog → Audio Clip
    → AudioClip이 할당되어 있나요?

[ ] 4. Main Camera → Audio Listener
    → 컴포넌트가 있나요?

[ ] 5. Unity Audio 설정
    → Edit → Project Settings → Audio → Global Volume: 1.0

[ ] 6. Play 모드 → Hierarchy → DontDestroyOnLoad
    → Loop_BGM_xxx GameObject가 생성되었나요?
    → Audio Source → Volume이 0이 아닌가요?
    → Audio Source → Clip이 할당되어 있나요?
```

**로그 확인**:
```
정상 로그:
✅ 🎵 [CuePlayer] 루프 BGM 시작: bgm_xxx_001 (Volume: 0.5, Clip: AudioClipName)
✅ 🔍 [CueProfile] bgm_base - 키 'bgm.xxx' 찾기 결과: True

문제 로그:
❌ ⚠️ [CueRegistry] 키를 찾을 수 없습니다: BGM.bgm.xxx
   → Event Key가 잘못됨 (언더스코어 대신 점 사용)
   
❌ 🎵 [CuePlayer] 루프 BGM 시작: bgm_xxx_001 (Volume: 0, Clip: None)
   → Volume이 0이거나 AudioClip 미할당
```

##### **문제 2: 씬 전환 시 BGM 중복 재생**

**증상**:
- Login → Lobby 이동 시 두 BGM이 동시에 재생됨
- DontDestroyOnLoad에 Loop_BGM_xxx가 여러 개 쌓임

**해결됨**: ✅
- CuePlayer가 자동으로 이전 BGM을 정지/제거합니다
- 로그에서 확인:
  ```
  🛑 [CuePlayer] 이전 BGM 정지: Loop_BGM_bgm_Login_001
  🎵 [CuePlayer] 루프 BGM 시작: bgm_Lobby_001
  ```

##### **문제 3: 씬 전환 시 BGM이 변경되지 않음**

**증상**:
- Lobby → Stage 이동 시 Lobby BGM 계속 재생
- Console에 BGM 재생 요청 로그 없음

**디버그 로그 확인**:
```
정상 로그 (씬 전환 시):
[SceneBGMStarter] Start 호출됨 - Key: bgm.stage.default, StageId: '', Delay: 0.5s
🎬 [SceneBGMStarter] PlayBGM() 시작 - Key: bgm.stage.default, StageId: ''
🎵 [SceneBGMStarter] BGM 재생 요청 전송: bgm.stage.default (StageId: '')
   → PlayDefaultBGM('bgm.stage.default') 호출 (StageId 없음)
✅ [SceneBGMStarter] BGM 재생 요청 완료: bgm.stage.default

📥 [BGMController] PlayDefaultBGM() 호출됨 - Key: 'bgm.stage.default', StageId: ''
🔍 [BGMController] ResolveKey() - 입력: 'bgm.stage.default', StageId: ''
   → 공용 키 사용: 'bgm.stage.default'
🔑 [BGMController] 키 해석 완료: 'bgm.stage.default' → 'bgm.stage.default'
📌 [BGMController] AddState() 호출 - Priority: Default, Key: 'bgm.stage.default'
✅ [BGMController] 상태 추가 완료: Default → bgm.stage.default (활성 상태 수: 1)
🔄 [BGMController] UpdateBGM() 호출 - 현재 BGM: 'bgm.lobby', 활성 상태 수: 1
🎯 [BGMController] 최고 우선순위: Default, Key: 'bgm.stage.default'
🎵 [BGMController] BGM 전환: 'bgm.lobby' → 'bgm.stage.default'
🎼 [BGMController] PlayBGM() 호출 - Key: 'bgm.stage.default', Domain: 'BGM'
📤 [BGMController] CueEmitter.Emit() 호출 - Key: 'bgm.stage.default', Domain: 'BGM'
🛑 [CuePlayer] 이전 BGM 정지: Loop_BGM_bgm_lobby_001
🎵 [CuePlayer] 루프 BGM 시작: bgm_stage_default_001

문제 로그 1 (SceneBGMStarter 없음):
(로그 없음) ← SceneBGMStarter가 실행되지 않음!

문제 로그 2 (중복 재생 방지):
⏸️ [BGMController] 이미 재생 중: 'bgm.stage.default' - 재생 스킵
→ 같은 키를 사용하고 있음 (BGM 키 확인 필요)
```

**해결 방법**:
1. 인게임씬에 SceneBGMStarter GameObject 추가
2. Stage Id 필드를 비우기 (공용 키 사용)
3. BGM 키가 다른지 확인 (Lobby: bgm.lobby, Stage: bgm.stage.default)

---

## 3. 테스트 체크리스트

### 기본 기능 테스트

- [ ] **씬 시작 시 컷신 재생**
  - SceneStartTrigger 배치 후 Play 모드 실행
  - 컷신이 자동으로 재생되는지 확인

- [ ] **이미지 표시**
  - 배경 이미지가 페이드 인되는지 확인
  - 초상 이미지가 올바른 위치에 표시되는지 확인

- [ ] **대사 표시**
  - 화자 이름이 표시되는지 확인
  - 타이핑 효과가 작동하는지 확인
  - 대사 텍스트가 올바르게 표시되는지 확인

- [ ] **효과음 재생**
  - SFX Step에서 효과음이 재생되는지 확인
  - CueSystem 연동이 정상인지 확인

- [ ] **스킵 기능**
  - 1차 클릭: 타이핑 즉시 완료되는지 확인
  - 2차 클릭: 다음 Step으로 이동하는지 확인
  - ESC: 컷신 전체 종료되는지 확인

- [ ] **컷신 종료 후 게임 재개**
  - 컷신 종료 후 플레이어 입력이 복구되는지 확인
  - UI 버튼이 다시 활성화되는지 확인
  - TimeScale이 정상으로 복구되는지 확인

- [ ] **컷신 도중 씬 이동**
  - 컷신 재생 중 씬을 전환해도 오류가 없는지 확인
  - Canvas가 정상적으로 정리되는지 확인

### 고급 기능 테스트

- [ ] **여러 컷신 연속 재생**
  - 여러 컷신을 순차적으로 재생해도 문제없는지 확인

- [ ] **트리거 중복 방지**
  - AreaTrigger의 Play Once 기능이 작동하는지 확인
  - SceneStartTrigger의 재생 기록이 유지되는지 확인

- [ ] **AreaTrigger 고급 기능**
  - Delay 설정 후 지연 재생 확인
  - Visual Element 자동 숨김 확인
  - Player Tag 변경 시 정상 인식 확인
  - 수동 트리거 (TriggerManually) 호출 테스트

---

## 4. Fade 효과 사용 방법

### Fade to Black (검은색 페이드 인)

**목적**: 컷신 종료 시 화면을 검은색으로 페이드

**올바른 설정**:
```
Step Type: Image
Image Sprite: 검은색 불투명 이미지 (100% 검은색, 반투명 X)
Is Fade: true (중요! Fade 레이어 사용)
Is Portrait: false (isFade가 true면 무시됨)
Fade In: true (alpha 0 → 1로 페이드 인)
Image Duration: 2f 이상
```

**동작 원리**:
1. 검은색 이미지가 Fade 레이어(최상위)에 표시됨
2. alpha 0(투명)에서 시작
3. `fadeDuration`(기본 0.5초) 동안 alpha 1(불투명)로 페이드 인
4. 배경/초상 이미지는 그대로 유지되면서 검은색이 덮음
5. Fade to Black 완성!

**주의사항**:
- ❌ **반투명 이미지 사용 금지** - 완전히 불투명한 검은색 이미지 사용
- ❌ **Is Fade: false로 설정 금지** - 배경 레이어를 덮어쓰게 됨
- ✅ **Is Fade: true 필수** - Fade 전용 레이어 사용

### Fade from Black (검은색 페이드 아웃)

```
Step Type: Image
Image Sprite: 검은색 불투명 이미지
Is Fade: true
Fade In: false (alpha 1 → 0로 페이드 아웃)
Image Duration: 2f
```

---

## 5. 문제 해결

### 컷신이 재생되지 않는 경우

1. **CutsceneManager 확인**
   - 씬에 CutsceneManager GameObject가 있는지 확인
   - CutsceneManager.Instance가 null이 아닌지 확인

2. **CutsceneData 확인**
   - Resources/CutsceneData/ 폴더에 파일이 있는지 확인
   - Cutscene ID가 정확한지 확인
   - CutsceneData의 유효성 검증 실행 (우클릭 → "유효성 검증")

3. **Canvas 프리팹 확인**
   - Resources/Prefabs/Cutscene/CutsceneCanvas.prefab이 있는지 확인
   - 프리팹 경로가 정확한지 확인

4. **Step 데이터 확인**
   - 각 Step의 필수 필드가 모두 채워져 있는지 확인
   - Image Step: Image Sprite 할당 확인
   - Dialogue Step: Dialogue Text 입력 확인
   - SFX Step: SFX Event Key 입력 확인

### 효과음이 재생되지 않는 경우

1. **CueProfile 확인**
   - CueProfile이 생성되어 있는지 확인
   - CueRegistry에 등록되어 있는지 확인
   - Event Key가 정확히 일치하는지 확인

2. **CueSystem 확인**
   - CuePlayer, CueRegistry, SoundManager가 씬에 있는지 확인
   - Console 로그에서 CueSystem 에러 확인

### 스킵이 작동하지 않는 경우

1. **CutsceneData 설정 확인**
   - Can Skip이 `true`로 설정되어 있는지 확인

2. **입력 처리 확인**
   - CutsceneManager의 Update 메서드가 호출되는지 확인
   - DialoguePanel의 IsTyping 상태 확인

### Fade 이미지가 작동하지 않는 경우

1. **CutsceneCanvas 프리팹 확인**
   - `Resources/Prefabs/Cutscene/CutsceneCanvas.prefab` 열기
   - Hierarchy에서 `CutsceneImagePanel` 찾기
   - 자식 오브젝트로 **"Fade Image"** GameObject가 있는지 확인
   
2. **Fade Image GameObject 생성 (없는 경우)**
   ```
   Hierarchy 구조:
   CutsceneCanvas
   └── CutsceneImagePanel
       ├── Background Image  (기존)
       ├── Portrait Image    (기존)
       └── Fade Image        (추가 필요)  ← 최상위에 배치
   ```
   
   - `CutsceneImagePanel` 우클릭 → UI → Image
   - 이름: `Fade Image`
   - Inspector 설정:
     - Anchor Preset: **Stretch (전체 화면)**
     - Left/Right/Top/Bottom: 0
     - Color: 흰색 (스프라이트로 덮어씌워질 예정)
     - Raycast Target: false
   - **CanvasGroup** 컴포넌트 자동 추가됨 (코드에서 처리)
   - **Hierarchy 순서**: 가장 아래(최상위 렌더링)에 배치

3. **CutsceneImagePanel 스크립트 Inspector 확인**
   - Fade Image 필드에 Fade Image GameObject 할당 확인
   - 자동 할당 안 되면 수동으로 드래그 앤 드롭

### AreaTrigger가 작동하지 않는 경우

1. **Collider2D 확인**
   - BoxCollider2D 또는 CircleCollider2D 컴포넌트가 있는지 확인
   - **Is Trigger: true** 설정 확인 (중요!)
   - Size가 너무 작지 않은지 확인

2. **Player Tag 확인**
   - Player GameObject에 **"Player" Tag**가 설정되어 있는지 확인
   - 또는 AreaTrigger의 Player Tag 필드를 Player의 실제 Tag에 맞게 변경
   - PlayerController 컴포넌트가 있으면 Tag 없이도 인식됨

3. **트리거 상태 확인**
   - Is Active: true 확인
   - Play Once = true인데 이미 재생했다면 재생 안 됨
   - Context Menu → "재생 기록 초기화" 실행

4. **Visual Element 문제**
   - Visual Element가 할당되지 않으면 자동 숨김 기능 무시됨 (정상)
   - Hide Visual On Complete: false로 설정하면 숨기지 않음

---

## 5. MVP 완성 기준

다음 체크리스트를 모두 통과하면 MVP 완성입니다:

✅ **기본 기능**
- [ ] 씬 시작 시 컷신 재생됨
- [ ] 이미지/대사 정상 출력
- [ ] 스킵 동작함
- [ ] 컷신 종료 후 게임 정상 재개
- [ ] 컷신 도중 씬 이동해도 오류 없음

✅ **트리거 기능**
- [ ] SceneStartTrigger 작동
- [ ] AreaTrigger 작동
- [ ] CodeTrigger 작동

✅ **CueSystem 연동**
- [ ] 효과음 재생됨
- [ ] CueProfile 등록 정상

---

## 6. 다음 단계

MVP가 완성되면 확장 버전으로 진행할 수 있습니다:

- Executor 패턴 리팩토링
- 고급 타이핑 효과
- 다중 컷신 큐 시스템
- 선택지 시스템
- 트랜지션 효과
- 에디터 도구
