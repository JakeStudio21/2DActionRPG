using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ⭐ AOE 시전자 타입 (CueSystem.ActorType과 구분)
/// </summary>
public enum AOECasterType
{
    Player,    // 플레이어 (Player → Enemy 데미지)
    Enemy      // 적 (Enemy → Player 데미지)
}

/// <summary>
/// ⭐ AOE 데미지 정책
/// </summary>
public enum AOEDamagePolicy
{
    Once,      // 즉시 1회 판정 (기존 방식, 기본값)
    Window,    // 지속 시간 동안 1회만 (보스 AOE용)
    Tick       // 일정 간격으로 반복 (DoT용)
}

/// <summary>
/// AOE 데미지 판정 전용 컴포넌트
/// Telegraph와 분리하여 순수 데미지 판정만 담당
/// Origin과 Center를 분리하여 중심 확장형/전방 확장형 스킬 지원
/// </summary>
public class DamageArea : MonoBehaviour
{
    [Header("🎯 AOE 설정")]
    [SerializeField] private AOEShapeType aoeShape = AOEShapeType.Circle;
    [SerializeField] private AOECenterMode centerMode = AOECenterMode.Centered;
    
    [Header("📐 기준 크기 (스케일 = 1.0 기준)")]
    [SerializeField] private float baseRadius = 1.0f;          // 원형/부채꼴 기준 반경
    [SerializeField] private Vector2 baseSize = Vector2.one;    // 사각형 기준 크기 (1x1)
    [SerializeField] private float baseAngle = 60f;            // 부채꼴 기준 각도 (60도)
    
    [Header("📍 위치 설정")]
    [SerializeField] private Vector3 origin;                    // Origin (Cast Point / Spawn Point)
    [SerializeField] private Vector3 forward;                   // Forward 방향 (정규화됨)
    [SerializeField] private float centerOffset = 0f;           // Forward-anchored 모드 시 오프셋 거리
    
    [Header("⚙️ 스케일 및 데미지")]
    [SerializeField] private float scaleMultiplier = 1.0f;      // 크기 스케일 배율
    [SerializeField] private float damageMultiplier = 1.0f;     // 데미지 배율
    [SerializeField] private int baseDamage = 10;               // 기본 데미지
    
    [Header("🎯 타겟 설정")]
    [SerializeField] private LayerMask targetLayerMask;         // 데미지 대상 레이어
    
    [Header("🧱 벽 충돌 설정")]
    [SerializeField] private bool checkWallBlocking = true;     // AOE도 벽 차단 체크
    [SerializeField] private LayerMask wallLayer;               // Wall Layer (런타임 자동 설정)
    
    [Header("🎨 이펙트")]
    [SerializeField] private GameObject hitEffect;              // 피격 이펙트
    [SerializeField] private float shakeIntensity = 0f;         // 스크린 셰이크 강도
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = false;  // ⭐ 릴리즈용: false (테스트 시 true로 변경)
    [SerializeField] private bool enableDebugGizmos = true;
    [SerializeField] private bool showGizmosInPlayMode = false;  // ⭐ 기본값: false (필요시 Inspector에서 켜기)
    
    [Header("⚡ 데미지 정책 (Phase 1)")]
    [Tooltip("Once: 즉시 1회 판정 (기본값), Window: 지속시간 1회, Tick: 반복 판정")]
    [SerializeField] private AOEDamagePolicy damagePolicy = AOEDamagePolicy.Once;
    
    [Tooltip("AOE 시전자 (Player/Enemy, 자동 설정됨)")]
    [SerializeField] private AOECasterType casterType = AOECasterType.Enemy;
    
    [Tooltip("Window 모드: 지속 시간 (초)")]
    [SerializeField] private float windowDuration = 0.5f;
    
    [Tooltip("Tick 모드: 데미지 간격 (초)")]
    [SerializeField] private float tickInterval = 0.2f;
    
    // 내부 상태
    private SkillData skillData;
    private BaseEnemy baseEnemy;
    private Vector3 calculatedCenter;  // 계산된 Center 위치
    private float spawnTime;  // 생성 시간 (Gizmos 표시 시간 제어용)
    private HashSet<Collider2D> hitTargets = new HashSet<Collider2D>(); // Once/Window용 중복 방지
    
