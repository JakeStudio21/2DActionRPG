# ⚠️ 룬 장착 슬롯 경고 메시지 설정 가이드

**작성일**: 2026-02-28  
**Phase**: 룬 장착 UX 개선  
**목적**: 3개 슬롯이 모두 찼을 때 경고 메시지 표시

---

## 🎯 구현 내용

### **동작 방식**

**Before**:
```
3개 슬롯 모두 장착됨
  ↓
다른 룬 [장착하기] 클릭
  ↓
자동으로 1번 슬롯 교체 (의도하지 않은 동작)
```

**After**:
```
3개 슬롯 모두 장착됨
  ↓
다른 룬 [장착하기] 클릭
  ↓
⚠️ "장착 슬롯이 가득 찼습니다! 먼저 장착된 룬을 해제해주세요."
  ↓
장착 실패 (안전)
```

---

## 📋 Unity Inspector 설정

### Step 1: RuneSubPanel 선택

1. **Hierarchy 창에서**:
   ```
   Canvas > SkillBookUI > Background > RuneSubPanel
   ```

2. **Inspector에서**:
   - `RunePanelUI` 컴포넌트 확인

---

### Step 2: TopPanel에 경고 메시지 오브젝트 생성 (권장)

#### 2-1. GameObject 생성

1. **Hierarchy에서**:
   ```
   RuneSubPanel > TopPanel (우클릭)
   ```
   또는
   ```
   RuneSubPanel > LeftPanel (우클릭)
   ```

2. **UI → Panel** 선택

3. **이름 변경**: `WarningMessage`

**💡 위치 선택**:
- **TopPanel** (권장): 화면 상단 중앙에 눈에 잘 띄게 표시
- **LeftPanel**: 장착 슬롯 바로 아래에 컨텍스트에 맞게 표시

---

#### 2-2. RectTransform 설정

**Option A: TopPanel (권장)**

```
Anchors: Top Center
Anchor Presets: Top Center

Position:
  X: 0
  Y: -50 (상단에서 50픽셀 아래)
  Z: 0

Size:
  Width: 400
  Height: 80
```

**Option B: LeftPanel**

```
Anchors: Bottom Center
Anchor Presets: Bottom Center

Position:
  X: 0
  Y: 30 (하단에서 30픽셀 위)
  Z: 0

Size:
  Width: 280
  Height: 80
```

---

#### 2-3. Panel 컴포넌트 설정

**Image 컴포넌트**:
```
Color: 빨강 반투명 (255, 100, 100, 200)
Image Type: Sliced
```

---

#### 2-4. Text 자식 오브젝트 생성

1. **WarningMessage 우클릭**:
   ```
   UI → Text - TextMeshPro
   ```

2. **이름**: `WarningText`

3. **RectTransform**:
   ```
   Anchors: Stretch (모든 면에 꽉 참)
   
   Left: 10
   Right: 10
   Top: 10
   Bottom: 10
   ```

4. **TextMeshProUGUI 설정**:
   ```
   Text: "⚠️ 장착 슬롯이 가득 찼습니다!\n먼저 장착된 룬을 해제해주세요."
   
   Font Size: 16
   Color: 흰색 (255, 255, 255, 255)
   
   Alignment: 
     Horizontal: Center
     Vertical: Middle
   
   Wrapping: Enabled
   Overflow: Truncate
   ```

---

### Step 3: RunePanelUI 컴포넌트 연결

1. **RuneSubPanel 선택**

2. **Inspector > RunePanelUI**

3. **경고 메시지 섹션**:
   ```
   Warning Message Object: WarningMessage (GameObject 드래그)
   Warning Message Text: WarningText (TextMeshProUGUI 드래그)
   Warning Display Duration: 3 (초)
   ```
   
   **💡 위치**: TopPanel 또는 LeftPanel 어디에 만들었든 상관없이 드래그 연결

---

## 🎨 UI 디자인 권장사항

### Option A: 심플 (권장)
```
배경 색상: 빨강 반투명 (255, 100, 100, 200)
테두리: 없음
텍스트: 흰색, 중앙 정렬
```

### Option B: 눈에 띄는 디자인
```
배경 색상: 주황 (255, 150, 0, 230)
테두리: 노란색 2px
아이콘: ⚠️ (텍스트 앞에)
애니메이션: 깜빡임 효과 (선택)
```

### Option C: 게임 UI 통합
```
기존 스킬 시스템의 경고 메시지 디자인과 동일하게
```

---

## 🧪 테스트

### Test 1: 경고 메시지 표시

**단계**:
1. Play 모드 진입
2. 룬 3개 해금 (Inspector ContextMenu 사용)
3. 룬 패널 열기
4. 3개 모두 장착
5. 4번째 룬 [장착하기] 클릭

