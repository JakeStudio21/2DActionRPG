using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 모든 스킬 인스턴스 관리
/// 액티브/패시브 분리, 장착 슬롯 관리 (Phase 1)
/// </summary>
public class PlayerSkillManager : MonoBehaviour
{
    [Header("📋 스킬 인스턴스")]
    [Tooltip("보유 중인 액티브 스킬 목록")]
    public List<SkillInstance> unlockedActiveSkills = new List<SkillInstance>();
    
    [Tooltip("보유 중인 패시브 스킬 목록")]
    public List<SkillInstance> unlockedPassiveSkills = new List<SkillInstance>();
    
    [Header("🎯 장착 슬롯")]
    [Tooltip("장착된 액티브 스킬 (기획서: 2개)")]
    public List<SkillInstance> equippedActiveSkills = new List<SkillInstance>(2);
    
    [Tooltip("장착된 패시브 스킬 (기획서: 3개)")]
    public List<SkillInstance> equippedPassiveSkills = new List<SkillInstance>(3);
    
    [Header("💎 SP 시스템 (Phase 2)")]
    [Tooltip("총 획득 SP (레벨업으로 얻은 누적)")]
    public int totalSP = 0;
    
    [Tooltip("사용한 SP (스킬 투자에 소모됨)")]
    public int usedSP = 0;
    
    /// <summary>
    /// 사용 가능한 SP (총 SP - 사용한 SP)
    /// </summary>
    public int AvailableSP => totalSP - usedSP;
    
    [Header("🎮 플레이어 레벨 (참조용)")]
    [Tooltip("현재 플레이어 레벨 (스킬 해금 조건 체크용)")]
    public int currentPlayerLevel = 1;
    
    [Header("🔗 시스템 참조")]
    [Tooltip("PlayerRuntimeStats 참조 (자동 탐색 또는 수동 할당)")]
    [SerializeField] private PlayerRuntimeStats playerStats;
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = true;
    
