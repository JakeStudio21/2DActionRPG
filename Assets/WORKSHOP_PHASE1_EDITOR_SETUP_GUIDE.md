# 🏭 공방(제작) 시스템 Phase 1 - Unity 에디터 설정 가이드

**작성일:** 2026-02-08  
**목적:** 공방 패널 기본 구조 구축 및 탭 전환 시스템 구현

---

## 📋 **Phase 1 목표**

- ✅ 공방 패널 추가 (WorkshopPanel)
- ✅ 3개 탭 시스템 구현 (강화/합성/분해)
- ✅ 로비 메뉴에서 공방 버튼으로 진입
- ✅ 탭 전환 작동 확인

---

## 🎯 **1단계: Lobby 씬 열기**

```
1. Unity Editor → Project 창
2. Assets/Scenes/ → Lobby.unity 더블클릭
3. Hierarchy 창에서 "LobbyUI" 오브젝트 찾기
```

---

## 🏗️ **2단계: WorkshopPanel 생성**

### **A. 패널 기본 구조 생성**

```
Hierarchy 창에서:

LobbyUI (Canvas 하위)
├─ LobbyPanel (기존)
├─ StageSelectPanel (기존)
├─ InventoryPanel (기존)
├─ ShopPanel (기존)
├─ CharacterInfoPanel (기존)
└─ WorkshopPanel 🆕 (새로 생성)

작업 순서:
1. LobbyUI 오브젝트 우클릭 → UI → Panel
2. 이름: "WorkshopPanel"
3. Inspector에서:
   - Rect Transform → Anchors: Stretch (양쪽 끝)
   - Width: 0, Height: 0 (전체 화면 채우기)
   - Pos X: 0, Pos Y: 0
```

### **B. WorkshopUI 스크립트 연결**

```
1. WorkshopPanel 선택
2. Inspector → Add Component
3. "WorkshopUI" 검색 → 추가
4. 체크박스: ✅ (활성화 상태)
```

---

## 🎨 **3단계: 배경 이미지 추가**

```
WorkshopPanel 하위에 생성:

1. WorkshopPanel 우클릭 → UI → Image
2. 이름: "Background"
3. Inspector 설정:
   - Rect Transform: Anchors Stretch (전체 화면)
   - Color: 검은색 (R:0, G:0, B:0, A:200) ← 반투명 배경
   - Raycast Target: ✅ (체크) ← 클릭 막기
```

---

## 📑 **4단계: 탭 버튼 그룹 생성**

### **A. TabGroup 컨테이너**

```
1. WorkshopPanel 우클릭 → Create Empty
2. 이름: "TabGroup"
3. Rect Transform 설정:
   - Anchors: Top Center (상단 중앙)
   - Pos X: 0, Pos Y: -80 (상단에서 80픽셀 아래)
   - Width: 600, Height: 60
```

### **B. 강화 탭 버튼**

```
1. TabGroup 우클릭 → UI → Button - TextMeshPro
2. 이름: "EnhancementTabButton"
3. Rect Transform:
   - Anchors: Top Left
   - Pos X: 0, Pos Y: 0
   - Width: 200, Height: 60
4. Text (TMP) 설정:
   - Text: "강화"
   - Font Size: 24
   - Alignment: Center Middle
   - Color: 흰색
```

### **C. 합성 탭 버튼**

```
1. EnhancementTabButton 복제 (Ctrl+D)
2. 이름: "FusionTabButton"
3. Rect Transform:
   - Pos X: 200, Pos Y: 0
4. Text: "합성"
```

### **D. 분해 탭 버튼**

```
1. FusionTabButton 복제 (Ctrl+D)
2. 이름: "DismantleTabButton"
3. Rect Transform:
   - Pos X: 400, Pos Y: 0
4. Text: "분해"
```

---

## 📦 **5단계: 서브 패널 생성 (3개)**

### **A. EnhancementSubPanel (강화)**

