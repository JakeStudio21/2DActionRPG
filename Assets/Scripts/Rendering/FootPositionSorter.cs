using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 발 위치 기준 동적 소팅 컴포넌트 (SortingGroup 우선 지원)
/// </summary>
public class FootPositionSorter : MonoBehaviour
{
    [Header("기본 설정")]
    [SerializeField] private Transform footPosition; // 발 위치 기준점
    [SerializeField] private Vector2 footOffset = Vector2.zero; // Pivot 보정값
    [SerializeField] private int baseLayer = IsometricSorting.CHARACTER_LAYER;
    [SerializeField] private int heightOffset = 0; // 점프/비행 높이
    
    // 🗺️ 아이소메트릭 데이터 연동 추가
    [Header("🗺️ 아이소메트릭 데이터 연동")]
    [SerializeField] private bool useIsometricData = true;
    [SerializeField] private bool enableDynamicHeight = true; // HeightCurve 사용 여부
    
    [Header("업데이트 정책")]
    [SerializeField] private UpdatePolicy updatePolicy = UpdatePolicy.LateUpdate;
    [SerializeField] private float updateInterval = 0.1f; // FixedInterval용
    [SerializeField] private float moveThreshold = 0.01f; // OnMoved용
    
    [Header("디버그")]
    [SerializeField] private bool showDebugGizmo = false;
    [SerializeField] private bool showOrderInInspector = false;
    [SerializeField] private bool enableDebugLogs = false;
    
    // 컴포넌트 캐시
    private SortingGroup sortingGroup;
    private SpriteRenderer spriteRenderer;
    private Grid sceneGrid;
    
    // 성능 최적화
    private int lastSortingOrder = int.MinValue;
    private float lastUpdateTime;
    private Vector3 lastPosition = Vector3.positiveInfinity;
    private float gridCellSizeY = 1f;
    
    // 🗺️ 플레이어 클래스 참조 (아이소메트릭 데이터용)
    private BaseClassBehaviour playerClass;
    private bool isIsometricDataInitialized = false;
    
    public enum UpdatePolicy
    {
        EveryFrame,    // 매 프레임
        LateUpdate,    // LateUpdate (기본)
        FixedInterval, // 고정 시간 간격
        OnMoved        // 이동 시에만
    }
    
    private void Awake()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 발 위치 기준점 설정
        if (footPosition == null)
        {
            footPosition = transform;
        }
        
        // Grid 정보 캐시
        CacheGridInfo();
        
