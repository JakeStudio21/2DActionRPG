using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CueSystem;

/// <summary>
/// BGM 컨트롤러 (스택 기반 우선순위 시스템)
/// Singleton + DontDestroyOnLoad
/// </summary>
public class BGMController : Singleton<BGMController>
{
    [Header("=== BGM 설정 ===")]
    [Tooltip("BGM 도메인 이름")]
    [SerializeField] private string bgmDomain = "BGM";
    
    [Tooltip("BGM 페이드 시간 (초)")]
    [SerializeField] private float fadeTime = 1f;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 우선순위 정의
    public enum BGMPriority
    {
        Default = 0,   // 기본 탐험
        Battle = 1,    // 전투
        Boss = 2,      // 보스
        Cutscene = 3   // 컷신 (최우선)
    }
    
    // 활성 상태 스택 (우선순위 → BGM 키)
    private Dictionary<BGMPriority, string> activeStates = new Dictionary<BGMPriority, string>();
    
    // 현재 재생 중인 BGM
    private string currentBGM = "";
    
    // 현재 스테이지 ID (폴백 시스템용)
    private string _currentStageId = null;
    
    protected override void Awake()
    {
        base.Awake();
        
        // CutsceneManager 이벤트 구독 (있으면)
        if (CutsceneSystem.CutsceneManager.Instance != null)
        {
            CutsceneSystem.CutsceneManager.Instance.OnCutsceneStart += OnCutsceneStarted;
            CutsceneSystem.CutsceneManager.Instance.OnCutsceneEnd += OnCutsceneEnded;
        }
        
        if (enableDebugLogs)
            Debug.Log("[BGMController] 초기화 완료");
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        // 이벤트 구독 해제
        if (CutsceneSystem.CutsceneManager.Instance != null)
        {
            CutsceneSystem.CutsceneManager.Instance.OnCutsceneStart -= OnCutsceneStarted;
            CutsceneSystem.CutsceneManager.Instance.OnCutsceneEnd -= OnCutsceneEnded;
        }
    }
    
    #region Public API
    
    /// <summary>
    /// 기본 BGM 재생 (씬 진입 시)
    /// </summary>
    public void PlayDefaultBGM(string bgmKey, string stageId = null)
    {
        if (enableDebugLogs)
            Debug.Log($"📥 [BGMController] PlayDefaultBGM() 호출됨 - Key: '{bgmKey}', StageId: '{stageId}'");
        
        // ✅ 씬 전환 시 이전 상태 초기화 (Battle, Boss 등 제거)
        if (enableDebugLogs && activeStates.Count > 0)
            Debug.Log($"🔄 [BGMController] 이전 씬 상태 초기화 (활성 상태 수: {activeStates.Count})");
        
        activeStates.Clear();
        
        // ✅ 현재 스테이지 ID 저장 (폴백 시스템용)
        _currentStageId = stageId;
        
        string resolvedKey = ResolveKey(bgmKey, stageId);
        
        if (enableDebugLogs)
            Debug.Log($"🔑 [BGMController] 키 해석 완료: '{bgmKey}' → '{resolvedKey}'");
        
        AddState(BGMPriority.Default, resolvedKey);
    }
    
    /// <summary>
    /// 전투 시작
    /// </summary>
    public void OnBattleStart(string stageId = null)
    {
        // ✅ stageId가 없으면 현재 스테이지 ID 사용
        string targetStageId = stageId ?? _currentStageId;
        
        string key = "bgm.stage.battle";
        string resolvedKey = ResolveKey(key, targetStageId);
        AddState(BGMPriority.Battle, resolvedKey);
    }
    
    /// <summary>
    /// 전투 종료
    /// </summary>
    public void OnBattleEnd()
    {
        RemoveState(BGMPriority.Battle);
    }
    
