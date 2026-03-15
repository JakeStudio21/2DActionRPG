# 글로벌 터치 이펙트 구현 계획

## 1. 개요

### 1.1 목표
- **1단계**: Loading, Lobby, Intro, Tutorial, Ingame_Loading 씬에서 화면 터치 시 이펙트 재생
- **2단계**: 인게임 씬 적용 (V-Pad, 스킬 버튼 예외 처리 별도)

### 1.2 적용 씬 (1단계)
| 씬명 | 경로 | 비고 |
|------|------|------|
| Loading | `Assets/Scenes/Loading.unity` | Tap to Start, 로그인 플로우 포함 |
| Lobby | `Assets/Scenes/Lobby.unity` | 메인 로비 |
| Intro | `Assets/Scenes/Intro.unity` | 인트로 컷신 |
| Tutorial | `Assets/Scenes/Tutorial.unity` | 튜토리얼 |
| Ingame_Loading | `Assets/Scenes/Ingame_Loading.unity` | 인게임 로딩 |

### 1.3 핵심 요구사항 체크리스트
- [x] 글로벌 매니저: DontDestroyOnLoad + 싱글톤/정적 접근
- [x] 최상단 UI 렌더링: Sort Order 100+ 전용 Canvas
- [x] 오브젝트 풀링: Queue/List 기반, Awake/Start 시 선할당
- [x] 입력: 마우스 + 모바일 터치, Screen → Canvas 좌표 변환
- [x] 이펙트 자동 회수: 재생 완료 후 풀 반납
- [x] 2단계 대비: `canSpawnEffect` 플래그

---

## 2. 아키텍처

### 2.1 컴포넌트 구조

```
[GlobalTouchManager] (MonoBehaviour, DontDestroyOnLoad)
├── 싱글톤 인스턴스
├── 전용 Canvas (Sort Order 100+)
├── 이펙트 풀 (Queue<GameObject>)
├── 터치 감지 및 스폰 로직
└── canSpawnEffect 플래그 (2단계용)

[TouchEffect] (MonoBehaviour, 이펙트 프리팹에 부착)
├── 재생 시간/애니메이션 감지
├── 풀 반납 콜백
└── GlobalTouchManager 참조
```

### 2.2 데이터 흐름

```
Input (마우스/터치)
    → GlobalTouchManager.Update()
    → canSpawnEffect 체크
    → Screen 좌표 → Canvas Local 좌표 변환
    → 풀에서 이펙트 가져오기 (없으면 스킵 또는 확장)
    → TouchEffect.Play(position) 호출
    → 재생 완료 (타이머/애니메이션)
    → TouchEffect.ReturnToPool()
    → 풀에 반납 (SetActive(false))
```

---

## 3. 스크립트 명세

### 3.1 GlobalTouchManager.cs

#### 3.1.1 기본 정보
| 항목 | 내용 |
|------|------|
| 경로 | `Assets/Scripts/UI/TouchEffect/GlobalTouchManager.cs` |
| 네임스페이스 | (선택) `TouchEffect` 또는 전역 |
| 상속 | `MonoBehaviour` |

#### 3.1.2 싱글톤 패턴
- **패턴**: Lazy Singleton (FindObjectOfType 또는 Instance getter에서 생성)
- **Awake**: 중복 인스턴스 시 `Destroy(gameObject)` 후 return
- **DontDestroyOnLoad(gameObject)** 호출
- **정적 접근**: `GlobalTouchManager.Instance`

#### 3.1.3 Inspector 필드 (SerializedField)

| 필드명 | 타입 | 기본값 | 설명 |
|--------|------|--------|------|
| effectPrefab | GameObject | (할당) | 터치 이펙트 프리팹 (TouchEffect 컴포넌트 필수) |
| poolSize | int | 10 | 초기 풀 크기 |
| touchCanvas | Canvas | (자동/할당) | 전용 Canvas. null이면 코드로 생성 |
| canvasSortOrder | int | 100 | Canvas.sortingOrder (다른 UI보다 위) |
| effectDuration | float | 0.5f | 이펙트 재생 시간 (TouchEffect에 전달용, fallback) |
| canSpawnEffect | bool | true | 이펙트 스폰 허용 (2단계: 인게임에서 false로 제어) |
| enableDebugLogs | bool | false | 디버그 로그 출력 |

