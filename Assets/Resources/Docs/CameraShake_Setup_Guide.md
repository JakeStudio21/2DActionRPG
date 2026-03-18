# 카메라 쉐이킹 에디터 설정 가이드

> 최종 업데이트: 2026-03
>
> **아키텍처 원칙**
> - `DamageArea` (Logic Layer) — 물리/데미지 판정만 담당
> - `CueProfile` (Presentation Layer) — VFX · SFX · Shake를 통합 관리
> - 모든 카메라 쉐이킹은 `CueEntry.shakeData` → `ScreenShakeManager.PlayShake()` 단일 경로로 처리

---

## 전체 흐름 요약

```
스킬 에셋 (hitCueKey 설정)
        ↓
DamageArea — 피격 대상 위치에서 CueEmitter.Emit(hitCueKey)
        ↓
CueProfile — hitCueKey 에 대응하는 CueEntry 조회
        ↓
CuePlayer — VFX 재생 + SFX 재생 + ShakeData → ScreenShakeManager.PlayShake()
        ↓
ScreenShakeManager — globalShakeMultiplier 적용 후 Cinemachine Impulse 발동
```

---

## Step 1. ScreenShakeManager 씬 배치 확인

> 이미 배치되어 있다면 이 단계는 건너뜁니다.

1. **Hierarchy** 창에서 `ScreenShakeManager` GameObject 존재 여부 확인
2. 없으면 빈 GameObject 생성 → 이름을 `ScreenShakeManager`로 변경
3. 아래 컴포넌트 추가:
   - `ScreenShakeManager` 스크립트
   - `CinemachineImpulseSource` 컴포넌트

### Inspector 설정값

| 필드 | 권장값 | 설명 |
|------|--------|------|
| `Global Shake Multiplier` | `1.0` | 0 = 진동 없음, 1 = 원본 강도 |
| `Shake Cooldown` | `0.05` | 중복 호출 방어 쿨다운 (초) |
| `Show Debug Logs` | `false` | 테스트 시 `true`로 변경 |

### CinemachineImpulseSource 설정값

| 필드 | 권장값 |
|------|--------|
| `Impulse Channel` | `1` |
| `Raw Signal` | `Bounce` 또는 `Rebound` |
| `Amplitude Gain` | `1` |
| `Frequency Gain` | `1` |
| `Sustain Time` | `0.2` (ShakeData.duration으로 런타임에 덮어씌워짐) |
| `Decay Time` | `0.5` |

---

## Step 2. Cinemachine Camera 리스너 설정

1. **Hierarchy** 창에서 Cinemachine 카메라 GameObject 선택
2. `CinemachineImpulseListener` 컴포넌트 추가
3. Inspector 설정:

| 필드 | 값 |
|------|----|
| `Channel Mask` | `1` (ScreenShakeManager와 동일) |
| `Gain` | `1.0` |
| `Use 2D Distance` | `true` (2D 게임) |
| `Use Listener Position` | `false` |

---

## Step 3. CueProfile에 hitCueKey 등록

> **플레이어 스킬** → `Player_assasin_profile.asset` (또는 해당 직업 프로필)
> **적 스킬** → 해당 몬스터 프로필 (예: `Enemy_Spider_profile.asset`)

### 3-1. 프로필 열기

`Assets/Resources/CueProfiles/` 폴더에서 해당 프로필 에셋 선택

### 3-2. VFX 등록 (VFX Cues 섹션)

피격 이펙트 프리팹이 있다면 VFX Cue를 등록합니다.

| 필드 | 설정값 예시 |
|------|-------------|
| `VFX Id` | `hit_slash` |
| `Pool Key` | 프리팹 풀 이름 (예: `Fx_Hit_Slash`) |
| `Duration` | `-1` (자동 감지) |
| `Rotation Mode` | `Fixed` (타격 이펙트는 방향 고정) |

### 3-3. SFX 등록 (SFX Cues 섹션)

타격음이 있다면 SFX Cue를 등록합니다.

| 필드 | 설정값 예시 |
|------|-------------|
| `SFX Id` | `sfx_hit_slash` |
| `Audio Clip` | 타격 AudioClip 할당 |
| `Volume` | `1.0` |
| `Pitch` | `1.0` |

### 3-4. CueEntry 등록 (Entries 섹션)

**Entries** 리스트에 항목 추가:

| 필드 | 설정값 예시 | 설명 |
|------|-------------|------|
| `Event Key` | `skill.assasin.multi_arrow.hit` | 스킬 에셋의 hitCueKey와 **반드시 일치** |
| `VFX Ids` | `hit_slash` | 위에서 등록한 VFX Id |
| `SFX Ids` | `sfx_hit_slash` | 위에서 등록한 SFX Id |
| `Priority` | `50` | |

### 3-5. ShakeData 설정 (핵심)

같은 CueEntry 안의 **Shake Data** 펼치기:

| 필드 | 가이드값 | 설명 |
|------|----------|------|
| `Use Shake` | ✅ `true` | **반드시 true여야 진동 발동** |
| `Intensity` | `0.3` ~ `1.0` | 일반 타격 0.3 / 강공격 0.6 / 보스 1.0 |
| `Duration` | `0.15` ~ `0.3` | 일반 0.2 / 강공격 0.3 |
| `Delay` | `0` | 타격 즉시 발동 (필요 시 예비 딜레이) |

