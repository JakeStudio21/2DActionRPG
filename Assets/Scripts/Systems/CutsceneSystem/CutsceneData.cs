using UnityEngine;
using System.Collections.Generic;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 타입 정의
    /// </summary>
    public enum CutsceneType
    {
        Intro,          // 게임 인트로
        ChapterMid,     // 챕터 중간 컷신
        Tutorial        // 튜토리얼 컷신
    }

    /// <summary>
    /// 컷신 데이터 ScriptableObject
    /// 하나의 컷신 전체를 정의하는 데이터 에셋
    /// </summary>
    [CreateAssetMenu(fileName = "CutsceneData", menuName = "CutsceneSystem/Cutscene Data", order = 1)]
    public class CutsceneData : ScriptableObject
    {
        [Header("=== 기본 정보 ===")]
        [Tooltip("컷신 고유 ID (예: Intro_001, CH1_MID_002)")]
        public string cutsceneId = "";
        
        [Tooltip("컷신 이름 (Inspector에서 식별용)")]
        public string cutsceneName = "";
        
        [Tooltip("컷신 타입")]
        public CutsceneType cutsceneType = CutsceneType.Intro;
        
        [Header("=== 컷신 Step 목록 ===")]
        [Tooltip("이 컷신을 구성하는 Step들 (순서대로 실행)")]
        public List<CutsceneStep> steps = new List<CutsceneStep>();
        
        [Header("=== 설정 ===")]
        [Tooltip("컷신 시작 시 게임 일시정지 여부")]
        public bool pauseGameOnStart = true;
        
        [Tooltip("스킵 가능 여부")]
        public bool canSkip = true;
        
        [Tooltip("한 번만 재생 (이미 재생한 경우 스킵)")]
        public bool playOnce = false;
        
        [Header("=== Canvas 설정 ===")]
        [Tooltip("사용할 Canvas 프리팹 경로 (비어있으면 기본값 사용)")]
        public string canvasPrefabPath = "Prefabs/Cutscene/CutsceneCanvas";
        
        [Header("=== BGM 설정 (선택사항) ===")]
        [Tooltip("컷신 전용 BGM 이벤트 키 (비어있으면 BGM 변경 안함)\n예: bgm.cutscene.intro")]
        public string bgmEventKey = "";
        
        [Tooltip("컷신 종료 시 이전 BGM으로 복귀 여부")]
        public bool resumePreviousBGM = true;
        
        [Header("=== 디버그 ===")]
        [Tooltip("컷신 설명 (기획 메모)")]
        [TextArea(3, 5)]
        public string description = "";
        
        /// <summary>
        /// 전체 컷신 예상 재생 시간 계산
        /// </summary>
        public float CalculateTotalDuration()
        {
            float totalDuration = 0f;
            
            foreach (var step in steps)
            {
                switch (step.stepType)
                {
                    case CutsceneStepType.Image:
                        totalDuration += step.imageDuration;
                        break;
                    
                    case CutsceneStepType.Dialogue:
                        // 타이핑 시간 계산
                        if (step.typingSpeed > 0)
                        {
                            float typingTime = step.dialogueText.Length / step.typingSpeed;
                            totalDuration += typingTime;
                        }
                        else
                        {
                            // 즉시 표시인 경우 기본 대기 시간
                            totalDuration += 0.5f;
                        }
                        break;
                    
                    case CutsceneStepType.Wait:
                        totalDuration += step.duration;
                        break;
                    
                    case CutsceneStepType.SFX:
                        // SFX는 즉시 실행되므로 시간 추가 안 함
                        break;
                    
                    case CutsceneStepType.Callback:
                        // Callback은 즉시 실행되므로 시간 추가 안 함
                        break;
                }
            }
            
            return totalDuration;
        }
        
        /// <summary>
        /// 컷신 데이터 유효성 검증
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            // 1. ID 체크
            if (string.IsNullOrEmpty(cutsceneId))
            {
                errorMessage = "컷신 ID가 비어있습니다. '기본 정보' 섹션에서 컷신 ID를 입력해주세요. (예: Intro_001)";
                return false;
            }
            
            // 2. 이름 체크 (선택사항이지만 권장)
            if (string.IsNullOrEmpty(cutsceneName))
            {
                Debug.LogWarning($"[CutsceneData] 컷신 ID '{cutsceneId}'의 이름이 비어있습니다. Inspector에서 식별을 위해 이름을 입력하는 것을 권장합니다.");
            }
            
            // 3. Step 체크
            if (steps == null || steps.Count == 0)
            {
                errorMessage = "Step이 하나도 없습니다. '컷신 Step 목록'에서 최소 1개 이상의 Step을 추가해주세요.";
                return false;
            }
            
            // 4. 각 Step 유효성 검증
            for (int i = 0; i < steps.Count; i++)
            {
                if (!steps[i].IsValid())
                {
                    string stepInfo = steps[i].GetDisplayName();
                    errorMessage = $"Step {i + 1} ({steps[i].stepType})이 유효하지 않습니다.\n" +
                                  $"Step 정보: {stepInfo}\n" +
                                  $"필요한 필드를 확인해주세요.";
                    return false;
                }
            }
            
            errorMessage = "";
            return true;
        }
        
        /// <summary>
        /// Inspector에서 컷신 정보 요약 표시
        /// </summary>
        public string GetSummary()
        {
            int imageCount = 0;
            int dialogueCount = 0;
            int sfxCount = 0;
            
            foreach (var step in steps)
            {
                switch (step.stepType)
                {
                    case CutsceneStepType.Image:
                        imageCount++;
                        break;
                    case CutsceneStepType.Dialogue:
                        dialogueCount++;
                        break;
                    case CutsceneStepType.SFX:
                        sfxCount++;
                        break;
                }
            }
            
            float totalDuration = CalculateTotalDuration();
            
            return $"ID: {cutsceneId}\n" +
                   $"타입: {cutsceneType}\n" +
                   $"총 Step: {steps.Count}개\n" +
                   $"  - 이미지: {imageCount}개\n" +
                   $"  - 대사: {dialogueCount}개\n" +
                   $"  - 효과음: {sfxCount}개\n" +
                   $"예상 재생 시간: {totalDuration:F1}초";
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Inspector에서 버튼으로 호출할 수 있는 테스트 메서드
        /// </summary>
        [ContextMenu("컷신 정보 출력")]
        private void PrintCutsceneInfo()
        {
            Debug.Log($"=== {cutsceneName} ===\n{GetSummary()}");
            
            Debug.Log("\n=== Step 목록 ===");
            for (int i = 0; i < steps.Count; i++)
            {
                Debug.Log($"{i + 1}. {steps[i].GetDisplayName()}");
            }
        }
        
        [ContextMenu("유효성 검증")]
        private void ValidateData()
        {
            if (IsValid(out string errorMessage))
            {
                Debug.Log($"✅ [{cutsceneName}] 유효성 검증 통과!");
            }
            else
            {
                Debug.LogError($"❌ [{cutsceneName}] 유효성 검증 실패: {errorMessage}");
            }
        }
#endif
    }
}
