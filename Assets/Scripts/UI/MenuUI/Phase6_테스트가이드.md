# Phase 6: 챕터/스테이지 UI 시스템 테스트 가이드

## 📋 Phase 6 완성도 확인

### ✅ 이미 구현된 기능 (100% 완료)
1. **ChapterMapUI** - 챕터 전환 UI 시스템
   - ◀ ▶ 버튼으로 챕터 전환
   - 챕터 제목, 설명, 진행도 표시
   - 챕터 해금/잠금 체크

2. **StageButtonUI** - 개별 스테이지 버튼
   - 동적 버튼 생성 (1~10개)
   - 잠금/해금/클리어 상태 시각화
   - 보스 스테이지 아이콘

3. **StageSelectPanelController** - 통합 컨트롤러
   - 챕터 전환 시 버튼 재생성
   - 스테이지 정보 표시
   - 진행도 업데이트

4. **ChapterManager & StageProgressManager** - 데이터 시스템
   - 50개 스테이지 관리
   - 진행도 저장/로드
   - 해금 로직

---

## 🎨 Unity 에디터 설정 (필수)

### **1. 스테이지 선택 패널 (Panel_Stage)**

#### **A. Hierarchy 구조 확인**
```
Panel_Stage (GameObject)
├─ ChapterMapUI (GameObject) ← 챕터 전환 UI
│  ├─ Button_PrevChapter (Button)
│  ├─ Text_ChapterTitle (TMP_Text)
│  ├─ Button_NextChapter (Button)
│  ├─ Text_ChapterDescription (TMP_Text)
│  └─ Text_ChapterProgress (TMP_Text)
│
├─ StageButtonContainer (GameObject) ← Grid Layout
│  └─ (동적 생성: StageButton_01~10)
│
├─ Stage_Info (GameObject) ← 스테이지 정보
│  ├─ Text_StageName (TMP_Text)
│  ├─ Text_Description (TMP_Text)
│  ├─ Text_BestTime (TMP_Text)
│  └─ Text_RewardPreview (TMP_Text)
│
├─ Button_Play (Button)
└─ Button_Back (Button)
```

#### **B. Panel_Stage Inspector 설정**
**StageSelectPanelController 컴포넌트:**
1. **Stage Select Panel**: `Panel_Stage` (자기 자신)
2. **Chapter Map UI**: `ChapterMapUI` GameObject 드래그
3. **Stage Button Container**: `StageButtonContainer` GameObject 드래그
4. **Stage Button Prefab**: `StageButtonUI_Prefab` 드래그
5. **Stage Name Text**: `Text_StageName` 드래그
6. **Stage Description Text**: `Text_Description` 드래그
7. **Best Time Text**: `Text_BestTime` 드래그
8. **Reward Preview Text**: `Text_RewardPreview` 드래그
9. **Play Button**: `Button_Play` 드래그
10. **Back Button**: `Button_Back` 드래그

#### **C. ChapterMapUI Inspector 설정**
**ChapterMapUI 컴포넌트:**
1. **Prev Chapter Button**: `Button_PrevChapter` 드래그
2. **Next Chapter Button**: `Button_NextChapter` 드래그
3. **Chapter Title Text**: `Text_ChapterTitle` 드래그
4. **Chapter Description Text**: `Text_ChapterDescription` 드래그
5. **Chapter Progress Text**: `Text_ChapterProgress` 드래그
6. **Max Chapter Id**: `5` (기본값)
7. **Enable Debug Logs**: ✅ 체크 (테스트 시)

#### **D. StageButtonContainer 설정**
**Grid Layout Group 컴포넌트 추가:**
1. `Add Component` → `Grid Layout Group`
2. **Cell Size**: X=100, Y=100 (크기 조절 가능)
3. **Spacing**: X=10, Y=10 (간격 조절 가능)
4. **Start Axis**: Horizontal
5. **Child Alignment**: Upper Left
6. **Constraint**: Fixed Column Count = 5 (2줄 × 5개 배치)

#### **E. StageButtonUI_Prefab 설정 확인**
**프리팹 구조:**
```
StageButtonUI_Prefab
├─ Button (Button + Image)
├─ Text_StageNumber (TMP_Text) ← "1~10"
├─ Icon_Lock (Image) ← 빨간색 잠금 아이콘
├─ Icon_Clear (Image) ← 초록색 체크마크
└─ Icon_Boss (Image) ← 금색 보스 아이콘
```

**StageButtonUI 컴포넌트:**
- **Button**: Button 컴포넌트
- **Button Image**: Image 컴포넌트
- **Stage Number Text**: Text_StageNumber
- **Lock Icon**: Icon_Lock GameObject
- **Clear Icon**: Icon_Clear GameObject
- **Boss Icon**: Icon_Boss GameObject
- **Enable Debug Logs**: ✅ 체크 (테스트 시)

