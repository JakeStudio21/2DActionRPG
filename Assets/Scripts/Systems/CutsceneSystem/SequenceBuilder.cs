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
        /// CutsceneData로부터 Sequence 생성
        /// </summary>
        public static Sequence BuildSequence(CutsceneData data, CutsceneContext context, GameObject linkTarget)
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
            
            // Sequence 생성
            Sequence sequence = DOTween.Sequence();
            
            // Sequence 정책 설정
            sequence.SetAutoKill(true);  // 끝나면 자동 정리
            sequence.SetLink(linkTarget); // 오브젝트 파괴 시 자동 Kill
            sequence.SetUpdate(true);     // Time.timeScale 무시 (컷신은 게임 일시정지와 무관하게 진행)
            
            // 각 Step을 Sequence에 추가
            foreach (var step in data.steps)
            {
                Tween stepTween = CutsceneStepExecutor.ExecuteStep(step, context);
                
                if (stepTween != null)
                {
                    sequence.Append(stepTween);
                }
                else
                {
                    Debug.LogWarning($"[SequenceBuilder] Step 트윈 생성 실패: {step.stepType}");
                }
            }
            
            // 완료 콜백은 CutsceneManager에서 처리
            
            Debug.Log($"[SequenceBuilder] Sequence 생성 완료: {data.cutsceneId} ({data.steps.Count}개 Step)");
            
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