#### 3.1.4 내부 필드 (Private)

| 필드명 | 타입 | 설명 |
|--------|------|------|
| _instance | static GlobalTouchManager | 싱글톤 인스턴스 |
| _effectPool | Queue\<GameObject\> | 재사용 이펙트 풀 |
| _canvasRect | RectTransform | 터치 Canvas의 RectTransform (좌표 변환용) |
| _eventCamera | Camera | Canvas가 Screen Space - Camera일 때 사용. Overlay면 null |

#### 3.1.5 메서드 명세

| 메서드 | 접근 | 반환 | 설명 |
|--------|------|------|------|
| Awake | private | void | 싱글톤 설정, DontDestroyOnLoad, Canvas 초기화, 풀 생성 |
| Start | private | void | (선택) 추가 초기화 |
| Update | private | void | 터치/마우스 입력 감지 → canSpawnEffect 체크 → SpawnEffect 호출 |
| EnsureCanvas | private | void | touchCanvas가 null이면 전용 Canvas GameObject 생성 및 설정 |
| InitializePool | private | void | poolSize만큼 effectPrefab 인스턴스 생성, 비활성화, Queue에 추가 |
| SpawnEffect | private | void | 풀에서 꺼내 위치 설정, Play 호출. 풀 비면 로그 후 스킵 |
| GetTouchScreenPosition | private | Vector2? | Input.mousePosition 또는 Input.GetTouch(0).position 반환. 입력 없으면 null |
| ScreenToCanvasLocal | private | Vector2 | Screen 좌표 → Canvas RectTransform 로컬 좌표 변환 (RectTransformUtility.ScreenPointToLocalPointInRectangle) |
| ReturnEffectToPool | public | void | TouchEffect에서 호출. GameObject 비활성화 후 Queue에 다시 추가 |
| SetCanSpawnEffect | public | void | canSpawnEffect 설정 (2단계: 인게임에서 false 호출) |

#### 3.1.6 좌표 변환 상세
- **Canvas Render Mode**: Screen Space - Overlay (추천) 또는 Screen Space - Camera
- **RectTransformUtility.ScreenPointToLocalPointInRectangle** 사용
- 파라미터: (canvasRect, screenPos, eventCamera, out localPoint)
- Overlay일 때 eventCamera는 null 전달 가능 (Unity 문서 확인)

#### 3.1.7 입력 처리 상세
```
매 프레임 Update에서:
1. if (!canSpawnEffect) return;
2. 마우스: Input.GetMouseButtonDown(0) → Input.mousePosition
3. 터치: Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began
4. 각 터치마다 SpawnEffect 호출 (멀티터치 지원 시)
```

---

### 3.2 TouchEffect.cs

#### 3.2.1 기본 정보
| 항목 | 내용 |
|------|------|
| 경로 | `Assets/Scripts/UI/TouchEffect/TouchEffect.cs` |
| 부착 대상 | 이펙트 프리팹 (2D 스프라이트 애니메이션 또는 파티클) |

#### 3.2.2 Inspector 필드

| 필드명 | 타입 | 기본값 | 설명 |
|--------|------|--------|------|
| duration | float | 0.5f | 재생 시간(초). 이 시간 후 풀 반납 |
| useAnimationLength | bool | false | true면 Animator/Animation 길이로 자동 판단 |
| manager | GlobalTouchManager | (자동) | 풀 반납 시 호출할 매니저. null이면 FindObjectOfType |

#### 3.2.3 메서드 명세

