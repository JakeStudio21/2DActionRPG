# 모듈 프리팹 제작 가이드

## 📋 네이밍 규칙

### **표준 네이밍 형식:**
```
Md_[테마]_[타입]_[크기]_[변형]
```

### **예시:**
- `Md_Sample_Room_08x12_A` - 샘플 테마, 방, 8x12 크기, A 변형
- `Md_Forest_Room_08x12_A` - 숲 테마, 방, 8x12 크기, A 변형
- `Md_Desert_Corridor_04x08_B` - 사막 테마, 복도, 4x8 크기, B 변형

### **각 요소 설명:**

| 요소 | 설명 | 예시 |
|------|------|------|
| **접두사** | 모듈 프리팹 식별자 (고정) | `Md_` |
| **테마** | Sample, Forest, Desert, Volcano, Ice 등 | `Forest` |
| **타입** | Room, Corridor, Corner, Hall, Bridge 등 | `Room` |
| **크기** | 가로x세로 (그리드 단위) | `08x12`, `12x12`, `04x08` |
| **변형** | 같은 크기의 다양한 디자인 | `A`, `B`, `C`, `D` |

---

## 🏗️ 프리팹 계층 구조 (표준)

### **필수 계층 구조:**

```
Md_[테마]_[타입]_[크기]_[변형]  ← Root (Transform: 0,0,0)
├─ Visual                       ← 시각적 요소
│  ├─ Grid (선택사항)           ← Tilemap 사용 시
│  │  ├─ Tilemap_Floor         ← 바닥 타일
│  │  ├─ Tilemap_Wall          ← 벽 타일
│  │  └─ Tilemap_Decoration    ← 장식 타일
│  ├─ Sprite_Ground             ← SpriteRenderer (Tilemap 미사용 시)
│  └─ Sprite_Props              ← 추가 스프라이트
├─ Colliders                    ← 충돌 처리 (Phase 3에서 구현)
│  ├─ Wall_North               
│  ├─ Wall_South               
│  ├─ Wall_East                
│  ├─ Wall_West                
│  └─ Obstacle_*               
├─ Effects                      ← 파티클/라이트
│  ├─ Particle_Dust            
│  ├─ Particle_Fireflies       
│  └─ Light_Ambient            
└─ Anchor                       ← 논리적 위치 마커
   ├─ SpawnPoint_Enemy_01      
   ├─ SpawnPoint_Player        
   └─ Waypoint_Patrol_01       
```

---

## 🎨 프리팹 제작 단계 (Unity Editor)

### **1. 빈 GameObject 생성**

1. **Hierarchy** 우클릭 → `Create Empty`
2. 이름 변경: `Md_Sample_Room_08x12_A`
3. Transform → Position: `(0, 0, 0)`

---

### **2. Visual 계층 생성**

#### **옵션 A: Tilemap 사용 (권장)**

1. `Md_Sample_Room_08x12_A` 우클릭 → `Create Empty` → 이름: `Visual`
2. `Visual` 우클릭 → `2D Object` → `Tilemap` → `Rectangular`
   - 자동으로 `Grid`와 `Tilemap` 생성됨
3. `Tilemap` 이름 변경: `Tilemap_Floor`
4. 추가 레이어 필요 시:
   - `Grid` 우클릭 → `2D Object` → `Tilemap` → `Rectangular`
   - 이름: `Tilemap_Wall`, `Tilemap_Decoration`

**Tilemap 설정:**
- **Tilemap Renderer**:
  - Sorting Layer: `Default`
  - Order in Layer: `0` (Floor), `1` (Wall), `2` (Decoration)
  - Chunk Size: `32x32` (최적화)

#### **옵션 B: SpriteRenderer 사용**

1. `Md_Sample_Room_08x12_A` 우클릭 → `Create Empty` → 이름: `Visual`
2. `Visual` 우클릭 → `2D Object` → `Sprite`
3. 이름: `Sprite_Ground`
4. **Sprite Renderer** 설정:
   - Sprite: 바닥 스프라이트 할당
   - Sorting Layer: `Default`
   - Order in Layer: `0`
5. **Inspector** → Static 체크 ✓ (Static Batching 최적화)

---

### **3. Colliders 계층 생성 (Phase 3에서 상세 구현)**

1. `Md_Sample_Room_08x12_A` 우클릭 → `Create Empty` → 이름: `Colliders`
2. 현재는 빈 GameObject로 두고 Phase 3에서 추가

---

### **4. Effects 계층 생성 (선택사항)**

1. `Md_Sample_Room_08x12_A` 우클릭 → `Create Empty` → 이름: `Effects`
2. `Effects` 우클릭 → `Effects` → `Particle System`
3. 이름: `Particle_Dust`
4. Particle System 설정 (예시):
   - Start Lifetime: `2`
   - Start Speed: `0.5`
   - Emission Rate: `5`

---

### **5. Anchor 계층 생성 (선택사항)**

