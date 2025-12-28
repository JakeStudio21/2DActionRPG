# Phase 7: 스토리 기록 UI 구현 계획

## 🎯 목표
로비에서 플레이한 모든 컷신을 다시 볼 수 있는 스토리 기록 시스템 구축

---

## 📊 전체 Phase 진행 상황

```
✅ Phase 0: 데이터 구조 확장 (완료)
✅ Phase 1: 진행도 시스템 확장 (완료)
✅ Phase 2: 챕터 시스템 기반 (완료)
✅ Phase 3: 컷신 시청 여부 추적 (완료)
✅ Phase 4: 컷신 자동 재생 시스템 (완료)
✅ Phase 5: 챕터 종료 처리 (완료)
✅ Phase 6: UI 개선 (방금 완료!) ← 여기까지 완료

🎬 Phase 7: 스토리 기록 UI (다음 단계) ← 지금 설명
📦 Phase 8: Resources 폴더 재편성 (선택)
🔧 Phase 9: 버튼 Pooling (선택)
```

---

## 🎬 Phase 7: 스토리 기록 UI (예상 1.5시간)

### 📋 구현 내용

**1. StoryLogUI.cs (신규 생성)**
- 로비 좌측 메뉴에 "스토리 기록" 버튼 추가
- 프롤로그/챕터1~5 탭 시스템
- 컷신 목록 자동 생성
- 로비에서 오버레이 재생 (씬 전환 없음)

**2. StoryLogItem.cs (신규 생성)**
- 개별 컷신 항목 UI 컴포넌트
- 재생 버튼
- 잠금/해금 상태 표시

---

## 🎨 UI 구조

### **스토리 기록 패널**

```
Panel_StoryLog
├─ Button_Close (X 버튼)
│
├─ TabContainer (챕터 선택)
│  ├─ Tab_Prologue (프롤로그)
│  ├─ Tab_Chapter1 (챕터 1)
│  ├─ Tab_Chapter2 (챕터 2)
│  ├─ Tab_Chapter3 (챕터 3)
│  ├─ Tab_Chapter4 (챕터 4)
│  └─ Tab_Chapter5 (챕터 5)
│
└─ CutsceneListContainer (Scroll View)
   ├─ CutsceneItem_01 (챕터 시작)
   ├─ CutsceneItem_02 (Stage 1 입장)
   ├─ CutsceneItem_03 (Stage 1 클리어)
   ├─ ...
   └─ CutsceneItem_N (챕터 종료)
```

---

## 🔑 핵심 기능

### **1. 컷신 목록 자동 생성**

**로직**:
```csharp
// ChapterData에서 컷신 ID 가져오기
string chapterStartCutscene = chapterData.chapterStartCutsceneId;
string chapterClearCutscene = chapterData.chapterClearCutsceneId;

// StageConfig에서 컷신 ID 가져오기
for (int i = 1; i <= 10; i++)
{
    string stageId = $"CH{chapterId:D2}_ST{i:D2}";
    var config = LoadStageConfig(stageId);
    
    if (!string.IsNullOrEmpty(config.enterCutsceneId))
        AddCutsceneItem(config.enterCutsceneId);
    
    if (!string.IsNullOrEmpty(config.clearCutsceneId))
        AddCutsceneItem(config.clearCutsceneId);
}
```

**결과**: 챕터별로 관련된 모든 컷신이 자동으로 목록에 추가됨

---

### **2. 잠금/해금 시스템**

**잠금 조건**:
```csharp
public bool IsCutsceneUnlocked(string cutsceneId)
{
    // CutsceneProgressTracker에서 시청 여부 확인
    if (CutsceneManager.Instance != null)
    {
        return CutsceneManager.Instance.HasSeenCutscene(cutsceneId);
    }
    return false;
}
```

**시각적 표시**:
- 🔒 잠김: 회색 + 자물쇠 아이콘 + "미시청"
- ✅ 해금: 흰색 + 재생 버튼 + 컷신 제목

---

### **3. 로비에서 오버레이 재생**

**핵심**:
```csharp
public void OnCutsceneItemClicked(string cutsceneId)
{
    // 씬 전환 없이 로비에서 오버레이 재생
    if (CutsceneManager.Instance != null)
    {
        CutsceneManager.Instance.PlayCutsceneInOverlay(cutsceneId);
    }
}
```

**재생 플로우**:
```
1. 컷신 항목 클릭
2. Panel_StoryLog 숨김 (비활성화 아님)
3. 로비 위에 오버레이로 컷신 재생
4. 컷신 종료
5. Panel_StoryLog 다시 표시
```