        // 🗺️ 아이소메트릭 데이터 초기화
        InitializeIsometricData();
    }
    
    private void OnEnable()
    {
        // 풀링에서 나올 때 즉시 계산
        UpdateSortingOrderImmediate();
    }
    
    private void Start()
    {
        // 초기 소팅 설정
        UpdateSortingOrderImmediate();
        
        if (enableDebugLogs)
        {
            Debug.Log($"🦶 [FootPositionSorter] {gameObject.name} 초기화 완료 - Order: {GetCurrentSortingOrder()}");
        }
    }
    
    private void Update()
    {
        if (updatePolicy == UpdatePolicy.EveryFrame)
        {
            UpdateSortingOrder();
        }
    }
    
    private void LateUpdate()
    {
        switch (updatePolicy)
        {
            case UpdatePolicy.LateUpdate:
                UpdateSortingOrder();
                break;
                
            case UpdatePolicy.FixedInterval:
                if (Time.time - lastUpdateTime >= updateInterval)
                {
                    UpdateSortingOrder();
                    lastUpdateTime = Time.time;
                }
                break;
                
            case UpdatePolicy.OnMoved:
                Vector3 currentPos = GetFootWorldPosition();
                if (Vector3.Distance(currentPos, lastPosition) >= moveThreshold)
                {
                    UpdateSortingOrder();
                    lastPosition = currentPos;
                }
                break;
        }
    }
    
    /// <summary>
    /// 컴포넌트 초기화 및 캐시
    /// </summary>
    private void InitializeComponents()
    {
        // SortingGroup 우선 (다중 스프라이트 지원)
        sortingGroup = GetComponent<SortingGroup>();
        if (sortingGroup == null)
        {
            // SortingGroup 없으면 SpriteRenderer 사용
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // SpriteRenderer도 없으면 자동 추가
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"⚠️ [FootPositionSorter] {gameObject.name}에 SortingGroup 또는 SpriteRenderer가 없습니다!");
            }
            else
            {
                // SpriteSortPoint를 Pivot으로 설정 (권장)
                spriteRenderer.spriteSortPoint = SpriteSortPoint.Pivot;
            }
        }
    }
    
    /// <summary>
    /// Grid 정보 캐시
    /// </summary>
    private void CacheGridInfo()
    {
        if (sceneGrid == null)
        {
            sceneGrid = FindObjectOfType<Grid>();
        }
        
        gridCellSizeY = sceneGrid != null ? sceneGrid.cellSize.y : 1f;
    }
    
    /// <summary>
    /// 소팅 오더 업데이트
    /// </summary>
    private void UpdateSortingOrder()
    {
        Vector3 footWorldPos = GetFootWorldPosition();
        
        // 🗺️ 동적 높이 오프셋 계산
        int dynamicHeightOffset = heightOffset; // 기본값
        
        if (enableDynamicHeight && useIsometricData && isIsometricDataInitialized && playerClass != null)
        {
            // HeightCurve 기반 높이 계산 (점프/스킬 등에 사용)
            float heightTime = CalculateHeightTime(); // 0~1 값
            int curveHeightOffset = playerClass.CalculateHeightOffset(heightTime);
            dynamicHeightOffset += curveHeightOffset;
            
            if (enableDebugLogs && curveHeightOffset != 0)
            {
                Debug.Log($"📈 [FootPositionSorter] {gameObject.name} - 동적 높이: {curveHeightOffset} (t={heightTime:F2})");
            }
        }
        
        int newSortingOrder = IsometricSorting.CalculateSortingOrder(
            footWorldPos.y, 
            gridCellSizeY, 
            baseLayer, 
            dynamicHeightOffset
        );
        
        // 캐시된 값과 다를 때만 설정 (성능 최적화)
        if (newSortingOrder != lastSortingOrder)
        {
            SetSortingOrder(newSortingOrder);
            lastSortingOrder = newSortingOrder;
            
            if (enableDebugLogs)
            {
                IsometricSorting.LogSortingInfo(gameObject.name, footWorldPos.y, newSortingOrder, baseLayer);
            }
        }
    }
    
    /// <summary>
    /// 즉시 소팅 오더 업데이트 (OnEnable용)
    /// </summary>
    public void UpdateSortingOrderImmediate()
    {
        lastSortingOrder = int.MinValue; // 캐시 무효화
        UpdateSortingOrder();
    }
    
    /// <summary>
    /// 발 위치 월드 좌표 계산 (아이소메트릭 데이터 지원)
    /// </summary>
    /// <returns>발 위치 월드 좌표</returns>
    private Vector3 GetFootWorldPosition()
    {
        Vector3 footPos = footPosition != null ? footPosition.position : transform.position;
        
        // 🗺️ 아이소메트릭 데이터 사용 시 동적 footOffset
        Vector2 currentFootOffset = footOffset;
        if (useIsometricData && isIsometricDataInitialized && playerClass != null)
        {
            currentFootOffset = playerClass.GetFootOffset();
        }
        
        return footPos + (Vector3)currentFootOffset;
    }
    
    /// <summary>
    /// 소팅 오더 설정 (SortingGroup 우선)
    /// </summary>
    /// <param name="order">설정할 소팅 오더</param>
    private void SetSortingOrder(int order)
    {
        if (sortingGroup != null)
        {
            sortingGroup.sortingOrder = order;
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }
    
    /// <summary>
    /// 현재 소팅 오더 가져오기
    /// </summary>
    /// <returns>현재 소팅 오더</returns>
    public int GetCurrentSortingOrder()
    {
        if (sortingGroup != null)
            return sortingGroup.sortingOrder;
        else if (spriteRenderer != null)
            return spriteRenderer.sortingOrder;
        else
            return 0;
    }
    
    /// <summary>
    /// 높이 오프셋 변경 (점프/비행 등)
    /// </summary>
    /// <param name="newHeightOffset">새 높이 오프셋</param>
    public void SetHeightOffset(int newHeightOffset)
    {
        heightOffset = newHeightOffset;
        UpdateSortingOrderImmediate();
    }
    
    /// <summary>
    /// 발 위치 기준점 변경
    /// </summary>
    /// <param name="newFootPosition">새 발 위치 Transform</param>
    public void SetFootPosition(Transform newFootPosition)
    {
        footPosition = newFootPosition;
        UpdateSortingOrderImmediate();
    }
    
    /// <summary>
    /// 공통 초기화 함수 (전투 시스템 재사용용)
    /// </summary>
    public void ResetSorting()
    {
        heightOffset = 0;
        lastSortingOrder = int.MinValue;
        UpdateSortingOrderImmediate();
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (showDebugGizmo)
        {
            Vector3 footPos = GetFootWorldPosition();
            
            // 발 위치 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(footPos, 0.1f);
            
            // 소팅 오더 라벨 표시
            UnityEditor.Handles.Label(
                footPos + Vector3.up * 0.5f, 
                $"Order: {GetCurrentSortingOrder()}\nY: {footPos.y:F2}"
            );
        }
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateSortingOrderImmediate();
        }
    }
    #endif
    
    /// <summary>
    /// 아이소메트릭 데이터 초기화
    /// </summary>
    private void InitializeIsometricData()
    {
        if (!useIsometricData)
        {
            if (enableDebugLogs)
                Debug.Log($"🗺️ [FootPositionSorter] {gameObject.name} - 아이소메트릭 데이터 사용 안함");
            return;
        }
        
        // Assasin 또는 Warrior 컴포넌트 찾기
        playerClass = GetComponent<Assasin>();
        if (playerClass == null)
            playerClass = GetComponent<Warrior>();
        
        if (playerClass != null)
        {
            // ScriptableObject에서 footOffset 가져오기
            Vector2 dataFootOffset = playerClass.GetFootOffset();
            
            // Inspector 값과 다르면 업데이트
            if (footOffset != dataFootOffset)
            {
                footOffset = dataFootOffset;
                
                if (enableDebugLogs)
                    Debug.Log($"🗺️ [FootPositionSorter] {gameObject.name} - FootOffset 업데이트: {footOffset}");
            }
            
            isIsometricDataInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log($"✅ [FootPositionSorter] {gameObject.name} - 아이소메트릭 데이터 연동 성공: {playerClass.ClassName}");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"⚠️ [FootPositionSorter] {gameObject.name} - 플레이어 클래스 없음, 기본값 사용");
        }
    }
    
    /// <summary>
    /// HeightCurve용 시간 값 계산 (0~1)
    /// 점프, 스킬, 애니메이션 등에서 사용할 높이 시간
    /// </summary>
    private float CalculateHeightTime()
    {
        // 기본 구현: 이동 속도 기반으로 높이 시간 계산
        if (transform.position != lastPosition)
        {
            // 이동 중일 때는 기본 높이 (0.0)
            return 0f;
        }
        
        // 정지 상태일 때는 기본 높이 (0.0)  
        // TODO: 점프/스킬 상태 감지 시 동적으로 변경
        return 0f;
    }
    
    /// <summary>
    /// 외부에서 높이 시간 설정 (점프/스킬용)
    /// </summary>
    /// <param name="heightTime">높이 곡선 시간 (0~1)</param>
    public void SetHeightTime(float heightTime)
    {
        if (enableDynamicHeight && useIsometricData && playerClass != null)
        {
            int newHeightOffset = playerClass.CalculateHeightOffset(heightTime);
            SetHeightOffset(newHeightOffset);
        }
    }
}