    void Awake()
    {
        // 같은 GameObject에서 먼저 찾기
        playerStats = GetComponent<PlayerRuntimeStats>();
        
        // 없으면 씬 전체에서 찾기
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerRuntimeStats>();
            
            if (playerStats == null)
                Debug.LogError("❌ [PlayerSkillManager] PlayerRuntimeStats를 찾을 수 없습니다!");
        }
    }
    
    void Start()
    {
        // PlayerDataManager에서 스킬 데이터 동기화 (Phase 3.5)
        SyncFromPlayerData();
        
        // 장착된 패시브 스탯 적용
        ApplyAllPassiveStats();
        
        // 장착된 스킬의 VFX 풀 사전 확보
        WarmupEquippedSkillEffects();
    }
    
    /// <summary>
    /// PlayerDataManager에서 스킬 데이터 동기화 (Phase 3.5)
    /// 로비에서 설정한 스킬 데이터를 전투 씬으로 가져옴
    /// </summary>
    private void SyncFromPlayerData()
    {
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.LogWarning("⚠️ [PlayerSkillManager] PlayerDataManager가 없거나 슬롯이 선택되지 않았습니다. 기본 데이터 사용.");
            return;
        }
        
        var playerDataManager = PlayerDataManager.Instance;
        var slotData = playerDataManager.GetCurrentSlotData();
        
        if (slotData == null)
        {
            Debug.LogError("❌ [PlayerSkillManager] 현재 슬롯 데이터를 가져올 수 없습니다!");
            return;
        }
        
        
        // 1. 보유 스킬 동기화
        unlockedActiveSkills = GetActiveSkillsFromSlotData(slotData);
        unlockedPassiveSkills = GetPassiveSkillsFromSlotData(slotData);
        
        // 2. 장착 슬롯 동기화
        equippedActiveSkills.Clear();
        equippedPassiveSkills.Clear();
        
        // 액티브 슬롯 (2개)
        for (int i = 0; i < 2; i++)
        {
            var skill = GetEquippedActiveSkill(slotData, i);
            equippedActiveSkills.Add(skill);
        }
        
        // 패시브 슬롯 (3개)
        for (int i = 0; i < 3; i++)
        {
            var skill = GetEquippedPassiveSkill(slotData, i);
            equippedPassiveSkills.Add(skill);
        }
        
        // 3. SP 동기화
        totalSP = slotData.totalSP;
        usedSP = slotData.usedSP;
        
        // 4. 플레이어 레벨 동기화
        currentPlayerLevel = slotData.level;
        
    }
    
    /// <summary>
    /// Tutorial 전용 스킬 직접 주입
    /// — PlayerDataManager/슬롯 없이 ActiveSkillData를 바로 장착 슬롯에 설정
    /// </summary>
    public void SetTutorialSkills(ActiveSkillData skill1, ActiveSkillData skill2)
    {
        equippedActiveSkills.Clear();
        unlockedActiveSkills.Clear();

        for (int i = 0; i < 2; i++)
        {
            ActiveSkillData data = (i == 0) ? skill1 : skill2;
            if (data != null)
            {
                var instance = new SkillInstance(data, 1, true);
                unlockedActiveSkills.Add(instance);
                equippedActiveSkills.Add(instance);
            }
            else
            {
                equippedActiveSkills.Add(null);
            }
        }

    }

    /// <summary>
    /// SlotData에서 액티브 스킬 목록 가져오기
    /// </summary>
    private List<SkillInstance> GetActiveSkillsFromSlotData(PlayerSlotData slotData)
    {
        List<SkillInstance> activeSkills = new List<SkillInstance>();
        
        if (slotData.skills != null)
        {
            foreach (var saveData in slotData.skills)
            {
                SkillInstance skill = saveData.ToSkillInstance();
                if (skill != null && skill.IsActiveSkill)
                {
                    activeSkills.Add(skill);
                }
            }
        }
        
        return activeSkills;
    }
    
    /// <summary>
    /// SlotData에서 패시브 스킬 목록 가져오기
    /// </summary>
    private List<SkillInstance> GetPassiveSkillsFromSlotData(PlayerSlotData slotData)
    {
        List<SkillInstance> passiveSkills = new List<SkillInstance>();
        
        if (slotData.skills != null)
        {
            foreach (var saveData in slotData.skills)
            {
                SkillInstance skill = saveData.ToSkillInstance();
                if (skill != null && skill.IsPassiveSkill)
                {
                    passiveSkills.Add(skill);
                }
            }
        }
        
        return passiveSkills;
    }
    
    /// <summary>
    /// 장착된 액티브 스킬 가져오기
    /// </summary>
    private SkillInstance GetEquippedActiveSkill(PlayerSlotData slotData, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotData.equippedActiveSkillIds.Length)
            return null;
        
        string skillID = slotData.equippedActiveSkillIds[slotIndex];
        if (string.IsNullOrEmpty(skillID)) return null;
        
        return FindSkillByID(slotData, skillID);
    }
    
    /// <summary>
    /// 장착된 패시브 스킬 가져오기
    /// </summary>
    private SkillInstance GetEquippedPassiveSkill(PlayerSlotData slotData, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotData.equippedPassiveSkillIds.Length)
            return null;
        
        string skillID = slotData.equippedPassiveSkillIds[slotIndex];
        if (string.IsNullOrEmpty(skillID)) return null;
        
        return FindSkillByID(slotData, skillID);
    }
    
    /// <summary>
    /// 스킬 ID로 SkillInstance 찾기
    /// </summary>
    private SkillInstance FindSkillByID(PlayerSlotData slotData, string skillID)
    {
        if (slotData.skills == null) return null;
        
        var saveData = slotData.skills.Find(s => s.skillID == skillID);
        return saveData?.ToSkillInstance();
    }
    
    // ===== 스킬 조회 =====
    
    /// <summary>
    /// skillID로 스킬 찾기
    /// </summary>
    public SkillInstance GetSkill(string skillID)
    {
        var skill = unlockedActiveSkills.Find(s => s.skillData != null && s.skillData.skillID == skillID);
        if (skill != null) return skill;
        
        return unlockedPassiveSkills.Find(s => s.skillData != null && s.skillData.skillID == skillID);
    }
    
    /// <summary>
    /// 슬롯 인덱스로 장착된 액티브 스킬 가져오기
    /// </summary>
    public SkillInstance GetEquippedActiveSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedActiveSkills.Count)
            return null;
        return equippedActiveSkills[slotIndex];
    }
    
    // ===== 스킬 장착/해제 =====
    
    /// <summary>
    /// 액티브 스킬 장착
    /// </summary>
    public bool EquipActiveSkill(SkillInstance skill, int slotIndex)
    {
        if (skill == null || !skill.IsUnlocked) return false;
        if (slotIndex < 0 || slotIndex >= 2) return false; // 슬롯 2개 제한
        
        // 기존 스킬 해제
        if (equippedActiveSkills.Count > slotIndex && equippedActiveSkills[slotIndex] != null)
        {
            equippedActiveSkills[slotIndex].isEquipped = false;
        }
        
        // 새 스킬 장착
        while (equippedActiveSkills.Count <= slotIndex)
            equippedActiveSkills.Add(null);
        
        equippedActiveSkills[slotIndex] = skill;
        skill.isEquipped = true;
        
        return true;
    }
    
    /// <summary>
    /// 패시브 스킬 장착
    /// </summary>
    public bool EquipPassiveSkill(SkillInstance skill, int slotIndex)
    {
        if (skill == null || !skill.IsUnlocked) return false;
        if (slotIndex < 0 || slotIndex >= 3) return false; // 슬롯 3개 제한
        
        // 기존 스킬 해제
        if (equippedPassiveSkills.Count > slotIndex && equippedPassiveSkills[slotIndex] != null)
        {
            equippedPassiveSkills[slotIndex].isEquipped = false;
        }
        
        // 새 스킬 장착
        while (equippedPassiveSkills.Count <= slotIndex)
            equippedPassiveSkills.Add(null);
        
        equippedPassiveSkills[slotIndex] = skill;
        skill.isEquipped = true;
        
        // 스탯 재계산
        ApplyAllPassiveStats();
        
        return true;
    }
    
    // ===== 패시브 스탯 적용 (핵심) =====

    /// <summary>
    /// EStatType에 따른 StatModifierType 결정
    /// Percent 단위 스탯은 Multiplicative, 나머지는 Additive
    /// </summary>
    private StatModifierType GetModifierType(EStatType statType)
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

    #region 🔥 VFX Warm-up

    /// <summary>
    /// 장착된 액티브 스킬의 castCueKey / aoeCueKey를 CueRegistry로 해석해
    /// 사용될 VFX 풀을 GamePoolManager에 사전 확보한다.
    /// 로비에서 스킬을 선택한 뒤 인게임 진입 시 자동 호출됨.
    /// </summary>
    public void WarmupEquippedSkillEffects()
    {
        if (GamePoolManager.Instance == null)
        {
            Debug.LogWarning("⚠️ [PlayerSkillManager] GamePoolManager 없음 — Warm-up 건너뜀");
            return;
        }
        if (CueSystem.CueRegistry.Instance == null)
        {
            Debug.LogWarning("⚠️ [PlayerSkillManager] CueRegistry 없음 — Warm-up 건너뜀");
            return;
        }

        var poolKeys = new System.Collections.Generic.HashSet<string>();

        foreach (var skillInstance in equippedActiveSkills)
        {
            if (skillInstance?.skillData is ActiveSkillData activeData)
            {
                GatherVfxPoolKeys(activeData.castCueKey, poolKeys);
                GatherVfxPoolKeys(activeData.aoeCueKey, poolKeys);
            }
        }

        if (poolKeys.Count == 0)
        {
            return;
        }

        foreach (var key in poolKeys)
        {
            // 이미 존재하는 풀을 3개 추가 확보 (ScenePoolConfig에 등록된 경우)
            GamePoolManager.Instance.ExpandPool(key, 3);
        }
    }

    /// <summary>
    /// CueRegistry에서 이벤트 키를 해석해 필요한 VFX poolKey 목록을 수집한다.
    /// </summary>
    private void GatherVfxPoolKeys(string cueEventKey, System.Collections.Generic.HashSet<string> result)
    {
        if (string.IsNullOrEmpty(cueEventKey)) return;

        var slot = CueSystem.CueRegistry.Instance.Resolve("Player", cueEventKey);
        if (slot == null || slot.IsEmpty) return;

        foreach (var vfxCue in slot.vfxCues)
        {
            if (!string.IsNullOrEmpty(vfxCue.poolKey))
                result.Add(vfxCue.poolKey);
        }
    }

    #endregion

    /// <summary>
    /// 장착된 모든 패시브 스킬의 스탯 보너스를 PlayerRuntimeStats에 적용
    /// CSV의 StatType1/StatType2를 단독 진실 소스로 사용
    /// </summary>
    public void ApplyAllPassiveStats()
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerRuntimeStats가 없습니다!");
            return;
        }
        
        ClearPassiveStats();
        
        foreach (var passive in equippedPassiveSkills)
        {
            if (passive == null || !passive.IsUnlocked) continue;
            if (!(passive.skillData is PassiveSkillData)) continue;
            
            string skillID = passive.skillData.skillID;
            var levelInfo = SkillLevelDataLoader.Instance.GetSkillLevelInfo(skillID, passive.currentLevel);
            
            if (levelInfo.level <= 0)
            {
                Debug.LogWarning($"⚠️ [{passive.skillData.skillName}] CSV 데이터 없음 (Lv.{passive.currentLevel})");
                continue;
            }
            
            // StatType1 적용
            EStatType st1 = levelInfo.StatType1;
            if (st1 != EStatType.None)
            {
                playerStats.AddPassiveStatBonus(skillID, st1, levelInfo.value1, GetModifierType(st1));
            }
            
            // StatType2 적용 (이중 스탯 패시브)
            EStatType st2 = levelInfo.StatType2;
            if (st2 != EStatType.None)
            {
                playerStats.AddPassiveStatBonus(skillID, st2, levelInfo.value2, GetModifierType(st2));
            }
        }
        
        playerStats.RecalculateAllStats();
    }
    
    /// <summary>
    /// 패시브 스탯 보너스 제거
    /// </summary>
    private void ClearPassiveStats()
    {
        if (playerStats == null) return;
        
        // 모든 패시브 스킬의 보너스 제거
        foreach (var passive in unlockedPassiveSkills)
        {
            if (passive == null || passive.skillData == null) continue;
            playerStats.RemovePassiveStatBonus(passive.skillData.skillID);
        }
    }
    
    // ===== SP 관리 (Phase 2 완성) =====
    
    /// <summary>
    /// SP 획득 (레벨업 시 호출)
    /// </summary>
    public void AddSP(int amount)
    {
        totalSP += amount;
    }
    
    /// <summary>
    /// SP 소모 가능 여부
    /// </summary>
    public bool CanAffordSP(int amount)
    {
        return AvailableSP >= amount;
    }
    
    /// <summary>
    /// 스킬 업그레이드 시도 (Phase 2 핵심 메서드)
    /// </summary>
    public bool TryUpgradeSkill(SkillInstance skill, int playerLevel)
    {
        if (skill == null || skill.skillData == null)
        {
            Debug.LogWarning("⚠️ 유효하지 않은 스킬입니다.");
            return false;
        }
        
        // ① 해금 조건: 플레이어 레벨 체크
        if (playerLevel < skill.skillData.unlockLevel)
        {
            return false;
        }
        
        // ② 만렙 조건: 최대 레벨 체크
        if (skill.IsMaxLevel)
        {
            return false;
        }
        
        // ③ 재화 조건: SP 체크
        int requiredSP = skill.GetRequiredSPForNextLevel();
        if (!CanAffordSP(requiredSP))
        {
            return false;
        }
        
        // 모든 조건 통과 → 레벨업 실행!
        int prevLevel = skill.currentLevel;
        skill.currentLevel++;
        usedSP += requiredSP;
        
            Dbg.Log($"✅ [{skill.skillData.skillName}] 레벨업 성공!");
        
        // 🔄 패시브 스킬이 장착 중이면 스탯 즉시 재계산
        if (skill.IsPassiveSkill && skill.isEquipped)
        {
            ApplyAllPassiveStats();
        }
        
        return true;
    }
    
    // ===== 초기화 (직업별 기본 스킬 지급) =====
    
    /// <summary>
    /// 신규 캐릭터 기본 스킬 설정
    /// </summary>
    public void InitializeDefaultSkills(PlayerType playerType)
    {
        // Phase 1: 기본 구조만 구현
        // Phase 2에서 실제 스킬 데이터 할당 예정
    }
    
    #region 🧪 테스트 메서드 (Play 모드 Context Menu)
    
    /// <summary>
    /// 테스트: '정령의 공명' 패시브 추가 및 장착
    /// </summary>
    [ContextMenu("Test: Add Spirit Resonance Passive")]
    public void TestAddSpiritResonance()
    {
        // SO 로드
        PassiveSkillData passiveData = Resources.Load<PassiveSkillData>("Skills/Passive/PassiveSkill_SpiritResonance");
        
        if (passiveData == null)
        {
            Debug.LogError("❌ PassiveSkill_SpiritResonance.asset를 찾을 수 없습니다!");
            Debug.LogError("   경로: Resources/Skills/Passive/PassiveSkill_SpiritResonance.asset");
            Debug.LogError("   먼저 'Tools → Skill System → Create Sample Skills' 메뉴를 실행하세요!");
            return;
        }
        
        // SkillInstance 생성 (레벨 1)
        var skillInstance = new SkillInstance(passiveData, 1);
        
        // 추가
        if (!unlockedPassiveSkills.Exists(s => s.skillData.skillID == passiveData.skillID))
            unlockedPassiveSkills.Add(skillInstance);
        
        // 슬롯 0에 장착
        EquipPassiveSkill(skillInstance, 0);
    }
    
    /// <summary>
    /// 테스트: SP 지급
    /// </summary>
    [ContextMenu("Test: Add 10 SP")]
    public void TestAdd10SP()
    {
        AddSP(10);
    }
    
    /// <summary>
    /// 통합 테스트: 모든 시나리오 자동 실행
    /// </summary>
    [ContextMenu("Test: Full Phase 2 Test")]
    public void TestFullPhase2()
    {
        
        // 초기화
        TestResetAllData();
        
        // 1. SP 지급
        AddSP(10);
        
        // 2. 정령의 공명 추가 (Lv.0 → Lv.1)
        TestAddSpiritResonance();
        
        // 공격력 확인 (Lv.1 = +5%)
        TestPrintStats();
        
        // 3. Lv.1 → Lv.2 업그레이드
        Dbg.Log("\n3️⃣ Lv.1 → Lv.2 레벨업");
        currentPlayerLevel = 10; // 레벨 충분하게
        var skill = unlockedPassiveSkills.Find(s => s.skillData.skillID == "passive_spirit_resonance");
        bool success = TryUpgradeSkill(skill, currentPlayerLevel);
        
        if (success)
        {
            TestPrintStats();
            
            // CSV 데이터 확인
            var levelInfo = SkillLevelDataLoader.Instance.GetSkillLevelInfo("passive_spirit_resonance", 2);
        }
        
        // 4. 레벨 부족 시나리오
        currentPlayerLevel = 3; // 레벨 낮춤
        bool failResult = TryUpgradeSkill(skill, currentPlayerLevel);
        
        // 5. SP 부족 시나리오
        currentPlayerLevel = 10;
        usedSP = totalSP - 1; // SP를 1만 남김
        bool spFailResult = TryUpgradeSkill(skill, currentPlayerLevel);
        
    }
    
    /// <summary>
    /// 테스트: 정령의 공명 레벨업 시도 (레벨 부족)
    /// </summary>
    [ContextMenu("Test: Upgrade Spirit (Level Check Fail)")]
    public void TestUpgradeSpiritLevelFail()
    {
        currentPlayerLevel = 3; // 플레이어 레벨 3으로 설정
        
        var skill = unlockedPassiveSkills.Find(s => s.skillData.skillID == "passive_spirit_resonance");
        if (skill == null)
        {
            Debug.LogError("❌ '정령의 공명' 패시브를 먼저 추가하세요!");
            return;
        }
        
        // unlockLevel = 5, 현재 playerLevel = 3 → 실패
        bool result = TryUpgradeSkill(skill, currentPlayerLevel);
    }
    
    /// <summary>
    /// 테스트: 정령의 공명 레벨업 성공
    /// </summary>
    [ContextMenu("Test: Upgrade Spirit (Success)")]
    public void TestUpgradeSpiritSuccess()
    {
        currentPlayerLevel = 10; // 플레이어 레벨 충분
        
        var skill = unlockedPassiveSkills.Find(s => s.skillData.skillID == "passive_spirit_resonance");
        if (skill == null)
        {
            Debug.LogError("❌ '정령의 공명' 패시브를 먼저 추가하세요!");
            return;
        }
        
        // SP 확인
        
        TryUpgradeSkill(skill, currentPlayerLevel);
    }
    
    /// <summary>
    /// 테스트: 현재 스탯 출력
    /// </summary>
    [ContextMenu("Test: Print Stats")]
    public void TestPrintStats()
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerRuntimeStats가 없습니다!");
            return;
        }
        
    }
    
    /// <summary>
    /// 테스트: 모든 스킬 목록 출력
    /// </summary>
    [ContextMenu("Test: List All Skills")]
    public void TestListAllSkills()
    {
    }
    
    /// <summary>
    /// 테스트: 모든 패시브 스탯 강제 재적용
    /// </summary>
    [ContextMenu("Test: Force Reapply Passive Stats")]
    public void TestForceReapply()
    {
        if (playerStats == null)
        {
            Debug.LogError("❌ PlayerRuntimeStats가 없습니다! Play 모드에서 실행하세요.");
            return;
        }
        
        ApplyAllPassiveStats();
    }
    
    /// <summary>
    /// 테스트: 데이터 초기화
    /// </summary>
    [ContextMenu("Test: Reset All Data")]
    public void TestResetAllData()
    {
        unlockedActiveSkills.Clear();
        unlockedPassiveSkills.Clear();
        equippedActiveSkills.Clear();
        equippedPassiveSkills.Clear();
        totalSP = 0;
        usedSP = 0;
        currentPlayerLevel = 1;
        
        // 스탯 초기화
        if (playerStats != null)
        {
            ClearPassiveStats();
            playerStats.RecalculateAllStats();
        }
        
        Dbg.Log("🗑️ 테스트 데이터 초기화 완료");
    }
    
    #endregion
}