    /// <summary>
    /// 보스 시작
    /// </summary>
    public void OnBossStart(string stageId = null)
    {
        // ✅ stageId가 없으면 현재 스테이지 ID 사용
        string targetStageId = stageId ?? _currentStageId;
        
        string key = "bgm.stage.boss";
        string resolvedKey = ResolveKey(key, targetStageId);
        AddState(BGMPriority.Boss, resolvedKey);
    }
    
    /// <summary>
    /// 보스 종료
    /// </summary>
    public void OnBossEnd()
    {
        RemoveState(BGMPriority.Boss);
    }
    
    /// <summary>
    /// BGM 즉시 정지
    /// </summary>
    public void StopBGM()
    {
        activeStates.Clear();
        currentBGM = "";
        
        // ✅ CuePlayer의 BGM 정지
        if (CueSystem.CuePlayer.Instance != null)
        {
            CueSystem.CuePlayer.Instance.StopCurrentBGM();
        }
        
        if (enableDebugLogs)
            Debug.Log("[BGMController] BGM 정지");
    }
    
    #endregion
    
    #region 상태 관리
    
    /// <summary>
    /// 상태 추가 (스택에 push)
    /// </summary>
    private void AddState(BGMPriority priority, string bgmKey)
    {
        if (enableDebugLogs)
            Debug.Log($"📌 [BGMController] AddState() 호출 - Priority: {priority}, Key: '{bgmKey}'");
        
        if (string.IsNullOrEmpty(bgmKey))
        {
            Debug.LogWarning($"⚠️ [BGMController] BGM 키가 비어있습니다 (Priority: {priority})");
            return;
        }
        
        activeStates[priority] = bgmKey;
        
        if (enableDebugLogs)
            Debug.Log($"✅ [BGMController] 상태 추가 완료: {priority} → {bgmKey} (활성 상태 수: {activeStates.Count})");
        
        UpdateBGM();
    }
    
    /// <summary>
    /// 상태 제거 (스택에서 pop)
    /// </summary>
    private void RemoveState(BGMPriority priority)
    {
        if (activeStates.ContainsKey(priority))
        {
            string removedKey = activeStates[priority];
            activeStates.Remove(priority);
            
            if (enableDebugLogs)
                Debug.Log($"[BGMController] 상태 제거: {priority} ({removedKey})");
            
            UpdateBGM();
        }
    }
    
