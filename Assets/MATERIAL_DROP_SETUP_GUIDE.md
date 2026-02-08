# 📦 재료 드롭 시스템 설정 가이드

> **완료 날짜**: 2026-02-08  
> **구현 방식**: 데이터 기반 (DropTable 연동)

---

## 🎯 시스템 개요

### **핵심 특징**
- ✅ **데이터 기반**: `DropTable` ScriptableObject로 모든 드롭 정의
- ✅ **단일 프리팹**: `MaterialPickup` 범용 프리팹 1개만 사용
- ✅ **동적 스프라이트**: `MaterialData`의 `icon`을 런타임에 할당
- ✅ **itemId 매핑**: `MAT_WEAPON_FRAGMENT` 같은 itemId로 재료 식별
- ✅ **기존 시스템 통합**: 골드/하트/장비와 동일한 드롭 흐름

---

## 📋 구현 단계

### **Phase 1: Drop_Material Prefab 생성** ✅

#### **Step 1-1: Prefab 생성**

1. **Hierarchy 창에서**:
   - 빈 GameObject 생성: `Drop_Material`
   
2. **Inspector에서 컴포넌트 추가**:
   - `SpriteRenderer` 추가
   - `Rigidbody2D` 추가
     - Body Type: `Dynamic`
     - Gravity Scale: `0.5` (포물선 낙하)
   - `CircleCollider2D` 추가
     - Is Trigger: ✅ 체크
     - Radius: `0.5`
   - `MaterialPickup` 스크립트 추가

3. **SpriteRenderer 설정**:
   - Sprite: 비워둠 (런타임에 할당)
   - Color: 흰색 (255, 255, 255, 255)
   - Sorting Layer: `Items`
   - Order in Layer: `10`

4. **MaterialPickup 스크립트 설정**:
   - Sprite Renderer: 자동 할당됨
   - Collider: 자동 할당됨
   - Bounce Height: `0.5`
   - Bounce Duration: `0.6`

5. **Prefab 저장**:
   - Project 창에서 폴더 생성: `Assets/Resources/Prefabs/Items/`
   - Hierarchy의 `Drop_Material`을 드래그하여 Prefab 생성
   
> **⭐ 네이밍 규칙**: `Drop_Currency`, `Drop_Equipment`와 동일한 규칙 사용

---

### **Phase 2: ScenePoolConfig 등록** ✅

#### **Step 2-1: Lobby 씬 설정**

1. **Lobby 씬 열기**: `Assets/Scenes/Lobby.unity`

2. **Hierarchy에서 `GamePoolManager` 찾기**

3. **Inspector → ScenePoolConfig → Pool List** 확장

4. **새 Pool 추가**:
   - Size: `+1` 클릭
   - **Tag**: `Drop_Material` ⭐
   - **Prefab**: `Drop_Material` Prefab 드래그
   - **Initial Size**: `20`
   - **Max Size**: `50`
   - **Auto Expand**: ✅ 체크

5. **저장**: `Ctrl+S`

#### **Step 2-2: 스테이지 씬 설정**

- `Assets/Scenes/Stage_001.unity` (또는 다른 스테이지 씬)
- 위와 동일한 방식으로 `Drop_Material` Pool 추가

---

### **Phase 3: DropTable 설정** ✅

#### **Step 3-1: DropTable 수정**

1. **Project 창에서** `Assets/Resources/Data/DropTables/` 경로

2. **기존 DropTable 열기** (예: `DT_BlueSlime.asset`)

3. **Inspector → Drop Entries** 확장

4. **재료 아이템 추가**:

**예시 1: BlueSlime (Lv.1 기본 몬스터)**
```
Drop Entry #4:
- Item ID: MAT_WEAPON_FRAGMENT
- Drop Chance: 30.0
- Quantity Range: Min 1, Max 3
- Rarity: Common
- Stage Level: 1
```

**예시 2: Grape (Lv.3 원거리 몬스터)**
```
Drop Entry #5:
- Item ID: MAT_ARMOR_CRYSTAL
- Drop Chance: 15.0
- Quantity Range: Min 1, Max 2
- Rarity: Uncommon
- Stage Level: 3
```

