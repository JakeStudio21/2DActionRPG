# 🔍 스킬 UI 디버깅 가이드

## RightPanel 스킬 리스트가 안 보이는 문제

---

## ✅ **체크리스트 (순서대로 확인)**

### **1단계: 스킬 데이터 확인**

```
Unity Editor 상단 메뉴:
Tools → Skills → 📊 현재 스킬 데이터 확인

확인 사항:
□ 액티브 스킬: X개 (0개면 문제!)
□ 패시브 스킬: X개 (0개면 문제!)
□ SP: X
□ 레벨: X

→ 스킬이 0개면:
  Tools → Skills → 📚 테스트용 스킬 데이터 초기화 실행
```

---

### **2단계: Inspector 연결 확인**

```
Hierarchy:
SkillBookPanel → SkillSubPanel 선택

Inspector → SkillTabController:

✅ 체크 항목:

📊 SP 표시:
□ SP Text: 연결됨 (None이 아님)
□ Player Level Text: 연결됨

🎯 좌측 장착 슬롯:
□ Active Equip Slots: Size 2
  □ [0]: ActiveSlot_0 연결됨
  □ [1]: ActiveSlot_1 연결됨
□ Passive Equip Slots: Size 3
  □ [0]: PassiveSlot_0 연결됨
  □ [1]: PassiveSlot_1 연결됨
  □ [2]: PassiveSlot_2 연결됨

📋 우측 스킬 리스트: ⭐⭐⭐ (가장 중요!)
□ Active Skill List Parent: 연결됨
  → RightPanel/ActiveSkillSection/ScrollView/Viewport/Content
□ Passive Skill List Parent: 연결됨
  → RightPanel/PassiveSkillSection/ScrollView/Viewport/Content
□ Skill List Item Prefab: 연결됨
  → SkillListItemPrefab

📖 하단 상세 패널:
□ Skill Detail Panel: 연결됨
  → BottomPanel/SkillDetailPanelObject
```

**만약 연결 안 됨:**
```
1. Hierarchy에서 해당 GameObject 찾기
2. SkillTabController Inspector로 드래그
```

---

### **3단계: Prefab 존재 확인**

```
Project 창:
Assets/Prefabs/UI/Skills/

✅ 확인:
□ SkillListItemPrefab.prefab 존재
□ SkillEquipSlotPrefab.prefab 존재

없으면:
→ 프리팹 제작 필요
→ SkillSubPanel_DetailedGuide.md 참고
```

---

### **4단계: Hierarchy 구조 확인**

```
Hierarchy:
SkillBookPanel
└─ SkillSubPanel
    ├─ LeftPanel ✅
    ├─ RightPanel ✅
    │   ├─ ActiveSkillSection ✅
    │   │   ├─ TitleText
    │   │   └─ Scroll View ✅
    │   │       └─ Viewport
    │   │           └─ Content ⭐ (여기에 생성됨!)
    │   └─ PassiveSkillSection ✅
    │       ├─ TitleText
    │       └─ Scroll View ✅
    │           └─ Viewport
    │               └─ Content ⭐ (여기에 생성됨!)
    └─ BottomPanel ✅

확인:
□ RightPanel 존재
□ ActiveSkillSection 존재
□ Scroll View 존재
□ Content 존재
```

---

### **5단계: Content Component 확인**

```
Hierarchy:
RightPanel → ActiveSkillSection → Scroll View → Viewport → Content 선택

Inspector:

✅ 필수 Component:
□ RectTransform
  - Anchor: Top-Center
  - Pivot: X=0.5, Y=1

□ Vertical Layout Group ⭐
  - Spacing: 5
  - Control Child Size: Width ✅, Height ✅
  - Child Force Expand: Width ✅, Height ❌

□ Content Size Fitter ⭐⭐⭐
  - Horizontal Fit: Unconstrained
  - Vertical Fit: Preferred Size

없으면:
→ Add Component로 추가
```

---

### **6단계: Play 모드 콘솔 확인**

```
Play 모드 진입 → 스킬북 패널 열기

Console 창에서 찾을 로그:

✅ 정상:
"✅ [SkillTabController] 초기화 완료"
"🔄 [SkillTabController] UI 전체 갱신 완료"

❌ 오류:
"❌ [SkillTabController] AccountDataManager를 찾을 수 없습니다!"
→ GameManager에서 AccountDataManager.Initialize() 호출 확인

"⚠️ skillListItemPrefab이 null입니다"
→ Prefab 연결 안 됨
```

---

### **7단계: Scene View 확인 (Play 모드)**

```
Play 모드에서:

1. Hierarchy: Content 선택 (ActiveSkillSection의)

2. Scene View 확인:
   □ Content에 자식 오브젝트가 생성되었는가?
   
   예:
   Content
   ├─ SkillListItemUI(Clone)
   ├─ SkillListItemUI(Clone)
   └─ SkillListItemUI(Clone)

3. 자식이 없으면:
   → RefreshSkillList() 호출 안 됨
   → 또는 스킬 데이터가 없음

4. 자식이 있는데 안 보이면:
   → Content Size Fitter 문제
   → Anchor/Pivot 문제
```

---

## 🔧 **문제별 해결 방법**

### **문제 1: 스킬 데이터가 없음**

```
증상:
- Tools → Skills → 현재 스킬 데이터 확인 → 0개

해결:
Tools → Skills → 📚 테스트용 스킬 데이터 초기화
```

### **문제 2: Inspector 연결 안 됨**

