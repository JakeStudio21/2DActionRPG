# 📦 아이템 드롭 프리팹 생성 가이드

## 🎯 목표
- Drop_Currency 프리팹 1개 생성
- Drop_Equipment 프리팹 1개 생성
- EquipmentFX_D/C/B/A 프리팹 4개 생성

---

## 📋 Step 1: Drop_Currency 프리팹 생성

### **1-1. 빈 GameObject 생성**
```
Hierarchy 우클릭 → Create Empty
이름: Drop_Currency
```

### **1-2. 컴포넌트 추가**

#### **SpriteRenderer**
```
Add Component → Rendering → Sprite Renderer
- Sprite: (비워둠, 런타임에 동적 할당)
- Sorting Layer: Default
- Order in Layer: 10
```

#### **CircleCollider2D**
```
Add Component → Physics 2D → Circle Collider 2D
- Is Trigger: ✅ 체크
- Radius: 0.5
```

#### **Rigidbody2D**
```
Add Component → Physics 2D → Rigidbody 2D
- Body Type: Dynamic
- Gravity Scale: 0
- Constraints: Freeze Rotation Z ✅ 체크
```

#### **CurrencyPickup 스크립트**
```
Add Component → Scripts → CurrencyPickup
Inspector 설정:
- Sprite Renderer: 자동 할당됨
- Item Collider: 자동 할당됨
- Rb: 자동 할당됨
- Pick Up Distance: 5
- Acceleration Rate: 0.2
- Move Speed: 3
- Pop Height Y: 1.5
- Pop Duration: 1
```

### **1-3. 내장 FX 추가 (ParticleSystem)**

#### **자식 오브젝트 생성**
```
Drop_Currency 우클릭 → Effects → Particle System
이름: Currency_FX
```

#### **ParticleSystem 설정**
```
Main:
- Duration: 1
- Looping: ✅ 체크
- Start Lifetime: 0.5~1.0
- Start Speed: 2~5
- Start Size: 0.1~0.3
- Start Color: Gold (255, 215, 0)

Emission:
- Rate over Time: 10

Shape:
- Shape: Circle
- Radius: 0.5

Renderer:
- Sorting Layer: Default
- Order in Layer: 11
```

#### **CurrencyPickup Inspector에 연결**
```
CurrencyPickup 컴포넌트:
- Currency Fx Instance: Currency_FX 드래그 앤 드롭
```

### **1-4. Animation Curve 설정**
```
CurrencyPickup 컴포넌트:
- Pop Animation Curve: 
  * 0.0: 0.0
  * 0.5: 1.0 (피크)
  * 1.0: 0.0
```

### **1-5. 프리팹 저장**
```
Hierarchy의 Drop_Currency를 
Assets/Prefabs/Pickup/ 폴더로 드래그

(Pickup 폴더가 없으면 생성)
```

---

## 📋 Step 2: Drop_Equipment 프리팹 생성

### **2-1. 빈 GameObject 생성**
```
Hierarchy 우클릭 → Create Empty
이름: Drop_Equipment
```

### **2-2. 컴포넌트 추가**

#### **SpriteRenderer**
```
Add Component → Rendering → Sprite Renderer
- Sprite: (비워둠, 런타임에 동적 할당)
- Sorting Layer: Default
- Order in Layer: 10
```

#### **CircleCollider2D**
```
Add Component → Physics 2D → Circle Collider 2D
- Is Trigger: ✅ 체크
- Radius: 0.5
```

#### **Rigidbody2D**
```
Add Component → Physics 2D → Rigidbody 2D
- Body Type: Dynamic
- Gravity Scale: 0
- Constraints: Freeze Rotation Z ✅ 체크
```

#### **EquipmentPickup 스크립트**
```
Add Component → Scripts → EquipmentPickup
Inspector 설정:
- Sprite Renderer: 자동 할당됨
- Item Collider: 자동 할당됨
- Rb: 자동 할당됨
- Pick Up Distance: 5
- Acceleration Rate: 0.2
- Move Speed: 3
- Pop Height Y: 1.5
- Pop Duration: 1
```

### **2-3. FX Attach Point 추가**

#### **자식 빈 오브젝트 생성**
```
Drop_Equipment 우클릭 → Create Empty
이름: FX_AttachPoint
Position: (0, 0, 0)
```

