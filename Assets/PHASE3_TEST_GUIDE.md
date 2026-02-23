# 🧪 Phase 3 장비 시스템 Unity Editor 테스트 가이드

## 📌 테스트 목적
- ItemDatabase 정적 캐싱 시스템 검증
- EquipmentInstance 런타임 인스턴스 생성 확인
- StatModifier 생성 및 적용 검증
- EquipmentManager 장착/해제 로직 검증
- 강화 레벨별 스탯 스케일링 확인
- Grade 6+ 귀속 시스템 검증
- PlayerRuntimeStats 연동 확인

---

## 🔧 Step 1: Unity Editor 설정

### **1-1. 테스트 오브젝트 생성**

1. **Hierarchy**에서 빈 GameObject 생성:
   - 우클릭 → `Create Empty`
   - 이름: `Phase3_TestManager`

2. **EquipmentManager 컴포넌트 추가**:
   - `Phase3_TestManager` 선택
   - Inspector → `Add Component`
   - `EquipmentManager` 검색 및 추가

3. **Phase3_EquipmentSystemTest 컴포넌트 추가**:
   - 같은 오브젝트에 `Add Component`
   - `Phase3_EquipmentSystemTest` 검색 및 추가

---

### **1-2. PlayerRuntimeStats 연결**

#### **방법 A: 기존 플레이어 오브젝트 사용** (권장)
1. Hierarchy에서 플레이어 오브젝트 찾기 (예: `Player_Assasin`, `Player_Warrior`)
2. `Phase3_EquipmentSystemTest` Inspector에서:
   - `Player Runtime Stats` 필드에 플레이어 오브젝트 드래그

#### **방법 B: 새로 생성**
1. Hierarchy에서 빈 GameObject 생성
2. `PlayerRuntimeStats` 컴포넌트 추가
3. `Phase3_EquipmentSystemTest`에 연결

---

### **1-3. 테스트용 장비 데이터 할당** (선택 사항)

Inspector의 **"테스트용 장비 데이터"** 섹션:
- `Test Weapon`: Resources/Data/EquipmentData/ 폴더에서 무기 드래그
- `Test Armor`: 방어구 드래그
- `Test Accessory`: 악세서리 드래그

> 💡 **Note**: 할당하지 않으면 ItemDatabase에서 자동으로 첫 번째 장비 사용

---

## 🚀 Step 2: 테스트 실행

### **방법 1: Context Menu (우클릭 메뉴)** ⭐ 권장

1. `Phase3_EquipmentSystemTest` 컴포넌트의 **우측 ⋮ 메뉴** 클릭
2. 원하는 테스트 선택:

```
🧪 Run All Tests          - 전체 테스트 실행
Test 1: ItemDatabase 초기화
Test 2: EquipmentInstance 생성
Test 3: StatModifier 생성
Test 4: 장비 착용
Test 5: 강화 스케일링
Test 6: 장비 해제
Test 7: 귀속 시스템
📊 현재 스탯 출력
🎒 장착 장비 목록
```

---

### **방법 2: Play Mode 자동 실행**

1. Inspector에서 `Auto Run On Start` 체크
2. Unity 재생 버튼 (▶️) 클릭
3. Console 창에서 결과 확인

---

## 📊 Step 3: 결과 확인

### **Console 창 확인 사항**

