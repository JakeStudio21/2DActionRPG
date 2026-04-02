using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 보상 시스템
    /// DropTable 기반 보상 계산 및 지급
    /// </summary>
    public class RewardSystem : MonoBehaviour
    {
        public static RewardSystem Instance { get; private set; }
        
        [Header("보상 설정")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("골드/EXP 커브")]
        [SerializeField] private float goldMultiplier = 1.0f;
        [SerializeField] private float expMultiplier = 1.0f;
        [SerializeField] private int baseGold = 100;
        [SerializeField] private int baseExp = 50;
        
        // 이벤트
        public System.Action<RewardResult> OnRewardsProcessed;
        public System.Action<int> OnGoldRewarded;
        public System.Action<int> OnExpRewarded;
        public System.Action<List<DropItemData>> OnItemsRewarded;
        
        private void Awake()
        {
            // 싱글톤 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            if (enableDebugLogs)
            {
                Debug.Log("�� [RewardSystem] 초기화 완료");
            }
        }
        
        /// <summary>
        /// 스테이지 클리어 보상 처리
        /// </summary>
        public RewardResult ProcessStageRewards(StageConfig stageConfig, bool isFirstClear, float clearTime)
        {
            var result = new RewardResult();
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎁 [RewardSystem] 보상 처리 시작: {stageConfig.StageID} (첫클리어: {isFirstClear})");
            }
            
            // 1. 기본 보상 계산 (골드/EXP)
            CalculateBaseRewards(stageConfig, isFirstClear, clearTime, result);
            
            // 2. 아이템 보상 처리
            ProcessItemRewards(stageConfig, isFirstClear, result);
            
            // 3. 보상 지급
            ApplyRewards(result);
            
            // 4. 이벤트 발생
            OnRewardsProcessed?.Invoke(result);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎁 [RewardSystem] 보상 처리 완료: 골드 {result.Gold}, EXP {result.Exp}, 아이템 {result.Items.Count}개");
            }
            
            return result;
        }
        
        /// <summary>
        /// 기본 보상 계산 (골드/EXP)
        /// </summary>
        private void CalculateBaseRewards(StageConfig stageConfig, bool isFirstClear, float clearTime, RewardResult result)
        {
            // 보상 테이블 선택
            DropTable rewardTable = isFirstClear ? stageConfig.FirstClearDropTable : stageConfig.RepeatClearDropTable;

            int rawGold;
            int rawExp;

            if (rewardTable != null)
            {
                rawGold = rewardTable.Gold;
                rawExp  = rewardTable.Exp;
            }
            else
            {
                // 드롭 테이블이 없을 때 하드코딩 기본값 사용
                rawGold = baseGold;
                rawExp  = baseExp;
            }

            // 클리어 시간 보너스 계산 (빠를수록 최대 1.5배)
            float timeBonus = CalculateTimeBonus(clearTime, stageConfig.TimeLimitSec);

            // RewardCalculator 를 통해 StageBaseLevel 기반 레벨 구간 배율 적용
            // (이전: 고정값 그대로 사용 → 수정: 레벨 구간 goldLevelMultiplier / expLevelMultiplier 반영)
            if (global::RewardCalculator.Instance != null)
            {
                var calcResult = global::RewardCalculator.Instance.CalculateStageClearReward(rawGold, rawExp, timeBonus, stageConfig);
                result.Gold = Mathf.RoundToInt(calcResult.gold * goldMultiplier);
                result.Exp  = Mathf.RoundToInt(calcResult.exp  * expMultiplier);

                // 아이템 드롭 파라미터를 RewardResult 에 전달 (ProcessItemRewards 에서 사용)
                result.EquipmentChanceMultiplier = calcResult.equipmentChanceMultiplier;
                result.MaterialAmountMultiplier  = calcResult.materialAmountMultiplier;
                result.MinRarity                 = calcResult.minRarity;
                result.MaxRarity                 = calcResult.maxRarity;
            }
            else
            {
                // RewardCalculator 가 씬에 없을 때 안전 fallback (레벨 배율 미적용)
                result.Gold = Mathf.RoundToInt(rawGold * timeBonus * goldMultiplier);
                result.Exp  = Mathf.RoundToInt(rawExp  * timeBonus * expMultiplier);
                Debug.LogWarning("[RewardSystem] RewardCalculator.Instance 가 null 입니다. 레벨 구간 배율이 적용되지 않습니다.");
            }

            if (enableDebugLogs)
            {
                Debug.Log($"💰 [RewardSystem] 기본 보상: 골드 {result.Gold}, EXP {result.Exp} " +
                          $"(rawGold={rawGold}, rawExp={rawExp}, timeBonus={timeBonus:F2})");
            }
        }
        
        /// <summary>
        /// 클리어 시간 보너스 계산
        /// </summary>
        private float CalculateTimeBonus(float clearTime, int timeLimit)
        {
            if (timeLimit <= 0) return 1.0f;
            
            float timeRatio = clearTime / timeLimit;
            float bonus = 1.0f;
            
            // 빠를수록 보너스 (최대 1.5배)
            if (timeRatio < 0.5f) bonus = 1.5f;
            else if (timeRatio < 0.7f) bonus = 1.3f;
            else if (timeRatio < 0.9f) bonus = 1.1f;
            
            return bonus;
        }
        
        /// <summary>
        /// 아이템 보상 처리
        /// </summary>
        private void ProcessItemRewards(StageConfig stageConfig, bool isFirstClear, RewardResult result)
        {
            DropTable rewardTable = isFirstClear ? stageConfig.FirstClearDropTable : stageConfig.RepeatClearDropTable;
            
            if (rewardTable != null && rewardTable.Items.Count > 0)
            {
                // isFirstClear 를 전달하여 최초 클리어 시 드롭률 1.5배 보너스가 실제로 적용되도록 합니다.
                var droppedItems = rewardTable.RollDrops(isFirstClear);
                result.Items = droppedItems;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"�� [RewardSystem] 아이템 드롭: {droppedItems.Count}개 (첫클리어: {isFirstClear})");
                }
            }
        }
        
        /// <summary>
        /// 보상 지급
        /// </summary>
        private void ApplyRewards(RewardResult result)
        {
            // 골드 지급
            if (result.Gold > 0)
            {
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.AddGold(result.Gold);
                    if (enableDebugLogs)
                    {
                        Debug.Log($"💰 [RewardSystem] 골드 지급 완료: +{result.Gold}");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ [RewardSystem] PlayerDataManager를 찾을 수 없어 골드 지급 실패");
                }
                OnGoldRewarded?.Invoke(result.Gold);
            }
            
            // EXP 지급
            if (result.Exp > 0)
            {
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.AddExp(result.Exp);
                    if (enableDebugLogs)
                    {
                        Debug.Log($"✨ [RewardSystem] 경험치 지급 완료: +{result.Exp}");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ [RewardSystem] PlayerDataManager를 찾을 수 없어 경험치 지급 실패");
                }
                OnExpRewarded?.Invoke(result.Exp);
            }
            
            // 아이템 지급 (V2 시스템 사용 - 장비 + 재료)
            if (result.Items.Count > 0)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    Debug.Log($"🔍 [DEBUG - RewardSystem] 아이템 보상 처리 시작!");
                    Debug.Log($"  result.Items.Count: {result.Items.Count}");
                    for (int debugIdx = 0; debugIdx < result.Items.Count; debugIdx++)
                    {
                        Debug.Log($"    [{debugIdx}] ItemID: {result.Items[debugIdx].ItemID}, Amount: {result.Items[debugIdx].Amount}");
                    }
                    Debug.Log($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                }
                
                if (PlayerDataManager.Instance != null && AccountDataManager.Instance != null)
                {
                    int equipmentCount = 0;
                    int materialCount = 0;
                    int unknownCount = 0;
                    
                    foreach (var itemData in result.Items)
                    {
                        string rawId = itemData.ItemID;

                        // ══════════════════════════════════════════════════════════
                        // ⭐ 0단계: GEN_EQUIP 키워드 — 동적 장비 생성 분기
                        //
                        //   DropTable 의 ItemID 가 "GEN_EQUIP_{minRank}_{maxRank}" 형식이면
                        //   ItemGenerator 를 통해 플레이어 클래스·등급에 맞는 장비를 생성합니다.
                        //
                        //   예: "GEN_EQUIP_B_S"  → B(희귀)~S(전설) 범위에서 랜덤 생성
                        //       "GEN_EQUIP_D_A"  → D(일반)~A(영웅) 범위에서 랜덤 생성
                        //       "GEN_EQUIP"      → RewardResult 의 MinRarity ~ MaxRarity 사용
                        // ══════════════════════════════════════════════════════════
                        if (rawId.StartsWith("GEN_EQUIP", System.StringComparison.OrdinalIgnoreCase))
                        {
                            GrantGeneratedEquipment(rawId, itemData.Amount, result);
                            equipmentCount += itemData.Amount;
                            continue;
                        }

                        // ⭐ 1단계: 장비 아이템 확인 (고정 ID 방식 — 기존 로직 유지)
                        string templateName = rawId;
                        var equipmentData = ItemTemplateResolver.Load(templateName);
                        
                        if (equipmentData != null)
                        {
                            // ✅ 장비 아이템: Amount만큼 개별 인스턴스 생성
                            int itemSuccessCount = 0;
                            for (int i = 0; i < itemData.Amount; i++)
                            {
                                ItemInstanceID newItemId = PlayerDataManager.Instance.AddItemV2(templateName, 0, true);
                                
                                if (!newItemId.IsEmpty)
                                {
                                    equipmentCount++;
                                    itemSuccessCount++;
                                    result.ItemInstanceIds.Add(newItemId);
                                }
                                else
                                {
                                    Debug.LogWarning($"⚠️ [RewardSystem] 장비 지급 실패: {rawId} (#{i+1}/{itemData.Amount})");
                                }
                            }
                            
                            if (itemSuccessCount > 0 && enableDebugLogs)
                            {
                                Debug.Log($"🎁 [RewardSystem] 장비 지급 완료: {rawId} x{itemSuccessCount}");
                            }
                        }
                        else
                        {
                            // ⭐ 2단계: 재료 아이템 확인 (MaterialDatabase)
                            var materialData = MaterialDatabase.Instance?.GetDataById(rawId);
                            
                            if (materialData != null)
                            {
                                // ✅ 재료 아이템: AccountDataManager.AddMaterial() 호출
                                AccountDataManager.Instance.AddMaterial(materialData.materialType, itemData.Amount);
                                materialCount += itemData.Amount;
                                
                                // ⭐ UI 표시용 MaterialRewards에 추가
                                var materialStack = new MaterialStack
                                {
                                    materialType = materialData.materialType,
                                    count = itemData.Amount
                                };
                                result.MaterialRewards.Add(materialStack);
                                
                                if (enableDebugLogs)
                                {
                                    Debug.Log($"🎁 [RewardSystem] 재료 지급 완료: {materialData.displayName} x{itemData.Amount} (MaterialType: {materialData.materialType})");
                                }
                            }
                            else
                            {
                                // ❌ 알 수 없는 아이템
                                unknownCount++;
                                Debug.LogWarning($"⚠️ [RewardSystem] 알 수 없는 아이템 (장비도 재료도 아님): {rawId}");
                            }
                        }
                    }
                    
                    // 최종 요약 로그
                    if (enableDebugLogs)
                    {
                        Debug.Log($"🎁 [RewardSystem] 아이템 지급 완료: 장비 {equipmentCount}개, 재료 {materialCount}개, 미확인 {unknownCount}개");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ [RewardSystem] PlayerDataManager 또는 AccountDataManager를 찾을 수 없어 아이템 지급 실패");
                }
                OnItemsRewarded?.Invoke(result.Items);
            }
        }
        
        // ─────────────────────────────────────────────────────────────
        // GEN_EQUIP 동적 장비 생성 헬퍼
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// "GEN_EQUIP_{minRank}_{maxRank}" 형식의 키워드를 파싱하여
        /// ItemGenerator 로 장비를 생성하고 플레이어 인벤토리에 지급합니다.
        /// </summary>
        /// <param name="keyword">GEN_EQUIP 키워드 문자열</param>
        /// <param name="amount">지급 횟수 (DropItemData.Amount)</param>
        /// <param name="result">결과 객체 (ItemInstanceIds 추가용)</param>
        private void GrantGeneratedEquipment(string keyword, int amount, RewardResult result)
        {
            if (global::ItemGenerator.Instance == null)
            {
                Debug.LogWarning("[RewardSystem] ItemGenerator.Instance 가 null 입니다. GEN_EQUIP 처리를 건너뜁니다.");
                return;
            }

            // 키워드에서 등급 범위 파싱 ("GEN_EQUIP_B_S" → min=B, max=S)
            EquipmentRank minRank = result.MinRarity;
            EquipmentRank maxRank = result.MaxRarity;
            ParseGenEquipKeyword(keyword, ref minRank, ref maxRank);

            // 현재 플레이어 타입 조회
            PlayerType playerType = PlayerDataManager.Instance != null
                ? PlayerDataManager.Instance.GetCurrentPlayerType()
                : PlayerType.Warrior;

            // 현재 스테이지 Config
            StageConfig stageConfig = StageManager.Instance?.CurrentStageConfig;

            var request = new global::EquipmentGenerationRequest
            {
                minRarity  = minRank,
                maxRarity  = maxRank,
                playerType = playerType,
                stageConfig = stageConfig,
            };

            for (int i = 0; i < amount; i++)
            {
                global::GenerationResult genResult = global::ItemGenerator.Instance.Generate(request);

                if (!genResult.isValid)
                {
                    Debug.LogWarning($"[RewardSystem] GEN_EQUIP 생성 실패 (#{i + 1}/{amount})");
                    continue;
                }

                // 인벤토리에 추가
                ItemInstanceID newItemId = PlayerDataManager.Instance.AddItemV2(genResult.templateId, 0, true);

                if (!newItemId.IsEmpty)
                {
                    result.ItemInstanceIds.Add(newItemId);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"🎁 [RewardSystem] GEN_EQUIP 지급: {genResult.templateId} " +
                                  $"| {genResult.rank.GetRankName()} | Soulbound: {genResult.isSoulbound}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[RewardSystem] GEN_EQUIP AddItemV2 실패: {genResult.templateId}");
                }
            }
        }

        /// <summary>
        /// GEN_EQUIP 키워드에서 등급 범위를 파싱합니다.
        ///
        /// 포맷 예시:
        ///   "GEN_EQUIP"       → minRank, maxRank 변경 없음 (RewardResult 값 그대로 사용)
        ///   "GEN_EQUIP_B_S"   → minRank = B, maxRank = S
        ///   "GEN_EQUIP_D"     → minRank = D (maxRank 변경 없음)
        /// </summary>
        private void ParseGenEquipKeyword(string keyword, ref EquipmentRank minRank, ref EquipmentRank maxRank)
        {
            // "GEN_EQUIP_B_S" → ["GEN", "EQUIP", "B", "S"]
            string[] parts = keyword.ToUpper().Split('_');

            // parts[0]="GEN", parts[1]="EQUIP", parts[2]=minRank(optional), parts[3]=maxRank(optional)
            if (parts.Length >= 3 && System.Enum.TryParse(parts[2], out EquipmentRank parsedMin))
                minRank = parsedMin;

            if (parts.Length >= 4 && System.Enum.TryParse(parts[3], out EquipmentRank parsedMax))
                maxRank = parsedMax;

            // 최소가 최대보다 높으면 교정
            if ((int)minRank > (int)maxRank)
                maxRank = minRank;
        }

        /// <summary>
        /// EquipmentData 로드 (EquipmentData ID 직접 사용)
        /// </summary>
        private EquipmentData LoadEquipmentData(string itemId)
        {
            EquipmentData equipment = null;
            
            // 1. Equipment 폴더에서 직접 로드
            equipment = Resources.Load<EquipmentData>($"Equipment/{itemId}");
            
            // 2. EquipmentData 폴더에서 로드 시도
            if (equipment == null)
            {
                equipment = Resources.Load<EquipmentData>($"EquipmentData/{itemId}");
            }
            
            // 3. 로드 실패 시 디버그 정보
            if (equipment == null && enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ [RewardSystem] EquipmentData 로드 실패: {itemId}");
                Debug.LogWarning($"   시도한 경로들:");
                Debug.LogWarning($"   - Equipment/{itemId}");
                Debug.LogWarning($"   - EquipmentData/{itemId}");
            }
            
            return equipment;
        }
        
        /// <summary>
        /// 보상 결과 데이터
        /// </summary>
        [System.Serializable]
        public class RewardResult
        {
            public int Gold;
            public int Exp;
            public List<DropItemData> Items = new List<DropItemData>();
            public List<ItemInstanceID> ItemInstanceIds = new List<ItemInstanceID>();
            public List<MaterialStack> MaterialRewards = new List<MaterialStack>();
            public bool IsFirstClear;
            public float ClearTime;

            // ── RewardCalculator 가 채워주는 드롭 파라미터 ──────────
            /// <summary>장비 드롭 확률 배율 (레벨 구간 + 타입 보정 결과)</summary>
            public float EquipmentChanceMultiplier = 1f;
            /// <summary>재료 수량 배율</summary>
            public float MaterialAmountMultiplier  = 1f;
            /// <summary>드롭 가능 최소 장비 등급</summary>
            public ItemSystem.EquipmentRank MinRarity = ItemSystem.EquipmentRank.D;
            /// <summary>드롭 가능 최대 장비 등급</summary>
            public ItemSystem.EquipmentRank MaxRarity = ItemSystem.EquipmentRank.A;
        }
    }
}
