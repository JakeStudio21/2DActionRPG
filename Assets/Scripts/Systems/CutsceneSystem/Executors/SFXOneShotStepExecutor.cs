using UnityEngine;
using DG.Tweening;
using CueSystem;

namespace CutsceneSystem
{
    /// <summary>
    /// SFX OneShot Step Executor
    /// 1회성 효과음 재생
    /// </summary>
    public class SFXOneShotStepExecutor : ICutsceneStepExecutor
    {
        public Tween Execute(CutsceneStep step, CutsceneContext context)
        {
            float delayTime = Mathf.Max(0.1f, step.duration);
            
            Tween tween = null;
            
            tween = DOVirtual.DelayedCall(delayTime, () => { })
                .OnStart(() => {
                    context.currentStepTween = tween;
                    
                    string domain = string.IsNullOrEmpty(step.sfxDomain) ? "Cutscene" : step.sfxDomain;
                    CueEmitter.Emit(step.sfxEventKey, domain);
                })
                .SetUpdate(true);
            
            return tween;
        }
        
        public void OnSkip(CutsceneContext context)
        {
            // OneShot은 정리 불필요
        }
    }
}
