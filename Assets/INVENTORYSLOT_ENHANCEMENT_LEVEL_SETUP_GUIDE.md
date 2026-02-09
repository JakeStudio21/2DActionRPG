# 📋 InventorySlot 강화 레벨 표시 기능 - Unity Editor 설정 가이드

## ✅ 코드 수정 완료

**파일:** `Assets/Scripts/UI/Inventory/InventorySlot.cs`

### 추가된 기능:
1. ✅ `enhancementLevelText` 필드 추가 (TMP_Text)
2. ✅ `UpdateEnhancementLevelDisplay()` 메서드 추가
3. ✅ `GetEnhancementLevel()` 헬퍼 메서드 추가
4. ✅ `SetEquipmentData()`에서 강화 레벨 자동 표시
5. ✅ `ClearSlot()`에서 강화 레벨 텍스트 정리
6. ✅ `SetupMaterial()`에서 강화 레벨 텍스트 숨김

---

## 🎨 Unity Editor 작업 (필수)

### Step 1: InventorySlot 프리팹 열기

**경로:** `Assets/Prefabs/UI/InventorySlot.prefab` (프리팹 경로는 프로젝트에 따라 다를 수 있음)

**찾는 방법:**
1. Project 창에서 "InventorySlot" 검색
2. 프리팹을 더블클릭하여 Prefab 편집 모드로 진입

---

### Step 2: EnhancementLevelText GameObject 생성

**Hierarchy 구조:**

```
InventorySlot (Root)
├── SlotImage (Image) - 슬롯 배경
├── ItemIcon (Image) - 아이템 아이콘
├── GradeBoard (ItemIconGradeFrame) - 등급 배경
├── BindIcon (Image) - 귀속 아이콘 (우측 상단)
├── CountText (TMP_Text) - 재료 수량 (우측 하단) - 기존
├── EnhancementLevelText (TMP_Text) 🆕 - 강화 레벨 (좌측 하단)
├── SelectionCheckbox (GameObject)
└── SelectionHighlight (Image)
```

**생성 방법:**
1. `InventorySlot` (Root) 우클릭
2. `UI → TextMeshPro - Text` 선택
3. 이름을 **"EnhancementLevelText"**로 변경

---

### Step 3: EnhancementLevelText Transform 설정

**RectTransform 설정:**

| 속성 | 값 | 설명 |
|------|-----|------|
| **Anchors** | (0, 0) | 좌측 하단 기준 |
| **Pivot** | (0, 0) | 좌측 하단 피벗 |
| **Position** | X=2, Y=2, Z=0 | 약간의 여백 |
| **Width** | 40 | 너비 |
| **Height** | 20 | 높이 |

**설정 방법:**
1. Inspector에서 **Rect Transform** 컴포넌트 찾기
2. **Anchors Preset** 클릭 (작은 네모 아이콘)
3. **Shift + Alt** 누른 채로 **좌측 하단** 클릭 (Anchor + Pivot 동시 설정)
4. **Pos X**: 2
5. **Pos Y**: 2
6. **Width**: 40
7. **Height**: 20

---

### Step 4: TextMeshPro - Text (UI) 컴포넌트 설정

**Text 설정:**

| 속성 | 값 | 설명 |
|------|-----|------|
| **Text** | "+5" | 기본 텍스트 (테스트용) |
| **Font Asset** | 프로젝트 기본 폰트 | (LiberationSans SDF 등) |
| **Font Size** | 16 | 가독성 유지 |
| **Color** | White (1, 1, 1, 1) | 흰색 고정 |
| **Alignment** | Left, Bottom | 좌측 하단 정렬 |
| **Auto Size** | ✅ Disabled | 고정 크기 사용 |

**Outline 설정 (가독성 향상):**

| 속성 | 값 |
|------|-----|
| **Material Preset** | TextMeshPro/Distance Field (SDF) Overlay |
| **Outline** | ✅ Enabled |
| **Outline Color** | Black (0, 0, 0, 1) |
| **Outline Width** | 0.2 |

**설정 방법:**
1. **Text Input**: "+5" (테스트용)
2. **Font Size**: 16
3. **Vertex Color**: White (1, 1, 1, 1)
4. **Horizontal Alignment**: Left
5. **Vertical Alignment**: Bottom
6. **Auto Size**: Disabled (체크 해제)
7. **Material Preset**: TextMeshPro/Distance Field (SDF) Overlay
8. **Extra Settings → Outline**: Enable 체크
9. **Outline Color**: Black
10. **Outline Width**: 0.2

