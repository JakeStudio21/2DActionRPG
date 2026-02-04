using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 강화 경고 데이터
    /// - 파괴 위험 경고
    /// - 하락 위험 경고
    /// </summary>
    [System.Serializable]
    public class EnhancementWarningData
    {
        public ItemInstanceId instanceId;
        public string itemTemplateName;
        public ItemGrade itemGrade;
        public int currentLevel;
        public int targetLevel;
        public float successRate;
        public EnhancementFailureType failureType;
        
        public int requiredMaterialAmount;
        public MaterialType requiredMaterialType;
        public int requiredGold;
        
        public string warningMessage;
        public string tooltipMessage;
        
        public EnhancementWarningData(
            ItemInstanceId instanceId,
            string templateName,
            ItemGrade grade,
            int currentLevel,
            int targetLevel,
            float successRate,
            EnhancementFailureType failureType,
            int materialAmount,
            MaterialType materialType,
            int goldCost)
        {
            this.instanceId = instanceId;
            this.itemTemplateName = templateName;
            this.itemGrade = grade;
            this.currentLevel = currentLevel;
            this.targetLevel = targetLevel;
            this.successRate = successRate;
            this.failureType = failureType;
            this.requiredMaterialAmount = materialAmount;
            this.requiredMaterialType = materialType;
            this.requiredGold = goldCost;
            
            GenerateMessages();
        }
        
        private void GenerateMessages()
        {
            var messages = new System.Collections.Generic.List<string>();
            var tooltips = new System.Collections.Generic.List<string>();
            
            messages.Add($"<b>{itemTemplateName} +{currentLevel}</b>을(를) <b>+{targetLevel}</b>로 강화하시겠습니까?");
            tooltips.Add($"아이템: {itemTemplateName} +{currentLevel}");
            tooltips.Add($"목표: +{targetLevel}");
            
            // 성공률 표시
            messages.Add($"\n성공 확률: <color=yellow>{successRate:F1}%</color>");
            tooltips.Add($"성공률: {successRate:F1}%");
            
            // 실패 시 처리 경고
            switch (failureType)
            {
                case EnhancementFailureType.Maintain:
                    messages.Add($"<color=green>실패 시: 강화 레벨 유지</color>");
                    tooltips.Add("실패 시: 유지");
                    break;
                    
                case EnhancementFailureType.Downgrade:
                    messages.Add($"<color=orange>⚠️ 실패 시: 1단계 하락 (+{currentLevel - 1})</color>");
                    tooltips.Add($"실패 시: {currentLevel - 1}단계로 하락");
                    break;
                    
                case EnhancementFailureType.Destroy:
                    messages.Add($"<color=red>💥 실패 시: 아이템 파괴!</color>");
                    messages.Add($"<color=red><b>이 아이템을 영구히 잃게 됩니다!</b></color>");
                    tooltips.Add("실패 시: 아이템 파괴 (복구 불가)");
                    break;
            }
            
            // 비용 표시
            messages.Add($"\n필요 재료: <color=cyan>{requiredMaterialType.GetDisplayName()} x {requiredMaterialAmount}</color>");
            messages.Add($"필요 골드: <color=yellow>{requiredGold}</color>");
            tooltips.Add($"재료: {requiredMaterialType.GetDisplayName()} x {requiredMaterialAmount}");
            tooltips.Add($"골드: {requiredGold}");
            
            messages.Add("\n계속하시겠습니까?");
            
            warningMessage = string.Join("\n", messages);
            tooltipMessage = string.Join("\n", tooltips);
        }
    }
}