**예상 결과**:
```
⚠️ 장착 슬롯이 가득 찼습니다!
먼저 장착된 룬을 해제해주세요.
```
(메시지가 LeftPanel 하단에 3초간 표시)

---

### Test 2: 정상 장착

**단계**:
1. 룬 1개만 장착된 상태
2. 다른 룬 [장착하기] 클릭

**예상 결과**:
- 빈 슬롯에 자동 장착 ✅
- 경고 메시지 없음

---

### Test 3: 해제 후 장착

**단계**:
1. 3개 슬롯 모두 차있음
2. 4번째 룬 [장착하기] 클릭 → 경고 메시지 표시
3. 기존 룬 하나 [해제하기] 클릭
4. 4번째 룬 다시 [장착하기] 클릭

**예상 결과**:
- 빈 슬롯에 정상 장착 ✅

---

## 📊 Hierarchy 구조

```
RuneSubPanel
├─ TopPanel
│  ├─ GoldText
│  └─ CrystalText
│
├─ LeftPanel
│  ├─ RuneEquipSlot (0)
│  ├─ RuneEquipSlot (1)
│  ├─ RuneEquipSlot (2)
│  └─ WarningMessage ★NEW★
│     └─ WarningText (TextMeshProUGUI)
│
├─ RightPanel
│  └─ ScrollView
│     └─ Content
│        └─ RuneListItemPrefab (동적 생성)
│
└─ BottomPanel
   ├─ RuneIcon
   ├─ RuneNameText
   ├─ MainStatText
   ├─ DescriptionText
   └─ SubStat1~3Text
```

---

## 🔧 코드 변경 사항

### 1. **RunePanelUI.cs**

#### 추가된 필드:
```csharp
[Header("=== Left Panel: 경고 메시지 ===")]
[SerializeField] private GameObject warningMessageObject;
[SerializeField] private TextMeshProUGUI warningMessageText;
[SerializeField] private float warningDisplayDuration = 3f;
```

#### 추가된 메서드:
```csharp
public void ShowWarningMessage(string message)
{
    warningMessageText.text = message;
    warningMessageObject.SetActive(true);
    StartCoroutine(HideWarningMessageAfterDelay());
}

private IEnumerator HideWarningMessageAfterDelay()
{
    yield return new WaitForSeconds(warningDisplayDuration);
    warningMessageObject.SetActive(false);
}
```

---

### 2. **RuneListItemUI.cs**

#### 수정된 Setup 메서드:
```csharp
public void Setup(RuneInstance rune, Action<RuneListItemUI> onClickCallback, RunePanelUI panel)
public void SetupAsLocked(RuneData data, Action<RuneListItemUI> onClickCallback, RunePanelUI panel)
```

#### 수정된 OnEquipButtonClicked():
```csharp
// 장착 시도 전 빈 슬롯 확인
if (emptySlotIndex < 0)
{
    panelUI.ShowWarningMessage("⚠️ 장착 슬롯이 가득 찼습니다!\n먼저 장착된 룬을 해제해주세요.");
    return;
}
```

---

## 💬 경고 메시지 문구 (한글)

### 현재 적용된 메시지:
```
⚠️ 장착 슬롯이 가득 찼습니다!
먼저 장착된 룬을 해제해주세요.
```

### 대체 문구 옵션:

#### Option 1 (간결):
```
⚠️ 슬롯이 가득 찼습니다!
룬을 먼저 해제하세요.
```

#### Option 2 (친절):
```
⚠️ 장착 슬롯을 모두 사용 중입니다.
새로운 룬을 장착하려면
먼저 기존 룬을 해제해주세요.
```

#### Option 3 (게임 톤):
```
⚠️ 룬 슬롯이 가득 찼습니다!
[해제하기] 버튼으로 슬롯을 비워주세요.
```

**수정 방법**: `RuneListItemUI.cs` Line 632의 메시지 문자열 변경

---

## 🎯 완료 체크리스트

- [ ] LeftPanel > WarningMessage GameObject 생성
- [ ] WarningMessage > WarningText (TMP) 생성
- [ ] WarningMessage 위치/크기 설정
- [ ] Panel 배경 색상 설정 (빨강 반투명)
- [ ] WarningText 폰트/정렬 설정
- [ ] RunePanelUI 컴포넌트에 연결
- [ ] Play 모드 → 3개 장착 → 4번째 장착 시도 테스트
- [ ] 경고 메시지 3초 후 자동 숨김 확인

---

## ✅ 완료!

**구현된 기능**:
- ✅ 3개 슬롯 모두 찼을 때 장착 차단
- ✅ 경고 메시지 표시 (3초 자동 숨김)
- ✅ 빈 슬롯이 있으면 자동 장착
- ✅ 기존 해제 기능 정상 작동

**다음 단계**: Unity Editor에서 UI 설정 → 테스트! 🚀
