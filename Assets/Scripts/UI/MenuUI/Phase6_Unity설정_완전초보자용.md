# Phase 6: Unity Editor 설정 - 순서대로만 따라하세요

**⚠️ 중요: 이 가이드는 위에서 아래로 순서대로 따라하세요. 건너뛰지 마세요!**

---

## 🎯 Part 1: StageButtonUI 프리팹 만들기 (20분)

### 📍 Step 1: 기본 버튼 만들기

**1. Hierarchy 창 찾기**
- Unity 왼쪽에 있는 창입니다
- "Hierarchy" 탭이 보입니다

**2. 버튼 만들기**
```
Hierarchy 창에서 빈 공간 우클릭
→ UI 선택
→ Button - TextMeshPro 클릭
```

**3. 이름 바꾸기**
- Hierarchy에 "Button (TMP)"가 생성됨
- 이것을 클릭하세요
- 클릭한 상태에서 F2 키 누르기 (또는 한 번 더 클릭)
- 이름을 `StageButtonUI_Prefab`으로 변경
- Enter 키 누르기

**4. 크기 조절**
- `StageButtonUI_Prefab` 선택된 상태에서
- 오른쪽 Inspector 창 보기
- Rect Transform 찾기 (제일 위에 있음)
- Width 숫자 클릭 → `100` 입력
- Height 숫자 클릭 → `100` 입력

**✅ 체크포인트**: Hierarchy에 "StageButtonUI_Prefab"이라는 100×100 버튼이 있어야 합니다

---

### 📍 Step 2: StageButtonUI 스크립트 붙이기

**1. StageButtonUI_Prefab이 선택된 상태에서**
- Inspector 창 아래로 스크롤
- 제일 아래 "Add Component" 버튼 클릭

**2. 검색창에 입력**
- `StageButtonUI` 타이핑
- 목록에서 "Stage Button UI (Script)" 클릭

**3. 자동 할당 확인**
- Inspector에 "Stage Button UI (Script)" 컴포넌트가 추가됨
- `button` 옆에 "Button (Button)" 이라고 써있음 → ✅ 자동 연결됨
- `buttonImage` 옆에 "StageButtonUI_Prefab (Image)" 써있음 → ✅ 자동 연결됨

**✅ 체크포인트**: Inspector에 Stage Button UI 컴포넌트가 보이고, button과 buttonImage가 자동으로 채워져 있어야 합니다

---

### 📍 Step 3: 스테이지 번호 텍스트 만들기

**❗ 중요: 이제부터는 StageButtonUI_Prefab의 **안에** 만듭니다**

**1. Hierarchy에서 StageButtonUI_Prefab 찾기**

**2. StageButtonUI_Prefab을 우클릭** (⚠️ 빈 공간 아님!)
```
우클릭 → UI → Text - TextMeshPro 클릭
```

**3. 이름 바꾸기**
- "Text (TMP)"가 StageButtonUI_Prefab **아래**에 생김 (들여쓰기됨)
- 이것을 클릭
- F2 키 → `Text_StageNumber`로 변경

**4. 숫자 입력하기**
- Text_StageNumber 선택된 상태에서
- Inspector → TextMeshPro - Text (UI) 컴포넌트 찾기
- "Text Input" 찾기 (큰 흰 박스)
- 거기에 `1` 입력 (숫자 하나만!)

**5. 크기 조절**
- 같은 컴포넌트에서 "Font Size" 찾기
- 숫자를 `40`으로 변경

**6. 가운데 정렬**
- "Alignment" 찾기 (여러 개 작은 버튼들)
- 가운데 버튼 클릭 (세로 3개 × 가로 3개 중 정중앙)

**7. 전체 채우기**
- Rect Transform 컴포넌트로 스크롤 올라가기
- Anchors 찾기 (작은 박스 아이콘)
- 박스 아이콘 클릭 → Alt+Shift 누른 채로 우측 하단 "Stretch Stretch" 클릭

