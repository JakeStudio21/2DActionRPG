using UnityEngine;
using DG.Tweening;

namespace CutsceneSystem
{
    /// <summary>
    /// Dialogue Step Executor
    /// 대사 표시 및 타이핑 효과
    /// </summary>
    public class DialogueStepExecutor : ICutsceneStepExecutor
    {
        public Tween Execute(CutsceneStep step, CutsceneContext context)
        {
            if (context.dialoguePanel == null)
            {
                Debug.LogError("[DialogueStepExecutor] DialoguePanel이 없습니다!");
                return null;
            }
            
            float typingDuration = CalculateTypingDuration(step);
            
            Tween tween = null;
            
            tween = DOVirtual.DelayedCall(typingDuration, () => { })
                .OnStart(() => {
                    context.currentStepTween = tween;
                    context.currentDialogueTween = tween;
                    
                    context.dialoguePanel.ShowDialogue(
                        step.speakerName,
                        step.dialogueText,
                        step.typingSpeed
                    );
                })
                .SetUpdate(true);
            
            return tween;
        }
        
        private float CalculateTypingDuration(CutsceneStep step)
        {
            if (step.typingSpeed > 0)
            {
                return step.dialogueText.Length / step.typingSpeed;
            }
            else
            {
                return 0.5f;
            }
        }
        
        public void OnSkip(CutsceneContext context)
        {
            if (context.dialoguePanel != null && context.dialoguePanel.IsTyping)
            {
                Debug.Log("[DialogueStepExecutor] ⏭️ 타이핑 즉시 완료");
                context.dialoguePanel.CompleteTyping();
            }
        }
    }
}
