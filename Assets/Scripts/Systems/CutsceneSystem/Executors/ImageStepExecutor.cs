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
            
            // ✅ 동적 플레이어 포트레이트 처리
            Sprite targetSprite = step.imageSprite;
            
            if (step.isPortrait && step.useDynamicPlayerPortrait)
            {
                targetSprite = GetPlayerPortrait();
                
                if (targetSprite == null)
                {
                    Debug.LogWarning("[ImageStepExecutor] 플레이어 포트레이트를 찾을 수 없습니다. 기본 이미지 사용.");
                    targetSprite = step.imageSprite; // fallback
                }
                else
                {
                }
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
                        targetSprite, // ✅ 동적으로 로드된 Sprite 사용
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
        
        /// <summary>
        /// ✅ 현재 플레이어 클래스의 포트레이트 가져오기
        /// </summary>
        private Sprite GetPlayerPortrait()
        {
            if (PlayerDataManager.Instance?.selectedPlayerData == null)
            {
                Debug.LogWarning("[ImageStepExecutor] PlayerDataManager를 찾을 수 없습니다.");
                return null;
            }
            
            var playerType = PlayerDataManager.Instance.selectedPlayerData.selectedPlayerType;
            
            // Resources/Portraits/Player/ 폴더에서 로드
            string portraitPath = playerType switch
            {
                PlayerType.Warrior => "Portraits/Player/Warrior_Portrait",
                PlayerType.Assasin => "Portraits/Player/Assasin_Portrait",
                PlayerType.Wizard => "Portraits/Player/Wizard_Portrait",
                _ => null
            };
            
            if (portraitPath != null)
            {
                Sprite portrait = Resources.Load<Sprite>(portraitPath);
                if (portrait != null)
                {
                    return portrait;
                }
                else
                {
                    Debug.LogWarning($"[ImageStepExecutor] 포트레이트를 찾을 수 없음: {portraitPath}");
                }
            }
            
            return null;
        }
        
        public void OnSkip(CutsceneContext context)
        {
            // 이미지는 특별한 정리 불필요
        }
    }
}
