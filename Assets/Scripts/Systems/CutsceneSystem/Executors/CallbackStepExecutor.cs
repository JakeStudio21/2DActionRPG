using UnityEngine;
using DG.Tweening;

namespace CutsceneSystem
{
    /// <summary>
    /// Callback Step Executor
    /// 커스텀 콜백 실행
    /// </summary>
    public class CallbackStepExecutor : ICutsceneStepExecutor
    {
        public Tween Execute(CutsceneStep step, CutsceneContext context)
        {
            Tween tween = null;
            
            tween = DOVirtual.DelayedCall(0f, () => { })
                .OnStart(() => {
                    context.currentStepTween = tween;
                    
                    if (context.onStepCallback != null)
                    {
                        context.onStepCallback.Invoke(step.callbackMethodName);
                    }
                    else
                    {
                        Debug.LogWarning($"[CallbackStepExecutor] ⚠️ 콜백이 등록되지 않았습니다: {step.callbackMethodName}");
                    }
                })
                .SetUpdate(true);
            
            return tween;
        }
        
        public void OnSkip(CutsceneContext context)
        {
            // Callback은 정리 불필요
        }
    }
}