    /// <summary>
    /// 🧱 Awake: wallLayer 자동 초기화
    /// </summary>
    private void Awake()
    {
        // wallLayer가 설정되지 않았으면 자동으로 Wall Layer 찾기
        if (wallLayer == 0)
        {
            int wallLayerIndex = LayerMask.NameToLayer("Wall");
            if (wallLayerIndex != -1)
            {
                wallLayer = 1 << wallLayerIndex; // LayerMask 값으로 변환
                if (enableDebugLogs)
                    Debug.Log($"✅ [DamageArea] wallLayer 자동 설정: Wall (Layer {wallLayerIndex})");
            }
            else
            {
                Debug.LogWarning("⚠️ [DamageArea] 'Wall' Layer가 존재하지 않습니다! 벽 차단 기능이 작동하지 않습니다.");
            }
        }
    }
    
    
    /// <summary>
    /// ⭐ Phase 4: DamageArea 초기화 (보스용 - 정책 파라미터 추가)
    /// </summary>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="skillEntry">보스 스킬 엔트리 (페이즈 스케일 포함)</param>
    /// <param name="origin">Origin 위치 (보스 중심 위치)</param>
    /// <param name="forward">Forward 방향 (정규화된 벡터)</param>
    /// <param name="enemy">BaseEnemy 참조 (데미지 계산용)</param>
    /// <param name="policy">데미지 정책 (기본값: Once)</param>
    public void Initialize(SkillData skillData, BossSkillEntry skillEntry, Vector3 origin, Vector3 forward, BaseEnemy enemy, AOEDamagePolicy policy = AOEDamagePolicy.Once)
    {
        if (skillData == null || skillEntry == null)
        {
            Debug.LogError("[DamageArea] SkillData 또는 BossSkillEntry가 null!");
            return;
        }
        
        // ⭐ 정책 설정 (InitializeCommon 호출 전에 설정)
        damagePolicy = policy;
        
        // ⭐ 공통 초기화 로직 호출 (scaleMultiplier는 BossSkillEntry에서 추출)
        InitializeCommon(skillData, origin, forward, enemy, skillEntry.skillScaleMultiplier);
    }
    
    /// <summary>
    /// ⭐ Phase 4: DamageArea 초기화 (엘리트용 - 정책 파라미터 추가)
    /// </summary>
    /// <param name="skillData">스킬 데이터</param>
    /// <param name="origin">Origin 위치 (엘리트 중심 위치)</param>
    /// <param name="forward">Forward 방향 (정규화된 벡터)</param>
    /// <param name="enemy">BaseEnemy 참조 (데미지 계산용)</param>
    /// <param name="scaleMultiplier">스케일 배율 (기본값 1.0)</param>
    /// <param name="policy">데미지 정책 (기본값: Once)</param>
    public void Initialize(SkillData skillData, Vector3 origin, Vector3 forward, BaseEnemy enemy, float scaleMultiplier = 1.0f, AOEDamagePolicy policy = AOEDamagePolicy.Once)
    {
        if (skillData == null)
        {
            Debug.LogError("[DamageArea] SkillData가 null!");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [DamageArea] 엘리트용 초기화 시작 - {skillData.SkillName}, Scale: {scaleMultiplier}, Policy: {policy}");
        }
        
        // ⭐ 정책 설정 (InitializeCommon 호출 전에 설정)
        damagePolicy = policy;
        
        // ⭐ 공통 초기화 로직 호출
        InitializeCommon(skillData, origin, forward, enemy, scaleMultiplier);
    }
    
    /// <summary>
    /// ⭐ Phase 1: 공통 초기화 로직 (코드 중복 제거)
    /// </summary>
    private void InitializeCommon(SkillData skillData, Vector3 origin, Vector3 forward, BaseEnemy enemy, float scaleMultiplier)
    {
        // 데이터 저장
        this.skillData = skillData;
        this.origin = origin;
        this.forward = forward.normalized;
        this.baseEnemy = enemy;
        this.scaleMultiplier = scaleMultiplier;
        
        // SkillData에서 AOE 설정 가져오기
        aoeShape = skillData.AoeShape;
        centerMode = skillData.AoeCenterMode;
        
        // 기준 크기 설정
        baseRadius = skillData.AoeRadius;
        baseSize = skillData.AoeSize;
        baseAngle = skillData.AoeAngle;
        
        // Center Offset 설정
        centerOffset = skillData.AoeCenterOffset;
        
        // Fallback: AoeOffset 사용 (호환성 유지)
        if (centerOffset <= 0f && centerMode == AOECenterMode.ForwardAnchored)
        {
            Vector2 aoeOffset = skillData.AoeOffset;
            centerOffset = aoeOffset.magnitude;
            
            if (enableDebugLogs && centerOffset > 0f)
            {
                Debug.Log($"[DamageArea] AoeCenterOffset이 0이므로 AoeOffset.magnitude({centerOffset}) 사용");
            }
        }
        
        // 데미지 배율 설정
        damageMultiplier = skillData.DamageMultiplier;
        
        // 기본 데미지 획득
        baseDamage = GetBaseDamage(enemy);
        
        // 타겟 레이어 설정
        int playerLayer = LayerMask.GetMask("Player");
        if (playerLayer == 0)
        {
            Debug.LogError("[DamageArea] 'Player' Layer가 존재하지 않습니다!");
            targetLayerMask = LayerMask.GetMask("Default");
        }
        else
        {
            targetLayerMask = playerLayer;
        }
        
        // 이펙트 설정
        hitEffect = skillData.HitEffect;
        shakeIntensity = skillData.ShakeIntensity;
        
        // ⭐ Phase 3: SkillData에서 Window/Tick 정책 정보 복사
        // (damagePolicy는 Initialize()에서 이미 설정되었지만, duration/interval은 여기서 설정)
        windowDuration = skillData.AoeDuration;
        tickInterval = skillData.AoeTickInterval;
        
        // 생성 시간 기록 (Gizmos 표시 시간 제어용)
        spawnTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [DamageArea] 초기화 완료:");
            Debug.Log($"   - Skill: {skillData.SkillName}");
            Debug.Log($"   - Origin: {origin}");
            Debug.Log($"   - Forward: {forward}");
            Debug.Log($"   - Shape: {aoeShape}, Center Mode: {centerMode}");
            Debug.Log($"   - Base Size: Radius={baseRadius}, Size={baseSize}, Angle={baseAngle}");
            Debug.Log($"   - Scale: {scaleMultiplier}x");
            Debug.Log($"   - Center Offset: {centerOffset}");
            Debug.Log($"   - Damage: Base={baseDamage}, Multiplier={damageMultiplier}x");
            Debug.Log($"   - Policy: {damagePolicy}"); // ⭐ Phase 4: 정책 로그 추가
        }
        
