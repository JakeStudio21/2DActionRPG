# 🎵 Phase 3: 컷신 중 BGM 제어 완벽 가이드

## ✅ Phase 3 완료 내역

### **구현된 기능**
1. ✅ **컷신 우선순위 BGM** - CutsceneData에 bgmEventKey 추가
2. ✅ **BGMController 연동** - AddState/RemoveState 메서드 추가
3. ✅ **컷신 중 BGM 제어** - BGM Step 타입 추가

---

## 🎯 컷신 중 BGM 제어 방법 3가지

### **방법 1: CutsceneData 전체 BGM 설정** (간단)
컷신 전체에 대해 하나의 BGM을 설정합니다.

**사용 시기**:
- 전체 컷신이 동일한 분위기일 때
- 간단한 인트로/아웃트로

**설정 방법**:
```
CutsceneData Inspector에서:
- BGM Event Key: "bgm.cutscene.intro"
- Resume Previous BGM: ✅ 체크
```

**동작**:
```
컷신 시작 → "bgm.cutscene.intro" 재생
컷신 종료 → 이전 BGM 복귀
```

---

### **방법 2: BGM Step 사용** (추천 ⭐)
컷신 중간에 BGM을 동적으로 변경합니다.

**사용 시기**:
- 컷신 중간에 분위기가 바뀔 때
- 긴장감/반전이 있는 스토리텔링
- 여러 장면으로 구성된 컷신

**설정 방법**:
```
CutsceneData Inspector에서:
1. Step 추가 (+ 버튼)
2. Step Type: "BGM" 선택
3. BGM Event Key: "bgm.cutscene.dramatic"
4. BGM Fade Time: 1초 (페이드 시간)
5. BGM Wait Time: 0초 (BGM 전환 후 대기 시간)
```

**예시 시나리오**:
```
=== 컷신: "평화에서 위기로" ===

Step 1 [Image]: 평화로운 마을 배경
Step 2 [Dialogue]: "오늘도 평화로운 하루군요."
Step 3 [BGM]: bgm.cutscene.peaceful (평화로운 BGM)
Step 4 [Wait]: 1초 대기
Step 5 [Image]: 어두운 그림자 (Fade In)
Step 6 [BGM]: bgm.cutscene.dramatic (긴장감 있는 BGM)
Step 7 [Dialogue]: "뭔가 이상한데..."
Step 8 [Image]: 적 출현!
Step 9 [BGM]: bgm.cutscene.battle (전투 BGM)
Step 10 [Dialogue]: "전투 준비!"
```

**실행 결과**:
```
🎵 평화로운 BGM 재생 (Step 3)
   ↓ (1초 대기)
🎵 긴장감 BGM으로 Fade 전환 (Step 6, 1초 페이드)
   ↓ (대사 진행)
🎵 전투 BGM으로 Fade 전환 (Step 9, 1초 페이드)
```

---

### **방법 3: Callback을 통한 BGM 제어** (고급)
코드를 통해 BGM을 직접 제어합니다.

**사용 시기**:
- 조건부 BGM 변경 (플레이어 선택에 따라)
- 복잡한 로직이 필요한 경우
- 런타임 데이터 기반 BGM 변경

**설정 방법**:

1. **Callback 메서드 작성** (예: `CutsceneCallbacks.cs`)
```csharp
using UnityEngine;

public class CutsceneCallbacks : MonoBehaviour
{
    private void Start()
    {
        // 컷신 매니저에 콜백 등록
        if (CutsceneSystem.CutsceneManager.Instance != null)
        {
            CutsceneSystem.CutsceneManager.Instance.RegisterCallback("ChangeToDramaticBGM", OnChangeToDramaticBGM);
            CutsceneSystem.CutsceneManager.Instance.RegisterCallback("ChangeToHappyBGM", OnChangeToHappyBGM);
            CutsceneSystem.CutsceneManager.Instance.RegisterCallback("RestorePreviousBGM", OnRestorePreviousBGM);
        }
    }
    
    private void OnChangeToDramaticBGM()
    {
        Debug.Log("🎵 [Callback] Dramatic BGM으로 전환!");
        
        if (BGMController.Instance != null)
        {
            BGMController.Instance.AddState(
                BGMController.BGMPriority.Cutscene, 
                "bgm.cutscene.dramatic"
            );
        }
    }
    
    private void OnChangeToHappyBGM()
    {
        Debug.Log("🎵 [Callback] Happy BGM으로 전환!");
        
        if (BGMController.Instance != null)
        {
            BGMController.Instance.AddState(
                BGMController.BGMPriority.Cutscene, 
                "bgm.cutscene.happy"
            );
        }
    }
    
    private void OnRestorePreviousBGM()
    {
        Debug.Log("🎵 [Callback] 이전 BGM으로 복귀!");
        
        if (BGMController.Instance != null)
        {
            BGMController.Instance.RemoveState(
                BGMController.BGMPriority.Cutscene
            );
        }
    }
    
    private void OnDestroy()
    {
        // 콜백 해제
        if (CutsceneSystem.CutsceneManager.Instance != null)
        {
            CutsceneSystem.CutsceneManager.Instance.UnregisterCallback("ChangeToDramaticBGM");
            CutsceneSystem.CutsceneManager.Instance.UnregisterCallback("ChangeToHappyBGM");
            CutsceneSystem.CutsceneManager.Instance.UnregisterCallback("RestorePreviousBGM");
        }
    }
}
```

2. **CutsceneData에서 Callback Step 추가**
```
Step Type: "Callback"
Callback Method Name: "ChangeToDramaticBGM"
```

**예시 시나리오**:
```
Step 1 [Dialogue]: "당신은 어떻게 하시겠습니까?"
Step 2 [Callback]: "ShowChoiceDialog" (플레이어 선택 UI 표시)
Step 3 [Wait]: 5초 (선택 대기)
Step 4 [Callback]: "ProcessPlayerChoice" (선택에 따라 BGM 변경)
   → 선택 A: ChangeToDramaticBGM()
   → 선택 B: ChangeToHappyBGM()
Step 5 [Dialogue]: (선택에 따른 후속 대사)
```

---

## 🎮 실전 예시: 보스 등장 컷신

### **시나리오**
```
1. 평화로운 탐험 중 보스의 기운 감지
2. 어두운 분위기로 전환
3. 보스 등장
4. 전투 돌입
```

### **CutsceneData 설정**

```
=== 기본 설정 ===
Cutscene ID: "BossIntro_001"
Cutscene Name: "보스 등장 컷신"
BGM Event Key: "" (비워둠, Step에서 제어)
Resume Previous BGM: ✅ 체크

=== Steps ===

[Step 1] Image
- Image Sprite: Background_Forest
- Is Fade: false
- Is Portrait: false
- Fade In: true
- Image Duration: 1초

[Step 2] Dialogue
- Speaker Name: "플레이어"
- Dialogue Text: "이 숲은 너무 조용한데..."
- Typing Speed: 20

[Step 3] Wait
- Duration: 0.5초

[Step 4] SFX
- SFX Event Key: "sfx.thunder" (천둥 효과음)
- Duration: 0초

[Step 5] Image
- Image Sprite: Background_Forest_Dark (어두운 버전)
- Fade In: true
- Image Duration: 1초

[Step 6] BGM
- BGM Event Key: "bgm.cutscene.ominous"
- BGM Fade Time: 2초
- BGM Wait Time: 0.5초

[Step 7] Dialogue
- Speaker Name: ""
- Dialogue Text: "뭔가 강력한 존재가 다가오고 있다..."
- Typing Speed: 15

[Step 8] Image
- Image Sprite: Portrait_Boss_Silhouette
- Is Portrait: true
- Fade In: true
- Image Duration: 2초

[Step 9] BGM
- BGM Event Key: "bgm.stage.boss"
- BGM Fade Time: 1초
- BGM Wait Time: 0초

[Step 10] Image
- Image Sprite: Portrait_Boss_Reveal
- Is Portrait: true
- Fade In: true
- Image Duration: 1.5초

[Step 11] SFX
- SFX Event Key: "sfx.boss_roar"
- Duration: 0초

[Step 12] Dialogue
- Speaker Name: "보스"
- Dialogue Text: "어리석은 인간이여, 여기까지 오다니..."
- Typing Speed: 20

[Step 13] Wait
- Duration: 1초

[Step 14] Image
- Image Sprite: Fade_Black
- Is Fade: true
- Fade In: true
- Image Duration: 1초

[Step 15] Callback
- Callback Method Name: "StartBossBattle"
```

