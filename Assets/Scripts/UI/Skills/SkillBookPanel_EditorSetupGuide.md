# 📚 스킬북 패널 Unity Editor 설정 가이드

## Phase 3-Revision: 사용자 이미지 기준 UI 구조

---

## 📋 **1단계: Hierarchy 구조 생성**

### **전체 구조 (WorkshopPanel 참고)**

```
Lobby
  └─ LobbyCanvas
      ├─ ShopPanel (기존)
      ├─ WorkshopPanel (기존)
      └─ SkillBookPanel (🆕 추가)
          ├─ TopPanel (상단: SP, 탭 버튼)
          ├─ SkillSubPanel (스킬 탭)
          │   ├─ LeftPanel (좌측: 장착 슬롯)
          │   ├─ RightPanel (우측: 스킬 리스트)
          │   └─ BottomPanel (하단: 상세 정보)
          └─ RuneSubPanel (룬 탭, Phase 4 이후)
```

---

## 🎯 **2단계: SkillBookPanel 상세 구조**

### **SkillBookPanel**

```
SkillBookPanel (GameObject + SkillBookPanelUI.cs)
├── BackgroundImage (Image) - 전체 배경
│
├── TopPanel (상단 정보 + 탭)
│   ├─ SPText (TMP) - "SP: 8 / 10"
│   ├─ PlayerLevelText (TMP) - "플레이어 Lv.5"
│   ├─ TabButtons
│   │   ├─ SkillTabButton (Button + TMP)
│   │   └─ RuneTabButton (Button + TMP)
│   └─ CloseButton (Button) - X 버튼
│
├── SkillSubPanel (GameObject)
│   ├─ LeftPanel (좌측 장착 슬롯, 세로 배치)
│   │   ├─ TitleText (TMP) - "장착 스킬"
│   │   ├─ ActiveSlotsTitle (TMP) - "액티브"
│   │   ├─ ActiveSlot_0 (SkillEquipSlotUI)
│   │   ├─ ActiveSlot_1 (SkillEquipSlotUI)
│   │   ├─ PassiveSlotsTitle (TMP) - "패시브"
│   │   ├─ PassiveSlot_0 (SkillEquipSlotUI)
│   │   ├─ PassiveSlot_1 (SkillEquipSlotUI)
│   │   └─ PassiveSlot_2 (SkillEquipSlotUI)
│   │
│   ├─ RightPanel (우측 스킬 리스트)
│   │   ├─ ActiveSkillSection
│   │   │   ├─ TitleText (TMP) - "액티브 스킬"
│   │   │   └─ Scroll View
│   │   │       └─ Content (SkillListItemUI 프리팹 생성됨)
│   │   └─ PassiveSkillSection
│   │       ├─ TitleText (TMP) - "패시브 스킬"
│   │       └─ Scroll View
│   │           └─ Content (SkillListItemUI 프리팹 생성됨)
│   │
│   └─ BottomPanel (하단 상세 정보)
│       └─ SkillDetailPanelObject (GameObject + SkillDetailPanel.cs)
│           ├─ SkillIcon (Image)
│           ├─ SkillNameText (TMP)
│           ├─ SkillDescriptionText (TMP)
│           ├─ SkillTypeText (TMP)
│           ├─ LevelInfoPanel
│           │   ├─ CurrentLevelText (TMP)
│           │   ├─ MaxLevelText (TMP)
│           │   ├─ RequiredSPText (TMP)
│           │   └─ UnlockLevelText (TMP)
│           └─ StatsComparePanel
│               ├─ CurrentStatsText (TMP)
│               └─ NextStatsText (TMP)
│
└── RuneSubPanel (GameObject, 비활성화)
    └─ (Phase 4 이후 구현)
```

---

## 🎯 **3단계: SkillListItemUI 프리팹 생성**

### **프리팹 구조 (이미지 기준)**

```
SkillListItemPrefab (GameObject + SkillListItemUI.cs)
├── BackgroundImage (Image)
├── ContentPanel (가로 레이아웃)
│   ├─ SkillIcon (Image)
│   ├─ InfoPanel (세로 레이아웃)
│   │   ├─ SkillNameText (TMP)
│   │   ├─ SkillTypeText (TMP) - "Combat"
│   │   └─ SkillLevelText (TMP) - "Lv.5/10"
│   └─ ButtonPanel (세로 레이아웃)
│       ├─ UpgradeButton (Button)
│       │   └─ UpgradeButtonText (TMP) - "레벨업"
│       └─ EquipButton (Button)
│           └─ EquipButtonText (TMP) - "장착" or "장착중"
└── LockOverlay (GameObject + Image)
    └─ LockIcon (Image)
```