```
1. WorkshopPanel 우클릭 → UI → Panel
2. 이름: "EnhancementSubPanel"
3. Rect Transform:
   - Anchors: Stretch (전체 화면)
   - Left: 50, Right: 50, Top: 150, Bottom: 100
4. Color: 회색 (R:50, G:50, B:50, A:255)
5. 자식 오브젝트 추가:
   - UI → Text - TextMeshPro
   - 이름: "PlaceholderText"
   - Text: "강화 UI (Phase 2에서 구현)"
   - Font Size: 32
   - Alignment: Center Middle
```

### **B. FusionSubPanel (합성)**

```
1. EnhancementSubPanel 복제 (Ctrl+D)
2. 이름: "FusionSubPanel"
3. 동일한 위치/크기
4. PlaceholderText:
   - Text: "합성 UI (Phase 3에서 구현)"
5. ⚠️ 초기 상태: 비활성화 (체크박스 해제)
```

### **C. DismantleSubPanel (분해)**

```
1. FusionSubPanel 복제 (Ctrl+D)
2. 이름: "DismantleSubPanel"
3. 동일한 위치/크기
4. PlaceholderText:
   - Text: "분해 UI (Phase 4에서 구현)"
5. ⚠️ 초기 상태: 비활성화 (체크박스 해제)
```

---

## 🔘 **6단계: 닫기 버튼 추가**

```
1. WorkshopPanel 우클릭 → UI → Button - TextMeshPro
2. 이름: "CloseButton"
3. Rect Transform:
   - Anchors: Top Right (우상단)
   - Pos X: -30, Pos Y: -30
   - Width: 50, Height: 50
4. Text:
   - Text: "X"
   - Font Size: 28
   - Alignment: Center Middle
   - Color: 흰색
5. Button 색상:
   - Normal: 빨간색 (R:200, G:50, B:50)
   - Highlighted: 밝은 빨강 (R:255, G:100, B:100)
```

---

## 🔗 **7단계: WorkshopUI 컴포넌트 연결**

```
WorkshopPanel 선택 → Inspector → WorkshopUI 컴포넌트

[📑 탭 버튼] 섹션:
- Enhancement Tab Button: EnhancementTabButton 드래그
- Fusion Tab Button: FusionTabButton 드래그
- Dismantle Tab Button: DismantleTabButton 드래그

[📑 탭 텍스트] 섹션:
- Enhancement Tab Text: EnhancementTabButton > Text (TMP) 드래그
- Fusion Tab Text: FusionTabButton > Text (TMP) 드래그
- Dismantle Tab Text: DismantleTabButton > Text (TMP) 드래그

[📦 서브 패널] 섹션:
- Enhancement Sub Panel: EnhancementSubPanel 드래그
- Fusion Sub Panel: FusionSubPanel 드래그
- Dismantle Sub Panel: DismantleSubPanel 드래그

[🔘 공통 버튼] 섹션:
- Close Button: CloseButton 드래그

[📊 디버그] 섹션:
- Show Debug Logs: ✅ (체크) ← 테스트용
```

---

## 🏠 **8단계: 로비 메뉴에 공방 버튼 추가**

### **A. 공방 버튼 생성**

```
Hierarchy 창에서 LobbyPanel 찾기:

LobbyPanel
└─ MenuButtons (또는 버튼들이 있는 컨테이너)
    ├─ ShopButton (기존)
    ├─ InventoryButton (기존)
    ├─ CharacterInfoButton (기존)
    └─ WorkshopButton 🆕 (새로 추가)

작업:
1. 기존 버튼 중 하나 복제 (예: ShopButton → Ctrl+D)
2. 이름: "WorkshopButton"
3. Text: "공방" (또는 "제작")
4. 위치: 다른 버튼들과 나란히 배치
```

### **B. 아이콘 추가 (선택사항)**

```
WorkshopButton 하위에 Image 추가:
- 이름: "Icon"
- Sprite: 망치/대장간 아이콘 (Assets/Sprites/UI/)
- Width: 40, Height: 40
```

---

## 🔧 **9단계: LobbyPanelManager 연결**

```
Hierarchy → LobbyUI 선택
Inspector → LobbyPanelManager 컴포넌트

[=== 패널 참조 ===] 섹션:
- Lobby Panel: LobbyPanel 드래그 (기존)
- Stage Select Panel: StageSelectPanel 드래그 (기존)
- Inventory Panel: InventoryPanel 드래그 (기존)
- Shop Panel: ShopPanel 드래그 (기존)
- Character Info Panel: CharacterInfoPanel 드래그 (기존)
- Workshop Panel: WorkshopPanel 드래그 🆕
```

