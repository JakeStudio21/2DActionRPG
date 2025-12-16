using UnityEngine;
using DG.Tweening;
using CueSystem;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 Step Executor (MVP 버전 - 단일 클래스)
    /// 각 Step 타입별 DOTween 트윈 생성
    /// </summary>
    public static class CutsceneStepExecutor
    {
        /// <summary>
        /// Step을 DOTween 트윈으로 변환
        /// </summary>
        public static Tween ExecuteStep(CutsceneStep step, CutsceneContext context)
        {
            if (step == null)
            {
                Debug.LogError("[CutsceneStepExecutor] Step이 null입니다!");
                return null;
            }
            
            if (!step.IsValid())
            {
                Debug.LogWarning($"[CutsceneStepExecutor] 유효하지 않은 Step: {step.stepType}");
                return null;
            }
            
            Debug.Log($"[CutsceneStepExecutor] Step 실행: {step.stepType}");
            
            switch (step.stepType)
            {
                case CutsceneStepType.Image:
                    return ExecuteImageStep(step, context);
                
                case CutsceneStepType.Dialogue:
                    return ExecuteDialogueStep(step, context);
                
                case CutsceneStepType.Wait:
                    return ExecuteWaitStep(step, context);
                
                case CutsceneStepType.SFX:
                    return ExecuteSFXStep(step, context);
                
                case CutsceneStepType.Callback:
                    return ExecuteCallbackStep(step, context);
                
                default:
                    Debug.LogWarning($"[CutsceneStepExecutor] 알 수 없는 Step 타입: {step.stepType}");
                    return null;
            }
        }
        
        /// <summary>
        /// Image Step 실행
        /// </summary>
        private static Tween ExecuteImageStep(CutsceneStep step, CutsceneContext context)
        {
            if (context.imagePanel == null)
            {
                Debug.LogError("[CutsceneStepExecutor] ImagePanel이 없습니다!");
                return null;
            }
            
            // 이미지 지속 시간만큼 대기 트윈 반환
            // OnStart에서 이미지 표시 (Tween이 실제로 시작될 때 실행됨)
            return DOVirtual.DelayedCall(step.imageDuration, () => { })
                .OnStart(() => {
                    string layerType = step.isFade ? "Fade" : (step.isPortrait ? "초상" : "배경");
                    Debug.Log($"[CutsceneStepExecutor] Image Step 시작 - sprite: {step.imageSprite?.name}, layer: {layerType}");
                    
                    // Fade 레이어 사용 시 Dialogue Panel 자동 숨김 (Fade 전 대화 정리)
                    if (step.isFade && context.dialoguePanel != null && context.dialoguePanel.IsActive)
                    {
                        Debug.Log("[CutsceneStepExecutor] Fade Step - Dialogue Panel 자동 숨김");
                        context.dialoguePanel.HideDialogue();
                    }
                    
                    context.imagePanel.ShowImage(
                        step.imageSprite,
                        step.isPortrait,
                        step.fadeIn,
                        step.imagePosition,
                        step.imageScale,
                        step.imageDuration,
                        step.isFade  // Fade 레이어 사용 여부 전달
                    );
                })
                .SetUpdate(true)
                .Pause();   // Sequence가 제어할 수 있도록 자동 재생 방지
        }
        
        /// <summary>
        /// Dialogue Step 실행
        /// </summary>
        private static Tween ExecuteDialogueStep(CutsceneStep step, CutsceneContext context)
        {
            if (context.dialoguePanel == null)
            {
                Debug.LogError("[CutsceneStepExecutor] DialoguePanel이 없습니다!");
                return null;
            }
            
            // 타이핑 시간 계산
            float typingDuration = 0f;
            if (step.typingSpeed > 0)
            {
                typingDuration = step.dialogueText.Length / step.typingSpeed;
            }
            else
            {
                // 즉시 표시인 경우 기본 대기 시간
                typingDuration = 0.5f;
            }
            
            // 타이핑 완료까지 대기 트윈 반환
            // OnStart에서 대사 표시 (Tween이 실제로 시작될 때 실행됨)
            return DOVirtual.DelayedCall(typingDuration, () => { })
                .OnStart(() => {
                    Debug.Log($"[CutsceneStepExecutor] Dialogue Step 시작 - text: {step.dialogueText?.Substring(0, System.Math.Min(20, step.dialogueText.Length))}");
                    context.dialoguePanel.ShowDialogue(
                        step.speakerName,
                        step.dialogueText,
                        step.typingSpeed
                    );
                })
                .SetUpdate(true)
                .Pause();   // Sequence가 제어할 수 있도록 자동 재생 방지
        }
        
        /// <summary>
        /// Wait Step 실행
        /// </summary>
        private static Tween ExecuteWaitStep(CutsceneStep step, CutsceneContext context)
        {
            return DOVirtual.DelayedCall(step.duration, () => { })
                .SetUpdate(true)
                .Pause();   // Sequence가 제어할 수 있도록 자동 재생 방지
        }
        
        /// <summary>
        /// SFX Step 실행
        /// </summary>
        private static Tween ExecuteSFXStep(CutsceneStep step, CutsceneContext context)
        {
            // SFX 재생 후 지정된 시간만큼 대기
            // OnStart에서 CueSystem 호출 (Tween이 실제로 시작될 때 실행됨)
            float delayTime = Mathf.Max(0.1f, step.duration); // 최소 0.1초 보장
            
            return DOVirtual.DelayedCall(delayTime, () => { })
                .OnStart(() => {
                    string domain = string.IsNullOrEmpty(step.sfxDomain) ? "Cutscene" : step.sfxDomain;
                    Debug.Log($"[CutsceneStepExecutor] SFX Step 시작 - key: {step.sfxEventKey}, domain: {domain}, delay: {delayTime}초");
                    CueEmitter.Emit(step.sfxEventKey, domain);
                })
                .SetUpdate(true)
                .Pause();   // Sequence가 제어할 수 있도록 자동 재생 방지
        }
        
        /// <summary>
        /// Callback Step 실행
        /// </summary>
        private static Tween ExecuteCallbackStep(CutsceneStep step, CutsceneContext context)
        {
            // 콜백 호출
            if (context.onStepCallback != null)
            {
                context.onStepCallback.Invoke(step.callbackMethodName);
            }
            else
            {
                Debug.LogWarning($"[CutsceneStepExecutor] 콜백이 등록되지 않았습니다: {step.callbackMethodName}");
            }
            
            // 즉시 완료되는 트윈 반환
            return DOVirtual.DelayedCall(0f, () => { })
                .SetUpdate(true)
                .Pause();   // Sequence가 제어할 수 있도록 자동 재생 방지
        }
    }
    
    /// <summary>
    /// 컷신 실행 컨텍스트
    /// </summary>
    public class CutsceneContext
    {
        public CutsceneImagePanel imagePanel;
        public DialoguePanel dialoguePanel;
        public System.Action<string> onStepCallback;
        
        public CutsceneContext(CutsceneImagePanel imagePanel, DialoguePanel dialoguePanel, System.Action<string> onStepCallback = null)
        {
            this.imagePanel = imagePanel;
            this.dialoguePanel = dialoguePanel;
            this.onStepCallback = onStepCallback;
        }
    }
}
