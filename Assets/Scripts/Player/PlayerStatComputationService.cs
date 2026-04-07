using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 스탯 공통 계산 서비스 (순수 C# 정적 헬퍼)
/// 
/// - PlayerRuntimeStats (인게임 MonoBehaviour) 와
///   LobbyEquippedItemsUI (로비 UI) 양쪽에서 동일한 공식으로 스탯을 계산한다.
/// - MonoBehaviour에 의존하지 않으므로 씬에 Player 프리팹 없이도 호출 가능하다.
///
/// 계산 파이프라인 (PlayerRuntimeStats 와 동일 순서):
///   1. 기본 스탯 (레벨 × 클래스 데이터)
///   2. 장비 StatModifier 합산
///   3. 패시브 스킬 보너스
///   4. 클래스 배율 적용
///   5. Clamp / 최종 검증
/// </summary>
public static class PlayerStatComputationService
{
    // ──────────────────────────────────────────
    // 공개 진입점
    // ──────────────────────────────────────────

    /// <summary>
    /// 선택된 슬롯의 최종 스탯을 계산해 스냅샷으로 반환한다.
    /// PlayerDataManager.selectedPlayerData 와 AccountDataManager 가 준비된 상태에서 호출해야 한다.
    /// </summary>
    public static PlayerFinalStatsSnapshot Compute(
        SelectedPlayerData playerData,
        PlayerType playerType)
    {
        if (playerData == null)
            return BuildDefaultSnapshot();

        BaseClassData classData = LoadClassData(playerType);

        var snapshot = new PlayerFinalStatsSnapshot();

        // 1단계: 기본 스탯
        ComputeBaseStats(ref snapshot, playerData, classData);

        // 2단계: 장비 StatModifier
        ApplyEquipmentStats(ref snapshot, playerData);

        // 3단계: 패시브 스킬 보너스
        ApplyPassiveSkillStats(ref snapshot, playerData);

        // 4단계: 클래스 배율
        ApplyClassMultipliers(ref snapshot, classData);

        // 5단계: 최종 Clamp
        ValidateSnapshot(ref snapshot);

        return snapshot;
    }

    // ──────────────────────────────────────────
    // 단계별 내부 메서드
    // ──────────────────────────────────────────

    private static void ComputeBaseStats(
        ref PlayerFinalStatsSnapshot s,
        SelectedPlayerData playerData,
        BaseClassData classData)
    {
        int level = playerData.currentLevel;

        // 공격력 (목표 역산 성장식)
        s.AttackDamage = classData != null
            ? classData.GetCharacterBaseAttack(level) + (level - 1) * 2f
            : 10f + (level - 1) * 2f;

        // 체력
        float baseHp     = classData != null ? classData.baseMaxHealth   : 200f;
        float hpPerLevel = classData != null ? classData.hpGainPerLevel  : 20f;
        s.MaxHealth = baseHp + (level - 1) * hpPerLevel;

        // 방어력
        float baseDef     = classData != null ? classData.baseDefense          : 5f;
        float defPerLevel = classData != null ? classData.defenseGainPerLevel  : 1f;
        s.Defense = baseDef + (level - 1) * defPerLevel;

        // 고정 스탯
        s.CriticalChance  = classData != null ? classData.baseCritRate     : 0.05f;
        s.CriticalDamage  = classData != null ? classData.baseCritDamage   : 1.5f;
        s.AttackSpeed     = classData != null ? classData.baseAttackSpeed  : 1.0f;
        s.MoveSpeed       = classData != null ? classData.baseMoveSpeed    : 4f;
        s.HealMultiplier  = classData != null ? classData.healMultiplier   : 1.0f;

        // 특수 스탯 초기화
        s.MoveSpeedPercentBonus  = 0f;
        s.SkillDamageBonus       = 0f;
        s.CooldownReduction      = 0f;
        s.DamageReduction        = 0f;
        s.HpRegen                = 0f;
        s.LifeSteal              = 0f;
        s.ArmorPenetration       = 0f;
        s.DodgeChance            = 0f;
        s.BlockChance            = 0f;
        s.ExpGainBonus           = 0f;
        s.StatusResist           = 0f;
        s.PierceDamageRetention  = 0.5f;
    }

    private static void ApplyEquipmentStats(
        ref PlayerFinalStatsSnapshot s,
        SelectedPlayerData playerData)
    {
        if (AccountDataManager.Instance == null) return;

        var equippedItems       = playerData.RuntimeEquippedItems;
        var equippedInstanceIds = playerData.RuntimeEquippedInstanceIds;

        foreach (var kvp in equippedItems)
        {
            EquipmentSlot slot      = kvp.Key;
            EquipmentData equipment = kvp.Value;
            if (equipment == null) continue;

            List<StatModifier> modifiers = null;

            if (equippedInstanceIds.TryGetValue(slot, out ItemInstanceID instanceId) &&
                !instanceId.IsEmpty)
            {
                var equipInstance = AccountDataManager.Instance.CreateEquipmentInstance(instanceId);
                if (equipInstance != null)
                    modifiers = equipInstance.GetStatModifiers();
            }

            modifiers ??= equipment.GetStatModifiers();

            foreach (var mod in modifiers)
                ApplySingleModifier(ref s, mod.statType, mod.value);
        }
    }

    private static void ApplyPassiveSkillStats(
        ref PlayerFinalStatsSnapshot s,
        SelectedPlayerData playerData)
    {
        if (SkillLevelDataLoader.Instance == null) return;

        string[] passiveIds = playerData.equippedPassiveSkillIds;
        if (passiveIds == null) return;

        // SelectedPlayerData에는 SkillInstance가 없으므로 AccountDataManager에서 조회
        if (AccountDataManager.Instance == null) return;

        foreach (string skillID in passiveIds)
        {
            if (string.IsNullOrEmpty(skillID)) continue;

            // 스킬 인스턴스의 현재 레벨 가져오기
            int skillLevel = GetPassiveSkillLevel(skillID, playerData);
            if (skillLevel <= 0) continue;

            var levelInfo = SkillLevelDataLoader.Instance.GetSkillLevelInfo(skillID, skillLevel);
            if (levelInfo.level <= 0) continue;

            EStatType st1 = levelInfo.StatType1;
            if (st1 != EStatType.None)
            {
                StatModifierType mt1 = GetModifierType(st1);
                ApplyPassiveBonus(ref s, st1, levelInfo.value1, mt1);
            }

            EStatType st2 = levelInfo.StatType2;
            if (st2 != EStatType.None)
            {
                StatModifierType mt2 = GetModifierType(st2);
                ApplyPassiveBonus(ref s, st2, levelInfo.value2, mt2);
            }
        }
    }

    private static void ApplyClassMultipliers(
        ref PlayerFinalStatsSnapshot s,
        BaseClassData classData)
    {
        if (classData == null) return;

        s.AttackDamage *= classData.AttackPowerMultiplier;

        // 이동속도: base × (1 + %보너스 합) × 클래스배율
        s.MoveSpeed *= (1f + s.MoveSpeedPercentBonus);
        s.MoveSpeed *= classData.MoveSpeedMultiplier;

        s.MaxHealth *= classData.HealthMultiplier;
    }

    private static void ValidateSnapshot(ref PlayerFinalStatsSnapshot s)
    {
        s.AttackDamage     = Mathf.Max(1f, s.AttackDamage);
        s.MaxHealth        = Mathf.Max(1f, s.MaxHealth);
        s.Defense          = Mathf.Max(0f, s.Defense);
        s.AttackSpeed      = Mathf.Clamp(s.AttackSpeed, 0.1f, 5f);
        s.MoveSpeed        = Mathf.Clamp(s.MoveSpeed, 0.3f, 20f);
        s.CriticalChance   = Mathf.Clamp01(s.CriticalChance);
        s.CriticalDamage   = Mathf.Max(1f, s.CriticalDamage);
        s.HealMultiplier   = Mathf.Max(0f, s.HealMultiplier);

        s.SkillDamageBonus       = Mathf.Max(0f, s.SkillDamageBonus);
        s.CooldownReduction      = Mathf.Clamp(s.CooldownReduction, 0f, 0.5f);
        s.DamageReduction        = Mathf.Clamp(s.DamageReduction,   0f, 0.8f);
        s.HpRegen                = Mathf.Max(0f, s.HpRegen);
        s.LifeSteal              = Mathf.Clamp(s.LifeSteal,         0f, 1.0f);
        s.ArmorPenetration       = Mathf.Clamp(s.ArmorPenetration,  0f, 1.0f);
        s.DodgeChance            = Mathf.Clamp01(s.DodgeChance);
        s.BlockChance            = Mathf.Clamp01(s.BlockChance);
        s.ExpGainBonus           = Mathf.Max(0f, s.ExpGainBonus);
        s.StatusResist           = Mathf.Clamp01(s.StatusResist);
        s.PierceDamageRetention  = Mathf.Clamp01(s.PierceDamageRetention);
    }

    // ──────────────────────────────────────────
    // 공통 헬퍼
    // ──────────────────────────────────────────

    private static void ApplySingleModifier(
        ref PlayerFinalStatsSnapshot s,
        EStatType statType,
        float value)
    {
        switch (statType)
        {
            case EStatType.ATK_FLAT:               s.AttackDamage          += value; break;
            case EStatType.ATK_PERCENT:            s.AttackDamage          *= (1f + value); break;
            case EStatType.DEF_FLAT:               s.Defense               += value; break;
            case EStatType.HP_FLAT:                s.MaxHealth             += value; break;
            case EStatType.ASPD:                   s.AttackSpeed           *= (1f + value); break;
            case EStatType.CRIT_RATE:              s.CriticalChance        += value; break;
            case EStatType.CRIT_DMG:               s.CriticalDamage        += value; break;
            case EStatType.MOVE_SPEED:             s.MoveSpeedPercentBonus += value; break;
            case EStatType.SKILL_DMG_PERCENT:      s.SkillDamageBonus      += value; break;
            case EStatType.COOLDOWN_REDUCTION:     s.CooldownReduction     += value; break;
            case EStatType.DAMAGE_REDUCTION_PERCENT: s.DamageReduction     += value; break;
            case EStatType.HP_REGEN:               s.HpRegen               += value; break;
            case EStatType.LIFESTEAL:              s.LifeSteal             += value; break;
            case EStatType.ARMOR_PENETRATION:      s.ArmorPenetration      += value; break;
            case EStatType.DODGE_CHANCE:           s.DodgeChance           += value; break;
            case EStatType.BLOCK_CHANCE:           s.BlockChance           += value; break;
            case EStatType.EXP_GAIN_PERCENT:       s.ExpGainBonus          += value; break;
            case EStatType.STATUS_RESIST_ALL:      s.StatusResist          += value; break;
            case EStatType.PIERCE_DAMAGE_RETENTION:s.PierceDamageRetention += value; break;
        }
    }

    private static void ApplyPassiveBonus(
        ref PlayerFinalStatsSnapshot s,
        EStatType statType,
        float value,
        StatModifierType modifierType)
    {
        if (modifierType == StatModifierType.Multiplicative)
        {
            switch (statType)
            {
                case EStatType.ATK_PERCENT: s.AttackDamage *= (1f + value); break;
                case EStatType.ASPD:        s.AttackSpeed  *= (1f + value); break;
                case EStatType.MOVE_SPEED:  s.MoveSpeedPercentBonus += value; break;
            }
        }
        else
        {
            ApplySingleModifier(ref s, statType, value);
        }
    }

    private static StatModifierType GetModifierType(EStatType statType)
    {
        switch (statType)
        {
            case EStatType.ATK_PERCENT:
            case EStatType.ASPD:
            case EStatType.MOVE_SPEED:
                return StatModifierType.Multiplicative;
            default:
                return StatModifierType.Additive;
        }
    }

    /// <summary>
    /// 로비에서 패시브 스킬의 현재 레벨을 가져온다.
    /// AccountDataManager.GetUnlockedPassiveSkills() 에서 skillID 로 검색한다.
    /// </summary>
    private static int GetPassiveSkillLevel(string skillID, SelectedPlayerData playerData)
    {
        if (AccountDataManager.Instance == null) return 1;

        var passives = AccountDataManager.Instance.GetUnlockedPassiveSkills();
        if (passives == null) return 1;

        foreach (var si in passives)
        {
            if (si?.skillData?.skillID == skillID && si.IsUnlocked)
                return si.currentLevel;
        }
        return 1;
    }

    /// <summary>
    /// Resources/PlayerData/{PlayerType}Data 경로로 BaseClassData 로드
    /// </summary>
    private static BaseClassData LoadClassData(PlayerType playerType)
    {
        string assetName = playerType switch
        {
            PlayerType.Assasin => "AssasinData",
            PlayerType.Warrior => "WarriorData",
            PlayerType.Wizard  => "WizardData",
            _                  => null
        };

        if (string.IsNullOrEmpty(assetName)) return null;

        return Resources.Load<BaseClassData>($"PlayerData/{assetName}");
    }

    private static PlayerFinalStatsSnapshot BuildDefaultSnapshot()
    {
        return new PlayerFinalStatsSnapshot
        {
            AttackDamage           = 10f,
            MaxHealth              = 200f,
            Defense                = 5f,
            CriticalChance         = 0.05f,
            CriticalDamage         = 1.5f,
            AttackSpeed            = 1.0f,
            MoveSpeed              = 4f,
            HealMultiplier         = 1.0f,
            PierceDamageRetention  = 0.5f,
        };
    }
}
