# Phase 6: Unity Editor 설정 - 초보자용 상세 가이드

## 🎯 최종 목표
챕터 시스템 UI를 Unity Editor에서 설정하여 **3개 스테이지 → 50개 스테이지 시스템**으로 확장

---

## 📦 Part 1: StageButtonUI 프리팹 생성 (20분)

### Step 1-1: 빈 Button 생성

**1. Hierarchy 창에서 우클릭**
```
우클릭 → UI → Button - TextMeshPro
```

**2. 이름 변경**
- Hierarchy에서 방금 생성된 "Button (TMP)" 선택
- Inspector 창 상단에서 이름을 `StageButtonUI_Prefab`으로 변경
- Enter 키로 확정

**3. RectTransform 크기 조정**
- Inspector → Rect Transform 컴포넌트
- Width: `100`
- Height: `100`
- (정사각형 버튼)

---

### Step 1-2: StageButtonUI 스크립트 추가

**1. Inspector → Add Component 클릭**

**2. 검색창에 "StageButtonUI" 입력**
- 자동완성으로 `StageButtonUI` 스크립트 나타남
- 클릭하여 추가

**3. 자동 할당 확인**
- `button`: Button 컴포넌트 자동 할당됨 ✅
- `buttonImage`: Image 컴포넌트 자동 할당됨 ✅

---

### Step 1-3: UI 요소 추가 (Text, Icon)

#### **A. 스테이지 번호 텍스트 생성**

**1. StageButtonUI_Prefab 선택 후 우클릭**
```
우클릭 → UI → Text - TextMeshPro
```

**2. 이름 변경: `Text_StageNumber`**

**3. TextMeshPro 설정**
- Inspector → TextMeshPro - Text (UI) 컴포넌트
- Text Input: `1` (임시)
- Font Size: `40`
- Alignment: 중앙 정렬 (가운데 버튼 클릭)
- Color: 흰색 (255, 255, 255, 255)

**4. RectTransform 설정**
- Anchor Presets: 중앙 하단 클릭 후 Alt+Shift 누른 채 클릭 (Stretch 모드)
- Left: `0`, Right: `0`, Top: `0`, Bottom: `0`
- (부모 크기에 맞춰 늘어남)

---

#### **B. 잠금 아이콘 생성**

**1. StageButtonUI_Prefab 선택 후 우클릭**
```
우클릭 → UI → Image
```

**2. 이름 변경: `Icon_Lock`**

**3. Image 설정**
- Source Image: 자물쇠 아이콘 스프라이트 할당 (없으면 임시로 UI-Default 사용)
- Color: 빨간색 (255, 0, 0, 255)
- Preserve Aspect: ✅ 체크

**4. RectTransform 설정**
- Anchor Presets: 중앙 (Alt+Shift 누른 채 중앙 클릭)
- Width: `60`, Height: `60`
- Pos X: `0`, Pos Y: `0`

---

#### **C. 클리어 아이콘 생성**

**1. StageButtonUI_Prefab 선택 후 우클릭**
```
우클릭 → UI → Image
```

**2. 이름 변경: `Icon_Clear`**

**3. Image 설정**
- Source Image: 체크마크 아이콘 스프라이트 (없으면 임시로 UI-Default 사용)
- Color: 초록색 (0, 255, 0, 255)
- Preserve Aspect: ✅ 체크

**4. RectTransform 설정**
- Anchor Presets: 우측 상단 (Alt+Shift 누른 채 우측 상단 클릭)
- Width: `30`, Height: `30`
- Pos X: `-10`, Pos Y: `-10`

**5. 초기 상태: 비활성화**
- Inspector 왼쪽 상단 체크박스 **해제** (GameObject 비활성화)

---

#### **D. 보스 아이콘 생성**

**1. StageButtonUI_Prefab 선택 후 우클릭**
```
우클릭 → UI → Image
```

**2. 이름 변경: `Icon_Boss`**

