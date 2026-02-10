# ✅ Phase 0: ResultFeedbackPopup 테스트 체크리스트

## 📋 테스트 준비

### ✅ 사전 준비 사항
- [ ] Unity 에디터 설정 가이드 완료 (`PHASE0_RESULTFEEDBACKPOPUP_EDITOR_GUIDE.md`)
- [ ] ResultFeedbackPopup 프리팹 생성됨 (Lobby Scene)
- [ ] ResultFeedbackPopup 컴포넌트 필드 모두 연결됨
- [ ] MaterialSlot 프리팹 할당 확인 ⭐ 중요!
- [ ] Lobby Scene 열림 (Play Mode로 테스트)

---

## 🧪 테스트 시나리오

### **테스트 1: 분해 결과 팝업 (3종 재료)**

#### 실행 방법
```
Unity 메뉴: Tools > Phase 0 Test > 1. 분해 결과 팝업 (3종 재료)
```

#### 예상 결과
- [ ] 팝업이 화면 중앙에 표시됨
- [ ] 어두운 배경 (BackgroundDim) 표시됨
- [ ] 타이틀: "분해 완료!" (초록색)
- [ ] 메시지: "3개 아이템 분해 완료"
- [ ] RewardPanel 표시됨, ResultItemPanel 숨김
- [ ] 재료 슬롯 3개 동적 생성:
  - WeaponFragment × 5
  - ArmorCrystal × 3
  - AccessoryCore × 1
- [ ] 3초 후 자동으로 팝업 닫힘

#### 디버그 로그 확인
```
[ResultFeedbackPopup] 분해 결과 표시: 3개 아이템, 3종류 재료
[ResultFeedbackPopup] 팝업 표시 (Z-Order 최상위)
[ResultFeedbackPopup] 재료 슬롯 3개 생성 완료
[ResultFeedbackPopup] 자동 닫기 시작 (3초 후)
[Phase 0 Test] 분해 결과 팝업 표시: 3개 아이템, 3종 재료
```

---

### **테스트 2: 분해 결과 팝업 (1종 재료 대량)**

#### 실행 방법
```
Unity 메뉴: Tools > Phase 0 Test > 2. 분해 결과 팝업 (1종 재료)
```

#### 예상 결과
- [ ] 팝업 표시됨
- [ ] 메시지: "5개 아이템 분해 완료"
- [ ] 재료 슬롯 1개만 생성:
  - WeaponFragment × 15
- [ ] 3초 후 자동 닫힘

---

### **테스트 3: 합성 결과 팝업 (B등급)**

#### 실행 방법
```
Unity 메뉴: Tools > Phase 0 Test > 3. 합성 결과 팝업 (B등급)
```

#### 예상 결과
- [ ] 팝업 표시됨
- [ ] 타이틀: "합성 성공!" (골드색)
- [ ] 메시지: "B 등급 아이템 획득!"
- [ ] ResultItemPanel 표시됨, RewardPanel 숨김
- [ ] 아이템 아이콘 표시됨
- [ ] ⭐ **등급 프레임 (ItemIconGradeFrame) 표시됨** (B등급 파란색 배경)
- [ ] 아이템 이름: "블레이징 소드 +0"
- [ ] 등급 텍스트: "B 등급" (파란색)
- [ ] 3초 후 자동 닫힘

#### 디버그 로그 확인
```
[ResultFeedbackPopup] 합성 결과 표시: 블레이징 소드 +0
[ResultFeedbackPopup] 팝업 표시 (Z-Order 최상위)
[ResultFeedbackPopup] 결과 아이템 표시: 블레이징 소드 (B)
[ResultFeedbackPopup] 자동 닫기 시작 (3초 후)
[Phase 0 Test] 합성 결과 팝업 표시: B등급 무기
```

---

### **테스트 4: 합성 결과 팝업 (S등급)**

#### 실행 방법
```
Unity 메뉴: Tools > Phase 0 Test > 4. 합성 결과 팝업 (S등급)
```

#### 예상 결과
- [ ] 팝업 표시됨
- [ ] 메시지: "S 등급 아이템 획득!"
- [ ] ⭐ **등급 프레임 (ItemIconGradeFrame) 표시됨** (S등급 골드색 배경)
- [ ] 아이템 이름: "드래곤 아머 +0"
- [ ] 등급 텍스트: "S 등급" (골드색)
- [ ] 3초 후 자동 닫힘

