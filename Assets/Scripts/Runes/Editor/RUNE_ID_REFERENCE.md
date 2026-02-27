# 🎯 룬 주옵션 ID 참고표

**출처:** `Assets/Resources/Combat/CSV/ConditionalModifier.csv`

**마지막 업데이트:** 2026-02-27

---

## ✅ 사용 가능한 주옵션 ID (8개)

### 🗡️ 공격형 (4개)

| ID | 표시 이름 | 효과 | 기본값 | 단위 |
|----|----------|------|--------|------|
| `BOSS_IGNORE_DEF` | 보스 방어 무시 % | 보스 방어력 무시 | 0.2 | Percent |
| `BOSS_DMG_UP` | 보스 피해 증가 % | 보스 추가 피해 | 0.2 | Percent |
| `BOSS_HP_HIGH_BONUS` | 보스 체력 50% 이상 추가 피해 | 보스 HP 50% 이상일 때 | 0.25 | Percent |
| `BOSS_BLOCK_HEAL` | 보스 회복 차단 % | 보스 회복 차단 | 0.5 | Percent |

---

### 🛡️ 생존형 (4개)

| ID | 표시 이름 | 효과 | 기본값 | 단위 |
|----|----------|------|--------|------|
| `BOSS_DOT_LIFESTEAL` | 보스 피해 HP DOT 회복 초당% | 보스 대상 DOT 흡혈 | 0.02 | Percent |
| `LOW_HP_DR` | 체력 30% 이하 피해 감소 | HP 30% 이하일 때 | 0.25 | Percent |
| `BOSS_AREA_DMG_REDUCE` | 보스 장판 피해 감소 % | 보스 장판 피해 감소 | 0.5 | Percent |
| `BOSS_ATK_DMG_REDUCE` | 보스 공격피해 감소 % | 보스 피해 감소 | 0.25 | Percent |

---

## 📝 사용 방법

### Unity Inspector에서 설정

1. `Project` 창에서 룬 데이터 에셋 선택 (예: `Rune_Boss_Hunter`)
2. Inspector에서 `Main Stat Modifier Id` 필드 찾기
3. 위 표의 ID를 **정확히** 복사 & 붙여넣기

**예시:**
```
Main Stat Modifier Id: BOSS_IGNORE_DEF
```

---

### 자동 수정 도구 사용

```
Tools → Rune System → Fix Rune Data → 1. 모든 룬 데이터 검증 및 수정
```

---

## 🎨 추천 조합

### 공격형 룬 (4종)
- **보스 사냥꾼**: `BOSS_IGNORE_DEF` (방어 무시 - 물리 딜러)
- **파괴자**: `BOSS_DMG_UP` (피해 증가 - 범용)
- **처형자**: `BOSS_BLOCK_HEAL` (회복 차단 - 힐러 카운터)
- **고체력 사냥꾼**: `BOSS_HP_HIGH_BONUS` (고체력 특화)

### 생존형 룬 (4종)
- **흡혈 룬**: `BOSS_DOT_LIFESTEAL` (DOT 흡혈 - 지속 회복)
- **불굴의 생존자**: `LOW_HP_DR` (저체력 방어 - 위기 대응)
- **보스 철벽**: `BOSS_AREA_DMG_REDUCE` (장판 감소 - 메카닉 대응)
- **강철 방패**: `BOSS_ATK_DMG_REDUCE` (보스 피해 감소 - 탱커)

---

## ⚠️ 주의사항

### 잘못된 ID (사용 금지!)

❌ `BOSS_DMG_PCT` → 오타! 올바른 이름: `BOSS_DMG_UP` ✅
❌ `BOSS_DMG_MULT` → CSV에 없음!
❌ `BOSS_CRIT_RATE` → CSV에 없음!
❌ `ELITE_GOLD_MULT` → CSV에 없음!

### 면역 시스템 (제거됨)

🔒 다음 ID들은 **보스 저항 시스템 전용**으로 이동했습니다:
- `BOSS_BIND_IMMUNE` (속박 면역)
- `BOSS_SLOW_IMMUNE` (슬로우 면역)
- `BOSS_POISON_IMMUNE` (독 면역)
- `BOSS_BURN_IMMUNE` (화상 면역)

📁 위치: `Assets/Resources/Combat/CSV/BossStatusResistMastery.csv`
💡 용도: 나중에 보스 저항 시스템에서 사용 예정

### 스케일링 규칙

- **주옵션**: 레벨업 시 `mainStatLevelGrowth` 배율만큼 증가
  - 예: Lv.1 = 0.2, Lv.15 = 0.2 × 1.7 = 0.34 (70% 증가)
  
- **부옵션**: 레벨 스케일링 없음 (원본 값 유지)

---

## 🔍 디버그 도구

### 모든 룬의 주옵션 확인
```
Tools → Rune System → Test Phase 4 → 3-2. 🔍 디버그: 모든 룬의 주옵션 확인
```

### ID 참고표 출력
```
Tools → Rune System → Fix Rune Data → 2. 주옵션 ID 참고표 출력
```

---

## 📚 관련 파일

- **CSV 데이터**: `Assets/Resources/Combat/CSV/ConditionalModifier.csv`
- **데이터베이스**: `ConditionalModifierDatabase.cs`
- **룬 데이터**: `Assets/Data/Runes/Rune_*.asset`

---

**업데이트 히스토리:**
- 2026-02-27: 초기 작성, CSV 기준으로 12개 ID 정리
- 2026-02-27 (v2): 면역 시스템 4개 제거, 룬 시스템 전용 8개로 정리
  - 제거된 항목: BOSS_BIND_IMMUNE, BOSS_SLOW_IMMUNE, BOSS_POISON_IMMUNE, BOSS_BURN_IMMUNE
  - 이동 위치: BossStatusResistMastery.csv (보스 저항 시스템 전용)
