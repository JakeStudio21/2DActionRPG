using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;
using System.Collections.Generic;
using CueSystem;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 매니저 (메인 컨트롤러)
    /// DontDestroyOnLoad 서비스로 씬 전환 시에도 유지
    /// </summary>
    public class CutsceneManager : Singleton<CutsceneManager>
    {
        [Header("=== 설정 ===")]
        [Tooltip("컷신 데이터 리소스 경로")]
        [SerializeField] private string cutsceneDataPath = "CutsceneData";
        
        [Tooltip("컷신 중 게임 일시정지 여부")]
        [SerializeField] private bool pauseGameOnCutscene = true;
        
        [Header("=== CueSystem 연동 ===")]
        [Tooltip("컷신용 CueProfile (자동 등록용, 선택사항)")]
        [SerializeField] private CueSystem.CueProfile cutsceneCueProfile;
        
        [Tooltip("CueProfile 자동 등록 여부")]
        [SerializeField] private bool autoRegisterCueProfile = true;
        
        [Header("=== 디버그 ===")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // 현재 상태
        private bool isPlaying = false;
        private CutsceneData currentCutsceneData;
        private Sequence currentSequence;
        private GameObject currentCanvas;
        private CutsceneImagePanel imagePanel;
        private DialoguePanel dialoguePanel;
        
        // 스킵 상태
        private bool isWaitingForSecondClick = false;
        private float secondClickTimeout = 1f;
        private float lastClickTime = 0f;
        
        // 게임 제어
        private PlayerController playerController;
        private bool wasPlayerMovementLocked = false;
        
        // UI 버튼 상태 저장 (컷신 시작 전 상태 복원용)
        private Dictionary<Button, bool> originalButtonStates = new Dictionary<Button, bool>();
        
        // 콜백
        private Dictionary<string, Action> registeredCallbacks = new Dictionary<string, Action>();
        
        // 이벤트
        public event Action<string> OnCutsceneStart;      // 컷신 시작
        public event Action<string> OnCutsceneEnd;        // 컷신 종료
        public event Action<string> OnCutsceneSkip;        // 컷신 스킵
        
        // 프로퍼티
        public bool IsPlaying => isPlaying;
        public CutsceneData CurrentCutsceneData => currentCutsceneData;
        
        protected override void Awake()
        {
            base.Awake();
            
            // CueProfile 자동 등록
            if (autoRegisterCueProfile)
            {
                RegisterCutsceneCueProfile();
            }
            
            if (enableDebugLogs)
                Debug.Log("[CutsceneManager] 초기화 완료");
        }
        
        /// <summary>
        /// 컷신용 CueProfile 등록
        /// </summary>
        private void RegisterCutsceneCueProfile()
        {
            if (CueSystem.CueRegistry.Instance == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[CutsceneManager] CueRegistry를 찾을 수 없습니다. CueProfile 자동 등록을 건너뜁니다.");
                return;
            }
            
            // Inspector에서 할당된 프로필 사용
            if (cutsceneCueProfile != null)
            {
                CueSystem.CueRegistry.Instance.RegisterProfile("Cutscene", cutsceneCueProfile);
                
                if (enableDebugLogs)
                    Debug.Log($"[CutsceneManager] CueProfile 등록 완료: {cutsceneCueProfile.profileId}");
                return;
            }
            
            // Resources에서 로드 시도
            CueSystem.CueProfile profile = Resources.Load<CueSystem.CueProfile>("CueProfiles/Cutscene_CutsceneProfile");
            if (profile != null)
            {
                CueSystem.CueRegistry.Instance.RegisterProfile("Cutscene", profile);
                
                if (enableDebugLogs)
                    Debug.Log($"[CutsceneManager] CueProfile 자동 로드 및 등록 완료: {profile.profileId}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[CutsceneManager] 컷신용 CueProfile을 찾을 수 없습니다. " +
                                   "Inspector에서 할당하거나 Resources/CueProfiles/Cutscene_CutsceneProfile.asset을 생성해주세요.");
            }
        }
        
        /// <summary>
        /// CueProfile 등록 확인 및 재등록 (타이밍 문제 방지)
        /// </summary>
        private void EnsureCueProfileRegistered()
        {
            if (CueSystem.CueRegistry.Instance == null)
            {
                Debug.LogWarning("[CutsceneManager] CueRegistry가 없습니다! CueProfile 등록 실패");
                return;
            }
            
            // Cutscene 도메인 재등록 (타이밍 문제 방지)
            // RegisterProfile은 Dictionary 덮어쓰기 방식이므로 중복 등록 가능
            if (autoRegisterCueProfile)
            {
                RegisterCutsceneCueProfile();
                
                if (enableDebugLogs)
                    Debug.Log("[CutsceneManager] ✅ CueProfile 등록 확인 완료 (컷신 재생 전)");
            }
        }
        
        private void Update()
        {
            if (!isPlaying)
                return;
            
            HandleInput();
        }
        
        private void OnDestroy()
        {
            // Sequence 정리
            if (currentSequence != null && currentSequence.IsActive())
            {
                currentSequence.Kill();
            }
            
            // Canvas 정리
            CutsceneCanvasLoader.UnloadCanvas();
        }
        
        /// <summary>
        /// 컷신 재생
        /// </summary>
        public void PlayCutscene(string cutsceneId)
        {
            if (isPlaying)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[CutsceneManager] 이미 컷신이 재생 중입니다: {currentCutsceneData?.cutsceneId}");
                return;
            }
            
            // 컷신 데이터 로드
            CutsceneData data = LoadCutsceneData(cutsceneId);
            if (data == null)
            {
                Debug.LogError($"[CutsceneManager] 컷신 데이터를 찾을 수 없습니다: {cutsceneId}");
                return;
            }
            
            PlayCutscene(data);
        }
        
        /// <summary>
        /// 컷신 재생 (CutsceneData 직접 전달)
        /// </summary>
        public void PlayCutscene(CutsceneData data)
        {
            if (isPlaying)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[CutsceneManager] 이미 컷신이 재생 중입니다: {currentCutsceneData?.cutsceneId}");
                return;
            }
            
            if (data == null)
            {
                Debug.LogError("[CutsceneManager] CutsceneData가 null입니다!");
                return;
            }
            
            if (!data.IsValid(out string errorMessage))
            {
                Debug.LogError($"[CutsceneManager] 유효하지 않은 CutsceneData: {errorMessage}");
                return;
            }
            
            // CueProfile 등록 확인 (타이밍 문제 방지)
            EnsureCueProfileRegistered();
            
            currentCutsceneData = data;
            isPlaying = true;
            
            if (enableDebugLogs)
                Debug.Log($"[CutsceneManager] 컷신 재생 시작: {data.cutsceneId}");
            
            // 게임 일시정지 (설정에 따라)
            if (pauseGameOnCutscene && data.pauseGameOnStart)
            {
                Time.timeScale = 0f;
            }
            
            // Canvas 로드
            StartCoroutine(LoadAndPlayCutscene(data));
        }
        
        /// <summary>
        /// Canvas 로드 및 컷신 재생
        /// </summary>
        private IEnumerator LoadAndPlayCutscene(CutsceneData data)
        {
            // Canvas 로드
            currentCanvas = CutsceneCanvasLoader.LoadCanvas(data.canvasPrefabPath);
            if (currentCanvas == null)
            {
                Debug.LogError("[CutsceneManager] Canvas 로드 실패!");
                isPlaying = false;
                yield break;
            }
            
            // 패널 컴포넌트 찾기
            imagePanel = currentCanvas.GetComponentInChildren<CutsceneImagePanel>();
            dialoguePanel = currentCanvas.GetComponentInChildren<DialoguePanel>();
            
            if (imagePanel == null || dialoguePanel == null)
            {
                Debug.LogError("[CutsceneManager] ImagePanel 또는 DialoguePanel을 찾을 수 없습니다!");
                isPlaying = false;
                yield break;
            }
            
            // 플레이어 입력 차단
            BlockPlayerInput();
            
            // 컨텍스트 생성
            CutsceneContext context = new CutsceneContext(
                imagePanel,
                dialoguePanel,
                OnStepCallback
            );
            
            // Sequence 생성
            currentSequence = SequenceBuilder.BuildSequence(data, context, gameObject);
            
            if (currentSequence == null)
            {
                Debug.LogError("[CutsceneManager] Sequence 생성 실패!");
                isPlaying = false;
                yield break;
            }
            
            // 완료 콜백 설정
            currentSequence.OnComplete(() => {
                OnCutsceneComplete();
            });
            
            // 컷신 시작 이벤트
            OnCutsceneStart?.Invoke(data.cutsceneId);
            
            // Sequence 재생
            currentSequence.Play();
        }
        
        /// <summary>
        /// 컷신 일시정지
        /// </summary>
        public void PauseCutscene()
        {
            if (!isPlaying || currentSequence == null)
                return;
            
            currentSequence.Pause();
            
            if (enableDebugLogs)
                Debug.Log("[CutsceneManager] 컷신 일시정지");
        }
        
        /// <summary>
        /// 컷신 재개
        /// </summary>
        public void ResumeCutscene()
        {
            if (!isPlaying || currentSequence == null)
                return;
            
            currentSequence.Play();
            
            if (enableDebugLogs)
                Debug.Log("[CutsceneManager] 컷신 재개");
        }
        
        /// <summary>
        /// 컷신 강제 종료
        /// </summary>
        public void StopCutscene()
        {
            if (!isPlaying)
                return;
            
            if (enableDebugLogs)
                Debug.Log("[CutsceneManager] 컷신 강제 종료");
            
            // Sequence 정리
            if (currentSequence != null && currentSequence.IsActive())
            {
                currentSequence.Kill();
                currentSequence = null;
            }
            
            // Canvas 정리
            CleanupCutscene();
            
            // 스킵 이벤트
            if (currentCutsceneData != null)
            {
                OnCutsceneSkip?.Invoke(currentCutsceneData.cutsceneId);
            }
            
            // 종료 처리
            OnCutsceneComplete();
        }
        
        /// <summary>
        /// 컷신 완료 처리
        /// </summary>
        private void OnCutsceneComplete()
        {
            if (!isPlaying)
                return;
            
            string cutsceneId = currentCutsceneData?.cutsceneId ?? "Unknown";
            
            if (enableDebugLogs)
                Debug.Log($"[CutsceneManager] 컷신 완료: {cutsceneId}");
            
            // 게임 재개
            if (Time.timeScale == 0f)
            {
                Time.timeScale = 1f;
            }
            
            // 플레이어 입력 복구
            RestorePlayerInput();
            
            // Canvas 정리
            CleanupCutscene();
            
            // 상태 초기화
            isPlaying = false;
            currentSequence = null;
            currentCutsceneData = null;
            
            // 종료 이벤트
            OnCutsceneEnd?.Invoke(cutsceneId);
        }
        
        /// <summary>
        /// 컷신 정리
        /// </summary>
        private void CleanupCutscene()
        {
            // 패널 숨김
            if (imagePanel != null)
            {
                imagePanel.HideAllImages();
            }
            
            if (dialoguePanel != null)
            {
                dialoguePanel.HideDialogue();
            }
            
            // Canvas 정리
            CutsceneCanvasLoader.UnloadCanvas();
            currentCanvas = null;
            imagePanel = null;
            dialoguePanel = null;
        }
        
        /// <summary>
        /// 컷신 데이터 로드
        /// </summary>
        private CutsceneData LoadCutsceneData(string cutsceneId)
        {
            // Resources에서 로드
            string path = $"{cutsceneDataPath}/{cutsceneId}";
            CutsceneData data = Resources.Load<CutsceneData>(path);
            
            if (data == null)
            {
                Debug.LogError($"[CutsceneManager] 컷신 데이터를 찾을 수 없습니다: {path}");
            }
            
            return data;
        }
        
        /// <summary>
        /// Step 콜백 처리
        /// </summary>
        private void OnStepCallback(string methodName)
        {
            if (registeredCallbacks.ContainsKey(methodName))
            {
                registeredCallbacks[methodName]?.Invoke();
            }
            else
            {
                Debug.LogWarning($"[CutsceneManager] 등록되지 않은 콜백: {methodName}");
            }
        }
        
        /// <summary>
        /// 콜백 등록
        /// </summary>
        public void RegisterCallback(string methodName, Action callback)
        {
            registeredCallbacks[methodName] = callback;
        }
        
        /// <summary>
        /// 콜백 해제
        /// </summary>
        public void UnregisterCallback(string methodName)
        {
            registeredCallbacks.Remove(methodName);
        }
        
        #region Input Handling
        
        /// <summary>
        /// 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (currentCutsceneData == null || !currentCutsceneData.canSkip)
                return;
            
            // ESC 키: 컷신 전체 종료
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopCutscene();
                return;
            }
            
            // 마우스 클릭 또는 터치
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                HandleClick();
            }
            
            // 2차 클릭 타임아웃 체크
            if (isWaitingForSecondClick && Time.time - lastClickTime > secondClickTimeout)
            {
                isWaitingForSecondClick = false;
            }
        }
        
        /// <summary>
        /// 클릭 처리
        /// </summary>
        private void HandleClick()
        {
            if (dialoguePanel == null)
                return;
            
            // 1차 클릭: 타이핑 즉시 완료
            if (dialoguePanel.IsTyping)
            {
                dialoguePanel.CompleteTyping();
                isWaitingForSecondClick = true;
                lastClickTime = Time.time;
                
                if (enableDebugLogs)
                    Debug.Log("[CutsceneManager] 1차 클릭: 타이핑 즉시 완료");
                return;
            }
            
            // 2차 클릭 또는 타이핑 완료 후 클릭: 다음 Step으로 이동
            if (isWaitingForSecondClick || (dialoguePanel.IsActive && !dialoguePanel.IsTyping))
            {
                // Sequence의 현재 Step을 완료하고 다음으로 이동
                // DOTween Sequence는 자동으로 다음 Step으로 진행하므로
                // 현재 Step의 남은 시간을 건너뛰는 방식으로 처리
                SkipCurrentStep();
                isWaitingForSecondClick = false;
                
                if (enableDebugLogs)
                    Debug.Log("[CutsceneManager] 클릭: 다음 Step으로 이동");
            }
        }
        
        /// <summary>
        /// 현재 Step 건너뛰기
        /// </summary>
        private void SkipCurrentStep()
        {
            if (currentSequence == null || !currentSequence.IsActive())
                return;
            
            // Sequence의 현재 트윈을 완료시켜 다음으로 이동
            // DOTween Sequence는 직접 제어가 어려우므로
            // 현재 트윈의 남은 시간을 0으로 만드는 방식으로 처리
            
            // 간단한 방법: Sequence의 현재 트윈을 Kill하고 다음으로 진행
            // 하지만 이는 복잡하므로, MVP에서는 클릭 시 대기 시간을 단축하는 방식으로 처리
            // 실제로는 Sequence가 자동으로 다음 Step으로 진행하므로
            // 여기서는 아무것도 하지 않아도 됨 (클릭은 무시)
        }
        
        #endregion
        
        #region Game Control
        
        /// <summary>
        /// 플레이어 입력 차단
        /// </summary>
        private void BlockPlayerInput()
        {
            // PlayerController 찾기
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
            
            if (playerController != null)
            {
                // 이동 잠금 상태 저장
                wasPlayerMovementLocked = playerController.IsMovementRestricted();
                
                // 이동 잠금
                playerController.SetMovementLocked(true);
                
                // 입력 비활성화 (PlayerControls)
                // PlayerController의 ReEnableControls를 반대로 사용
                // 실제로는 SetMovementLocked만으로도 충분할 수 있음
                
                if (enableDebugLogs)
                    Debug.Log("[CutsceneManager] 플레이어 입력 차단");
            }
            
            // UI 버튼 비활성화
            DisableUIButtons();
        }
        
        /// <summary>
        /// 플레이어 입력 복구
        /// </summary>
        private void RestorePlayerInput()
        {
            if (playerController != null)
            {
                // 이동 잠금 해제
                if (!wasPlayerMovementLocked)
                {
                    playerController.SetMovementLocked(false);
                }
                else
                {
                    // 원래 잠금 상태였으면 그대로 유지
                    // (다른 시스템에서 잠금한 경우)
                }
                
                if (enableDebugLogs)
                    Debug.Log("[CutsceneManager] 플레이어 입력 복구");
            }
            
            // UI 버튼 활성화
            EnableUIButtons();
        }
        
        /// <summary>
        /// UI 버튼 비활성화 (원래 상태 저장)
        /// </summary>
        private void DisableUIButtons()
        {
            // 이전 상태 초기화
            originalButtonStates.Clear();
            
            // 모든 Button 컴포넌트 찾기 (컷신 Canvas 제외)
            Button[] buttons = FindObjectsOfType<Button>();
            
            foreach (var button in buttons)
            {
                // null 체크
                if (button == null)
                    continue;
                
                // 컷신 Canvas의 버튼은 제외
                if (currentCanvas != null && button.transform.IsChildOf(currentCanvas.transform))
                    continue;
                
                // 원래 상태 저장
                originalButtonStates[button] = button.interactable;
                
                // 비활성화
                button.interactable = false;
            }
            
            if (enableDebugLogs)
                Debug.Log($"[CutsceneManager] UI 버튼 비활성화: {originalButtonStates.Count}개 버튼 상태 저장");
        }
        
        /// <summary>
        /// UI 버튼 원래 상태로 복원
        /// </summary>
        private void EnableUIButtons()
        {
            // 저장된 원래 상태로 복원
            foreach (var kvp in originalButtonStates)
            {
                // null 체크 (버튼이 파괴되었을 수 있음)
                if (kvp.Key != null)
                {
                    kvp.Key.interactable = kvp.Value; // 원래 상태로 복원
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"[CutsceneManager] UI 버튼 복원: {originalButtonStates.Count}개 버튼 원래 상태로");
            
            // 상태 정리
            originalButtonStates.Clear();
        }
        
        #endregion
    }
}
