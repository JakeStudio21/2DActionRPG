using System.Collections.Generic;
using UnityEngine;
using ItemSystem;
using StageSystem;

/// <summary>
/// 장비 공장 — '장비 생성 의뢰(EquipmentGenerationRequest)'를 실제 아이템 ID로 변환합니다.
///
/// 역할:
///   RewardCalculator 가 계산한 등급 범위(minRarity ~ maxRarity)와
///   플레이어 클래스 정보를 받아 아래 절차를 수행합니다.
///
///   1. 장비 슬롯 롤링   — 현재 플레이어 클래스 장비 50%, 나머지 2클래스 각 25%
///   2. 등급 롤링        — 8등급(D~TR) 가중치 기반, 인스펙터에서 기획자가 직접 조정
///   3. ID 조합          — ITEM_{TYPE}_{CLASS}_{GRADE} 컨벤션으로 문자열 조합
///   4. 에셋 로드 검증   — ItemTemplateResolver.Load() 실패 시 Fallback 장비 반환
///   5. Soulbound 판정   — SS(rank 5) 이상이면 isSoulbound = true
/// </summary>
public class ItemGenerator : MonoBehaviour
{
    public static ItemGenerator Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────
    // 인스펙터 설정
    // ─────────────────────────────────────────────────────────────────

    [Header("🎲 등급별 기본 가중치 (D ~ TR)")]
    [Tooltip("등급별 기본 가중치. 인스펙터에서 기획자가 직접 조정합니다.\n" +
             "실제 롤링 시에는 minRarity ~ maxRarity 범위 안의 항목만 사용됩니다.")]
    [SerializeField] private List<RankWeight> rankWeights = new List<RankWeight>
    {
        new RankWeight { rank = EquipmentRank.D,  weight = 40f },
        new RankWeight { rank = EquipmentRank.C,  weight = 25f },
        new RankWeight { rank = EquipmentRank.B,  weight = 15f },
        new RankWeight { rank = EquipmentRank.A,  weight = 10f },
        new RankWeight { rank = EquipmentRank.S,  weight =  6f },
        new RankWeight { rank = EquipmentRank.SS, weight =  2f },
        new RankWeight { rank = EquipmentRank.EX, weight =  1.5f },
        new RankWeight { rank = EquipmentRank.TR, weight =  0.5f },
    };

    [Header("🎯 클래스 장비 슬롯 비율")]
    [Tooltip("현재 플레이어 클래스 장비가 뽑힐 확률 (0~1)\n" +
             "나머지 확률을 다른 클래스들이 균등 분배합니다.")]
    [Range(0f, 1f)]
    [SerializeField] private float currentClassSlotRatio = 0.5f;

    [Header("🔄 Fallback 장비")]
    [Tooltip("아이템 ID 조합 후 에셋을 찾지 못했을 때 대신 지급하는 아이템 ID.\n" +
             "비워 두면 null 을 반환합니다.")]
    [SerializeField] private string fallbackItemId = "";

    // ─────────────────────────────────────────────────────────────────
    // 클래스 → 아이템 ID 문자열 매핑 (실제 에셋 파일명 기준)
    //
    //   PlayerType.Warrior → "WARRIOR"  (나이트 캐릭터)
    //   PlayerType.Assasin → "ASSASIN"  (아처 캐릭터)
    //   PlayerType.Wizard  → "WIZARD"   (위자드 캐릭터)
    //
    // 이 매핑이 아이템 에셋 파일명과 1:1 대응됩니다.
    // (예: ITEM_SWORD_D, ITEM_BELT_ASSASIN_C_Equipment, ITEM_ARMOR_WARRIOR_B_Equipment)
    // ─────────────────────────────────────────────────────────────────

    // 클래스 전용 슬롯 (무기 포함): 스마트 드롭 비율 계산에 사용
    private static readonly EquipmentSlot[] ClassSpecificSlots = new[]
    {
        EquipmentSlot.MainWeapon,
        EquipmentSlot.Helmet,
        EquipmentSlot.Armor,
        EquipmentSlot.Gloves,
        EquipmentSlot.Boots,
        EquipmentSlot.Belt,
    };

    // 공용 슬롯 (악세서리): 클래스 불문 동일 확률
    private static readonly EquipmentSlot[] SharedSlots = new[]
    {
        EquipmentSlot.Ring1,
        EquipmentSlot.Ring2,
        EquipmentSlot.Necklace,
    };

    // 전체 클래스 목록 (슬롯 가중치 계산에 사용)
    private static readonly PlayerType[] AllClasses = new[]
    {
        PlayerType.Warrior,  // Knight
        PlayerType.Assasin,  // Archer
        PlayerType.Wizard,
    };