**3. Image 설정**
- Source Image: 왕관 아이콘 스프라이트 (없으면 임시로 UI-Default 사용)
- Color: 금색 (255, 215, 0, 255)
- Preserve Aspect: ✅ 체크

**4. RectTransform 설정**
- Anchor Presets: 좌측 상단 (Alt+Shift 누른 채 좌측 상단 클릭)
- Width: `40`, Height: `40`
- Pos X: `10`, Pos Y: `-10`

**5. 초기 상태: 비활성화**
- Inspector 왼쪽 상단 체크박스 **해제** (GameObject 비활성화)

---

### Step 1-4: StageButtonUI Inspector 연결

**1. Hierarchy에서 `StageButtonUI_Prefab` 선택**

**2. Inspector → StageButtonUI 컴포넌트로 스크롤**

**3. 드래그 앤 드롭으로 할당**

| 필드 | 할당할 GameObject |
|------|-------------------|
| `button` | 자동 할당됨 ✅ |
| `buttonImage` | 자동 할당됨 ✅ |
| `stageNumberText` | Hierarchy에서 `Text_StageNumber` 드래그 |
| `lockIcon` | Hierarchy에서 `Icon_Lock` 드래그 |
| `clearIcon` | Hierarchy에서 `Icon_Clear` 드래그 |
| `bossIcon` | Hierarchy에서 `Icon_Boss` 드래그 |

**4. 색상 설정 (기본값 유지 가능)**
- `Locked Color`: (0.5, 0.5, 0.5, 1) - 회색
- `Unlocked Color`: (1, 1, 1, 1) - 흰색
- `Cleared Color`: (0.8, 1, 0.8, 1) - 연두색
- `Selected Color`: (1, 1, 0.6, 1) - 노란색

---

### Step 1-5: 프리팹 저장

**1. Project 창에서 폴더 생성 (없으면)**
```
Project → Assets 우클릭 → Create → Folder
이름: "Prefabs"

Prefabs 폴더 내부에서 우클릭 → Create → Folder
이름: "UI"
```

**2. Hierarchy에서 StageButtonUI_Prefab을 Project 창으로 드래그**
```
Hierarchy의 "StageButtonUI_Prefab"을
Project의 "Assets/Prefabs/UI/" 폴더로 드래그
```

**3. 프리팹 생성 확인**
- Project 창에서 `StageButtonUI_Prefab.prefab` 파일 생성 확인
- 아이콘이 파란색 큐브 모양으로 표시됨

**4. Hierarchy에서 원본 삭제**
- Hierarchy의 `StageButtonUI_Prefab` 선택
- Delete 키 또는 우클릭 → Delete
- (프리팹은 Project에 저장되었으므로 안전)

---

## 📦 Part 2: Panel_Stage 구조 개편 (30분)

### Step 2-1: 기존 Panel_Stage 찾기

**1. Hierarchy 창에서 검색**
```
검색창에 "Panel_Stage" 입력
```

**2. Panel_Stage GameObject 선택**
- 로비 씬의 Canvas 하위에 위치
- 만약 없다면 LobbyUIController의 `stageSelectPanel` 필드 확인

---

### Step 2-2: ChapterMapUI 영역 생성

#### **A. ChapterMapUI GameObject 생성**

**1. Panel_Stage 선택 후 우클릭**
```
우클릭 → Create Empty
```

**2. 이름 변경: `ChapterMapUI`**

**3. RectTransform 설정**
- Anchor Presets: 상단 중앙 (Alt+Shift 누른 채 상단 중앙 클릭)
- Width: `800`
- Height: `150`
- Pos X: `0`
- Pos Y: `-100` (상단에서 아래로 100px)

---

#### **B. 이전 챕터 버튼 (◀) 생성**

**1. ChapterMapUI 선택 후 우클릭**
```
우클릭 → UI → Button - TextMeshPro
```

**2. 이름 변경: `Button_PrevChapter`**

**3. Button 설정**
- Inspector → Button 컴포넌트
- Interactable: ✅ 체크
- Transition: Color Tint

