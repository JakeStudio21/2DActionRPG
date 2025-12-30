using UnityEngine;
using DamageNumbersPro;

/// <summary>
/// 데미지 넘버 표시 시스템 (DamageNumbersPro 플러그인 통합)
/// ⭐ Phase 1: 기본 데미지 숫자만 표시 (크리티컬/DOT/힐/콤보 제외)
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    #region Singleton

    public static DamageNumberManager Instance { get; private set; }

    #endregion

    #region Inspector Settings

    [Header("프리팹 설정")]
    [Tooltip("DamageNumber Prefab (Inspector 할당 우선)")]
    [SerializeField] private DamageNumber damageNumberPrefab;

    [Header("위치 오프셋")]
    [Tooltip("플레이어 피격 시 데미지 숫자 Y 오프셋 (머리 위)")]
    [SerializeField] private float playerDamageOffsetY = 1.5f;

    [Tooltip("적 피격 시 데미지 숫자 Y 오프셋 (머리 위)")]
    [SerializeField] private float enemyDamageOffsetY = 1.5f;

    [Header("🎨 색상 설정 (Phase 2)")]
    [Tooltip("일반 데미지 색상 (몬스터 피격)")]
    [SerializeField] private Color normalDamageColor = Color.white;

    [Tooltip("플레이어 피격 데미지 색상")]
    [SerializeField] private Color playerHitDamageColor = Color.red;

    [Tooltip("크리티컬 데미지 색상")]
    [SerializeField] private Color criticalDamageColor = new Color(1f, 0.6f, 0f); // 밝은 주황

    [Tooltip("DoT (지속 데미지) 색상")]
    [SerializeField] private Color dotDamageColor = new Color(0.7f, 0f, 1f); // 보라

    [Tooltip("힐 색상")]
    [SerializeField] private Color healColor = Color.green;

    [Header("🎯 크기 배율 (Phase 2)")]
    [Tooltip("일반 데미지 크기 배율")]
    [SerializeField] private float normalDamageScale = 1.0f;

    [Tooltip("플레이어 피격 크기 배율")]
    [SerializeField] private float playerHitDamageScale = 1.0f;

    [Tooltip("크리티컬 데미지 크기 배율")]
    [SerializeField] private float criticalDamageScale = 1.5f;

    [Tooltip("DoT 데미지 크기 배율")]
    [SerializeField] private float dotDamageScale = 0.8f;

    [Tooltip("힐 크기 배율")]
    [SerializeField] private float healScale = 1.2f;

    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // ✅ 싱글톤 중복 생성 방지
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[DamageNumberManager] 이미 Instance가 존재합니다! {gameObject.name}을(를) 제거합니다.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ Prefab 검증 (Inspector 할당 우선, 없으면 Resources.Load 백업)
        ValidatePrefab();
        
        if (enableDebugLogs)
        {
            Debug.Log("[DamageNumberManager] 초기화 완료 - DontDestroyOnLoad 적용됨");
        }
    }

    #endregion

    #region Prefab Management

    /// <summary>
    /// Prefab 검증 및 백업 로드
    /// </summary>
    private void ValidatePrefab()
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("[DamageNumberManager] Inspector에 Prefab이 할당되지 않았습니다! Resources에서 로드를 시도합니다...");
            
            // Resources.Load 백업
            damageNumberPrefab = Resources.Load<DamageNumber>("Prefabs/VFX/DamageNumber");

            if (damageNumberPrefab != null)
            {
                Debug.Log("[DamageNumberManager] Resources에서 DamageNumber Prefab 로드 성공!");
            }
            else
            {
                Debug.LogError("[DamageNumberManager] DamageNumber Prefab을 찾을 수 없습니다! " +
                              "Inspector에 할당하거나 Resources/Prefabs/VFX/DamageNumber.prefab 경로를 확인하세요.");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[DamageNumberManager] Prefab 할당 확인: {damageNumberPrefab.name}");
            }
        }
    }

    #endregion

    #region Public API - Phase 1

    /// <summary>
    /// ⭐ Phase 1: 기본 데미지 숫자 표시 (색상 구분 + 앵커 시스템)
    /// </summary>
    /// <param name="targetPosition">피격 대상의 transform.position (오프셋 계산 전)</param>
    /// <param name="damage">표시할 데미지 값</param>
    /// <param name="isPlayer">true: 플레이어 피격, false: 적 피격</param>
    /// <param name="targetTransform">피격 대상의 Transform (앵커 검색용, 선택)</param>
    public void ShowDamage(Vector3 targetPosition, int damage, bool isPlayer, Transform targetTransform = null)
    {
        // Prefab 검증
        if (damageNumberPrefab == null)
        {
            Debug.LogError("[DamageNumberManager] Prefab이 없어서 데미지 숫자를 표시할 수 없습니다!");
            return;
        }

        // ✅ 앵커 우선 검색 → 없으면 오프셋 사용
        Vector3 displayPosition = GetDamageNumberPosition(targetPosition, isPlayer, targetTransform);

        // 데미지 숫자 생성 (DamageNumbersPro API)
        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition, damage);

        // ⭐ Phase 1.5: 색상 구분 적용
        Color damageColor = isPlayer ? playerHitDamageColor : normalDamageColor;
        float damageScale = isPlayer ? playerHitDamageScale : normalDamageScale;
        
        spawnedNumber.SetColor(damageColor);
        spawnedNumber.transform.localScale *= damageScale;

        if (enableDebugLogs)
        {
            bool usedAnchor = targetTransform != null && FindDamageNumberAnchor(targetTransform) != null;
            Debug.Log($"[DamageNumberManager] 데미지 표시: {damage} at {displayPosition:F2} " +
                     $"(Target: {targetPosition:F2}, isPlayer={isPlayer}, Anchor={usedAnchor})");
        }
    }
    
    /// <summary>
    /// ⭐ 데미지 숫자 표시 위치 계산 (앵커 우선)
    /// </summary>
    private Vector3 GetDamageNumberPosition(Vector3 targetPosition, bool isPlayer, Transform targetTransform)
    {
        // 1순위: 앵커 찾기
        if (targetTransform != null)
        {
            Transform anchor = FindDamageNumberAnchor(targetTransform);
            if (anchor != null)
            {
                return anchor.position;
            }
        }
        
        // 2순위: 오프셋 사용
        float offsetY = isPlayer ? playerDamageOffsetY : enemyDamageOffsetY;
        return targetPosition + Vector3.up * offsetY;
    }
    
    /// <summary>
    /// ⭐ DamageNumberAnchor 찾기 (자식 오브젝트 검색)
    /// </summary>
    private Transform FindDamageNumberAnchor(Transform root)
    {
        // 직접 검색 (이름으로)
        Transform anchor = root.Find("DamageNumberAnchor");
        if (anchor != null)
        {
            return anchor;
        }
        
        // 재귀 검색 (깊은 계층 구조 대응)
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "DamageNumberAnchor")
            {
                return child;
            }
        }
        
        return null;
    }

    #endregion

    #region Future Expansion - Phase 2 (준비됨)

    /*
    /// <summary>
    /// 🔮 Phase 2: 크리티컬 데미지 표시 (밝은 주황, 큰 크기, 팝 애니메이션)
    /// </summary>
    /// <param name="targetPosition">피격 대상 위치</param>
    /// <param name="damage">크리티컬 데미지 값</param>
    /// <param name="isPlayer">true: 플레이어 피격, false: 적 피격</param>
    public void ShowCriticalDamage(Vector3 targetPosition, int damage, bool isPlayer)
    {
        if (damageNumberPrefab == null) return;

        float offsetY = isPlayer ? playerDamageOffsetY : enemyDamageOffsetY;
        Vector3 displayPosition = targetPosition + Vector3.up * offsetY;

        var dn = damageNumberPrefab.Spawn(displayPosition, damage);
        
        // ⭐ Inspector 색상/크기 사용
        dn.SetColor(criticalDamageColor);
        dn.transform.localScale *= criticalDamageScale; // 기본 1.5배
        
        // ⭐ 팝 애니메이션 (DamageNumbersPro 기능 활용)
        // Prefab에서 Scale Fade In 설정으로 팝 효과
        
        // 선택: "!" 접두사 추가
        // dn.enableLeftText = true;
        // dn.leftText = "!";
        
        if (enableDebugLogs)
            Debug.Log($"💥 [DamageNumberManager] 크리티컬 데미지: {damage} (색상: {criticalDamageColor})");
    }

    /// <summary>
    /// 🔮 Phase 2: DoT (지속 데미지) 표시 (보라색, 작은 크기, 짧은 애니메이션)
    /// </summary>
    /// <param name="targetPosition">피격 대상 위치</param>
    /// <param name="damage">DoT 데미지 값</param>
    /// <param name="isPlayer">true: 플레이어 피격, false: 적 피격</param>
    public void ShowDOTDamage(Vector3 targetPosition, int damage, bool isPlayer)
    {
        if (damageNumberPrefab == null) return;

        float offsetY = isPlayer ? playerDamageOffsetY : enemyDamageOffsetY;
        Vector3 displayPosition = targetPosition + Vector3.up * offsetY;

        var dn = damageNumberPrefab.Spawn(displayPosition, damage);
        
        // ⭐ Inspector 색상/크기 사용
        dn.SetColor(dotDamageColor); // 기본 보라색
        dn.transform.localScale *= dotDamageScale; // 기본 0.8배 (작게)
        
        // ⭐ 짧은 애니메이션 (lifetime 단축)
        dn.lifetime = 1.0f; // 기본 2초 → 1초로 단축 (툭툭 효과)
        
        if (enableDebugLogs)
            Debug.Log($"🟣 [DamageNumberManager] DoT 데미지: {damage} (색상: {dotDamageColor})");
    }

    /// <summary>
    /// 🔮 Phase 2: 힐 숫자 표시 (초록색, "+" 접두사)
    /// </summary>
    /// <param name="targetPosition">회복 대상 위치</param>
    /// <param name="healing">회복량</param>
    /// <param name="isPlayer">true: 플레이어 회복, false: 적 회복</param>
    public void ShowHealing(Vector3 targetPosition, int healing, bool isPlayer)
    {
        if (damageNumberPrefab == null) return;

        float offsetY = isPlayer ? playerDamageOffsetY : enemyDamageOffsetY;
        Vector3 displayPosition = targetPosition + Vector3.up * offsetY;

        var dn = damageNumberPrefab.Spawn(displayPosition, healing);
        
        // ⭐ Inspector 색상/크기 사용
        dn.SetColor(healColor); // 기본 초록색
        dn.transform.localScale *= healScale; // 기본 1.2배
        
        // ⭐ "+" 접두사 추가
        dn.enableLeftText = true;
        dn.leftText = "+";
        
        if (enableDebugLogs)
            Debug.Log($"💚 [DamageNumberManager] 힐: +{healing} (색상: {healColor})");
    }

    /// <summary>
    /// 🔮 Phase 3 예정: 콤보 카운트 표시
    /// </summary>
    /// <param name="targetPosition">표시 위치</param>
    /// <param name="comboCount">콤보 카운트</param>
    public void ShowComboCount(Vector3 targetPosition, int comboCount)
    {
        if (damageNumberPrefab == null) return;

        // "x5 COMBO!" 등
        var dn = damageNumberPrefab.Spawn(targetPosition);
        dn.enableNumber = false;
        dn.enableLeftText = true;
        dn.leftText = $"x{comboCount} COMBO!";
        dn.SetColor(new Color(1f, 0.8f, 0f)); // 금색
        dn.transform.localScale *= 1.5f;
    }
    */

    #endregion
}