**✅ 체크포인트**: 
- Hierarchy에서 StageButtonUI_Prefab을 펼치면(▶ 클릭) Text_StageNumber가 **들여쓰기되어** 보임
- Scene 뷰에서 버튼 안에 "1"이라는 숫자가 보임

---

### 📍 Step 4: 잠금 아이콘 만들기

**❗ 다시 StageButtonUI_Prefab의 안에 만듭니다**

**1. StageButtonUI_Prefab을 우클릭**
```
우클릭 → UI → Image 클릭
```

**2. 이름 바꾸기**
- "Image"가 StageButtonUI_Prefab 아래에 생김
- F2 키 → `Icon_Lock`으로 변경

**3. 색상 바꾸기 (빨간색)**
- Icon_Lock 선택된 상태에서
- Inspector → Image 컴포넌트 찾기
- "Color" 찾기 (작은 색상 박스)
- 색상 박스 클릭
- R=255, G=0, B=0, A=255 입력
- 또는 색상 선택창에서 빨간색 선택

**4. 크기 조절**
- Rect Transform으로 가기
- Width: `60`
- Height: `60`

**5. 위치 조절**
- Anchors: 가운데 클릭 (Alt+Shift 없이 그냥 중앙 클릭)
- Pos X: `0`
- Pos Y: `0`

**✅ 체크포인트**: Scene 뷰에서 버튼 가운데에 빨간 사각형이 보여야 합니다

---

### 📍 Step 5: 클리어 체크마크 아이콘 만들기

**1. StageButtonUI_Prefab을 우클릭**
```
우클릭 → UI → Image 클릭
```

**2. 이름 바꾸기**
- F2 키 → `Icon_Clear`로 변경

**3. 색상 바꾸기 (초록색)**
- Icon_Clear 선택
- Inspector → Image 컴포넌트
- Color: R=0, G=255, B=0, A=255 (초록색)

**4. 크기 조절**
- Width: `30`
- Height: `30`

**5. 위치 조절 (우측 상단)**
- Anchors: 우측 상단 클릭 (오른쪽 위 구석)
- Pos X: `-10`
- Pos Y: `-10`

**6. ⚠️ 초기 상태 끄기**
- Inspector 맨 위 GameObject 이름 옆에 **체크박스** 있음
- 그 체크박스 **클릭해서 해제** (회색으로 만들기)
- Hierarchy에서 Icon_Clear 이름이 회색으로 변함

**✅ 체크포인트**: Icon_Clear가 Hierarchy에서 회색으로 보이고, Scene에서 안 보여야 정상입니다

---

### 📍 Step 6: 보스 아이콘 만들기

**1. StageButtonUI_Prefab을 우클릭**
```
우클릭 → UI → Image 클릭
```

**2. 이름 바꾸기**
- F2 키 → `Icon_Boss`로 변경

**3. 색상 바꾸기 (금색)**
- Icon_Boss 선택
- Inspector → Image 컴포넌트
- Color: R=255, G=215, B=0, A=255 (금색)

**4. 크기 조절**
- Width: `40`
- Height: `40`

**5. 위치 조절 (좌측 상단)**
- Anchors: 좌측 상단 클릭
- Pos X: `10`
- Pos Y: `-10`

**6. ⚠️ 초기 상태 끄기**
- Inspector 맨 위 체크박스 **해제**
- Hierarchy에서 Icon_Boss 이름이 회색으로 변함

**✅ 체크포인트**: Icon_Boss도 Hierarchy에서 회색으로 보이고, Scene에서 안 보여야 정상입니다

---

### 📍 Step 7: Inspector 연결하기 (드래그 앤 드롭)

**❗ 이제 만든 것들을 연결합니다**

**1. Hierarchy에서 StageButtonUI_Prefab 클릭**

**2. Inspector에서 Stage Button UI (Script) 컴포넌트 찾기**
- 아래로 스크롤하면 보임

**3. 연결 시작 (총 4개 연결)**

**[연결 1] stageNumberText 연결**
- `stageNumberText` 필드 옆에 "None (TMP_Text)" 써있음
- Hierarchy에서 `Text_StageNumber` 찾기
- `Text_StageNumber`를 **마우스로 드래그**해서
- `stageNumberText` 필드에 **드롭** (놓기)
- "Text_StageNumber (TextMeshPro...)" 로 바뀜 → ✅

