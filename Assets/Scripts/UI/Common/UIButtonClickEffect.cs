using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using CueSystem;
using System.Collections;

/// <summary>
/// 🎯 UI 버튼 클릭 효과 시스템 (단순화 버전)
/// - DOTween 기반 Scale 효과
/// - Unity Button Transition (Color Tint) 사용 권장
/// - onClick 딜레이 시스템 (효과 재생 후 동작 실행)
/// - Layout Group 튐 방지 (scaleTarget 사용)
/// - DOTween 중첩 방지 (Kill/Reset 로직)
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonClickEffect : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Button Type")]
    [SerializeField] private UIButtonType buttonType = UIButtonType.Navigation;
    [Tooltip("Navigation, Action, Tab, Utility 모두 지원")]
    
    [Header("Scale Target (Layout 튐 방지)")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private bool autoCreateScaleTarget = true;
    [Tooltip("null이면 자식에서 자동 탐색. Layout Group 내부라면 반드시 설정하세요")]
    
    [Header("DOTween 설정 - Scale")]
    [SerializeField] private float scaleDuration = 0.1f;
    [SerializeField] private float scaleAmount = 0.95f;
    [SerializeField] private float utilityScaleAmount = 0.97f;  // Utility 전용
    [SerializeField] private float punchScale = 0.2f;  // Action 타입용
    
    [Header("Click Delay (효과 후 동작 실행)")]
    [SerializeField] private bool enableClickDelay = true;
    [SerializeField] private float effectDelayTime = 0.2f;
    [Tooltip("효과 재생 후 대기 시간 (초). 이 시간 후 실제 onClick 이벤트 실행")]
    
    [Header("Cue System")]
    [SerializeField] private bool useAutoCueKey = true;
    [SerializeField] private string customCueKey = "";
    [Tooltip("자동: ui.button.{타입명} / 수동: customCueKey 입력")]
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion
    
    #region Private Fields
    
    private Button button;
    
    // DOTween 중첩 방지용
    private Tween currentScaleTween;
    
    // 원본 상태 저장
    private Vector3 originalScale = Vector3.one;
    
    // onClick 이벤트 백업
    private Button.ButtonClickedEvent originalOnClick;
    private bool hasSetupClickDelay = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        button = GetComponent<Button>();
        
        // scaleTarget 자동 설정
        SetupScaleTarget();
    }
    
    /// <summary>
    /// Start에서 onClick 딜레이 시스템 설정
    /// - Awake()에서 하면 Runtime 이벤트(AddListener)가 아직 등록 안 됨
    /// - Start()에서도 같은 GameObject의 다른 컴포넌트 Start() 순서 보장 안 됨
    /// - 해결: 1프레임 대기 후 백업 (모든 Start() 완료 후)
    /// </summary>
    private void Start()
    {
        // 🆕 1프레임 대기 후 설정 (모든 컴포넌트의 Start() 완료 보장)
        StartCoroutine(SetupClickDelaySystemDelayed());
    }
    
    /// <summary>
    /// 🆕 1프레임 대기 후 onClick 딜레이 시스템 설정
    /// </summary>
    private IEnumerator SetupClickDelaySystemDelayed()
    {
        // 1프레임 대기 (모든 Start() 완료 대기)
        yield return null;
        
        // onClick 딜레이 시스템 설정
        if (enableClickDelay)
        {
            SetupClickDelaySystem();
        }
        else
        {
            // 딜레이 비활성화: 효과만 재생 (기존 onClick은 유지)
            button.onClick.AddListener(OnClickEffectOnly);
        }
        
        if (showDebugLogs)
        {
            int persistentCount = button.onClick.GetPersistentEventCount();
            Debug.Log($"🎯 [UIButtonClickEffect] {gameObject.name}: 설정 완료 (딜레이: {enableClickDelay}, Persistent 이벤트: {persistentCount}개)");
        }
    }
    
    private void OnDisable()
    {
        // DOTween 안전 정리
        KillAllTweens();
        
        // 상태 원복
        ResetToOriginalState();
    }
    
    private void OnDestroy()
    {
        KillAllTweens();
    }
    
    #endregion
    
    #region Setup
    
    /// <summary>
    /// scaleTarget 자동 설정
    /// </summary>
    private void SetupScaleTarget()
    {
        // 이미 설정되어 있으면 스킵
        if (scaleTarget != null)
        {
            originalScale = scaleTarget.localScale;
            
            if (showDebugLogs)
                Debug.Log($"✅ [UIButtonClickEffect] {gameObject.name}: scaleTarget 이미 설정됨 (수동 설정)");
            
            return;
        }
        
        // 자동 탐색
        if (autoCreateScaleTarget)
        {
            if (showDebugLogs)
                Debug.Log($"🔍 [UIButtonClickEffect] {gameObject.name}: scaleTarget 자동 탐색 시작 (자식 수: {transform.childCount})");
            
            // 첫 번째 자식 RectTransform 찾기
            if (transform.childCount > 0)
            {
                scaleTarget = transform.GetChild(0) as RectTransform;
                
                if (scaleTarget != null)
                {
                    originalScale = scaleTarget.localScale;
                    
                    if (showDebugLogs)
                        Debug.Log($"✅ [UIButtonClickEffect] {gameObject.name}: 자식 RectTransform을 scaleTarget으로 설정 ({scaleTarget.name})");
                    
                    return;
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"⚠️ [UIButtonClickEffect] {gameObject.name}: 첫 번째 자식이 RectTransform이 아님 ({transform.GetChild(0).GetType()})");
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [UIButtonClickEffect] {gameObject.name}: 자식이 없음 (childCount: 0)");
            }
        }
        
        // 자식도 없으면 버튼 자체 사용
        scaleTarget = GetComponent<RectTransform>();
        originalScale = scaleTarget.localScale;
        
        if (showDebugLogs)
            Debug.Log($"⚠️ [UIButtonClickEffect] {gameObject.name}: 버튼 자체를 scaleTarget으로 사용 (Layout Group 주의!)");
    }
    
    /// <summary>
    /// onClick 딜레이 시스템 설정
    /// </summary>
    private void SetupClickDelaySystem()
    {
        if (hasSetupClickDelay)
            return;
        
        hasSetupClickDelay = true;
        
        // 1. 기존 onClick 백업 (Persistent + Runtime 이벤트 모두 포함)
        originalOnClick = button.onClick;
        
        if (showDebugLogs)
        {
            int persistentCount = originalOnClick.GetPersistentEventCount();
            
            Debug.Log($"📦 [UIButtonClickEffect] {gameObject.name}: onClick 백업 완료\n" +
                     $"   - Button Type: {buttonType}\n" +
                     $"   - Delay: {effectDelayTime}초\n" +
                     $"   - Persistent 이벤트: {persistentCount}개\n" +
                     $"   - Runtime 이벤트도 포함됨 (백업 완료)");
        }
        
        // 2. 새로운 onClick으로 완전 교체 (효과 핸들러만)
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(OnClickWithDelay);
        
        if (showDebugLogs)
            Debug.Log($"✅ [UIButtonClickEffect] {gameObject.name}: 클릭 딜레이 시스템 설정 완료");
    }
    
    #endregion
    
    #region Click Handlers
    
    /// <summary>
    /// 효과만 재생 (딜레이 비활성화)
    /// </summary>
    private void OnClickEffectOnly()
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [UIButtonClickEffect] {gameObject.name} 클릭! (타입: {buttonType}, 딜레이 비활성화)");
        
        PlayClickEffect();
        EmitCueEvent();
    }
    
    /// <summary>
    /// 효과 + 딜레이 + 이벤트 실행
    /// </summary>
    private void OnClickWithDelay()
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [UIButtonClickEffect] {gameObject.name} 클릭! (타입: {buttonType}, 딜레이: {effectDelayTime}초)");
        
        StartCoroutine(ClickEffectSequence());
    }
    
    /// <summary>
    /// 클릭 효과 시퀀스 (효과 → 딜레이 → 이벤트)
    /// </summary>
    private IEnumerator ClickEffectSequence()
    {
        if (showDebugLogs)
            Debug.Log($"🎬 [UIButtonClickEffect] {gameObject.name}: ClickEffectSequence 시작");
        
        // 1. 효과 재생
        if (showDebugLogs)
            Debug.Log($"1️⃣ [UIButtonClickEffect] {gameObject.name}: PlayClickEffect 호출");
        
        PlayClickEffect();
        
        // 2. Cue 이벤트 발행
        if (showDebugLogs)
            Debug.Log($"2️⃣ [UIButtonClickEffect] {gameObject.name}: EmitCueEvent 호출");
        
        EmitCueEvent();
        
        // 3. 딜레이 (효과를 볼 시간 제공)
        if (showDebugLogs)
            Debug.Log($"3️⃣ [UIButtonClickEffect] {gameObject.name}: {effectDelayTime}초 대기 중...");
        
        yield return new WaitForSecondsRealtime(effectDelayTime);
        
        // 4. 원래 onClick 이벤트 실행
        if (showDebugLogs)
            Debug.Log($"4️⃣ [UIButtonClickEffect] {gameObject.name}: 딜레이 완료, onClick 실행 시작");
        
        InvokeOriginalClickEvents();
        
        if (showDebugLogs)
            Debug.Log($"🏁 [UIButtonClickEffect] {gameObject.name}: ClickEffectSequence 완료");
    }
    
    /// <summary>
    /// 원래 onClick 이벤트 실행 (백업된 이벤트)
    /// </summary>
    private void InvokeOriginalClickEvents()
    {
        if (originalOnClick == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [UIButtonClickEffect] {gameObject.name}: 백업된 onClick 이벤트가 없습니다!");
            return;
        }
        
        if (showDebugLogs)
        {
            int persistentCount = originalOnClick.GetPersistentEventCount();
            Debug.Log($"▶️ [UIButtonClickEffect] {gameObject.name}: 백업된 onClick 실행 중... (Persistent: {persistentCount}개)");
        }
        
        // 백업된 onClick 실행 (Persistent + Runtime 이벤트 모두 포함)
        originalOnClick.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"✅ [UIButtonClickEffect] {gameObject.name}: onClick 실행 완료!");
    }
    
    #endregion
    
    #region Effect Playback
    
    /// <summary>
    /// 버튼 타입별 클릭 효과 재생
    /// </summary>
    private void PlayClickEffect()
    {
        if (showDebugLogs)
            Debug.Log($"🎨 [UIButtonClickEffect] {gameObject.name}: PlayClickEffect 시작 (타입: {buttonType}, scaleTarget: {scaleTarget != null})");
        
        // scaleTarget 필수 체크
        if (scaleTarget == null)
        {
            Debug.LogError($"❌ [UIButtonClickEffect] {gameObject.name}: scaleTarget이 null! 효과를 재생할 수 없습니다.");
            return;
        }
        
        // 1. 기존 Tween 정리
        KillAllTweens();
        
        // 2. 상태 리셋
        ResetToOriginalState();
        
        // 3. 버튼 타입별 효과 실행
        switch (buttonType)
        {
            case UIButtonType.Navigation:
                PlayNavigationEffect();
                break;
            
            case UIButtonType.Action:
                PlayActionEffect();
                break;
            
            case UIButtonType.Utility:
                PlayUtilityEffect();
                break;
            
            case UIButtonType.Tab:
                PlayTabEffect();
                break;
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ [UIButtonClickEffect] {gameObject.name}: PlayClickEffect 완료");
    }
    
    /// <summary>
    /// Navigation 버튼 효과 (약한 Scale)
    /// </summary>
    private void PlayNavigationEffect()
    {
        if (scaleTarget != null)
        {
            currentScaleTween = scaleTarget.DOScale(originalScale * scaleAmount, scaleDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    scaleTarget.DOScale(originalScale, scaleDuration * 0.5f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)
                        .OnComplete(() => currentScaleTween = null);
                });
        }
    }
    
    /// <summary>
    /// Action 버튼 효과 (강한 Punch)
    /// </summary>
    private void PlayActionEffect()
    {
        if (scaleTarget != null)
        {
            if (showDebugLogs)
                Debug.Log($"💥 [UIButtonClickEffect] {gameObject.name}: Action Punch 효과 시작 (punchScale: {punchScale})");
            
            currentScaleTween = scaleTarget.DOPunchScale(Vector3.one * punchScale, scaleDuration * 3f, 5, 0.5f)
                .SetEase(Ease.OutElastic)
                .SetUpdate(true)
                .OnComplete(() => 
                {
                    currentScaleTween = null;
                    
                    if (showDebugLogs)
                        Debug.Log($"✅ [UIButtonClickEffect] {gameObject.name}: Action Punch 효과 완료");
                });
        }
        else
        {
            Debug.LogError($"❌ [UIButtonClickEffect] {gameObject.name}: PlayActionEffect - scaleTarget이 null!");
        }
    }
    
    /// <summary>
    /// Utility 버튼 효과 (최소한의 Scale)
    /// </summary>
    private void PlayUtilityEffect()
    {
        if (scaleTarget != null)
        {
            currentScaleTween = scaleTarget.DOScale(originalScale * utilityScaleAmount, scaleDuration * 0.4f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    scaleTarget.DOScale(originalScale, scaleDuration * 0.4f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)
                        .OnComplete(() => currentScaleTween = null);
                });
        }
    }
    
    /// <summary>
    /// Tab 버튼 효과 (매우 약한 Scale)
    /// </summary>
    private void PlayTabEffect()
    {
        if (scaleTarget != null)
        {
            // Tab은 가장 약한 효과 (98% 축소, 짧은 시간)
            currentScaleTween = scaleTarget.DOScale(originalScale * 0.98f, scaleDuration * 0.4f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    scaleTarget.DOScale(originalScale, scaleDuration * 0.4f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)
                        .OnComplete(() => currentScaleTween = null);
                });
        }
    }
    
    #endregion
    
    #region Cue System
    
    /// <summary>
    /// Cue System 이벤트 발행
    /// </summary>
    private void EmitCueEvent()
    {
        string eventKey = GetCueEventKey();
        
        if (string.IsNullOrEmpty(eventKey))
            return;
        
        // CueEmitter 사용 (UI domain)
        var context = CueContext.From(transform, 1.0f);
        bool success = CueEmitter.Emit(eventKey, "UI", context);
        
        if (showDebugLogs)
            Debug.Log($"🎵 [UIButtonClickEffect] Cue 발행: {eventKey} → {(success ? "성공" : "실패")}");
    }
    
    /// <summary>
    /// Cue 이벤트 키 생성
    /// </summary>
    private string GetCueEventKey()
    {
        // 수동 override
        if (!useAutoCueKey && !string.IsNullOrEmpty(customCueKey))
        {
            return customCueKey;
        }
        
        // 자동 생성: ui.button.{타입명}
        return $"ui.button.{buttonType.ToString().ToLower()}";
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 모든 Tween 즉시 종료
    /// </summary>
    private void KillAllTweens()
    {
        currentScaleTween?.Kill();
        currentScaleTween = null;
    }
    
    /// <summary>
    /// 원본 상태로 리셋
    /// </summary>
    private void ResetToOriginalState()
    {
        if (scaleTarget != null)
        {
            scaleTarget.localScale = originalScale;
        }
    }
    
    #endregion
    
    
    #region Editor Helper
    
#if UNITY_EDITOR
    [ContextMenu("테스트: Navigation 효과")]
    private void TestNavigationEffect()
    {
        buttonType = UIButtonType.Navigation;
        PlayClickEffect();
    }
    
    [ContextMenu("테스트: Action 효과")]
    private void TestActionEffect()
    {
        buttonType = UIButtonType.Action;
        PlayClickEffect();
    }
    
    [ContextMenu("테스트: Utility 효과")]
    private void TestUtilityEffect()
    {
        buttonType = UIButtonType.Utility;
        PlayClickEffect();
    }
    
    [ContextMenu("테스트: Tab 효과")]
    private void TestTabEffect()
    {
        buttonType = UIButtonType.Tab;
        PlayClickEffect();
    }
#endif
    
    #endregion
}