| 메서드 | 접근 | 반환 | 설명 |
|--------|------|------|------|
| Play | public | void | (Vector2 localPosition) 위치 설정, 활성화, 재생 시작, 코루틴/Invoke로 타이머 시작 |
| ReturnToPool | private | void | manager.ReturnEffectToPool(gameObject) 호출 |
| OnPlayStarted | private | void | Play 호출 시. Animator/Animation 있으면 Play, 파티클 있으면 Play |
| StartReturnTimer | private | void | duration 후 또는 애니메이션 완료 시 ReturnToPool 호출 |

#### 3.2.4 재생 완료 판단 로직
- **옵션 A**: `Invoke(nameof(ReturnToPool), duration)` 또는 `Coroutine` + `WaitForSeconds(duration)`
- **옵션 B**: Animator가 있으면 `AnimatorStateInfo.length` 사용
- **옵션 C**: AnimationClip이 있으면 `clip.length` 사용
- **우선순위**: useAnimationLength && (Animator|Animation) 있음 → 애니메이션 길이, else → duration

---

## 4. 하이라키 셋업 가이드

### 4.1 글로벌 매니저 배치 (권장: 프리팹 + 씬 로드 시 생성)

#### 방법 A: 첫 씬에 배치 (Loading 또는 Intro)
1. 빈 GameObject 생성 → 이름: `GlobalTouchManager`
2. `GlobalTouchManager` 스크립트 추가
3. **effectPrefab** 필드에 터치 이펙트 프리팹 할당
4. **poolSize**: 10
5. **touchCanvas**: 비워둠 (코드에서 자동 생성) 또는 미리 만들어서 할당
6. **canvasSortOrder**: 100 (또는 32767 등 최상위)
7. 이 GameObject는 DontDestroyOnLoad로 씬 전환 시 유지됨

#### 방법 B: Resources에서 프리팹으로 로드
1. `Resources/Prefabs/UI/GlobalTouchManager.prefab` 생성
2. 위와 동일 설정
3. Loading(또는 Boot) 씬에서 `Instantiate(Resources.Load<GameObject>("Prefabs/UI/GlobalTouchManager"))` 로 생성
4. 또는 씬에 배치 후 DontDestroyOnLoad

### 4.2 전용 Canvas 구조 (코드 자동 생성 시)

```
GlobalTouchManager (GameObject)
└── TouchEffectCanvas (자동 생성)
    ├── Canvas (Render Mode: Screen Space - Overlay, Sort Order: 100)
    ├── CanvasScaler (UI Scale Mode: Scale With Screen Size, 1920x1080)
    ├── GraphicRaycaster (Raycast Target: false 권장 - 터치 이벤트 차단 방지)
    └── (이펙트 인스턴스들이 여기 자식으로 생성됨)
```

### 4.3 수동 Canvas 설정 (Inspector 할당 시)

1. Canvas GameObject 생성
2. **Render Mode**: Screen Space - Overlay
3. **Sorting Order**: 100 이상 (로딩 일러스트, 로비 팝업 등보다 큼)
4. CanvasScaler: Reference Resolution 1920x1080, Match 0.5
5. GraphicRaycaster: **Blocking Objects = None** (선택) - 터치가 아래 UI로 전달되도록
6. GlobalTouchManager의 **touchCanvas** 필드에 이 Canvas 할당

### 4.4 이펙트 프리팹 구조

```
TouchEffect_Webtoon (프리팹)
├── TouchEffect (스크립트)
├── RectTransform (Anchor: stretch, Pivot: center)
├── Image 또는 SpriteRenderer (2D 웹툰풍 스프라이트)
├── Animator (선택) - 스프라이트 애니메이션
└── CanvasGroup (선택) - 알파 페이드
```

- **초기 상태**: 비활성화 (프리팹 루트 또는 최상위)
- **크기**: 100x100 ~ 200x200 픽셀 (Inspector에서 조정)

### 4.5 씬별 사전 작업

| 씬 | 작업 |
|------|------|
| Loading | GlobalTouchManager가 DontDestroyOnLoad로 유지되므로, **Loading이 첫 씬**이면 여기에 배치. 또는 Boot 씬에서 생성 |
| Lobby | 별도 작업 없음 (매니저가 이미 존재) |
| Intro | 별도 작업 없음 |
| Tutorial | 별도 작업 없음 |
| Ingame_Loading | 별도 작업 없음 |

