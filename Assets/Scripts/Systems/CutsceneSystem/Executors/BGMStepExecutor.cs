using UnityEngine;
using DG.Tweening;

namespace CutsceneSystem
{
    /// <summary>
    /// BGM Step Executor - 컷신 중 BGM 전환
    /// Phase 3: BGM+컷신 통합
    /// </summary>
    public class BGMStepExecutor : ICutsceneStepExecutor
    {
        public Tween Execute(CutsceneStep step, CutsceneContext context)
        {
            // BGM 전환 요청
            if (BGMController.Instance != null)
            {
                // 컷신 우선순위로 BGM 전환
                BGMController.Instance.AddState(BGMController.BGMPriority.Cutscene, step.bgmEventKey);
                
            }
            else
            {
                Debug.LogWarning("[BGMStepExecutor] BGMController가 없어 BGM을 전환할 수 없습니다.");
            }
            
            // Wait Tween 생성 (bgmWaitTime만큼 대기)
            float totalWaitTime = step.bgmFadeTime + step.bgmWaitTime;
            
            if (totalWaitTime <= 0)
            {
                // 즉시 완료 (대기 시간 없음)
                return DOVirtual.DelayedCall(0.01f, () => { }, false)
                    .SetUpdate(true);
            }
            
            // 대기 시간만큼 Tween 생성
            return DOVirtual.DelayedCall(totalWaitTime, () => 
            {
            }, false)
            .SetUpdate(true);
        }
        
        public void OnSkip(CutsceneContext context)
        {
            // BGM 전환은 이미 시작되었으므로 스킵 시 추가 작업 없음
        }
    }
}





