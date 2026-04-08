using UnityEngine;
using StageSystem;

/// <summary>
/// 웨이브 트리거 존 - 플레이어가 진입하면 웨이브 시작
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaveTriggerZone : MonoBehaviour
{
    [Header("트리거 설정")]
    [Tooltip("발동시킬 트리거 ID")]
    [SerializeField] private WaveTriggerId triggerIdToActivate = WaveTriggerId.PlayerReachedPoint;
    
    [Tooltip("한 번만 발동 (재진입 시 무시)")]
    [SerializeField] private bool triggerOnce = true;
    
    [Tooltip("트리거 발동 후 오브젝트 비활성화")]
    [SerializeField] private bool disableAfterTrigger = true;
    
    [Header("웨이브 강제 클리어 (OR 조건)")]
    [Tooltip("체크 시: 플레이어가 이 존을 통과하면 현재 진행 중인 웨이브를 즉시 클리어 판정.\n" +
             "남은 몬스터를 처치하지 않아도 다음 웨이브로 진행됩니다.\n" +
             "보스 스테이지에서 이전 웨이브를 스킵할 때 사용.")]
    [SerializeField] private bool forceCompleteCurrentWave = false;
    
    [Header("SimpleMob 직접 연결 (선택)")]
    [Tooltip("설정 시 StageManager를 거치지 않고 이 WaveSpawner를 직접 트리거합니다.\n" +
             "여러 구역을 독립적으로 운영할 때 사용. WaveData는 WaveSpawner에 설정.")]
    [SerializeField] private WaveSpawner directWaveSpawner;
    
    [Header("시각적 피드백")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f); // 주황색 반투명
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 상태
    private bool hasTriggered = false;
    private StageManager stageManager;
    private WaveController waveController;
    private Collider2D triggerCollider;
    
    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        
        // Trigger 설정 확인
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[WaveTriggerZone] {gameObject.name}의 Collider가 Trigger가 아닙니다! Is Trigger를 체크하세요.");
            triggerCollider.isTrigger = true;
        }
    }
    
    private void Start()
    {
        // StageManager 찾기
        stageManager = FindObjectOfType<StageManager>();
        
        if (stageManager == null)
        {
            Debug.LogError($"[WaveTriggerZone] StageManager를 찾을 수 없습니다! 씬에 StageManager가 있는지 확인하세요.");
        }
        
        // ForceComplete 옵션 사용 시 WaveController도 캐싱
        if (forceCompleteCurrentWave)
        {
            waveController = FindObjectOfType<WaveController>();
            if (waveController == null)
                Debug.LogWarning($"[WaveTriggerZone] {gameObject.name} — forceCompleteCurrentWave=true인데 WaveController를 찾을 수 없습니다.");
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [WaveTriggerZone] {gameObject.name} 초기화 완료 - Trigger ID: {triggerIdToActivate}");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어인지 확인
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player"))
        {
            return;
        }
        
        // 이미 발동했는지 체크
        if (triggerOnce && hasTriggered)
        {
            if (enableDebugLogs)
                Debug.Log($"⏭️ [WaveTriggerZone] {gameObject.name} - 이미 발동됨 (무시)");
            return;
        }
        
        // 트리거 발동
        ActivateTrigger();
    }
    
    /// <summary>
    /// 트리거 발동
    /// </summary>
    private void ActivateTrigger()
    {
        hasTriggered = true;
        
        if (enableDebugLogs)
            Debug.Log($"🎯 [WaveTriggerZone] {gameObject.name} 발동! Trigger ID: {triggerIdToActivate}");
        
        // [OR 조건] 현재 웨이브 강제 클리어 (몬스터가 남아있어도 다음 웨이브로 진행)
        if (forceCompleteCurrentWave && waveController != null)
        {
            waveController.ForceCompleteCurrentWave();
            if (enableDebugLogs)
                Debug.Log($"⏭️ [WaveTriggerZone] {gameObject.name} → 현재 웨이브 강제 클리어");
        }
        
        // [방식 B] 직접 연결된 WaveSpawner가 있으면 StageManager 없이 직접 트리거
        if (directWaveSpawner != null)
        {
            directWaveSpawner.TriggerStart();
            if (enableDebugLogs)
                Debug.Log($"🚀 [WaveTriggerZone] {gameObject.name} → WaveSpawner 직접 트리거: {directWaveSpawner.name}");
        }
        else if (!forceCompleteCurrentWave)
        {
            // [방식 A] StageManager 경유 (forceComplete 단독 사용이 아닐 때만)
            if (stageManager == null)
            {
                Debug.LogError($"[WaveTriggerZone] StageManager가 null이고 directWaveSpawner도 없습니다! 트리거를 발동할 수 없습니다.");
            }
            else
            {
                stageManager.TriggerWave(triggerIdToActivate);
            }
        }
        else if (stageManager != null)
        {
            // forceComplete + StageManager 경유 모드: 강제 클리어 후 추가 트리거도 발동 가능
            // (directWaveSpawner가 없는 경우에만 진입. triggerIdToActivate가 None이 아닐 때 발동)
            if (triggerIdToActivate != WaveTriggerId.None)
                stageManager.TriggerWave(triggerIdToActivate);
        }
        
        // 비활성화
        if (disableAfterTrigger)
        {
            if (enableDebugLogs)
                Debug.Log($"💤 [WaveTriggerZone] {gameObject.name} 비활성화");
            
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 트리거 리셋 (재사용 시)
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        gameObject.SetActive(true);
        
        if (enableDebugLogs)
            Debug.Log($"🔄 [WaveTriggerZone] {gameObject.name} 리셋 완료");
    }
    
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;
        
        // 트리거 영역 시각화
        Gizmos.color = hasTriggered ? Color.gray : gizmoColor;
        
        if (col is BoxCollider2D boxCollider)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is CircleCollider2D circleCollider)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius * transform.lossyScale.x);
        }
        
        // 트리거 ID 표시 (Scene View에서)
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, 
            $"Trigger: {triggerIdToActivate}\n{(hasTriggered ? "발동됨" : "대기 중")}");
        #endif
    }
}

