using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            
            if (rewardTable != null)
            {
                // 기본 보상
                result.Gold = rewardTable.Gold;
                result.Exp = rewardTable.Exp;
                
                // 클리어 시간 보너스 (빠를수록 보너스)
                float timeBonus = CalculateTimeBonus(clearTime, stageConfig.TimeLimitSec);
                result.Gold = Mathf.RoundToInt(result.Gold * timeBonus);
                result.Exp = Mathf.RoundToInt(result.Exp * timeBonus);
            }
            else
            {
                // 기본값 사용
                result.Gold = baseGold;
                result.Exp = baseExp;
            }
            
            // 골드/EXP 커브 적용
            result.Gold = Mathf.RoundToInt(result.Gold * goldMultiplier);
            result.Exp = Mathf.RoundToInt(result.Exp * expMultiplier);
            
            if (enableDebugLogs)
            {
                Debug.Log($"💰 [RewardSystem] 기본 보상: 골드 {result.Gold}, EXP {result.Exp}");
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
                // 확률 기반 아이템 드롭
                var droppedItems = rewardTable.RollDrops();
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
            
            // 아이템 지급 (V2 시스템 사용)
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
                
                if (PlayerDataManager.Instance != null)
                {
                    int successCount = 0;
                    int skippedCount = 0;
                    foreach (var itemData in result.Items)
                    {
                        string rawId = itemData.ItemID;
                        
                        // ⭐ templateName = itemID 그대로 사용 (ItemTemplateResolver가 자동으로 Asset 파일 찾음)
                        // 예: "ITEM_ARMOR_WIZARD_B" → ItemTemplateResolver가 "Equipment/ITEM_ARMOR_WIZARD_B_Equipment" 찾아줌
                        string templateName = rawId;
                        
                        if (enableDebugLogs)
                            Debug.Log($"🔍 [DEBUG - RewardSystem] 아이템 지급 - rawId: {rawId} → templateName: {templateName}, Amount: {itemData.Amount}");
                        
                        // ⭐ 장비 아이템인지 확인 (V2 시스템은 장비 전용)
                        var equipmentData = ItemTemplateResolver.Load(templateName);
                        if (equipmentData == null)
                        {
                            if (enableDebugLogs)
                                Debug.LogWarning($"⚠️ [RewardSystem] 장비 아이템이 아니므로 V2 시스템 건너뜀: {rawId}");
                            skippedCount++;
                            continue;
                        }
                        
                        // ⭐ 장비 아이템: Amount만큼 개별 인스턴스 생성
                        int itemSuccessCount = 0;
                        for (int i = 0; i < itemData.Amount; i++)
                        {
                            ItemInstanceID newItemId = PlayerDataManager.Instance.AddItemV2(templateName, 0, true);
                            
                            if (!newItemId.IsEmpty)
                            {
                                successCount++;
                                itemSuccessCount++;
                                result.ItemInstanceIds.Add(newItemId);  // ⭐ 추가: 생성된 인스턴스 ID 저장
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ [RewardSystem] 아이템 지급 실패: {rawId} (#{i+1}/{itemData.Amount})");
                            }
                        }
                        
                        if (itemSuccessCount > 0)
                        {
                            Debug.Log($"🎁 [RewardSystem] 아이템 지급 완료 (V2): {rawId} x{itemSuccessCount}");
                        }
                    }
                    
                    if (successCount > 0 || skippedCount > 0)
                    {
                        Debug.Log($"🎁 [RewardSystem] 아이템 지급 완료: 장비 {successCount}개 성공, 기타 {skippedCount}개 건너뜀");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ [RewardSystem] PlayerDataManager를 찾을 수 없어 아이템 지급 실패");
                }
                OnItemsRewarded?.Invoke(result.Items);
            }
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
            public List<ItemInstanceID> ItemInstanceIds = new List<ItemInstanceID>();  // ⭐ 추가: 생성된 아이템 인스턴스 ID 목록
            public bool IsFirstClear;
            public float ClearTime;
        }
    }
}