> **강도 참고표**
>
> | 상황 | Intensity | Duration |
> |------|-----------|----------|
> | 일반 플레이어 스킬 타격 | `0.3` | `0.15` |
> | 강화 스킬 / 광역기 타격 | `0.5` | `0.2` |
> | 보스 돌진 / 강타 | `0.8` | `0.25` |
> | 보스 필살기 | `1.0` | `0.3` |

---

## Step 4. 스킬 에셋에 hitCueKey 입력

### 플레이어 스킬 (ActiveSkillData)

`Assets/Resources/Skills/Active/` 에서 해당 스킬 에셋 선택

**CueSystem 이벤트 키 섹션**에서:

| 필드 | 예시 | 설명 |
|------|------|------|
| `Cast Cue Key` | `skill.assasin.multi_arrow.cast` | 시전 이펙트 키 (기존 필드) |
| `Aoe Cue Key` | `skill.assasin.multi_arrow.aoe` | AOE 범위 이펙트 키 (기존 필드) |
| `Hit Cue Key` | `skill.assasin.multi_arrow.hit` | **타격 연출 키 (신규)** — CueEntry의 Event Key와 정확히 일치 |

> `Hit Cue Key`가 비어 있으면 피격 시 아무 연출도 발동하지 않습니다.

### 적 스킬 (SkillData)

`Assets/Resources/Skills/` 에서 해당 스킬 에셋 선택

**CueSystem 이벤트 키 섹션**에서:

| 필드 | 예시 |
|------|------|
| `Hit Cue Key` | `skill.boss_dash.hit` |

---

## Step 5. CueProfile 도메인 연결 확인

`CueEmitter.Emit(hitCueKey, domain, context)` 에서 `domain`은 다음과 같이 결정됩니다:

| 시전자 | domain 값 |
|--------|-----------|
| 플레이어 스킬 (DamageArea) | `"Player"` |
| 적 스킬 (DamageArea) | `baseEnemy.CueEmitDomain` |
| 보스 스킬 직접 호출 | `baseEnemy.CueEmitDomain` |

**CueProfile 파일명 규칙**: `{Domain}_{profile_name}.asset`
- `Player_assasin_profile.asset` → domain = `"Player"`
- `Enemy_Spider_profile.asset` → domain = `"Enemy_Spider"` (BaseEnemy에서 자동 설정)

---

## Step 6. 동작 테스트 체크리스트

```
□ ScreenShakeManager GameObject가 씬에 존재하는가?
□ CinemachineImpulseSource 컴포넌트가 붙어 있는가?
□ Cinemachine 카메라에 CinemachineImpulseListener가 있는가?
□ Channel Mask가 ScreenShakeManager와 일치하는가? (기본값 1)
□ 스킬 에셋의 Hit Cue Key가 입력되어 있는가?
□ CueProfile에 동일한 Event Key의 CueEntry가 등록되어 있는가?
□ CueEntry.ShakeData.useShake = true 인가?
□ 몬스터가 없는 빈 공간에 스킬을 쏠 때 카메라가 흔들리지 않는가? ← 핵심 검증
□ 몬스터를 맞혔을 때 각 피격 위치에서 이펙트가 개별 발동하는가?
```

---

## 설정 예시: MultiShot 스킬 (암살자)

**`Player_assasin_profile.asset` → Entries 추가**

```
Event Key   : skill.assasin.multi_arrow.hit
VFX Ids     : [hit_arrow]
SFX Ids     : [sfx_arrow_hit]
ShakeData
  useShake  : true
  intensity : 0.3
  duration  : 0.15
  delay     : 0
```

**`ActiveSkill_MultiShot.asset`**

```
Hit Cue Key : skill.assasin.multi_arrow.hit
```

---

## 설정 예시: Arrow Rain 스킬 (광역기)

**`Player_assasin_profile.asset` → Entries 추가**

```
Event Key   : skill.assasin.arrow_rain.hit
VFX Ids     : [hit_arrow_rain]
SFX Ids     : [sfx_arrow_impact]
ShakeData
  useShake  : true
  intensity : 0.5
  duration  : 0.2
  delay     : 0
```

> Arrow Rain처럼 여러 몬스터를 동시에 타격하면 각 몬스터 위치에서 개별 Emit이 호출됩니다.
> ScreenShakeManager의 **Throttling(0.05초 쿨다운)** 이 자동으로 중복 호출을 방어하므로 진동이 과도하게 중첩되지 않습니다.

---

## 자주 묻는 문제

**Q. 스킬을 맞혔는데 카메라가 전혀 안 흔들린다**
- 스킬 에셋 `Hit Cue Key` 입력 여부 확인
- CueEntry `Event Key`와 오타 없이 정확히 일치하는지 확인
- CueEntry `ShakeData.useShake = true` 확인
- `ScreenShakeManager.Global Shake Multiplier > 0` 확인

**Q. 몬스터 없는 빈 공간에서도 카메라가 흔들린다**
- `CuePlayer.cs`에서 castCueKey / aoeCueKey에 ShakeData가 설정되어 있을 수 있음
- 시전 연출(cast, aoe) CueEntry의 `ShakeData.useShake = false` 로 설정하거나 제거

**Q. 여러 몬스터를 동시에 맞히면 진동이 너무 강하다**
- `ScreenShakeManager.Shake Cooldown` 값을 높이거나 (기본 `0.05`)
- ShakeData `intensity`를 낮춤

**Q. 진동이 너무 길게 지속된다**
- CueEntry `ShakeData.duration` 값을 낮춤 (권장: `0.15 ~ 0.25`)
