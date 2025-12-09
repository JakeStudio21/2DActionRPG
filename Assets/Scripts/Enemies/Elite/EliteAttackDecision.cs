using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 엘리트 몬스터 공격 결정 시스템
/// 평타 vs 스킬 확률 기반 선택
/// 스킬 여러 개일 때 가중치 선택
/// </summary>
public class EliteAttackDecision
{
    private bool enableDebugLogs;
    
    public EliteAttackDecision(bool debug = false)
    {
        enableDebugLogs = debug;
    }

    /// <summary>
    /// 공격 타입 결정 (평타 or 스킬)
    /// </summary>
    public enum AttackDecisionType
    {
        MeleeAttack,    // 평타
        UseSkill        // 스킬
    }

    /// <summary>
    /// 공격 결정 결과
    /// </summary>
    public class DecisionResult
    {
        public AttackDecisionType DecisionType;
        public SkillData SelectedSkill;  // 스킬 선택 시
        public string Reason;            // 결정 이유 (디버그용)
        
        public bool IsSkill => DecisionType == AttackDecisionType.UseSkill;
        public bool IsMelee => DecisionType == AttackDecisionType.MeleeAttack;
    }

    /// <summary>
    /// 메인 결정 메서드 - 평타 vs 스킬 결정
    /// </summary>
    public DecisionResult DecideAttack(
        EnemyData enemyData, 
        EliteSkillController skillController,
        float distanceToPlayer)
    {
        // 기본값: 평타
        DecisionResult result = new DecisionResult
        {
            DecisionType = AttackDecisionType.MeleeAttack,
            SelectedSkill = null,
            Reason = "기본값 (평타)"
        };

        // 스킬 데이터 없으면 평타
        if (enemyData == null || !enemyData.HasSkillData)
        {
            result.Reason = "스킬 데이터 없음";
            return result;
        }

        // 확률 계산
        int meleeProb = enemyData.MeleeAttackProbability;
        int skillProb = enemyData.SkillUseProbability;
        int total = meleeProb + skillProb;

        if (total == 0)
        {
            result.Reason = "확률 합계 0";
            return result;
        }

        // 랜덤 선택
        int randomValue = Random.Range(0, total);

        if (randomValue < meleeProb)
        {
            // 평타 선택
            result.DecisionType = AttackDecisionType.MeleeAttack;
            result.Reason = $"확률 선택: 평타 ({meleeProb}/{total})";
            
            if (enableDebugLogs)
            {
                Debug.Log($"[EliteAttackDecision] {result.Reason}");
            }
            
            return result;
        }
        else
        {
            // 스킬 선택 시도
            SkillData selectedSkill = DecideSkill(enemyData, skillController, distanceToPlayer);
            
            if (selectedSkill != null)
            {
                result.DecisionType = AttackDecisionType.UseSkill;
                result.SelectedSkill = selectedSkill;
                result.Reason = $"스킬 선택: {selectedSkill.SkillName} ({skillProb}/{total})";
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[EliteAttackDecision] {result.Reason}");
                }
            }
            else
            {
                // 스킬 사용 불가 시 평타로 fallback
                result.DecisionType = AttackDecisionType.MeleeAttack;
                result.Reason = "스킬 사용 불가 → 평타로 fallback";
                
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[EliteAttackDecision] {result.Reason}");
                }
            }
            
