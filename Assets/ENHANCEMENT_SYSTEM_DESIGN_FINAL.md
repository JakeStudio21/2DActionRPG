# 강화 시스템 최종 설계 (2개 SO 분리 구조)

## 🎯 **핵심 개념**

### **책임 분리 원칙**

```
1️⃣ EnhanceLevelTableSO → "강화 시도 자체의 규칙"
   - 비용 (골드, 재료)
   - 확률 (성공률)
   - 보너스 트리거

2️⃣ EnhanceCurveTableSO → "강화했을 때 얼마나 강해지는가"
   - 스탯 증가율
   - 성장 곡선

🔗 연결: 장비가 가진 enhancementCurveGroupId 로 처리
```

---

## 1️⃣ **EnhanceLevelTableSO** - 강화 시도 규칙

### **📊 데이터 구조**

```csharp
EnhanceLevelTableSO
 └─ levelTable[0~15]
    ├─ level              // 강화 단계 (0~15)
    ├─ goldCost           // 소모 골드
    ├─ materialCount      // 소모 강화 재료 개수
    ├─ successRate        // 성공 확률 (0.0~1.0)
    ├─ bonusId            // 구간 보너스 ID (BONUS_LV3, BONUS_LV6 등)
    └─ bonusDescription   // 보너스 설명 (Inspector 표시용)
```

### **📌 담당 역할**
- ✅ "이 레벨을 강화하려면 무엇이 얼마나 드는가"
- ✅ "강화 성공 확률은 얼마인가"
- ✅ "이 레벨에서 보너스가 열리는가"

### **❌ 포함하지 않음**
- ❌ 스탯 증가량
- ❌ 장비 타입
- ❌ 성장 곡선 정보

### **🔧 주요 메서드**

```csharp
// 레벨 데이터 조회
LevelData GetLevelData(int level)

// 비용 조회
int GetGoldCost(int level)
int GetMaterialCount(int level)

// 확률 조회
float GetSuccessRate(int level) // 0~100%

// 보너스 조회
bool HasBonus(int level)
string GetBonusId(int level)
```

---

## 2️⃣ **EnhanceCurveTableSO** - 성장 곡선

### **📊 데이터 구조**

```csharp
EnhanceCurveTableSO
 └─ curveGroups[]
    ├─ groupId            // 곡선 그룹 ID (CURVE_WEAPON, CURVE_ARMOR 등)
    ├─ curveName          // 인스펙터 표시용 이름
    └─ levelRanges[]
       ├─ startLevel      // 시작 강화 레벨 (예: 1)
       ├─ endLevel        // 끝 강화 레벨 (예: 5)
       └─ statRateAdd     // 레벨당 스탯 증가율 (%) (예: 1.5)
```

### **📌 담당 역할**
- ✅ "이 장비는 어떤 성장 곡선을 쓰는가"
- ✅ "해당 강화 레벨에서 얼마만큼 스탯이 증가하는가"
- ✅ "누적 스탯 증가량은 얼마인가"

### **❌ 포함하지 않음**
- ❌ 골드 / 재료 / 성공 확률
- ❌ 보너스 트리거 정보

### **🔧 주요 메서드**

```csharp
// 특정 레벨 스탯 증가율
float GetStatRateAdd(string groupId, int level)

// 누적 스탯 증가율 (0 ~ targetLevel)
float GetTotalStatBonus(string groupId, int targetLevel)

// 그룹 존재 확인
bool HasCurveGroup(string groupId)

// 모든 그룹 ID 목록
string[] GetAllGroupIds()
```

---

## 🔗 **연결 구조**

### **EquipmentData에 curveGroupId 추가**

```csharp
[Header("⚡ 강화 시스템")]
[Tooltip("강화 성장 곡선 ID (예: CURVE_WEAPON, CURVE_ARMOR, CURVE_ACCESSORY)")]
public string enhancementCurveGroupId = "CURVE_STANDARD";
```

### **연결 흐름**

```
1. 장비 선택
   └─ EquipmentData.enhancementCurveGroupId 읽기
       └─ "CURVE_WEAPON"

2. 강화 시도 (비용/확률)
   └─ EnhanceLevelTableSO.GetLevelData(10)
       └─ 골드: 4100G, 재료: 5개, 성공률: 58%

3. 스탯 증가율 (결과)
   └─ EnhanceCurveTableSO.GetStatRateAdd("CURVE_WEAPON", 10)
       └─ +2.0%
```

---