**[연결 2] lockIcon 연결**
- `lockIcon` 필드 옆에 "None (Game Object)" 써있음
- Hierarchy에서 `Icon_Lock` 찾기
- `Icon_Lock`을 드래그해서
- `lockIcon` 필드에 드롭
- "Icon_Lock (GameObject)" 로 바뀜 → ✅

**[연결 3] clearIcon 연결**
- `clearIcon` 필드에
- Hierarchy에서 `Icon_Clear` 드래그해서 드롭
- "Icon_Clear (GameObject)" → ✅

**[연결 4] bossIcon 연결**
- `bossIcon` 필드에
- Hierarchy에서 `Icon_Boss` 드래그해서 드롭
- "Icon_Boss (GameObject)" → ✅

**✅ 체크포인트**: 
Stage Button UI 컴포넌트의 필드 상태:
- button: Button (Button) ✅ (자동)
- buttonImage: StageButtonUI_Prefab (Image) ✅ (자동)
- stageNumberText: Text_StageNumber (TextMeshPro...) ✅ (방금 연결)
- lockIcon: Icon_Lock (GameObject) ✅ (방금 연결)
- clearIcon: Icon_Clear (GameObject) ✅ (방금 연결)
- bossIcon: Icon_Boss (GameObject) ✅ (방금 연결)

**모두 "None"이 아니어야 합니다!**

---

### 📍 Step 8: Hierarchy 구조 최종 확인

**Hierarchy에서 StageButtonUI_Prefab을 펼쳤을 때 (▶ 클릭) 이렇게 보여야 합니다:**

```
StageButtonUI_Prefab
├─ Text (TMP)  ← 이건 원래 있던 거 (무시)
├─ Text_StageNumber  ← ✅ 우리가 만든 것
├─ Icon_Lock  ← ✅ 우리가 만든 것
├─ Icon_Clear (회색)  ← ✅ 우리가 만든 것 (비활성)
└─ Icon_Boss (회색)  ← ✅ 우리가 만든 것 (비활성)
```

**❗ "Text (TMP)"는 원래 있던 거라 무시하세요 (삭제해도 됨)**

---

### 📍 Step 9: 프리팹으로 저장하기

**1. Project 창 찾기**
- Unity 아래쪽에 "Project" 탭이 있습니다

**2. 폴더 만들기 (없으면)**
```
Project 창에서 "Assets" 폴더 클릭
→ 빈 공간 우클릭
→ Create → Folder
→ 이름: "Prefabs" 입력
→ Enter

Prefabs 폴더 더블클릭해서 들어가기
→ 빈 공간 우클릭
→ Create → Folder
→ 이름: "UI" 입력
→ Enter
```

**3. 프리팹 저장**
- Hierarchy에서 `StageButtonUI_Prefab` 클릭
- 마우스로 **드래그** 시작
- Project 창의 `Assets/Prefabs/UI/` 폴더로 **드롭**
- 파란 큐브 아이콘이 생김 → ✅ 프리팹 완성!

**4. Hierarchy에서 원본 삭제**
- Hierarchy의 `StageButtonUI_Prefab` 클릭
- Delete 키 누르기
- (Canvas와 EventSystem은 그대로 둠)

**✅ 체크포인트**: 
- Project → Assets → Prefabs → UI 폴더에 "StageButtonUI_Prefab" (파란 큐브) 있음
- Hierarchy에는 더 이상 StageButtonUI_Prefab 없음

---

## 🎉 Part 1 완료!

**확인 방법:**
1. Project → Assets → Prefabs → UI → StageButtonUI_Prefab 더블클릭
2. Scene 뷰에 버튼이 보임
3. 숫자 "1", 빨간 사각형(잠금), 초록/금색은 안 보임 (비활성화)

**다음 단계**: Part 2로 이동하세요!

---
---

## 🎯 Part 2: Panel_Stage 구조 만들기 (30분)