#### **EquipmentPickup Inspector에 연결**
```
EquipmentPickup 컴포넌트:
- Fx Attach Point: FX_AttachPoint 드래그 앤 드롭
```

### **2-4. Animation Curve 설정**
```
EquipmentPickup 컴포넌트:
- Pop Animation Curve: 
  * 0.0: 0.0
  * 0.5: 1.0 (피크)
  * 1.0: 0.0
```

### **2-5. 프리팹 저장**
```
Hierarchy의 Drop_Equipment를 
Assets/Prefabs/Pickup/ 폴더로 드래그
```

---

## 📋 Step 3: EquipmentFX 프리팹 4개 생성

### **3-1. EquipmentFX_D (White, 일반 등급)**

#### **ParticleSystem 생성**
```
Hierarchy 우클릭 → Effects → Particle System
이름: EquipmentFX_D
```

#### **ParticleSystem 설정**
```
Main:
- Duration: 1
- Looping: ✅ 체크
- Start Lifetime: 0.5~1.0
- Start Speed: 1~3
- Start Size: 0.1~0.2
- Start Color: White (255, 255, 255)

Emission:
- Rate over Time: 5

Shape:
- Shape: Circle
- Radius: 0.5

Renderer:
- Sorting Layer: Default
- Order in Layer: 11
```

#### **프리팹 저장**
```
Assets/Prefabs/VFX/Equipment/ 폴더로 드래그
(폴더가 없으면 생성)
```

---

### **3-2. EquipmentFX_C (Green, 고급 등급)**
```
EquipmentFX_D 복제 (Ctrl+D)
이름: EquipmentFX_C

Main:
- Start Color: Green (0, 255, 0)

Emission:
- Rate over Time: 8

프리팹 저장
```

---

### **3-3. EquipmentFX_B (Blue, 희귀 등급)**
```
EquipmentFX_D 복제
이름: EquipmentFX_B

Main:
- Start Color: Blue (0, 153, 255)

Emission:
- Rate over Time: 12

프리팹 저장
```

---

### **3-4. EquipmentFX_A (Purple, 영웅 등급)**
```
EquipmentFX_D 복제
이름: EquipmentFX_A

Main:
- Start Color: Purple (153, 0, 255)

Emission:
- Rate over Time: 15

프리팹 저장
```

---

## ✅ 완료 체크리스트

- [ ] Drop_Currency 프리팹 생성 완료
- [ ] Drop_Currency에 CurrencyPickup 컴포넌트 부착
- [ ] Drop_Currency에 내장 FX (ParticleSystem) 추가
- [ ] Drop_Equipment 프리팹 생성 완료
- [ ] Drop_Equipment에 EquipmentPickup 컴포넌트 부착
- [ ] Drop_Equipment에 FX_AttachPoint 자식 오브젝트 추가
- [ ] EquipmentFX_D 프리팹 생성 (White)
- [ ] EquipmentFX_C 프리팹 생성 (Green)
- [ ] EquipmentFX_B 프리팹 생성 (Blue)
- [ ] EquipmentFX_A 프리팹 생성 (Purple)

---

## 📂 최종 폴더 구조

```
Assets/
├── Prefabs/
│   ├── Pickup/
│   │   ├── Drop_Currency.prefab
│   │   └── Drop_Equipment.prefab
│   └── VFX/
│       └── Equipment/
│           ├── EquipmentFX_D.prefab
│           ├── EquipmentFX_C.prefab
│           ├── EquipmentFX_B.prefab
│           └── EquipmentFX_A.prefab
```

---

## 🎯 다음 단계

프리팹 생성이 완료되면:
- **Phase 4**: EnemyHealth.SpawnSingleItem() 리팩토링
- **Phase 5**: ScenePoolConfig 업데이트

---

## 💡 팁

1. **Animation Curve 팁**:
   - Inspector에서 Pop Animation Curve를 클릭하면 커브 에디터가 열립니다
   - 부드러운 포물선을 만드세요

2. **ParticleSystem 미리보기**:
   - Scene View에서 ParticleSystem을 선택하면 실시간 미리보기가 됩니다
   - Play 버튼을 눌러 효과를 확인하세요

3. **프리팹 변형**:
   - 프리팹을 생성한 후 수정하면 자동으로 저장됩니다
   - Prefab Mode로 들어가서 편집하는 것을 권장합니다

