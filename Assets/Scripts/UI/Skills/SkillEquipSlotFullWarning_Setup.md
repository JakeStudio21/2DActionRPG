# 🎮 스킬 슬롯 가득참 경고 메시지 시스템 - Unity 에디터 설정 가이드

## 📋 목차
1. [개요](#개요)
2. [Unity 에디터 설정](#unity-에디터-설정)
3. [UI 디자인 권장사항](#ui-디자인-권장사항)
4. [테스트 시나리오](#테스트-시나리오)
5. [문제 해결](#문제-해결)

---

## 개요

스킬 시스템에 **액티브 스킬 슬롯**과 **패시브 스킬 슬롯**을 독립적으로 체크하고, 슬롯이 가득 찼을 때 사용자에게 경고 메시지를 표시하는 기능이 추가되었습니다.

### 주요 기능
- **독립적인 슬롯 체크**: 액티브 스킬 슬롯(2개)과 패시브 스킬 슬롯(3개)을 별도로 관리
- **스킬 타입별 경고 메시지**: "액티브 스킬 슬롯이 가득 찼습니다!" 또는 "패시브 스킬 슬롯이 가득 찼습니다!"
- **해제 기능**: 장착된 스킬을 우측 패널에서 "해제하기" 버튼으로 간편하게 해제
- **버튼 텍스트 변경**: "장착" → "장착하기", "장착중" → "해제하기"

---

## Unity 에디터 설정

### 1. 워닝 메시지 UI 오브젝트 생성

#### 1-1. SkillSubPanel 하위 구조 확인
```
SkillSubPanel (또는 SkillBookPanel)
├── TopPanel (SP 표시 영역)
├── LeftPanel (장착 슬롯)
├── RightPanel (스킬 리스트)
└── BottomPanel (상세 정보)
```

#### 1-2. WarningMessage GameObject 생성
1. **Hierarchy**에서 `TopPanel` (권장) 또는 `SkillSubPanel`을 우클릭
2. `UI > Panel` 선택하여 새 Panel 생성
3. 이름을 `WarningMessage`로 변경

#### 1-3. RectTransform 설정
- **Anchors**: Center-Middle
- **Pivot**: (0.5, 0.5)
- **Width**: 400
- **Height**: 100
- **Position**:
  - TopPanel 내부: X=0, Y=-50 (상단 SP 표시 아래)
  - 독립 배치: X=0, Y=300 (화면 중앙 상단)

#### 1-4. Image 컴포넌트 설정
- **Color**: (1, 0.3, 0.3, 0.9) (반투명 빨간색)
- **Raycast Target**: ✅ 체크 (클릭 방지)

---

### 2. 경고 텍스트 추가

#### 2-1. TextMeshProUGUI 오브젝트 생성
1. `WarningMessage`를 우클릭
2. `UI > Text - TextMeshPro` 선택
3. 이름을 `WarningText`로 변경

#### 2-2. RectTransform 설정
- **Anchors**: Stretch-Stretch (상하좌우 꽉 채움)
- **Left/Right/Top/Bottom**: 각각 10

#### 2-3. TextMeshProUGUI 컴포넌트 설정
- **Text**: (비워두거나 임시 텍스트)
  ```
  ⚠️ 액티브 스킬 슬롯이 가득 찼습니다!
  먼저 액티브 스킬을 해제해주세요.
  ```
- **Font Size**: 18
- **Alignment**: Center (가운데 정렬)
- **Color**: (1, 1, 1, 1) (흰색)
- **Wrapping**: ✅ Enable
- **Overflow**: Truncate

---

### 3. SkillTabController 컴포넌트 연결

#### 3-1. SkillTabController 찾기
1. **Hierarchy**에서 `SkillSubPanel` (또는 SkillTabController가 붙은 GameObject) 선택
2. **Inspector**에서 `SkillTabController` 컴포넌트 확인

#### 3-2. 워닝 메시지 필드 연결
`SkillTabController` 컴포넌트의 `⚠️ 경고 메시지` 섹션에서:

| 필드 | 연결할 GameObject | 설명 |
|------|------------------|------|
| **Warning Message Object** | `WarningMessage` | 경고 메시지 Panel |
| **Warning Message Text** | `WarningText` (TextMeshProUGUI) | 경고 텍스트 |
| **Warning Display Duration** | `3` | 메시지 표시 시간(초) |

#### 3-3. 저장
- `Ctrl + S` (또는 `File > Save`)로 Scene 저장
- `WarningMessage` GameObject를 **비활성화** (✅ 체크 해제) - 코드에서 자동으로 표시됨

---

## UI 디자인 권장사항

### 배치 옵션

#### 옵션 1: TopPanel 내부 배치 (권장)
- **장점**: SP 표시 근처에 위치하여 시선 이동이 적음
- **단점**: TopPanel의 다른 UI와 겹칠 수 있음
- **Position**: Y = -50 (SP 표시 아래)

#### 옵션 2: 화면 중앙 상단 배치
- **장점**: 가장 눈에 잘 띔
- **단점**: 스킬 리스트를 가릴 수 있음
- **Position**: Y = 300

#### 옵션 3: LeftPanel 상단 배치
- **장점**: 장착 슬롯 바로 위에 표시되어 직관적
- **단점**: LeftPanel 크기에 따라 위치 조정 필요

### 색상 및 스타일

#### 경고 배경색
```csharp
// 빨간색 (위험)
Color = (1.0, 0.3, 0.3, 0.9)

// 주황색 (주의)
Color = (1.0, 0.6, 0.0, 0.9)

// 노란색 (안내)
Color = (1.0, 0.9, 0.3, 0.9)
```

#### 텍스트 아이콘
- ⚠️ (경고)
- ❌ (불가)
- ⛔ (금지)

---

## 테스트 시나리오

### 시나리오 1: 액티브 스킬 슬롯 가득참
1. **액티브 스킬 2개**를 장착
2. **3번째 액티브 스킬**을 장착 시도
3. **기대 결과**:
   ```
   ⚠️ 액티브 스킬 슬롯이 가득 찼습니다!
   먼저 액티브 스킬을 해제해주세요.
   ```
4. 패시브 스킬은 정상 장착 가능

### 시나리오 2: 패시브 스킬 슬롯 가득참
1. **패시브 스킬 3개**를 장착
2. **4번째 패시브 스킬**을 장착 시도
3. **기대 결과**:
   ```
   ⚠️ 패시브 스킬 슬롯이 가득 찼습니다!
   먼저 패시브 스킬을 해제해주세요.
   ```
4. 액티브 스킬은 정상 장착 가능

### 시나리오 3: 스킬 해제
1. 장착된 스킬의 **"해제하기"** 버튼 클릭
2. **기대 결과**:
   - 버튼 텍스트: "해제하기" → "장착하기"
   - 좌측 LeftPanel의 해당 슬롯이 비워짐
   - UI가 즉시 갱신됨

### 시나리오 4: 워닝 메시지 자동 숨김
1. 슬롯 가득참 경고 메시지 표시
2. **3초 대기**
3. **기대 결과**: 메시지가 자동으로 사라짐

---

## 문제 해결

### 1. 워닝 메시지가 표시되지 않음
- **원인**: SkillTabController의 필드가 연결되지 않았음
- **해결책**:
  1. SkillTabController의 `Warning Message Object`와 `Warning Message Text` 확인
  2. GameObject가 정확히 연결되었는지 확인

### 2. 메시지가 계속 표시됨
- **원인**: `WarningMessage` GameObject가 초기에 활성화되어 있음
- **해결책**:
  1. Hierarchy에서 `WarningMessage` 선택
  2. Inspector 상단의 체크박스 해제 (비활성화)
  3. Scene 저장

### 3. 버튼 텍스트가 변경되지 않음
- **원인**: `SkillListItemUI` Prefab의 버튼 필드 연결 누락
- **해결책**:
  1. `Assets/Prefabs/UI/Skill/SkillListItemPrefab` 선택
  2. `equipButtonText` 필드에 TextMeshProUGUI 연결
  3. Prefab 저장 (`Ctrl + S` 또는 `Overrides > Apply All`)

### 4. 액티브/패시브 구분이 안 됨
- **원인**: SkillData의 타입이 올바르게 설정되지 않음
- **해결책**:
  1. 스킬 ScriptableObject 확인
  2. `ActiveSkillData` 또는 `PassiveSkillData` 타입인지 확인
  3. 스킬 데이터베이스에서 타입 재설정

### 5. 슬롯이 비어있는데도 경고 표시
- **원인**: `FindEmptySlot` 로직 오류
- **해결책**:
  1. Unity Console에서 디버그 로그 확인
  2. `SkillTabController`의 `showDebugLogs` 활성화
  3. 로그에서 `FindEmptySlot` 반환값 확인

---

## 📝 변경 이력

| 버전 | 날짜 | 내용 |
|------|------|------|
| 1.0 | 2026-02-28 | 최초 작성 - 스킬 슬롯 가득참 경고 시스템 |

---

## 🔗 관련 문서
- `SkillTabController.cs` - 메인 스킬 UI 컨트롤러
- `SkillListItemUI.cs` - 스킬 리스트 아이템 UI
- `RuneEquipSlotFullWarning_Setup.md` - 룬 시스템 경고 메시지 (참고)

---

**작성일**: 2026-02-28  
**작성자**: AI Assistant  
**상태**: ✅ 완료