#### ✅ **성공 케이스**
```
=================================================
🧪 Phase 3 장비 시스템 통합 테스트 시작
=================================================

✅ ItemDatabase 초기화 완료 - 12개 장비 데이터

--- Test 1: ItemDatabase 초기화 ---
총 12개 EquipmentData 로드됨
  - Sword_S_Equipment (Grade: Grade8, Type: Weapon)
  - Sword_A_Equipment (Grade: Grade7, Type: Weapon)
  ...
✅ Test 1 완료

--- Test 2: EquipmentInstance 생성 ---
생성된 인스턴스: Sword_S_Equipment+0 (ITEM_12345...)
  - instanceId: ITEM_12345...
  - equipmentDataName: Sword_S_Equipment
  - enhanceLevel: 0
  - isBound: False
✅ Test 2 완료

--- Test 3: StatModifier 생성 ---
Sword_S_Equipment의 StatModifier 목록 (총 3개):
  - ATK_FLAT: 50 (Flat) | Source: Sword_S_Equipment+0
  - ASPD: 1.2 (Percent) | Source: Sword_S_Equipment
  - CRIT_RATE: 0.15 (Percent) | Source: Sword_S_Equipment
✅ Test 3 완료

--- Test 4: 장비 착용 및 스탯 적용 ---
초기 스탯:
  - 공격력: 20.0
  - 방어력: 10.0
  - 치명확률: 0.05

[EquipmentManager] Sword_S_Equipment+0 (ID: ITEM_...) 장착됨! 슬롯: MainWeapon

장비 착용 후 스탯:
  - 공격력: 70.0 (변화: +50.0)  ✅
  - 방어력: 10.0 (변화: +0.0)
  - 치명확률: 0.20 (변화: +0.15)  ✅
✅ Test 4 완료

--- Test 5: 강화 레벨별 스탯 스케일링 ---
장비: Sword_S_Equipment
기본 공격력: 50

강화 레벨별 스탯:
  +0강: 공격력 50.0 (예상: 50.0)   ✅
  +2강: 공격력 60.0 (예상: 60.0)   ✅
  +4강: 공격력 70.0 (예상: 70.0)   ✅
  +6강: 공격력 80.0 (예상: 80.0)   ✅
  +8강: 공격력 90.0 (예상: 90.0)   ✅
  +10강: 공격력 100.0 (예상: 100.0) ✅
✅ Test 5 완료

--- Test 6: 장비 해제 ---
[EquipmentManager] Sword_S_Equipment+0 (ID: ITEM_...) 해제됨! 슬롯: MainWeapon

장비 해제 완료
  - 해제 전 공격력: 70.0
  - 해제 후 공격력: 20.0  ✅
  - 변화: -50.0
✅ Test 6 완료

--- Test 7: 귀속 시스템 테스트 ---
테스트 장비: Sword_S_Equipment (Grade: Grade8)
초기 귀속 상태: False

[EquipmentManager] Sword_S_Equipment 귀속됨! (Grade: Grade8)

장착 후 귀속 상태: True  ✅
✅ 귀속 시스템 정상 작동!
✅ Test 7 완료

=================================================
✅ Phase 3 장비 시스템 통합 테스트 완료!
=================================================
```

---

#### ❌ **실패 케이스 및 해결 방법**

| 에러 메시지 | 원인 | 해결 방법 |
|-------------|------|-----------|
| `❌ EquipmentManager를 찾을 수 없습니다!` | EquipmentManager 누락 | Step 1-1 재확인 |
| `❌ PlayerRuntimeStats를 찾을 수 없습니다!` | PlayerRuntimeStats 누락 | Step 1-2 재확인 |
| `⚠️ ItemDatabase에 장비 데이터가 없습니다.` | Resources 폴더에 SO 없음 | `Resources/Data/EquipmentData/` 생성 및 SO 추가 |
| `⚠️ 테스트용 장비 데이터를 찾을 수 없습니다.` | 장비 SO 없음 | 장비 ScriptableObject 생성 |
| `장비 착용 후 스탯 변화 없음` | PlayerRuntimeStats 재계산 안 됨 | `RecalculateAllStats()` 호출 확인 |
| `강화 스케일링 안 됨` | CalculateEnhancedValue() 버그 | EquipmentInstance 코드 확인 |

---

## 🔍 Step 4: 추가 디버깅

### **A. 현재 스탯 실시간 확인**
```
Phase3_EquipmentSystemTest 우클릭 → 📊 현재 스탯 출력
```

### **B. 장착 장비 목록 확인**
```
Phase3_EquipmentSystemTest 우클릭 → 🎒 장착 장비 목록
```

### **C. ItemDatabase 캐시 확인**
Console 창에서 자동으로 출력됨 (게임 시작 시):
```
[ItemDatabase] 초기화 완료 - 12개 장비 데이터
=== ItemDatabase 캐시 정보 ===
총 12개 EquipmentData:
  - Sword_S_Equipment: 신성한 검 (Grade: Grade8)
  - Sword_A_Equipment: 강철 검 (Grade: Grade7)
  ...
```

---

## 🎯 Step 5: 성공 기준

### **필수 검증 항목**

- [ ] **Test 1**: ItemDatabase가 EquipmentData를 성공적으로 로드
- [ ] **Test 2**: EquipmentInstance가 정상 생성됨
- [ ] **Test 3**: StatModifier 리스트가 정상 생성됨
- [ ] **Test 4**: 장비 착용 시 PlayerRuntimeStats의 스탯이 증가
- [ ] **Test 5**: 강화 레벨에 따라 스탯이 정확히 스케일링 (레벨당 +10%)
- [ ] **Test 6**: 장비 해제 시 스탯이 원래대로 복구
- [ ] **Test 7**: Grade 6 이상 장비가 착용 시 자동으로 귀속됨