**4. Text 변경**
- ChapterMapUI → Button_PrevChapter → Text (TMP) 선택
- Text Input: `◀`
- Font Size: `60`
- Alignment: 중앙

**5. RectTransform 설정**
- Anchor Presets: 좌측 중앙
- Width: `80`, Height: `80`
- Pos X: `50`, Pos Y: `0`

---

#### **C. 챕터 제목 텍스트 생성**

**1. ChapterMapUI 선택 후 우클릭**
```
우클릭 → UI → Text - TextMeshPro
```

**2. 이름 변경: `Text_ChapterTitle`**

**3. TextMeshPro 설정**
- Text Input: `Chapter 1: 초원의 시작`
- Font Size: `40`
- Alignment: 중앙 정렬
- Font Style: Bold
- Color: 흰색

**4. RectTransform 설정**
- Anchor Presets: 중앙
- Width: `500`, Height: `60`
- Pos X: `0`, Pos Y: `20`

---

#### **D. 다음 챕터 버튼 (▶) 생성**

**1. ChapterMapUI 선택 후 우클릭**
```
우클릭 → UI → Button - TextMeshPro
```

**2. 이름 변경: `Button_NextChapter`**

**3. Button 설정**
- Button 컴포넌트 동일

**4. Text 변경**
- Text Input: `▶`
- Font Size: `60`
- Alignment: 중앙

**5. RectTransform 설정**
- Anchor Presets: 우측 중앙
- Width: `80`, Height: `80`
- Pos X: `-50`, Pos Y: `0`

---

#### **E. 챕터 설명 텍스트 생성**

**1. ChapterMapUI 선택 후 우클릭**
```
우클릭 → UI → Text - TextMeshPro
```

**2. 이름 변경: `Text_ChapterDescription`**

**3. TextMeshPro 설정**
- Text Input: `평화로운 초원에서 모험을 시작하세요`
- Font Size: `24`
- Alignment: 중앙 정렬
- Color: 회색 (180, 180, 180, 255)

**4. RectTransform 설정**
- Anchor Presets: 중앙
- Width: `500`, Height: `40`
- Pos X: `0`, Pos Y: `-30`

---

#### **F. 챕터 진행도 텍스트 생성**

**1. ChapterMapUI 선택 후 우클릭**
```
우클릭 → UI → Text - TextMeshPro
```

**2. 이름 변경: `Text_ChapterProgress`**

**3. TextMeshPro 설정**
- Text Input: `3/10 클리어`
- Font Size: `28`
- Alignment: 중앙 정렬
- Color: 노란색 (255, 255, 0, 255)

**4. RectTransform 설정**
- Anchor Presets: 중앙
- Width: `200`, Height: `40`
- Pos X: `0`, Pos Y: `-65`

---

#### **G. (선택사항) 챕터 아이콘 이미지 생성**

**1. ChapterMapUI 선택 후 우클릭**
```
우클릭 → UI → Image
```

**2. 이름 변경: `Image_ChapterIcon`**

**3. Image 설정**
- Source Image: 챕터 아이콘 스프라이트 할당
- Preserve Aspect: ✅ 체크

**4. RectTransform 설정**
- Anchor Presets: 좌측 중앙
- Width: `100`, Height: `100`
- Pos X: `-300`, Pos Y: `0`

---

#### **H. ChapterMapUI 스크립트 추가**

**1. Hierarchy에서 `ChapterMapUI` GameObject 선택**

**2. Inspector → Add Component → "ChapterMapUI" 검색 후 추가**

**3. Inspector 연결**

| 필드 | 할당할 GameObject |
|------|-------------------|
| `prevChapterButton` | Button_PrevChapter 드래그 |
| `nextChapterButton` | Button_NextChapter 드래그 |
| `chapterTitleText` | Text_ChapterTitle 드래그 |
| `chapterDescriptionText` | Text_ChapterDescription 드래그 |
| `chapterProgressText` | Text_ChapterProgress 드래그 |
| `chapterIconImage` | Image_ChapterIcon 드래그 (선택사항) |