**예시 3: Elite_SandGolem (엘리트 몬스터)**
```
Drop Entry #6:
- Item ID: MAT_ACCESSORY_CORE
- Drop Chance: 5.0
- Quantity Range: Min 1, Max 1
- Rarity: Rare
- Stage Level: 5
```

5. **저장**: `Ctrl+S`

#### **Step 3-2: 새 DropTable 생성**

1. **Project 창**: `Assets/Resources/Data/DropTables/` 우클릭
2. **Create → ScriptableObjects → DropTable**
3. **이름 설정**: `DT_NewMonster`
4. **위와 동일한 방식으로 Drop Entries 설정**

---

## 🗂️ itemId 매핑 테이블

> **⭐ JSON 외부 연동 안전**: MaterialData에 `materialId` 필드가 명시적으로 저장되어 있어, DropTable의 `itemId`와 직접 매칭됩니다.

### **무기 재료**
| itemId (DropTable) | materialId (SO) | MaterialType (Enum) | 표시명 | 등급 |
|--------|--------|--------------|--------|------|
| `MAT_WEAPON_FRAGMENT` | `MAT_WEAPON_FRAGMENT` | WeaponFragment | 무기 강화 파편 | Common |
| `MAT_WEAPON_CRYSTAL` | `MAT_WEAPON_CRYSTAL` | WeaponCrystal | 무기 강화 결정 | Uncommon |
| `MAT_WEAPON_CORE` | `MAT_WEAPON_CORE` | WeaponCore | 무기 강화 코어 | Rare |

### **방어구 재료**
| itemId (DropTable) | materialId (SO) | MaterialType (Enum) | 표시명 | 등급 |
|--------|--------|--------------|--------|------|
| `MAT_ARMOR_FRAGMENT` | `MAT_ARMOR_FRAGMENT` | ArmorFragment | 방어구 강화 파편 | Common |
| `MAT_ARMOR_CRYSTAL` | `MAT_ARMOR_CRYSTAL` | ArmorCrystal | 방어구 강화 결정 | Uncommon |
| `MAT_ARMOR_CORE` | `MAT_ARMOR_CORE` | ArmorCore | 방어구 강화 코어 | Rare |

### **악세사리 재료**
| itemId (DropTable) | materialId (SO) | MaterialType (Enum) | 표시명 | 등급 |
|--------|--------|--------------|--------|------|
| `MAT_ACCESSORY_FRAGMENT` | `MAT_ACCESSORY_FRAGMENT` | AccessoryFragment | 악세사리 강화 파편 | Common |
| `MAT_ACCESSORY_CRYSTAL` | `MAT_ACCESSORY_CRYSTAL` | AccessoryCrystal | 악세사리 강화 결정 | Uncommon |
| `MAT_ACCESSORY_CORE` | `MAT_ACCESSORY_CORE` | AccessoryCore | 악세사리 강화 코어 | Rare |

### **기타 재료**
| itemId (DropTable) | materialId (SO) | MaterialType (Enum) | 표시명 | 등급 |
|--------|--------|--------------|--------|------|
| `MAT_CRAFTING_ESSENCE` | `MAT_CRAFTING_ESSENCE` | CraftingEssence | 제작 정수 | Uncommon |
| `ITEM_GOLD` | `ITEM_GOLD` | Gold | 골드 | Common |

---

## 🔍 디버깅 가이드

### **테스트 1: 재료 드롭 확인**

**실행 방법**:
1. Unity Editor 실행
2. Lobby → Stage 진입
3. 몬스터 처치

**정상 로그**:
```
📦 [EnemyHealth] 재료 드롭 성공: 무기 강화 파편 x3
📦 [EnemyHealth] 재료 드롭 성공: 방어구 강화 결정 x1
```

**에러 시나리오**:
- ❌ `'Drop_Material' 풀에서 오브젝트를 스폰할 수 없습니다!`
  - **원인**: ScenePoolConfig에 Drop_Material 등록 안 됨
  - **해결**: Phase 2 다시 확인
  
- ❌ `MaterialData를 찾을 수 없습니다: WeaponFragment`
  - **원인**: MaterialDatabase에 WeaponFragment 등록 안 됨
  - **해결**: `Tools → Create Material Database` 실행