### **성공 시 예상 결과**

✅ **모든 테스트 통과**
- ItemDatabase: 자동 초기화, 전역 캐싱
- EquipmentInstance: 정상 생성 및 로딩
- StatModifier: 정확한 스탯 변환
- EquipmentManager: 장착/해제 정상 작동
- PlayerRuntimeStats: 실시간 스탯 반영
- 강화 시스템: 화이트리스트 스탯만 10% 증가
- 귀속 시스템: Grade 6+ 자동 귀속

---

## 🐛 트러블슈팅

### **Q1: "ItemDatabase에 장비 데이터가 없습니다" 에러**

**해결 방법:**
1. `Resources/Data/EquipmentData/` 폴더가 존재하는지 확인
2. 해당 폴더에 EquipmentData ScriptableObject가 있는지 확인
3. 없다면 대체 경로: `Resources/Data/` 폴더도 검색됨

**ScriptableObject 생성 방법:**
```
Project 창 우클릭
→ Create → Equipment → EquipmentData
→ 이름: "Sword_S_Equipment"
→ Inspector에서 필드 설정
```

---

### **Q2: "PlayerRuntimeStats의 스탯이 변하지 않습니다"**

**원인:**
- `ApplyStatModifiers()`가 호출되지 않음
- `RecalculateAllStats()`가 호출되지 않음

**해결 방법:**
1. `EquipmentManager.EquipItem()` 내부에서 `RecalculateAllStats()` 호출 확인
2. Console에서 `[PlayerRuntimeStats] RecalculateAllStats()` 로그 확인
3. `AddStatModifier()` 로그 확인

---

### **Q3: "강화 스케일링이 작동하지 않습니다"**

**원인:**
- `CalculateEnhancedValue()` 화이트리스트에 해당 스탯이 없음

**해결 방법:**
1. `EquipmentInstance.cs` 확인:
```csharp
// 화이트리스트: ATK_FLAT, DEF_FLAT, HP_FLAT만
if (statType == EStatType.ATK_FLAT || 
    statType == EStatType.DEF_FLAT || 
    statType == EStatType.HP_FLAT)
{
    return baseValue * (1f + level * 0.1f);
}
```
2. 크리티컬, 공속 등은 강화 영향 없음 (의도된 동작)

---

### **Q4: "귀속이 되지 않습니다"**

**원인:**
- 테스트 장비의 `itemGrade`가 6 미만

**해결 방법:**
1. Inspector에서 테스트 장비의 `Item Grade`를 `Grade6` 이상으로 설정
2. 또는 Test 7에서 자동으로 Grade 7로 처리됨

---

## 📝 테스트 체크리스트

### **사전 준비**
- [ ] EquipmentManager 컴포넌트 추가됨
- [ ] PlayerRuntimeStats 컴포넌트 연결됨
- [ ] Resources/Data/EquipmentData/ 폴더에 장비 SO 존재

### **테스트 실행**
- [ ] "🧪 Run All Tests" 실행
- [ ] Console에 에러 없음
- [ ] 모든 테스트에 "✅" 표시

### **결과 검증**
- [ ] 장비 착용 시 공격력 증가 확인
- [ ] 강화 +10일 때 기본 공격력 × 2.0 확인
- [ ] 장비 해제 시 스탯 원래대로 복구
- [ ] Grade 6+ 장비 귀속 확인

---

## 🎉 성공 시 다음 단계

Phase 3 완료 후 가능한 확장:

### **Option 1: 슬롯 호환성 체크 구현**
- `EquipmentManager.EquipItem()` 슬롯 검증 로직 추가
- `ArmorType`, `AccessoryType` 기반 자동 슬롯 매칭

### **Option 2: 귀속 시스템 UI 연동**
- `OnItemBound` 이벤트 구독
- 팝업 알림: "아이템이 귀속되었습니다!"

### **Option 3: 강화 시스템 UI**
- 강화 버튼 추가
- `EquipmentInstance.enhanceLevel++` 증가
- 실시간 스탯 변화 표시

---

## 📚 참고 문서

- `COMBAT_FORMULA_DESIGN.md` - 전투 공식 전체 설계
- `StatDefinition.csv` - 스탯 정의 및 적용 순서
- `StatSourceMapping.csv` - 스탯 소스 매핑
- `ConditionalModifier.csv` - 조건부 모디파이어

---

**작성일**: 2026-02-19  
**버전**: Phase 3.0  
**작성자**: AI Assistant

