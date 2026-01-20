# CompositeCollider2D 적용 가이드

## 🎯 목적

모듈 프리팹 경계에서 플레이어/몬스터가 **걸리는 현상을 제거**하여 부드러운 이동 제공

---

## 📋 방법 선택 가이드

| 방법 | 난이도 | 속도 | 권장 대상 |
|------|--------|------|----------|
| **방법 1: 수동 설정** | ⭐⭐⭐ 어려움 | 느림 | 콜라이더 세밀 조정 필요 |
| **방법 2: 자동 스크립트** | ⭐ 쉬움 | 빠름 | 대량 모듈 일괄 처리 (권장) |
| **방법 3: Tilemap 자동** | ⭐⭐ 보통 | 보통 | Tilemap 사용 시 |

---

## 🚀 방법 1: 수동 설정 (세밀한 제어)

### **Step 1: 프리팹에 BoxCollider2D 추가**

#### **1-1. 프리팹 열기**
```
Project → Assets/Prefabs/Modules/Sample/Md_Sample_Room_08x12_A 더블클릭
```

#### **1-2. Colliders 계층 설정**
```
Md_Sample_Room_08x12_A (Root)
└─ Colliders (GameObject)
   ├─ Wall_North (빈 GameObject)
   ├─ Wall_South
   ├─ Wall_East
   └─ Wall_West
```

#### **1-3. 각 벽에 BoxCollider2D 추가**

**Wall_North:**
1. `Wall_North` GameObject 선택
2. **Add Component** → `Box Collider 2D`
3. **Inspector 설정:**
   - Size X: `8` (방 가로 길이)
   - Size Y: `0.5` (벽 두께)
   - Offset X: `0`
   - Offset Y: `6` (방 높이 절반)
   - ✅ **Used By Composite 체크** ⭐ 중요!

**Wall_South:**
- Size: `(8, 0.5)`
- Offset: `(0, -6)`
- ✅ **Used By Composite**

**Wall_East:**
- Size: `(0.5, 12)` (벽 두께 × 방 세로 길이)
- Offset: `(4, 0)` (방 가로 절반)
- ✅ **Used By Composite**

**Wall_West:**
- Size: `(0.5, 12)`
- Offset: `(-4, 0)`
- ✅ **Used By Composite**

#### **1-4. 프리팹 저장**
```
Ctrl + S (저장)
```

---

### **Step 2: Scene에 ColliderMap 생성**

#### **2-1. 빈 GameObject 생성**
```
Hierarchy 우클릭 → Create Empty → 이름: ColliderMap
```

#### **2-2. Rigidbody2D 추가**
1. `ColliderMap` 선택
2. **Add Component** → `Rigidbody 2D`
3. **Inspector 설정:**
   - **Body Type: Static** ⭐
   - Simulated: ✓

#### **2-3. CompositeCollider2D 추가**
1. **Add Component** → `Composite Collider 2D`
2. **Inspector 설정:**
   - **Geometry Type: Polygons** (권장)
   - Generation Type: Synchronous
   - Vertex Distance: 0.0005

---

### **Step 3: 모듈을 ColliderMap 하위로 이동**

#### **3-1. 모듈 드래그**
```
Hierarchy:
- Md_Sample_Room_08x12_A (드래그)
  ↓
- ColliderMap (드롭)

결과:
ColliderMap
└─ Md_Sample_Room_08x12_A
```

#### **3-2. 다른 모듈도 반복**
```
ColliderMap
├─ Md_Sample_Room_08x12_A
├─ Md_Sample_Room_12x12_A
└─ Md_Sample_Corridor_04x08_A
```

#### **3-3. 자동 병합 확인**
- Scene View에서 초록색 콜라이더 라인 확인
- 경계선이 **하나로 연결된** 것 확인 ✅

---

## ⚡ 방법 2: 자동 스크립트 (권장)

### **Unity Editor Tool 사용:**

#### **Step 1: 프리팹에 BoxCollider2D 추가 (수동)**
- 위의 방법 1 Step 1과 동일
- **Used By Composite는 체크 안 해도 됨** (자동 설정)

