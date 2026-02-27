# 📚 스킬북 패널 상호작용 흐름

## Phase 3-Revision: 사용자 이미지 기준 UI 구조

---

## 🎯 **핵심 변경 사항**

### **Before (Phase 3 초기안)**
- LeftPanel: 스킬 리스트
- RightPanel: 상세 정보 + 버튼 (레벨업, 장착)
- BottomPanel: 장착 슬롯

### **After (Phase 3-Revision, 사용자 요청)**
- LeftPanel: **장착 슬롯** (액티브 2개, 패시브 3개)
- RightPanel: **스킬 리스트** (각 아이템에 레벨업/장착 버튼)
- BottomPanel: **상세 정보** (버튼 없음, 읽기 전용)

---

## 📋 **전체 상호작용 흐름**

### **1. 패널 진입**

```
사용자: "스킬&룬" 버튼 클릭
  ↓
LobbyUIController.OnSkillBookButton()
  ↓
LobbyPanelManager.ShowSkillBookPanel()
  ↓
SkillBookPanelUI.OnPanelOpened()
  ↓
SkillTabController.OnTabActivated()
  ↓
RefreshUI() 실행:
  - UpdateSPDisplay() → 상단 SP/레벨 표시
  - RefreshSkillList() → 우측 스킬 리스트 생성
  - RefreshEquipSlots() → 좌측 장착 슬롯 갱신
```

---

## 🎮 **2. 스킬 선택 (우측 리스트)**

### **사용자 행동:**
우측 스킬 리스트에서 "정령의 공명" 아이템 클릭

### **시스템 반응:**

```
SkillListItemUI.OnItemClicked()
  ↓
SkillTabController.ShowSkillDetail(skill)
  ↓
SkillDetailPanel.ShowSkillDetail(skill, playerLevel)
  ↓
하단 패널에 표시:
  - 스킬 이름: "정령의 공명"
  - 스킬 설명: "전투 중 정령의 힘을 빌려..."
  - 스킬 타입: "[패시브] Combat"
  - 현재 레벨: Lv.1
  - 최대 레벨: Lv.5
  - 필요 SP: 2
  - 해금 레벨: ✅ Lv.1
  - 현재 스탯: HP +0.5%
  - 다음 스탯: HP +1.0%
```

**동시에:**
```
SkillTabController.SetSelectedSkillItem(this)
  ↓
기존 선택 해제 (배경색 복구)
  ↓
새 선택 설정 (배경색 변경: 연한 파란색)
```

---

## ⬆️ **3. 스킬 레벨업 (우측 리스트)**

### **사용자 행동:**
"정령의 공명" 아이템의 **[레벨업]** 버튼 클릭

### **시스템 반응:**

```
SkillListItemUI.OnUpgradeButtonClicked()
  ↓
SkillTabController.TryUpgradeSkill(skill)
  ↓
AccountDataManager.TryUpgradeSkill(skill, playerLevel)
  ↓
검증 단계:
  1. 해금 조건: playerLevel >= skill.unlockLevel ✅
  2. 만렙 조건: skill.currentLevel < maxLevel ✅
  3. SP 조건: availableSP >= requiredSP ✅
  ↓
레벨업 실행:
  - skill.currentLevel: 1 → 2
  - accountData.usedSP: 0 → 2
  - 저장 데이터 업데이트
  - Save()
  ↓
SkillTabController.RefreshUI()
  ↓
UI 갱신:
  - 상단 SP: "10/10" → "8/10"
  - 우측 리스트 아이템: "Lv.1/5" → "Lv.2/5"
  - [레벨업] 버튼 텍스트 갱신
  ↓
하단 상세 패널 갱신 (선택된 스킬인 경우):
  - SkillTabController.ShowSkillDetail(skill)
  - 현재 레벨: "Lv.2"
  - 필요 SP: 2 → 3
  - 현재 스탯: HP +1.0%
  - 다음 스탯: HP +1.5%
```

### **버튼 상태 변경 (UpdateButtonStates):**

