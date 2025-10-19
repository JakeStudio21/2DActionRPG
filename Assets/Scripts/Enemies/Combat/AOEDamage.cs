using UnityEngine;

/// <summary>
/// AOE 이펙트 프리팹용 데미지 처리 스크립트
/// ⭐ [New System] 다양한 모양의 AOE 데미지 영역 지원
/// </summary>
public class AOEDamage : MonoBehaviour
{
    #region Inspector 설정
    
    [Header("AOE Damage Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer
    [SerializeField] private AOEShapeType aoeShape = AOEShapeType.Circle;
    
    [Header("Advanced Settings")]
    [SerializeField] private bool damageOnce = true; // 한 번만 데미지 (true) 또는 지속 데미지 (false)
    [SerializeField] private float damageInterval = 0.5f; // 지속 데미지 간격
    [SerializeField] private bool showDebugGizmos = true;
    
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackThrust = 15f; // 넉백 강도
    [SerializeField] private Transform knockbackSource; // 넉백 방향 소스 (몬스터 Transform)
    
    #endregion
    
    #region Private Fields
    
    private Collider2D aoeCollider;
    private bool hasDamaged = false;
    private float lastDamageTime = 0f;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        // 콜라이더 설정
        SetupCollider();
    }
    
    private void OnEnable()
    {
        // 풀링 시스템 대응: 활성화될 때마다 초기화
        hasDamaged = false;
        lastDamageTime = 0f;
    }
    
    private void Start()
    {
        // 초기 설정
        ConfigureAOECollider();
    }
    
    private void Update()
    {
        // 지속 데미지 처리
        if (!damageOnce && Time.time - lastDamageTime >= damageInterval)
        {
            ProcessDamage();
            lastDamageTime = Time.time;
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 데미지 설정
    /// </summary>
    public void SetDamage(int newDamage)
    {
        damageAmount = newDamage;
        Debug.Log($"[AOEDamage] 데미지 설정: {damageAmount}");
    }
    
    /// <summary>
    /// AOE 모양 설정
    /// </summary>
    public void SetAOEShape(AOEShapeType newShape)
    {
        aoeShape = newShape;
        ConfigureAOECollider();
        Debug.Log($"[AOEDamage] AOE 모양 설정: {aoeShape}");
    }
    
    
    /// <summary>
    /// 넉백 강도 설정
    /// </summary>
    public void SetKnockbackThrust(float newThrust)
    {
        knockbackThrust = newThrust;
        Debug.Log($"[AOEDamage] 넉백 강도 설정: {knockbackThrust}");
    }
    
    /// <summary>
    /// 넉백 방향 소스 설정
    /// </summary>
    public void SetKnockbackSource(Transform source)
    {
        knockbackSource = source;
        Debug.Log($"[AOEDamage] 넉백 소스 설정: {(source != null ? source.name : "없음")}");
    }
    
    /// <summary>
    /// 데미지 처리 활성화 (외부에서 호출)
    /// </summary>
    public void EnableDamage()
    {
        if (damageOnce && !hasDamaged)
        {
            ProcessDamage();
            hasDamaged = true;
        }
        else if (!damageOnce)
        {
            ProcessDamage();
        }
    }
    
    #endregion
    
    #region Collider Setup
    
    /// <summary>
    /// 콜라이더 설정
    /// </summary>
    private void SetupCollider()
    {
        // 기존 콜라이더 제거
        if (aoeCollider != null)
        {
            DestroyImmediate(aoeCollider);
        }
        
        // 모양에 따른 콜라이더 생성
        switch (aoeShape)
        {
            case AOEShapeType.Circle:
                aoeCollider = gameObject.AddComponent<CircleCollider2D>();
                break;
                
            case AOEShapeType.Rectangle:
                aoeCollider = gameObject.AddComponent<BoxCollider2D>();
                break;
                
            case AOEShapeType.Triangle:
                // 삼각형은 복잡하므로 원형으로 대체
                aoeCollider = gameObject.AddComponent<CircleCollider2D>();
                Debug.LogWarning("[AOEDamage] 삼각형은 원형으로 대체됩니다. 삼각형 콜라이더는 별도 구현이 필요합니다.");
                break;
        }
        
        // 공통 설정
        if (aoeCollider != null)
        {
            aoeCollider.isTrigger = true;
        }
    }
    
    /// <summary>
    /// AOE 콜라이더 설정
    /// </summary>
    private void ConfigureAOECollider()
    {
        if (aoeCollider == null) return;
        
        // ⭐ 콜라이더 크기는 Inspector에서 Edit Collider로 직접 설정
        // aoeRange는 더 이상 사용하지 않음
        
        Debug.Log($"[AOEDamage] 콜라이더 설정 완료: {aoeShape} (크기는 Inspector에서 설정)");
    }
    
    #endregion
    
    #region Damage Processing
    
    /// <summary>
    /// 데미지 처리
    /// </summary>
    private void ProcessDamage()
    {
        // 콜라이더 기반 데미지 처리
        if (aoeCollider != null)
        {
            ProcessColliderDamage();
        }
        else
        {
            // 콜라이더가 없으면 범위 기반 데미지 처리
            ProcessRangeDamage();
        }
    }
    
    /// <summary>
    /// 콜라이더 기반 데미지 처리
    /// </summary>
    private void ProcessColliderDamage()
    {
        // 콜라이더와 겹치는 플레이어 찾기
        Collider2D[] playersInRange = new Collider2D[10];
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(playerLayerMask);
        filter.useTriggers = true;
        
        int playerCount = aoeCollider.OverlapCollider(filter, playersInRange);
        
        for (int i = 0; i < playerCount; i++)
        {
            Collider2D playerCollider = playersInRange[i];
            if (playerCollider.TryGetComponent(out PlayerHealth playerHealth))
            {
                // ⭐ 넉백 소스가 설정되어 있으면 해당 소스 사용, 없으면 이펙트 Transform 사용
                Transform damageSource = knockbackSource != null ? knockbackSource : transform;
                playerHealth.TakeDamage(damageAmount, damageSource);
                Debug.Log($"[AOEDamage] 플레이어에게 {damageAmount} 데미지 적용! ({playerCollider.name})");
            }
        }
    }
    
    /// <summary>
    /// 범위 기반 데미지 처리 (콜라이더 없을 때)
    /// ⭐ 이제는 콜라이더를 항상 사용하므로 거의 사용되지 않음
    /// </summary>
    private void ProcessRangeDamage()
    {
        Debug.LogWarning("[AOEDamage] ProcessRangeDamage 호출됨 - 콜라이더를 사용하는 것을 권장합니다.");
        
        // 기본 범위로 처리 (콜라이더가 없을 때만)
        float defaultRange = 5f;
        Collider2D[] playersInRange = Physics2D.OverlapCircleAll(transform.position, defaultRange, playerLayerMask);
        
        foreach (Collider2D playerCollider in playersInRange)
        {
            if (playerCollider.TryGetComponent(out PlayerHealth playerHealth))
            {
                // ⭐ 넉백 소스가 설정되어 있으면 해당 소스 사용, 없으면 이펙트 Transform 사용
                Transform damageSource = knockbackSource != null ? knockbackSource : transform;
                playerHealth.TakeDamage(damageAmount, damageSource);
                Debug.Log($"[AOEDamage] 플레이어에게 {damageAmount} 데미지 적용! ({playerCollider.name})");
            }
        }
    }
    
    #endregion
    
    #region Collision Detection
    
    /// <summary>
    /// 트리거 진입 시 데미지 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (damageOnce && hasDamaged) return;
        
        // 플레이어 Layer Mask 체크
        if (((1 << other.gameObject.layer) & playerLayerMask) != 0)
        {
            if (other.TryGetComponent(out PlayerHealth playerHealth))
            {
                // ⭐ 넉백 소스가 설정되어 있으면 해당 소스 사용, 없으면 이펙트 Transform 사용
                Transform damageSource = knockbackSource != null ? knockbackSource : transform;
                playerHealth.TakeDamage(damageAmount, damageSource);
                Debug.Log($"[AOEDamage] 플레이어에게 {damageAmount} 데미지를 입혔습니다. ({other.name})");
                
                if (damageOnce)
                {
                    hasDamaged = true;
                }
            }
        }
    }
    
    /// <summary>
    /// 트리거 내부에서 지속 데미지 처리
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        if (damageOnce) return;
        
        // 지속 데미지 간격 체크
        if (Time.time - lastDamageTime >= damageInterval)
        {
            if (((1 << other.gameObject.layer) & playerLayerMask) != 0)
            {
                if (other.TryGetComponent(out PlayerHealth playerHealth))
                {
                    // ⭐ 넉백 소스가 설정되어 있으면 해당 소스 사용, 없으면 이펙트 Transform 사용
                    Transform damageSource = knockbackSource != null ? knockbackSource : transform;
                    playerHealth.TakeDamage(damageAmount, damageSource);
                    Debug.Log($"[AOEDamage] 지속 데미지: {damageAmount} ({other.name})");
                    lastDamageTime = Time.time;
                }
            }
        }
    }
    
    #endregion
    
    #region Debug 및 시각화
    
    /// <summary>
    /// AOE 범위 시각화 (에디터에서만)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // ⭐ 콜라이더가 있으면 콜라이더 크기로 표시
        if (aoeCollider != null)
        {
            Gizmos.color = Color.red;
            
            if (aoeCollider is CircleCollider2D circleCollider)
            {
                Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
            }
            else if (aoeCollider is BoxCollider2D boxCollider)
            {
                Gizmos.DrawWireCube(transform.position, boxCollider.size);
            }
            else if (aoeCollider is PolygonCollider2D polygonCollider)
            {
                // PolygonCollider2D는 복잡하므로 중심점만 표시
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
        else
        {
            // 콜라이더가 없으면 기본 범위 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 5f);
        }
        
        // 데미지 정보 표시
        Gizmos.color = Color.white;
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"Damage: {damageAmount}\nShape: {aoeShape}\nCollider: {(aoeCollider != null ? aoeCollider.GetType().Name : "없음")}");
        #endif
    }
    
    /// <summary>
    /// AOE 데미지 디버그 정보
    /// </summary>
    [ContextMenu("Debug AOE Damage Info")]
    public void DebugAOEDamageInfo()
    {
        string info = $"=== AOEDamage {gameObject.name} ===\n";
        info += $"Damage Amount: {damageAmount}\n";
        info += $"AOE Shape: {aoeShape}\n";
        info += $"Damage Once: {damageOnce}\n";
        info += $"Damage Interval: {damageInterval:F1}초\n";
        info += $"Has Damaged: {hasDamaged}\n";
        info += $"Collider: {(aoeCollider != null ? aoeCollider.GetType().Name : "없음")}\n";
        
        Debug.Log(info);
    }
    
    #endregion
}
