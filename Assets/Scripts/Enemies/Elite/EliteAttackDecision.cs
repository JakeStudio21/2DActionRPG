using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 엘리트 몬스터 공격 결정 시스템
/// 평타 vs 스킬 확률 기반 선택
/// 스킬 여러 개일 때 가중치 선택
/// </summary>
public class EliteAttackDecision
{
    public EliteAttackDecision()
    {
    }

    /// <summary>
    /// 공격 타입 결정 (평타 or 스킬 or 대기)
    /// </summary>
    public enum AttackDecisionType
    {
        MeleeAttack,    // 평타
        UseSkill,       // 스킬
        Skip            // 이번 턴 공격 없음 (원거리 + 스킬 쿨다운)
    }

    /// <summary>
    /// 공격 결정 결과
    /// </summary>
    public class DecisionResult
    {
        public AttackDecisionType DecisionType;
        public SkillData SelectedSkill;  // 스킬 선택 시
        public string Reason;            // 결정 이유 (디버그용)

        public bool IsSkill  => DecisionType == AttackDecisionType.UseSkill;
        public bool IsMelee  => DecisionType == AttackDecisionType.MeleeAttack;
        public bool IsSkip   => DecisionType == AttackDecisionType.Skip;
    }

    /// <summary>
    /// 메인 결정 메서드 - 평타 vs 스킬 결정
    /// </summary>
    /// <param name="enemyData">몬스터 데이터</param>
    /// <param name="skillController">스킬 컨트롤러 (쿨다운 체크용)</param>
    /// <param name="distanceToPlayer">현재 플레이어까지 거리</param>
    /// <param name="meleeRange">평타 유효 사거리 (이 거리 밖이면 평타 제외)</param>
    public DecisionResult DecideAttack(
        EnemyData enemyData,
        EliteSkillController skillController,
        float distanceToPlayer,
        float meleeRange = 1.8f)
    {
        // 기본값: 평타
        DecisionResult result = new DecisionResult
        {
            DecisionType = AttackDecisionType.MeleeAttack,
            SelectedSkill = null,
            Reason = "기본값 (평타)"
        };

        // ─────────────────────────────────────────────────────
        // ⭐ 거리 기반 1차 분기
        //    플레이어가 평타 사거리 밖이면 → 스킬만 시도
        //    스킬도 없으면 → Skip (이번 턴 공격 없음, 짧은 대기)
        // ─────────────────────────────────────────────────────
        bool playerOutOfMeleeRange = distanceToPlayer > meleeRange;

        if (playerOutOfMeleeRange)
        {

            SkillData selectedSkill = null;

            if (enemyData != null && enemyData.HasSkillData)
                selectedSkill = DecideSkill(enemyData, skillController, distanceToPlayer);

            if (selectedSkill != null)
            {
                result.DecisionType = AttackDecisionType.UseSkill;
                result.SelectedSkill = selectedSkill;
                result.Reason = $"원거리 → 강제 스킬: {selectedSkill.SkillName}";
            }
            else
            {
                // 스킬도 쿨다운이면 이번 턴 skip
                result.DecisionType = AttackDecisionType.Skip;
                result.Reason = $"원거리({distanceToPlayer:F1}) + 스킬 쿨다운 → 공격 대기";
            }


            return result;
        }

        // ─────────────────────────────────────────────────────
        // 근접 범위: 기존 확률 기반 (평타 60% / 스킬 40%)
        // ─────────────────────────────────────────────────────

        // 스킬 데이터 없으면 평타
        if (enemyData == null || !enemyData.HasSkillData)
        {
            result.Reason = "스킬 데이터 없음 → 평타";
            return result;
        }

        // 확률 계산
        int meleeProb = enemyData.MeleeAttackProbability;
        int skillProb = enemyData.SkillUseProbability;
        int total = meleeProb + skillProb;

        if (total == 0)
        {
            result.Reason = "확률 합계 0 → 평타";
            return result;
        }

        // 랜덤 선택
        int randomValue = Random.Range(0, total);

        if (randomValue < meleeProb)
        {
            result.DecisionType = AttackDecisionType.MeleeAttack;
            result.Reason = $"확률 선택: 평타 ({meleeProb}/{total})";


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

            }
            else
            {
                // 스킬 사용 불가 시 평타로 fallback
                result.DecisionType = AttackDecisionType.MeleeAttack;
                result.Reason = "스킬 사용 불가 → 평타로 fallback";

                    Debug.LogWarning($"[EliteAttackDecision] {result.Reason}");
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
                continue;
            }

            // 거리 체크
            if (!skill.IsInRange(distanceToPlayer))
            {
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
            return null;
        }

        // 스킬 1개면 바로 선택
        if (availableSkills.Count == 1)
        {
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


