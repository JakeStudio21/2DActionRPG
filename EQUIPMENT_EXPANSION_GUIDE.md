# 🎒 8종 장비 시스템 확장 가이드

## ✅ 완료된 코드 작업

### 1. EquipmentData.cs
- ✅ `EquipmentSlot` enum 수정 완료
  - `Shield` → `Belt` 변경
  - `Gloves` 추가
- ✅ `ArmorType` enum 수정 완료
  - `Gloves`, `Belt` 추가

### 2. PlayerRuntimeStats.cs
- ✅ `ApplyEquipmentStats()` 슬롯별 스탯 적용 확장 완료
  - 무기: 공격력, 공격속도, 크리티컬
  - 투구, 상의: 방어력
  - 장갑: 공격력
  - 신발: 이동속도
  - 반지: 공격력 or 체력
  - 목걸이, 허리띠: 체력

### 3. PlayerDataManager.cs
- ✅ `DetermineEquipmentSlot()` 로직 확장 완료
  - ArmorType 우선 확인으로 정확한 슬롯 결정
  - Gloves, Belt 지원 추가

---

## 📋 Unity Editor 작업 가이드

### **Phase 1: 장비창 UI 수정 (30분)**

#### 1-1. LobbyEquippedItemsUI 수정 (로비 장비창)
**위치:** `Assets/Scripts/UI/Inventory/LobbyEquippedItemsUI.cs`

**현재 상태:**
```csharp
Line 14-22:
[SerializeField] private InventorySlot weaponSlot;
[SerializeField] private InventorySlot armorSlot;
[SerializeField] private InventorySlot bootsSlot;
[SerializeField] private InventorySlot helmetSlot;
[SerializeField] private InventorySlot shieldSlot;      // ⚠️ Belt로 변경 필요
[SerializeField] private InventorySlot ring1Slot;
[SerializeField] private InventorySlot ring2Slot;
[SerializeField] private InventorySlot necklaceSlot;
```

**수정 필요 사항:**
1. **코드 수정 (2분)**
   - Line 19: `shieldSlot` → `glovesSlot`로 변경
   - Line 20 추가: `[SerializeField] private InventorySlot beltSlot;`
   
2. **Unity Hierarchy 작업 (10분)**
   - Hierarchy에서 `Lobby` 씬 열기
   - `Canvas/InventoryPanel/EquippedItemsPanel` 찾기
   - 현재 슬롯 구조:
     ```
     EquippedItemsPanel
     ├─ WeaponSlot
     ├─ ArmorSlot
     ├─ BootsSlot
     ├─ HelmetSlot (비활성화?)
     ├─ ShieldSlot (비활성화?)
     ├─ Ring1Slot (비활성화?)
     ├─ Ring2Slot (비활성화?)
     └─ NecklaceSlot (비활성화?)
     ```
   
3. **슬롯 활성화 및 배치 (15분)**
   - 모든 슬롯 GameObject 활성화
   - `ShieldSlot` 이름 → `GlovesSlot`로 변경 (또는 새로 추가)
   - `BeltSlot` GameObject 추가 (GlovesSlot 복제)
   - Grid Layout Group으로 3x3 또는 2x5 배치
   
4. **Inspector 연결 (3분)**
   - `LobbyEquippedItemsUI` 컴포넌트 선택
   - 각 슬롯을 Inspector 필드에 드래그 앤 드롭
     - Weapon Slot → WeaponSlot
     - Armor Slot → ArmorSlot
     - Gloves Slot → GlovesSlot (신규)
     - Boots Slot → BootsSlot
     - Belt Slot → BeltSlot (신규)
     - Helmet Slot → HelmetSlot
     - Ring1 Slot → Ring1Slot
     - Ring2 Slot → Ring2Slot
     - Necklace Slot → NecklaceSlot

---

#### 1-2. CharacterInfoUI 수정 (캐릭터 정보창)
**위치:** `Assets/Scripts/UI/PlayerUI/CharacterInfoUI.cs`

**현재 상태:**
- LobbyEquippedItemsUI를 참조하므로 자동으로 연동됨
- 별도 수정 불필요!

---

### **Phase 2: 슬롯 UI 레이아웃 디자인 (20분)**

