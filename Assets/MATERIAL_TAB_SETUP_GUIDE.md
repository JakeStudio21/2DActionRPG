# 📑 재료 탭 시스템 Unity Editor 설정 가이드

**작업 시간**: 약 20~30분  
**난이도**: ⭐⭐ 보통

---

## 📋 목차

1. [Step 1: 탭 버튼 UI 생성](#step-1-탭-버튼-ui-생성)
2. [Step 2: LobbyInventoryUI Inspector 연결](#step-2-lobbyinventoryui-inspector-연결)
3. [Step 3: InventorySlot Prefab 수정](#step-3-inventoryslot-prefab-수정)
4. [Step 4: 테스트 재료 추가](#step-4-테스트-재료-추가)
5. [Step 5: 최종 테스트](#step-5-최종-테스트)

---

## Step 1: 탭 버튼 UI 생성

### 1-1. TabButtonsPanel 생성

**위치**: `Lobby Scene → Canvas → LobbyInventoryPanel`

1. **LobbyInventoryPanel** GameObject 하위에 **Empty GameObject** 생성
2. 이름: `TabButtonsPanel`
3. **Rect Transform** 설정:
   - Anchor: `Top Stretch` (상단 전체)
   - Pos Y: `-50` (Header 아래)
   - Height: `60`
   - Left: `20`, Right: `20`

4. **Horizontal Layout Group** 컴포넌트 추가:
   - Child Alignment: `Middle Left`
   - Spacing: `10`
   - Child Force Expand: Width ✅, Height ✅
   - Child Control Size: Width ✅, Height ✅

---

### 1-2. 장비 탭 버튼 생성

**위치**: `TabButtonsPanel` 하위

1. **UI → Button - TextMeshPro** 생성
2. 이름: `EquipmentTabButton`
3. **Button** 컴포넌트:
   - Interactable: ✅
   - Transition: Color Tint
   - Normal Color: `#FFFFFF` (흰색)
   - Highlighted Color: `#F0F0F0`
   - Pressed Color: `#C8C8C8`
   - Selected Color: `#F0F0F0`
   - Disabled Color: `#808080`

4. **Image** 컴포넌트:
   - Source Image: `UI Sprite` (배경)
   - Color: `#444444` (짙은 회색)

5. **하위 Text (TMP)** 오브젝트 선택:
   - 이름: `EquipmentTabText`
   - Text: `장비`
   - Font Size: `24`
   - Alignment: `Center & Middle`
   - Color: `#FFFFFF` (흰색, Alpha 1.0)
   - Font Style: `Bold`

---

### 1-3. 재료 탭 버튼 생성

**방법 1: 복사하기 (추천)**

1. **EquipmentTabButton** 복사 (Ctrl+D)
2. 이름: `MaterialTabButton`
3. 하위 Text 오브젝트 이름: `MaterialTabText`
4. Text 내용: `재료`
5. Color: `#FFFFFF` (Alpha 0.6) ← 비활성 상태

**방법 2: 처음부터 만들기**

- 위 1-2 단계와 동일하되, Text를 "재료"로 설정
- Color Alpha: `0.6` (비활성 상태)

---

### 1-4. 탭 버튼 레이아웃 확인

완성 구조:
```
TabButtonsPanel
├── EquipmentTabButton
│   └── EquipmentTabText (TMP)
└── MaterialTabButton
    └── MaterialTabText (TMP)
```

---

## Step 2: LobbyInventoryUI Inspector 연결

### 2-1. LobbyInventoryUI GameObject 선택

**위치**: `Lobby Scene → Canvas → LobbyInventoryPanel`

LobbyInventoryPanel GameObject에 **LobbyInventoryUI** 컴포넌트가 있는지 확인

---

### 2-2. Inspector 필드 연결

**📑 탭 시스템 섹션** (스크립트에 추가된 새 필드):

| 필드 이름 | 연결할 GameObject | 타입 |
|----------|------------------|------|
| `Equipment Tab Button` | TabButtonsPanel/EquipmentTabButton | Button |
| `Material Tab Button` | TabButtonsPanel/MaterialTabButton | Button |
| `Equipment Tab Text` | EquipmentTabButton/EquipmentTabText | TMP_Text |
| `Material Tab Text` | MaterialTabButton/MaterialTabText | TMP_Text |

**연결 방법**:
1. LobbyInventoryUI Inspector에서 필드 찾기
2. Hierarchy에서 해당 GameObject 드래그 → 필드에 드롭
3. 또는 필드 오른쪽 ⊙ 버튼 클릭 → GameObject 선택

---

### 2-3. 연결 확인

모든 필드가 `None (GameObject)` 상태가 아닌지 확인:

- ✅ Equipment Tab Button: `EquipmentTabButton`
- ✅ Material Tab Button: `MaterialTabButton`
- ✅ Equipment Tab Text: `EquipmentTabText (TMP_Text)`
- ✅ Material Tab Text: `MaterialTabText (TMP_Text)`

---

## Step 3: InventorySlot Prefab 수정

### 3-1. Prefab 열기

**방법 1: Prefab Mode**
1. Project 창에서 `Assets/Prefabs/UI/InventorySlot` 찾기
2. 더블클릭 → Prefab Mode 진입

**방법 2: Scene에서 수정**
1. Hierarchy에서 InventorySlot 인스턴스 선택
2. Inspector 상단 `Open Prefab` 클릭

---

### 3-2. CountText 추가 (재료 수량 표시)

**위치**: `InventorySlot/EffectTarget` 하위

1. **UI → Text - TextMeshPro** 생성
2. 이름: `CountText`
3. **Rect Transform**:
   - Anchor: `Bottom Right` (우하단)
   - Pivot: `(1, 0)`
   - Pos X: `-5`, Pos Y: `5`
   - Width: `60`, Height: `30`

4. **TextMeshProUGUI** 설정:
   - Text: `X999` (미리보기용)
   - Font Size: `18`
   - Alignment: `Bottom Right`
   - Color: `#FFFFFF` (흰색)
   - Font Style: `Bold`
   - Outline: Width `0.2`, Color `#000000` (검은색)

5. **초기 상태**: `Active = false` (비활성화) ← 중요!

---

### 3-3. RarityBorder 추가 (재료 등급 테두리)

**위치**: `InventorySlot/EffectTarget` 하위

1. **UI → Image** 생성
2. 이름: `RarityBorder`
3. **Rect Transform**:
   - Anchor: `Stretch All` (전체)
   - Left: `0`, Right: `0`, Top: `0`, Bottom: `0`

4. **Image** 설정:
   - Source Image: `UI Sprite` (테두리용, 선택 가능)
   - Image Type: `Sliced` (9-Slice)
   - Color: `#FFFFFF` (기본, 코드에서 변경됨)
   - Raycast Target: ❌ (클릭 방지)

5. **Outline** 컴포넌트 추가 (선택사항):
   - Effect Color: `#FFFFFF`
   - Effect Distance: `(2, -2)`

6. **초기 상태**: `Enabled = false` (비활성화) ← 중요!

---

### 3-4. InventorySlot Inspector 연결

**InventorySlot** 컴포넌트 Inspector에서:

**📦 재료 데이터 섹션** (새로 추가된 필드):

| 필드 이름 | 연결할 GameObject | 타입 |
|----------|------------------|------|
| `Count Text` | EffectTarget/CountText | TMP_Text |
| `Rarity Border` | EffectTarget/RarityBorder | Image |

**연결 방법**:
1. InventorySlot GameObject 선택
2. Inspector에서 InventorySlot 컴포넌트 찾기
3. Hierarchy에서 CountText, RarityBorder 드래그 → 필드에 드롭

---

### 3-5. Prefab 저장

**Prefab Mode인 경우**:
- 상단 `Save` 버튼 클릭
- `← (Back)` 버튼으로 Scene으로 돌아가기

**Scene에서 수정한 경우**:
- Inspector 상단 `Overrides` 드롭다운
- `Apply All` 클릭

---

### 3-6. 최종 구조 확인

```
InventorySlot (Prefab)
├── EffectTarget
│   ├── ItemIcon (Image) ← 기존
│   ├── CountText (TMP_Text) ← 신규, Active=false
│   └── RarityBorder (Image) ← 신규, Enabled=false
├── ItemIcon_Background (기존)
├── BindIcon (기존)
└── ...
```

---

## Step 4: 테스트 재료 추가

### 4-1. 테스트 스크립트 생성 (선택사항)

**위치**: `Assets/Scripts/Tests/MaterialTestHelper.cs`

```csharp
using UnityEngine;

public class MaterialTestHelper : MonoBehaviour
{
    [ContextMenu("Add Test Materials")]
    public void AddTestMaterials()
    {
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogWarning("AccountDataManager가 초기화되지 않았습니다.");
            return;
        }
        
        var manager = AccountDataManager.Instance;
        
        // 9가지 재료 추가
        manager.AddMaterial(MaterialType.WeaponFragment, 100);
        manager.AddMaterial(MaterialType.WeaponCrystal, 50);
        manager.AddMaterial(MaterialType.WeaponCore, 20);
        
        manager.AddMaterial(MaterialType.ArmorFragment, 80);
        manager.AddMaterial(MaterialType.ArmorCrystal, 40);
        manager.AddMaterial(MaterialType.ArmorCore, 15);
        
        manager.AddMaterial(MaterialType.AccessoryFragment, 60);
        manager.AddMaterial(MaterialType.AccessoryCrystal, 30);
        manager.AddMaterial(MaterialType.AccessoryCore, 10);
        
        manager.Save();
        
        Debug.Log("✅ 테스트 재료 추가 완료!");
    }
    
    [ContextMenu("Clear All Materials")]
    public void ClearAllMaterials()
    {
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogWarning("AccountDataManager가 초기화되지 않았습니다.");
            return;
        }
        
        var manager = AccountDataManager.Instance;
        manager.GetAccountData().materials.Clear();
        manager.Save();
        
        Debug.Log("🧹 모든 재료 삭제 완료!");
    }
}
```

---

### 4-2. 테스트 스크립트 사용

**방법 1: Inspector Context Menu**

1. Lobby Scene에 빈 GameObject 생성 → 이름: `MaterialTestHelper`
2. `MaterialTestHelper.cs` 스크립트 추가
3. Inspector에서 스크립트 우클릭 → `Add Test Materials` 선택

**방법 2: Console Command**

Play Mode에서 Console에 입력:
```csharp
AccountDataManager.Instance.AddMaterial(MaterialType.WeaponFragment, 100);
AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCrystal, 50);
AccountDataManager.Instance.Save();
```

---

### 4-3. Tools 메뉴로 재료 추가 (추천)

기존 Phase 테스트를 활용:

1. **Unity Editor 상단 메뉴**: `Tools → Phase 1 Test`
2. Console 확인 → Test 5 통과 시 재료 추가됨
3. 또는 수동으로 추가:

```csharp
// GameManager.cs의 Start() 또는 Awake()에 임시 추가
#if UNITY_EDITOR
if (AccountDataManager.IsInitialized())
{
    AccountDataManager.Instance.AddMaterial(MaterialType.WeaponFragment, 100);
    AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCrystal, 50);
    AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryCore, 20);
    AccountDataManager.Instance.Save();
    Debug.Log("✅ 테스트 재료 추가 완료!");
}
#endif
```

---

## Step 5: 최종 테스트

### 5-1. Play Mode 진입

1. Lobby Scene 실행
2. Console 확인:
   - `✅ [LobbyInventoryUI] 장비 탭 버튼 이벤트 연결 완료`
   - `✅ [LobbyInventoryUI] 재료 탭 버튼 이벤트 연결 완료`
   - `✅ [LobbyInventoryUI] AccountDataManager 재료 이벤트 구독 완료`

---

### 5-2. 기능 테스트

**테스트 1: 장비 탭 (기존 기능)**

1. 가방 버튼 클릭 → 인벤토리 열림
2. 기본적으로 "장비" 탭 활성화
3. 장비 아이템들이 표시됨
4. 장비 클릭 → 상세 패널 열림

**결과**: ✅ 장비 탭 정상 작동

---

**테스트 2: 재료 탭 전환**

1. 인벤토리 열린 상태에서
2. "재료" 탭 버튼 클릭
3. 화면 전환:
   - 장비 슬롯들이 사라짐
   - 재료 슬롯들이 나타남 (최대 16칸)
   - 빈 슬롯은 숨김 처리

**결과**: ✅ 탭 전환 정상 작동

---

**테스트 3: 재료 표시**

재료 슬롯 확인:
- ✅ 아이콘 표시됨 (MaterialDatabase의 icon)
- ✅ 수량 표시됨 ("X100" 형식)
- ✅ 등급별 테두리 색상:
  - 파편 (Common): 회색
  - 결정 (Uncommon): 초록
  - 코어 (Rare): 파랑
- ✅ 등급별 배경 색상 (ItemIconGradeFrame)

**결과**: ✅ 재료 표시 정상

---

**테스트 4: 탭 버튼 상태**

- ✅ 활성 탭: Alpha 1.0 (밝음)
- ✅ 비활성 탭: Alpha 0.6 (어두움)
- ✅ 탭 전환 시 버튼 상태 자동 변경

**결과**: ✅ 버튼 상태 정상

---

**테스트 5: 재료 추가/제거 반영**

Console에서 실행:
```csharp
AccountDataManager.Instance.AddMaterial(MaterialType.WeaponFragment, 50);
```

- ✅ 재료 탭에서 수량 자동 갱신
- ✅ "X100" → "X150" 변경

**결과**: ✅ 이벤트 시스템 정상

---

### 5-3. 디버깅 로그 확인

**정상 동작 로그**:
```
✅ [LobbyInventoryUI] 장비 탭 버튼 이벤트 연결 완료
✅ [LobbyInventoryUI] 재료 탭 버튼 이벤트 연결 완료
📑 [LobbyInventoryUI] 탭 전환: Material
📦 [LobbyInventoryUI] 재료 탭 갱신: 9개 재료
📦 [InventorySlot] 재료 설정: 무기 강화 파편 x100
📦 [InventorySlot] 재료 설정: 무기 강화 결정 x50
...
📑 [LobbyInventoryUI] 탭 버튼 상태 업데이트 완료 (현재: Material)
```

---

## ⚠️ 문제 해결

### 문제 1: 탭 버튼이 작동하지 않음

**원인**: Inspector 연결 누락

**해결**:
1. LobbyInventoryUI Inspector 확인
2. 4개 필드가 모두 연결되었는지 확인
3. Console에서 "이벤트 연결 완료" 로그 확인

---

### 문제 2: 재료가 표시되지 않음

**원인 1**: 재료 데이터 없음

**해결**:
- Step 4 참고하여 테스트 재료 추가
- Console에서 `AccountDataManager.Instance.PrintStats()` 실행 → 재료 확인

**원인 2**: InventorySlot Prefab 필드 미연결

**해결**:
- Step 3-4 참고하여 CountText, RarityBorder 연결
- Prefab 저장 확인

---

### 문제 3: MaterialDatabase 에러

**에러**: `MaterialDatabase.asset을 찾을 수 없습니다.`

**해결**:
1. Unity Editor 상단: `Tools → Create Material Database`
2. Console 확인: "✅ MaterialDatabase 생성 완료"
3. `Assets/Resources/Data/MaterialDatabase.asset` 생성 확인

---

### 문제 4: 재료 아이콘이 없음

**원인**: MaterialData에 icon 미할당

**해결**:
1. Project 창: `Assets/Resources/Data/Materials/`
2. 각 MaterialData.asset 선택
3. Inspector에서 `icon` 필드에 스프라이트 할당
4. 9개 모두 할당 후 저장

---

### 문제 5: 슬롯 클릭 시 에러

**에러**: `NullReferenceException`

**원인**: CountText 또는 RarityBorder 연결 누락

**해결**:
1. InventorySlot Prefab 열기
2. Inspector에서 필드 연결 확인
3. 비어있으면 Step 3-4 다시 진행

---

### 문제 6: 재료 등급별 테두리 색상이 표시되지 않음

**증상**: 재료 슬롯에 테두리가 표시되지 않거나, 색상이 변하지 않음

**디버깅 1단계: 콘솔 로그 확인**

재료 탭 전환 시 다음 로그가 나와야 함:
```
[InventorySlot] 재료 테두리 설정: WeaponFragment | Rarity: Common | Color: RGBA(0.5, 0.5, 0.5, 1.0) | Alpha: 1 | Enabled: True
```

**만약 `rarityBorder가 null입니다` 로그가 나온다면**:
- → **Step 3-4**로 이동하여 `RarityBorder` Inspector 연결

---

**디버깅 2단계: RarityBorder Image 설정 확인**

**InventorySlot Prefab → RarityBorder GameObject 선택** 후 Inspector 확인:

| 설정 항목 | 올바른 값 | 설명 |
|----------|----------|------|
| **Image.Color** | `White (255, 255, 255, 255)` | ⚠️ 중요! 스크립트에서 색상을 덮어씀 |
| **Image.Material** | `None (Material)` or `Default UI Material` | Sprite 기본 Material |
| **Image.Raycast Target** | ❌ (체크 해제) | 클릭 이벤트 방지 |
| **Image.Enabled** | ❌ (초기 비활성화) | 스크립트에서 활성화 |
| **Source Image** | `UI Sprite` (선택사항) | 테두리 이미지 (없으면 단색) |

---

**디버깅 3단계: 시각적 확인**

**RarityBorder가 보이지 않는 이유**:
1. **Image 컴포넌트가 Disabled**: 스크립트에서 `enabled = true` 호출 확인
2. **Alpha가 0**: Color의 Alpha 값이 255 (또는 1.0)인지 확인
3. **Source Image 없음**: Image Type을 `Simple`로 변경하거나, 단색 Sprite 할당
4. **Z-Order 문제**: RarityBorder가 다른 UI 요소 뒤에 숨겨짐 → Hierarchy 순서 조정

---

**해결 방법 (추천)**:

**옵션 A: Source Image 제거 (단색 테두리)**
1. RarityBorder GameObject 선택
2. Inspector → Image → `Source Image = None`
3. **Image Type**: `Simple`
4. **Color**: `White (255, 255, 255, 255)`
5. Play Mode → 재료 탭 확인

**옵션 B: UI Sprite 사용 (9-Slice 테두리)**
1. Project 창에서 UI Sprite 찾기 (예: `UI-Border`, `Panel`)
2. RarityBorder → Image → `Source Image` 할당
3. **Image Type**: `Sliced`
4. **Color**: `White (255, 255, 255, 255)`
5. Play Mode → 재료 탭 확인

---

**디버깅 4단계: 수동 테스트**

Play Mode에서 Console에 입력:
```csharp
// 슬롯 0의 RarityBorder 수동 활성화
var slot = GameObject.Find("InventorySlot(Clone)").GetComponent<InventorySlot>();
var border = slot.GetComponentInChildren<Image>(); // RarityBorder
border.enabled = true;
border.color = Color.red; // 빨간색으로 테스트
```

**결과**:
- ✅ 빨간 테두리가 보인다면 → 스크립트 로직 확인
- ❌ 아무것도 안 보인다면 → RarityBorder GameObject/Component 설정 문제

---

## 📊 완료 체크리스트

### UI 생성
- [ ] TabButtonsPanel 생성 (Horizontal Layout Group)
- [ ] EquipmentTabButton 생성 (Text: "장비")
- [ ] MaterialTabButton 생성 (Text: "재료")

### Inspector 연결
- [ ] LobbyInventoryUI.equipmentTabButton 연결
- [ ] LobbyInventoryUI.materialTabButton 연결
- [ ] LobbyInventoryUI.equipmentTabText 연결
- [ ] LobbyInventoryUI.materialTabText 연결

### Prefab 수정
- [ ] InventorySlot Prefab에 CountText 추가 (Active=false)
- [ ] InventorySlot Prefab에 RarityBorder 추가 (Enabled=false)
- [ ] InventorySlot.countText 연결
- [ ] InventorySlot.rarityBorder 연결
- [ ] Prefab 저장 완료

### 테스트
- [ ] MaterialDatabase.asset 생성 (Tools → Create Material Database)
- [ ] 9개 MaterialData 아이콘 할당
- [ ] 테스트 재료 추가 (Step 4)
- [ ] 장비 탭 정상 작동
- [ ] 재료 탭 전환 정상 작동
- [ ] 재료 표시 정상 (아이콘, 수량, 테두리)
- [ ] 탭 버튼 상태 변경 정상
- [ ] 재료 추가/제거 시 UI 자동 갱신

---

## 🎉 완료!

모든 체크리스트를 완료했다면 재료 탭 시스템이 정상 작동합니다!

---

## ✅ 최종 검증 완료 (2026-02-08)

### **검증된 기능**
- ✅ 재료 탭: 등급별 컬러 테두리 (`RarityBorder`) 정상 표시
- ✅ 장비 탭: `GradeBoard` 활성화/비활성화 자동 전환
- ✅ 탭 전환: 64칸 ↔ 16칸 슬롯 재사용 방식 정상 작동
- ✅ UI 충돌 해결: `RarityBorder` ↔ `GradeBoard` 역할 분리

### **주요 수정 사항**
1. `InventorySlot.SetupMaterial()`: `RarityBorder` GameObject 활성화
2. `InventorySlot.SetEquipmentData()`: `GradeBoard` GameObject 활성화/비활성화
3. `LobbyInventoryUI.RefreshInventoryUI()`: 재료 탭에서 장비 탭 전환 시 64칸 복원
4. 재료/장비 모드별 UI 요소 완전 분리 (충돌 방지)

### **설계 결정**
- **현재**: 슬롯 재사용 방식 유지 (64칸 공유)
- **향후**: 퀘스트 탭 추가 시 UI 분리 방식으로 리팩토링 예정

---

## 📝 다음 단계 (선택)

### Phase D: 몬스터 드롭 시스템 (1.5시간)
- 몬스터 처치 시 재료 드롭
- MaterialPickup 오브젝트 생성
- 풀링 시스템 연동

### Phase E: 재료 상세 패널 (1시간)
- ItemDetailPopup에 재료 모드 추가
- 재료 설명, 사용처, 획득처 표시
- 재료 클릭 → 상세 정보 팝업

### Phase F: 귀속/퀘스트 탭 (2시간)
- Bound 탭 추가 (귀속 아이템 필터)
- Quest 탭 추가 (퀘스트 아이템)
- UI 분리 방식으로 리팩토링 (탭별 독립 ScrollView)

---

**작성일**: 2026-02-07  
**최종 업데이트**: 2026-02-08  
**버전**: 1.1  
**작성자**: AI Assistant

