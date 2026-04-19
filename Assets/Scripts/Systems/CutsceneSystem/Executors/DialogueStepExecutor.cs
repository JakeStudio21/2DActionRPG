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
            
            // ✅ 스피커 이름과 대사 텍스트 키워드 치환
            string processedSpeakerName = ReplaceDynamicKeywords(step.speakerName);
            string processedDialogueText = ReplaceDynamicKeywords(step.dialogueText);
            
            float typingDuration = CalculateTypingDuration(step);
            
            Tween tween = null;
            
            tween = DOVirtual.DelayedCall(typingDuration, () => { })
                .OnStart(() => {
                    context.currentStepTween = tween;
                    context.currentDialogueTween = tween;
                    
                    // ✅ 치환된 텍스트 사용
                    context.dialoguePanel.ShowDialogue(
                        processedSpeakerName,
                        processedDialogueText,
                        step.typingSpeed
                    );
                })
                .SetUpdate(true);
            
            return tween;
        }
        
        /// <summary>
        /// ✅ 동적 키워드를 실제 플레이어 정보로 치환
        /// </summary>
        private string ReplaceDynamicKeywords(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // {PLAYER_NAME} → 플레이어 이름 (예: "용사", "모험가")
            if (text.Contains("{PLAYER_NAME}"))
            {
                string playerName = GetPlayerName();
                text = text.Replace("{PLAYER_NAME}", playerName);
            }
            
            // {PLAYER_CLASS} → 클래스 이름 (예: "전사", "암살자")
            if (text.Contains("{PLAYER_CLASS}"))
            {
                string className = GetPlayerClassName();
                text = text.Replace("{PLAYER_CLASS}", className);
            }
            
            // {PLAYER} → 간편 버전 (클래스 이름)
            if (text.Contains("{PLAYER}"))
            {
                string className = GetPlayerClassName();
                text = text.Replace("{PLAYER}", className);
            }
            
            return text;
        }
        
        /// <summary>
        /// 현재 플레이어 이름 가져오기
        /// </summary>
        private string GetPlayerName()
        {
            if (PlayerDataManager.Instance?.selectedPlayerData == null)
            {
                Debug.LogWarning("[DialogueStepExecutor] PlayerDataManager를 찾을 수 없습니다. 기본값 사용.");
                return "플레이어";
            }
            
            string playerName = PlayerDataManager.Instance.selectedPlayerData.playerName;
            
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[DialogueStepExecutor] 플레이어 이름이 비어있습니다. 기본값 사용.");
                return "플레이어";
            }
            
            return playerName;
        }
        
        /// <summary>
        /// 현재 플레이어 클래스 이름 가져오기 (한글)
        /// </summary>
        private string GetPlayerClassName()
        {
            if (PlayerDataManager.Instance?.selectedPlayerData == null)
            {
                Debug.LogWarning("[DialogueStepExecutor] PlayerDataManager를 찾을 수 없습니다. 기본값 사용.");
                return "모험가";
            }
            
            var playerType = PlayerDataManager.Instance.selectedPlayerData.selectedPlayerType;
            
            string className = playerType switch
            {
                PlayerType.Warrior => "전사",
                PlayerType.Assasin => "암살자",
                PlayerType.Wizard => "마법사",
                _ => "모험가"
            };
            
            return className;
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
                context.dialoguePanel.CompleteTyping();
            }
        }
    }
}
