# Phase 6: UI 통합 - Unity Editor 설정 가이드

## 🎯 목표
챕터 시스템 UI를 Unity Editor에서 설정하여 3개 → 50개 스테이지 시스템으로 확장

---

## 📋 설정 단계

### Step 1: StageButtonUI 프리팹 생성

**1-1. 빈 GameObject 생성**
- Hierarchy → 우클릭 → UI → Button
- 이름: `StageButtonUI_Prefab`

**1-2. StageButtonUI 컴포넌트 추가**
- Inspector → Add Component → `StageButtonUI`

**1-3. UI 요소 구조 생성**
```
StageButtonUI_Prefab (Button, Image, StageButtonUI)
├─ Text_StageNumber (TextMeshPro - Text)
├─ Icon_Lock (Image) - 잠금 아이콘
├─ Icon_Clear (Image) - 클리어 체크마크
└─ Icon_Boss (Image) - 보스 아이콘 (왕관 등)
```

**1-4. StageButtonUI Inspector 설정**
- `button`: 자동 할당됨
- `buttonImage`: 자동 할당됨
- `stageNumberText`: Text_StageNumber 드래그
- `lockIcon`: Icon_Lock GameObject 드래그
- `clearIcon`: Icon_Clear GameObject 드래그
- `bossIcon`: Icon_Boss GameObject 드래그
- 색상 설정 (기본값 사용 가능):
  - `lockedColor`: (0.5, 0.5, 0.5, 1) 회색
  - `unlockedColor`: (1, 1, 1, 1) 흰색
  - `clearedColor`: (0.8, 1, 0.8, 1) 연두색
  - `selectedColor`: (1, 1, 0.6, 1) 노란색

**1-5. 프리팹 저장**
- `StageButtonUI_Prefab`을 Hierarchy에서 `Assets/Prefabs/UI/` 폴더로 드래그
- Hierarchy에서 원본 삭제

---

### Step 2: Panel_Stage 구조 개편

**2-1. ChapterMapUI 영역 생성**
```
Panel_Stage (기존)
├─ ChapterMapUI (신규 GameObject 생성)
│  ├─ Button_PrevChapter (◀ 버튼)
│  ├─ Text_ChapterTitle (TextMeshPro)
│  ├─ Button_NextChapter (▶ 버튼)
│  ├─ Text_ChapterDescription (TextMeshPro)
│  ├─ Text_ChapterProgress (TextMeshPro) "3/10 클리어"
│  └─ Image_ChapterIcon (Image, 선택사항)
```

**2-2. ChapterMapUI 컴포넌트 추가**
- `ChapterMapUI` GameObject 선택
- Inspector → Add Component → `ChapterMapUI`
- Inspector 설정:
  - `prevChapterButton`: Button_PrevChapter 드래그
  - `nextChapterButton`: Button_NextChapter 드래그
  - `chapterTitleText`: Text_ChapterTitle 드래그
  - `chapterDescriptionText`: Text_ChapterDescription 드래그
  - `chapterProgressText`: Text_ChapterProgress 드래그
  - `chapterIconImage`: Image_ChapterIcon 드래그 (선택사항)
  - `maxChapterId`: 5
  - `enableDebugLogs`: ✅ 체크 (개발 중)

**2-3. StageButtonContainer 생성**
```
Panel_Stage
├─ ChapterMapUI (위에서 생성)
├─ StageButtonContainer (신규 GameObject 생성)
│  └─ (여기에 동적 버튼 생성됨)
```

**2-4. Grid Layout Group 설정**
- `StageButtonContainer` 선택
- Add Component → Layout → `Grid Layout Group`
- 설정:
  - `Cell Size`: (100, 100) 또는 원하는 크기
  - `Spacing`: (10, 10)
  - `Start Corner`: Upper Left
  - `Start Axis`: Horizontal
  - `Constraint`: Fixed Column Count
  - `Constraint Count`: 5 (5열) 또는 2 (2열)

---

### Step 3: StageSelectPanelController 재설정

**3-1. Panel_Stage의 StageSelectPanelController 설정**
- `Panel_Stage` GameObject 선택
- Inspector → `StageSelectPanelController` 컴포넌트 확인

**3-2. 새로운 필드 설정**
- `chapterMapUI`: ChapterMapUI GameObject 드래그
- `stageButtonContainer`: StageButtonContainer GameObject 드래그
- `stageButtonPrefab`: StageButtonUI_Prefab 프리팹 드래그

**3-3. 레거시 필드 제거 (선택사항)**
- `stageButtons[]` 배열 → 빈 배열로 변경 (사용 안 함)
- `stageImages[]` 배열 → 빈 배열로 변경 (사용 안 함)
- `stageIds[]` 배열 → 빈 배열로 변경 (사용 안 함)