#### **Step 2: 자동 설정 실행**

1. **Unity Editor 메뉴:**
   ```
   Tools → Module System → Setup Composite Collider
   ```

2. **설정:**
   - Module Prefix: `Md_`
   - Collider Map Name: `ColliderMap`
   - ✅ Create ColliderMap
   - ✅ Set 'Used By Composite'
   - Geometry Type: `Polygons`

3. **실행:**
   ```
   Setup Composite Collider 버튼 클릭
   ```

4. **완료!**
   - ColliderMap 자동 생성
   - 모든 모듈 자동 이동
   - 모든 BoxCollider2D 자동 설정 ✅

---

## 🗺️ 방법 3: Tilemap 자동 콜라이더

### **Tilemap 사용 시:**

#### **Step 1: TilemapCollider2D 추가**

1. 프리팹의 `Tilemap_Wall` 선택
2. **Add Component** → `Tilemap Collider 2D`
3. **Inspector:**
   - ✅ **Used By Composite 체크**

#### **Step 2: CompositeCollider2D 설정**

- 방법 1 Step 2와 동일
- 또는 방법 2 자동 스크립트 사용

**장점:**
- 타일 배치에 따라 자동 콜라이더 생성
- 복잡한 형태 자동 처리
- 타일 변경 시 자동 업데이트

---

## 🔍 테스트 방법

### **1. Scene View 시각적 확인**

**적용 전:**
```
┌─────┐│┌─────┐  ← 2개 라인 (틈새)
│     │││     │
└─────┘│└─────┘
```

**적용 후:**
```
┌─────────────┐  ← 1개 연속 라인
│             │
└─────────────┘
```

---

### **2. Play Mode 테스트**

#### **테스트 시나리오:**

1. **Play 버튼 클릭**
2. **플레이어로 모듈 경계선 따라 이동**
   - 상하좌우 모든 방향
   - 경계선을 천천히 이동
3. **걸림 현상 체크:**
   - ✅ 부드럽게 이동: 성공!
   - ❌ 멈추거나 튕김: 재설정 필요

#### **몬스터 테스트:**

1. 몬스터 Chase 모드 활성화
2. 모듈 경계 통과 시 자연스러운지 확인
3. AI 경로 찾기 정상 작동 확인

---

## ⚙️ Inspector 상세 설정

### **Rigidbody2D (ColliderMap):**

```
Body Type: Static                 ← 벽은 움직이지 않음
Material: None
Simulated: ✓
Use Auto Mass: ✓
Collision Detection: Discrete
Sleeping Mode: Start Awake
Interpolate: None
Constraints: None
```

---

### **CompositeCollider2D (ColliderMap):**

```
Geometry Type: Polygons           ← 권장 (최적화)
  또는: Outlines                  ← 정확 (무거움)

Generation Type: Synchronous      ← 즉시 생성
Vertex Distance: 0.0005           ← 낮을수록 정밀
Offset Distance: 0.005
Edge Radius: 0                    ← 모서리 각짐
```

---

### **BoxCollider2D (모듈 내부):**

```
Material: None
Is Trigger: (체크 안 함)
Used By Effector: (체크 안 함)
Auto Tiling: (체크 안 함)
✅ Used By Composite: 체크        ← 필수!
```

---

## 🚨 문제 해결

### **Q1: CompositeCollider2D가 생성 안 돼요**

**A:** BoxCollider2D의 `Used By Composite` 체크 확인
```
각 BoxCollider2D → Inspector → Used By Composite ✓
```

---

### **Q2: 여전히 걸려요**

**A:** 다음 순서로 체크:

1. **ColliderMap 계층 확인:**
   ```
   ColliderMap (Rigidbody2D + CompositeCollider2D)
   └─ 모든 모듈이 하위에 있는지?
   ```

2. **Rigidbody2D Body Type:**
   ```
   Static으로 설정되어 있는지?
   ```

3. **Physics2D 설정 (선택):**
   ```
   Edit → Project Settings → Physics 2D
   Default Contact Offset: 0.01 → 0.005로 감소
   ```

