# 바리케이드 시스템 설정 가이드

## 📋 **목차**
1. [프리셋 에셋 생성](#step-6-프리셋-에셋-생성)
2. [프리팹 생성 및 설정](#step-7-프리팹-생성-및-설정)
3. [씬 배치 및 테스트](#step-8-씬-배치-및-테스트)

---

## 🚀 **Step 6: 프리셋 에셋 생성** (10분)

### **1. 프리셋 폴더 생성**

Unity Project 창에서:
```
Assets/Resources/Data/Barricade/Presets/ 폴더 생성
```

### **2. 프리셋 5개 생성**

각 프리셋을 생성하는 방법:
1. Project 창에서 우클릭
2. `Create → Game → Barricade → Preset` 선택
3. 아래 설정값 입력

---

### **프리셋 1: Preset_Simple_Vine** (반복 덩쿨용)

**파일명:** `Preset_Simple_Vine.asset`

```
==== 프리셋 정보 ====
Preset Name: 간단한 덩쿨
Description: 1타로 파괴되는 반복 배치용 덩쿨
Category: Normal

==== 파괴 모드 ====
Break Mode: Hits
Hits To Break: 1

==== 시각적 단계 ====
Stage Mode: None

==== 연출 레벨 ====
Presentation Level: 0 (조용히 파괴)

==== HP/타수 표시 ====
Hp Bar Mode: Off

==== 피격 피드백 ====
Hit Sound: (null)
Hit VFX: (null)
Hit Shake Intensity: 0

==== 파괴 피드백 ====
Destroy Sound: (null)
Destroy VFX: (null)
Use Big Destroy Fx: false
Destroy Shake Intensity: 0

==== 피로도 완화 설정 ====
Camera Shake Cooldown: 0.5
Big Fx Cooldown: 2
Limit Consecutive Playback: true

==== 보상 모드 ====
Reward Mode: None

==== 메시지 ====
Show Message On Break: false

==== 스테이지 연동 ====
Unlock Area On Destroy: false

==== 파편 효과 ====
Spawn Debris: false
```

---

### **프리셋 2: Preset_Normal_Wood** (일반 바리케이드)

**파일명:** `Preset_Normal_Wood.asset`

```
==== 프리셋 정보 ====
Preset Name: 일반 나무 바리케이드
Description: 3타로 파괴되는 기본 바리케이드
Category: Normal

==== 파괴 모드 ====
Break Mode: Hits
Hits To Break: 3

==== 시각적 단계 ====
Stage Mode: Simple
Visual Stages: (3개 설정)
  - Stage 0: HP Threshold = 1.0, Sprite = (정상 스프라이트)
  - Stage 1: HP Threshold = 0.66, Sprite = (균열1 스프라이트)
  - Stage 2: HP Threshold = 0.33, Sprite = (균열2 스프라이트)

==== 연출 레벨 ====
Presentation Level: 1 (최소 연출)

==== HP/타수 표시 ====
Hp Bar Mode: Small
Show Remaining Hits: true

==== 피격 피드백 ====
Hit Sound: (나무 피격 사운드)
Hit VFX: (작은 파티클)
Hit Shake Intensity: 0.05

==== 파괴 피드백 ====
Destroy Sound: (나무 파괴 사운드)
Destroy VFX: (파괴 이펙트)
Use Big Destroy Fx: false
Destroy Shake Intensity: 0.1

==== 보상 모드 ====
Reward Mode: Normal

==== 파편 효과 ====
Spawn Debris: true
Debris Count: 5
```

---

### **프리셋 3: Preset_Important_Stone** (중요 바리케이드)

**파일명:** `Preset_Important_Stone.asset`

```
==== 프리셋 정보 ====
Preset Name: 중요 돌 바리케이드
Description: 5타로 파괴되는 키 오브젝트
Category: Important

==== 파괴 모드 ====
Break Mode: Hits
Hits To Break: 5

==== 시각적 단계 ====
Stage Mode: Simple
Visual Stages: (3개 설정)

==== 연출 레벨 ====
Presentation Level: 2 (표준 연출)

==== HP/타수 표시 ====
Hp Bar Mode: OnlyImportant
Show Remaining Hits: true

==== 피격 피드백 ====
Hit Sound: (돌 피격 사운드)
Hit VFX: (파티클)
Hit Shake Intensity: 0.1

==== 파괴 피드백 ====
Destroy Sound: (돌 파괴 사운드)
Destroy VFX: (파괴 이펙트)
Use Big Destroy Fx: false
Destroy Shake Intensity: 0.15

==== 보상 모드 ====
Reward Mode: Special
Reward Table: (BarricadeRewardTable 할당)

==== 메시지 ====
Show Message On Break: true
Break Message: "중요한 장애물을 제거했습니다!"
Message Duration: 2

==== 파편 효과 ====
Spawn Debris: true
Debris Count: 8
```

---

### **프리셋 4: Preset_Boss_Metal** (보스룸 입구)

**파일명:** `Preset_Boss_Metal.asset`

```
==== 프리셋 정보 ====
Preset Name: 보스룸 금속 바리케이드
Description: HP 300, 보스룸 입구 차단
Category: Boss

==== 파괴 모드 ====
Break Mode: HP
Max HP: 300
Show Damage Numbers: true

==== 시각적 단계 ====
Stage Mode: Advanced
Visual Stages: (5개 설정)
  - Stage 0: HP Threshold = 1.0, Sprite = (정상)
  - Stage 1: HP Threshold = 0.8, Sprite = (균열1)
  - Stage 2: HP Threshold = 0.6, Sprite = (균열2)
  - Stage 3: HP Threshold = 0.4, Sprite = (균열3)
  - Stage 4: HP Threshold = 0.2, Sprite = (파괴 직전)

==== 연출 레벨 ====
Presentation Level: 3 (풀 연출)

==== HP/타수 표시 ====
Hp Bar Mode: Full
Show Remaining Hits: false

==== 피격 피드백 ====
Hit Sound: (금속 피격 사운드)
Hit VFX: (불꽃 파티클)
Hit Shake Intensity: 0.1

==== 파괴 피드백 ====
Destroy Sound: (폭발 사운드)
Destroy VFX: (큰 폭발 이펙트)
Use Big Destroy Fx: true
Destroy Shake Intensity: 0.3

==== 피로도 완화 설정 ====
Camera Shake Cooldown: 0.5
Big Fx Cooldown: 2
Limit Consecutive Playback: true

==== 보상 모드 ====
Reward Mode: Special
Reward Table: (특수 보상 테이블)

==== 메시지 ====
Show Message On Break: true
Break Message: "보스룸 입구가 열렸습니다!"
Message Duration: 3

==== 스테이지 연동 ====
Unlock Area On Destroy: true
Unlock Area ID: "BOSS_ROOM"

==== 파편 효과 ====
Spawn Debris: true
Debris Count: 15
```

---

### **프리셋 5: Preset_Secret_Magic** (비밀 구역)

**파일명:** `Preset_Secret_Magic.asset`

```
==== 프리셋 정보 ====
Preset Name: 마법 바리케이드
Description: 특별한 열쇠가 필요한 비밀 구역
Category: Secret

==== 파괴 모드 ====
Break Mode: Condition
Condition Type: SpecialKey
Required Item ID: "MAGIC_KEY"
Condition Message: "마법의 열쇠가 필요합니다"

==== 시각적 단계 ====
Stage Mode: Simple
Visual Stages: (1개, 고정 스프라이트)

==== 연출 레벨 ====
Presentation Level: 3 (풀 연출)

==== HP/타수 표시 ====
Hp Bar Mode: Off

==== 파괴 피드백 ====
Destroy Sound: (마법 해제 사운드)
Destroy VFX: (마법 이펙트)
Use Big Destroy Fx: true
Destroy Shake Intensity: 0.2

==== 보상 모드 ====
Reward Mode: Special
Reward Table: (특수 보상 테이블)

==== 메시지 ====
Show Message On Break: true
Break Message: "비밀의 방이 열렸습니다!"
Message Duration: 3

==== 스테이지 연동 ====
Unlock Area On Destroy: true
Unlock Area ID: "SECRET_ROOM"
```

---

## 🚀 **Step 7: 프리팹 생성 및 설정** (10분)

### **1. BarricadeHPDisplay UI 프리팹 생성**

#### **Canvas 설정:**
1. Hierarchy에서 우클릭 → `Create Empty`
2. 이름: `BarricadeHPDisplay`
3. `Add Component` → `Canvas`
   - Render Mode: **World Space**
   - Width: 100
   - Height: 30
4. `Add Component` → `BarricadeHPDisplay` 스크립트

#### **UI 구조 생성:**
```
BarricadeHPDisplay (Canvas)
├─ HPBarRoot (Image - 배경)
│  └─ Fill (Image - 채움, Fill 타입)
└─ HitsText (Text - "3")
```

**HPBarRoot 설정:**
- Width: 80, Height: 10
- Color: 검은색 반투명 (0, 0, 0, 0.5)

**Fill 설정:**
- Anchor: Stretch (좌우)
- Image Type: **Filled**
- Fill Method: Horizontal
- Fill Amount: 1.0
- Color: 초록색 (0, 1, 0, 1)

**HitsText 설정:**
- Font Size: 20
- Alignment: Center/Middle
- Color: 흰색

**BarricadeHPDisplay 스크립트 Inspector:**
- HP Bar Root: HPBarRoot 할당
- Fill Image: Fill 할당
- Hits Text: HitsText 할당

프리팹 저장: `Assets/Prefabs/UI/BarricadeHPDisplay.prefab`

---

### **2. Barricade 베이스 프리팹 생성**

#### **GameObject 생성:**
1. Hierarchy에서 우클릭 → `2D Object → Sprite`
2. 이름: `Barricade_Base`

#### **컴포넌트 추가:**
1. `Add Component` → `Box Collider 2D`
   - Is Trigger: **false** (물리 충돌)
2. `Add Component` → `Barricade` 스크립트
3. `Add Component` → `PickUpSpawner` 스크립트 (선택)

#### **자식 오브젝트 추가:**
1. Barricade_Base 우클릭 → `Create Empty`
2. 이름: `HPDisplay`
3. `Assets/Prefabs/UI/BarricadeHPDisplay.prefab` 드래그
4. Position: (0, 1, 0) - 바리케이드 위에 표시

프리팹 저장: `Assets/Prefabs/Barricade_Base.prefab`

---

### **3. 프리셋별 바리케이드 프리팹 생성**

각 프리팹은 `Barricade_Base`를 복제하여 생성:

#### **Barricade_Vine.prefab** (반복 덩쿨)
- Sprite: 덩쿨 이미지
- Barricade 스크립트:
  - **Preset: Preset_Simple_Vine**
- Collider: 작게 조정

#### **Barricade_Wood_Normal.prefab** (일반)
- Sprite: 나무 바리케이드 이미지
- Barricade 스크립트:
  - **Preset: Preset_Normal_Wood**
- PickUpSpawner:
  - Health Drop Chance: 30%
  - Gold Drop Chance: 70%

#### **Barricade_Stone_Important.prefab** (중요)
- Sprite: 돌 바리케이드 이미지
- Barricade 스크립트:
  - **Preset: Preset_Important_Stone**
- PickUpSpawner:
  - Gold: 50~100
  - Equipment Drop Chance: 30%

#### **Barricade_Boss_Entrance.prefab** (보스룸)
- Sprite: 금속 문 이미지
- Barricade 스크립트:
  - **Preset: Preset_Boss_Metal**
- PickUpSpawner:
  - Gold: 100~200
  - Equipment: S급 장비

#### **Barricade_Secret_Magic.prefab** (비밀)
- Sprite: 마법 장벽 이미지
- Barricade 스크립트:
  - **Preset: Preset_Secret_Magic**

---

## 🚀 **Step 8: 씬 배치 및 테스트** (7분)

### **1. 매니저 오브젝트 배치**

#### **BarricadeFeedbackManager:**
1. Hierarchy에서 우클릭 → `Create Empty`
2. 이름: `BarricadeFeedbackManager`
3. `Add Component` → `BarricadeFeedbackManager` 스크립트

#### **CameraShake:**
1. Main Camera 선택
2. `Add Component` → `CameraShake` 스크립트

---

### **2. 테스트 씬 구성**

```
[Stage_Barricade_Test]

[플레이어 스폰]
    ↓
[구역 1: 반복 피로도 테스트]
├─ Vine × 10 (Barricade_Vine)
│   → 1타씩, 조용히 파괴
└─ 결과: 피로도 없이 빠르게 정리

    ↓
[구역 2: 일반 바리케이드]
├─ Wood × 3 (Barricade_Wood_Normal)
│   → 3타, 최소 연출
└─ 골드/포션 드롭

    ↓
[구역 3: 중요 바리케이드]
└─ Stone_Important (Barricade_Stone_Important)
    → 5타, 표준 연출
    → HP 바 표시
    → 특수 보상

    ↓
[구역 4: 보스룸 입구]
└─ Boss_Metal (Barricade_Boss_Entrance)
    → HP 300, 풀 연출
    → 보스룸 문 해금

[비밀 경로]
└─ Secret_Magic (Barricade_Secret_Magic)
    → 열쇠 필요
    → 비밀방 해금
```

---

### **3. 테스트 체크리스트**

#### **반복 덩쿨 (Vine):**
- [ ] 1타로 즉시 파괴
- [ ] 사운드/VFX 없음 (조용)
- [ ] 10개 연속 파괴해도 피로 없음
- [ ] HP 바 표시 안 됨

#### **일반 바리케이드 (Wood):**
- [ ] 3타로 파괴
- [ ] 피격 시 작은 사운드
- [ ] 단계별 스프라이트 변경 (정상 → 균열1 → 균열2)
- [ ] HP 바 숫자 표시 (3 → 2 → 1)
- [ ] 골드/포션 드롭

#### **중요 바리케이드 (Stone):**
- [ ] 5타로 파괴
- [ ] HP 바 표시 (OnlyImportant 모드)
- [ ] 파괴 시 메시지 표시
- [ ] 특수 보상 드롭

#### **보스룸 바리케이드 (Metal):**
- [ ] HP 300 표시
- [ ] 데미지 숫자 표시
- [ ] 5단계 스프라이트 변경
- [ ] 파괴 시 카메라 큰 흔들림
- [ ] 큰 폭발 이펙트
- [ ] "보스룸 입구가 열렸습니다!" 메시지
- [ ] 보스룸 문 활성화

#### **비밀 바리케이드 (Magic):**
- [ ] 열쇠 없이 공격 → "마법의 열쇠가 필요합니다" 메시지
- [ ] 열쇠 획득 후 공격 → 파괴
- [ ] 마법 이펙트 재생
- [ ] "비밀의 방이 열렸습니다!" 메시지

#### **피로도 완화 시스템:**
- [ ] 연속 파괴 시 카메라 흔들림 쿨다운 작동
- [ ] 큰 이펙트 연속 재생 제한 (3회)
- [ ] 사운드 쿨다운 작동

---

### **4. 디버그 로그 확인**

Unity Console에서 확인할 로그:
```
[Barricade] Barricade_Wood_001 초기화 시작 (프리셋: 일반 나무 바리케이드)
[Barricade] Hits 모드 초기화: 3타 필요
[Barricade] Barricade_Wood_001 피격! 남은 타수: 2/3
[Barricade] 단계 변경: 균열1 (HP: 66%)
[BarricadeFeedbackManager] 사운드 재생: WoodHit
[Barricade] 파괴됨: Barricade_Wood_001
[BarricadeFeedbackManager] 카메라 흔들림 실행 (강도: 0.1, 시간: 0.3초)
```

---

## ✅ **완료!**

이제 프리셋 기반 바리케이드 시스템이 완전히 구축되었습니다!

### **작업자 워크플로우:**
1. 새 바리케이드 필요 → `Barricade_Base` 복제
2. Preset 필드에 원하는 프리셋 드래그 → 완료! (10초)
3. 스프라이트만 교체하면 끝!

### **다음 단계:**
- [ ] 실제 스프라이트 에셋 추가
- [ ] 사운드 에셋 추가
- [ ] VFX 프리팹 추가
- [ ] 보상 테이블 설정
- [ ] 스테이지에 배치

---

## 📚 **참고 자료**

### **프리셋 수정 시:**
- Unity Project 창에서 프리셋 에셋 선택
- Inspector에서 값 수정
- 저장 → **모든 해당 바리케이드에 즉시 적용!**

### **새 프리셋 추가 시:**
1. 기존 프리셋 복제 (Ctrl+D)
2. 값 수정
3. 새 바리케이드 프리팹에 할당

### **문제 해결:**
- HP 바 안 보임 → HpBarMode 확인
- 카메라 안 흔들림 → CameraShake 컴포넌트 확인
- 보상 안 드롭 → RewardMode 확인