---

## 🎮 **10단계: LobbyUIController 연결**

```
Hierarchy → LobbyUI 선택
Inspector → LobbyUIController 컴포넌트

[🏪 로비 메뉴 버튼] 섹션:
- Shop Button: ShopButton 드래그 (기존)
- Inventory Button: InventoryButton 드래그 (기존)
- Character Info Button: CharacterInfoButton 드래그 (기존)
- Workshop Button: WorkshopButton 드래그 🆕
- Quit Game Button: QuitGameButton 드래그 (기존)
```

---

## 🎯 **11단계: LobbyInitializer 연결**

```
Hierarchy → LobbyUI 선택
Inspector → LobbyInitializer 컴포넌트

[=== UI 요소 ===] 섹션:
- Start Game Button: StartGameButton 드래그 (기존)
- Inventory Button: InventoryButton 드래그 (기존)
- Character Info Button: CharacterInfoButton 드래그 (기존)
- Shop Button: ShopButton 드래그 (기존)
- Workshop Button: WorkshopButton 드래그 🆕
- Quit Game Button: QuitGameButton 드래그 (기존)
- ... (기타)
```

---

## ✅ **12단계: 최종 체크리스트**

### **패널 구조 확인**

```
☐ WorkshopPanel 생성됨
☐ WorkshopUI 스크립트 연결됨
☐ Background 이미지 있음
☐ TabGroup > 3개 탭 버튼 있음
☐ EnhancementSubPanel 있음 (활성화)
☐ FusionSubPanel 있음 (비활성화)
☐ DismantleSubPanel 있음 (비활성화)
☐ CloseButton 있음
```

### **컴포넌트 연결 확인**

```
☐ WorkshopUI → 모든 필드 연결됨 (None 없음)
☐ LobbyPanelManager → workshopPanel 연결됨
☐ LobbyUIController → workshopButton 연결됨
☐ LobbyInitializer → workshopButton 연결됨
```

### **초기 상태 확인**

```
☐ WorkshopPanel: 활성화 (체크박스 ✅)
☐ EnhancementSubPanel: 활성화 (체크박스 ✅)
☐ FusionSubPanel: 비활성화 (체크박스 ☐)
☐ DismantleSubPanel: 비활성화 (체크박스 ☐)
```

---

## 🧪 **테스트 방법**

### **1단계: 씬 저장 및 실행**

```
1. File → Save Scene (Ctrl+S)
2. Play 버튼 클릭 (또는 Ctrl+P)
```

### **2단계: 로비 진입 확인**

```
✅ 로비 화면에 "공방" 버튼이 보이는가?
✅ 다른 버튼들(가방, 상점, 캐릭터 정보)과 나란히 있는가?
```

### **3단계: 공방 패널 열기**

```
1. "공방" 버튼 클릭
2. 확인 사항:
   ✅ 공방 패널이 화면 전체를 덮는가?
   ✅ 상단에 3개 탭 버튼(강화/합성/분해)이 보이는가?
   ✅ "강화 UI (Phase 2에서 구현)" 텍스트가 보이는가?
   ✅ 우상단에 X 닫기 버튼이 있는가?
```

### **4단계: 탭 전환 테스트**

```
1. "합성" 탭 클릭
   ✅ "합성 UI (Phase 3에서 구현)" 텍스트로 변경되는가?
   ✅ "강화" 탭 텍스트가 흐려지는가? (Alpha 50%)
   ✅ "합성" 탭 텍스트가 밝아지는가? (Alpha 100%)

2. "분해" 탭 클릭
   ✅ "분해 UI (Phase 4에서 구현)" 텍스트로 변경되는가?
   ✅ "분해" 탭만 밝고 나머지는 흐린가?

3. "강화" 탭 클릭
   ✅ 다시 "강화 UI" 텍스트로 돌아오는가?
```

### **5단계: 닫기 버튼 테스트**