---

### **2. 치트 도구 추가 (테스트용)**

#### **Lobby Scene에 치트 도구 추가:**
1. `Lobby` Scene 열기
2. Hierarchy에서 빈 GameObject 생성 → `CheatTool` 이름 변경
3. `Add Component` → `StageProgressCheatTool` 스크립트 추가
4. Inspector 설정:
   - **Enable Cheats**: ✅ 체크
   - **Show Logs**: ✅ 체크
   - 나머지는 기본값

---

## 🧪 테스트 시나리오

### **테스트 준비**
1. Unity 에디터에서 `Lobby` Scene 실행
2. 화면 왼쪽 상단에 **치트 키 안내**가 표시되는지 확인
3. 로비에서 **스테이지 선택** 버튼 클릭

---

### **시나리오 1: 초기 상태 확인 (Chapter 1만 해금)**

#### **예상 결과:**
- ✅ Chapter 1 선택됨
- ✅ 스테이지 버튼 10개 생성 (1~10)
- ✅ Stage 1만 해금 (흰색 버튼)
- ✅ Stage 2~10 잠김 (회색 버튼 + 빨간 잠금 아이콘)
- ✅ Stage 10에 금색 보스 아이콘 표시
- ✅ ◀ 버튼 비활성화 (첫 챕터)
- ✅ ▶ 버튼 비활성화 (Chapter 2 잠김)

#### **로그 확인:**
```
[ChapterMapUI] 챕터 1 표시
[StageSelectPanelController] 스테이지 버튼 생성 완료: 10개
[StageButtonUI] Setup 완료: CH01_ST01 (Unlocked: True)
[StageButtonUI] Setup 완료: CH01_ST02 (Unlocked: False)
...
```

---

### **시나리오 2: 챕터 1 클리어 후 챕터 2 전환**

#### **치트 실행:**
1. **F1 키** 누르기 → Chapter 1 전체 클리어

#### **예상 결과:**
- ✅ Chapter 1 모든 스테이지 초록색 + 체크마크
- ✅ 챕터 진행도: "10/10 클리어 ✅"
- ✅ ▶ 버튼 활성화 (Chapter 2 해금됨)

#### **치트 실행 2:**
2. **▶ 버튼** 클릭 → Chapter 2로 전환

#### **예상 결과:**
- ✅ 챕터 제목: "Chapter 2: 사막의 모험"
- ✅ 스테이지 버튼 재생성 (10개)
- ✅ CH02_ST01만 해금 (흰색)
- ✅ CH02_ST02~10 잠김 (회색 + 잠금 아이콘)
- ✅ ◀ 버튼 활성화 (이전 챕터 이동 가능)

#### **로그 확인:**
```
[StageProgressCheatTool] 🎮 치트: Chapter 1 모든 스테이지 클리어
[StageProgressCheatTool] ✅ Chapter 1 클리어 완료! Chapter 2 해금됨
[ChapterMapUI] 다음 챕터로 이동: 1 → 2
[StageSelectPanelController] 챕터 전환: 1 → 2
[StageSelectPanelController] 스테이지 버튼 생성 완료: 10개
```

---

### **시나리오 3: 스테이지 순차 해금**

#### **치트 실행:**
1. **F6 키** 연속 누르기 → 다음 스테이지 하나씩 해금

#### **예상 결과:**
- ✅ F6 1회: CH02_ST02 해금
- ✅ F6 2회: CH02_ST03 해금
- ✅ ... (순차 해금)
- ✅ 해금된 스테이지는 흰색으로 변경
- ✅ 버튼 클릭 가능해짐

#### **로그 확인:**
```
[StageProgressCheatTool] 🔓 치트: CH02_ST02 해금
[StageProgressCheatTool] 🔓 치트: CH02_ST03 해금
...
```

---

### **시나리오 4: 스테이지 클릭 및 정보 표시**

#### **테스트:**
1. 해금된 스테이지 버튼 클릭

#### **예상 결과:**
- ✅ 선택된 스테이지 노란색으로 변경
- ✅ Stage_Info 패널에 정보 표시:
  - 스테이지 이름: "Chapter 2 - Stage 1"
  - 설명: (StageConfig 내용)
  - 최고 기록: "미클리어" 또는 "30.00초"
  - 보상: "DROP_CH02_ST01_FIRST"
- ✅ Play 버튼 활성화

#### **로그 확인:**
```
[StageSelectPanelController] 스테이지 선택됨: CH02_ST01
[StageSelectPanelController] StageConfig 로드 성공: Chapter 2 - Stage 1
[StageSelectPanelController] 스테이지 이름 설정: Chapter 2 - Stage 1
```