#### 2-1. 추천 레이아웃 (3x3 그리드)
```
┌─────────┬─────────┬─────────┐
│ 투구    │  목걸이 │  무기   │
│ Helmet  │Necklace │ Weapon  │
├─────────┼─────────┼─────────┤
│ 상의    │  반지1  │  장갑   │
│ Armor   │  Ring1  │ Gloves  │
├─────────┼─────────┼─────────┤
│ 신발    │  반지2  │ 허리띠  │
│ Boots   │  Ring2  │  Belt   │
└─────────┴─────────┴─────────┘
```

#### 2-2. 대안 레이아웃 (캐릭터 중심)
```
      투구
    Helmet
      
장갑    무기    목걸이
Gloves Weapon Necklace

상의    반지1   반지2
Armor  Ring1   Ring2

신발           허리띠
Boots          Belt
```

#### 2-3. Grid Layout Group 설정
**EquippedItemsPanel에 추가:**
- Component → Layout → Grid Layout Group
- Cell Size: 80x80 (아이콘 크기)
- Spacing: 10x10
- Start Corner: Upper Left
- Start Axis: Horizontal
- Child Alignment: Middle Center
- Constraint: Fixed Column Count = 3

---

### **Phase 3: 슬롯별 아이콘 설정 (10분)**

#### 3-1. 각 슬롯 GameObject 구조
```
WeaponSlot (InventorySlot 컴포넌트)
├─ Background (Image) - 슬롯 배경
├─ ItemIcon (Image) - 아이템 아이콘
├─ LockIcon (Image, optional) - 잠금 아이콘
└─ SlotLabel (TMP_Text) - "무기" 라벨
```

#### 3-2. 라벨 텍스트 설정
각 슬롯의 `SlotLabel` 텍스트 변경:
- WeaponSlot: "무기"
- HelmetSlot: "투구"
- ArmorSlot: "상의"
- GlovesSlot: "장갑" (신규)
- BootsSlot: "신발"
- BeltSlot: "허리띠" (신규)
- Ring1Slot: "반지1"
- Ring2Slot: "반지2"
- NecklaceSlot: "목걸이"

---

### **Phase 4: EquipmentData ScriptableObject 생성 (Editor 외부 작업)**

#### 4-1. 새 장비 데이터 생성
**Unity Editor:**
1. Project 창에서 우클릭
2. Create → Equipment → EquipmentData
3. 파일명 예시:
   - `Gloves_Iron.asset` (장갑 - 철)
   - `Gloves_Steel.asset` (장갑 - 강철)
   - `Belt_Leather.asset` (허리띠 - 가죽)
   - `Belt_Steel.asset` (허리띠 - 강철)

#### 4-2. Inspector 설정 예시 (장갑)
```
Equipment Name: 철 장갑
Item ID: ITEM_GLOVES_IRON_001
Equipment Type: Armor
Armor Type: Gloves ⭐ 중요!
Item Grade: C
Usable Class: None (모든 클래스)
Is Tradable: true
Required Level: 1

=== 무기 전투 스탯 ===
Attack Damage: 5 ⭐ 장갑은 공격력 제공
Attack Speed: 1
Attack Range: 1

=== 방어구 전용 스탯 ===
Defense Bonus: 0
Speed Bonus: 0
Health Bonus: 0

=== 상점 시스템 ===
Buy Price: 150
Sell Price: 75

=== 드롭/픽업 연결 ===
Pickup Prefab: Drop_Equipment ⭐ 공용 프리팹
Icon: Gloves_Icon (Sprite)
```

#### 4-3. Inspector 설정 예시 (허리띠)
```
Equipment Name: 가죽 허리띠
Item ID: ITEM_BELT_LEATHER_001
Equipment Type: Armor
Armor Type: Belt ⭐ 중요!
Item Grade: C
Usable Class: None

=== 무기 전투 스탯 ===
Attack Damage: 0
Attack Speed: 1
Attack Range: 1

=== 방어구 전용 스탯 ===
Defense Bonus: 0
Speed Bonus: 0
Health Bonus: 50 ⭐ 허리띠는 체력 제공

=== 상점 시스템 ===
Buy Price: 200
Sell Price: 100

=== 드롭/픽업 연결 ===
Pickup Prefab: Drop_Equipment
Icon: Belt_Icon (Sprite)
```

---

### **Phase 5: 테스트 체크리스트**

