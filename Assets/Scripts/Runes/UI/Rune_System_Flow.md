# 룬 시스템 현재 동작 흐름

## 📱 전체 UI 구조

```
Lobby 씬
└── Canvas
    ├── RuneInventoryPanel (인벤토리)
    ├── RuneEquipSlot[0/1/2] (장착 슬롯 3개)
    ├── RuneEnhancePanel (강화 UI)
    └── RuneTooltipPanel (툴팁)
```

---

## 🔄 주요 동작 흐름

### 1️⃣ 룬 확인 및 선택
```
플레이어
  → 룬 UI 버튼 클릭 (또는 RuneInventoryPanel 활성화)
  → RuneInventoryPanel 표시
  → 보유 룬 목록 (4x5 그리드, 스크롤 가능)
  → 룬 슬롯 마우스 오버
  → RuneTooltipPanel 표시 (0.5초 후)
```

### 2️⃣ 룬 장착 (드래그 앤 드롭)
```
인벤토리 룬 슬롯 클릭 & 드래그
  → 원본 슬롯 반투명 처리
  → 임시 아이콘 생성 (마우스 커서 따라다님)
  → RuneEquipSlot[0/1/2] 위로 드롭
  → RuneManager.EquipRune() 호출
  → 성공: 장착 슬롯에 룬 표시
  → 실패: 에러 메시지 2초간 표시
```

### 3️⃣ 룬 강화 - 레벨업
```
인벤토리 또는 장착 슬롯의 룬 클릭
  → RuneEnhancePanel 활성화
  → 레벨업 탭 (기본 선택됨)
  → 현재 스탯 / 다음 스탯 표시
  → [강화] 버튼 클릭
  → RuneEnhanceManager.TryLevelUp() 호출
  → 레벨 증가 (Lv.N → Lv.N+1)
  → Lv.3/6/9: 부옵션 자동 추첨
  → 모든 UI 갱신
```

### 4️⃣ 룬 강화 - 한계돌파
```
최대 레벨 룬 선택 (Lv.10)
  → RuneEnhancePanel > 한계돌파 탭 클릭
  → 재료 목록 표시 (동일 runeId, Lv.1 필터링)
  → 재료 룬 클릭 (선택 표시)
  → [한계돌파] 버튼 클릭
  → RuneEnhanceManager.TryLimitBreak() 호출
  → 한계돌파 +1 (최대 레벨 10→11)
  → 재료 룬 소모 (인벤토리에서 삭제)
  → 장착 슬롯 별 1개 활성화
  → 모든 UI 갱신
```

### 5️⃣ 룬 장착 해제
```
방법 1: 우클릭
  RuneEquipSlot 우클릭
    → RuneManager.UnequipRune() 호출
    → 슬롯 비움
    → 인벤토리에 룬 복귀

방법 2: 더블클릭
  RuneEquipSlot 더블클릭 (0.3초 이내 2회)
    → 동일하게 해제
```

---

## 🎯 핵심 매니저 역할

```
RuneInventoryManager
  - 보유 룬 목록 관리 (추가/삭제)
  
RuneManager
  - 장착/해제 처리
  - 조건부 모디파이어 수집 및 PlayerRuntimeStats 전달
  
RuneEnhanceManager
  - 레벨업 처리
  - 한계돌파 처리
  - 부옵션 추첨
```

---

## 🖼️ UI 컴포넌트 역할

```
RuneInventoryUI
  - 인벤토리 그리드 표시
  - 룬 슬롯 생성/삭제
  - 정렬/필터 (미구현)

RuneSlotUI
  - 개별 룬 슬롯 표시
  - 아이콘, 이름, 레벨, 부옵션 개수
  - 클릭 이벤트 → 강화 UI 열기

RuneEquipSlotUI
  - 장착 슬롯 3개
  - 드롭 타겟
  - 우클릭/더블클릭 해제
  - 한계돌파 별 표시

RuneEnhanceUI
  - 레벨업/한계돌파 탭
  - 스탯 미리보기
  - 재료 선택
  - 강화/한계돌파 버튼

RuneTooltipUI
  - 마우스 오버 시 상세 정보
  - 주 효과, 부옵션, 조건 표시

RuneDragHandler
  - 드래그 앤 드롭 처리
  - 임시 아이콘 생성/이동/삭제
```

---

## 🔁 데이터 흐름

```
1. 장착
   인벤토리 → RuneManager.EquipRune()
   → 슬롯에 참조 저장
   → PlayerRuntimeStats.SetConditionalModifiers()
   → UI 갱신

2. 강화
   UI → RuneEnhanceManager.TryLevelUp()
   → RuneInstance.currentLevel++
   → 부옵션 추첨 (마일스톤)
   → UI 갱신

3. 한계돌파
   UI → RuneEnhanceManager.TryLimitBreak()
   → RuneInstance.limitBreakCount++
   → RuneInstance.maxLevel++
   → 재료 RuneInventoryManager.RemoveRune()
   → UI 갱신
```

---

## ⚠️ 현재 구조의 특징

### 장점
- ✅ 매니저가 데이터 관리 (UI는 표시만)
- ✅ 드래그 앤 드롭 직관적
- ✅ 모든 동작 후 UI 자동 갱신

### 개선 필요 사항
- ❌ RuneInventoryPanel + RuneEnhancePanel 동시 표시 (화면 복잡)
- ❌ 강화 UI가 별도 패널 (룬 선택과 강화가 분리됨)
- ❌ 재료 선택이 한계돌파 탭 안에만 존재
- ❌ 장착 슬롯 위치가 불명확할 수 있음
- ❌ 정렬/필터 미구현
- ❌ 일괄 처리 (여러 룬 한번에) 미구현

---

## 💡 예상되는 UX 개선 방향

### 1. 통합 패널 구조
```
현재: 인벤토리 + 강화 UI (분리)
개선: 탭 방식 통합 UI
  - 인벤토리 탭
  - 장착 관리 탭
  - 강화 탭
```

### 2. 강화 플로우 단순화
```
현재: 룬 선택 → 강화 UI 열기 → 탭 전환 → 강화
개선: 룬 클릭 → 우측 상세 패널 → 즉시 강화
```

### 3. 장착 UI 개선
```
현재: 드래그 앤 드롭만
개선: 
  - 드래그 앤 드롭
  - 클릭 → 슬롯 선택 방식 추가
  - 스왑 기능 (장착된 룬끼리 교체)
```

### 4. 재료 선택 개선
```
현재: 한계돌파 탭 안에서만
개선: 인벤토리에서 직접 재료 선택 후 한계돌파
```

---

**현재 버전**: Phase 5-2 Complete  
**다음 개선**: UX 리팩토링 필요
