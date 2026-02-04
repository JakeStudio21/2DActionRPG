using System.Collections.Generic;

namespace Systems
{
    /// <summary>
    /// 합성 경고 데이터 (강화된 아이템 합성 시)
    /// </summary>
    [System.Serializable]
    public class FusionWarningData
    {
        public List<ItemInstanceId> materialIds;
        public string targetTemplateName;
        public ItemGrade currentGrade;
        public ItemGrade nextGrade;
        public int requiredCount;
        public int fusionCost;
        public int maxEnhancementLevel; // 재료 중 최대 강화 수치
        public string warningMessage;
        public string tooltipMessage;

        public FusionWarningData(
            List<ItemInstanceId> materialIds,
            string targetTemplateName,
            ItemGrade currentGrade,
            ItemGrade nextGrade,
            int requiredCount,
            int fusionCost,
            int maxEnhancementLevel)
        {
            this.materialIds = materialIds;
            this.targetTemplateName = targetTemplateName;
            this.currentGrade = currentGrade;
            this.nextGrade = nextGrade;
            this.requiredCount = requiredCount;
            this.fusionCost = fusionCost;
            this.maxEnhancementLevel = maxEnhancementLevel;
            GenerateMessages();
        }

        private void GenerateMessages()
        {
            var messages = new List<string>();
            var tooltips = new List<string>();

            messages.Add($"<b>{targetTemplateName}</b> 아이템을 합성하시겠습니까?");
            tooltips.Add($"합성: {currentGrade} → {nextGrade}");

            messages.Add($"\n재료: {currentGrade}등급 x{requiredCount}");
            messages.Add($"비용: <color=yellow>{fusionCost} 골드</color>");
            messages.Add($"결과: <color=green>{nextGrade}등급</color> 아이템 1개");

            // 강화 경고
            if (maxEnhancementLevel > 0)
            {
                messages.Add($"\n<color=red>⚠️ 경고</color>");
                messages.Add($"<color=orange>재료 중 <b>+{maxEnhancementLevel}</b>까지 강화된 아이템이 있습니다.</color>");
                messages.Add($"<color=yellow>합성 시 강화 단계는 복구되지 않습니다.</color>");
                tooltips.Add($"강화 손실: +{maxEnhancementLevel} → +0");
            }

            messages.Add("\n계속하시겠습니까?");

            warningMessage = string.Join("\n", messages);
            tooltipMessage = string.Join("\n", tooltips);
        }
    }
}

