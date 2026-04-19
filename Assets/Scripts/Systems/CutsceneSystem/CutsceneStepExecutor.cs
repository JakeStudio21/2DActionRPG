using UnityEngine;
using DG.Tweening;
using CueSystem;
using System.Collections.Generic;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 Step Executor (확장 버전 - Factory 패턴)
    /// 각 Step 타입별 Executor를 관리하고 실행
    /// </summary>
    public class CutsceneStepExecutor
    {
        private Dictionary<CutsceneStepType, ICutsceneStepExecutor> _executors;
        
        public CutsceneStepExecutor()
        {
            InitializeExecutors();
        }
        
        private void InitializeExecutors()
        {
            _executors = new Dictionary<CutsceneStepType, ICutsceneStepExecutor>
            {
                { CutsceneStepType.Image, new ImageStepExecutor() },
                { CutsceneStepType.Dialogue, new DialogueStepExecutor() },
                { CutsceneStepType.Wait, new WaitStepExecutor() },
                { CutsceneStepType.SFX, new SFXOneShotStepExecutor() },
                { CutsceneStepType.BGM, new BGMStepExecutor() },
                { CutsceneStepType.Callback, new CallbackStepExecutor() }
            };
            
        }
        
        public Tween ExecuteStep(CutsceneStep step, CutsceneContext context)
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
            
            if (_executors.TryGetValue(step.stepType, out var executor))
            {
                return executor.Execute(step, context);
            }
            
            Debug.LogWarning($"[CutsceneStepExecutor] ⚠️ 알 수 없는 Step 타입: {step.stepType}");
            return null;
        }
        
        public void OnSkipAll(CutsceneContext context)
        {
            
            foreach (var executor in _executors.Values)
            {
                executor.OnSkip(context);
            }
        }
    }
    
    /// <summary>
    /// 컷신 실행 컨텍스트 (확장 버전)
    /// 모든 Executor가 공유하는 공통 Context
    /// </summary>
    public class CutsceneContext
    {
        public CutsceneImagePanel imagePanel;
        public DialoguePanel dialoguePanel;
        public System.Action<string> onStepCallback;
        
        // 확장 버전 - 스킵 상태 관리
        public bool isFirstClickOnDialogue = true;
        public Tween currentDialogueTween = null;
        
        // Step 단위 스킵을 위한 현재 Step Tween 추적
        public Tween currentStepTween = null;
        
        // Step 단위 스킵을 위한 Step 종료 시간 추적
        public float currentStepEndTime = 0f;
        
        public CutsceneContext(CutsceneImagePanel imagePanel, DialoguePanel dialoguePanel, System.Action<string> onStepCallback = null)
        {
            this.imagePanel = imagePanel;
            this.dialoguePanel = dialoguePanel;
            this.onStepCallback = onStepCallback;
        }
    }
}