### **권장 크기:**
- Width: 300px (이미지 기준)
- Height: 80px

---

## 🎯 **4단계: SkillEquipSlotUI 프리팹 (좌측 배치용)**

### **프리팹 구조**

```
SkillEquipSlotPrefab (GameObject + SkillEquipSlotUI.cs)
├── BackgroundImage (Image)
├── SkillIcon (Image) - 크게
├── SlotNumberText (TMP) - "액티브 1"
├── SkillLevelText (TMP) - "Lv.2"
└── EmptyOverlay (GameObject + Image)
    └─ EmptyText (TMP) - "빈 슬롯"
```

### **권장 크기:**
- Width: 100px
- Height: 100px

---

## 🎯 **5단계: SkillBookPanelUI 컴포넌트 설정**

### **Inspector 연결**

**📑 탭 버튼:**
- Skill Tab Button: TopPanel/TabButtons/SkillTabButton
- Rune Tab Button: TopPanel/TabButtons/RuneTabButton
- Skill Tab Text: SkillTabButton/Text (TMP)
- Rune Tab Text: RuneTabButton/Text (TMP)

**📦 서브 패널:**
- Skill Sub Panel: SkillSubPanel GameObject
- Rune Sub Panel: RuneSubPanel GameObject (비활성화)

**🔘 공통 버튼:**
- Close Button: TopPanel/CloseButton

**🎮 탭 컨트롤러:**
- Skill Tab Controller: SkillSubPanel에서 SkillTabController 컴포넌트 찾기

---

## 🎯 **6단계: SkillTabController 컴포넌트 설정**

### **SkillSubPanel에 SkillTabController 추가**

**📊 SP 표시:**
- SP Text: TopPanel/SPText
- Player Level Text: TopPanel/PlayerLevelText

**🎯 좌측 장착 슬롯:**
- Active Equip Slots: 크기 2
  - [0]: LeftPanel/ActiveSlot_0
  - [1]: LeftPanel/ActiveSlot_1
- Passive Equip Slots: 크기 3
  - [0]: LeftPanel/PassiveSlot_0
  - [1]: LeftPanel/PassiveSlot_1
  - [2]: LeftPanel/PassiveSlot_2

**📋 우측 스킬 리스트:**
- Active Skill List Parent: RightPanel/ActiveSkillSection/Scroll View/Content
- Passive Skill List Parent: RightPanel/PassiveSkillSection/Scroll View/Content
- Skill List Item Prefab: SkillListItemPrefab 드래그

**📖 하단 상세 패널:**
- Skill Detail Panel: BottomPanel/SkillDetailPanelObject (SkillDetailPanel 컴포넌트)

---

## 🎯 **7단계: SkillEquipSlotUI 개별 설정**

### **각 슬롯별 설정**

**ActiveSlot_0:**
- Slot Index: 0
- Is Active Slot: ✅ true
- Skill Icon: SkillIcon 드래그
- Background Image: BackgroundImage 드래그
- Slot Number Text: SlotNumberText 드래그
- Skill Level Text: SkillLevelText 드래그
- Empty Overlay: EmptyOverlay GameObject 드래그

**나머지 슬롯 동일하게 설정**

---

## 🎯 **8단계: SkillDetailPanel 컴포넌트 설정**

### **Inspector 연결**

**🎨 기본 정보:**
- Skill Icon: SkillIcon
- Skill Name Text: SkillNameText
- Skill Description Text: SkillDescriptionText
- Skill Type Text: SkillTypeText

**📊 레벨 정보:**
- Current Level Text: LevelInfoPanel/CurrentLevelText
- Max Level Text: LevelInfoPanel/MaxLevelText
- Required SP Text: LevelInfoPanel/RequiredSPText
- Unlock Level Text: LevelInfoPanel/UnlockLevelText

**📈 스탯 비교:**
- Current Stats Text: StatsComparePanel/CurrentStatsText
- Next Stats Text: StatsComparePanel/NextStatsText
- Stats Compare Panel: StatsComparePanel GameObject

---

## 🎯 **9단계: LobbyPanelManager 연결**

### **Inspector 설정**

**LobbyPanelManager 컴포넌트:**
- Skill Book Panel: SkillBookPanel GameObject 드래그