### 📍 Step 1: Panel_Stage 찾기

**1. Hierarchy 창에서 검색**
- Hierarchy 창 위에 검색창 있음
- `Panel_Stage` 입력

**2. 찾았다면**
- Panel_Stage 클릭
- Scene 뷰에서 해당 패널이 하이라이트됨

**3. 못 찾았다면**
- Hierarchy에서 "Canvas" 찾기
- Canvas 펼치기 (▶ 클릭)
- 그 안에서 Panel_Stage 찾기

**✅ 체크포인트**: Panel_Stage가 선택되어 있고, Inspector에 StageSelectPanelController 컴포넌트가 보임

---

### 📍 Step 2: ChapterMapUI 빈 오브젝트 만들기

**1. Hierarchy에서 Panel_Stage 우클릭**
```
우클릭 → Create Empty 클릭
```

**2. 이름 바꾸기**
- "GameObject"가 Panel_Stage **아래**에 생김 (들여쓰기됨)
- F2 키 → `ChapterMapUI` 입력
- Enter

**3. 크기 및 위치 설정**
- ChapterMapUI 선택
- Inspector → Rect Transform
- Width: `800`
- Height: `150`
- Pos X: `0`
- Pos Y: `-100`

**✅ 체크포인트**: Panel_Stage를 펼치면 ChapterMapUI가 들여쓰기되어 보임

---

### 📍 Step 3: 이전 챕터 버튼 (◀) 만들기

**❗ ChapterMapUI의 **안에** 만듭니다**

**1. Hierarchy에서 ChapterMapUI 우클릭**
```
우클릭 → UI → Button - TextMeshPro 클릭
```

**2. 이름 바꾸기**
- F2 키 → `Button_PrevChapter`

**3. 버튼 텍스트 바꾸기**
- Hierarchy에서 Button_PrevChapter 펼치기 (▶)
- 그 안에 "Text (TMP)" 있음
- 그것을 클릭
- Inspector → TextMeshPro - Text (UI) 컴포넌트
- Text Input에 `◀` 입력 (또는 `<` 입력)
- Font Size: `60`

**4. 버튼 위치 조절**
- Hierarchy에서 Button_PrevChapter 클릭 (부모!)
- Inspector → Rect Transform
- Anchors: 좌측 중앙 클릭 (왼쪽 가운데)
- Width: `80`, Height: `80`
- Pos X: `50`, Pos Y: `0`

**✅ 체크포인트**: Scene 뷰에서 ChapterMapUI 안쪽 왼쪽에 "◀" 버튼 보임

---

### 📍 Step 4: 챕터 제목 텍스트 만들기

**1. ChapterMapUI 우클릭**
```
우클릭 → UI → Text - TextMeshPro
```

**2. 이름 바꾸기**
- F2 키 → `Text_ChapterTitle`

**3. 텍스트 설정**
- Inspector → TextMeshPro - Text (UI)
- Text Input: `Chapter 1: 초원의 시작`
- Font Size: `40`
- Alignment: 가운데 버튼 클릭
- Font Style: Bold (B 버튼 클릭)

**4. 위치 조절**
- Rect Transform
- Width: `500`, Height: `60`
- Pos X: `0`, Pos Y: `20`

---

### 📍 Step 5: 다음 챕터 버튼 (▶) 만들기

**1. ChapterMapUI 우클릭**
```
우클릭 → UI → Button - TextMeshPro
```

**2. 이름: `Button_NextChapter`**

**3. 텍스트: `▶` (또는 `>`), Font Size: 60**

**4. 위치**
- Anchors: 우측 중앙
- Width: `80`, Height: `80`
- Pos X: `-50`, Pos Y: `0`

---

### 📍 Step 6: 챕터 설명 텍스트 만들기

**1. ChapterMapUI 우클릭 → UI → Text - TextMeshPro**

**2. 이름: `Text_ChapterDescription`**

**3. 텍스트**
- Input: `평화로운 초원에서 모험을 시작하세요`
- Font Size: `24`
- Alignment: 가운데
- Color: 회색 (R=180, G=180, B=180)