**3-4. 기존 UI 유지**
- `stageSelectPanel`: 기존 유지
- `playButton`: 기존 유지
- `backButton`: 기존 유지
- `stageNameText`: 기존 유지
- `stageDescriptionText`: 기존 유지
- `bestTimeText`: 기존 유지
- `rewardPreviewText`: 기존 유지

---

### Step 4: 레이아웃 조정

**4-1. ChapterMapUI 위치**
- Position: (0, 200, 0) 상단 영역
- Size: (800, 100)

**4-2. StageButtonContainer 위치**
- Position: (0, 0, 0) 중앙 영역
- Size: (600, 300) Grid Layout에 따라 조정

**4-3. StageInfoPanel 위치**
- Position: (0, -150, 0) 하단 영역
- Size: (600, 200)

---

## 🧪 테스트 체크리스트

### 1. 챕터 전환 테스트
- [ ] ◀ 버튼 클릭 시 이전 챕터로 이동
- [ ] ▶ 버튼 클릭 시 다음 챕터로 이동
- [ ] Chapter 1에서 ◀ 버튼 비활성화
- [ ] Chapter 5에서 ▶ 버튼 비활성화
- [ ] 잠긴 챕터로 이동 시 버튼 비활성화

### 2. 스테이지 버튼 생성 테스트
- [ ] 챕터 전환 시 1~10개 버튼 동적 생성
- [ ] 기존 버튼 자동 제거 (메모리 누수 없음)
- [ ] Grid Layout 자동 배치 정상 작동
- [ ] 버튼 개수가 챕터별로 다를 수 있음 (stageCount)

### 3. 잠금/해금 시각화 테스트
- [ ] 잠긴 스테이지: 회색 + 자물쇠 아이콘
- [ ] 해금된 스테이지: 흰색 + 번호 표시
- [ ] 클리어한 스테이지: 연두색 + 체크마크 아이콘
- [ ] 선택된 스테이지: 노란색 하이라이트

### 4. 보스 스테이지 아이콘 테스트
- [ ] Stage 10에만 보스 아이콘 표시
- [ ] Stage 1~9에는 보스 아이콘 숨김

### 5. 스테이지 정보 연동 테스트
- [ ] 스테이지 버튼 클릭 시 StageInfoPanel 업데이트
- [ ] 스테이지 이름 정상 표시
- [ ] 설명, 최고 기록, 보상 정상 표시
- [ ] Play 버튼 활성화

### 6. 게임 시작 테스트
- [ ] Play 버튼 클릭 시 씬 전환
- [ ] 올바른 씬 로드 (CH01_ST01, CH02_ST05 등)
- [ ] 스테이지 진행도 정상 기록

### 7. 진행도 시스템 연동 테스트
- [ ] 스테이지 클리어 시 자동 해금 (Stage 1 → 2)
- [ ] 챕터 클리어 시 다음 챕터 해금 (Chapter 1 → 2)
- [ ] 재입장 시 진행도 유지
- [ ] UI 실시간 업데이트 (이벤트 구독)

---

## 🎨 UI 레이아웃 예시

```
┌─────────────────────────────────────────────┐
│  ◀  Chapter 1: 초원의 시작  ▶             │
│  "평화로운 초원에서 모험을 시작하세요"      │
│  3/10 클리어 ✅                              │
├─────────────────────────────────────────────┤
│  [1] [2] [3] [4] [5]                         │
│  [6] [7] [8] [9] [👑10]                      │
│  (1=해금, 2=클리어✓, 3~10=잠금🔒)            │
├─────────────────────────────────────────────┤
│  Stage 1: 초원 입구                          │
│  "첫 번째 모험이 시작됩니다."                │
│  최고 기록: 45.2초 | 보상: Gold Chest        │
│  [▶ Play]                       [← Back]     │
└─────────────────────────────────────────────┘
```

---

## ⚠️ 주의사항

### 1. Resources 폴더 구조
- ChapterData: `Resources/Stages/Chapters/CH01~05_Data.asset`
- StageConfig: `Resources/Stages/Configs/Chapters/CH01_ST01_Config.asset`

### 2. 씬 이름 일치
- StageConfig.SceneName과 실제 씬 파일명 일치 필수
- 예: `CH01_ST01` (씬 파일) = `CH01_ST01` (StageConfig)

### 3. 레거시 시스템 제거
- STAGE_001~003 참조 제거 완료
- CH01_ST01~CH05_ST10 체계로 통일

---

## 🚀 다음 단계 (Phase 7)

- 스토리 기록 UI (로비 좌측 메뉴)
- 컷신 목록 자동 생성
- 로비에서 오버레이 재생

---

**작성일**: 2025-12-28  
**작성자**: Phase 6 UI 개선 시스템