**4. 설정값 입력**
- `maxChapterId`: `5`
- `enableDebugLogs`: ✅ 체크

---

### Step 2-3: StageButtonContainer 생성

#### **A. 빈 GameObject 생성**

**1. Panel_Stage 선택 후 우클릭**
```
우클릭 → Create Empty
```

**2. 이름 변경: `StageButtonContainer`**

**3. RectTransform 설정**
- Anchor Presets: 중앙 (Alt+Shift 누른 채 중앙 클릭)
- Width: `600`
- Height: `300`
- Pos X: `0`
- Pos Y: `0` (중앙)

---

#### **B. Grid Layout Group 추가**

**1. StageButtonContainer 선택**

**2. Inspector → Add Component → "Grid Layout Group" 검색 후 추가**

**3. Grid Layout Group 설정**

**기본 설정:**
```
Padding:
  - Left: 10
  - Right: 10
  - Top: 10
  - Bottom: 10

Cell Size:
  - X: 100
  - Y: 100

Spacing:
  - X: 10
  - Y: 10

Start Corner: Upper Left
Start Axis: Horizontal
Child Alignment: Middle Center
Constraint: Fixed Column Count
Constraint Count: 5
```

**결과**: 5열 × 2행 배치 (총 10개 버튼)

---

#### **C. (선택사항) Content Size Fitter 추가**

**1. StageButtonContainer 선택**

**2. Inspector → Add Component → "Content Size Fitter" 검색 후 추가**

**3. Content Size Fitter 설정**
```
Horizontal Fit: Unconstrained
Vertical Fit: Preferred Size
```

**효과**: 버튼 개수에 따라 높이 자동 조절

---

### Step 2-4: StageSelectPanelController 재설정

#### **A. Panel_Stage의 StageSelectPanelController 찾기**

**1. Hierarchy에서 `Panel_Stage` GameObject 선택**

**2. Inspector에서 `StageSelectPanelController` 컴포넌트 찾기**
- 스크롤하여 컴포넌트 확인

---

#### **B. 새로운 필드 설정**

**필수 할당:**

| 필드 | 할당 방법 |
|------|-----------|
| `stageSelectPanel` | 기존 유지 (Panel_Stage 자신) |
| `playButton` | 기존 유지 |
| `backButton` | 기존 유지 |
| **🆕 `chapterMapUI`** | Hierarchy에서 `ChapterMapUI` 드래그 |
| **🆕 `stageButtonContainer`** | Hierarchy에서 `StageButtonContainer` 드래그 |
| **🆕 `stageButtonPrefab`** | Project에서 `StageButtonUI_Prefab.prefab` 드래그 |
| `stageNameText` | 기존 유지 |
| `stageDescriptionText` | 기존 유지 |
| `bestTimeText` | 기존 유지 |
| `rewardPreviewText` | 기존 유지 |

---

#### **C. 레거시 필드 정리 (선택사항)**

**더 이상 사용하지 않는 필드:**
```
stageButtons[] → 배열 크기 0으로 변경
stageImages[] → 배열 크기 0으로 변경
stageIds[] → 배열 크기 0으로 변경
colorReferenceButton → 그대로 유지 (혹시 모를 호환성)
```

---

## 📦 Part 3: LobbyUIController 연결 확인 (5분)

### Step 3-1: LobbyUIController 검증

**1. Hierarchy에서 `Canvas` 또는 `LobbyUIController` GameObject 찾기**

**2. Inspector → `LobbyUIController` 컴포넌트 확인**

**3. 필수 연결 확인**

| 필드 | 확인 사항 |
|------|-----------|
| `stageSelectPanelController` | Panel_Stage의 StageSelectPanelController 컴포넌트가 할당되어 있는지 확인 |

**4. 할당되지 않았다면**
- Hierarchy에서 `Panel_Stage` 선택
- `LobbyUIController`의 `stageSelectPanelController` 필드로 드래그