    // ─────────────────────────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────────
    // 공개 API
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 장비 생성 의뢰를 처리하여 최종 GenerationResult 를 반환합니다.
    ///
    /// 호출 위치: RewardSystem.ApplyRewards() — GEN_EQUIP 키워드 분기
    /// </summary>
    public GenerationResult Generate(EquipmentGenerationRequest request)
    {
        var result = new GenerationResult();

        // ── 1. 장비 슬롯 롤링 ────────────────────────────────────
        EquipmentSlot slot = RollEquipmentSlot(request.playerType, request.stageConfig);

        // ── 2. 등급 롤링 ──────────────────────────────────────────
        EquipmentRank rank = RollRank(request.minRarity, request.maxRarity);

        // ── 3. 아이템 ID 조합 ─────────────────────────────────────
        string itemId = BuildItemId(slot, rank, request.playerType);

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogWarning("[ItemGenerator] 아이템 ID 조합 실패. Fallback 사용.");
            itemId = fallbackItemId;
        }

        // ── 4. 에셋 로드 검증 ─────────────────────────────────────
        if (!string.IsNullOrEmpty(itemId))
        {
            var equipData = ItemTemplateResolver.Load(itemId);
            if (equipData == null)
            {
                Debug.LogWarning($"[ItemGenerator] 에셋 로드 실패: {itemId}. Fallback({fallbackItemId}) 사용.");
                itemId = fallbackItemId;

                // Fallback 도 검증
                if (!string.IsNullOrEmpty(fallbackItemId) && ItemTemplateResolver.Load(fallbackItemId) == null)
                {
                    Debug.LogError($"[ItemGenerator] Fallback 에셋도 로드 실패: {fallbackItemId}. null 반환.");
                    itemId = null;
                }
            }
        }

        // ── 5. Soulbound 판정 (SS 이상) ───────────────────────────
        // SS(5), EX(6), TR(7) 등급은 장착 시 영혼 귀속 처리 대상으로 표시합니다.
        bool isSoulbound = (int)rank >= (int)EquipmentRank.SS;

        result.templateId  = itemId;
        result.rank        = rank;
        result.slot        = slot;
        result.isSoulbound = isSoulbound;
        result.isValid     = !string.IsNullOrEmpty(itemId);

