# 미니맵 듀얼 시스템 설정 가이드

## 아키텍처 개요

```
MinimapManager         ← 데이터 허브 (Singleton). UI 조작 없음.
  ├── MinimapData      ← 씬별 맵 이미지 + 월드 바운드 SO
  ├── List<MinimapMarker>  ← 등록된 마커 목록
  └── Events (OnMarkerRegistered, OnMarkerUnregistered, OnDataChanged)

BaseMapView (abstract) ← 공통 로직 (마커 관리, Edge Clamping, Anti-Popping)
  ├── MinimapView      ← MinimapPanel 부착. 고정형 전체 미니맵.
  └── RadarMapView     ← RadarMapPanel 부착. 스크롤링 레이더맵.

MiniMapUIManager       ← SetActive() 토글 전담. 게임 로직 없음.
```

---

## 파일 구성

```
Assets/
├── Scripts/Minimap/
│   ├── MinimapData.cs          ScriptableObject
│   ├── MinimapMarker.cs        월드 오브젝트 마커 컴포넌트
│   ├── MinimapManager.cs       데이터 관리자 (Singleton)
│   ├── BaseMapView.cs          공통 추상 기반 클래스
│   ├── MinimapView.cs          전체 미니맵 렌더러
│   ├── RadarMapView.cs         레이더맵 렌더러
│   └── MiniMapUIManager.cs     패널 토글 관리자
├── Scripts/Editor/
│   └── MinimapNavMeshExtractor.cs   NavMesh PNG 추출 툴
└── Resources/Minimaps/         ← 이 폴더 (Resources.Load 접근 경로)
    ├── {씬이름}_minimap.png
    ├── {씬이름}_MinimapData.asset
    └── MINIMAP_SETUP_GUIDE.md
```

---

## STEP 1 — NavMesh 맵 이미지 추출 (씬별 1회)

### 전제 조건
- 해당 씬이 Unity Editor에 **열려 있어야** 함
- 씬에 **NavMesh가 베이크**되어 있어야 함

### 절차

1. `Tools > Minimap > Extract NavMesh Map Image`
2. **`① NavMesh 분석`** 클릭 → World Min/Max, 삼각형 수 확인
3. 해상도(기본 512) 및 여백(기본 1.0) 설정 후 **`② PNG 추출 및 저장`** 클릭

### 결과물 (자동 생성)
| 파일 | 경로 | 내용 |
|------|------|------|
| PNG 이미지 | `Assets/Resources/Minimaps/{씬이름}_minimap.png` | 흰색 Walkable 구역, 투명 배경 |
| MinimapData 에셋 | `Assets/Resources/Minimaps/{씬이름}_MinimapData.asset` | worldMin/Max 자동 기입, PNG 연결됨 |

### PNG 후보정 (선택)
포토샵 등으로 후보정 후 **같은 파일명으로 덮어쓰기**하면 자동 반영됩니다.

---

## STEP 2 — MinimapManager 설정

씬의 관리 GameObject(예: GameManager 또는 별도 MinimapManager 오브젝트)에 `MinimapManager` 컴포넌트를 추가합니다.

| Inspector 필드 | 설정값 |
|---------------|--------|
| Data | 비워두기 (씬 이름으로 **자동 로드**) 또는 직접 에셋 연결 |

> **자동 로드 조건**: `Assets/Resources/Minimaps/{SceneName}_MinimapData.asset` 파일이 존재해야 합니다.

> **플레이어 스폰이 늦는 씬**: `PlayerSpawner` 등에서 `MinimapManager.Instance.SetPlayer(transform, playerController)` 호출로 플레이어를 수동 주입하세요.

---

## STEP 3 — UI 계층 구성

Canvas(Screen Space - Overlay)에 아래 구조를 만듭니다.

```
Canvas
  └── MapRoot
        ├── RadarMapPanel              ← MiniMapUIManager.radarMapPanel
        │     ├── MinimapMask [RectMask2D]   ← RadarMapView 부착
        │     │     ├── RadarBackground [Image]
        │     │     ├── RadarMarkerRoot  [Transform]
        │     │     └── RadarPlayerMarker [Image]
        │     └── ExpandButton [Button]     → MiniMapUIManager.ShowMinimap()
        │
        └── MinimapPanel               ← MiniMapUIManager.minimapPanel
              ├── Backdrop [Image]           반투명 오버레이 (선택)
              ├── MinimapMask [RectMask2D]  ← MinimapView 부착
              │     ├── MinimapBackground [Image]
              │     ├── MinimapMarkerRoot [Transform]
              │     └── MinimapPlayerMarker [Image]
              └── CollapseButton [Button]   → MiniMapUIManager.ShowRadarMap()
```

### 권장 크기
| 패널 | 크기 |
|------|------|
| RadarMapPanel / MinimapMask | 160 × 160 (소형) |
| MinimapPanel / MinimapMask | 400 × 400 (대형, 화면 중앙) |

---

## STEP 4 — 컴포넌트 부착 및 Inspector 연결

### MiniMapUIManager
`MapRoot` 또는 Canvas GameObject에 부착.

| 필드 | 연결 대상 |
|------|----------|
| Radar Map Panel | RadarMapPanel GameObject |
| Minimap Panel | MinimapPanel GameObject |

### RadarMapView
`RadarMapPanel > MinimapMask` GameObject에 부착.