---

### **테스트 5: 팝업 수동 닫기**

#### 실행 방법
```
1. 테스트 1~4 중 하나 실행하여 팝업 표시
2. Unity 메뉴: Tools > Phase 0 Test > 5. 팝업 수동 닫기
```

#### 예상 결과
- [ ] 팝업이 즉시 닫힘
- [ ] 자동 닫기 코루틴 중지됨
- [ ] BackgroundDim 비활성화됨
- [ ] PopupPanel 비활성화됨
- [ ] 재료 슬롯 모두 제거됨

#### 디버그 로그 확인
```
[ResultFeedbackPopup] 팝업 닫기
[ResultFeedbackPopup] 모든 재료 슬롯 제거 완료
[Phase 0 Test] 팝업 닫기
```

---

### **테스트 6: 자동 닫기 (3초 타이머)**

#### 실행 방법
```
Unity 메뉴: Tools > Phase 0 Test > 6. 자동 닫기 테스트 (3초)
```

#### 예상 결과
- [ ] 팝업 표시됨
- [ ] 3초 대기 (타이머)
- [ ] 정확히 3초 후 팝업 자동 닫힘

#### 타이머 확인 방법
```
1. 팝업 표시 후 스톱워치 시작
2. 팝업이 닫히는 시점 측정
3. 약 3.0초 경과 확인
```

#### 디버그 로그 확인
```
[ResultFeedbackPopup] 자동 닫기 시작 (3초 후)
... (3초 대기) ...
[ResultFeedbackPopup] 자동 닫기 실행
[ResultFeedbackPopup] 팝업 닫기
```

---

### **테스트 7: 연속 호출 (팝업 교체)**

#### 실행 방법
```
1. 테스트 1 실행 (분해 결과 표시)
2. 즉시 테스트 3 실행 (합성 결과 표시)
```

#### 예상 결과
- [ ] 분해 결과 팝업이 먼저 표시됨
- [ ] 합성 결과 팝업으로 즉시 교체됨 (덮어쓰기)
- [ ] 이전 재료 슬롯이 제거되고 새 아이템 표시
- [ ] 자동 닫기 타이머가 재시작됨 (3초)

---

### **테스트 8: 닫기 버튼 클릭**

#### 실행 방법
```
1. 테스트 1~4 중 하나 실행하여 팝업 표시
2. [확인] 버튼 클릭 (마우스)
```

#### 예상 결과
- [ ] 버튼 클릭 시 팝업 즉시 닫힘
- [ ] 자동 닫기 대기 중이어도 즉시 닫힘

#### 디버그 로그 확인
```
[ResultFeedbackPopup] 닫기 버튼 클릭
[ResultFeedbackPopup] 팝업 닫기
```

---

### **테스트 9: Z-Order 확인 (최상위 표시)**

#### 실행 방법
```
1. Lobby Scene에서 여러 UI 요소 활성화 (WorkshopPanel 등)
2. 테스트 1 실행 (팝업 표시)
```

#### 예상 결과
- [ ] 팝업이 모든 UI 위에 표시됨
- [ ] WorkshopPanel 등이 팝업 아래에 가려짐
- [ ] Hierarchy에서 ResultFeedbackPopup이 최하위 → 최상위로 이동 확인

#### Hierarchy 확인
```
Canvas
├── ... (다른 UI 요소들)
└── ResultFeedbackPopup ← 최상위 (Sibling Index 가장 큼)
```

---

### **테스트 10: MaterialSlot 프리팹 미할당 시 경고**

#### 실행 방법
```
1. ResultFeedbackPopup 컴포넌트에서 Material Slot Prefab 할당 해제
2. 테스트 1 실행
```

#### 예상 결과
- [ ] 팝업 표시됨 (기본 구조)
- [ ] 재료 슬롯 생성 실패
- [ ] Console에 경고 메시지:
  ```
  [ResultFeedbackPopup] materialSlotPrefab 또는 rewardSlotsContainer가 null입니다!
  ```

---

## 🔍 상세 검증 항목

