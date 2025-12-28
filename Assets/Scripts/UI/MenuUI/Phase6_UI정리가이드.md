# Phase 6: UI 정리 가이드 - 중복 요소 제거

## 📊 겹치는 UI 요소 분석 결과

### ❌ 제거할 것 (1개)

**stageSelectTitleText**
- **위치**: Panel_Stage > Stage_Info (또는 최상단)
- **현재 역할**: "스테이지를 선택하세요" 같은 고정 안내 문구
- **제거 이유**: ChapterMapUI의 `Text_ChapterTitle`이 더 유용한 동적 정보 제공
- **대체**: ChapterMapUI의 "Chapter 1: 초원의 시작" 표시

---

### ✅ 유지할 것 (4개)

**1. stageNameText**
- **역할**: 선택한 스테이지 이름 ("Chapter 1 - Stage 1")
- **필요 이유**: 챕터 전체가 아닌 **개별 스테이지** 이름

**2. stageDescriptionText**
- **역할**: 선택한 스테이지 설명
- **필요 이유**: 챕터 설명이 아닌 **스테이지별 상세 설명**

**3. bestTimeText**
- **역할**: 해당 스테이지 최고 클리어 시간
- **필요 이유**: 스테이지별 개인 기록

**4. rewardPreviewText**
- **역할**: 해당 스테이지 보상 정보
- **필요 이유**: 스테이지별 보상 미리보기

---

## 🔧 Unity Editor 정리 작업

### Step 1: stageSelectTitleText UI 제거 (5분)

**1. Hierarchy에서 찾기**
```
Panel_Stage 펼치기 (▶)
→ "Stage_Select_Text" 또는 "Text_StageSelectTitle" 찾기
```

**2. GameObject 삭제**
```
해당 GameObject 선택
→ Delete 키
```

**3. StageSelectPanelController Inspector 정리**
```
Panel_Stage 선택
→ Inspector → Stage Select Panel Controller
→ "stageSelectTitleText" 필드 확인
→ "None (TMP_Text)" 상태로 두기 (스크립트에서 이미 제거됨)
```

---

### Step 2: 레이아웃 재배치 (선택사항, 10분)

**목표**: 남은 UI 요소들을 깔끔하게 정리

**권장 구조**:

```
Panel_Stage
├─ ChapterMapUI (상단, Pos Y: -100)
│  └─ 챕터 제목, 설명, 진행도
│
├─ StageButtonContainer (중앙, Pos Y: 0)
│  └─ 1~10개 버튼
│
├─ Stage_Info (하단, Pos Y: 200)
│  ├─ stageNameText (제목)
│  ├─ stageDescriptionText (설명)
│  ├─ bestTimeText (기록)
│  └─ rewardPreviewText (보상)
│
└─ Button_Play (최하단, Pos Y: 350)
```

**조정 방법**:
1. Stage_Info GameObject 선택
2. Rect Transform → Pos Y 값 조정
3. Scene 뷰에서 위치 확인

---

### Step 3: 기본 텍스트 설정 (선택사항)

**stageNameText 기본 텍스트**:
```
"스테이지를 선택하세요"
```

**stageDescriptionText 기본 텍스트**:
```
"" (비워두기)
```

**이유**: 스테이지 선택 전에 빈 화면보다 안내 문구가 친절함

---

## 🎨 UI 흐름 (정리 후)

### **1. 패널 열릴 때**
```
ChapterMapUI: "Chapter 1: 초원의 시작" ← 챕터 정보
              "평화로운 초원에서 모험을 시작하세요"
              "2/10 클리어"

StageButtons: [1✓] [2] [3🔒] [4🔒] [5🔒]
              [6🔒] [7🔒] [8🔒] [9🔒] [10👑🔒]

Stage_Info:   "스테이지를 선택하세요" ← 안내 문구
              (나머지 비어있음)
```

---

### **2. Stage 1 선택 시**
```
ChapterMapUI: (동일)

StageButtons: [1✓ ⭐] [2] [3🔒] ... ← Stage 1 선택됨

Stage_Info:   "Chapter 1 - Stage 1" ← 스테이지 이름
              "첫 번째 모험이 시작됩니다" ← 스테이지 설명
              "최고 기록: 45.2초" ← 개인 기록
              "보상: Gold Chest" ← 보상 정보
```

---

### **3. Chapter 2로 전환 시**
```
ChapterMapUI: "Chapter 2: 사막의 시련" ← 챕터 정보 자동 변경
              "뜨거운 사막을 건너세요"
              "0/10 클리어"

StageButtons: [1🔒] [2🔒] ... ← 새로운 챕터 버튼

Stage_Info:   "스테이지를 선택하세요" ← 다시 초기화
```

---

## ✅ 정리 완료 체크리스트

- [ ] Hierarchy에서 "Stage_Select_Text" 또는 유사 GameObject 삭제
- [ ] Panel_Stage Inspector에서 stageSelectTitleText 필드 None 상태 확인
- [ ] Play Mode 진입 시 에러 없음
- [ ] 스테이지 선택 패널 열 때 정상 작동
- [ ] ChapterMapUI에 챕터 제목 표시됨
- [ ] Stage_Info에 스테이지 상세 정보 표시됨

---

## 🧪 테스트

### Test 1: 패널 열기
```
로비 → 게임 시작 버튼 클릭
→ ChapterMapUI에 "Chapter 1: 초원의 시작" 보임
→ Stage_Info에 "스테이지를 선택하세요" 보임
→ ✅ 정상
```

### Test 2: 스테이지 선택
```
Stage 1 버튼 클릭
→ Stage_Info에 "Chapter 1 - Stage 1" 표시됨
→ 설명, 기록, 보상 모두 표시됨
→ ✅ 정상
```

### Test 3: 챕터 전환
```
▶ 버튼 클릭
→ ChapterMapUI에 "Chapter 2: ..." 표시됨
→ Stage_Info가 초기화됨
→ ✅ 정상
```

---

## 📋 최종 정리 결과

### **제거된 것**
- ❌ stageSelectTitleText (중복)

### **유지된 것**
- ✅ ChapterMapUI: 챕터 정보 (제목, 설명, 진행도)
- ✅ stageNameText: 개별 스테이지 이름
- ✅ stageDescriptionText: 개별 스테이지 설명
- ✅ bestTimeText: 최고 기록
- ✅ rewardPreviewText: 보상 미리보기

### **역할 분리**
```
ChapterMapUI ← 챕터 전체 정보 (1개 챕터 = 10개 스테이지)
Stage_Info ← 개별 스테이지 상세 정보 (1개 스테이지)
```

---

**작성일**: 2025-12-28  
**소요 시간**: 5~15분  
**난이도**: ⭐ (쉬움)

