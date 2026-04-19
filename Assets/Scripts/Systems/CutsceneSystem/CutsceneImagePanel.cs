using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 이미지 패널 (배경/초상 이미지 표시)
    /// DOTween을 사용한 페이드/스케일 애니메이션
    /// </summary>
    public class CutsceneImagePanel : MonoBehaviour
    {
        [Header("=== 참조 ===")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image fadeImage;  // Fade 전용 레이어 (최상위)
        [SerializeField] private CanvasGroup backgroundCanvasGroup;
        [SerializeField] private CanvasGroup portraitCanvasGroup;
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        
        [Header("=== 설정 ===")]
        [Tooltip("페이드 인/아웃 시간 (초)")]
        [SerializeField] private float fadeDuration = 0.5f;
        
        [Tooltip("스케일 애니메이션 시간 (초)")]
        [SerializeField] private float scaleDuration = 0.3f;
        
        [Header("=== 디버그 ===")]
        // 현재 활성 트윈 추적
        private Tween currentBackgroundTween;
        private Tween currentPortraitTween;
        private Tween currentFadeTween;
        
        private void Awake()
        {
            // 컴포넌트 자동 참조
            if (backgroundImage == null)
                backgroundImage = transform.Find("Background Image")?.GetComponent<Image>();
            
            if (portraitImage == null)
                portraitImage = transform.Find("Portrait Image")?.GetComponent<Image>();
            
            if (fadeImage == null)
                fadeImage = transform.Find("Fade Image")?.GetComponent<Image>();
            
            if (backgroundCanvasGroup == null)
                backgroundCanvasGroup = backgroundImage?.GetComponent<CanvasGroup>();
            
            if (portraitCanvasGroup == null)
                portraitCanvasGroup = portraitImage?.GetComponent<CanvasGroup>();
            
            if (fadeCanvasGroup == null)
                fadeCanvasGroup = fadeImage?.GetComponent<CanvasGroup>();
            
            // CanvasGroup이 없으면 추가
            if (backgroundImage != null && backgroundCanvasGroup == null)
            {
                backgroundCanvasGroup = backgroundImage.gameObject.AddComponent<CanvasGroup>();
            }
            
            if (portraitImage != null && portraitCanvasGroup == null)
            {
                portraitCanvasGroup = portraitImage.gameObject.AddComponent<CanvasGroup>();
            }
            
            if (fadeImage != null && fadeCanvasGroup == null)
            {
                fadeCanvasGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
            }
            
            // 초기 상태: 숨김
            HideAllImages();
        }
        
        private void OnDestroy()
        {
            // 트윈 정리
            KillAllTweens();
        }
        
        /// <summary>
        /// 이미지 표시 (배경, 초상, 또는 Fade)
        /// </summary>
        /// <param name="isFade">Fade 레이어 사용 여부 (배경/초상 위에 표시)</param>
        public void ShowImage(Sprite sprite, bool isPortrait, bool fadeIn = true, Vector2 position = default, float scale = 1f, float duration = 2f, bool isFade = false)
        {
            if (sprite == null)
            {
                Debug.LogWarning("[CutsceneImagePanel] 스프라이트가 null입니다!");
                return;
            }
            
            // Fade 레이어 우선 선택
            Image targetImage;
            CanvasGroup targetCanvasGroup;
            string layerName = isFade ? "Fade" : (isPortrait ? "초상" : "배경");
            
            if (isFade)
            {
                targetImage = fadeImage;
                targetCanvasGroup = fadeCanvasGroup;
            }
            else
            {
                targetImage = isPortrait ? portraitImage : backgroundImage;
                targetCanvasGroup = isPortrait ? portraitCanvasGroup : backgroundCanvasGroup;
            }
            
            if (targetImage == null || targetCanvasGroup == null)
            {
                Debug.LogError($"[CutsceneImagePanel] {layerName} 이미지 컴포넌트를 찾을 수 없습니다!");
                return;
            }
            
            // 기존 트윈 정리
            if (isFade)
                KillFadeTween();
            else if (isPortrait)
                KillPortraitTween();
            else
                KillBackgroundTween();
            
            // 이미지 설정
            targetImage.sprite = sprite;
            targetImage.gameObject.SetActive(true);
            
            // 위치 설정
            if (position != default)
            {
                RectTransform rectTransform = targetImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = position;
                }
            }
            
            // 스케일 설정
            targetImage.transform.localScale = Vector3.one * scale;
            
            // 페이드 인 애니메이션
            if (fadeIn)
            {
                targetCanvasGroup.alpha = 0f;
                Tween fadeTween = targetCanvasGroup.DOFade(1f, fadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true); // Time.timeScale 무시
                
                if (isFade)
                    currentFadeTween = fadeTween;
                else if (isPortrait)
                    currentPortraitTween = fadeTween;
                else
                    currentBackgroundTween = fadeTween;
            }
            else
            {
                targetCanvasGroup.alpha = 1f;
            }
            
            // 자동 숨김 제거: 명시적으로 숨기고 싶을 때는 별도 Image Step 사용
            // (duration 파라미터는 Sequence 타이밍 제어용으로만 사용됨)
            
        }
        
        /// <summary>
        /// 이미지 숨김
        /// </summary>
        public void HideImage(bool isPortrait, bool fadeOut = true, bool isFade = false)
        {
            Image targetImage;
            CanvasGroup targetCanvasGroup;
            
            if (isFade)
            {
                targetImage = fadeImage;
                targetCanvasGroup = fadeCanvasGroup;
            }
            else
            {
                targetImage = isPortrait ? portraitImage : backgroundImage;
                targetCanvasGroup = isPortrait ? portraitCanvasGroup : backgroundCanvasGroup;
            }
            
            if (targetImage == null || targetCanvasGroup == null)
                return;
            
            // 기존 트윈 정리
            if (isFade)
                KillFadeTween();
            else if (isPortrait)
                KillPortraitTween();
            else
                KillBackgroundTween();
            
            if (fadeOut)
            {
                // 페이드 아웃 애니메이션
                Tween fadeOutTween = targetCanvasGroup.DOFade(0f, fadeDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true) // Time.timeScale 무시
                    .OnComplete(() => {
                        targetImage.gameObject.SetActive(false);
                        targetImage.sprite = null;
                    });
                
                if (isFade)
                    currentFadeTween = fadeOutTween;
                else if (isPortrait)
                    currentPortraitTween = fadeOutTween;
                else
                    currentBackgroundTween = fadeOutTween;
            }
            else
            {
                // 즉시 숨김
                targetImage.gameObject.SetActive(false);
                targetImage.sprite = null;
                targetCanvasGroup.alpha = 0f;
            }
            
            string layerName = isFade ? "Fade" : (isPortrait ? "초상" : "배경");
        }
        
        /// <summary>
        /// 모든 이미지 숨김
        /// </summary>
        public void HideAllImages()
        {
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
                if (backgroundCanvasGroup != null)
                    backgroundCanvasGroup.alpha = 0f;
            }
            
            if (portraitImage != null)
            {
                portraitImage.gameObject.SetActive(false);
                if (portraitCanvasGroup != null)
                    portraitCanvasGroup.alpha = 0f;
            }
            
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(false);
                if (fadeCanvasGroup != null)
                    fadeCanvasGroup.alpha = 0f;
            }
            
            KillAllTweens();
        }
        
        /// <summary>
        /// 배경 트윈 정리
        /// </summary>
        private void KillBackgroundTween()
        {
            if (currentBackgroundTween != null && currentBackgroundTween.IsActive())
            {
                currentBackgroundTween.Kill();
                currentBackgroundTween = null;
            }
        }
        
        /// <summary>
        /// 초상 트윈 정리
        /// </summary>
        private void KillPortraitTween()
        {
            if (currentPortraitTween != null && currentPortraitTween.IsActive())
            {
                currentPortraitTween.Kill();
                currentPortraitTween = null;
            }
        }
        
        /// <summary>
        /// Fade 트윈 정리
        /// </summary>
        private void KillFadeTween()
        {
            if (currentFadeTween != null && currentFadeTween.IsActive())
            {
                currentFadeTween.Kill();
                currentFadeTween = null;
            }
        }
        
        /// <summary>
        /// 모든 트윈 정리
        /// </summary>
        private void KillAllTweens()
        {
            KillBackgroundTween();
            KillPortraitTween();
            KillFadeTween();
        }
        
        /// <summary>
        /// 현재 표시 중인 이미지가 있는지 확인
        /// </summary>
        public bool HasActiveImage(bool isPortrait, bool isFade = false)
        {
            Image targetImage;
            CanvasGroup targetCanvasGroup;
            
            if (isFade)
            {
                targetImage = fadeImage;
                targetCanvasGroup = fadeCanvasGroup;
            }
            else
            {
                targetImage = isPortrait ? portraitImage : backgroundImage;
                targetCanvasGroup = isPortrait ? portraitCanvasGroup : backgroundCanvasGroup;
            }
            
            return targetImage != null && 
                   targetImage.gameObject.activeSelf && 
                   targetCanvasGroup != null && 
                   targetCanvasGroup.alpha > 0f;
        }
    }
}