    /// <summary>
    /// 현재 최고 우선순위 BGM 재생
    /// </summary>
    private void UpdateBGM()
    {
        if (enableDebugLogs)
            Debug.Log($"🔄 [BGMController] UpdateBGM() 호출 - 현재 BGM: '{currentBGM}', 활성 상태 수: {activeStates.Count}");
        
        // 활성 상태가 없으면 정지
        if (activeStates.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log($"⚠️ [BGMController] 활성 상태 없음 - BGM 정지");
            StopBGM();
            return;
        }
        
        // 가장 높은 우선순위 찾기
        var highestPriority = activeStates.Keys.Max();
        string bgmKey = activeStates[highestPriority];
        
        if (enableDebugLogs)
            Debug.Log($"🎯 [BGMController] 최고 우선순위: {highestPriority}, Key: '{bgmKey}'");
        
        // 중복 재생 방지
        if (currentBGM == bgmKey)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"⏸️ [BGMController] 이미 재생 중: '{bgmKey}' - 재생 스킵");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"🎵 [BGMController] BGM 전환: '{currentBGM}' → '{bgmKey}'");
        
        // BGM 전환
        PlayBGM(bgmKey);
    }
    
    #endregion
    
    #region BGM 재생
    
    /// <summary>
    /// BGM 재생 (실제 CueSystem 호출) - Fade 지원
    /// </summary>
    private void PlayBGM(string bgmKey)
    {
        if (enableDebugLogs)
            Debug.Log($"🎼 [BGMController] PlayBGM() 호출 - Key: '{bgmKey}', Domain: '{bgmDomain}'");
        
        if (string.IsNullOrEmpty(bgmKey))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"⚠️ [BGMController] BGM 키가 비어있음 - 재생 중단");
            return;
        }
        
        // ✅ Fade 적용 BGM 재생
        if (CueSystem.CuePlayer.Instance != null)
        {
            bool success = CueSystem.CuePlayer.Instance.PlayBGMWithFade(bgmKey, bgmDomain, fadeTime);
            
            if (success)
            {
                currentBGM = bgmKey;
                
                if (enableDebugLogs)
                    Debug.Log($"✅ [BGMController] 🎵 BGM 재생 완료 (Fade {fadeTime}s): {bgmKey}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"⚠️ [BGMController] BGM 재생 실패: {bgmKey}");
            }
        }
        else
        {
            Debug.LogError("🔴 [BGMController] CuePlayer가 없습니다!");
        }
    }
    
    #endregion
    
    #region 폴백 시스템
    
    /// <summary>
    /// BGM 키 해석 (폴백 시스템)
    /// </summary>
    private string ResolveKey(string requestedKey, string stageId = null)
    {
        if (enableDebugLogs)
            Debug.Log($"🔍 [BGMController] ResolveKey() - 입력: '{requestedKey}', StageId: '{stageId}'");
        
        // stageId가 제공되면 스테이지 전용 키 생성
        if (!string.IsNullOrEmpty(stageId) && requestedKey.Contains("bgm.stage."))
        {
            if (enableDebugLogs)
                Debug.Log($"   → StageId 있음 + 'bgm.stage.' 포함 - 스테이지 전용 키 생성 시도");
            
            // "bgm.stage.battle" + "STAGE_001" → "bgm.stage.STAGE_001.battle"
            string[] parts = requestedKey.Split('.');
            if (parts.Length >= 3)
            {
                // bgm.stage.{stageId}.{suffix}
                string suffix = parts[2];
                string stageSpecificKey = $"bgm.stage.{stageId}.{suffix}";
                
                if (enableDebugLogs)
                    Debug.Log($"   → 스테이지 전용 키 생성: '{stageSpecificKey}' (suffix: {suffix})");
                
                // ✅ 1순위: 스테이지 전용 키 존재 확인
                if (CueSystem.CueRegistry.Instance != null)
                {
                    bool hasStageSpecificKey = CueSystem.CueRegistry.Instance.HasKey(bgmDomain, stageSpecificKey);
                    
                    if (hasStageSpecificKey)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"   ✅ 스테이지 전용 키 발견: '{stageSpecificKey}'");
                        return stageSpecificKey;
                    }
                    else
                    {
                        if (enableDebugLogs)
                            Debug.Log($"   ⚠️ 스테이지 전용 키 없음: '{stageSpecificKey}'");
                        Debug.Log($"   🔄 폴백: 공용 키 사용 '{requestedKey}'");
                    }
                }
            }
        }
        
        // 2순위: 요청된 키 그대로 반환 (공용 키)
        if (enableDebugLogs)
            Debug.Log($"   → 공용 키 사용: '{requestedKey}'");
        
        return requestedKey;
    }
    
    #endregion
    
    #region 이벤트 리스너
    
    /// <summary>
    /// 컷신 시작 (자동)
    /// </summary>
    private void OnCutsceneStarted(string cutsceneId)
    {
        // TODO: 컷신별 전용 BGM이 있으면 재생
        // 현재는 컷신 시작 시 BGM 유지 (일시정지 등은 CutsceneManager에서 처리)
        
        if (enableDebugLogs)
            Debug.Log($"[BGMController] 컷신 시작: {cutsceneId}");
    }
    
    /// <summary>
    /// 컷신 종료 (자동)
    /// </summary>
    private void OnCutsceneEnded(string cutsceneId)
    {
        // 컷신 종료 시 이전 BGM으로 자동 복귀 (스택 시스템이 자동 처리)
        
        if (enableDebugLogs)
            Debug.Log($"[BGMController] 컷신 종료: {cutsceneId}");
    }
    
    #endregion
}
