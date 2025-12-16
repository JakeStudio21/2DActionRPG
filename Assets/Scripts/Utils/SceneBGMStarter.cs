using UnityEngine;

/// <summary>
/// 씬별 BGM 자동 재생 스크립트
/// 씬 시작 시 지정된 BGM을 재생합니다.
/// </summary>
public class SceneBGMStarter : MonoBehaviour
{
    [Header("=== BGM 설정 ===")]
    [Tooltip("재생할 BGM 이벤트 키 (예: bgm.lobby, bgm.stage.default)")]
    [SerializeField] private string bgmEventKey = "bgm.lobby";
    
    [Tooltip("스테이지 ID (선택사항, 예: STAGE_001)")]
    [SerializeField] private string stageId = "";
    
    [Tooltip("씬 로드 후 지연 시간 (초)")]
    [SerializeField] private float delay = 0.5f;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Start()
    {
        if (enableDebugLogs)
            Debug.Log($"[SceneBGMStarter] Start 호출됨 - Key: {bgmEventKey}, StageId: '{stageId}', Delay: {delay}s");
        
        if (delay > 0f)
        {
            Invoke(nameof(PlayBGM), delay);
        }
        else
        {
            PlayBGM();
        }
    }
    
    private void PlayBGM()
    {
        if (enableDebugLogs)
            Debug.Log($"🎬 [SceneBGMStarter] PlayBGM() 시작 - Key: {bgmEventKey}, StageId: '{stageId}'");
        
        if (BGMController.Instance == null)
        {
            Debug.LogWarning("⚠️ [SceneBGMStarter] BGMController를 찾을 수 없습니다!");
            return;
        }
        
        if (string.IsNullOrEmpty(bgmEventKey))
        {
            Debug.LogWarning("⚠️ [SceneBGMStarter] BGM 이벤트 키가 비어있습니다!");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"🎵 [SceneBGMStarter] BGM 재생 요청 전송: {bgmEventKey} (StageId: '{stageId}')");
        
        // 스테이지 ID가 있으면 전달
        if (!string.IsNullOrEmpty(stageId))
        {
            if (enableDebugLogs)
                Debug.Log($"   → PlayDefaultBGM('{bgmEventKey}', '{stageId}') 호출");
            BGMController.Instance.PlayDefaultBGM(bgmEventKey, stageId);
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"   → PlayDefaultBGM('{bgmEventKey}') 호출 (StageId 없음)");
            BGMController.Instance.PlayDefaultBGM(bgmEventKey);
        }
        
        if (enableDebugLogs)
            Debug.Log($"✅ [SceneBGMStarter] BGM 재생 요청 완료: {bgmEventKey}");
    }
}
