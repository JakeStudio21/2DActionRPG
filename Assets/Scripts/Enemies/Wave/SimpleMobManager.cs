using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SimpleMob 중앙 AI 관리자
/// - 모든 SimpleMob의 AI를 일괄 업데이트 (0.1초 간격)
/// - 화면 밖 몬스터는 AI 스킵
/// - 개별 코루틴 방식보다 효율적
/// </summary>
public class SimpleMobManager : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private float aiUpdateInterval = 0.1f;
    [SerializeField] private bool skipOffScreenMobs = true;
    [SerializeField] private float screenPadding = 2f; // 화면 밖 여유 범위
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showGizmos = false;
    
    // 관리 중인 SimpleMob 리스트
    private List<SimpleMob> activeMobs = new List<SimpleMob>();
    private Camera mainCamera;
    
    // 화면 경계
    private Plane[] frustumPlanes;
    
    private void Awake()
    {
        mainCamera = Camera.main;
    }
    
    private void Start()
    {
        InvokeRepeating(nameof(UpdateAllMobAI), 0f, aiUpdateInterval);
    }
    
    /// <summary>
    /// SimpleMob 등록
    /// </summary>
    public void RegisterMob(SimpleMob mob)
    {
        if (!activeMobs.Contains(mob))
        {
            activeMobs.Add(mob);
            
            if (enableDebugLogs)
                Debug.Log($"[SimpleMobManager] 몬스터 등록: {mob.name} (총 {activeMobs.Count}마리)");
        }
    }
    
    /// <summary>
    /// SimpleMob 제거
    /// </summary>
    public void UnregisterMob(SimpleMob mob)
    {
        if (activeMobs.Contains(mob))
        {
            activeMobs.Remove(mob);
            
            if (enableDebugLogs)
                Debug.Log($"[SimpleMobManager] 몬스터 제거: {mob.name} (남은 {activeMobs.Count}마리)");
        }
    }
    
    /// <summary>
    /// 모든 SimpleMob AI 일괄 업데이트
    /// </summary>
    private void UpdateAllMobAI()
    {
        if (activeMobs.Count == 0) return;
        
        // 화면 경계 계산 (skipOffScreenMobs가 true일 때만)
        if (skipOffScreenMobs && mainCamera != null)
        {
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        }
        
        int updatedCount = 0;
        
        // 모든 몬스터 AI 업데이트
        for (int i = activeMobs.Count - 1; i >= 0; i--)
        {
            SimpleMob mob = activeMobs[i];
            
            // null 체크
            if (mob == null || mob.gameObject == null)
            {
                activeMobs.RemoveAt(i);
                continue;
            }
            
            // 죽은 몬스터 스킵
            if (mob.IsDead)
            {
                continue;
            }
            
            // 화면 밖 몬스터 스킵 (옵션)
            if (skipOffScreenMobs && !IsInCameraView(mob.transform.position))
            {
                continue;
            }
            
            // AI 업데이트
            mob.UpdateAI();
            updatedCount++;
        }
        
        if (enableDebugLogs && updatedCount > 0)
        {
            Debug.Log($"[SimpleMobManager] AI 업데이트: {updatedCount}/{activeMobs.Count}마리");
        }
    }
    
    /// <summary>
    /// 카메라 시야 내에 있는지 체크
    /// </summary>
    private bool IsInCameraView(Vector3 position)
    {
        if (mainCamera == null) return true;
        
        // Bounds 생성 (패딩 적용)
        Bounds bounds = new Bounds(position, Vector3.one * screenPadding);
        
        // Frustum 내부 체크
        return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
    }
    
    /// <summary>
    /// 현재 활성 몬스터 수
    /// </summary>
    public int GetActiveMobCount()
    {
        // null과 죽은 몬스터 제외
        int count = 0;
        for (int i = activeMobs.Count - 1; i >= 0; i--)
        {
            SimpleMob mob = activeMobs[i];
            if (mob != null && mob.gameObject != null && !mob.IsDead)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// 모든 SimpleMob 강제 제거
    /// </summary>
    public void ClearAllMobs()
    {
        foreach (SimpleMob mob in activeMobs)
        {
            if (mob != null && mob.gameObject != null)
            {
                if (GamePoolManager.Instance != null)
                {
                    GamePoolManager.Instance.ReturnToPool(mob.gameObject.tag, mob.gameObject);
                }
                else
                {
                    Destroy(mob.gameObject);
                }
            }
        }
        
        activeMobs.Clear();
        
        if (enableDebugLogs)
            Debug.Log("[SimpleMobManager] 모든 몬스터 제거 완료");
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || activeMobs.Count == 0) return;
        
        // 활성 몬스터 시각화
        Gizmos.color = Color.green;
        foreach (SimpleMob mob in activeMobs)
        {
            if (mob != null && !mob.IsDead)
            {
                Gizmos.DrawWireSphere(mob.transform.position, 0.5f);
                
                // 플레이어로의 방향 표시
                if (mob.PlayerTransform != null)
                {
                    Gizmos.DrawLine(mob.transform.position, mob.PlayerTransform.position);
                }
            }
        }
    }
}

