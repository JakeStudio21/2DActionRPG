using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 Sequence 빌더
    /// CutsceneData를 DOTween Sequence로 변환
    /// </summary>
    public static class SequenceBuilder
    {
        /// <summary>
        /// CutsceneData로부터 Sequence 생성 (확장 버전)
        /// </summary>
        public static Sequence BuildSequence(
            CutsceneData data, 
            CutsceneContext context, 
            GameObject linkTarget,
            CutsceneStepExecutor executor)
        {
            if (data == null)
            {
                Debug.LogError("[SequenceBuilder] CutsceneData가 null입니다!");
                return null;
            }
            
            if (!data.IsValid(out string errorMessage))
            {
                Debug.LogError($"[SequenceBuilder] 유효하지 않은 CutsceneData: {errorMessage}");
                return null;
            }
            
            if (executor == null)
            {
                Debug.LogError("[SequenceBuilder] CutsceneStepExecutor가 null입니다!");
                return null;
            }
            
            // Sequence 생성
            Sequence sequence = DOTween.Sequence();
            
            // Sequence 정책 설정
            sequence.SetAutoKill(true);
            sequence.SetLink(linkTarget);
            sequence.SetUpdate(true);
            
            // 각 Step을 Sequence에 추가
            int stepIndex = 0;
            float accumulatedTime = 0f;
            
            foreach (var step in data.steps)
            {
                Tween stepTween = executor.ExecuteStep(step, context);
                
                if (stepTween != null)
                {
                    // Step 종료 시간 계산
                    float stepDuration = stepTween.Duration();
                    accumulatedTime += stepDuration;
                    float capturedEndTime = accumulatedTime;
                    
                    // OnPlay 콜백으로 endTime 설정
                    stepTween.OnPlay(() => {
                        context.currentStepEndTime = capturedEndTime;
                    });
                    
                    sequence.Append(stepTween);
                    stepIndex++;
                }
                else
                {
                    Debug.LogWarning($"[SequenceBuilder] Step 트윈 생성 실패: {step.stepType}");
                }
            }
            
            Debug.Log($"[SequenceBuilder] ✅ Sequence 생성 완료: {data.cutsceneId} ({data.steps.Count}개 Step)");
            
            return sequence;
        }
        
        /// <summary>
        /// Sequence에 정책 적용 (확장용)
        /// </summary>
        public static void ApplySequencePolicies(Sequence sequence, GameObject linkTarget, bool ignoreTimeScale = false)
        {
            if (sequence == null)
                return;
            
            sequence.SetAutoKill(true);
            sequence.SetLink(linkTarget);
            
            if (ignoreTimeScale)
            {
                sequence.SetUpdate(true); // 게임 Pause 되어도 컷신 진행
            }
        }
    }
}
