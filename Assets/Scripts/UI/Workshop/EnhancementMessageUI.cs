using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Systems;

namespace UI.Workshop
{
    /// <summary>
    /// 강화 결과 메세지 UI
    /// 
    /// 위치: EnhancementSubPanel > RightSection > 강화메세지패널
    /// 
    /// 표시 흐름:
    ///   1. EnhancementUI.ExecuteEnhancement() 결과 수신
    ///   2. 결과 타입에 맞는 텍스트/색상 세팅
    ///   3. DOTween Sequence로 등장 → 강조 → 퇴장 연출
    ///   4. 연출 종료 후 패널 비활성화
    /// </summary>
    public class EnhancementMessageUI : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────
        [Header("UI 참조")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private Image bgImage;
        [SerializeField] private TMP_Text mainMessageText;
        [SerializeField] private TMP_Text subMessageText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("VFX 프리팹")]
        [SerializeField] private GameObject successVFXPrefab;
        [SerializeField] private GameObject failVFXPrefab;
        [SerializeField] private Transform vfxAnchor;

        [Header("배경 색상")]
        [SerializeField] private Color successBgColor   = new Color(0.85f, 0.65f, 0.05f, 0.85f);
        [SerializeField] private Color failBgColor      = new Color(0.25f, 0.25f, 0.30f, 0.85f);
        [SerializeField] private Color downgradeBgColor = new Color(0.75f, 0.35f, 0.05f, 0.85f);
        [SerializeField] private Color destroyBgColor   = new Color(0.70f, 0.05f, 0.05f, 0.85f);

        [Header("텍스트 색상")]
        [SerializeField] private Color successTextColor   = new Color(1f, 0.92f, 0.30f, 1f);
        [SerializeField] private Color failTextColor      = new Color(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color downgradeTextColor = new Color(1f, 0.55f, 0.10f, 1f);
        [SerializeField] private Color destroyTextColor   = new Color(1f, 0.25f, 0.25f, 1f);

        [Header("타이밍 설정")]
        [SerializeField] private float displayDuration  = 2.0f;
        [SerializeField] private float fadeOutDuration  = 0.4f;

        // ── 런타임 ────────────────────────────────────────────────
        private Sequence currentSequence;
        private Vector2   panelOriginalPos;
        private RectTransform panelRect;

        // ════════════════════════════════════════════════════════════
        //  Unity Lifecycle
        // ════════════════════════════════════════════════════════════

        private void Awake()
        {
            panelRect = messagePanel != null
                ? messagePanel.GetComponent<RectTransform>()
                : GetComponent<RectTransform>();

            panelOriginalPos = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;

            if (canvasGroup == null && messagePanel != null)
                canvasGroup = messagePanel.GetComponent<CanvasGroup>();

            // SetActive(false) 대신 alpha=0으로 숨김
            // SetActive(false) 상태에서는 첫 DOTween 호출이 정상 동작하지 않음
            HideImmediate();
        }

        private void OnDisable()
        {
            KillCurrentSequence();
        }

        // ════════════════════════════════════════════════════════════
        //  Public API
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// 강화 성공 연출 재생
        /// </summary>
        /// <param name="fromLevel">강화 전 레벨</param>
        /// <param name="toLevel">강화 후 레벨</param>
        public void ShowSuccess(int fromLevel, int toLevel)
        {
            SetupPanel(
                mainText:  "강화 성공!",
                subText:   $"+{fromLevel} → +{toLevel} 달성!",
                bgColor:   successBgColor,
                textColor: successTextColor
            );

            SpawnVFX(successVFXPrefab);
            PlaySuccessSequence();
        }

        /// <summary>
        /// 강화 실패(유지) 연출 재생
        /// </summary>
        public void ShowFailMaintain(int level)
        {
            SetupPanel(
                mainText:  "강화 실패",
                subText:   $"+{level} 강화 수치 유지",
                bgColor:   failBgColor,
                textColor: failTextColor
            );

            SpawnVFX(failVFXPrefab);
            PlayFailSequence(shakeStrength: 8f);
        }

        /// <summary>
        /// 강화 실패(하락) 연출 재생
        /// </summary>
        public void ShowFailDowngrade(int fromLevel, int toLevel)
        {
            SetupPanel(
                mainText:  "강화 실패",
                subText:   $"+{fromLevel} → +{toLevel} 강화 수치 하락!",
                bgColor:   downgradeBgColor,
                textColor: downgradeTextColor
            );

            SpawnVFX(failVFXPrefab);
            PlayFailSequence(shakeStrength: 12f);
        }

        /// <summary>
        /// 아이템 파괴 연출 재생
        /// </summary>
        public void ShowDestroy(int fromLevel)
        {
            SetupPanel(
                mainText:  "아이템 파괴!",
                subText:   $"+{fromLevel} 아이템이 소멸했습니다",
                bgColor:   destroyBgColor,
                textColor: destroyTextColor
            );

            SpawnVFX(failVFXPrefab);
            PlayDestroySequence();
        }

        /// <summary>
        /// 패널 즉시 숨김 (외부 호출용)
        /// </summary>
        public void Hide()
        {
            KillCurrentSequence();
            HideImmediate();
        }

        /// <summary>
        /// 패널 즉시 숨김 내부 처리
        /// SetActive 대신 CanvasGroup으로 숨겨서 DOTween 첫 호출 문제 방지
        /// </summary>
        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable   = false;
            }