### **UI 표시 검증**
- [ ] 팝업 크기: 600 × 400 (중앙 고정)
- [ ] BackgroundDim: 화면 전체 덮음, 반투명 검은색
- [ ] TitleText: 굵은 글씨, 크기 32
- [ ] MessageText: 회색, 크기 20
- [ ] 재료 슬롯: HorizontalLayoutGroup, 간격 20px
- [ ] 아이템 아이콘: 128 × 128, Preserve Aspect
- [ ] ⭐ **등급 프레임: 아이콘 배경에 등급별 색상 표시**
- [ ] 닫기 버튼: 초록색, 하단 중앙

### **동적 생성 검증**
- [ ] 재료 슬롯이 Container 하위에 생성됨
- [ ] 각 슬롯에 MaterialType 표시됨
- [ ] 각 슬롯에 개수(count) 표시됨
- [ ] 이전 슬롯이 제거되고 새 슬롯 생성됨

### **등급별 색상 검증**

**등급 텍스트 색상:**
- [ ] D등급: 회색
- [ ] C등급: 연두색
- [ ] B등급: 파란색
- [ ] A등급: 보라색
- [ ] S등급: 골드
- [ ] SS등급: 주황색
- [ ] EX등급: 빨간색
- [ ] TR등급: 시안

**⭐ 등급 프레임 배경 색상 (합성 결과만):**
- [ ] ItemIconGradeFrame이 등급별로 배경 색상 표시
- [ ] 색상은 ItemGradeColorManager에서 자동 적용
- [ ] 분해 결과에서는 등급 프레임 비활성화됨

### **코루틴 검증**
- [ ] 자동 닫기 코루틴 시작됨
- [ ] 3초 후 정확히 Close() 호출됨
- [ ] 연속 호출 시 이전 코루틴 중지됨
- [ ] 수동 닫기 시 코루틴 중지됨

---

## 🐛 예상 문제 및 해결 방법

### **문제 1: 팝업이 표시되지 않음**
**원인**: PopupPanel / BackgroundDim이 초기 활성화 상태
**해결**: Awake()에서 비활성화 처리 확인

### **문제 2: 재료 슬롯이 생성되지 않음**
**원인**: MaterialSlot 프리팹 미할당
**해결**: Inspector에서 InventorySlot.prefab 할당

### **문제 3: 자동 닫기가 작동하지 않음**
**원인**: autoCloseDelay = 0
**해결**: Inspector에서 autoCloseDelay = 3 설정

### **문제 4: Z-Order가 최상위가 아님**
**원인**: SetAsLastSibling() 미호출
**해결**: ShowPopup()에서 transform.SetAsLastSibling() 확인

### **문제 5: 아이템 아이콘이 표시되지 않음**
**원인**: EquipmentData.icon이 null
**해결**: 실제 게임에서는 Resources.Load()로 아이콘 로드 필요

### **문제 6: 등급 프레임이 표시되지 않음**
**원인**: resultItemGradeFrame 필드 미할당
**해결**: Inspector에서 ItemIconGradeFrame 컴포넌트 연결

### **문제 7: 등급 프레임 색상이 흰색으로만 나옴**
**원인**: ItemGradeColorManager가 초기화되지 않음
**해결**: Lobby Scene에 ItemGradeColorManager가 존재하는지 확인

---

## ✅ 최종 검증 (모든 테스트 통과 후)

- [ ] 분해 결과 팝업 정상 작동 (1종, 3종 재료)
- [ ] 합성 결과 팝업 정상 작동 (B, S등급)
- [ ] 자동 닫기 (3초) 정상 작동
- [ ] 수동 닫기 (버튼) 정상 작동
- [ ] 연속 호출 시 팝업 교체 정상
- [ ] Z-Order 최상위 표시 확인
- [ ] 재료 슬롯 동적 생성/제거 정상
- [ ] 등급별 색상 표시 정상
- [ ] 디버그 로그 모두 정상 출력
- [ ] 컴파일 에러 0개

---

## 📊 성공 기준

**Phase 0 완료 조건 (DoD):**
1. ✅ ResultFeedbackPopup.cs 작성 완료
2. ✅ Unity 프리팹 생성 완료
3. ✅ 위 테스트 1~10 모두 통과
4. ✅ 디버그 로그 정상 출력
5. ✅ Z-Order 최상위 확인
6. ✅ 재료 슬롯 동적 생성 확인
7. ✅ 자동 닫기 정상 작동

**모든 테스트 통과 시 Phase 1 (분해 UI) 진행 가능!**