#### 5-1. 로비 인벤토리 테스트
- [ ] 9개 슬롯 모두 표시되는가?
- [ ] 장갑 착용 시 공격력 증가하는가?
- [ ] 허리띠 착용 시 체력 증가하는가?
- [ ] 슬롯 클릭 시 장착/해제 정상 작동하는가?

#### 5-2. 캐릭터 정보창 테스트
- [ ] 9개 슬롯 정보 정상 표시되는가?
- [ ] 스탯 계산 정확한가?
- [ ] 장비 교체 시 실시간 반영되는가?

#### 5-3. 인게임 테스트
- [ ] 장비 드롭 시 정상 드롭되는가?
- [ ] 픽업 시 인벤토리에 추가되는가?
- [ ] 스탯 실시간 적용되는가?

---

## 🎨 권장 아이콘 사이즈

### UI 아이콘
- 슬롯 배경: 100x100px
- 아이템 아이콘: 80x80px
- 라벨 폰트: 14pt

### 드롭 아이콘
- EquipmentData.icon: 64x64px ~ 128x128px
- PNG 투명 배경
- 등급별 테두리 색상 적용 (EquipmentPickup에서 자동)

---

## ⚠️ 주의사항

### 1. ArmorType 필수 설정
- EquipmentData 생성 시 **반드시** `ArmorType` 설정!
- 설정 안 하면 `DetermineEquipmentSlot()`에서 이름 기반 Fallback 사용
- 정확한 슬롯 결정을 위해 ArmorType 필수

### 2. 프리팹 불필요
- 방어구/악세서리는 GameObject 프리팹 생성 불필요
- EquipmentData ScriptableObject만 생성
- 시각적 표현 없음 (아이콘만 표시)

### 3. 스탯 적용 규칙
| 슬롯 | 주 스탯 | 필드명 |
|------|---------|--------|
| 무기 | 공격력, 공속, 크리 | attackDamage, attackSpeed, criticalChance |
| 투구 | 방어력 | defenseBonus |
| 상의 | 방어력 | defenseBonus |
| 장갑 | 공격력 | attackDamage |
| 신발 | 이동속도 | speedBonus |
| 반지 | 공격력 or 체력 | attackDamage, healthBonus |
| 목걸이 | 체력 | healthBonus |
| 허리띠 | 체력 | healthBonus |

---

## 📊 예상 작업 시간

| 작업 | 예상 시간 |
|------|-----------|
| 코드 수정 (LobbyEquippedItemsUI) | 2분 |
| Hierarchy 슬롯 추가/배치 | 15분 |
| Inspector 연결 | 3분 |
| 레이아웃 조정 | 10분 |
| 라벨/아이콘 설정 | 10분 |
| EquipmentData 생성 (5개) | 15분 |
| 테스트 | 20분 |
| **합계** | **1시간 15분** |

---

## ✅ 완료 후 확인사항

1. ✅ 9개 슬롯 모두 표시
2. ✅ 장비 착용 시 스탯 정상 적용
3. ✅ 장비 교체 정상 작동
4. ✅ 캐릭터 정보창 정상 표시
5. ✅ 드롭/픽업 정상 작동
6. ✅ 저장/로드 정상 작동

---

## 🚀 다음 단계 (선택 사항)

### 향후 확장 가능 항목
1. **세트 효과 시스템**
   - 같은 시리즈 장비 착용 시 보너스
   
2. **장비 강화 시스템**
   - +1, +2, +3 등급 강화
   
3. **장비 소켓 시스템**
   - 보석 장착으로 추가 스탯
   
4. **장비 외형 시스템 (비추천)**
   - 플레이어 스프라이트 변경
   - 작업량 방대 (10~20시간)

---

## 📞 문제 발생 시 체크포인트

### 컴파일 에러
- `EquipmentSlot.Shield` 참조 에러 → `EquipmentSlot.Belt`로 변경
- 기존 코드에서 Shield 사용하는 곳 검색 필요

### 슬롯 미표시
- Inspector에서 slotXXX 필드 제대로 연결되었는지 확인
- GameObject 활성화 상태 확인

### 스탯 미적용
- EquipmentData의 ArmorType 설정 확인
- PlayerRuntimeStats 로그 확인 (showDebugLogs = true)

---

작성일: 2026-01-28  
버전: 1.0  
작성자: AI Assistant