```
레벨업 버튼:
  - 조건: !skill.IsMaxLevel && !isLocked && CanAffordSP(requiredSP)
  - 텍스트:
    - skill.IsMaxLevel → "만렙"
    - isLocked → "Lv.X"
    - !canAfford → "SP부족"
    - else → "레벨업"
```

---

## 🎯 **4. 스킬 장착 (우측 리스트)**

### **사용자 행동:**
"정령의 공명" 아이템의 **[장착]** 버튼 클릭

### **시스템 반응:**

```
SkillListItemUI.OnEquipButtonClicked()
  ↓
SkillTabController.EquipSkill(skill)
  ↓
분기: skill.IsPassiveSkill
  ↓
SkillTabController.EquipPassiveSkill(skill)
  ↓
AccountDataManager.FindEmptyPassiveSlot()
  - 빈 슬롯 찾기: 슬롯 0
  ↓
AccountDataManager.EquipPassiveSkill(skill, 0)
  ↓
장착 처리:
  - equippedPassiveSkillIds[0] = "passive_spirit_resonance"
  - skill.isEquipped = true
  - UpdateSkillSaveData(skill)
  - Save()
  ↓
SkillTabController.RefreshUI()
  ↓
UI 갱신:
  - 좌측 [패시브 슬롯 1]: "정령의 공명" 아이콘 표시
  - 우측 리스트 아이템:
    - 배경색: 황금색 (장착 상태)
    - [장착] 버튼 → [장착중] (비활성화)
```

### **장착 슬롯이 가득 찬 경우:**

```
AccountDataManager.FindEmptyPassiveSlot() → -1
  ↓
emptySlotIndex = 0 (슬롯 0에 덮어씌우기)
  ↓
기존 스킬 해제:
  - oldSkill = GetEquippedPassiveSkill(0)
  - UnequipSkillByID(oldSkill.skillID)
  ↓
새 스킬 장착:
  - equippedPassiveSkillIds[0] = "passive_spirit_resonance"
  - skill.isEquipped = true
  ↓
RefreshUI()
```

---

## 🔓 **5. 스킬 장착 해제 (좌측 슬롯)**

### **사용자 행동:**
좌측 [패시브 슬롯 1] **우클릭**

### **시스템 반응:**

```
SkillEquipSlotUI.OnPointerClick(eventData)
  ↓
eventData.button == PointerEventData.InputButton.Right
  ↓
SkillTabController.UnequipSkill(equippedSkill)
  ↓
AccountDataManager.UnequipSkill(skill)
  ↓
해제 처리:
  - skill.isEquipped = false
  - equippedPassiveSkillIds[0] = null
  - UpdateSkillSaveData(skill)
  - Save()
  ↓
SkillTabController.RefreshUI()
  ↓
UI 갱신:
  - 좌측 [패시브 슬롯 1]: "빈 슬롯" 표시
  - 우측 리스트 아이템:
    - 배경색: 흰색 (해제 상태)
    - [장착중] 버튼 → [장착] (활성화)
```

---

## 📖 **6. 스킬 상세 보기 (좌측 슬롯)**

### **사용자 행동:**
좌측 [패시브 슬롯 1] **좌클릭**

### **시스템 반응:**

```
SkillEquipSlotUI.OnPointerClick(eventData)
  ↓
eventData.button == PointerEventData.InputButton.Left
  ↓
SkillTabController.ShowSkillDetail(equippedSkill)
  ↓
하단 패널에 장착된 스킬 상세 정보 표시
```

---

## 🔄 **7. 탭 전환 (스킬 ↔ 룬)**

### **사용자 행동:**
상단 **[룬]** 탭 버튼 클릭

### **시스템 반응:**

```
SkillBookPanelUI.SwitchTab(SkillBookTabType.Rune)
  ↓
DeactivateAllSubPanels()
  - skillSubPanel.SetActive(false)
  - runeSubPanel.SetActive(false)
  ↓
runeSubPanel.SetActive(true)
  ↓
UpdateTabButtonStates()
  - [스킬] 탭: 반투명 (alpha 0.5)
  - [룬] 탭: 불투명 (alpha 1.0)
  ↓
OnTabChanged 이벤트 발행
```