        if (result.isValid)
        {
            Debug.Log($"✅ [ItemGenerator] 생성 완료: {itemId} | {rank.GetRankName()} | Slot: {slot} | Soulbound: {isSoulbound}");
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────
    // 슬롯 롤링
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 스마트 드롭 — 현재 클래스 전용 슬롯에 50%, 나머지 클래스 슬롯에 각 25% 배분.
    /// 공용 슬롯(악세서리)은 별도 가중치로 독립 롤링에 참여합니다.
    /// </summary>
    private EquipmentSlot RollEquipmentSlot(PlayerType playerType, StageConfig stageConfig)
    {
        // 슬롯 풀과 가중치 구성
        var slotPool    = new List<EquipmentSlot>();
        var slotWeights = new List<float>();

        // ── 클래스 전용 슬롯 ──────────────────────────────────────
        // 현재 클래스의 전용 슬롯: currentClassSlotRatio (기본 50%)
        // 나머지 두 클래스 슬롯: 각 (1 - currentClassSlotRatio) / 2
        float otherClassRatio = (1f - currentClassSlotRatio) / 2f;

        foreach (var cls in AllClasses)
        {
            float classWeight = (cls == playerType) ? currentClassSlotRatio : otherClassRatio;

            // 해당 클래스 전용 슬롯 목록에서 하나를 선택할 확률을 각 슬롯에 균등 배분
            float perSlotWeight = classWeight / ClassSpecificSlots.Length;

            foreach (var slot in ClassSpecificSlots)
            {
                slotPool.Add(slot);
                slotWeights.Add(perSlotWeight);
            }
        }

        // ── 공용 슬롯 (악세서리) ─────────────────────────────────
        // 공용 슬롯은 currentClassSlotRatio / SharedSlots.Length 로 균등 배분
        // (공용 슬롯이 전체 weight 에서 차지하는 비중은 기획자가 조정 가능하도록
        //  classSpecific: shared = 1 : sharedRatio 로 잡습니다.)
        const float sharedSlotRatio = 0.3f; // 공용 슬롯의 전체 weight 비중
        float perSharedWeight = sharedSlotRatio / SharedSlots.Length;
        foreach (var slot in SharedSlots)
        {
            slotPool.Add(slot);
            slotWeights.Add(perSharedWeight);
        }

        // 타겟 파밍 보정: 특정 슬롯의 weight 를 weightMultiplier 배로 증폭
        ApplyTargetFarmingToSlotWeights(slotPool, slotWeights, stageConfig);

        return WeightedPick(slotPool, slotWeights);
    }

    /// <summary>
    /// StageConfig.targetFarmingEntries 중 슬롯 필터가 활성화된 항목을 슬롯 가중치에 반영합니다.
    /// </summary>
    private void ApplyTargetFarmingToSlotWeights(
        List<EquipmentSlot> slots,
        List<float>         weights,
        StageConfig         stageConfig)
    {
        if (stageConfig == null || stageConfig.targetFarmingEntries == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            foreach (var entry in stageConfig.targetFarmingEntries)
            {
                // 슬롯 필터가 활성화된 엔트리만 적용 (itemIdFilter 전용 엔트리는 이 단계에서 무시)
                if (entry.useSlotFilter && string.IsNullOrEmpty(entry.itemIdFilter))
                {
                    if (slots[i] == entry.slotFilter)
                        weights[i] *= entry.weightMultiplier;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 등급 롤링
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// minRarity ~ maxRarity 범위 내의 등급을 가중치 기반으로 롤링합니다.
    /// </summary>
    private EquipmentRank RollRank(EquipmentRank minRarity, EquipmentRank maxRarity)
    {
        var validRanks   = new List<EquipmentRank>();
        var validWeights = new List<float>();

        foreach (var rw in rankWeights)
        {
            if ((int)rw.rank >= (int)minRarity && (int)rw.rank <= (int)maxRarity)
            {
                validRanks.Add(rw.rank);
                validWeights.Add(Mathf.Max(0f, rw.weight));
            }
        }

        if (validRanks.Count == 0)
        {
            Debug.LogWarning($"[ItemGenerator] [{minRarity}~{maxRarity}] 범위에 유효한 등급이 없습니다. minRarity 반환.");
            return minRarity;
        }

        return WeightedPick(validRanks, validWeights);
    }

    // ─────────────────────────────────────────────────────────────────
    // ID 조합
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// ITEM_{TYPE}_{CLASS}_{GRADE} 컨벤션으로 장비 ID를 조합합니다.
    ///
    /// 클래스 명칭 매핑 (PlayerType → 에셋 파일명 CLASS 부분):
    ///   PlayerType.Warrior (나이트) → "WARRIOR"
    ///   PlayerType.Assasin (아처)   → "ASSASIN"
    ///   PlayerType.Wizard  (위자드) → "WIZARD"
    ///
    /// 슬롯 → 타입 문자열 매핑:
    ///   MainWeapon → 클래스별 무기 타입 (SWORD / BOW / STAFF)
    ///   방어구 슬롯 → 슬롯 이름 그대로 (HELMET, ARMOR, GLOVES, BOOTS, BELT)
    ///   악세서리 → RING, NECKLACE (클래스 구분 없음)
    /// </summary>
    private string BuildItemId(EquipmentSlot slot, EquipmentRank rank, PlayerType playerType)
    {
        string typeStr  = GetTypeString(slot, playerType);
        string classStr = GetClassString(slot, playerType);
        string gradeStr = rank.ToString(); // D, C, B, A, S, SS, EX, TR

        if (string.IsNullOrEmpty(typeStr))
        {
            Debug.LogWarning($"[ItemGenerator] 슬롯 {slot} / 클래스 {playerType} 에 대한 타입 문자열이 없습니다.");
            return null;
        }

        // 무기는 클래스 구분 없음 → ITEM_SWORD_D / ITEM_BOW_C / ITEM_STAFF_B
        if (string.IsNullOrEmpty(classStr))
            return $"ITEM_{typeStr}_{gradeStr}";

        return $"ITEM_{typeStr}_{classStr}_{gradeStr}";
    }

    /// <summary>
    /// 슬롯과 플레이어 타입을 조합하여 타입 문자열을 반환합니다.
    /// </summary>
    private string GetTypeString(EquipmentSlot slot, PlayerType playerType)
    {
        switch (slot)
        {
            case EquipmentSlot.MainWeapon:
                // 무기 타입은 클래스마다 다릅니다.
                return playerType switch
                {
                    PlayerType.Warrior => "SWORD",  // 나이트(Knight) → 검
                    PlayerType.Assasin => "BOW",    // 아처(Archer)   → 활
                    PlayerType.Wizard  => "STAFF",  // 위자드(Wizard)  → 스태프
                    _                  => "SWORD",
                };

            case EquipmentSlot.Helmet:    return "HELMET";
            case EquipmentSlot.Armor:     return "ARMOR";
            case EquipmentSlot.Gloves:    return "GLOVES";
            case EquipmentSlot.Boots:     return "BOOTS";
            case EquipmentSlot.Belt:      return "BELT";

            // 악세서리(공용)
            case EquipmentSlot.Ring1:
            case EquipmentSlot.Ring2:     return "RING";
            case EquipmentSlot.Necklace:  return "NECKLACE";

            default:
                Debug.LogWarning($"[ItemGenerator] 알 수 없는 슬롯: {slot}");
                return null;
        }
    }

    /// <summary>
    /// 클래스 문자열을 반환합니다.
    /// 공용 슬롯(악세서리)은 클래스 구분이 없으므로 빈 문자열을 반환합니다.
    ///
    /// 명칭 매핑:
    ///   Warrior(나이트) → "KNIGHT"
    ///   Assasin(아처)   → "ARCHER"
    ///   Wizard(위자드)  → "WIZARD"
    /// </summary>
    private string GetClassString(EquipmentSlot slot, PlayerType playerType)
    {
        // 무기: 클래스 구분 없음 → ITEM_SWORD_D / ITEM_BOW_D / ITEM_STAFF_D
        if (slot == EquipmentSlot.MainWeapon)
            return "";

        // 악세서리: 공용이지만 파일명에 NONE 포함 → ITEM_RING_NONE_D / ITEM_NECKLACE_NONE_D
        if (slot == EquipmentSlot.Ring1 || slot == EquipmentSlot.Ring2 || slot == EquipmentSlot.Necklace)
            return "NONE";

        // 방어구: 클래스별 파일명 → ITEM_BELT_WARRIOR_C / ITEM_HELMET_ASSASIN_B 등
        return playerType switch
        {
            PlayerType.Warrior => "WARRIOR",
            PlayerType.Assasin => "ASSASIN",
            PlayerType.Wizard  => "WIZARD",
            _                  => "WARRIOR",
        };
    }

    // ─────────────────────────────────────────────────────────────────
    // 가중치 기반 선택 유틸
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 가중치 리스트를 기반으로 항목 하나를 무작위 선택합니다.
    /// </summary>
    private T WeightedPick<T>(List<T> items, List<float> weights)
    {
        float total = 0f;
        foreach (var w in weights) total += w;

        if (total <= 0f)
        {
            Debug.LogWarning("[ItemGenerator] WeightedPick: 총 가중치가 0 이하입니다. 첫 번째 항목 반환.");
            return items[0];
        }

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return items[i];
        }

        return items[items.Count - 1]; // 부동소수점 오차 안전 처리
    }
}

// ─────────────────────────────────────────────────────────────────────
// 데이터 구조체 및 열거형
// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 장비 생성 의뢰 — RewardSystem 이 ItemGenerator.Generate() 에 전달하는 파라미터
/// </summary>
[System.Serializable]
public class EquipmentGenerationRequest
{
    /// <summary>드롭 가능한 최소 장비 등급 (RewardCalculator 가 설정)</summary>
    public EquipmentRank minRarity = EquipmentRank.D;

    /// <summary>드롭 가능한 최대 장비 등급 (RewardCalculator 가 설정)</summary>
    public EquipmentRank maxRarity = EquipmentRank.A;

    /// <summary>
    /// 현재 플레이어 타입 (PlayerDataManager.GetCurrentPlayerType() 으로 조회).
    ///
    /// ID 조합 시 매핑:
    ///   Warrior → KNIGHT  (나이트)
    ///   Assasin → ARCHER  (아처)
    ///   Wizard  → WIZARD  (위자드)
    /// </summary>
    public PlayerType playerType = PlayerType.Warrior;

    /// <summary>타겟 파밍 / 오버라이드 테이블 참조용 스테이지 설정</summary>
    public StageConfig stageConfig;
}

/// <summary>
/// 장비 생성 결과 — ItemGenerator.Generate() 반환값
/// </summary>
[System.Serializable]
public class GenerationResult
{
    /// <summary>생성된 장비의 templateId (AddItemV2 에 전달)</summary>
    public string templateId;

    /// <summary>결정된 등급</summary>
    public EquipmentRank rank;

    /// <summary>결정된 슬롯</summary>
    public EquipmentSlot slot;

    /// <summary>
    /// 영혼 귀속 여부 — SS(rank 5) 이상이면 true.
    /// 실제 귀속 처리는 향후 장착 시스템에서 이 플래그를 읽어 처리합니다.
    /// </summary>
    public bool isSoulbound;

    /// <summary>생성에 성공했으면 true</summary>
    public bool isValid;
}

/// <summary>
/// 등급별 가중치 쌍 — 인스펙터에서 기획자가 직접 조정
/// </summary>
[System.Serializable]
public class RankWeight
{
    public EquipmentRank rank;

    [Range(0f, 100f)]
    public float weight;
}
