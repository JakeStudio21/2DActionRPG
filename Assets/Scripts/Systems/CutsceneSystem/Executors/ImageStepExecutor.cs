using UnityEngine;
using DG.Tweening;

namespace CutsceneSystem
{
    /// <summary>
    /// Image Step Executor
    /// 이미지 페이드 인/아웃 처리
    /// </summary>
    public class ImageStepExecutor : ICutsceneStepExecutor
    {
        public Tween Execute(CutsceneStep step, CutsceneContext context)
        {
            if (context.imagePanel == null)
            {
                Debug.LogError("[ImageStepExecutor] ImagePanel이 없습니다!");
                return null;
            }
            
            // DelayedCall이 OnStart 전에 생성되어야 함
            Tween tween = null;
            
            tween = DOVirtual.DelayedCall(step.imageDuration, () => { })
                .OnStart(() => {
                    context.currentStepTween = tween;
                    
                    if (step.isFade && context.dialoguePanel != null && context.dialoguePanel.IsActive)
                    {
                        context.dialoguePanel.HideDialogue();
                    }
                    
                    context.imagePanel.ShowImage(
                        step.imageSprite,
                        step.isPortrait,
                        step.fadeIn,
                        step.imagePosition,
                        step.imageScale,
                        step.imageDuration,
                        step.isFade
                    );
                })
                .SetUpdate(true);
            
            return tween;
        }
        
        public void OnSkip(CutsceneContext context)
        {
            // 이미지는 특별한 정리 불필요
        }
    }
}