            if (panelRect != null)
            {
                panelRect.localScale       = Vector3.one;
                panelRect.anchoredPosition = panelOriginalPos;
            }
        }

        // ════════════════════════════════════════════════════════════
        //  내부 셋업
        // ════════════════════════════════════════════════════════════

        private void SetupPanel(string mainText, string subText, Color bgColor, Color textColor)
        {
            KillCurrentSequence();

            if (messagePanel == null) return;

            // 텍스트
            if (mainMessageText != null)
            {
                mainMessageText.text  = mainText;
                mainMessageText.color = textColor;
            }

            if (subMessageText != null)
            {
                subMessageText.text  = subText;
                subMessageText.color = new Color(textColor.r, textColor.g, textColor.b, 0.85f);
            }

            // 배경 색상
            if (bgImage != null)
                bgImage.color = bgColor;

            // 초기 상태 리셋
            if (panelRect != null)
            {
                panelRect.localScale       = Vector3.zero;
                panelRect.anchoredPosition = panelOriginalPos;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha          = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable   = true;
            }

            // 서브텍스트 초기 오프셋 (아래서 올라오는 효과용)
            if (subMessageText != null)
            {
                var subRect = subMessageText.GetComponent<RectTransform>();
                if (subRect != null)
                    subRect.anchoredPosition += Vector2.down * 10f;
            }

            // SetActive 사용하지 않음 - CanvasGroup alpha로 표시 제어
        }

        private void SpawnVFX(GameObject prefab)
        {
            if (prefab == null) return;

            // Screen Space - Overlay 환경에서 파티클 Canvas가 올바르게 동작하려면
            // 씬 루트 레벨(parent=null)로 스폰해야 자체 Canvas Sort Order가 적용됨
            Vector3 spawnPos = vfxAnchor != null ? vfxAnchor.position : transform.position;
            Instantiate(prefab, spawnPos, Quaternion.identity);
        }

        // ════════════════════════════════════════════════════════════
        //  DOTween Sequences
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// 강화 성공 시퀀스
        ///   [0.00~0.30] Scale 0 → 1.1  (OutBack)  + Alpha 0 → 1
        ///   [0.30~0.55] Scale 1.1 → 1  (InOutSine) + Main Text 금색 펄스
        ///   [0.40~0.55] Sub Text Y 슬라이드업  (OutCubic)
        ///   [유지 후]   Y +20 올라가며 Alpha 0 (InQuad)
        /// </summary>
        private void PlaySuccessSequence()
        {
            if (panelRect == null || canvasGroup == null) return;

            var subRect = subMessageText != null
                ? subMessageText.GetComponent<RectTransform>()
                : null;

            currentSequence = DOTween.Sequence().SetUpdate(true);

            // Stage 1: 등장
            currentSequence.Join(panelRect.DOScale(1.1f, 0.30f).SetEase(Ease.OutBack));
            currentSequence.Join(canvasGroup.DOFade(1f, 0.25f).SetEase(Ease.Linear));

            // Stage 2: 강조
            currentSequence.Append(panelRect.DOScale(1.0f, 0.25f).SetEase(Ease.InOutSine));

            if (mainMessageText != null)
            {
                currentSequence.Join(
                    mainMessageText.DOColor(Color.white, 0.15f)
                        .SetEase(Ease.Flash, 2, 0f)
                        .SetLoops(1)
                );
            }

            // 서브텍스트 슬라이드업 (0.1초 딜레이)
            if (subRect != null)
            {
                Vector2 targetPos = subRect.anchoredPosition + Vector2.up * 10f;
                currentSequence.Insert(0.40f,
                    subRect.DOAnchorPos(targetPos, 0.20f).SetEase(Ease.OutCubic)
                );
                currentSequence.Insert(0.40f,
                    subMessageText.DOFade(1f, 0.20f).SetEase(Ease.OutCubic)
                );
            }

            // Stage 3: 유지 후 퇴장
            currentSequence.AppendInterval(displayDuration);
            currentSequence.Append(
                canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad)
            );
            currentSequence.Join(
                panelRect.DOAnchorPos(panelOriginalPos + Vector2.up * 20f, fadeOutDuration)
                    .SetEase(Ease.InQuad)
            );
            currentSequence.OnComplete(OnSequenceComplete);
        }

        /// <summary>
        /// 강화 실패 시퀀스 (유지 / 하락 공통, shakeStrength로 강도 조절)
        ///   [0.00~0.20] Scale 0.8 → 1  (OutCubic) + Alpha 0 → 1
        ///   [0.20~0.50] DOShakePosition + Main Text 색상 펄스 x2
        ///   [유지 후]   Scale → 0.9 + Alpha 0  (InQuad)
        /// </summary>
        private void PlayFailSequence(float shakeStrength)
        {
            if (panelRect == null || canvasGroup == null) return;

            // 등장 시 Scale 0.8에서 시작
            panelRect.localScale = Vector3.one * 0.8f;

            currentSequence = DOTween.Sequence().SetUpdate(true);

            // Stage 1: 등장
            currentSequence.Join(panelRect.DOScale(1.0f, 0.20f).SetEase(Ease.OutCubic));
            currentSequence.Join(canvasGroup.DOFade(1f, 0.20f).SetEase(Ease.Linear));

            // Stage 2: 진동 강조
            currentSequence.Append(
                panelRect.DOShakeAnchorPos(0.30f, strength: shakeStrength, vibrato: 20, randomness: 90f)
                    .SetUpdate(true)
            );

            if (mainMessageText != null)
            {
                Color originalColor = mainMessageText.color;
                currentSequence.Join(
                    mainMessageText.DOColor(Color.white, 0.15f)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetEase(Ease.Flash)
                );
            }

            // Stage 3: 유지 후 퇴장
            currentSequence.AppendInterval(displayDuration);
            currentSequence.Append(
                canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad)
            );
            currentSequence.Join(
                panelRect.DOScale(0.9f, fadeOutDuration).SetEase(Ease.InQuad)
            );
            currentSequence.OnComplete(OnSequenceComplete);
        }

        /// <summary>
        /// 아이템 파괴 시퀀스 (특수 강렬 연출)
        ///   [0.00~0.25] Scale 1.3 → 1  (OutBounce) + Alpha 0 → 1
        ///   [0.25~0.70] DOShakePosition 강함 + 배경 빨간 점멸 + MainText 흔들림
        ///   [유지 후]   Scale → 1.2 + Alpha 0  (터지듯 확대 후 소멸)
        /// </summary>
        private void PlayDestroySequence()
        {
            if (panelRect == null || canvasGroup == null) return;

            // 등장 시 Scale 1.3에서 시작
            panelRect.localScale = Vector3.one * 1.3f;

            Color originalBgColor = bgImage != null ? bgImage.color : destroyBgColor;
            Color flashColor      = new Color(1f, 0.1f, 0.1f, originalBgColor.a);

            currentSequence = DOTween.Sequence().SetUpdate(true);

            // Stage 1: 강렬 등장
            currentSequence.Join(panelRect.DOScale(1.0f, 0.25f).SetEase(Ease.OutBounce));
            currentSequence.Join(canvasGroup.DOFade(1f, 0.20f).SetEase(Ease.Linear));

            // Stage 2: 강한 진동 + 배경 빨간 점멸
            currentSequence.Append(
                panelRect.DOShakeAnchorPos(0.45f, strength: 15f, vibrato: 25, randomness: 90f)
                    .SetUpdate(true)
            );

            if (bgImage != null)
            {
                currentSequence.Join(
                    bgImage.DOColor(flashColor, 0.10f)
                        .SetLoops(3, LoopType.Yoyo)
                        .SetEase(Ease.Flash)
                );
            }

            if (mainMessageText != null)
            {
                currentSequence.Join(
                    mainMessageText.transform.DOShakeScale(0.30f, strength: 0.2f, vibrato: 10)
                        .SetUpdate(true)
                );
            }

            // Stage 3: 유지 후 퇴장 (확대되며 소멸)
            currentSequence.AppendInterval(displayDuration);
            currentSequence.Append(
                canvasGroup.DOFade(0f, fadeOutDuration + 0.2f).SetEase(Ease.InQuad)
            );
            currentSequence.Join(
                panelRect.DOScale(1.2f, fadeOutDuration + 0.2f).SetEase(Ease.InQuad)
            );
            currentSequence.OnComplete(OnSequenceComplete);
        }

        // ════════════════════════════════════════════════════════════
        //  헬퍼
        // ════════════════════════════════════════════════════════════

        private void OnSequenceComplete()
        {
            HideImmediate();
        }

        private void KillCurrentSequence()
        {
            if (currentSequence != null && currentSequence.IsActive())
            {
                currentSequence.Kill(complete: false);
                currentSequence = null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("테스트: 성공 연출")]
        private void TestSuccess() => ShowSuccess(3, 4);

        [ContextMenu("테스트: 실패(유지) 연출")]
        private void TestFailMaintain() => ShowFailMaintain(5);

        [ContextMenu("테스트: 실패(하락) 연출")]
        private void TestFailDowngrade() => ShowFailDowngrade(5, 4);

        [ContextMenu("테스트: 파괴 연출")]
        private void TestDestroy() => ShowDestroy(8);
#endif
    }
}