### **예상 실행 흐름**

```
00:00 - 평화로운 숲 배경 (탐험 BGM 계속 재생)
00:01 - "이 숲은 너무 조용한데..."
00:03 - ⚡ 천둥 소리!
00:03 - 숲이 어두워짐 (Fade)
00:04 - 🎵 불길한 BGM으로 전환 (2초 페이드)
00:06 - "뭔가 강력한 존재가..."
00:08 - 보스 실루엣 등장
00:10 - 🎵 보스 BGM으로 전환 (1초 페이드)
00:11 - 보스 얼굴 공개!
00:11 - 🔊 보스 포효!
00:12 - "어리석은 인간이여..."
00:15 - 1초 대기
00:16 - 화면 암전 (Fade to Black)
00:17 - 전투 시작! (Callback)
00:17 - 컷신 종료 → 보스 BGM 계속 재생 (이미 Boss Priority)
```

---

## 📊 BGM 우선순위 스택 예시

### **시나리오: 전투 중 컷신 재생**

```
1. 전투 중 (Battle BGM 재생 중)
2. 보스 등장 컷신 트리거
3. 컷신 내에서 BGM 전환
4. 컷신 종료 후 전투 복귀
```

**BGM 스택 변화**:
```
=== 전투 시작 ===
{ Default: "bgm.stage.default", Battle: "bgm.stage.battle" }
→ 전투 BGM 재생 (Battle > Default)

=== 컷신 시작 (CutsceneData.bgmEventKey = "") ===
{ Default: "...", Battle: "..." }
→ 전투 BGM 계속 재생 (컷신 BGM 설정 안 함)

=== 컷신 Step 3: BGM Step 실행 ===
{ Default: "...", Battle: "...", Cutscene: "bgm.cutscene.dramatic" }
→ 컷신 BGM 재생 (Cutscene > Battle > Default) ⭐

=== 컷신 Step 7: BGM Step 실행 ===
{ Default: "...", Battle: "...", Cutscene: "bgm.stage.boss" }
→ 보스 BGM 재생 (Cutscene > Battle > Default) ⭐

=== 컷신 종료 ===
{ Default: "...", Battle: "..." }
→ 전투 BGM 복귀 ✅ (Cutscene 우선순위 제거)
```

---

## 🎯 사용 팁 & 모범 사례

### **1. BGM Event Key 네이밍 규칙**
```
bgm.cutscene.{분위기}

예시:
- bgm.cutscene.peaceful (평화로운)
- bgm.cutscene.dramatic (긴장감)
- bgm.cutscene.sad (슬픈)
- bgm.cutscene.happy (행복한)
- bgm.cutscene.mysterious (신비로운)
- bgm.cutscene.epic (웅장한)
```

### **2. Fade Time 설정 가이드**
```
- 즉시 전환: 0초 (충격적인 순간)
- 빠른 전환: 0.5초 (긴박한 상황)
- 일반 전환: 1초 (대부분의 경우)
- 부드러운 전환: 2초 (분위기 전환)
- 매우 부드러운 전환: 3초 (긴 씬 전환)
```

### **3. BGM Wait Time 활용**
```
- 0초: 즉시 다음 Step (일반적)
- 0.5~1초: BGM이 완전히 전환된 후 대사 진행
- 1~2초: BGM의 인트로를 들려주고 싶을 때
```

### **4. Step 순서 최적화**
```
✅ 좋은 예:
Step 1 [Image]: 배경 설정
Step 2 [BGM]: BGM 전환 (먼저!)
Step 3 [Dialogue]: 대사 (BGM 전환 중 진행)

❌ 나쁜 예:
Step 1 [Dialogue]: 대사 (BGM이 아직 안 바뀜)
Step 2 [BGM]: BGM 전환 (대사 진행 중 바뀜 - 어색)
```