```
증상:
- Active Skill List Parent: None
- Passive Skill List Parent: None
- Skill List Item Prefab: None

해결:
1. Hierarchy에서 Content GameObject 찾기
2. SkillTabController Inspector로 드래그
3. Prefab도 Project 창에서 드래그
```

### **문제 3: Prefab이 없음**

```
증상:
- Assets/Prefabs/UI/Skills/ 폴더에 파일 없음

해결:
1. SkillListItemPrefab 제작
   (SkillSubPanel_DetailedGuide.md 4장 참고)
2. Project 창 Assets/Prefabs/UI/Skills/에 저장
```

### **문제 4: Content Component 누락**

```
증상:
- Content에 Vertical Layout Group 없음
- Content Size Fitter 없음

해결:
1. Content 선택
2. Add Component → Vertical Layout Group
3. Add Component → Content Size Fitter
4. Vertical Fit: Preferred Size 설정
```

### **문제 5: SkillTabController 초기화 안 됨**

```
증상:
- 콘솔에 "초기화 완료" 로그 없음

해결:
1. SkillBookPanelUI 확인:
   - Skill Tab Controller 연결되었는지
   
2. SkillBookPanelUI.SwitchTab() 확인:
   - skillTabController.OnTabActivated() 호출하는지
```

### **문제 6: AccountDataManager 없음**

```
증상:
- "AccountDataManager를 찾을 수 없습니다!" 오류

해결:
1. GameManager.Awake() 확인:
   AccountDataManager.Initialize() 호출하는지

2. 없으면 추가:
   void Awake()
   {
       AccountDataManager.Initialize();
   }
```

---

## 🧪 **단계별 테스트**

### **Test 1: 스킬 데이터 확인**

```
Play 모드 전:
Tools → Skills → 📊 현재 스킬 데이터 확인

기대 결과:
액티브 스킬: 2개 이상
패시브 스킬: 2개 이상

실패 시:
Tools → Skills → 📚 테스트용 스킬 데이터 초기화
```

### **Test 2: Inspector 연결 확인**

```
Play 모드 전:
1. SkillSubPanel 선택
2. Inspector → SkillTabController
3. 모든 필드가 "None"이 아닌지 확인

실패 시:
각 필드에 GameObject 드래그
```

### **Test 3: Prefab 생성 확인**

```
Play 모드:
1. 스킬북 패널 열기
2. Hierarchy: Content 선택
3. 자식 오브젝트 확인:
   SkillListItemUI(Clone) 여러 개 있어야 함

실패 시:
- Prefab 연결 확인
- RefreshSkillList() 호출 확인
```

### **Test 4: UI 표시 확인**

```
Play 모드:
1. Game View에서 RightPanel 확인
2. "⚔️ 액티브 스킬" 아래에 스킬 아이템들 보여야 함
3. 각 아이템에 [레벨업] [장착] 버튼 보여야 함

실패 시:
- Content Size Fitter 확인
- Scroll View 설정 확인
```

---

## 📊 **디버깅 흐름도**

```
스킬 리스트가 안 보임
↓
1. 스킬 데이터 있나? (Tools → Skills → 확인)
   ❌ 없음 → Tools → Skills → 초기화
   ✅ 있음 → 2번으로
↓
2. Inspector 연결됐나? (SkillTabController)
   ❌ 안 됨 → Content, Prefab 드래그
   ✅ 됐음 → 3번으로
↓
3. Prefab 있나? (Project 창)
   ❌ 없음 → Prefab 제작
   ✅ 있음 → 4번으로
↓
4. Content Component 있나? (Vertical Layout, Content Size Fitter)
   ❌ 없음 → Add Component
   ✅ 있음 → 5번으로
↓
5. Play 모드 콘솔 확인
   ❌ 오류 있음 → 오류 메시지 확인
   ✅ 정상 → 6번으로
↓
6. Scene View에서 Content 자식 확인
   ❌ 자식 없음 → RefreshSkillList() 호출 안 됨
   ✅ 자식 있음 → Content Size Fitter 문제
↓
해결!
```

---

## 🎯 **빠른 해결 (90% 원인)**

```
대부분의 경우 이 3가지가 문제:

1. 스킬 데이터가 없음 (50%)
   → Tools → Skills → 테스트용 스킬 데이터 초기화

2. Inspector 연결 안 됨 (30%)
   → Content GameObject 드래그
   → Prefab 드래그

3. Content Size Fitter 없음 (10%)
   → Content에 Component 추가
   → Vertical Fit: Preferred Size
```

---

## 💡 **완전 체크리스트 (인쇄용)**

```
□ 스킬 데이터 있음 (Tools → Skills → 확인)
□ Active Skill List Parent 연결
□ Passive Skill List Parent 연결
□ Skill List Item Prefab 연결
□ SkillListItemPrefab.prefab 존재
□ Content에 Vertical Layout Group 있음
□ Content에 Content Size Fitter 있음
□ Content Size Fitter: Vertical Fit = Preferred Size
□ Scroll View: Horizontal ❌, Vertical ✅
□ Play 모드 콘솔에 오류 없음
□ Scene View에서 Content 자식 생성됨
□ Game View에서 스킬 아이템 보임
```

---

## 🚀 **해결 완료 후**

스킬 리스트가 정상적으로 보이면:

1. ✅ 스킬 클릭 → 하단에 상세 정보 표시
2. ✅ [레벨업] 버튼 → SP 차감, 레벨업
3. ✅ [장착] 버튼 → 좌측 슬롯에 아이콘 표시

이 3가지가 모두 작동하면 성공!