1. `Md_Sample_Room_08x12_A` 우클릭 → `Create Empty` → 이름: `Anchor`
2. `Anchor` 우클릭 → `Create Empty` → 이름: `SpawnPoint_Enemy_01`
3. Position 설정: `(5, 0, 5)` (원하는 위치)
4. Tag 설정: `EnemySpawn` (Inspector → Tag)

---

### **6. 프리팹으로 저장**

1. **Hierarchy**에서 `Md_Sample_Room_08x12_A` 선택
2. **Project** 창에서 `Assets/Prefabs/Modules/Sample/` 폴더로 드래그
3. 프리팹 생성 완료! (파란색 아이콘으로 변경됨)

---

## ✅ 프리팹 제작 체크리스트

### **필수 항목:**

- [ ] Root GameObject 이름: `Md_[테마]_[타입]_[크기]_[변형]` 형식
- [ ] Root Transform: `(0, 0, 0)`
- [ ] `Visual` 계층 존재
- [ ] `Visual` 하위에 Tilemap 또는 SpriteRenderer 존재
- [ ] `Colliders` 계층 존재 (현재는 비어있어도 됨)
- [ ] 프리팹으로 저장됨 (파란색 아이콘)

### **선택 항목:**

- [ ] `Effects` 계층 (파티클/라이트)
- [ ] `Anchor` 계층 (스폰 포인트 등)
- [ ] Static 설정 (SpriteRenderer 사용 시)

---

## 📦 샘플 프리팹 목록 (Phase 1 제작 권장)

### **최소 3개 프리팹 제작:**

| 프리팹 이름 | 설명 | 크기 |
|------------|------|------|
| `Md_Sample_Room_08x12_A` | 작은 방 | 8x12 |
| `Md_Sample_Room_12x12_A` | 정사각형 방 | 12x12 |
| `Md_Sample_Corridor_04x08_A` | 복도 | 4x8 |

### **추가 프리팹 (선택사항):**

| 프리팹 이름 | 설명 | 크기 |
|------------|------|------|
| `Md_Sample_Room_16x16_A` | 큰 방 | 16x16 |
| `Md_Sample_Corner_08x08_A` | 코너 (L자) | 8x8 |
| `Md_Sample_Hall_16x08_A` | 홀 | 16x8 |

---

## 🎯 프리팹 제작 시 주의사항

### **1. Transform 기준점:**
- Root의 Position은 항상 `(0, 0, 0)`
- 모듈의 중심점 또는 좌측 하단을 기준으로 통일

### **2. Tilemap vs SpriteRenderer:**
| 항목 | Tilemap | SpriteRenderer |
|------|---------|----------------|
| **장점** | 타일 편집 쉬움, 자동 콜라이더 | 단순, 가벼움 |
| **단점** | 복잡, 병합 필요 | 세밀한 디자인 어려움 |
| **권장 용도** | 복잡한 맵, 큰 모듈 | 작은 모듈, 단순 배경 |

### **3. Sorting Order:**
- Floor: `0`
- Wall: `1`
- Decoration: `2`
- Effects: `3`

### **4. Static Batching:**
- `Visual` 하위 SpriteRenderer는 Static 체크 ✓
- Tilemap은 자동 최적화되므로 Static 불필요

---

## 🔄 테마 프리팹 제작 (Phase 2 이후)

### **Forest 테마 프리팹 제작:**

1. `Md_Sample_Room_08x12_A` 프리팹 복제
2. 이름 변경: `Md_Forest_Room_08x12_A`
3. `Visual` 하위 스프라이트/타일 변경:
   - 바닥: 잔디 텍스처
   - 벽: 나무 텍스처
   - 장식: 나뭇잎, 덤불 등
4. `Effects` 수정:
   - Particle_Fireflies 추가 (숲 분위기)
5. `Assets/Prefabs/Modules/Forest/` 폴더에 저장

---

## 🚀 다음 단계

### **Phase 1 완료 후:**
- [ ] 샘플 프리팹 3개 제작 완료
- [ ] `Assets/Prefabs/Modules/Sample/` 폴더에 저장 완료
- [ ] Hierarchy에서 테스트 배치 확인

### **Phase 2 준비:**
- Editor Tool이 자동으로 매핑 생성
- Forest 테마 프리팹 제작 시작

---

## 📞 문제 해결

### **Q: Tilemap이 보이지 않아요**
**A:** Tilemap Renderer의 Sorting Layer 확인, Tile Palette에서 타일 그리기

### **Q: 프리팹 이름이 자동으로 변경돼요**
**A:** Unity 재시작, `.meta` 파일 충돌 해결

### **Q: Collider는 언제 추가하나요?**
**A:** Phase 3에서 추가합니다. 현재는 `Colliders` GameObject만 만들어두세요.

---

**작성일:** 2026-01-18  
**버전:** 1.0  
**작성자:** AI Assistant