| 필드 | 연결 대상 |
|------|----------|
| Mask Rect | MinimapMask의 RectTransform |
| Bg Rect | RadarBackground의 RectTransform |
| Bg Image | RadarBackground의 Image 컴포넌트 |
| Player Marker | RadarPlayerMarker의 RectTransform |
| Marker Root | RadarMarkerRoot의 Transform |
| Boss/Enemy/Quest/Exit Marker Prefab | 각 마커 UI 프리팹 |
| Zoom Multiplier | 2.0 (기본, 플레이 중 조정) |
| Edge Padding | 8 |

### MinimapView
`MinimapPanel > MinimapMask` GameObject에 부착.

| 필드 | 연결 대상 |
|------|----------|
| Mask Rect | MinimapMask의 RectTransform |
| Bg Rect | MinimapBackground의 RectTransform |
| Bg Image | MinimapBackground의 Image 컴포넌트 |
| Player Marker | MinimapPlayerMarker의 RectTransform |
| Marker Root | MinimapMarkerRoot의 Transform |
| Boss/Enemy/Quest/Exit Marker Prefab | 각 마커 UI 프리팹 |
| Edge Padding | 8 |

---

## STEP 5 — 마커 프리팹 제작

각 마커 프리팹은 `Image 컴포넌트 하나`만 있는 단순한 UI GameObject입니다.

```
BossMarkerPrefab
  └── [RectTransform]
  └── [Image] - 기본 스프라이트는 MinimapMarker.defaultSprite로 오버라이드됨
```

| 마커 종류 | 권장 크기 (레이더) | 권장 크기 (미니맵) |
|----------|-----------------|-----------------|
| 플레이어 (별도 처리) | 16 × 16 | 20 × 20 |
| 보스 | 12 × 12 | 16 × 16 |
| 적 | 6 × 6 | 8 × 8 |
| 퀘스트 | 12 × 12 | 14 × 14 |
| 출구 | 12 × 12 | 14 × 14 |

> RadarMapView와 MinimapView는 **각자 독립된 프리팹 슬롯**을 가지므로 크기·스타일을 다르게 설정할 수 있습니다.

---

## STEP 6 — 월드 오브젝트에 MinimapMarker 부착

보스, 출구 등 미니맵에 표시할 월드 오브젝트에 `MinimapMarker` 컴포넌트를 추가합니다.

```
Boss_SandElemental (GameObject)
  └── [기존 컴포넌트들]
  └── MinimapMarker
        ├── Marker Type:    Boss
        ├── Default Sprite: 보스 아이콘
        ├── Arrow Sprite:   방향 화살표 아이콘
        └── Marker Color:   Red
```

**동작 원리:**
- `OnEnable()` → `MinimapManager.Register()` → `OnMarkerRegistered` 이벤트 → 활성 View들이 UI 마커 생성
- `OnDisable()` → `MinimapManager.Unregister()` → `OnMarkerUnregistered` 이벤트 → 활성 View들이 UI 마커 제거
- **코드 추가 없이** MinimapMarker 컴포넌트만 붙이면 됩니다.

---

## STEP 7 — 플레이어 태그 확인

`MinimapManager`는 `GameObject.FindGameObjectWithTag("Player")`로 플레이어를 탐색합니다.
플레이어 오브젝트에 **`Player` 태그**가 설정되어 있는지 확인하세요.

---

## 씬별 반복 작업 체크리스트

- [ ] NavMesh 베이크 완료
- [ ] `Tools > Minimap > Extract NavMesh Map Image` 실행
- [ ] PNG 후보정 후 덮어쓰기 (선택)
- [ ] MinimapManager의 `Data` 필드가 비어 있으면 자동 로드 / 또는 수동 연결
- [ ] 플레이인 후 씬 뷰에서 파란 Gizmo 박스가 맵 범위와 일치하는지 확인
- [ ] 레이더맵에서 zoomMultiplier 값 조정 (플레이 중 Inspector에서 조정 가능)

---

## 런타임 API

```csharp
// 패널 전환 (버튼 외 코드에서 호출)
MiniMapUIManager.Instance.ShowRadarMap();
MiniMapUIManager.Instance.ShowMinimap();

// 현재 상태 확인
bool isRadar = MiniMapUIManager.Instance.IsRadarMapVisible;

// 플레이어 스폰이 늦는 씬에서 수동 주입
MinimapManager.Instance.SetPlayer(playerTransform, playerController);

// MinimapData 수동 교체
MinimapManager.Instance.SetData(newMinimapData);
```

---

## 문제 해결

| 증상 | 원인 | 해결 |
|------|------|------|
| `NavMesh를 찾을 수 없습니다` | NavMesh 미베이크 | Window > AI > Navigation > Bake |
| 맵 이미지가 표시되지 않음 | MinimapData 로드 실패 | 파일명이 `{씬이름}_MinimapData.asset`인지 확인 |
| 플레이어 마커가 없음 | Player 태그 미설정 | 플레이어 GameObject에 Player 태그 확인 |
| 마커가 등록되지 않음 | 프리팹 미연결 | 각 View의 Marker Prefab 슬롯 확인 |
| 패널 전환 시 마커 위치 튐(팝핑) | ForceRefresh 미호출 | View가 BaseMapView를 상속하고 `base.OnEnable()` 호출하는지 확인 |
| 레이더맵 배경이 움직이지 않음 | zoomMultiplier ≤ 0 | Inspector에서 양수 값(예: 2.0) 설정 |
| 전체 미니맵 마커 위치 오차 | worldMin/Max 불일치 | 추출 툴 재실행 또는 MinimapData 수동 조정 |