---

## 🎯 **10단계: LobbyUIController 버튼 연결**

### **Unity Editor OnClick 이벤트**

**스킬북 버튼:**
1. 로비 UI에서 "스킬&룬" 버튼 찾기
2. Button 컴포넌트 → OnClick()
3. LobbyUIController 드래그
4. 함수: `LobbyUIController.OnSkillBookButton()`

---

## 🎮 **11단계: 테스트**

### **기본 테스트 흐름**

1. **Play 모드 진입**
2. **캐릭터 선택**
3. **"스킬&룬" 버튼 클릭**
4. **스킬 탭 확인**:
   - 좌측: 장착 슬롯 5개
   - 우측: 스킬 목록
   - 하단: 상세 정보 (비어있음)
5. **우측 스킬 클릭** → 하단에 상세 정보 표시
6. **[레벨업] 버튼 클릭** → SP 차감, 레벨업
7. **[장착] 버튼 클릭** → 좌측 슬롯에 아이콘 표시

---

## 📐 **권장 레이아웃 (이미지 기준)**

```
┌─────────────────────────────────────────────────┐
│  SP: 8/10   Lv.5  [스킬][룬]           [X]    │ ← TopPanel
├───────────┬─────────────────────────────────────┤
│           │  [액티브 스킬]                      │
│ 장착 스킬 │  ┌──────────────────────────────┐ │
│           │  │ [갈래화살] Lv.5/10           │ │
│ [액티브]  │  │ WaveClear  [레벨업] [장착중] │ │
│ ┌───────┐│  └──────────────────────────────┘ │
│ │스킬1  ││  ┌──────────────────────────────┐ │
│ └───────┘│  │ [화살의비] Lv.11/10          │ │
│ ┌───────┐│  │ BossBurst  [만렙] [장착]    │ │
│ │스킬2  ││  └──────────────────────────────┘ │
│ └───────┘│                                     │
│          │  [패시브 스킬]                      │
│ [패시브] │  ┌──────────────────────────────┐ │
│ ┌───────┐│  │ [정령의공명] Lv.2/5          │ │
│ │스킬1  ││  │ Combat    [레벨업] [장착]   │ │
│ └───────┘│  └──────────────────────────────┘ │
│ ┌───────┐│                                     │
│ │스킬2  ││  (스크롤...)                       │
│ └───────┘│                                     │
│ ┌───────┐│                                     │
│ │스킬3  ││                                     │
│ └───────┘│                                     │
├───────────┴─────────────────────────────────────┤
│ **하단: 선택된 스킬 상세 정보**               │
│ [갈래 화살]                                    │
│ 적에게 3갈래 화살을 발사합니다                 │
│ [액티브] WaveClear | 현재: Lv.5 | 최대: Lv.10│
│ 필요 레벨: Lv.1 ✅ | 다음 레벨 SP: 6          │
│                                                 │
│ 현재: 데미지 180%, 쿨다운 2.5초               │
│ 다음: 데미지 190%, 쿨다운 2.4초               │
└─────────────────────────────────────────────────┘
```

---

## 🎨 **3단계: SkillListItemPrefab 제작**

### **프리팹 구조 (가로 배치)**

```
SkillListItemPrefab (300px × 80px)
├── BackgroundImage (Image)
├── Button (Button 컴포넌트) - 아이템 전체 클릭용
├── ContentPanel (Horizontal Layout Group)
│   ├─ SkillIcon (80×80px, Image)
│   ├─ InfoPanel (Vertical Layout Group, 120px)
│   │   ├─ SkillNameText (TMP, 16pt)
│   │   ├─ SkillTypeText (TMP, 12pt, 회색)
│   │   └─ SkillLevelText (TMP, 14pt)
│   └─ ButtonPanel (Horizontal Layout Group, 100px)
│       ├─ UpgradeButton (Button, 45px)
│       │   └─ Text (TMP) - "레벨업"
│       └─ EquipButton (Button, 45px)
│           └─ Text (TMP) - "장착"
└── LockOverlay (Image, 반투명 검은색)
    └─ LockIcon (Image, 자물쇠)
```

### **Layout 설정:**
- ContentPanel: Horizontal Layout Group
  - Child Force Expand: Width ✅, Height ✅
  - Spacing: 10
- InfoPanel: Vertical Layout Group
  - Child Force Expand: Width ✅
- ButtonPanel: Horizontal Layout Group
  - Spacing: 5

---