---

## 📦 Part 4: 레이아웃 미세 조정 (10분)

### Step 4-1: Panel_Stage 전체 레이아웃

**권장 구조:**

```
Panel_Stage (전체 패널)
├─ ChapterMapUI (상단, Pos Y: -100)
│  └─ (버튼, 텍스트들)
│
├─ StageButtonContainer (중앙, Pos Y: 0)
│  └─ (동적 생성 버튼들)
│
├─ StageInfoPanel (하단, Pos Y: 200)
│  ├─ Text_StageName
│  ├─ Text_Description
│  ├─ Text_BestTime
│  └─ Text_RewardPreview
│
└─ Button_Play (최하단, Pos Y: 350)
```

---

### Step 4-2: RectTransform 미세 조정

**ChapterMapUI:**
```
Anchor: Top Center
Pos X: 0
Pos Y: -100
Width: 800
Height: 150
```

**StageButtonContainer:**
```
Anchor: Center
Pos X: 0
Pos Y: 0
Width: 600
Height: 300
```

**StageInfoPanel:**
```
Anchor: Bottom Center
Pos X: 0
Pos Y: 200
Width: 700
Height: 200
```

**Button_Play:**
```
Anchor: Bottom Center
Pos X: 0
Pos Y: 50
Width: 200
Height: 60
```

---

## 🧪 Part 5: 테스트 (10분)

### Step 5-1: Play Mode 진입

**1. Unity Editor → Play 버튼 클릭 (Ctrl+P)**

**2. 로비 씬 시작 확인**

---

### Step 5-2: 기본 동작 테스트

**테스트 항목:**

**1. 챕터 전환**
- [ ] ◀ 버튼 클릭 시 콘솔에 로그 출력
- [ ] ▶ 버튼 클릭 시 콘솔에 로그 출력
- [ ] Chapter 1에서 ◀ 버튼 비활성화 확인

**2. 스테이지 버튼 생성**
- [ ] 챕터 선택 시 1~10개 버튼 동적 생성
- [ ] Grid Layout으로 정렬됨 (5×2)
- [ ] 버튼에 번호 표시 (1, 2, 3, ...)

**3. 시각적 상태**
- [ ] 해금된 스테이지: 흰색
- [ ] 잠긴 스테이지: 회색 + 자물쇠 아이콘
- [ ] Stage 10: 보스 아이콘 표시

**4. 스테이지 선택**
- [ ] 해금된 스테이지 클릭 시 노란색 하이라이트
- [ ] StageInfoPanel에 정보 표시
- [ ] Play 버튼 활성화

---

### Step 5-3: 콘솔 로그 확인

**정상 작동 시 콘솔 로그:**

```
[ChapterMapUI] 챕터 1 표시
[StageSelectPanelController] 챕터 1 스테이지 버튼 생성 시작
[StageSelectPanelController] 버튼 생성: CH01_ST01 (Index: 1, Unlocked: True, ...)
[StageSelectPanelController] 버튼 생성: CH01_ST02 (Index: 2, Unlocked: False, ...)
...
[StageSelectPanelController] 스테이지 버튼 생성 완료: 10개
```

---

### Step 5-4: 에러 처리

**자주 발생하는 에러:**

**1. "NullReferenceException: Object reference not set"**
- **원인**: Inspector 연결이 안 됨
- **해결**: Part 2-4, 3-1 다시 확인

**2. "stageButtonPrefab이 null입니다!"**
- **원인**: 프리팹 할당 안 됨
- **해결**: StageSelectPanelController에 StageButtonUI_Prefab 드래그

**3. "ChapterData를 찾을 수 없습니다"**
- **원인**: Resources/Stages/Chapters/ 폴더에 ChapterData 없음
- **해결**: Phase 2에서 ChapterData 생성 필요 (이전 단계)

**4. "버튼이 생성되지 않음"**
- **원인**: Grid Layout Group 설정 오류
- **해결**: Constraint Count = 5로 설정

---