### **스킬 탭 비활성화:**

```
SkillTabController.OnTabDeactivated()
  ↓
정리 작업:
  - currentSelectedItem.SetSelected(false)
  - skillDetailPanel.Hide()
```

---

## 🚪 **8. 패널 닫기**

### **사용자 행동:**
상단 **[X]** 버튼 클릭

### **시스템 반응:**

```
SkillBookPanelUI.OnCloseButtonClicked()
  ↓
LobbyUIController.OnBackToLobby()
  ↓
LobbyPanelManager.ShowLobbyPanel()
  ↓
BringPanelToFront(lobbyPanel)
  ↓
SkillBookPanelUI.OnPanelClosed()
  ↓
SkillTabController.OnTabDeactivated()
```

---

## 🔍 **9. 데이터 흐름 요약**

### **AccountDataManager → UI**

```
AccountDataManager (데이터 소스)
  ↓
GetUnlockedActiveSkills()
GetUnlockedPassiveSkills()
  ↓
SkillTabController.RefreshSkillList()
  ↓
SkillListItemUI.Setup(skill, playerLevel, controller)
  ↓
화면 표시
```

### **UI → AccountDataManager**

```
사용자 행동 (클릭)
  ↓
SkillListItemUI.OnUpgradeButtonClicked()
  ↓
SkillTabController.TryUpgradeSkill(skill)
  ↓
AccountDataManager.TryUpgradeSkill(skill, playerLevel)
  ↓
accountData 수정 (currentLevel++, usedSP++)
  ↓
Save() → JSON 저장
  ↓
RefreshUI() → 화면 갱신
```

---

## ⚡ **10. 실시간 상태 업데이트**

### **버튼 상태 결정 로직 (SkillListItemUI.UpdateButtonStates)**

```csharp
// 레벨업 버튼
bool canUpgrade = !skill.IsMaxLevel 
                  && !isLocked 
                  && tabController.CanAffordSP(requiredSP);
upgradeButton.interactable = canUpgrade;

// 장착 버튼
bool canEquip = skill.IsUnlocked && !isLocked;
equipButton.interactable = canEquip;

// 버튼 텍스트
if (skill.IsMaxLevel)
    upgradeButtonText.text = "만렙";
else if (isLocked)
    upgradeButtonText.text = $"Lv.{skill.unlockLevel}";
else if (!canUpgrade)
    upgradeButtonText.text = "SP부족";
else
    upgradeButtonText.text = "레벨업";

if (skill.isEquipped)
    equipButtonText.text = "장착중";
else if (!skill.IsUnlocked)
    equipButtonText.text = "미해금";
else
    equipButtonText.text = "장착";
```

---

## 🔄 **11. 인게임 동기화 (PlayerSkillManager)**

### **Phase 4 이후: 전투 진입 시**

```
전투 씬 로드
  ↓
PlayerSkillManager.Start()
  ↓
SyncFromAccountData()
  ↓
AccountDataManager에서 데이터 복원:
  - GetUnlockedActiveSkills()
  - GetUnlockedPassiveSkills()
  - GetEquippedActiveSkill(i)
  - GetEquippedPassiveSkill(i)
  ↓
PlayerSkillManager.unlockedActiveSkills = ...
PlayerSkillManager.unlockedPassiveSkills = ...
  ↓
ApplyPassiveSkillsToStats()
  ↓
PlayerRuntimeStats.RecalculateStats()
```

---

## 🎯 **12. 주요 클래스 책임 요약**

### **SkillBookPanelUI**
- **책임**: 최상위 패널 관리, 탭 전환
- **핵심 메서드**:
  - `SwitchTab(SkillBookTabType tab)` → 서브 패널 전환
  - `OnPanelOpened()` → 초기 탭 설정
  - `OnCloseButtonClicked()` → 로비로 복귀