## 🎨 **4단계: SkillEquipSlotPrefab 제작**

### **프리팹 구조 (세로 배치)**

```
SkillEquipSlotPrefab (100px × 120px)
├── BackgroundImage (Image)
├── SkillIcon (Image, 80×80px)
├── SlotNumberText (TMP, 상단) - "액티브 1"
├── SkillLevelText (TMP, 우측 하단) - "Lv.2"
└── EmptyOverlay (Image)
    └─ EmptyText (TMP) - "빈 슬롯"
```

---

## 🎯 **5단계: 컴포넌트 상세 설정**

### **SkillBookPanelUI 설정**

```
📑 탭 버튼:
- skillTabButton: TopPanel/TabButtons/SkillTabButton
- runeTabButton: TopPanel/TabButtons/RuneTabButton
- skillTabText: SkillTabButton/Text
- runeTabText: RuneTabButton/Text

📦 서브 패널:
- skillSubPanel: SkillSubPanel GameObject
- runeSubPanel: RuneSubPanel GameObject

🔘 공통 버튼:
- closeButton: TopPanel/CloseButton

🎮 탭 컨트롤러:
- skillTabController: SkillSubPanel의 SkillTabController 컴포넌트
```

### **SkillTabController 설정**

```
📊 SP 표시:
- spText: TopPanel/SPText
- playerLevelText: TopPanel/PlayerLevelText

🎯 좌측 장착 슬롯:
- activeEquipSlots[0]: LeftPanel/ActiveSlot_0
- activeEquipSlots[1]: LeftPanel/ActiveSlot_1
- passiveEquipSlots[0]: LeftPanel/PassiveSlot_0
- passiveEquipSlots[1]: LeftPanel/PassiveSlot_1
- passiveEquipSlots[2]: LeftPanel/PassiveSlot_2

📋 우측 스킬 리스트:
- activeSkillListParent: RightPanel/ActiveSkillSection/ScrollView/Content
- passiveSkillListParent: RightPanel/PassiveSkillSection/ScrollView/Content
- skillListItemPrefab: SkillListItemPrefab

📖 하단 상세 패널:
- skillDetailPanel: BottomPanel/SkillDetailPanelObject (SkillDetailPanel 컴포넌트)
```

### **SkillListItemUI 프리팹 설정**

```
🎨 기본 UI:
- skillIcon: SkillIcon
- backgroundImage: BackgroundImage
- skillNameText: InfoPanel/SkillNameText
- skillLevelText: InfoPanel/SkillLevelText
- skillTypeText: InfoPanel/SkillTypeText
- lockOverlay: LockOverlay

🎮 버튼:
- upgradeButton: ButtonPanel/UpgradeButton
- upgradeButtonText: UpgradeButton/Text
- equipButton: ButtonPanel/EquipButton
- equipButtonText: EquipButton/Text

💡 Button 컴포넌트:
- 프리팹 루트에 Button 컴포넌트 추가
- OnClick: SkillListItemUI.OnItemClickedFromButton()
```

---

## 🎯 **6단계: LobbyPanelManager 설정**

### **Inspector 추가 설정**

```
=== 패널 참조 ===
- Skill Book Panel: SkillBookPanel GameObject 드래그
```

---

## 🎯 **7단계: LobbyUIController 버튼 연결**

### **Unity Editor OnClick 이벤트**

**로비 UI "스킬&룬" 버튼:**
1. Lobby UI에서 "스킬&룬" 버튼 찾기 (또는 신규 생성)
2. Button → OnClick()
3. LobbyUIController 드래그
4. 함수: `LobbyUIController.OnSkillBookButton()`

---

## 🧪 **8단계: 초기 데이터 세팅 (Context Menu 테스트)**

### **PlayerSkillManager에 임시 데이터 이전 메서드 추가**

```csharp
[ContextMenu("Test: Migrate to AccountData")]
public void TestMigrateToAccountData()
{
    if (AccountDataManager.Instance == null) return;
    
    // 기존 스킬 데이터를 AccountDataManager로 이전
    foreach (var skill in unlockedActiveSkills)
    {
        if (skill != null && skill.skillData != null)
        {
            AccountDataManager.Instance.AddSkill(skill.skillData);
        }
    }
    
    foreach (var skill in unlockedPassiveSkills)
    {
        if (skill != null && skill.skillData != null)
        {
            AccountDataManager.Instance.AddSkill(skill.skillData);
        }
    }
    
    // SP 이전
    AccountDataManager.Instance.AddSP(totalSP);
    AccountDataManager.Instance.SetPlayerLevel(currentPlayerLevel);
    
    Debug.Log("✅ PlayerSkillManager → AccountDataManager 데이터 이전 완료!");
}
```

