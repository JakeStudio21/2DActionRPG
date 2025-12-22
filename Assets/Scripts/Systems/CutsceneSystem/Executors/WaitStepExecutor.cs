using UnityEngine;
using DG.Tweening;

namespace CutsceneSystem
{
    /// <summary>
    /// Wait Step Executor
    /// 지정된 시간만큼 대기
    /// </summary>
    public class WaitStepExecutor : ICutsceneStepExecutor
    {
        public Tween Execute(CutsceneStep step, CutsceneContext context)
        {
            Tween tween = null;
            
            tween = DOVirtual.DelayedCall(step.duration, () => { })
                .OnStart(() => {
                    context.currentStepTween = tween;
                })
                .SetUpdate(true);
            
            return tween;
        }
        
        public void OnSkip(CutsceneContext context)
        {
            // Wait는 정리 불필요
        }
    }
}