---

### Step 5: InventorySlot 컴포넌트 연결

**Inspector 설정:**

1. `InventorySlot` (Root) 선택
2. **Inspector → InventorySlot (Script)** 컴포넌트 찾기
3. **🎨 UI 컴포넌트** 섹션에서 **Enhancement Level Text** 필드 찾기
4. Hierarchy에서 **EnhancementLevelText** 드래그 → 필드에 드롭

**확인 사항:**
- ✅ `slotImage`: SlotImage 연결됨
- ✅ `itemIconImage`: ItemIcon 연결됨
- ✅ `itemIconGradeFrame`: GradeBoard 연결됨
- ✅ `slotButton`: InventorySlot (Root) 연결됨
- ✅ `bindIcon`: BindIcon 연결됨
- ✅ `countText`: CountText 연결됨
- 🆕 `enhancementLevelText`: **EnhancementLevelText 연결** ⭐ 필수!
- ✅ `rarityBorder`: (재료 테두리, 있다면)

---

### Step 6: 초기 상태 설정

**EnhancementLevelText GameObject 설정:**

1. Hierarchy에서 **EnhancementLevelText** 선택
2. Inspector 상단의 **체크박스 해제** (GameObject 비활성화)
   - 이유: 런타임에 강화된 아이템만 표시되도록 기본 비활성화

**확인 사항:**
- ✅ EnhancementLevelText GameObject가 비활성화 상태 (체크박스 해제)

---

### Step 7: 프리팹 저장 및 적용

1. **Ctrl + S** (저장)
2. Hierarchy 창 상단의 **"< Back"** 버튼 클릭 (Prefab 편집 모드 종료)
3. Project 창에서 프리팹 파일이 수정됨 (Modified 표시)

---

## 🧪 테스트 시나리오

### Test 1: 기본 표시 테스트

**준비:**
1. Unity Play 모드 진입
2. 로비 → 보관창고 열기

**확인 항목:**
- ✅ **+0 아이템**: 강화 레벨 표시 안 됨
- ✅ **+1 아이템**: "+1" 좌측 하단에 흰색 표시
- ✅ **+5 아이템**: "+5" 좌측 하단에 흰색 표시
- ✅ **+10 아이템**: "+10" 좌측 하단에 흰색 표시
- ✅ **+15 아이템**: "+15" 좌측 하단에 흰색 표시

### Test 2: 공방 인벤토리 테스트

**준비:**
1. Unity Play 모드 진입
2. 로비 → 공방 열기
3. 좌측 인벤토리에서 강화된 아이템 확인

**확인 항목:**
- ✅ 강화 레벨 표시됨 (장비 탭)
- ✅ 강화 레벨 표시 안 됨 (재료 탭)

### Test 3: 강화 실행 후 실시간 업데이트

**준비:**
1. Unity Play 모드 진입
2. 로비 → 공방 → 강화 탭
3. +4 아이템 선택 후 강화 실행

**확인 항목:**
- ✅ 강화 성공 시: "+4" → "+5"로 자동 갱신
- ✅ 강화 실패 하락 시: "+10" → "+9"로 자동 갱신
- ✅ 아이템 파괴 시: 슬롯 비워지고 강화 레벨 표시 사라짐

### Test 4: 재료 탭 전환 테스트

**준비:**
1. Unity Play 모드 진입
2. 로비 → 공방 → 재료 탭

**확인 항목:**
- ✅ 재료 수량 표시됨 ("X999")
- ✅ 강화 레벨 표시 안 됨

---

## 🔍 문제 해결 가이드

### 문제 1: 강화 레벨이 표시되지 않음

**증상:**
- +5 아이템인데 "+5" 텍스트가 보이지 않음

**원인:**
1. `enhancementLevelText` 필드가 Inspector에 연결되지 않음
2. EnhancementLevelText GameObject가 비활성화된 채로 활성화되지 않음
3. ItemInstanceId가 유효하지 않음

**해결 방법:**
1. InventorySlot 컴포넌트 Inspector 확인:
   - `Enhancement Level Text` 필드에 **EnhancementLevelText** GameObject 연결
2. Console 창에서 디버그 로그 확인:
   - "✨ [InventorySlot] 강화 레벨 표시: +5" 로그가 나타나는지 확인
3. InventorySlot.cs에서 `showDebugLogs = true` 설정 후 재테스트