### **SkillTabController**
- **책임**: 스킬 탭 내부 3개 영역 (좌/우/하단) 관리
- **핵심 메서드**:
  - `RefreshUI()` → 전체 UI 갱신
  - `TryUpgradeSkill(skill)` → 레벨업 처리
  - `EquipSkill(skill)` → 장착 처리
  - `ShowSkillDetail(skill)` → 하단 상세 패널 표시

### **SkillListItemUI**
- **책임**: 우측 스킬 리스트 개별 아이템
- **핵심 메서드**:
  - `UpdateUI()` → 아이콘, 레벨, 버튼 상태 갱신
  - `OnUpgradeButtonClicked()` → 레벨업 요청
  - `OnEquipButtonClicked()` → 장착 요청
  - `OnItemClicked()` → 선택 → 하단 상세 표시

### **SkillDetailPanel**
- **책임**: 하단 상세 정보 표시 (읽기 전용)
- **핵심 메서드**:
  - `ShowSkillDetail(skill, playerLevel)` → 정보 표시
  - `UpdateLevelInfo(skill, playerLevel)` → 레벨 정보
  - `UpdateStatsInfo(skill)` → 스탯 비교 (현재/다음)

### **SkillEquipSlotUI**
- **책임**: 좌측 장착 슬롯 (5개)
- **핵심 메서드**:
  - `SetSkill(SkillInstance skill)` → 장착된 스킬 표시
  - `OnPointerClick(eventData)` → 좌클릭(상세), 우클릭(해제)

### **AccountDataManager**
- **책임**: 스킬 데이터 영구 저장/로드
- **핵심 메서드**:
  - `TryUpgradeSkill(skill, playerLevel)` → 레벨업 + 저장
  - `EquipActiveSkill(skill, slotIndex)` → 액티브 장착 + 저장
  - `EquipPassiveSkill(skill, slotIndex)` → 패시브 장착 + 저장
  - `GetUnlockedActiveSkills()` → 보유 액티브 스킬 목록
  - `GetUnlockedPassiveSkills()` → 보유 패시브 스킬 목록

---

## 💡 **13. 사용자 경험 (UX) 흐름**

```
[로비 화면]
  ↓ "스킬&룬" 버튼 클릭
[스킬북 패널 열림]
  - 좌측: 장착된 스킬 (2 액티브, 3 패시브)
  - 우측: 보유 스킬 리스트 (스크롤)
  - 하단: 비어있음
  ↓ 우측 "정령의 공명" 클릭
[하단에 상세 정보 표시]
  - 이름: "정령의 공명"
  - 설명: "전투 중 정령의 힘을..."
  - 현재: HP +0.5%
  - 다음: HP +1.0%
  - 필요 SP: 2
  ↓ [레벨업] 버튼 클릭
[SP 차감, 레벨업]
  - SP: 10/10 → 8/10
  - 레벨: Lv.1 → Lv.2
  - 스탯: HP +0.5% → HP +1.0%
  ↓ [장착] 버튼 클릭
[좌측 슬롯에 아이콘 표시]
  - [패시브 슬롯 1]: "정령의 공명" 표시
  - [장착] → [장착중] (비활성화)
  ↓ [X] 버튼 클릭
[로비로 복귀]
```

---

## ✅ **Phase 3-Revision 완료!**

사용자 요청사항:
- ✅ 좌측: 장착 슬롯
- ✅ 우측: 스킬 리스트 + 레벨업/장착 버튼
- ✅ 하단: 상세 정보 (버튼 없음)
- ✅ 최상위: 스킬&룬 패널 (WorkshopUI 구조)
- ✅ AccountDataManager 통합 (로비-인게임 브릿지)

**다음 단계 (Phase 4):**
- 전투 진입 시 스킬 데이터 동기화
- 액티브 스킬 실제 사용 (마우스 우클릭)
- 패시브 스킬 실시간 스탯 반영
