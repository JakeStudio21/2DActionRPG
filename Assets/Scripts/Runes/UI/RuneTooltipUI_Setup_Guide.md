# RuneTooltipUI 설정 가이드 (상세)

## 📋 UI 구조

```
RuneTooltipPanel (GameObject + RuneTooltipUI 스크립트)
├── BasicInfoContainer
│   ├── RuneIcon (Image)
│   ├── RuneNameText (TMP)
│   ├── RuneTypeText (TMP)
│   └── ObtainMethodText (TMP)
│
├── LevelInfoContainer
│   ├── LevelText (TMP)
│   └── LimitBreakText (TMP)
│
├── MainStatContainer (Empty GameObject)
│   ├── MainStatNameText (TMP)
│   └── MainStatValueText (TMP)
│
└── SubStatsContainer (Empty GameObject)
    └── SubStatsList (Empty GameObject + Vertical Layout Group)
        └── [SubStatRow 프리팹들이 런타임에 생성됨]
```

---

## 🎯 Unity 작업 순서

### 1. RuneTooltipPanel 만들기
```
Hierarchy 우클릭 → UI → Panel
이름: RuneTooltipPanel
```

### 2. RuneTooltipUI 스크립트 추가
```
RuneTooltipPanel 선택
Inspector → Add Component → RuneTooltipUI
```

---

## 📦 하위 UI 요소 생성

### 3. 기본 정보 컨테이너
```
RuneTooltipPanel 우클릭 → Create Empty
이름: BasicInfoContainer

BasicInfoContainer 우클릭 → UI → Image
이름: RuneIcon

BasicInfoContainer 우클릭 → UI → Text - TextMeshPro
이름: RuneNameText

BasicInfoContainer 우클릭 → UI → Text - TextMeshPro
이름: RuneTypeText

BasicInfoContainer 우클릭 → UI → Text - TextMeshPro
이름: ObtainMethodText
```

### 4. 레벨 정보 컨테이너
```
RuneTooltipPanel 우클릭 → Create Empty
이름: LevelInfoContainer

LevelInfoContainer 우클릭 → UI → Text - TextMeshPro
이름: LevelText

LevelInfoContainer 우클릭 → UI → Text - TextMeshPro
이름: LimitBreakText
```

### 5. 주옵션 컨테이너
```
RuneTooltipPanel 우클릭 → Create Empty
이름: MainStatContainer
(선택) MainStatContainer → Add Component → Horizontal Layout Group

MainStatContainer 우클릭 → UI → Text - TextMeshPro
이름: MainStatNameText

MainStatContainer 우클릭 → UI → Text - TextMeshPro
이름: MainStatValueText
```

### 6. 부옵션 컨테이너
```
RuneTooltipPanel 우클릭 → Create Empty
이름: SubStatsContainer

SubStatsContainer 우클릭 → Create Empty
이름: SubStatsList

SubStatsList 선택
Inspector → Add Component → Vertical Layout Group
```

---

## 🔗 Inspector 매핑 (RuneTooltipPanel의 RuneTooltipUI 컴포넌트)

### === 기본 정보 ===
```
Rune Icon Image: [드래그] BasicInfoContainer/RuneIcon (Image)
Rune Name Text: [드래그] BasicInfoContainer/RuneNameText (TMP)
Rune Type Text: [드래그] BasicInfoContainer/RuneTypeText (TMP)
Obtain Method Text: [드래그] BasicInfoContainer/ObtainMethodText (TMP)
```

### === 레벨 & 한계돌파 ===
```
Level Text: [드래그] LevelInfoContainer/LevelText (TMP)
Limit Break Text: [드래그] LevelInfoContainer/LimitBreakText (TMP)
```

### === 주옵션 (Main Stat) ===
```
Main Stat Container: [드래그] MainStatContainer (GameObject)
Main Stat Name Text: [드래그] MainStatContainer/MainStatNameText (TMP)
Main Stat Value Text: [드래그] MainStatContainer/MainStatValueText (TMP)
```

### === 부옵션 (Sub Stats) ===
```
Sub Stats Container: [드래그] SubStatsContainer (GameObject)
Sub Stats List: [드래그] SubStatsContainer/SubStatsList (Transform)
Sub Stat Row Prefab: [드래그] Assets/Prefabs/UI/Rune/SubStatRowPrefab.prefab
```

### === 툴팁 패널 ===
```
Tooltip Panel: [드래그] RuneTooltipPanel (자기 자신)
```

---

## ⚙️ Layout Group 설정 (선택사항)

### Horizontal Layout Group (MainStatContainer)
```
Child Alignment: Middle Left
Spacing: 10
Child Force Expand: Width ✓, Height ✗
Padding: 5, 5, 5, 5
```

### Vertical Layout Group (SubStatsList)
```
Child Alignment: Upper Left
Spacing: 5
Child Force Expand: Width ✓, Height ✗
Padding: 10, 10, 5, 5
```

---

## 🎨 텍스트 샘플 설정 (디자인 확인용)

### RuneNameText
```
Text: "보스 사냥꾼"
Font Size: 24
Font Style: Bold
Color: White
```

### LevelText
```
Text: "레벨: 15 / 15"
Font Size: 16
Color: Yellow
```

### MainStatNameText
```
Text: "주옵션: 보스 피해 증가 %"
Font Size: 14
Color: Cyan
```

### MainStatValueText
```
Text: "+34.0%"
Font Size: 14
Font Style: Bold
Color: Yellow
```

---

## ✅ 매핑 확인 체크리스트

- [ ] Rune Icon Image 연결됨
- [ ] Rune Name Text 연결됨
- [ ] Rune Type Text 연결됨
- [ ] Obtain Method Text 연결됨
- [ ] Level Text 연결됨
- [ ] Limit Break Text 연결됨
- [ ] Main Stat Container 연결됨
- [ ] Main Stat Name Text 연결됨
- [ ] Main Stat Value Text 연결됨
- [ ] Sub Stats Container 연결됨
- [ ] Sub Stats List 연결됨
- [ ] Sub Stat Row Prefab 연결됨
- [ ] Tooltip Panel 연결됨 (자기 자신)

---

## 🚀 테스트

### 1. 씬 실행
```
Play 버튼 클릭
```

### 2. 테스트 룬 생성
```
Tools > Rune System > Phase 5 - UI > 1. 테스트 룬 생성
```

### 3. 룬 클릭
```
인벤토리에서 룬 클릭 → 툴팁에 상세 정보 표시 확인
```

### 4. 확인 사항
- [ ] 룬 아이콘 표시
- [ ] 룬 이름 표시
- [ ] 레벨 정보 표시 (Lv.X / Y)
- [ ] 한계돌파 정보 표시 (한계돌파: X / Y)
- [ ] 주옵션 이름 & 값 표시 (스케일링 적용됨)
- [ ] 부옵션들 표시 (원본 값)
- [ ] 빈 슬롯 안내 문구 표시 ("Lv.3 도달 시 부옵션 개방")

---

## ⚠️ 주의사항

### Tooltip Panel은 자기 자신!
```
Tooltip Panel: RuneTooltipPanel (자기 자신)
```
이렇게 연결하면 `HideTooltip()`에서 `tooltipPanel.SetActive(false)`로 자신을 숨깁니다.

### SubStatsList는 Transform!
```
Sub Stats List: SubStatsList의 Transform 컴포넌트
```
Instantiate 시 부모로 사용되므로 Transform을 드래그해야 합니다.

---

**작성일**: 2026-02-27  
**버전**: Phase 5-1 상세 가이드