---

## 📋 Step별 구현 계획

### **Step 1: StoryLogUI.cs 스크립트 생성 (30분)**

**기능**:
```csharp
public class StoryLogUI : MonoBehaviour
{
    [Header("UI 요소")]
    public GameObject storyLogPanel;
    public Button closeButton;
    public Transform cutsceneListContainer;
    public GameObject cutsceneItemPrefab;
    
    [Header("탭 버튼")]
    public Button[] chapterTabs; // 6개 (프롤로그 + 챕터1~5)
    
    private int currentChapterId = 0; // 0 = 프롤로그
    
    // 패널 열기/닫기
    public void ShowPanel()
    public void HidePanel()
    
    // 탭 전환
    public void OnTabClicked(int chapterId)
    
    // 컷신 목록 생성
    private void GenerateCutsceneList(int chapterId)
    
    // 컷신 재생
    private void OnCutsceneItemClicked(string cutsceneId)
}
```

---

### **Step 2: StoryLogItem.cs 프리팹 생성 (30분)**

**UI 구조**:
```
CutsceneItem_Prefab
├─ Image_Background (배경)
├─ Image_Thumbnail (썸네일, 선택사항)
├─ Text_Title ("챕터 1 시작")
├─ Text_Description ("초원으로의 여정")
├─ Button_Play (▶ 버튼)
└─ Icon_Lock (🔒 잠금 아이콘)
```

**스크립트**:
```csharp
public class StoryLogItem : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Button playButton;
    public GameObject lockIcon;
    
    private string cutsceneId;
    public System.Action<string> OnPlayClicked;
    
    public void Setup(string cutsceneId, bool isUnlocked)
    {
        this.cutsceneId = cutsceneId;
        
        // 제목, 설명 설정
        titleText.text = GetCutsceneTitle(cutsceneId);
        descriptionText.text = GetCutsceneDescription(cutsceneId);
        
        // 잠금/해금 상태
        playButton.interactable = isUnlocked;
        lockIcon.SetActive(!isUnlocked);
        
        // 클릭 이벤트
        playButton.onClick.AddListener(() => OnPlayClicked?.Invoke(cutsceneId));
    }
}
```

---

### **Step 3: LobbyUIController 연동 (20분)**

**로비 메뉴에 버튼 추가**:
```csharp
[Header("🎬 스토리 기록")]
public Button storyLogButton;
public StoryLogUI storyLogUI;

private void Start()
{
    // 기존 코드...
    
    if (storyLogButton != null)
        storyLogButton.onClick.AddListener(OnStoryLogButtonClicked);
}

private void OnStoryLogButtonClicked()
{
    if (storyLogUI != null)
    {
        storyLogUI.ShowPanel();
    }
}
```

---

### **Step 4: CutsceneManager 오버레이 재생 기능 (30분)**

**기능 추가**:
```csharp
public class CutsceneManager : Singleton<CutsceneManager>
{
    // 기존 필드...
    
    [Header("🎬 스토리 기록용")]
    public GameObject overlayCanvas; // 로비 위에 표시될 캔버스
    
    /// <summary>
    /// 로비에서 오버레이로 컷신 재생
    /// </summary>
    public void PlayCutsceneInOverlay(string cutsceneId)
    {
        if (string.IsNullOrEmpty(cutsceneId))
            return;
        
        // 오버레이 활성화
        if (overlayCanvas != null)
            overlayCanvas.SetActive(true);
        
        // 컷신 재생
        PlayCutscene(cutsceneId);
        
        // 재생 완료 시 오버레이 비활성화
        OnCutsceneComplete += OnOverlayCutsceneComplete;
    }
    
    private void OnOverlayCutsceneComplete(string cutsceneId)
    {
        if (overlayCanvas != null)
            overlayCanvas.SetActive(false);
        
        OnCutsceneComplete -= OnOverlayCutsceneComplete;
    }
}
```

---

## 🎨 UI 레이아웃 예시

```
┌─────────────────────────────────────────────┐
│  스토리 기록                          [X]    │
├─────────────────────────────────────────────┤
│  [프롤로그] [챕터1] [챕터2] [챕터3] ...     │
├─────────────────────────────────────────────┤
│  ┌─────────────────────────────────┐        │
│  │ 📜 챕터 1 시작            [▶]   │        │
│  │ "초원으로의 여정"               │        │
│  └─────────────────────────────────┘        │
│  ┌─────────────────────────────────┐        │
│  │ 📜 Stage 1 입장           [▶]   │        │
│  │ "첫 번째 모험의 시작"           │        │
│  └─────────────────────────────────┘        │
│  ┌─────────────────────────────────┐        │
│  │ 🔒 Stage 1 클리어          미시청 │        │
│  │ "???"                           │        │
│  └─────────────────────────────────┘        │
│  ...                                        │
└─────────────────────────────────────────────┘
```