---

### **시나리오 5: 챕터 2~5 전환**

#### **치트 실행:**
1. **F2 키** 누르기 → Chapter 2 클리어
2. **▶ 버튼** 클릭 → Chapter 3 전환
3. **F3 키** 누르기 → Chapter 3 클리어
4. **▶ 버튼** 클릭 → Chapter 4 전환
5. **F4 키** 누르기 → Chapter 4 클리어
6. **▶ 버튼** 클릭 → Chapter 5 전환

#### **예상 결과:**
- ✅ Chapter 1~5 모두 전환 가능
- ✅ 각 챕터마다 10개 버튼 생성
- ✅ 클리어한 챕터는 진행도 "10/10 클리어 ✅"
- ✅ Chapter 5에서 ▶ 버튼 비활성화 (마지막 챕터)

---

### **시나리오 6: 진행도 초기화**

#### **치트 실행:**
1. **F5 키** 누르기 → 진행도 초기화

#### **예상 결과:**
- ✅ 모든 챕터 진행도 초기화
- ✅ CH01_ST01만 해금
- ✅ Chapter 1로 자동 전환
- ✅ 다른 챕터 잠김

#### **로그 확인:**
```
[StageProgressCheatTool] 🔄 치트: 모든 진행도 초기화
[StageProgressCheatTool] ✅ 진행도 초기화 완료! CH01_ST01만 해금됨
```

---

## 🎯 치트 키 요약

| 키 | 기능 | 설명 |
|----|------|------|
| **F1** | Chapter 1 클리어 | CH01_ST01~10 모두 클리어 + Chapter 2 해금 |
| **F2** | Chapter 2 클리어 | CH02_ST01~10 모두 클리어 + Chapter 3 해금 |
| **F3** | Chapter 3 클리어 | CH03_ST01~10 모두 클리어 + Chapter 4 해금 |
| **F4** | Chapter 4 클리어 | CH04_ST01~10 모두 클리어 + Chapter 5 해금 |
| **F5** | 진행도 초기화 | 모든 진행도 삭제, CH01_ST01만 해금 |
| **F6** | 다음 스테이지 해금 | 현재 챕터의 다음 잠긴 스테이지 하나만 해금 |
| **F7** | 현재 챕터 전체 해금 | 현재 챕터의 모든 스테이지 해금 (클리어는 X) |

---

## ❌ 문제 발생 시 체크리스트

### **1. 버튼이 생성되지 않음**
- [ ] `StageButtonPrefab`이 Inspector에 할당되었는지 확인
- [ ] `StageButtonContainer`에 Grid Layout Group이 있는지 확인
- [ ] Console에 에러 메시지 확인

### **2. 챕터 전환이 안 됨**
- [ ] `ChapterMapUI`가 `StageSelectPanelController`에 연결되었는지 확인
- [ ] `OnChapterChanged` 이벤트 구독 로그 확인
- [ ] Chapter 1 클리어 후 Chapter 2 해금되었는지 확인 (F1 키)

### **3. 스테이지 정보가 표시되지 않음**
- [ ] `Text_StageName`, `Text_Description` 등이 Inspector에 연결되었는지 확인
- [ ] `StageConfig` 파일이 존재하는지 확인 (`Resources/Stages/Configs/Chapters/`)

### **4. 치트 키가 작동하지 않음**
- [ ] `StageProgressCheatTool` 컴포넌트가 활성화되어 있는지 확인
- [ ] `Enable Cheats`가 체크되어 있는지 확인
- [ ] Lobby Scene에 CheatTool GameObject가 있는지 확인

---

## 🎉 테스트 통과 기준

### **✅ Phase 6 완료 조건**
- [ ] 5개 챕터 모두 전환 가능
- [ ] 각 챕터마다 10개 버튼 동적 생성
- [ ] 잠금/해금/클리어 상태 시각화 정상
- [ ] 보스 아이콘 (Stage 10) 표시
- [ ] 챕터 진행도 텍스트 정확
- [ ] 스테이지 정보 표시 정상
- [ ] Play 버튼 클릭 시 게임 시작 (씬 로드)

---

## 📝 다음 단계

Phase 6 테스트 완료 후:
1. **Phase 6-B 완료 확인** - 50개 씬/Config 생성
2. **Phase 7 진행 여부** - 추가 UI 기능 (스토리 기록 등)
3. **몬스터/웨이브 시스템 구현** - 각 스테이지별 몬스터 배치

---

**테스트 중 문제 발생 시 Console 로그를 복사하여 보고해주세요!** 🚀