### **5. Resume Previous BGM 활용**
```
CutsceneData의 "Resume Previous BGM" 체크:

✅ 체크: 컷신 종료 후 이전 BGM으로 복귀 (일반적)
❌ 체크 해제: 컷신 BGM이 그대로 유지
   → 컷신이 게임플레이로 자연스럽게 이어질 때
   → 예: 튜토리얼 컷신 → 튜토리얼 전투 (같은 BGM)
```

---

## 🧪 테스트 체크리스트

### **기본 테스트**
- [ ] CutsceneData의 bgmEventKey가 정상 재생됨
- [ ] 컷신 종료 시 이전 BGM으로 복귀됨
- [ ] BGM Step이 정상 작동함
- [ ] BGM Fade가 자연스러움

### **BGM Step 테스트**
- [ ] 컷신 중간에 BGM이 정상 전환됨
- [ ] Fade Time이 올바르게 적용됨
- [ ] Wait Time이 올바르게 적용됨
- [ ] 여러 BGM Step이 순차적으로 작동함

### **우선순위 테스트**
- [ ] 컷신 BGM이 전투 BGM보다 우선함 (Cutscene > Battle)
- [ ] 컷신 종료 후 전투 BGM으로 정상 복귀함
- [ ] 보스 BGM 중 컷신 재생 시 컷신 BGM이 우선함

### **Callback 테스트**
- [ ] Callback을 통한 BGM 제어가 작동함
- [ ] 조건부 BGM 변경이 작동함
- [ ] Callback 등록/해제가 정상 작동함

---

## 🔍 문제 해결

### **문제 1: BGM Step이 작동하지 않음**
```
증상: BGM Step이 있는데도 BGM이 안 바뀜

확인 사항:
1. Console에서 "[BGMStepExecutor] BGM Step 시작" 로그 확인
2. BGM Event Key가 CueProfile에 등록되어 있는지 확인
3. BGMController.Instance가 null이 아닌지 확인
```

### **문제 2: 컷신 종료 후 BGM이 안 돌아옴**
```
증상: 컷신 종료 후 BGM이 멈춤

확인 사항:
1. CutsceneData의 "Resume Previous BGM" 체크 확인
2. 이전 BGM 상태가 스택에 남아있는지 확인
3. Console에서 "컷신 BGM 종료, 이전 BGM으로 복귀" 로그 확인
```

### **문제 3: BGM이 여러 개 중복 재생됨**
```
증상: 두 개 이상의 BGM이 동시에 들림

원인: BGM Step에서 RemoveState를 호출하지 않음

해결: BGM Step은 AddState만 호출 (Cutscene 우선순위 유지)
     컷신 종료 시 CutsceneManager가 자동으로 RemoveState 호출
```

---

## ✅ Phase 3 완료 확인

### **구현 완료 체크리스트**
- [x] CutsceneStepType에 BGM 타입 추가
- [x] CutsceneStep에 BGM 필드 추가
- [x] BGMStepExecutor 구현
- [x] CutsceneStepExecutor에 BGMStepExecutor 등록
- [x] 테스트 가이드 작성
- [x] 예시 시나리오 작성

### **테스트 완료 확인**
Unity에서 다음 테스트를 완료하세요:
1. 간단한 BGM Step 테스트 (1개 BGM 전환)
2. 복잡한 BGM Step 테스트 (여러 BGM 전환)
3. 전투 중 컷신 재생 테스트 (우선순위 확인)
4. Callback을 통한 BGM 제어 테스트 (선택적)

---

## 🎉 Phase 3 완료!

**축하합니다!** 컷신 시스템의 BGM 통합이 완료되었습니다!

이제 컷신에서:
- ✅ 전체 컷신 BGM 설정 가능
- ✅ 컷신 중간에 BGM 동적 전환 가능
- ✅ 우선순위 시스템으로 자동 복귀
- ✅ Callback을 통한 고급 제어

**다음 단계**:
- Phase 2 (고급 타이핑 효과) 진행 또는
- 다른 게임 시스템 개발로 이동!

질문이나 문제가 있으면 알려주세요! 😊