---

### 문제 2: 텍스트가 잘려서 보임

**증상:**
- "+15"가 "+1..."처럼 잘려서 표시됨

**원인:**
- RectTransform Width가 너무 작음 (40 미만)

**해결 방법:**
1. EnhancementLevelText 선택
2. **Width**: 40 → 50으로 증가
3. 또는 **Auto Size** 활성화

---

### 문제 3: 재료 탭에서도 강화 레벨이 보임

**증상:**
- 재료 탭에서 "X999"와 "+5"가 동시에 표시됨

**원인:**
- `SetupMaterial()` 메서드에서 강화 레벨 텍스트를 숨기지 않음

**해결 방법:**
1. InventorySlot.cs Line 850 확인:
   ```csharp
   // 🆕 강화 레벨 텍스트 숨김 (재료 모드에서는 사용 안 함)
   if (enhancementLevelText != null)
   {
       enhancementLevelText.gameObject.SetActive(false);
   }
   ```
2. 코드가 있는지 확인 (최신 버전)

---

### 문제 4: 텍스트가 슬롯 밖으로 삐져나옴

**증상:**
- "+15" 텍스트가 슬롯 영역 밖으로 나감

**원인:**
- Anchors가 잘못 설정됨 (좌측 하단이 아님)

**해결 방법:**
1. EnhancementLevelText 선택
2. **Rect Transform → Anchors Preset** 클릭
3. **Shift + Alt** 누른 채로 **좌측 하단** 클릭
4. **Pos X**: 2, **Pos Y**: 2 확인

---

### 문제 5: 텍스트가 배경과 겹쳐서 안 보임

**증상:**
- 어두운 아이템 아이콘 위에서 흰색 텍스트가 안 보임

**원인:**
- Outline이 설정되지 않음

**해결 방법:**
1. EnhancementLevelText 선택
2. **TextMeshPro - Text (UI) 컴포넌트**에서:
   - **Extra Settings → Outline**: Enable 체크
   - **Outline Color**: Black (0, 0, 0, 1)
   - **Outline Width**: 0.2 ~ 0.3
3. 또는 **Material Preset**을 **TextMeshPro/Distance Field (SDF) Overlay**로 변경

---

## 📊 적용 범위

### ✅ 자동 적용될 곳 (프리팹 기반)

이 프리팹을 사용하는 모든 UI에 자동 적용됩니다:

1. **LobbyInventoryUI** (로비 보관창고)
2. **WorkshopInventoryUI** (공방 인벤토리)
3. **ShopInventoryUI** (상점 인벤토리)
4. **ActiveInventory** (인게임 가방)

---

## 🎉 완료 확인

### 체크리스트

- [ ] EnhancementLevelText GameObject 생성 완료
- [ ] RectTransform 설정 완료 (좌측 하단, X=2, Y=2)
- [ ] TextMeshPro 컴포넌트 설정 완료 (Font Size=16, White)
- [ ] Outline 설정 완료 (Black, Width=0.2)
- [ ] InventorySlot 컴포넌트 연결 완료 (`enhancementLevelText` 필드)
- [ ] 초기 비활성화 상태 설정 완료
- [ ] 프리팹 저장 완료
- [ ] Test 1: 기본 표시 테스트 통과
- [ ] Test 2: 공방 인벤토리 테스트 통과
- [ ] Test 3: 강화 후 실시간 업데이트 테스트 통과
- [ ] Test 4: 재료 탭 전환 테스트 통과

---

## 📝 다음 단계 (선택 사항)

### 추가 개선 옵션 (나중에 구현):

1. **애니메이션 효과**
   - +10 이상 강화 성공 시 반짝임 효과
   - DOTween으로 Pulsing 애니메이션

2. **강화 레벨별 색상 구분**
   - +1~4: 흰색
   - +5~9: 연두색
   - +10~15: 노란색

3. **Tooltip 확장**
   - 마우스 오버 시 강화 시도 횟수 표시
   - "강화 레벨: +5 (시도: 12회)"

---

## 📚 참고 자료

- **InventorySlot.cs**: `Assets/Scripts/UI/Inventory/InventorySlot.cs`
- **관련 메모리**: [[memory:14280755]] (ItemTemplateResolver 사용)
- **데이터 구조**: ItemInstanceData.enhancementLevel (0~15)

---

**작성일:** 2026-02-09  
**버전:** 1.0