## 📊 **실제 데이터 예시**

### **EnhanceLevelTableSO**

| Lv | Gold | Material | Success | BonusId |
|----|------|----------|---------|---------|
| 0 | 0 | 0 | 100% | - |
| 1 | 200 | 1 | 95% | - |
| 2 | 350 | 1 | 92% | - |
| 3 | 550 | 2 | 88% | BONUS_LV3 |
| 4 | 800 | 2 | 85% | - |
| 5 | 1,100 | 3 | 82% | - |
| 6 | 1,500 | 3 | 75% | BONUS_LV6 |
| 7 | 2,000 | 4 | 72% | - |
| 8 | 2,600 | 4 | 68% | - |
| 9 | 3,300 | 5 | 62% | BONUS_LV9 |
| 10 | 4,100 | 5 | 58% | - |
| 11 | 5,200 | 6 | 50% | - |
| 12 | 6,600 | 6 | 45% | BONUS_LV12 |
| 13 | 8,300 | 7 | 40% | - |
| 14 | 10,500 | 7 | 35% | - |
| 15 | 13,500 | 8 | 30% | BONUS_LV15 |

---

### **EnhanceCurveTableSO**

#### **CURVE_WEAPON (무기 곡선)**

| 레벨 구간 | 스탯 증가율 | 누적 (Lv 10) |
|-----------|-------------|--------------|
| 1~5 | +1.5% / Lv | +7.5% |
| 6~10 | +2.0% / Lv | +17.5% |
| 11~15 | +3.0% / Lv | +32.5% |

#### **CURVE_ARMOR (방어구 곡선)**

| 레벨 구간 | 스탯 증가율 | 누적 (Lv 10) |
|-----------|-------------|--------------|
| 1~5 | +1.2% / Lv | +6.0% |
| 6~10 | +1.6% / Lv | +14.0% |
| 11~15 | +2.4% / Lv | +26.0% |

#### **CURVE_ACCESSORY (악세사리 곡선)**

| 레벨 구간 | 스탯 증가율 | 누적 (Lv 10) |
|-----------|-------------|--------------|
| 1~5 | +0.8% / Lv | +4.0% |
| 6~10 | +1.0% / Lv | +9.0% |
| 11~15 | +1.5% / Lv | +16.5% |

---

## 💻 **코드 사용 예시**

### **강화 시스템에서 사용**

```csharp
// 1. 장비 데이터에서 curveGroupId 읽기
string curveGroupId = equipmentData.enhancementCurveGroupId; // "CURVE_WEAPON"

// 2. 레벨별 비용 조회 (Level Table)
var levelTable = Resources.Load<EnhanceLevelTableSO>("Data/EnhanceLevelTable");
int goldCost = levelTable.GetGoldCost(targetLevel);
int materialCount = levelTable.GetMaterialCount(targetLevel);
float successRate = levelTable.GetSuccessRate(targetLevel);

// 3. 스탯 증가율 조회 (Curve Table)
var curveTable = Resources.Load<EnhanceCurveTableSO>("Data/EnhanceCurveTable");
float statRate = curveTable.GetStatRateAdd(curveGroupId, targetLevel);
float totalBonus = curveTable.GetTotalStatBonus(curveGroupId, targetLevel);

// 4. UI 표시
Debug.Log($"Lv {targetLevel} 강화:");
Debug.Log($"  비용: {goldCost}G, 재료: {materialCount}개");
Debug.Log($"  성공률: {successRate}%");
Debug.Log($"  스탯 증가: +{statRate}%");
Debug.Log($"  누적 스탯: +{totalBonus}%");
```

---

## 🎯 **분리 구조의 장점**

### **1. 재사용성 ⭐⭐⭐**

**동일한 비용/확률로 다양한 곡선 적용 가능:**

```
EnhanceLevelTableSO (공통)
└─ Lv 1~15 비용/성공률

EnhanceCurveTableSO
├─ CURVE_WEAPON (무기 전용)
├─ CURVE_ARMOR (방어구 전용)
├─ CURVE_ACCESSORY (악세사리 전용)
└─ CURVE_LEGENDARY (특수 장비 전용)
```

---

### **2. 밸런스 조정 용이 ⭐⭐⭐**

**곡선 조정 시:**
```
"초급 구간 스탯 증가량을 1.5% → 2.0%로 상향"
→ CURVE_WEAPON의 levelRanges[0].statRateAdd만 수정
→ 1번 수정으로 모든 무기에 자동 반영!
```