**중요**: GlobalTouchManager는 **한 씬에만** 배치. DontDestroyOnLoad로 이후 씬에서도 유지됨.

### 4.6 씬 로드 순서 확인

- **첫 진입 씬**: Loading 또는 Intro
- GlobalTouchManager를 **가장 먼저 로드되는 씬**에 배치
- Build Settings의 Scenes In Build에서 순서 확인

---

## 5. 풀링 로직 상세

### 5.1 초기화 (Awake/Start)

```
1. EnsureCanvas() → Canvas 준비
2. _effectPool = new Queue<GameObject>()
3. for (i = 0; i < poolSize; i++)
   - go = Instantiate(effectPrefab, canvasRect)
   - go.SetActive(false)
   - _effectPool.Enqueue(go)
```

### 5.2 스폰 시

```
1. if (_effectPool.Count == 0) → 로그 후 return (또는 풀 확장)
2. go = _effectPool.Dequeue()
3. go.transform.localPosition = localPos (Vector3)
4. go.SetActive(true)
5. go.GetComponent<TouchEffect>().Play(localPos)
```

### 5.3 반납 시 (TouchEffect.ReturnToPool → Manager.ReturnEffectToPool)

```
1. go.SetActive(false)
2. _effectPool.Enqueue(go)
```

### 5.4 풀 확장 (선택)

- 풀 비었을 때 `Instantiate` 1~5개 추가 후 Enqueue
- 또는 `poolExpandSize` 필드로 확장 개수 지정

---

## 6. 2단계 대비 (인게임)

### 6.1 canSpawnEffect 활용

- 인게임 씬 진입 시: `GlobalTouchManager.Instance.SetCanSpawnEffect(false)`
- 인게임 씬 퇴장 시: `GlobalTouchManager.Instance.SetCanSpawnEffect(true)`
- 또는 **씬 이름 기반 자동 제어**: 인게임 씬 목록에 있으면 false

### 6.2 예외 영역 (2단계)

- V-Pad, 스킬 버튼 영역 터치 시 이펙트 생략
- **구현 방식**: 터치 위치가 특정 RectTransform 내부인지 체크
- `RectTransformUtility.RectangleContainsScreenPoint` 사용

---

## 7. 파일 생성 체크리스트

| 순서 | 파일 | 설명 |
|------|------|------|
| 1 | `Assets/Scripts/UI/TouchEffect/GlobalTouchManager.cs` | 글로벌 매니저 |
| 2 | `Assets/Scripts/UI/TouchEffect/TouchEffect.cs` | 이펙트 제어 |
| 3 | `Assets/Resources/Prefabs/UI/TouchEffect_Webtoon.prefab` | 이펙트 프리팹 (또는 기존 에셋 활용) |
| 4 | `Assets/Resources/Prefabs/UI/GlobalTouchManager.prefab` | (선택) 매니저 프리팹 |

---

## 8. 테스트 시나리오

1. **Loading 씬** 진입 → Tap to Start 전 화면 터치 → 이펙트 표시
2. **Lobby** 진입 → 아무 곳이나 터치 → 이펙트 표시, 다른 UI 위에 렌더링
3. **Intro** 재생 중 터치 → 이펙트 표시
4. **Tutorial** 진행 중 터치 → 이펙트 표시
5. **Ingame_Loading** → 이펙트 표시
6. **인게임** (2단계) → canSpawnEffect = false 시 이펙트 미표시

---

## 9. 참고: 기존 프로젝트 연동

- **GamePoolManager**: 별도 풀 시스템. GlobalTouchManager는 자체 풀 사용 (경량, UI 전용)
- **Cue System**: 터치 이펙트는 Cue 미사용 (직접 풀 관리)
- **SceneTransitionManager**: 동일한 DontDestroyOnLoad 패턴 참고