- ❌ `알 수 없는 재료 itemId: MAT_UNKNOWN`
  - **원인**: DropTable의 itemId가 잘못됨
  - **해결**: 위 매핑 테이블 참고하여 수정

---

### **테스트 2: 재료 획득 확인**

**실행 방법**:
1. 드롭된 재료 아이템에 플레이어 충돌
2. Console 로그 확인

**정상 로그**:
```
✅ [MaterialPickup] 플레이어가 재료 획득: 무기 강화 파편 x3
✅ [AccountDataManager] 재료 추가: WeaponFragment +3 (총: 3개)
```

---

### **테스트 3: 인벤토리 표시 확인**

**실행 방법**:
1. Lobby로 복귀
2. 가방 버튼 클릭
3. "재료" 탭 클릭

**정상 동작**:
- ✅ 재료 아이콘 표시됨
- ✅ 수량 표시 ("X3" 형식)
- ✅ 등급별 테두리 색상 적용

---

## 🛠️ 트러블슈팅

### **문제 1: 재료가 드롭되지 않음**

**체크리스트**:
1. DropTable에 `MAT_*` itemId 추가했는지 확인
2. Drop Chance가 0%가 아닌지 확인
3. EnemyData에 DropGroupId 설정되었는지 확인

---

### **문제 2: 재료 아이콘이 표시되지 않음**

**체크리스트**:
1. MaterialDatabase에 해당 MaterialType 등록되었는지 확인
2. MaterialData의 `icon` 필드에 Sprite 할당되었는지 확인
3. `Tools → Validate Material Database` 실행하여 검증

---

### **문제 3: 재료 수량이 증가하지 않음**

**체크리스트**:
1. AccountDataManager가 초기화되었는지 확인
2. MaterialPickup의 `OnTriggerEnter2D`가 호출되는지 로그 확인
3. Player의 Layer가 "Player"인지 확인

---

## ✅ 최종 체크리스트

### **Phase 1: Prefab**
- [ ] Drop_Material Prefab 생성 ⭐
- [ ] SpriteRenderer, Rigidbody2D, CircleCollider2D 추가
- [ ] MaterialPickup 스크립트 추가
- [ ] Sorting Layer: Items, Order: 10

### **Phase 2: Pool 등록**
- [ ] Lobby 씬에 Drop_Material Pool 추가 ⭐
- [ ] Stage 씬들에 Drop_Material Pool 추가 ⭐
- [ ] Initial Size: 20, Max Size: 50

### **Phase 3: DropTable 설정**
- [ ] 기존 DropTable에 재료 항목 추가
- [ ] itemId를 매핑 테이블대로 입력
- [ ] Drop Chance 설정 (5~30% 권장)

### **Phase 4: 테스트**
- [ ] 몬스터 처치 시 재료 드롭 확인
- [ ] 재료 획득 시 AccountDataManager 업데이트 확인
- [ ] 인벤토리 재료 탭에서 표시 확인

---

## 🎯 추천 드롭 확률

### **일반 몬스터 (Lv.1~3)**
- 파편 (Fragment): 30%
- 결정 (Crystal): 10%
- 코어 (Core): 3%

### **엘리트 몬스터 (Lv.5~7)**
- 파편 (Fragment): 50%
- 결정 (Crystal): 25%
- 코어 (Core): 10%

### **보스 몬스터 (Lv.10)**
- 결정 (Crystal): 80%
- 코어 (Core): 50%
- 제작 정수: 30%

---

## 📌 참고 파일

- `Assets/Scripts/Items/MaterialPickup.cs` - 재료 픽업 스크립트
- `Assets/Scripts/Systems/MaterialType.cs` - itemId 매핑 메서드
- `Assets/Scripts/Enemies/Combat/EnemyHealth.cs` - 드롭 시스템
- `Assets/Scripts/Items/DropTable.cs` - 드롭 테이블 구조
- `Assets/Scripts/Systems/MaterialDatabase.cs` - 재료 데이터베이스

---

**🎉 설정 완료!** 이제 모든 몬스터가 재료를 드롭할 수 있습니다.