**4. 위치**
- Width: `500`, Height: `40`
- Pos X: `0`, Pos Y: `-30`

---

### 📍 Step 7: 챕터 진행도 텍스트 만들기

**1. ChapterMapUI 우클릭 → UI → Text - TextMeshPro**

**2. 이름: `Text_ChapterProgress`**

**3. 텍스트**
- Input: `3/10 클리어`
- Font Size: `28`
- Alignment: 가운데
- Color: 노란색 (R=255, G=255, B=0)

**4. 위치**
- Width: `200`, Height: `40`
- Pos X: `0`, Pos Y: `-65`

---

### 📍 Step 8: ChapterMapUI 스크립트 추가

**1. Hierarchy에서 ChapterMapUI 클릭** (부모!)

**2. Inspector → Add Component**

**3. 검색: `ChapterMapUI` 입력 → 클릭**

---

### 📍 Step 9: ChapterMapUI Inspector 연결 (5개)

**ChapterMapUI 선택 상태에서:**

**1. prevChapterButton 연결**
- Hierarchy에서 `Button_PrevChapter` 드래그
- `prevChapterButton` 필드에 드롭

**2. nextChapterButton 연결**
- `Button_NextChapter` 드래그 → 드롭

**3. chapterTitleText 연결**
- `Text_ChapterTitle` 드래그 → 드롭

**4. chapterDescriptionText 연결**
- `Text_ChapterDescription` 드래그 → 드롭

**5. chapterProgressText 연결**
- `Text_ChapterProgress` 드래그 → 드롭

**6. 설정값 입력**
- `maxChapterId`: `5` 입력
- `enableDebugLogs`: 체크박스 ✅

**✅ 체크포인트**: ChapterMapUI 컴포넌트의 5개 필드 모두 "None"이 아님

---

### 📍 Step 10: StageButtonContainer 만들기

**1. Panel_Stage 우클릭**
```
우클릭 → Create Empty
```

**2. 이름: `StageButtonContainer`**

**3. 위치**
- Rect Transform
- Width: `600`, Height: `300`
- Pos X: `0`, Pos Y: `0`

---

### 📍 Step 11: Grid Layout Group 추가

**1. StageButtonContainer 선택**

**2. Add Component → `Grid Layout Group` 검색 → 추가**

**3. 설정 입력**

| 항목 | 값 |
|------|-----|
| Padding - Left | 10 |
| Padding - Right | 10 |
| Padding - Top | 10 |
| Padding - Bottom | 10 |
| Cell Size - X | 100 |
| Cell Size - Y | 100 |
| Spacing - X | 10 |
| Spacing - Y | 10 |
| Start Corner | Upper Left |
| Start Axis | Horizontal |
| Constraint | Fixed Column Count |
| Constraint Count | 5 |

**✅ 체크포인트**: Grid Layout Group 컴포넌트의 Constraint Count가 5

---

### 📍 Step 12: StageSelectPanelController 재설정 (핵심!)

**1. Hierarchy에서 Panel_Stage 클릭**

**2. Inspector에서 Stage Select Panel Controller 찾기**
- 아래로 스크롤

**3. 새로운 필드 3개 연결**

**[연결 1] chapterMapUI**
- Hierarchy에서 `ChapterMapUI` 드래그
- `chapterMapUI` 필드에 드롭
- "ChapterMapUI (ChapterMapUI)" → ✅

**[연결 2] stageButtonContainer**
- Hierarchy에서 `StageButtonContainer` 드래그
- `stageButtonContainer` 필드에 드롭
- "StageButtonContainer (RectTransform)" → ✅

**[연결 3] stageButtonPrefab**
- **Project 창** 열기
- Project → Assets → Prefabs → UI → `StageButtonUI_Prefab` 찾기
- 파란 큐브 아이콘이 있는 프리팹
- 그것을 **드래그**
- Inspector의 `stageButtonPrefab` 필드에 **드롭**
- "StageButtonUI_Prefab (GameObject)" → ✅