```
1. X 닫기 버튼 클릭
2. 확인 사항:
   ✅ 공방 패널이 닫히고 로비로 돌아가는가?
   ✅ 다시 "공방" 버튼을 클릭하면 다시 열리는가?
   ✅ 다시 열었을 때 "강화" 탭이 기본으로 선택되어 있는가?
```

### **6단계: 콘솔 로그 확인**

```
Unity Console 창에서 다음 로그 확인:

✅ "🏭 [WorkshopUI] Awake() - 공방 UI 초기화"
✅ "✅ [WorkshopUI] 강화 탭 버튼 이벤트 연결"
✅ "✅ [WorkshopUI] 합성 탭 버튼 이벤트 연결"
✅ "✅ [WorkshopUI] 분해 탭 버튼 이벤트 연결"
✅ "✅ [WorkshopUI] 닫기 버튼 이벤트 연결"
✅ "✅ [WorkshopUI] Start() - 공방 UI 준비 완료"

탭 클릭 시:
✅ "🔄 [WorkshopUI] 탭 전환: Enhancement → Fusion"
✅ "⚗️ [WorkshopUI] 합성 패널 활성화"
```

---

## 🚨 **문제 해결**

### **문제 1: 공방 버튼이 작동하지 않음**

```
원인: LobbyInitializer에 workshopButton이 연결되지 않음

해결:
1. Hierarchy → LobbyUI 선택
2. Inspector → LobbyInitializer
3. "Workshop Button" 필드에 WorkshopButton 드래그
4. Play 후 Console에서 "✅ 공방 버튼" 로그 확인
```

### **문제 2: 탭 전환이 작동하지 않음**

```
원인: WorkshopUI 컴포넌트에 서브 패널이 연결되지 않음

해결:
1. WorkshopPanel 선택
2. Inspector → WorkshopUI
3. [📦 서브 패널] 섹션의 3개 필드가 모두 연결되었는지 확인
   - None이 있으면 해당 패널 드래그
```

### **문제 3: 탭 텍스트 색상이 변하지 않음**

```
원인: 탭 텍스트(TMP)가 연결되지 않음

해결:
1. WorkshopPanel 선택
2. Inspector → WorkshopUI
3. [📑 탭 텍스트] 섹션:
   - 각 버튼의 하위 Text (TMP) 오브젝트를 드래그
   - 예: EnhancementTabButton > Text (TMP)
```

### **문제 4: X 버튼이 작동하지 않음**

```
원인: CloseButton이 WorkshopUI에 연결되지 않음

해결:
1. WorkshopPanel 선택
2. Inspector → WorkshopUI
3. [🔘 공통 버튼] → Close Button에 CloseButton 드래그
```

### **문제 5: 콘솔에 에러 발생**

```
"🔴 [WorkshopUI] enhancementTabButton이 null입니다!"

해결:
1. WorkshopPanel 선택
2. Inspector → WorkshopUI
3. [📑 탭 버튼] 섹션의 3개 필드 모두 연결
4. 버튼 오브젝트 자체를 드래그 (Text 아님!)
```

---

## 📊 **Phase 1 완료 기준**

```
✅ 1. 공방 버튼 클릭 시 공방 패널이 열림
✅ 2. 3개 탭(강화/합성/분해) 전환 작동
✅ 3. 탭 전환 시 텍스트 색상 변경 (Alpha 50% ↔ 100%)
✅ 4. X 닫기 버튼으로 로비 복귀
✅ 5. 콘솔에 에러 없음 (빨간색 로그 0개)
✅ 6. 디버그 로그가 정상 출력됨

위 6개 항목이 모두 확인되면 Phase 1 완료!
```

---

## 🎯 **다음 단계 (Phase 2 예고)**

```
Phase 2에서 구현할 내용:
- EnhancementUI.cs 구현
- 아이템 선택 슬롯
- 성공률/재료/골드 표시
- 강화 실행 버튼
- 결과 팝업 (성공/실패/파괴)

Phase 1이 완료되면 말씀해주세요!
```

---

## 💾 **씬 저장 잊지 마세요!**

```
File → Save Scene (Ctrl+S)
File → Save Project (Ctrl+Shift+S)
```

**Phase 1 에디터 작업 완료! 🎉**