---

## 🎯 **9단계: 패널 z-order 설정**

### **Canvas 하위 패널 순서 (중요!)**

```
LobbyCanvas
  ├─ LobbyPanel (기본, 항상 보임)
  ├─ ShopPanel (활성화, z-order 변경)
  ├─ WorkshopPanel (활성화, z-order 변경)
  ├─ InventoryPanel (활성화, z-order 변경)
  ├─ CharacterInfoPanel (활성화, z-order 변경)
  └─ SkillBookPanel (활성화, z-order 변경) ← 🆕
```

**중요:**
- 모든 패널이 **활성화(Active) 상태**로 유지
- `SetAsLastSibling()`으로 순서만 변경
- 버튼 클릭 시 해당 패널이 최상위로 올라옴

---

## 🎮 **10단계: 테스트 시나리오**

### **전체 흐름 테스트**

```
1. Play 모드 진입
2. 캐릭터 선택
3. "스킬&룬" 버튼 클릭
4. [스킬] 탭 확인:
   - 좌측: 장착 슬롯 (비어있음)
   - 우측: 스킬 목록 (정령의 공명, 갈래 화살 등)
   - 하단: 빈 상태
5. 우측에서 "정령의 공명" 클릭
   - 하단에 상세 정보 표시
6. "정령의 공명"의 [레벨업] 버튼 클릭
   - SP 차감, Lv.1 → Lv.2
   - 상단 SP 표시 변경: "8/10" → "6/10"
7. "정령의 공명"의 [장착] 버튼 클릭
   - 좌측 [패시브 슬롯 1]에 아이콘 표시
   - [장착] 버튼 → [장착중] 변경
8. [X] 버튼으로 닫기
   - 로비로 복귀
```

---

## 💡 **추가 팁**

### **색상 가이드 (이미지 기준)**

```
배경: RGB(222, 184, 135) - 베이지
잠금: RGB(77, 77, 77) - 어두운 회색
장착: RGB(255, 204, 77) - 황금색
선택: RGB(200, 230, 255) - 연한 파란색
```

### **버튼 색상**

```
레벨업 버튼: RGB(100, 200, 100) - 초록색
장착 버튼: RGB(100, 150, 255) - 파란색
비활성화: RGB(150, 150, 150) - 회색
```

### **폰트 크기**

```
제목: 18pt
본문: 14pt
작은 글씨: 12pt
버튼: 14pt
```

---

## 🔧 **디버깅 가이드**

### **스킬 데이터가 안 보일 때**

1. Play 모드에서 `PlayerSkillManager` 찾기
2. Context Menu: `Test: Migrate to AccountData` 실행
3. Console에서 "데이터 이전 완료!" 확인
4. 스킬북 패널 다시 열기

### **버튼이 안 눌릴 때**

1. SkillListItemUI 프리팹에 Button 컴포넌트 확인
2. OnClick 이벤트: `SkillListItemUI.OnItemClickedFromButton()` 연결
3. UpgradeButton, EquipButton도 개별 확인

---

## 📊 **Phase 3-Revision 체크리스트**

- [x] SkillListItemUI: 우측 리스트용, 레벨업/장착 버튼 포함
- [x] SkillDetailPanel: 하단 상세 정보, 버튼 없음 (읽기 전용)
- [x] SkillEquipSlotUI: 좌측 장착 슬롯 (역할 유지)
- [x] SkillTabController: 레이아웃 재구성 (좌/우/하단)
- [x] SkillBookPanelUI: 최상위 패널, 탭 전환 (WorkshopUI 구조)
- [x] AccountDataManager: 스킬 데이터 통합
- [x] LobbyPanelManager: ShowSkillBookPanel() 추가
- [x] LobbyUIController: OnSkillBookButton() 추가

---

## 🚀 **다음 단계**

1. Unity Editor에서 SkillBookPanel 생성
2. 프리팹 2개 제작 (SkillListItemPrefab, SkillEquipSlotPrefab)
3. 모든 컴포넌트 Inspector 연결
4. Play 모드에서 테스트
5. Phase 4 진행 (전투 시스템 연결)

---

## 🎉 **개선 완료!**

사용자 이미지 기준으로 정확히 구현되었습니다!