**✅ 체크포인트**: 
Stage Select Panel Controller의 새 필드들:
- chapterMapUI: ChapterMapUI (ChapterMapUI) ✅
- stageButtonContainer: StageButtonContainer (RectTransform) ✅
- stageButtonPrefab: StageButtonUI_Prefab (GameObject) ✅

---

## 🎉 Part 2 완료!

**최종 Hierarchy 구조:**

```
Canvas
└─ Panel_Stage
   ├─ ChapterMapUI ← ✅ 새로 만든 것
   │  ├─ Button_PrevChapter
   │  ├─ Text_ChapterTitle
   │  ├─ Button_NextChapter
   │  ├─ Text_ChapterDescription
   │  └─ Text_ChapterProgress
   ├─ StageButtonContainer ← ✅ 새로 만든 것
   ├─ (기존 UI 요소들...)
   ├─ Button_Play
   └─ Button_Back
```

---

## 🧪 Part 3: 테스트하기 (5분)

### 📍 Step 1: Play Mode 진입

**1. Unity 상단 Play 버튼 클릭 (▶ 모양)**

**2. 게임 시작됨**

---

### 📍 Step 2: 로비에서 확인

**1. 캐릭터 슬롯 선택**

**2. "게임 시작" 버튼 클릭**

**3. 스테이지 선택 화면 확인**
- 상단에 "◀ Chapter 1: 초원의 시작 ▶" 보임?
- 중앙에 버튼 10개 (5×2) 생성됨?
- 버튼에 숫자 1~10 보임?
- Stage 1은 흰색, 나머지는 회색?

---

### 📍 Step 3: 콘솔 확인

**1. Console 창 열기**
- 상단 메뉴 → Window → General → Console

**2. 로그 확인**

**정상 로그:**
```
[ChapterMapUI] 챕터 1 표시
[StageSelectPanelController] 챕터 1 스테이지 버튼 생성 시작
[StageSelectPanelController] 버튼 생성: CH01_ST01 (Index: 1, Unlocked: True, ...)
...
[StageSelectPanelController] 스테이지 버튼 생성 완료: 10개
```

**에러가 있다면?** → 아래 문제 해결 섹션으로

---

## ⚠️ 문제 해결

### 문제 1: "stageButtonPrefab이 null입니다!"

**원인**: 프리팹을 연결 안 함

**해결**:
1. Hierarchy → Panel_Stage 클릭
2. Inspector → Stage Select Panel Controller
3. `stageButtonPrefab` 필드 확인
4. "None (Game Object)" 이면 안 됨!
5. Project → Assets → Prefabs → UI → StageButtonUI_Prefab 드래그
6. 필드에 드롭

---

### 문제 2: "ChapterMapUI가 null입니다!"

**원인**: ChapterMapUI 연결 안 함

**해결**:
1. Panel_Stage 클릭
2. Stage Select Panel Controller 찾기
3. `chapterMapUI` 필드에
4. Hierarchy의 ChapterMapUI 드래그

---

### 문제 3: 버튼이 안 보여요

**원인**: StageButtonContainer 위치가 잘못됨

**해결**:
1. Hierarchy → Panel_Stage → StageButtonContainer 클릭
2. Rect Transform
3. Pos X: 0, Pos Y: 0으로 수정

---

### 문제 4: 버튼이 1개만 보여요

**원인**: Grid Layout 설정 오류

**해결**:
1. StageButtonContainer 클릭
2. Grid Layout Group 컴포넌트
3. Constraint: Fixed Column Count로 변경
4. Constraint Count: 5로 변경

---

## ✅ 최종 체크리스트

- [ ] Project에 StageButtonUI_Prefab.prefab 있음
- [ ] Hierarchy에 ChapterMapUI 있음
- [ ] Hierarchy에 StageButtonContainer 있음
- [ ] Panel_Stage의 StageSelectPanelController에 3개 필드 연결됨
- [ ] Play Mode에서 버튼 10개 생성됨
- [ ] 콘솔에 에러 없음

---

**모두 완료하셨나요? 축하합니다! 🎉**

질문이 있으시면 에러 메시지와 함께 알려주세요!