            return result;
        }
    }

    /// <summary>
    /// 스킬 선택 - 여러 스킬 중 하나 선택
    /// </summary>
    private SkillData DecideSkill(
        EnemyData enemyData, 
        EliteSkillController skillController,
        float distanceToPlayer)
    {
        if (enemyData.SkillDataList == null || enemyData.SkillDataList.Count == 0)
        {
            return null;
        }

        // 사용 가능한 스킬 필터링
        List<SkillData> availableSkills = new List<SkillData>();
        List<int> availableProbabilities = new List<int>();

        for (int i = 0; i < enemyData.SkillDataList.Count; i++)
        {
            SkillData skill = enemyData.SkillDataList[i];
            
            if (skill == null) continue;

            // 쿨다운 체크
            if (skillController != null && !skillController.CanUseSkill(skill))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[EliteAttackDecision] {skill.SkillName}: 쿨다운 중");
                }
                continue;
            }

            // 거리 체크
            if (!skill.IsInRange(distanceToPlayer))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[EliteAttackDecision] {skill.SkillName}: 범위 밖 (현재: {distanceToPlayer:F1}, 범위: {skill.MinRange}~{skill.MaxRange})");
                }
                continue;
            }

            // 사용 가능한 스킬 추가
            availableSkills.Add(skill);
            
            // 확률 가져오기
            int probability = i < enemyData.SkillProbabilities.Count ? enemyData.SkillProbabilities[i] : 100;
            availableProbabilities.Add(probability);
        }

        // 사용 가능한 스킬 없으면 null
        if (availableSkills.Count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[EliteAttackDecision] 사용 가능한 스킬 없음");
            }
            return null;
        }

        // 스킬 1개면 바로 선택
        if (availableSkills.Count == 1)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[EliteAttackDecision] 유일한 스킬 선택: {availableSkills[0].SkillName}");
            }
            return availableSkills[0];
        }

        // 여러 스킬 중 가중치 선택
        return SelectSkillByWeight(availableSkills, availableProbabilities);
    }

    /// <summary>
    /// 가중치 기반 스킬 선택
    /// </summary>
    private SkillData SelectSkillByWeight(List<SkillData> skills, List<int> probabilities)
    {
        // 확률 합계 계산
        int totalWeight = 0;
        foreach (int prob in probabilities)
        {
            totalWeight += prob;
        }

        if (totalWeight == 0)
        {
            // 균등 선택
            int randomIndex = Random.Range(0, skills.Count);
            return skills[randomIndex];
        }

        // 가중치 기반 랜덤 선택
        int randomValue = Random.Range(0, totalWeight);
        int cumulativeWeight = 0;

        for (int i = 0; i < skills.Count; i++)
        {
            cumulativeWeight += probabilities[i];
            
            if (randomValue < cumulativeWeight)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[EliteAttackDecision] 가중치 선택: {skills[i].SkillName} ({probabilities[i]}/{totalWeight})");
                }
                return skills[i];
            }
        }

        // Fallback (마지막 스킬)
        return skills[skills.Count - 1];
    }

    /// <summary>
    /// 강제 평타 결정 (스킬 사용 불가 시)
    /// </summary>
    public static DecisionResult ForceMeleeAttack(string reason = "강제 평타")
    {
        return new DecisionResult
        {
            DecisionType = AttackDecisionType.MeleeAttack,
            SelectedSkill = null,
            Reason = reason
        };
    }

    /// <summary>
    /// 강제 스킬 결정 (테스트용)
    /// </summary>
    public static DecisionResult ForceSkill(SkillData skill, string reason = "강제 스킬")
    {
        return new DecisionResult
        {
            DecisionType = AttackDecisionType.UseSkill,
            SelectedSkill = skill,
            Reason = reason
        };
    }

    /// <summary>
    /// 통계 정보 (디버그용)
    /// </summary>
    public class AttackStatistics
    {
        public int TotalAttacks = 0;
        public int MeleeAttacks = 0;
        public int SkillAttacks = 0;
        public Dictionary<string, int> SkillUsageCount = new Dictionary<string, int>();

        public float MeleePercentage => TotalAttacks > 0 ? (float)MeleeAttacks / TotalAttacks * 100f : 0f;
        public float SkillPercentage => TotalAttacks > 0 ? (float)SkillAttacks / TotalAttacks * 100f : 0f;

        public void RecordMelee()
        {
            TotalAttacks++;
            MeleeAttacks++;
        }

        public void RecordSkill(string skillName)
        {
            TotalAttacks++;
            SkillAttacks++;
            
            if (!SkillUsageCount.ContainsKey(skillName))
            {
                SkillUsageCount[skillName] = 0;
            }
            SkillUsageCount[skillName]++;
        }

        public string GetSummary()
        {
            string summary = $"=== Attack Statistics ===\n";
            summary += $"Total: {TotalAttacks}\n";
            summary += $"Melee: {MeleeAttacks} ({MeleePercentage:F1}%)\n";
            summary += $"Skill: {SkillAttacks} ({SkillPercentage:F1}%)\n";
            
            if (SkillUsageCount.Count > 0)
            {
                summary += $"\n=== Skill Usage ===\n";
                foreach (var pair in SkillUsageCount)
                {
                    float percentage = (float)pair.Value / SkillAttacks * 100f;
                    summary += $"{pair.Key}: {pair.Value} ({percentage:F1}%)\n";
                }
            }
            
            return summary;
        }

        public void Reset()
        {
            TotalAttacks = 0;
            MeleeAttacks = 0;
            SkillAttacks = 0;
            SkillUsageCount.Clear();
        }
    }
}


