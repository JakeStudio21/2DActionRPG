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
    [SerializeField] private Color normalDamageColor = Color.white; // 흰색

    [Tooltip("플레이어 피격 데미지 색상")]
    [SerializeField] private Color playerHitDamageColor = new Color(1f, 0.3f, 0f); // 주황색

    [Tooltip("크리티컬 데미지 색상 (플레이어 → 몬스터)")]
    [SerializeField] private Color criticalDamageColor = Color.yellow; // 노란색
    
    [Tooltip("플레이어 피격 크리티컬 색상 (몬스터 → 플레이어)")]
    [SerializeField] private Color playerHitCriticalColor = Color.red; // 빨간색

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
    [SerializeField] private float healScale = 0.8f;
    
    [Header("🛡️ 면역 설정 (Phase 4-C)")]
    [Tooltip("면역 텍스트 색상")]
    [SerializeField] private Color immunityColor = new Color(0.3f, 0.7f, 1f); // 밝은 파란색
    
    [Tooltip("면역 텍스트 크기 배율")]
    [SerializeField] private float immunityScale = 1.3f;

    [Header("🔮 상태이상 텍스트")]
    [Tooltip("상태이상 적용 텍스트 색상")]
    [SerializeField] private Color statusAppliedColor = new Color(1f, 0.75f, 0.1f); // 주황-노랑

    [Tooltip("상태이상 저항 텍스트 색상")]
    [SerializeField] private Color statusResistedColor = new Color(0.4f, 0.85f, 1f); // 하늘색

    [Tooltip("상태이상 텍스트 크기 배율")]
    [SerializeField] private float statusTextScale = 1.1f;

    [Tooltip("상태이상 텍스트 Y 추가 오프셋 (음수 = 데미지 숫자 아래로 배치)")]
    [SerializeField] private float statusTextExtraOffsetY = -0.5f;

    [Header("🌀 회피/블록 텍스트")]
    [Tooltip("회피(DODGE) 텍스트 색상")]
    [SerializeField] private Color dodgeColor = new Color(0.4f, 1f, 1f); // 밝은 청록

    [Tooltip("블록(BLOCK) 텍스트 색상")]
    [SerializeField] private Color blockColor = new Color(0.8f, 0.8f, 0.8f); // 밝은 회색(은색)

    [Tooltip("회피/블록 텍스트 크기 배율")]
    [SerializeField] private float dodgeBlockScale = 1.0f;

    [Tooltip("회피/블록 텍스트 Y 추가 오프셋 (데미지 숫자보다 위에 표시)")]
    [SerializeField] private float dodgeBlockExtraOffsetY = 1.0f;

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

        // ⚙️ 색상 & 크기 강제 설정 (Phase 1 시각 피드백)
        normalDamageColor = Color.white; // 일반 데미지: 흰색
        playerHitDamageColor = new Color(1f, 0.3f, 0f); // 플레이어 피격: 주황색
        criticalDamageColor = Color.yellow; // 몬스터 크리티컬 (플레이어 → 몬스터): 노란색
        playerHitCriticalColor = Color.red; // 플레이어 크리티컬 (몬스터 → 플레이어): 빨간색
        criticalDamageScale = 1.5f; // 크리티컬: 1.5배 크기
        
        Debug.Log($"🎨 [DamageNumberManager] 시각 설정 완료:");
        Debug.Log($"  - 일반 데미지 (플레이어 → 몬스터): 흰색 {normalDamageColor}");
        Debug.Log($"  - 플레이어 피격 (몬스터 → 플레이어): 주황색 {playerHitDamageColor}");
        Debug.Log($"  - 몬스터 크리티컬 (플레이어 → 몬스터): 노란색 {criticalDamageColor}");
        Debug.Log($"  - 플레이어 크리티컬 (몬스터 → 플레이어): 빨간색 {playerHitCriticalColor}");
        Debug.Log($"  - 크기: {criticalDamageScale}배");

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
    
    #region Public API - Phase 4-C (DamageResult 통합)
    
    /// <summary>
    /// ⚙️ Phase 4-C: DamageResult 기반 데미지 숫자 표시 (크리티컬, 면역 연출 포함)
    /// </summary>
    /// <param name="targetPosition">피격 대상의 transform.position</param>
    /// <param name="result">CombatFormula.DamageResult 객체</param>
    /// <param name="isPlayer">true: 플레이어 피격, false: 적 피격</param>
    /// <param name="targetTransform">피격 대상의 Transform (앵커 검색용, 선택)</param>
    public void ShowDamage(Vector3 targetPosition, CombatFormula.DamageResult result, bool isPlayer, Transform targetTransform = null)
    {
        // Prefab 검증
        if (damageNumberPrefab == null)
        {
            Debug.LogError("[DamageNumberManager] Prefab이 없어서 데미지 숫자를 표시할 수 없습니다!");
            return;
        }
        
        // ✅ 앵커 우선 검색 → 없으면 오프셋 사용
        Vector3 displayPosition = GetDamageNumberPosition(targetPosition, isPlayer, targetTransform);
        
        // 🛡️ 우선순위 1: 면역 (Immunity) 처리
        if (result.hasImmunity && !string.IsNullOrEmpty(result.resistedEffects))
        {
            ShowImmunityNumber(displayPosition, result.resistedEffects, result.finalDamage);
            return; // 면역 표시만 하고 종료
        }
        
        // 💥 우선순위 2: 크리티컬 데미지
        if (result.isCritical)
        {
            ShowCriticalDamageNumber(displayPosition, result.finalDamage, isPlayer);
            return;
        }
        
        // ⚔️ 우선순위 3: 일반 데미지
        ShowNormalDamageNumber(displayPosition, result.finalDamage, isPlayer);
    }
    
    /// <summary>
    /// 💥 크리티컬 데미지 표시 (공격자에 따라 색상 구분)
    /// </summary>
    private void ShowCriticalDamageNumber(Vector3 displayPosition, int damage, bool isPlayer)
    {
        // 데미지 숫자 생성
        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition, damage);
        
        // ⭐ 크리티컬 색상 구분
        // isPlayer = true → 플레이어가 피격 (몬스터 → 플레이어) → 노란색
        // isPlayer = false → 몬스터가 피격 (플레이어 → 몬스터) → 빨간색
        Color critColor = isPlayer ? playerHitCriticalColor : criticalDamageColor;
        
        spawnedNumber.SetColor(critColor);
        spawnedNumber.transform.localScale *= criticalDamageScale; // 1.5배
        
        // ⭐ 강제 디버그 (색상 확인용)
        string target = isPlayer ? "플레이어 피격" : "몬스터 피격";
        Debug.Log($"💥 [DamageNumberManager] 크리티컬 {target}: {damage} | 색상: {critColor} | 크기: {criticalDamageScale}배");
    }
    
    /// <summary>
    /// ⚔️ 일반 데미지 표시
    /// </summary>
    private void ShowNormalDamageNumber(Vector3 displayPosition, int damage, bool isPlayer)
    {
        // 데미지 숫자 생성
        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition, damage);
        
        // ⭐ 색상 구분
        Color damageColor = isPlayer ? playerHitDamageColor : normalDamageColor;
        float damageScale = isPlayer ? playerHitDamageScale : normalDamageScale;
        
        spawnedNumber.SetColor(damageColor);
        spawnedNumber.transform.localScale *= damageScale;
        
        // ⭐ 강제 디버그 (색상 확인용)
        string targetType = isPlayer ? "플레이어 피격" : "몬스터 피격";
        Debug.Log($"⚔️ [DamageNumberManager] {targetType}: {damage} | 색상: {damageColor} | 크기: {damageScale}배");
    }
    
    /// <summary>
    /// 🛡️ 면역 텍스트 표시 (파란색, "IMMUNE (효과명)")
    /// </summary>
    private void ShowImmunityNumber(Vector3 displayPosition, string resistedEffects, int damage)
    {
        // 데미지 숫자 생성 (0으로 설정)
        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition, 0);
        
        // ⭐ 데미지 숫자 숨기기 (면역 텍스트만 표시)
        spawnedNumber.enableNumber = false;
        
        // ⭐ 면역 텍스트 설정 (영어로 표시 - 한글 폰트 미지원)
        spawnedNumber.enableTopText = true;
        spawnedNumber.topText = "IMMUNE"; // 위쪽에 "IMMUNE" 표시
        
        // ⭐ 면역된 효과 이름 (아래쪽)
        spawnedNumber.enableBottomText = true;
        spawnedNumber.bottomText = $"({resistedEffects})"; // 예: (Bind)
        
        // ⭐ 색상 및 크기
        spawnedNumber.SetColor(immunityColor); // 파란색
        spawnedNumber.transform.localScale *= immunityScale; // 1.3배로 크게 표시
        
        if (enableDebugLogs)
        {
            Debug.Log($"🛡️ [DamageNumberManager] 면역 표시: {resistedEffects} at {displayPosition:F2}");
        }
    }

    /// <summary>
    /// 💚 흡혈 회복 숫자 표시 (+N, 초록색)
    /// </summary>
    /// <param name="targetPosition">플레이어 위치</param>
    /// <param name="amount">회복량</param>
    /// <param name="targetTransform">플레이어 Transform (앵커 검색용)</param>
    public void ShowHealNumber(Vector3 targetPosition, int amount, Transform targetTransform = null)
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogError("[DamageNumberManager] Prefab이 없어서 힐 숫자를 표시할 수 없습니다!");
            return;
        }

        if (amount <= 0) return;

        Vector3 displayPosition = GetDamageNumberPosition(targetPosition, isPlayer: true, targetTransform);

        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition, amount);
        spawnedNumber.SetColor(healColor);
        spawnedNumber.transform.localScale *= healScale;

        // "+" 기호를 숫자 위에 표시
        spawnedNumber.enableTopText = true;
        spawnedNumber.topText = "+";

        if (enableDebugLogs)
            Debug.Log($"💚 [DamageNumberManager] 흡혈 회복 표시: +{amount} at {displayPosition:F2}");
    }

    /// <summary>
    /// 🔮 상태이상 적용 텍스트 표시 (효과 이름, 주황-노랑)
    /// 예: "Bind" "Poison"
    /// </summary>
    public void ShowStatusEffectApplied(Vector3 targetPosition, string effectName, Transform targetTransform = null)
    {
        if (damageNumberPrefab == null) return;

        Vector3 displayPosition = GetDamageNumberPosition(targetPosition, isPlayer: true, targetTransform)
                                  + Vector3.up * statusTextExtraOffsetY;

        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition);
        spawnedNumber.enableNumber   = false;
        spawnedNumber.enableTopText  = true;
        spawnedNumber.topText        = effectName;
        spawnedNumber.SetColor(statusAppliedColor);
        spawnedNumber.transform.localScale *= statusTextScale;

        if (enableDebugLogs)
            Debug.Log($"🔮 [DamageNumberManager] 상태이상 적용 표시: {effectName} at {displayPosition:F2}");
    }

    /// <summary>
    /// 🔮 상태이상 저항 성공 텍스트 표시 ("RESIST", 하늘색)
    /// </summary>
    public void ShowStatusEffectResisted(Vector3 targetPosition, string effectName, Transform targetTransform = null)
    {
        if (damageNumberPrefab == null) return;

        Vector3 displayPosition = GetDamageNumberPosition(targetPosition, isPlayer: true, targetTransform)
                                  + Vector3.up * statusTextExtraOffsetY;

        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition);
        spawnedNumber.enableNumber     = false;
        spawnedNumber.enableTopText    = true;
        spawnedNumber.topText          = "RESIST";
        spawnedNumber.enableBottomText = false;
        spawnedNumber.SetColor(statusResistedColor);
        spawnedNumber.transform.localScale *= statusTextScale;

        if (enableDebugLogs)
            Debug.Log($"🔮 [DamageNumberManager] 상태이상 저항 표시: RESIST ({effectName}) at {displayPosition:F2}");
    }

    /// <summary>
    /// 💨 회피 성공 텍스트 표시 ("DODGE", 청록색)
    /// </summary>
    public void ShowDodgeText(Vector3 targetPosition, Transform targetTransform = null)
    {
        if (damageNumberPrefab == null) return;

        Vector3 displayPosition = GetDamageNumberPosition(targetPosition, isPlayer: true, targetTransform)
                                  + Vector3.up * dodgeBlockExtraOffsetY;

        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition);
        spawnedNumber.enableNumber  = false;
        spawnedNumber.enableTopText = true;
        spawnedNumber.topText       = "MISS";
        spawnedNumber.SetColor(dodgeColor);
        spawnedNumber.transform.localScale *= dodgeBlockScale;

        if (enableDebugLogs)
            Debug.Log($"💨 [DamageNumberManager] 회피 텍스트 표시: DODGE at {displayPosition:F2}");
    }

    /// <summary>
    /// 🛡️ 블록 성공 텍스트 표시 ("BLOCK", 은색)
    /// </summary>
    public void ShowBlockText(Vector3 targetPosition, Transform targetTransform = null)
    {
        if (damageNumberPrefab == null) return;

        Vector3 displayPosition = GetDamageNumberPosition(targetPosition, isPlayer: true, targetTransform)
                                  + Vector3.up * dodgeBlockExtraOffsetY;

        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition);
        spawnedNumber.enableNumber  = false;
        spawnedNumber.enableTopText = true;
        spawnedNumber.topText       = "BLOCK";
        spawnedNumber.SetColor(blockColor);
        spawnedNumber.transform.localScale *= dodgeBlockScale;

        if (enableDebugLogs)
            Debug.Log($"🛡️ [DamageNumberManager] 블록 텍스트 표시: BLOCK at {displayPosition:F2}");
    }

    /// <summary>
    /// 🆙 레벨업 텍스트 표시 ("LEVEL UP!" + 새 레벨, 금색)
    /// DamageNumbersPro 폰트를 그대로 사용하며, topText에 "LEVEL UP!", bottomText에 "Lv.N" 표시
    /// </summary>
    /// <param name="targetPosition">플레이어 월드 좌표</param>
    /// <param name="newLevel">레벨업 후 레벨</param>
    public void ShowLevelUpText(Vector3 targetPosition, int newLevel)
    {
        if (damageNumberPrefab == null) return;

        // 머리 위 기준으로 조금 더 높게 표시 (일반 숫자보다 위)
        Vector3 displayPosition = targetPosition + Vector3.up * (enemyDamageOffsetY + 1.0f);

        DamageNumber spawnedNumber = damageNumberPrefab.Spawn(displayPosition);
        spawnedNumber.enableNumber     = false;
        spawnedNumber.enableTopText    = true;
        spawnedNumber.topText          = "LEVEL UP!";
        spawnedNumber.enableBottomText = true;
        spawnedNumber.bottomText       = $"Lv.{newLevel}";
        spawnedNumber.SetColor(new Color(1f, 0.85f, 0f)); // 금색
        spawnedNumber.transform.localScale *= 1.0f;       // 강조 크기

        if (enableDebugLogs)
            Debug.Log($"🆙 [DamageNumberManager] 레벨업 텍스트 표시: LEVEL UP! Lv.{newLevel} at {displayPosition:F2}");
    }

    #endregion

    #region Future Expansion - Phase 2 (준비됨)

    /*
    /// <summary>
    /// 🔮 Phase 2: 크리티컬 데미지 표시 (노란색, 큰 크기, 팝 애니메이션)
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

