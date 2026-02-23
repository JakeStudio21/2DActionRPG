# 🎮 Phase 4-C 룬 시스템 실전 테스트 가이드

## 📋 목차
1. [테스트 준비](#1-테스트-준비)
2. [예시 1: 플레이어 보스 공격 (흡혈)](#2-예시-1-플레이어-보스-공격-흡혈)
3. [예시 2: 보스가 플레이어 공격 (회복 차단)](#3-예시-2-보스가-플레이어-공격-회복-차단)
4. [예시 3: 보스 공격 시 면역 발동](#4-예시-3-보스-공격-시-면역-발동)
5. [문제 해결](#5-문제-해결)

---

## 1. 테스트 준비

### Step 1-1: 테스트용 룬 생성
```
Unity Editor 상단 메뉴:
Tools → Generate Test Runes 클릭

✅ 생성 확인:
Project 창 → Resources/Runes 폴더
- RUNE_BOSS_HUNTER.asset (보스 사냥꾼)
- RUNE_BOSS_DEFENDER.asset (보스 방어자)
- RUNE_LIFESTEAL.asset (흡혈 룬)
```

### Step 1-2: RuneManager 설정
```
1. Hierarchy에서 GameManager (또는 DontDestroyOnLoad 오브젝트) 선택
2. Add Component → RuneManager
3. Inspector 설정:
   - Number Of Rune Slots: 5
```

### Step 1-3: PlayerRuntimeStats 확인
```
1. Hierarchy에서 Player 오브젝트 선택
2. PlayerRuntimeStats 컴포넌트 확인:
   - Active Conditional Modifiers: (비어있음)
```

### Step 1-4: CombatFormulaConfig 로그 활성화
```
Project 창 → Resources/Combat/CombatFormulaConfig.asset 선택
Inspector:
☑ Enable Detailed Logs (체크 확인!)
```

---

## 2. 예시 1: 플레이어 보스 공격 (흡혈) ❤️

### 🎯 테스트 시나리오
```
플레이어가 보스를 공격 → 데미지의 2%만큼 체력 회복
룬: RUNE_LIFESTEAL (BOSS_DOT_LIFESTEAL)
```

### Step 2-1: 흡혈 룬 장착
```csharp
// Unity Console 창에서 실행 (개발자 콘솔 또는 치트 스크립트)
RuneManager.Instance.EquipRune(
    Resources.Load<RuneData>("Runes/RUNE_LIFESTEAL"), 
    0 // 슬롯 0번
);
```

**또는 Inspector에서 직접 설정:**
```
1. RuneManager 컴포넌트 선택
2. Equipped Runes 배열 펼치기
3. Element 0 → RUNE_LIFESTEAL 드래그 앤 드롭
```

### Step 2-2: 게임 플레이 시작
```
1. Play 버튼 클릭
2. 보스가 등장하는 스테이지로 이동
3. 플레이어 체력을 일부러 낮춤 (보스 공격 받기)
```

### Step 2-3: 보스 공격 및 로그 확인
```
플레이어가 보스를 공격 (기본공격 또는 스킬)

✅ 예상 로그:
[CombatFormula] Phase 7: 흡혈 3.0
💚 [DamageSource] 흡혈: 3 HP 회복
❤️ [PlayerHealth] 체력 회복: +3 (153/200)
```

### Step 2-4: 흡혈 확인 방법
```
방법 1: 로그 확인
  - Console 창에서 "흡혈" 검색

방법 2: UI 확인
  - 플레이어 체력바가 공격 시마다 약간씩 증가하는지 확인

방법 3: 수치 검증
  - 최종 데미지 150 → 흡혈 3 (150 × 0.02 = 3)
  - 최종 데미지 200 → 흡혈 4 (200 × 0.02 = 4)
```

### ✅ 성공 조건
- [x] 보스 공격 시 흡혈 로그 출력
- [x] 플레이어 체력 실제로 회복됨
- [x] 일반 몬스터 공격 시 흡혈 발동 안 함 (보스 전용)

---

## 3. 예시 2: 보스가 플레이어 공격 (회복 차단) 🚫

### 🎯 테스트 시나리오
```
보스가 플레이어를 공격 → 플레이어에게 50% 회복 차단 디버프 (5초)
룬: RUNE_BOSS_DEFENDER (BOSS_BLOCK_HEAL)
```

### Step 3-1: 회복 차단 룬 장착
```csharp
// 기존 룬 해제 후 새 룬 장착
RuneManager.Instance.UnequipRune(0);
RuneManager.Instance.EquipRune(
    Resources.Load<RuneData>("Runes/RUNE_BOSS_DEFENDER"), 
    0
);
```

### Step 3-2: 보스에게 공격받기
```
1. Play 버튼 클릭
2. 보스 스테이지로 이동
3. 의도적으로 보스 공격 받기
```

### Step 3-3: 회복 차단 로그 확인
```
보스가 플레이어를 공격

✅ 예상 로그 (CombatFormula):
[CombatFormula] Phase 7: 회복 차단 50%

✅ 예상 로그 (PlayerHealth):
🚫 [PlayerHealth] Player 회복 차단 50% 적용됨.
```

### Step 3-4: 회복 차단 효과 테스트
```
1. 보스에게 맞은 직후 (5초 이내)
2. 포션 사용 또는 회복 아이템 획득
3. Console 로그 확인:

✅ 예상 로그 (회복 시도 시):
🚫 [PlayerHealth] 회복 차단 발동! 100 중 50 차단됨. 실제 회복량: 50
❤️ [PlayerHealth] 체력 회복: +50 (100/200)

정상: 100 회복 → 실제 50만 회복됨 ✅
비정상: 100 회복 → 100 전부 회복됨 ❌
```

### Step 3-5: 회복 차단 해제 확인
```
5초 대기 후:

✅ 예상 로그:
✅ [PlayerHealth] 회복 차단 해제됨.

이후 회복 시도 시 100% 회복되는지 확인
```

### ✅ 성공 조건
- [x] 보스 공격 시 회복 차단 로그 출력
- [x] 5초간 회복량 50% 감소
- [x] 5초 후 정상 회복
- [x] 일반 몬스터 공격 시 회복 차단 발동 안 함

---

## 4. 예시 3: 보스 공격 시 면역 발동 🛡️

### 🎯 테스트 시나리오
```
보스가 플레이어를 공격 (Bind 상태이상 시도) → 면역으로 차단
룬: RUNE_BOSS_DEFENDER (IMMUNE_BIND)
```

### Step 4-1: 면역 룬 장착
```csharp
// RUNE_BOSS_DEFENDER에 IMMUNE_BIND가 포함되어 있음
RuneManager.Instance.EquipRune(
    Resources.Load<RuneData>("Runes/RUNE_BOSS_DEFENDER"), 
    0
);
```

### Step 4-2: 속박 공격을 하는 보스 찾기
```
Bind(속박) 상태이상을 부여하는 보스 스킬이 필요합니다.

확인 방법:
1. Project 창 → Resources/Enemies 폴더
2. 보스 AttackData 에셋 선택
3. Inspector에서 Bind Effect 확인
```

**⚠️ 중요: Bind 공격을 하는 보스가 없다면 테스트용 보스 수정 필요!**

### Step 4-3: 보스의 Bind 공격 받기
```
1. Play 버튼 클릭
2. Bind 공격 보스 스테이지로 이동
3. 보스의 Bind 스킬 공격 받기
```

### Step 4-4: 면역 로그 확인
```
보스가 Bind 공격 시도

✅ 예상 로그 (CombatFormula):
[CombatFormula] Phase 0: 면역 효과 발동 - Bind

✅ 예상 로그 (PlayerHealth):
🛡️ [PlayerHealth] 플레이어 면역 발동! 저항한 효과: Bind

✅ 예상 로그 (MeleeAttack - 상태이상 차단):
[MeleeAttack] Bind 효과 면역으로 차단됨 (resistedEffects에 포함)
```

### Step 4-5: 면역 효과 확인
```
면역 발동 후:
- 플레이어 이동 가능 ✅ (속박 걸리지 않음)
- 플레이어 상태 UI에 Bind 아이콘 없음 ✅
- 데미지는 정상적으로 받음 ✅ (면역 ≠ 무적)
```

### ✅ 성공 조건
- [x] 보스 Bind 공격 시 면역 로그 출력
- [x] 플레이어에게 Bind 상태이상 부여 안 됨
- [x] 데미지는 정상적으로 받음
- [x] 일반 몬스터 Bind 공격은 정상 적용됨 (보스 전용 면역)

---

## 5. 문제 해결

### 문제 1: 룬 효과가 적용되지 않음

**증상:**
```
보스 공격해도 흡혈 안 됨
보스에게 맞아도 회복 차단 안 됨
```

**해결 방법:**
```
1. RuneManager가 씬에 존재하는지 확인
   Hierarchy → Search: RuneManager

2. 룬이 정상 장착되었는지 확인
   RuneManager → Inspector → Equipped Runes 배열 확인

3. PlayerRuntimeStats 연동 확인
   Player → PlayerRuntimeStats 컴포넌트 확인
   Active Conditional Modifiers: (룬 효과가 있어야 함)

4. Console 로그 확인
   [RuneManager] PlayerRuntimeStats에 3개의 조건부 모디파이어를 업데이트했습니다.
```

---

### 문제 2: 로그가 출력되지 않음

**증상:**
```
전투는 정상인데 로그가 안 보임
```

**해결 방법:**
```
1. CombatFormulaConfig 확인
   Project → Resources/Combat/CombatFormulaConfig.asset
   Inspector: ☑ Enable Detailed Logs 체크!

2. Console 창 필터 확인
   Console 창 상단:
   ☑ Info (파란색)
   ☑ Warning (노란색)
   ☑ Error (빨간색)

3. Console 검색 활용
   Console 창 우측 상단 검색창:
   - "흡혈" 검색
   - "면역" 검색
   - "회복 차단" 검색
```

---

### 문제 3: 보스가 아닌 일반 몬스터에게도 룬 효과 발동

**증상:**
```
일반 슬라임 공격해도 흡혈 발동
```

**원인:**
```
대상 몬스터가 IEnemyTarget.IsBoss() = true 반환 중
```

**해결 방법:**
```
1. 해당 몬스터의 EnemyData 확인
   Project → Resources/Enemies/[몬스터명]Data.asset
   Inspector: Enemy Type = Boss로 설정되어 있는지 확인

2. 일반 몬스터는 EnemyType.Normal이어야 함
   보스는 EnemyType.Boss
   엘리트는 EnemyType.Elite
```

---

### 문제 4: 회복 차단이 5초 후에도 해제되지 않음

**증상:**
```
5초 지나도 계속 회복량 50% 감소
```

**해결 방법:**
```
1. PlayerHealth.cs 확인
   healingBlockRoutine이 정상 실행되는지 로그 확인:
   ✅ [PlayerHealth] 회복 차단 해제됨.

2. Time.timeScale 확인
   게임이 일시정지 상태가 아닌지 확인
   Time.timeScale = 1.0f (정상)
   Time.timeScale = 0.0f (일시정지, 코루틴 멈춤!)

3. PlayerHealth가 파괴되지 않았는지 확인
   씬 전환 시 PlayerHealth 오브젝트 유지되는지 확인
```

---

## 6. 빠른 테스트 스크립트 (선택 사항)

Unity Console 창에서 실행할 수 있는 치트 명령어를 만들면 테스트가 더 편리합니다.

### 치트 스크립트 생성

```csharp
// Assets/Scripts/Debug/RuneTestCheat.cs
using UnityEngine;

public class RuneTestCheat : MonoBehaviour
{
    void Update()
    {
        // F1: 흡혈 룬 장착
        if (Input.GetKeyDown(KeyCode.F1))
        {
            EquipLifeStealRune();
        }
        
        // F2: 방어 룬 장착 (면역 + 회복 차단)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            EquipDefenderRune();
        }
        
        // F3: 보스 사냥꾼 룬 장착
        if (Input.GetKeyDown(KeyCode.F3))
        {
            EquipHunterRune();
        }
        
        // F4: 모든 룬 해제
        if (Input.GetKeyDown(KeyCode.F4))
        {
            UnequipAllRunes();
        }
        
        // F5: 플레이어 체력 50%로 설정
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SetPlayerHpTo50Percent();
        }
    }
    
    void EquipLifeStealRune()
    {
        var rune = Resources.Load<RuneData>("Runes/RUNE_LIFESTEAL");
        RuneManager.Instance.EquipRune(rune, 0);
        Debug.Log("✅ 흡혈 룬 장착! (F1)");
    }
    
    void EquipDefenderRune()
    {
        var rune = Resources.Load<RuneData>("Runes/RUNE_BOSS_DEFENDER");
        RuneManager.Instance.EquipRune(rune, 0);
        Debug.Log("✅ 방어 룬 장착! (F2)");
    }
    
    void EquipHunterRune()
    {
        var rune = Resources.Load<RuneData>("Runes/RUNE_BOSS_HUNTER");
        RuneManager.Instance.EquipRune(rune, 0);
        Debug.Log("✅ 사냥꾼 룬 장착! (F3)");
    }
    
    void UnequipAllRunes()
    {
        for (int i = 0; i < 5; i++)
        {
            RuneManager.Instance.UnequipRune(i);
        }
        Debug.Log("✅ 모든 룬 해제! (F4)");
    }
    
    void SetPlayerHpTo50Percent()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            // 현재 체력을 최대 체력의 50%로 설정
            int targetHp = playerHealth.maxHealth / 2;
            // TakeDamage 대신 직접 설정 (테스트용)
            playerHealth.currentHealth = targetHp;
            playerHealth.UpdateUI();
            Debug.Log($"✅ 플레이어 체력 50%로 설정! ({targetHp}/{playerHealth.maxHealth})");
        }
    }
}
```

### 사용 방법
```
1. 빈 GameObject 생성 (Hierarchy 우클릭 → Create Empty)
2. 이름: RuneTestCheat
3. Add Component → RuneTestCheat 스크립트 추가
4. Play 모드에서 단축키 사용:
   F1: 흡혈 룬
   F2: 방어 룬
   F3: 사냥꾼 룬
   F4: 모든 룬 해제
   F5: 플레이어 체력 50%
```

---

## 7. 테스트 단축키 모음

### 🎮 룬 장착/해제 (Ctrl+Shift 조합)
- **[Ctrl+Shift+F1]**: 흡혈 룬 장착 (RUNE_LIFESTEAL)
- **[Ctrl+Shift+F2]**: 방어 룬 장착 (RUNE_BOSS_DEFENDER - 면역 + 회복 차단)
- **[Ctrl+Shift+F3]**: 사냥꾼 룬 장착 (RUNE_BOSS_HUNTER - 보스 피해 증가)
- **[Ctrl+Shift+F4]**: 모든 룬 해제
- **[Ctrl+Shift+F5]**: 플레이어 체력 50%로 설정
- **[Ctrl+Shift+F6]**: 현재 룬 상태 출력

### 🧪 상태이상 테스트 (Test 2, 3용)
- **[Ctrl+Shift+F9]**: 플레이어에게 Bind 상태이상 적용 (면역 테스트)
- **[Ctrl+Shift+H]**: 플레이어에게 회복 차단 적용 (회복 차단 테스트, H = Healing Block)

> 💡 **Tip**: Ctrl+Shift를 먼저 누른 상태에서 F1~F6, F9, H 키를 누르세요!

---

## 8. 최종 체크리스트

### ✅ 테스트 전 준비
- [ ] Tools → Generate Test Runes 실행
- [ ] RuneManager 컴포넌트 추가 (플레이어에 이미 있음)
- [ ] RuntimeTest_Phase4C 컴포넌트 추가 (씬 아무 오브젝트나)
- [ ] ~~StatusEffectManager 추가~~ (자동 생성됨!)
- [ ] CombatFormulaConfig → Enable Detailed Logs 체크

### ✅ 예시 1: 흡혈 테스트
- [ ] RUNE_LIFESTEAL 장착
- [ ] 보스 공격 시 "흡혈" 로그 출력
- [ ] 플레이어 체력 실제로 회복됨
- [ ] 일반 몬스터에게는 흡혈 발동 안 함

### ✅ 예시 2: 회복 차단 테스트
- [ ] **[Ctrl+Shift+F2]** 키로 RUNE_BOSS_DEFENDER 장착
- [ ] **[Ctrl+Shift+H]** 키로 회복 차단 직접 적용
- [ ] Console에서 "회복 차단 50% 적용 완료" 로그 확인
- [ ] 회복 아이템 사용 또는 자동 회복 시도
- [ ] 5초간 회복량 50% 감소 확인
- [ ] 5초 후 "회복 차단 해제" 로그 확인

### ✅ 예시 3: 면역 테스트
- [ ] **[Ctrl+Shift+F2]** 키로 RUNE_BOSS_DEFENDER 장착 (IMMUNE_BIND 포함)
- [ ] **[Ctrl+Shift+F9]** 키로 Bind 상태이상 적용 시도
- [ ] Console에서 "🛡️ 면역 활성화! 저항 효과: Bind" 로그 확인
- [ ] "Bind 상태이상 적용 차단됨" 로그 확인
- [ ] 플레이어가 여전히 이동 가능한지 확인
- [ ] **[Ctrl+Shift+F4]** 키로 룬 해제 후 **[Ctrl+Shift+F9]** 재시도 → Bind 정상 적용됨 확인

---

## 📊 예상 테스트 결과 (성공 시)

### 예시 1 성공 로그
```
[CombatFormula] === 플레이어 → 몬스터 데미지 계산 시작 ===
[CombatFormula] 조건부 모디파이어 적용: BOSS_DOT_LIFESTEAL (흡혈 +2%)
[CombatFormula] Phase 7: 흡혈 3.0
💚 [DamageSource] 흡혈: 3 HP 회복
❤️ [PlayerHealth] 체력 회복: +3 (153/200)
[CombatFormula] === 최종 데미지: 150 ===
```

### 예시 2 성공 로그 (Ctrl+Shift+H 키 사용)
```
🧪 [Ctrl+Shift+H] 플레이어에게 회복 차단 적용...
🚫 [PlayerHealth] 회복 차단 50% 적용! (5초)
✅ 회복 차단 50% 적용 완료! (5초간 회복량 50% 감소)
💡 Tip: 이제 회복 포션을 먹거나 체력 회복을 시도하면 50%만 회복됩니다.

(5초 이내 회복 시도)
🚫 [PlayerHealth] 회복 차단 발동! 100 중 50 차단됨. 실제 회복량: 50
❤️ [PlayerHealth] 체력 회복: +50 (100/200)

(5초 후)
✅ [PlayerHealth] 회복 차단 해제됨.
```

### 예시 3 성공 로그 (Ctrl+Shift+F9 키 사용, Ctrl+Shift+F2 룬 장착 시)
```
========================================
🧪 [Ctrl+Shift+F9] Bind 상태이상 적용 테스트
========================================
🛡️ 발견: 보스 저항 (Immunity_Bind)
💥 [PlayerHealth] 1 데미지 받음 (크리티컬: False, 백어택: False) (199/200)
🛡️ [PlayerHealth] 플레이어 면역 발동! 저항한 효과: Bind
🛡️ 면역 활성화! 저항 효과: Bind
⏭️ Bind 상태이상 적용 차단됨!
========================================
💡 Tip: [Ctrl+Shift+F2] 방어 룬 장착 후 다시 시도하면 면역이 발동합니다!
========================================
🛡️ [StatusEffectManager] UnnamedPlayer이(가) Bind에 면역! 차단됨.
```

---

## 🎯 테스트 완료 후

모든 테스트가 성공하면:
1. ✅ Phase 4-C 룬 시스템 완벽 작동!
2. ✅ 실전 전투 통합 완료!
3. ✅ 다음 단계로 진행 가능!

테스트 중 문제 발생 시:
- 위의 "문제 해결" 섹션 참고
- 로그 캡처해서 공유
- 예상 동작과 실제 동작 비교

---

**🎮 즐거운 테스트 되세요!** 🎯