        // ⭐ Phase 4: damagePolicy는 Initialize()에서 이미 설정됨 (중복 설정 제거)
        // casterType은 여기서 설정
        casterType = AOECasterType.Enemy;
        
        // ⭐ 데미지 정책 실행
        ExecuteDamagePolicy();
    }
    
    /// <summary>
    /// 데미지 판정 및 적용
    /// </summary>
    public void PerformDamage()
    {
        // ⭐ Phase C: skillData는 선택적 (오버로드된 InitializeForPlayer는 null 가능)
        // baseDamage, aoeShape 등이 직접 설정되어 있으면 작동 가능
        
        // Center 계산 (Gizmos 표시를 위해 저장)
        calculatedCenter = CalculateCenter();
        
        // Transform 위치를 Center로 설정 (Gizmos 표시용)
        transform.position = calculatedCenter;
        
        // 형태별 Overlap 검사
        Collider2D[] hits = null;
        
        switch (aoeShape)
        {
            case AOEShapeType.Circle:
                hits = OverlapCircle(calculatedCenter);
                break;
                
            case AOEShapeType.Triangle: // Fan (부채꼴)
                hits = OverlapFan(calculatedCenter, forward);
                break;
                
            case AOEShapeType.Rectangle:
                hits = OverlapRectangle(calculatedCenter, forward);
                break;
                
            default:
                Debug.LogWarning($"[DamageArea] 지원하지 않는 AOE 형태: {aoeShape}");
                return;
        }
        
        if (hits == null || hits.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[DamageArea] 데미지 판정: 히트 없음");
            }
            return;
        }
        
        // 피격 대상에게 데미지 적용
        if (enableDebugLogs)
        {
            Debug.Log($"💥 [DamageArea] 데미지 판정 실행:");
            Debug.Log($"   - 히트 수: {hits.Length}");
            Debug.Log($"   - Center: {calculatedCenter}");
            Debug.Log($"   - Shape: {aoeShape}");
        }
        
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();
        int blockedCount = 0; // 🧱 벽에 막힌 타겟 수
        int hitCount = 0;     // ✅ 실제 타격 수
        
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            
            GameObject target = hit.gameObject;
            
            // 중복 처리 방지
            if (processedTargets.Contains(target))
                continue;
            
            processedTargets.Add(target);
            
            // 🧱 벽 차단 체크
            if (checkWallBlocking && IsBlockedByWall(hit.transform))
            {
                blockedCount++;
                if (enableDebugLogs)
                    Debug.Log($"🚫 [DamageArea] {hit.name} - 벽에 막힘");
                continue; // 이 타겟은 스킵
            }
            
            // ⭐ ApplyDamageToTarget 호출 (Player/Enemy 자동 판별)
            ApplyDamageToTarget(hit);
            hitCount++;
        }
        
        // 🧱 벽 차단 통계 로그
        if (enableDebugLogs && (blockedCount > 0 || hitCount > 0))
        {
            Debug.Log($"📊 [DamageArea] 결과: {hitCount}개 타격, {blockedCount}개 벽에 막힘");
        }
        
        
        // 스크린 셰이크 (한 번만)
        if (shakeIntensity > 0f && processedTargets.Count > 0)
        {
            // TODO: 스크린 셰이크 구현
            if (enableDebugLogs)
            {
                Debug.Log($"   - 스크린 셰이크: {shakeIntensity}");
            }
        }
    }
    
    #region ⭐ Phase 1: 플레이어 스킬 지원 + 정책 시스템
    
    /// <summary>
    /// ⭐ Phase 1: 플레이어용 초기화 (새로 추가)
    /// </summary>
    public void InitializeForPlayer(SkillData skillData, Vector3 origin, Vector3 forward, int playerBaseDamage, float scaleMultiplier = 1.0f, AOEDamagePolicy policy = AOEDamagePolicy.Once)
    {
        if (skillData == null)
        {
            Debug.LogError("[DamageArea] SkillData가 null!");
            return;
        }
        
        // 기본 설정
        this.skillData = skillData;
        this.origin = origin;
        this.forward = forward.normalized;
        this.scaleMultiplier = scaleMultiplier;
        this.baseDamage = playerBaseDamage;
        
        // SkillData 복사
        aoeShape = skillData.AoeShape;
        centerMode = skillData.AoeCenterMode;
        baseRadius = skillData.AoeRadius;
        baseSize = skillData.AoeSize;
        baseAngle = skillData.AoeAngle;
        centerOffset = skillData.AoeCenterOffset;
        damageMultiplier = skillData.DamageMultiplier;
        
        // 이펙트 설정
        hitEffect = skillData.HitEffect;
        shakeIntensity = skillData.ShakeIntensity;
        
        // ⭐ 정책 설정
        damagePolicy = policy;
        casterType = AOECasterType.Player;
        
        // ⭐ 타겟 레이어: Enemy
        int enemyLayer = LayerMask.GetMask("Enemy");
        if (enemyLayer == 0)
        {
            Debug.LogError("[DamageArea] 'Enemy' Layer가 존재하지 않습니다!");
            targetLayerMask = LayerMask.GetMask("Default");
        }
        else
        {
            targetLayerMask = enemyLayer;
        }
        
        // 생성 시간 기록
        spawnTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [DamageArea] 플레이어용 초기화 완료:");
            Debug.Log($"   - Skill: {skillData.SkillName}");
            Debug.Log($"   - Policy: {policy}");
            Debug.Log($"   - Base Damage: {playerBaseDamage}");
            Debug.Log($"   - Damage Multiplier: {damageMultiplier}x");
        }
        
        // ⭐ 정책 실행
        ExecuteDamagePolicy();
    }
    
    /// <summary>
    /// ⭐ Phase C-1: 플레이어용 초기화 (오버로드 - 개별 파라미터)
    /// SkillData 타입 제약 없이 사용 가능 (AssasinSkillData, WarriorSkillData 등)
    /// </summary>
    public void InitializeForPlayer(
        AOEShapeType shape,
        Vector3 origin,
        Vector3 forward,
        float radius,
        Vector2 size,
        float angle,
        int playerBaseDamage,
        float damageMultiplier = 1.0f,
        float scaleMultiplier = 1.0f,
        AOEDamagePolicy policy = AOEDamagePolicy.Once,
        GameObject hitEffectPrefab = null)
    {
        // 개별 파라미터로 직접 설정
        this.aoeShape = shape;
        this.origin = origin;
        this.forward = forward.normalized;
        this.baseRadius = radius;
        this.baseSize = size;
        this.baseAngle = angle;
        this.baseDamage = playerBaseDamage;
        this.damageMultiplier = damageMultiplier;
        this.scaleMultiplier = scaleMultiplier;
        this.hitEffect = hitEffectPrefab;
        
        // ⭐ Rectangle AOE 자동 처리: 플레이어 앞쪽으로 생성
        if (shape == AOEShapeType.Rectangle)
        {
            this.centerMode = AOECenterMode.ForwardAnchored;
            this.centerOffset = size.x / 2f; // 사각형 가로 길이의 절반
            
            if (enableDebugLogs)
                Debug.Log($"🎯 [DamageArea] Rectangle 감지 → ForwardAnchored 모드 (Offset: {this.centerOffset})");
        }
        else
        {
            this.centerMode = AOECenterMode.Centered; // Circle/Triangle은 중심 기준
            this.centerOffset = 0f;
        }
        
        this.shakeIntensity = 0f;
        this.skillData = null; // SkillData 없음
        
        // ⭐ 정책 설정
        damagePolicy = policy;
        casterType = AOECasterType.Player;
        
        // ⭐ 타겟 레이어: Enemy
        int enemyLayer = LayerMask.GetMask("Enemy");
        
        if (enemyLayer == 0)
        {
            Debug.LogError("[DamageArea] 'Enemy' Layer가 존재하지 않습니다!");
            targetLayerMask = LayerMask.GetMask("Default");
        }
        else
        {
            targetLayerMask = enemyLayer;
        }
        
        // 생성 시간 기록
        spawnTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [DamageArea] 플레이어 {shape} AOE 생성 (Policy: {policy}, Damage: {playerBaseDamage})");
        }
        
        // ⭐ 정책 실행
        ExecuteDamagePolicy();
    }
    
    /// <summary>
    /// ⭐ Phase 1: 데미지 정책 실행
    /// </summary>
    private void ExecuteDamagePolicy()
    {
        switch (damagePolicy)
        {
            case AOEDamagePolicy.Once:
                PerformDamage(); // 기존 방식 그대로
                if (enableDebugLogs)
                    Debug.Log($"[DamageArea] Policy: Once - 즉시 1회 판정");
                break;
                
            case AOEDamagePolicy.Window:
                StartCoroutine(WindowDamageRoutine());
                if (enableDebugLogs)
                    Debug.Log($"[DamageArea] Policy: Window - {windowDuration}초 동안 1회만");
                break;
                
            case AOEDamagePolicy.Tick:
                StartCoroutine(TickDamageRoutine());
                if (enableDebugLogs)
                    Debug.Log($"[DamageArea] Policy: Tick - {tickInterval}초마다 반복");
                break;
        }
    }
    
    /// <summary>
    /// ⭐ Window 정책: 지속시간 동안 1회만 판정
    /// </summary>
    private IEnumerator WindowDamageRoutine()
    {
        hitTargets.Clear();
        float elapsed = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[DamageArea] Window 시작 - {windowDuration}초");
        
        while (elapsed < windowDuration)
        {
            PerformWindowDamage();
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[DamageArea] Window 종료 - 총 타격: {hitTargets.Count}명");
        }
    }
    
    /// <summary>
    /// ⭐ Tick 정책: 일정 간격으로 반복 판정
    /// </summary>
    private IEnumerator TickDamageRoutine()
    {
        int tickCount = 0;
        float elapsed = 0f;
        
        if (enableDebugLogs)
            Debug.Log($"[DamageArea] Tick 시작 - {tickInterval}초마다, 총 {windowDuration}초");
        
        while (elapsed < windowDuration)
        {
            PerformDamage(); // 매 틱마다 판정
            tickCount++;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[DamageArea] Tick #{tickCount} - {elapsed:F2}초");
            }
            
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[DamageArea] Tick 종료 - 총 {tickCount}회 판정");
        }
    }
    
    /// <summary>
    /// ⭐ Window 모드 전용 데미지 (1회만)
    /// </summary>
    private void PerformWindowDamage()
    {
        calculatedCenter = CalculateCenter();
        Collider2D[] hits = DetectTargets();
        
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hitTargets.Contains(hit)) continue; // 이미 맞은 적
            
            ApplyDamageToTarget(hit);
            hitTargets.Add(hit);
        }
    }
    
    /// <summary>
    /// ⭐ 타겟 감지 (형태별 Overlap)
    /// </summary>
    private Collider2D[] DetectTargets()
    {
        switch (aoeShape)
        {
            case AOEShapeType.Circle:
                return OverlapCircle(calculatedCenter);
                
            case AOEShapeType.Triangle: // Fan
                return OverlapFan(calculatedCenter, forward);
                
            case AOEShapeType.Rectangle:
                return OverlapRectangle(calculatedCenter, forward);
                
            default:
                Debug.LogWarning($"[DamageArea] 지원하지 않는 AOE 형태: {aoeShape}");
                return new Collider2D[0];
        }
    }
    
    /// <summary>
    /// ⭐ 타겟에게 데미지 적용 (Player/Enemy 자동 판별)
    /// </summary>
    private void ApplyDamageToTarget(Collider2D hit)
    {
        int finalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
        
        Debug.LogWarning($"🔥 [DamageArea] ApplyDamageToTarget 호출: {hit.name}, CasterType={casterType}, Damage={finalDamage}");
        
        if (casterType == AOECasterType.Enemy)
        {
            // Enemy → Player
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.LogWarning($"✅ [DamageArea] PlayerHealth 발견! 데미지 적용: {finalDamage}");
                
                playerHealth.TakeDamage(finalDamage, baseEnemy != null ? baseEnemy.transform : transform);
                
                if (hitEffect != null)
                {
                    GameObject effect = Instantiate(hitEffect, hit.transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }
            }
            else
            {
                Debug.LogError($"❌ [DamageArea] PlayerHealth 없음! Target: {hit.name}");
            }
        }
        else if (casterType == AOECasterType.Player)
        {
            // Player → Enemy
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                Debug.LogWarning($"✅ [DamageArea] EnemyHealth 발견! 데미지 적용: {finalDamage}");
                
                // ⭐ EnemyHealth.TakeDamage는 1개 인자만 받음
                enemyHealth.TakeDamage(finalDamage);
                
                if (hitEffect != null)
                {
                    GameObject effect = Instantiate(hitEffect, hit.transform.position, Quaternion.identity);
                    Destroy(effect, 2f);
                }
            }
            else
            {
                Debug.LogError($"❌ [DamageArea] EnemyHealth 없음! Target: {hit.name}");
            }
        }
    }
    
    #endregion
    
    /// <summary>
    /// 계산된 Center 위치 반환 (Phase 4: VFX 생성 위치 동기화용)
    /// PerformDamage() 호출 전에도 사용 가능하도록 즉시 계산
    /// </summary>
    public Vector3 GetCalculatedCenter()
    {
        return CalculateCenter();
    }
    
    /// <summary>
    /// ⭐ 이펙트 생성 위치 계산 (파티클 Left Pivot 전용)
    /// 
    /// DamageArea의 Center에서 Width/2만큼 왼쪽으로 이동한 위치 반환.
    /// 파티클 이펙트는 피봇 변경이 어려워 Left Pivot을 사용하므로,
    /// Center 피봇인 DamageArea/Telegraph와 정렬하기 위해 보정 필요.
    /// 
    /// 계산 흐름:
    /// 1. CalculateCenter() = Origin + forward * centerOffset
    /// 2. Left Pivot 보정 = Center - forward * (width/2)
    /// 
    /// ⚠️ 주의: centerOffset과 width/2는 다른 값!
    /// 
    /// ✅ Center Pivot 이펙트는 GetCalculatedCenter() 사용!
    /// </summary>
    public Vector3 GetEffectSpawnPositionForLeftPivot()
    {
        Vector3 center = CalculateCenter(); // DamageArea의 진짜 Center
        
        // Rectangle만 보정 (Left Pivot → Center 맞춤)
        if (aoeShape == AOEShapeType.Rectangle)
        {
            // ⭐ Width/2만큼 왼쪽으로 이동
            float halfWidth = baseSize.x * scaleMultiplier * 0.5f;
            Vector3 correctedPosition = center - forward * halfWidth;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎨 [DamageArea] Left Pivot 이펙트 위치 보정:");
                Debug.Log($"   - Center: {center}");
                Debug.Log($"   - Width: {baseSize.x * scaleMultiplier}, Half: {halfWidth}");
                Debug.Log($"   - Corrected Position: {correctedPosition}");
                Debug.Log($"   - Offset: {halfWidth} (왼쪽으로 이동)");
            }
            
            return correctedPosition;
        }
        
        // Circle/Triangle은 Center Pivot 가정 (보정 불필요)
        if (enableDebugLogs && (aoeShape == AOEShapeType.Circle || aoeShape == AOEShapeType.Triangle))
        {
            Debug.Log($"🎨 [DamageArea] {aoeShape} - Center Pivot 이펙트, 보정 불필요");
        }
        
        return center;
    }
    
    /// <summary>
    /// Center 위치 계산
    /// </summary>
    private Vector3 CalculateCenter()
    {
        if (centerMode == AOECenterMode.Centered)
        {
            return origin;  // 오프셋 0
        }
        else // ForwardAnchored
        {
            float offset = centerOffset * scaleMultiplier;
            Vector3 calculatedCenter = origin + forward * offset;
            
            if (enableDebugLogs)
                Debug.Log($"📍 [DamageArea] ForwardAnchored: Origin={origin}, Offset={offset}, Center={calculatedCenter}");
            
            return calculatedCenter;
        }
    }
    
    /// <summary>
    /// 원형 Overlap 검사
    /// </summary>
    private Collider2D[] OverlapCircle(Vector3 center)
    {
        float finalRadius = baseRadius * scaleMultiplier;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, finalRadius, targetLayerMask);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[DamageArea] Circle Overlap: Center={center}, Radius={finalRadius}, Hits={hits.Length}");
        }
        
        return hits;
    }
    
    /// <summary>
    /// 부채꼴 (Fan) Overlap 검사
    /// </summary>
    private Collider2D[] OverlapFan(Vector3 center, Vector3 forward)
    {
        // ⭐ 수정: baseRadius는 이미 skillData.AoeRadius 값임
        float finalRadius = baseRadius * scaleMultiplier;
        
        // ⭐ 수정: baseAngle은 이미 skillData.AoeAngle 값임
        float finalAngle = baseAngle;
        
        // 1단계: 원형으로 먼저 필터링
        Collider2D[] allHits = Physics2D.OverlapCircleAll(center, finalRadius, targetLayerMask);
        List<Collider2D> fanHits = new List<Collider2D>();
        
        // 2단계: 각도 필터링
        foreach (var hit in allHits)
        {
            Vector3 toTarget = (hit.transform.position - center).normalized;
            float dotProduct = Vector3.Dot(forward, toTarget);
            
            // Clamp to [-1, 1] for Acos
            dotProduct = Mathf.Clamp(dotProduct, -1f, 1f);
            float angleToTarget = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
            
            if (angleToTarget <= finalAngle / 2f)
            {
                fanHits.Add(hit);
            }
        }
        
        return fanHits.ToArray();
    }
    
    /// <summary>
    /// 사각형 Overlap 검사
    /// </summary>
    private Collider2D[] OverlapRectangle(Vector3 center, Vector3 forward)
    {
        // ⭐ 수정: baseSize는 이미 skillData.AoeSize 값임
        Vector2 finalSize = baseSize * scaleMultiplier;
        
        // forward 방향을 각도로 변환
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[DamageArea] Rectangle Overlap: Center={center}, Size={finalSize}, Angle={angle}°");
        }
        
        return Physics2D.OverlapBoxAll(center, finalSize, angle, targetLayerMask);
    }
    
    /// <summary>
    /// 플레이어에게 데미지 적용
    /// </summary>
    private void ApplyDamageToPlayer(GameObject player)
    {
        if (player == null) return;
        
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning($"[DamageArea] {player.name}에 PlayerHealth 컴포넌트가 없습니다!");
            return;
        }
        
        // 최종 데미지 계산
        float totalMultiplier = damageMultiplier * scaleMultiplier;
        int finalDamage = Mathf.RoundToInt(baseDamage * totalMultiplier);
        
        // 데미지 적용
        Transform damageSource = baseEnemy != null ? baseEnemy.transform : transform;
        playerHealth.TakeDamage(finalDamage, damageSource);
        
        // Hit 이펙트
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, player.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Screen Shake
        if (shakeIntensity > 0f)
        {
            TriggerScreenShake(shakeIntensity);
        }
    }
    
    /// <summary>
    /// 기본 데미지 획득 (BaseEnemy의 MeleeAttack에서)
    /// </summary>
    private int GetBaseDamage(BaseEnemy enemy)
    {
        if (enemy == null) return 10;  // 기본값
        
        var meleeAttack = enemy.GetComponent<MeleeAttack>();
        if (meleeAttack != null)
        {
            return meleeAttack.GetScaledDamage();
        }
        
        return 10;  // Fallback
    }
    
    /// <summary>
    /// Screen Shake 트리거
    /// </summary>
    private void TriggerScreenShake(float intensity)
    {
        if (ScreenShakeManager.Instance != null)
        {
            ScreenShakeManager.Instance.ShakeScreen(intensity);
        }
    }
    
    /// <summary>
    /// 디버그 Gizmos 그리기 (Scene View)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!enableDebugGizmos) return;
        
        // 플레이 모드에서 5초 경과 후 Gizmos 숨김 (디버그 시간 연장)
        if (Application.isPlaying)
        {
            if (!showGizmosInPlayMode) return;
            
            // 생성 후 5초가 지나면 Gizmos 표시 안 함
            if (spawnTime > 0f && Time.time - spawnTime > 5.0f)
            {
                return;
            }
        }
        
        // ⭐ skillData가 없어도 (플레이어용) 시각화 가능
        if (skillData == null)
        {
            DrawGizmosWithoutSkillData();
            return;
        }
        
        DrawGizmosWithSkillData();
    }
    
    /// <summary>
    /// SkillData 없이 기본 Gizmos 그리기 (플레이어용 포함)
    /// </summary>
    private void DrawGizmosWithoutSkillData()
    {
        // ⭐ Center 계산 (ForwardAnchored 적용!)
        Vector3 center = CalculateCenter();
        
        float finalRadius = baseRadius * scaleMultiplier;
        
        switch (aoeShape)
        {
            case AOEShapeType.Circle:
                // ⭐ 플레이어/적 구분 색상
                Gizmos.color = (casterType == AOECasterType.Player) 
                    ? new Color(0f, 0f, 1f, 0.4f)  // 파란색 (플레이어)
                    : new Color(1f, 0f, 0f, 0.4f); // 빨간색 (적)
                DrawCircle(center, finalRadius);
                // ⭐ 중심점 표시
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(center, 0.5f);
                break;
            case AOEShapeType.Triangle:
                // ⭐ 플레이어/적 구분 색상
                Gizmos.color = (casterType == AOECasterType.Player) 
                    ? new Color(0f, 0f, 1f, 0.4f)  // 파란색 (플레이어)
                    : new Color(1f, 0f, 0f, 0.4f); // 빨간색 (적)
                DrawFan(center, forward, finalRadius, baseAngle);
                break;
            case AOEShapeType.Rectangle:
                // ⭐ Rectangle은 항상 빨간색 (명확한 시각화)
                Gizmos.color = new Color(1f, 0f, 0f, 0.4f); // 빨간색
                DrawRotatedRectangle(center, forward, baseSize * scaleMultiplier);
                break;
        }
        
        // ⭐ Origin/Center/Forward 표시 (DrawGizmosWithSkillData()와 동일)
        // Origin 표시 (파란색)
        if (origin != Vector3.zero)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);  // 파란색
            Gizmos.DrawWireSphere(origin, 0.25f);
            Gizmos.DrawSphere(origin, 0.15f);
        }
        
        // Center 표시 (초록색)
        Gizmos.color = new Color(0f, 1f, 0f, 0.8f);  // 초록색
        Gizmos.DrawWireSphere(center, 0.25f);
        Gizmos.DrawSphere(center, 0.15f);
        
        // Forward 방향선 (노란색)
        if (forward != Vector3.zero)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);  // 노란색
            float arrowLength = 1.5f;
            Vector3 arrowEnd = center + forward * arrowLength;
            Gizmos.DrawLine(center, arrowEnd);
            
            // 화살표 끝 그리기
            Vector3 right = new Vector3(-forward.y, forward.x, 0f) * 0.2f;
            Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.3f + right);
            Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.3f - right);
        }
    }
    
    /// <summary>
    /// SkillData를 사용하여 정확한 Gizmos 그리기
    /// </summary>
    private void DrawGizmosWithSkillData()
    {
        // Center 계산 (항상 다시 계산하여 최신 값 사용)
        Vector3 center = CalculateCenter();
        
        // AOE 영역 시각화 (빨간색 반투명)
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        
        switch (aoeShape)
        {
            case AOEShapeType.Circle:
                // ⭐ 수정: baseRadius는 이미 skillData.AoeRadius 값임 (InitializeCommon에서 할당)
                float circleRadius = baseRadius * scaleMultiplier;
                DrawCircle(center, circleRadius);
                break;
                
            case AOEShapeType.Triangle: // Fan (부채꼴)
                // ⭐ 수정: baseRadius는 이미 skillData.AoeRadius 값임
                float fanRadius = baseRadius * scaleMultiplier;
                float fanAngle = baseAngle;  // ⭐ 수정: skillData.AoeAngle → baseAngle
                
                // Forward 방향이 없으면 기본값 사용
                Vector3 fanForward = forward;
                if (fanForward == Vector3.zero)
                {
                    fanForward = Vector3.right;
                }
                
                DrawFan(center, fanForward, fanRadius, fanAngle);
                break;
                
            case AOEShapeType.Rectangle:
                // ⭐ 수정: baseSize는 이미 skillData.AoeSize 값임 (InitializeCommon에서 할당)
                Vector2 rectSize = baseSize * scaleMultiplier;
                DrawRotatedRectangle(center, forward, rectSize);
                break;
        }
        
        // Origin 표시 (파란색)
        if (origin != Vector3.zero)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.8f);  // 파란색
            Gizmos.DrawWireSphere(origin, 0.25f);
            Gizmos.DrawSphere(origin, 0.15f);
        }
        
        // Center 표시 (초록색)
        Gizmos.color = new Color(0f, 1f, 0f, 0.8f);  // 초록색
        Gizmos.DrawWireSphere(center, 0.25f);
        Gizmos.DrawSphere(center, 0.15f);
        
        // Forward 방향선 (노란색)
        if (forward != Vector3.zero)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.8f);  // 노란색
            float arrowLength = 1.5f;
            Vector3 arrowEnd = center + forward * arrowLength;
            Gizmos.DrawLine(center, arrowEnd);
            
            // 화살표 끝 그리기
            Vector3 right = new Vector3(-forward.y, forward.x, 0f) * 0.2f;
            Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.3f + right);
            Gizmos.DrawLine(arrowEnd, arrowEnd - forward * 0.3f - right);
        }
        
        // Origin과 Center 연결선 (Center 모드가 ForwardAnchored일 때만)
        if (centerMode == AOECenterMode.ForwardAnchored && origin != Vector3.zero)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);  // 흰색 반투명
            Gizmos.DrawLine(origin, center);
        }
    }
    
    /// <summary>
    /// 원형 Gizmos 그리기 (정밀하게)
    /// </summary>
    private void DrawCircle(Vector3 center, float radius)
    {
        int segments = 64;  // 원의 세그먼트 수 (더 부드럽게)
        float angleStep = 360f / segments;
        
        Vector3 prevPoint = center + new Vector3(radius, 0f, 0f);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            );
            
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        
        // 반경선 하나 그리기
        Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.6f);
        Gizmos.DrawLine(center, center + Vector3.right * radius);
    }
    
    /// <summary>
    /// 부채꼴 (Fan) Gizmos 그리기
    /// </summary>
    private void DrawFan(Vector3 center, Vector3 forward, float radius, float angle)
    {
        int segments = Mathf.Max(16, Mathf.RoundToInt(angle / 5f));  // 각도에 따라 세그먼트 수 조정
        float halfAngle = angle / 2f;
        float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        
        // 부채꼴 호 그리기
        Vector3 prevPoint = center + GetDirectionFromAngle(forwardAngle - halfAngle) * radius;
        Gizmos.DrawLine(center, prevPoint);  // 중심에서 첫 번째 변
        
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = forwardAngle - halfAngle + (angle / segments) * i;
            Vector3 currentPoint = center + GetDirectionFromAngle(currentAngle) * radius;
            
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        
        Gizmos.DrawLine(center, prevPoint);  // 중심에서 두 번째 변
        
        // Forward 방향선 강조
        Gizmos.color = new Color(1f, 0.7f, 0f, 0.8f);  // 주황색
        Gizmos.DrawLine(center, center + forward * radius);
    }
    
    /// <summary>
    /// 회전된 사각형 Gizmos 그리기
    /// </summary>
    private void DrawRotatedRectangle(Vector3 center, Vector3 forward, Vector2 size)
    {
        float angle = Mathf.Atan2(forward.y, forward.x);
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        
        float halfWidth = size.x / 2f;
        float halfHeight = size.y / 2f;
        
        // 로컬 좌표계의 네 모서리
        Vector3[] corners = new Vector3[]
        {
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f)
        };
        
        // 회전 적용하여 월드 좌표로 변환
        for (int i = 0; i < corners.Length; i++)
        {
            float x = corners[i].x * cos - corners[i].y * sin;
            float y = corners[i].x * sin + corners[i].y * cos;
            corners[i] = center + new Vector3(x, y, 0f);
        }
        
        // 네 변 그리기
        for (int i = 0; i < corners.Length; i++)
        {
            int nextIndex = (i + 1) % corners.Length;
            Gizmos.DrawLine(corners[i], corners[nextIndex]);
        }
        
        // Forward 방향선 강조
        Gizmos.color = new Color(1f, 0.7f, 0f, 0.8f);  // 주황색
        float forwardLength = Mathf.Max(size.x, size.y) * 0.6f;
        Gizmos.DrawLine(center, center + forward * forwardLength);
        
        // ⭐ 대각선 제거: 면이 뒤집어진 것처럼 보이는 혼란 방지
        // Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        // Gizmos.DrawLine(corners[0], corners[2]);
        // Gizmos.DrawLine(corners[1], corners[3]);
    }
    
    /// <summary>
    /// 각도로부터 방향 벡터 얻기
    /// </summary>
    private Vector3 GetDirectionFromAngle(float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0f);
    }
    
    #region 🧱 벽 충돌 시스템
    
    /// <summary>
    /// 벽이 중간에 있는지 Raycast로 체크
    /// </summary>
    private bool IsBlockedByWall(Transform target)
    {
        if (wallLayer == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("🚨 [DamageArea] wallLayer가 설정되지 않았습니다! checkWallBlocking은 true인데 Layer가 없음!");
            return false;
        }
        
        Vector2 rayOrigin = this.origin; // AOE Origin (Cast Point)
        Vector2 direction = (target.position - (Vector3)rayOrigin).normalized;
        float distance = Vector2.Distance(rayOrigin, target.position);
        
        // Raycast로 벽 감지
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, distance, wallLayer);
        
        if (hit.collider != null)
        {
            if (enableDebugLogs)
                Debug.Log($"🧱 [DamageArea] 벽 차단: {target.name} ← {hit.collider.name} (거리: {hit.distance:F2})");
            return true;
        }
        
        return false;
    }
    
    #endregion
}