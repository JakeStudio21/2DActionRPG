using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 대화 패널 (화자 이름 + 대사 텍스트)
    /// DOTween을 사용한 타이핑 효과 및 페이드 애니메이션
    /// </summary>
    public class DialoguePanel : MonoBehaviour
    {
        [Header("=== 참조 ===")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("=== 설정 ===")]
        [Tooltip("페이드 인/아웃 시간 (초)")]
        [SerializeField] private float fadeDuration = 0.3f;
        
        [Tooltip("기본 타이핑 속도 (글자/초)")]
        [SerializeField] private float defaultTypingSpeed = 20f;
        
        [Header("=== 디버그 ===")]
        // 현재 상태
        private bool isTyping = false;
        private string currentFullText = "";
        private Coroutine typingCoroutine;
        private Tween currentFadeTween;
        private Tween currentTextTween;
        
        private void Awake()
        {
            // 컴포넌트 자동 참조
            if (dialoguePanel == null)
                dialoguePanel = gameObject;
            
            if (speakerNameText == null)
                speakerNameText = transform.Find("Name Text")?.GetComponent<TextMeshProUGUI>();
            
            if (dialogueText == null)
                dialogueText = transform.Find("Body Text")?.GetComponent<TextMeshProUGUI>();
            
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            // CanvasGroup이 없으면 추가
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // 초기 상태: 즉시 숨김 (애니메이션 없음)
            // 컷신 시작 시 빈 Dialogue 박스가 잠깐 보이는 버그 방지
            // GameObject는 활성 상태 유지 (GetComponentInChildren으로 찾을 수 있도록)
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            
            if (dialogueText != null)
                dialogueText.text = "";
            
            if (speakerNameText != null)
                speakerNameText.text = "";
        }
        
        private void OnDestroy()
        {
            // 트윈 및 코루틴 정리
            KillAllTweens();
            StopTyping();
        }
        
        /// <summary>
        /// 대사 표시
        /// </summary>
        public void ShowDialogue(string speakerName, string text, float typingSpeed = 0f)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning("[DialoguePanel] 대사 텍스트가 비어있습니다!");
                return;
            }
            
            // 기존 애니메이션 정리
            StopTyping();
            KillAllTweens();
            
            // 화자 이름 설정
            if (speakerNameText != null)
            {
                if (!string.IsNullOrEmpty(speakerName))
                {
                    speakerNameText.text = speakerName;
                    speakerNameText.gameObject.SetActive(true);
                }
                else
                {
                    speakerNameText.text = "";
                    speakerNameText.gameObject.SetActive(false);
                }
            }
            
            // 타이핑 속도 설정 (0이면 기본값 사용)
            if (typingSpeed <= 0)
                typingSpeed = defaultTypingSpeed;
            
            // CanvasGroup 활성화
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false; // 컷신은 클릭으로 스킵하므로 차단하지 않음
                canvasGroup.interactable = false; // Dialogue는 상호작용 안 함
            }
            
            // 페이드 인
            canvasGroup.alpha = 0f;
            currentFadeTween = canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true); // Time.timeScale 무시
            
            // 타이핑 효과 시작
            currentFullText = text;
            
            if (typingSpeed > 0)
            {
                StartTyping(text, typingSpeed);
            }
            else
            {
                // 즉시 표시
                if (dialogueText != null)
                    dialogueText.text = text;
            }
            
        }
        
        /// <summary>
        /// 대사 숨김
        /// </summary>
        public void HideDialogue()
        {
            StopTyping();
            KillAllTweens();
            
            // 페이드 아웃
            if (canvasGroup != null)
            {
                // 즉시 비활성화 (클릭 차단)
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                
                currentFadeTween = canvasGroup.DOFade(0f, fadeDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true) // Time.timeScale 무시
                    .OnComplete(() => {
                        // GameObject는 활성 상태 유지 (GetComponentInChildren으로 찾을 수 있도록)
                        if (dialogueText != null)
                            dialogueText.text = "";
                        if (speakerNameText != null)
                            speakerNameText.text = "";
                    });
            }
            else
            {
                // CanvasGroup이 없는 경우 (비정상 상황)
                if (dialogueText != null)
                    dialogueText.text = "";
                if (speakerNameText != null)
                    speakerNameText.text = "";
            }
            
        }
        
        /// <summary>
        /// 타이핑 효과 시작 (DOText 우선, Fallback은 코루틴)
        /// </summary>
        private void StartTyping(string text, float typingSpeed)
        {
            isTyping = true;
            
            if (dialogueText == null)
            {
                isTyping = false;
                return;
            }
            
            // 1순위: DOText() 사용
            try
            {
                dialogueText.text = "";
                float duration = text.Length / typingSpeed;
                
                currentTextTween = dialogueText.DOText(text, duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true) // Time.timeScale 무시
                    .OnComplete(() => {
                        isTyping = false;
                    });
                
            }
            catch
            {
                // 2순위: 코루틴 Fallback
                    Debug.LogWarning("[DialoguePanel] DOText 실패, 코루틴 Fallback 사용");
                
                dialogueText.text = "";
                typingCoroutine = StartCoroutine(TypingCoroutine(text, typingSpeed));
            }
        }
        
        /// <summary>
        /// 코루틴 기반 타이핑 효과 (Fallback)
        /// </summary>
        private IEnumerator TypingCoroutine(string text, float typingSpeed)
        {
            if (dialogueText == null)
            {
                isTyping = false;
                yield break;
            }
            
            dialogueText.text = "";
            float delay = 1f / typingSpeed;
            
            for (int i = 0; i <= text.Length; i++)
            {
                if (!isTyping) // 중단됨
                    yield break;
                
                dialogueText.text = text.Substring(0, i);
                yield return new WaitForSeconds(delay);
            }
            
            isTyping = false;
        }
        
        /// <summary>
        /// 현재 타이핑 즉시 완료 (1차 스킵)
        /// </summary>
        public void CompleteTyping()
        {
            if (!isTyping)
                return;
            
            StopTyping();
            
            // 전체 텍스트 즉시 표시
            if (dialogueText != null && !string.IsNullOrEmpty(currentFullText))
            {
                dialogueText.text = currentFullText;
            }
            
            isTyping = false;
            
        }
        
        /// <summary>
        /// 타이핑 중단
        /// </summary>
        private void StopTyping()
        {
            isTyping = false;
            
            // 코루틴 정리
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            
            // DOText 트윈 정리
            if (currentTextTween != null && currentTextTween.IsActive())
            {
                currentTextTween.Kill();
                currentTextTween = null;
            }
        }
        
        /// <summary>
        /// 트윈 정리
        /// </summary>
        private void KillAllTweens()
        {
            if (currentFadeTween != null && currentFadeTween.IsActive())
            {
                currentFadeTween.Kill();
                currentFadeTween = null;
            }
            
            if (currentTextTween != null && currentTextTween.IsActive())
            {
                currentTextTween.Kill();
                currentTextTween = null;
            }
        }
        
        /// <summary>
        /// 현재 타이핑 중인지 확인
        /// </summary>
        public bool IsTyping => isTyping;
        
        /// <summary>
        /// 대사 패널이 활성화되어 있는지 확인
        /// (GameObject가 아닌 CanvasGroup의 alpha 값으로 판단)
        /// </summary>
        public bool IsActive => canvasGroup != null && canvasGroup.alpha > 0f;
    }
}