---

## 📦 필요한 데이터

### **컷신 메타데이터 (선택사항)**

**CutsceneData.cs 확장**:
```csharp
[CreateAssetMenu(fileName = "CutsceneData", menuName = "Cutscene/Data")]
public class CutsceneData : ScriptableObject
{
    public string cutsceneId;
    public string displayTitle; // "챕터 1 시작"
    public string displayDescription; // "초원으로의 여정"
    public Sprite thumbnail; // 썸네일 이미지
}
```

**또는 간단하게**:
```csharp
// StoryLogUI에서 cutsceneId로 제목 자동 생성
private string GetCutsceneTitle(string cutsceneId)
{
    if (cutsceneId.Contains("START"))
        return "챕터 시작";
    if (cutsceneId.Contains("ENTER"))
        return "스테이지 입장";
    if (cutsceneId.Contains("CLEAR"))
        return "스테이지 클리어";
    
    return cutsceneId; // 기본값
}
```

---

## 🔧 Unity Editor 설정 작업

### **1. Panel_StoryLog 생성**
- Canvas 하위에 새 패널 생성
- Scroll View 추가
- Tab 버튼 6개 추가

### **2. CutsceneItem_Prefab 생성**
- UI → Button 기반
- StoryLogItem 스크립트 추가
- 프리팹 저장

### **3. LobbyUIController 연결**
- 로비 메뉴에 "스토리 기록" 버튼 추가
- StoryLogUI 컴포넌트 연결

---

## ✅ Phase 7 완료 조건

- [ ] StoryLogUI.cs 생성 및 기능 구현
- [ ] StoryLogItem.cs 프리팹 생성
- [ ] 로비에 "스토리 기록" 버튼 추가
- [ ] 탭 전환 작동 (프롤로그 ↔ 챕터1~5)
- [ ] 컷신 목록 자동 생성
- [ ] 잠금/해금 시각화
- [ ] 로비에서 오버레이 재생 작동
- [ ] 컷신 종료 후 스토리 기록 패널로 복귀

---

## ⏱️ 예상 소요 시간

| Step | 작업 | 소요 시간 |
|------|------|-----------|
| Step 1 | StoryLogUI.cs 생성 | 30분 |
| Step 2 | StoryLogItem.cs 프리팹 | 30분 |
| Step 3 | LobbyUIController 연동 | 20분 |
| Step 4 | CutsceneManager 확장 | 30분 |
| **합계** | | **1시간 50분** |

---

## 🎯 다음 단계 (선택사항)

### **Phase 8: Resources 폴더 재편성 (검토 후 결정)**

**목표**: 50개 스테이지 에셋 정리

**내용**:
```
Resources/Stages/
├─ Configs/
│  └─ Chapters/
│     ├─ CH01/
│     │  ├─ CH01_ST01_Config.asset
│     │  ├─ CH01_ST02_Config.asset
│     │  └─ ...
│     ├─ CH02/
│     └─ ...
├─ Chapters/
│  ├─ CH01_Data.asset
│  └─ ...
└─ (기타)
```

**필요성**: 중간 정도 (파일 많아지면 관리 어려움)

---

### **Phase 9: 버튼 Pooling (검토 후 결정)**

**목표**: 성능 최적화

**내용**:
- StageButtonUI 풀링
- 챕터 전환 시 Instantiate/Destroy → 풀링으로 변경

**필요성**: 낮음 (현재 10개 버튼이라 성능 문제 없음)

---

## 🚀 권장 진행 순서

**1. Phase 7 완료 (필수)**
- 스토리 기록 UI는 유저 경험에 중요
- 컷신 시스템 완성도 높임

**2. Phase 8 검토 (선택)**
- 50개 에셋 생성 후 정리 필요 시 진행
- 현재는 3개 테스트만 있어서 불필요

**3. Phase 9 검토 (선택)**
- 성능 문제 발생 시 진행
- 현재는 불필요

---

**다음 세션 시작 방법**:
- "Phase 7 Step 1부터 진행해줘"
- "StoryLogUI.cs 구현해줘"
- "스토리 기록 UI 만들어줘"

---

**작성일**: 2025-12-28  
**Phase 6 완료**: ✅  
**Phase 7 준비**: ✅