**비용 조정 시:**
```
"Lv 10 강화 비용을 4100G → 3000G로 하향"
→ EnhanceLevelTableSO의 levelTable[10].goldCost만 수정
→ 모든 장비에 자동 반영!
```

---

### **3. 확장성 ⭐⭐⭐**

#### **이벤트 강화 테이블**
```
EnhanceLevelTableSO_Event
└─ 성공률 +10%, 골드 -30%

EnhanceCurveTableSO (공통)
└─ 동일한 곡선 재사용!
```

#### **특수 무기 타입**
```
EnhanceLevelTableSO (공통)
└─ 비용/확률 동일

EnhanceCurveTableSO
└─ CURVE_WEAPON_SWORD (검 전용)
    └─ 1.7% → 2.3% → 3.5% (더 높은 증가율)
```

---

### **4. 데이터 중복 제거 ⭐⭐**

**기존 구조 (통합):**
```
Lv 1~5 각각에 statRateAdd = 1.5 입력
→ 5번 중복 입력!
```

**분리 구조:**
```
CURVE_WEAPON
└─ LevelRange { start: 1, end: 5, rate: 1.5 }
→ 1번만 입력!
```

---

## 🔧 **Unity에서 사용하기**

### **Step 1: 테이블 생성**

```
Unity 상단 메뉴 → Tools → Workshop → Generate Enhancement Tables

1. "✨ 강화 테이블 생성 (Lv 0~15)" 버튼 클릭
2. 두 개의 SO 파일 자동 생성:
   - Assets/Resources/Data/EnhanceLevelTable.asset
   - Assets/Resources/Data/EnhanceCurveTable.asset

3. "🧪 데이터 검증" 버튼 클릭 (선택)
   - Console 창에서 데이터 확인
```

---

### **Step 2: 장비에 곡선 ID 설정**

```
1. Project 창 → Assets/Resources/EquipmentData/무기 폴더
2. Sword_A.asset 선택
3. Inspector 창:
   ⚡ 강화 시스템
   └─ Enhancement Curve Group Id: "CURVE_WEAPON"

4. 다른 장비도 설정:
   - 무기: "CURVE_WEAPON"
   - 방어구: "CURVE_ARMOR"
   - 악세사리: "CURVE_ACCESSORY"
```

---

### **Step 3: 게임에서 사용**

강화 시스템이 자동으로 두 테이블을 참조합니다:
- 비용/확률: EnhanceLevelTableSO
- 스탯 증가: EnhanceCurveTableSO + equipmentData.enhancementCurveGroupId

---

## 📋 **파일 구조**

```
Assets/
├─ Scripts/
│  └─ Systems/
│     ├─ EnhanceLevelTableSO.cs ⭐ 신규
│     └─ EnhanceCurveTableSO.cs ⭐ 신규
│
├─ Editor/
│  └─ EnhancementTableAutoGenerator.cs ⭐ 신규
│
└─ Resources/
   └─ Data/
      ├─ EnhanceLevelTable.asset ⭐ 자동 생성
      └─ EnhanceCurveTable.asset ⭐ 자동 생성
```

---

## ✅ **완료 체크리스트**

- [x] EnhanceLevelTableSO 클래스 생성
- [x] EnhanceCurveTableSO 클래스 생성
- [x] EquipmentData에 enhancementCurveGroupId 추가
- [x] Editor Tool 생성 (자동 데이터 입력)
- [ ] Unity에서 테이블 생성 (Tools 메뉴)
- [ ] 장비에 curveGroupId 설정
- [ ] EnhancementSystem에서 새 SO 사용
- [ ] EnhancementUI에서 곡선 기반 스탯 표시
- [ ] 게임 테스트

---

## 🎯 **다음 단계**

### **Unity 작업 (5분)**
```
Tools → Workshop → Generate Enhancement Tables
→ 테이블 자동 생성
→ 검증
```

### **EnhancementSystem 연동 (15분)**
```
기존 EnhancementData → 새로운 두 SO 전환
- EnhanceLevelTableSO: 비용/확률
- EnhanceCurveTableSO: 스탯 증가율
```

### **EnhancementUI 연동 (10분)**
```
equipmentData.enhancementCurveGroupId 기반
스탯 증가율 표시
```

---

**작성일**: 2026-02-09  
**버전**: 3.0 (Final - 2개 SO 분리)  
**파일**: `Assets/ENHANCEMENT_SYSTEM_DESIGN_FINAL.md`