## 📊 완성 체크리스트

### 프리팹
- [ ] `Assets/Prefabs/UI/StageButtonUI_Prefab.prefab` 생성됨
- [ ] 프리팹에 Text, Icon 4개 포함됨
- [ ] StageButtonUI 컴포넌트 모든 필드 연결됨

### Panel_Stage
- [ ] ChapterMapUI GameObject 생성 + 컴포넌트 추가
- [ ] ChapterMapUI 6개 UI 요소 생성 (버튼2, 텍스트4)
- [ ] StageButtonContainer 생성 + Grid Layout Group 추가
- [ ] StageSelectPanelController 3개 신규 필드 연결

### 연동
- [ ] LobbyUIController → StageSelectPanelController 연결
- [ ] Play Mode 진입 시 에러 없음
- [ ] 챕터 전환 작동
- [ ] 스테이지 버튼 동적 생성

---

## ⚠️ 문제 해결 (Troubleshooting)

### 문제 1: "StageButtonUI 컴포넌트를 찾을 수 없습니다"

**증상**: Add Component에서 StageButtonUI 검색해도 안 나옴

**해결책**:
1. 스크립트 컴파일 에러 확인 (Console 창)
2. Unity Editor 재시작
3. Assets → Reimport All

---

### 문제 2: "버튼 클릭이 안 됩니다"

**증상**: 스테이지 버튼 클릭해도 반응 없음

**해결책**:
1. Canvas에 `GraphicRaycaster` 컴포넌트 있는지 확인
2. EventSystem GameObject 있는지 확인 (Hierarchy)
3. Button 컴포넌트 Interactable ✅ 체크 확인

---

### 문제 3: "Grid Layout이 이상하게 배치됩니다"

**증상**: 버튼이 겹치거나 화면 밖으로 나감

**해결책**:
1. StageButtonContainer 크기 확인 (Width: 600, Height: 300)
2. Grid Layout Group 설정 재확인:
   - Cell Size: (100, 100)
   - Spacing: (10, 10)
   - Constraint Count: 5
3. Canvas Scaler 확인 (Scale With Screen Size 권장)

---

### 문제 4: "챕터 데이터를 찾을 수 없습니다"

**증상**: 콘솔에 "ChapterData를 찾을 수 없습니다: Chapter 1"

**해결책**:
1. Resources/Stages/Chapters/ 폴더 확인
2. CH01_Data.asset 파일 있는지 확인
3. Phase 2 (챕터 시스템 기반) 먼저 완료 필요

---

## 🎨 UI 디자인 팁

### 색상 팔레트 (권장)

**배경:**
- 패널 배경: (30, 30, 40, 230) - 어두운 반투명

**텍스트:**
- 제목: (255, 255, 255, 255) - 흰색
- 설명: (180, 180, 180, 255) - 회색
- 강조: (255, 215, 0, 255) - 금색

**버튼 상태:**
- 잠김: (100, 100, 100, 255) - 진한 회색
- 해금: (255, 255, 255, 255) - 흰색
- 클리어: (150, 255, 150, 255) - 연두색
- 선택: (255, 255, 150, 255) - 노란색

---

### 폰트 크기 (권장)

- 챕터 제목: 40~50
- 스테이지 이름: 32~40
- 설명 텍스트: 20~24
- 버튼 번호: 40~50

---

## 📦 다음 단계

**이 설정이 완료되면:**

1. **Play Mode 테스트**
   - 챕터 전환 작동 확인
   - 스테이지 버튼 생성 확인
   - 게임 시작 가능 확인

2. **Resources 폴더 정리**
   - 레거시 STAGE_001~003 파일 삭제
   - CH01_ST01~CH05_ST10 파일 확인

3. **Phase 7 준비**
   - 스토리 기록 UI 구현
   - 컷신 다시보기 기능

---

**작성일**: 2025-12-28  
**소요 시간**: 약 1시간~1시간 30분 (처음 설정 시)  
**난이도**: ⭐⭐⭐ (중급)

