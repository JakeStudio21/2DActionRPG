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

    [Header("분리력 (Separation)")]
    [Tooltip("분리력 계산을 활성화합니다.")]
    [SerializeField] private bool enableSeparation = true;

    [Tooltip("분리력 계산을 수행할 최대 몬스터 수. 이 수를 초과하면 비용 절감을 위해 계산을 스킵합니다.")]
    [SerializeField] private int separationMaxMobs = 80;

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
            
            // AI 업데이트 (호출 주기를 전달하여 서브클래스 타이머 연산에 활용)
            mob.UpdateAI(aiUpdateInterval);
            updatedCount++;
        }

        // ③ 분리력 — 몬스터끼리 겹치지 않도록 서로 밀어냄
        // 몬스터 수가 상한을 초과하면 연산 스킵 (성능 안전장치)
        if (enableSeparation && activeMobs.Count <= separationMaxMobs)
        {
            ApplySeparationToAll();
        }
        
        if (enableDebugLogs && updatedCount > 0)
        {
            Debug.Log($"[SimpleMobManager] AI 업데이트: {updatedCount}/{activeMobs.Count}마리");
        }
    }

    /// <summary>
    /// 모든 활성 몬스터에 분리력 계산 및 적용
    /// O(n²) 쌍 비교 — sqrMagnitude 사용으로 sqrt 연산 없음
    /// aiUpdateInterval(0.1s) 주기로만 호출되므로 실제 부담 낮음
    /// </summary>
    private void ApplySeparationToAll()
    {
        int count = activeMobs.Count;

        for (int i = 0; i < count; i++)
        {
            SimpleMob a = activeMobs[i];
            if (a == null || a.IsDead) continue;

            // contactRange 안에서 정지 중인 몹은 분리력 적용 대상에서 제외
            // → 플레이어 근처에 멈춘 상태에서 밀려나는 진동 방지
            if (a.IsInContactRange) continue;

            Vector2 posA = a.transform.position;
            Vector2 totalForce = Vector2.zero;

            // SimpleMobData에서 반경/강도 읽기 (null이면 Manager 기본값 사용)
            float radius    = a.MobData != null ? a.MobData.separationRadius   : 1.2f;
            float strength  = a.MobData != null ? a.MobData.separationStrength : 2.0f;
            float sqrRadius = radius * radius;

            for (int j = i + 1; j < count; j++)
            {
                SimpleMob b = activeMobs[j];
                if (b == null || b.IsDead) continue;

                Vector2 posB  = b.transform.position;
                Vector2 diff  = posA - posB;
                float sqrDist = diff.sqrMagnitude;

                // 범위 밖이면 스킵
                if (sqrDist >= sqrRadius || sqrDist < 0.0001f) continue;

                // 가까울수록 강하게 밀어냄 (1/dist 비례)
                float dist      = Mathf.Sqrt(sqrDist);
                Vector2 pushDir = diff / dist;
                Vector2 push    = pushDir * (strength * (1f - dist / radius));

                totalForce += push;                    // a는 b로부터 밀려남

                // b가 contactRange 안에서 정지 중이 아닐 때만 반력 적용
                if (!b.IsInContactRange)
                    b.ApplySeparationForce(-push);
            }

            if (totalForce.sqrMagnitude > 0.0001f)
                a.ApplySeparationForce(totalForce);
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