4. **Collider Offset 재조정:**
   ```
   Wall_North Offset Y를 6.01로 약간 증가
   Wall_South Offset Y를 -6.01로 약간 감소
   (0.01f 오버랩으로 틈새 제거)
   ```

---

### **Q3: Scene에 초록색 라인이 안 보여요**

**A:** Gizmos 활성화 확인
```
Scene View 우측 상단 → Gizmos 버튼 클릭 (활성화)
```

---

### **Q4: Play 시 콜라이더가 사라져요**

**A:** Rigidbody2D가 Static인지 확인
```
ColliderMap → Rigidbody2D → Body Type: Static
(Dynamic이면 중력으로 떨어짐)
```

---

## 📊 성능 비교

### **개별 BoxCollider2D (적용 전):**
```
모듈 50개 × 콜라이더 4개 = 200개 콜라이더
Physics2D 연산: 200번
```

### **CompositeCollider2D (적용 후):**
```
1개 CompositeCollider2D
Physics2D 연산: 1번
→ 성능 200배 향상! 🚀
```

---

## ✅ 체크리스트

### **프리팹 준비:**
- [ ] Colliders GameObject 계층 존재
- [ ] 각 벽에 BoxCollider2D 추가
- [ ] Size/Offset 정확히 설정
- [ ] Used By Composite 체크 ✓

### **Scene 설정:**
- [ ] ColliderMap GameObject 생성
- [ ] Rigidbody2D 추가 (Static)
- [ ] CompositeCollider2D 추가
- [ ] 모든 모듈을 ColliderMap 하위로 이동
- [ ] Scene View에서 초록색 라인 확인

### **테스트:**
- [ ] Play Mode에서 경계선 이동 부드러움
- [ ] 몬스터 이동 정상
- [ ] 걸림 현상 없음

---

## 🎯 다음 단계

### **모든 테마 프리팹에 적용:**

1. **Forest 테마:**
   ```
   Md_Forest_* 프리팹에 동일하게 적용
   ```

2. **자동 스크립트 활용:**
   ```
   Tools → Module System → Setup Composite Collider
   → 한 번에 모든 모듈 설정!
   ```

3. **프리팹 교체 후 자동 적용:**
   ```
   Replace Theme 후 → Setup Composite Collider 실행
   ```

---

## 💡 추가 팁

### **Tip 1: 디버그 시각화**

Scene View에서 CompositeCollider2D 영역을 명확히 보려면:

```csharp
// ColliderMap에 추가 (옵션)
void OnDrawGizmos()
{
    CompositeCollider2D comp = GetComponent<CompositeCollider2D>();
    if (comp != null)
    {
        Gizmos.color = Color.green;
        // CompositeCollider2D 영역 그리기
    }
}
```

---

### **Tip 2: 레이어 분리**

ColliderMap을 별도 레이어로 분리:

1. **Layer 생성:**
   ```
   Edit → Project Settings → Tags and Layers
   Layer 8: "Walls"
   ```

2. **ColliderMap에 적용:**
   ```
   ColliderMap → Layer: Walls
   ```

3. **충돌 매트릭스 설정:**
   ```
   Edit → Project Settings → Physics 2D
   Layer Collision Matrix에서 필요한 충돌만 활성화
   ```

---

### **Tip 3: 런타임 동적 생성**

프로그래밍 방식으로 CompositeCollider2D 생성:

```csharp
GameObject colliderMap = new GameObject("ColliderMap");
Rigidbody2D rb = colliderMap.AddComponent<Rigidbody2D>();
rb.bodyType = RigidbodyType2D.Static;

CompositeCollider2D comp = colliderMap.AddComponent<CompositeCollider2D>();
comp.geometryType = CompositeCollider2D.GeometryType.Polygons;

// 모듈을 colliderMap 하위로 이동
moduleObject.transform.SetParent(colliderMap.transform);
```

---

**작성일:** 2026-01-19  
**버전:** 1.0  
**관련 도구:** `Tools → Module System → Setup Composite Collider`

