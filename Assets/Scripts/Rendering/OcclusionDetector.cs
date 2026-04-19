using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 🔍 오클루전 감지 컴포넌트 (플레이어용)
/// 캐릭터가 Occluder 뒤에 있을 때 자동 감지 및 페이드 처리 요청
/// </summary>
public class OcclusionDetector : MonoBehaviour
{
    [Header("🔍 감지 설정")]
    [SerializeField] private float detectionRadius = 2f; // 감지 반경
    [SerializeField] private LayerMask occluderLayer = -1; // Occluder 레이어 (기본: 모든 레이어)
    [SerializeField] private float checkInterval = 0.2f; // 체크 간격 (초)
    
    [Header("📏 소팅 비교 설정")]
    [SerializeField] private int sortingOrderBuffer = 50; // 소팅 오더 여유값
    
    [SerializeField] private bool showDetectionGizmo = false;
    
    // 컴포넌트 참조
    private FootPositionSorter footPositionSorter;
    
    // 현재 상태
    private HashSet<AlphaFader> activeOccluders = new HashSet<AlphaFader>();
    private Collider2D[] detectionBuffer = new Collider2D[10]; // 성능 최적화용 버퍼
    
    // 성능 최적화
    private float lastCheckTime;
    private Vector3 lastPosition = Vector3.positiveInfinity;
    private float moveThreshold = 0.1f; // 이동 감지 임계값
    
    void Start()
    {
        // FootPositionSorter 참조 가져오기
        footPositionSorter = GetComponent<FootPositionSorter>();
        if (footPositionSorter == null)
        {
            Debug.LogWarning($"⚠️ [OcclusionDetector] {gameObject.name}에 FootPositionSorter가 없습니다! 소팅 비교 불가");
        }
        
    }
    
    void Update()
    {
        // 체크 간격 제어
        if (Time.time - lastCheckTime < checkInterval) return;
        
        // 이동 감지 최적화 (움직이지 않으면 체크 스킵)
        Vector3 currentPos = transform.position;
        if (Vector3.Distance(currentPos, lastPosition) < moveThreshold && activeOccluders.Count == 0)
        {
            return;
        }
        
        CheckForOcclusion();
        
        lastCheckTime = Time.time;
        lastPosition = currentPos;
    }
    
    /// <summary>
    /// 🔍 오클루전 감지 및 처리
    /// </summary>
    private void CheckForOcclusion()
    {
        // 현재 캐릭터 소팅 오더 가져오기
        int playerSortingOrder = GetPlayerSortingOrder();
        
        // 주변 Occluder 감지
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position, 
            detectionRadius, 
            detectionBuffer, 
            occluderLayer
        );
        
        HashSet<AlphaFader> currentOccluders = new HashSet<AlphaFader>();
        
        // 감지된 오브젝트들 중 실제 Occluder 찾기
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = detectionBuffer[i];
            if (hit == null || hit.gameObject == gameObject) continue;
            
            // Occluder 태그 확인
            if (!hit.CompareTag("Occluder")) continue;
            
            // AlphaFader 컴포넌트 확인
            AlphaFader fader = hit.GetComponent<AlphaFader>();
            if (fader == null) continue;
            
            // 소팅 오더 비교 (Occluder가 플레이어보다 앞에 있어야 함)
            int occluderSortingOrder = fader.GetSortingOrder();
            if (occluderSortingOrder <= playerSortingOrder + sortingOrderBuffer)
            {
                // 플레이어가 Occluder 뒤에 있음
                currentOccluders.Add(fader);
                
            }
        }
        
        // 새로 가려진 오브젝트들 페이드 시작
        foreach (AlphaFader fader in currentOccluders)
        {
            if (!activeOccluders.Contains(fader))
            {
                fader.StartFadeOut();
            }
        }
        
        // 더 이상 가려지지 않는 오브젝트들 페이드 복구
        foreach (AlphaFader fader in activeOccluders)
        {
            if (!currentOccluders.Contains(fader))
            {
                fader.StartFadeIn();
                
            }
        }
        
        // 활성 목록 업데이트
        activeOccluders = currentOccluders;
    }
    
    /// <summary>
    /// 플레이어 소팅 오더 가져오기
    /// </summary>
    private int GetPlayerSortingOrder()
    {
        if (footPositionSorter != null)
        {
            return footPositionSorter.GetCurrentSortingOrder();
        }
        
        // 백업: SpriteRenderer에서 직접 가져오기
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return spriteRenderer.sortingOrder;
        }
        
        // 기본값: CHARACTER_LAYER
        return IsometricSorting.CHARACTER_LAYER;
    }
    
    /// <summary>
    /// 감지 반경 설정
    /// </summary>
    public void SetDetectionRadius(float radius)
    {
        detectionRadius = radius;
    }
    
    /// <summary>
    /// 모든 페이드 상태 초기화 (씬 전환 시 등)
    /// </summary>
    public void ClearAllOcclusions()
    {
        foreach (AlphaFader fader in activeOccluders)
        {
            if (fader != null)
            {
                fader.StartFadeIn();
            }
        }
        
        activeOccluders.Clear();
        
    }
    
    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (showDetectionGizmo)
        {
            // 감지 반경 표시 (2D에서는 원을 위에서 본 구로 표현)
            Gizmos.color = Color.cyan;
            Vector3 position = transform.position;
            
            // 원 모양으로 표시 (Y축 기준으로 평평한 구)
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(position, Vector3.forward, detectionRadius);
            
            // 활성 오클루더들 표시
            Gizmos.color = Color.red;
            foreach (AlphaFader fader in activeOccluders)
            {
                if (fader != null)
                {
                    Gizmos.DrawLine(transform.position, fader.transform.position);
                }
            }
        }
    }
    #endif
    
    void OnDisable()
    {
        // 비활성화 시 모든 페이드 복구
        ClearAllOcclusions();
    }
}