using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스킬 탭 컨트롤러 (SkillSubPanel 내부)
/// Phase 3-Revision: 좌측(장착) + 우측(리스트) + 하단(상세)
/// </summary>
public class SkillTabController : MonoBehaviour
{
    [Header("📊 SP 표시")]
    [SerializeField] private TextMeshProUGUI spText;
    [SerializeField] private TextMeshProUGUI playerLevelText;
    
    [Header("🎯 좌측: 장착 슬롯")]
    [SerializeField] private SkillEquipSlotUI[] activeEquipSlots; // 2개
    [SerializeField] private SkillEquipSlotUI[] passiveEquipSlots; // 3개
    
    [Header("📋 우측: 스킬 리스트")]
    [SerializeField] private Transform activeSkillListParent;
    [SerializeField] private Transform passiveSkillListParent;
    [SerializeField] private GameObject skillListItemPrefab;
    
    [Header("📖 하단: 상세 정보 패널")]
    [SerializeField] private SkillDetailPanel skillDetailPanel;
    
    [Header("⚠️ 경고 메시지 (TopPanel 권장)")]
    [Tooltip("슬롯이 가득 찼을 때 표시되는 경고 메시지")]
    [SerializeField] private GameObject warningMessageObject;
    [SerializeField] private TextMeshProUGUI warningMessageText;
    [SerializeField] private float warningDisplayDuration = 3f;
    
    [Header("🔗 데이터 소스")]
    private PlayerDataManager playerDataManager;
    
    [Header("🔧 디버그")]
    
    // 생성된 스킬 아이템 캐시
    private List<SkillListItemUI> activeItemUIList = new List<SkillListItemUI>();
    private List<SkillListItemUI> passiveItemUIList = new List<SkillListItemUI>();
    
    // 현재 선택된 스킬 아이템
    private SkillListItemUI currentSelectedItem;
    
    void Start()
    {
        Initialize();
    }
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize()
    {
        // PlayerDataManager 참조
        playerDataManager = PlayerDataManager.Instance;
        if (playerDataManager == null)
        {
            Debug.LogError("❌ [SkillTabController] PlayerDataManager를 찾을 수 없습니다!");
            return;
        }
        
        // SkillDetailPanel 초기화
        if (skillDetailPanel != null)
        {
            skillDetailPanel.Hide();
        }
        
        // 경고 메시지 초기 숨김
        if (warningMessageObject != null)
        {
            warningMessageObject.SetActive(false);
        }
        
        // 장착 슬롯 초기화
        InitializeEquipSlots();
        
        // UI 갱신
        RefreshUI();
        
    }
    
    /// <summary>
    /// 장착 슬롯 초기화
    /// </summary>
    private void InitializeEquipSlots()
    {
        // 액티브 슬롯 (2개)
        for (int i = 0; i < activeEquipSlots.Length; i++)
        {
            if (activeEquipSlots[i] != null)
            {
                activeEquipSlots[i].Setup(i, true, this);
            }
        }
        
        // 패시브 슬롯 (3개)
        for (int i = 0; i < passiveEquipSlots.Length; i++)
        {
            if (passiveEquipSlots[i] != null)
            {
                passiveEquipSlots[i].Setup(i, false, this);
            }
        }
    }
    
    /// <summary>
    /// 전체 UI 갱신
    /// </summary>
    public void RefreshUI()
    {
        if (playerDataManager == null || !playerDataManager.IsSlotSelected) return;
        
        // SP 표시 갱신
        UpdateSPDisplay();
        
        // 스킬 리스트 갱신
        RefreshSkillList();
        
        // 장착 슬롯 갱신
        RefreshEquipSlots();
        
    }
    
    /// <summary>
    /// SP 표시 갱신
    /// </summary>
    private void UpdateSPDisplay()
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null)
        {
            Debug.LogWarning("⚠️ [SkillTabController] UpdateSPDisplay: slotData가 null입니다!");
            return;
        }
        
        if (spText != null)
        {
            int totalSP = slotData.totalSP;
            int usedSP = slotData.usedSP;
            int availableSP = totalSP - usedSP;
            
            // SP는 항상 /60으로 표시 (레벨과 무관)
            int maxDisplaySP = 60;
            
            spText.text = $"{availableSP}/{maxDisplaySP}";
            
        }
        
        if (playerLevelText != null)
        {
            int playerLevel = slotData.level;
            playerLevelText.text = $"Lv.{playerLevel}";
            
        }
    }
    
    /// <summary>
    /// 스킬 리스트 갱신
    /// </summary>
    private void RefreshSkillList()
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null)
        {
            Debug.LogWarning("⚠️ [SkillTabController] RefreshSkillList: slotData가 null입니다!");
            return;
        }
        
        int playerLevel = slotData.level;
        
        // 액티브 스킬 목록
        List<SkillInstance> activeSkills = GetActiveSkillsFromSlotData(slotData);
        
        RefreshSkillCategory(
            activeSkills,
            activeSkillListParent,
            activeItemUIList,
            playerLevel
        );
        
        // 패시브 스킬 목록
        List<SkillInstance> passiveSkills = GetPassiveSkillsFromSlotData(slotData);
        
        RefreshSkillCategory(
            passiveSkills,
            passiveSkillListParent,
            passiveItemUIList,
            playerLevel
        );
    }
    
    /// <summary>
    /// SlotData에서 액티브 스킬 목록 가져오기 (모든 스킬 포함 - 해금/미해금)
    /// </summary>
    private List<SkillInstance> GetActiveSkillsFromSlotData(PlayerSlotData slotData)
    {
        List<SkillInstance> activeSkills = new List<SkillInstance>();
        
        // 1. Resources 폴더에서 모든 액티브 스킬 SO 로드
        ActiveSkillData[] allActiveSkills = Resources.LoadAll<ActiveSkillData>("Skills/Active");
        
        
        // 2. 각 스킬 SO에 대해 SkillInstance 생성
        foreach (var skillSO in allActiveSkills)
        {
            if (skillSO == null) continue;
            
            // 저장 데이터에서 해당 스킬 찾기
            SkillInstanceSaveData saveData = null;
            if (slotData.skills != null)
            {
                saveData = slotData.skills.Find(s => s.skillID == skillSO.skillID);
            }
            
            SkillInstance skill;
            if (saveData != null)
            {
                // 해금됨: 저장된 레벨/장착 상태 사용
                skill = saveData.ToSkillInstance();
            }
            else
            {
                // 미해금: Lv.0 상태로 생성
                skill = new SkillInstance(skillSO, level: 0, equipped: false);
            }
            
            activeSkills.Add(skill);
        }
        
        // 3. 정렬: 1차) 스킬 타입(WaveClear → BossBurst), 2차) 해금 레벨 낮은 순
        activeSkills.Sort((a, b) => 
        {
            var aData = a.skillData as ActiveSkillData;
            var bData = b.skillData as ActiveSkillData;
            
            // 1차 정렬: 스킬 타입 (WaveClear=0, BossBurst=1)
            int typeCompare = aData.skillType.CompareTo(bData.skillType);
            if (typeCompare != 0)
                return typeCompare;
            
            // 2차 정렬: 해금 레벨
            return a.skillData.unlockLevel.CompareTo(b.skillData.unlockLevel);
        });
        
        
        return activeSkills;
    }
    
    /// <summary>
    /// SlotData에서 패시브 스킬 목록 가져오기 (모든 스킬 포함 - 해금/미해금)
    /// </summary>
    private List<SkillInstance> GetPassiveSkillsFromSlotData(PlayerSlotData slotData)
    {
        List<SkillInstance> passiveSkills = new List<SkillInstance>();
        
        // 1. Resources 폴더에서 모든 패시브 스킬 SO 로드
        PassiveSkillData[] allPassiveSkills = Resources.LoadAll<PassiveSkillData>("Skills/Passive");
        
        
        // 2. 각 스킬 SO에 대해 SkillInstance 생성
        foreach (var skillSO in allPassiveSkills)
        {
            if (skillSO == null) continue;
            
            // 저장 데이터에서 해당 스킬 찾기
            SkillInstanceSaveData saveData = null;
            if (slotData.skills != null)
            {
                saveData = slotData.skills.Find(s => s.skillID == skillSO.skillID);
            }
            
            SkillInstance skill;
            if (saveData != null)
            {
                // 해금됨: 저장된 레벨/장착 상태 사용
                skill = saveData.ToSkillInstance();
            }
            else
            {
                // 미해금: Lv.0 상태로 생성
                skill = new SkillInstance(skillSO, level: 0, equipped: false);
            }
            
            passiveSkills.Add(skill);
        }
        
        // 3. 정렬: 해금 레벨 낮은 순 (해금 여부와 무관하게 항상 고정 순서)
        passiveSkills.Sort((a, b) => 
        {
            return a.skillData.unlockLevel.CompareTo(b.skillData.unlockLevel);
        });
        
        
        return passiveSkills;
    }
    
    /// <summary>
    /// 특정 카테고리 스킬 목록 갱신
    /// </summary>
    private void RefreshSkillCategory(List<SkillInstance> skills, Transform parent, List<SkillListItemUI> itemUIList, int playerLevel)
    {
        if (parent == null || skillListItemPrefab == null) return;
        
        // 기존 아이템보다 스킬이 많으면 생성
        while (itemUIList.Count < skills.Count)
        {
            GameObject itemObj = Instantiate(skillListItemPrefab, parent);
            SkillListItemUI itemUI = itemObj.GetComponent<SkillListItemUI>();
            if (itemUI != null)
            {
                itemUIList.Add(itemUI);
            }
        }
        
        // 각 아이템에 스킬 할당
        for (int i = 0; i < itemUIList.Count; i++)
        {
            if (i < skills.Count)
            {
                itemUIList[i].Setup(skills[i], playerLevel, this);
                itemUIList[i].gameObject.SetActive(true);
            }
            else
            {
                itemUIList[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 장착 슬롯 갱신
    /// </summary>
    private void RefreshEquipSlots()
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null) return;
        
        // 액티브 슬롯 갱신
        for (int i = 0; i < activeEquipSlots.Length; i++)
        {
            if (activeEquipSlots[i] != null)
            {
                SkillInstance equipped = GetEquippedActiveSkill(slotData, i);
                activeEquipSlots[i].SetSkill(equipped);
            }
        }
        
        // 패시브 슬롯 갱신
        for (int i = 0; i < passiveEquipSlots.Length; i++)
        {
            if (passiveEquipSlots[i] != null)
            {
                SkillInstance equipped = GetEquippedPassiveSkill(slotData, i);
                passiveEquipSlots[i].SetSkill(equipped);
            }
        }
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
    
    /// <summary>
    /// 스킬 상세 정보 표시 (하단 패널)
    /// </summary>
    public void ShowSkillDetail(SkillInstance skill)
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null || skillDetailPanel == null) return;
        
        int playerLevel = slotData.level;
        skillDetailPanel.ShowSkillDetail(skill, playerLevel);
        
    }
    
    /// <summary>
    /// 선택된 스킬 아이템 설정
    /// </summary>
    public void SetSelectedSkillItem(SkillListItemUI item)
    {
        // 기존 선택 해제
        if (currentSelectedItem != null)
        {
            currentSelectedItem.SetSelected(false);
        }
        
        // 새 선택 설정
        currentSelectedItem = item;
        if (currentSelectedItem != null)
        {
            currentSelectedItem.SetSelected(true);
        }
    }
    
    /// <summary>
    /// 스킬 레벨업 시도 (Phase 3-Revision: 우측 리스트에서 호출)
    /// </summary>
    public bool TryUpgradeSkill(SkillInstance skill)
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null) return false;
        
        int playerLevel = slotData.level;
        
        // ① 해금 조건
        if (playerLevel < skill.skillData.unlockLevel)
        {
            Debug.LogWarning($"🔒 [{skill.skillData.skillName}] 해금 레벨 부족! (요구: Lv.{skill.skillData.unlockLevel}, 현재: Lv.{playerLevel})");
            return false;
        }
        
        // ② 만렙 조건
        if (skill.IsMaxLevel)
        {
            Debug.LogWarning($"⚠️ [{skill.skillData.skillName}] 이미 최대 레벨입니다! (Lv.{skill.skillData.maxLevel})");
            return false;
        }
        
        // ③ SP 조건
        int requiredSP = skill.GetRequiredSPForNextLevel();
        int availableSP = slotData.totalSP - slotData.usedSP;
        
        if (availableSP < requiredSP)
        {
            Debug.LogWarning($"💎 [{skill.skillData.skillName}] SP 부족! (필요: {requiredSP}, 보유: {availableSP})");
            return false;
        }
        
        // 레벨업 실행
        skill.currentLevel++;
        slotData.usedSP += requiredSP;
        
        // SelectedPlayerData도 동기화 (메모리 캐시)
        if (playerDataManager.selectedPlayerData != null)
        {
            playerDataManager.selectedPlayerData.usedSP = slotData.usedSP;
            playerDataManager.selectedPlayerData.totalSP = slotData.totalSP;
        }
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(slotData, skill);
        
        // SelectedPlayerData의 스킬 리스트도 업데이트
        if (playerDataManager.selectedPlayerData != null)
        {
            UpdateSelectedPlayerDataSkills(playerDataManager.selectedPlayerData, slotData);
        }
        
        playerDataManager.SaveCurrentSlot();
        
        // UI 갱신
        RefreshUI();
        
        
        return true;
    }
    
    /// <summary>
    /// 스킬 저장 데이터 업데이트
    /// </summary>
    private void UpdateSkillSaveData(PlayerSlotData slotData, SkillInstance skill)
    {
        if (skill == null || skill.skillData == null) return;
        
        var saveData = slotData.skills.Find(s => s.skillID == skill.skillData.skillID);
        
        if (saveData != null)
        {
            // 기존 데이터 업데이트
            saveData.currentLevel = skill.currentLevel;
            saveData.isEquipped = skill.isEquipped;
        }
        else
        {
            // 새 데이터 추가
            slotData.skills.Add(SkillInstanceSaveData.FromSkillInstance(skill));
        }
    }
    
    /// <summary>
    /// 스킬 장착 (Phase 3-Revision: 우측 리스트에서 호출)
    /// </summary>
    public void EquipSkill(SkillInstance skill)
    {
        if (skill == null || !skill.IsUnlocked)
        {
            Debug.LogWarning("⚠️ 미해금 스킬은 장착할 수 없습니다!");
            return;
        }
        
        if (skill.IsActiveSkill)
        {
            EquipActiveSkill(skill);
        }
        else if (skill.IsPassiveSkill)
        {
            EquipPassiveSkill(skill);
        }
        
        // UI 갱신
        RefreshUI();
    }
    
    /// <summary>
    /// 액티브 스킬 장착
    /// </summary>
    private void EquipActiveSkill(SkillInstance skill)
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null) return;
        
        var activeData = skill.skillData as ActiveSkillData;
        if (activeData == null)
        {
            Debug.LogError($"❌ [{skill.skillData.skillName}] ActiveSkillData가 아닙니다");
            return;
        }
        
        if (skill.isEquipped)
        {
            return;
        }
        
        // 스킬 타입에 맞는 슬롯 결정 (제약: Skill1=WaveClear, Skill2=BossBurst)
        int targetSlotIndex = -1;
        
        if (activeData.skillType == ActiveSkillType.WaveClear)
        {
            // 광역기는 슬롯 0 (Skill1)만 가능
            targetSlotIndex = 0;
        }
        else if (activeData.skillType == ActiveSkillType.BossBurst)
        {
            // 단일기는 슬롯 1 (Skill2)만 가능
            targetSlotIndex = 1;
        }
        else
        {
            Debug.LogError($"❌ [{skill.skillData.skillName}] 알 수 없는 스킬 타입: {activeData.skillType}");
            return;
        }
        
        // 기존 장착 스킬 해제
        string oldSkillID = slotData.equippedActiveSkillIds[targetSlotIndex];
        if (!string.IsNullOrEmpty(oldSkillID))
        {
            UnequipSkillByID(slotData, oldSkillID);
            
        }
        
        // 새 스킬 장착
        skill.isEquipped = true;
        slotData.equippedActiveSkillIds[targetSlotIndex] = skill.skillData.skillID;
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(slotData, skill);
        
        // SelectedPlayerData도 동기화
        if (playerDataManager.selectedPlayerData != null)
        {
            UpdateSelectedPlayerDataSkills(playerDataManager.selectedPlayerData, slotData);
        }
        
        playerDataManager.SaveCurrentSlot();
        
    }
    
    /// <summary>
    /// 빈 액티브 슬롯 찾기
    /// </summary>
    private int FindEmptyActiveSlot(PlayerSlotData slotData)
    {
        for (int i = 0; i < slotData.equippedActiveSkillIds.Length; i++)
        {
            if (string.IsNullOrEmpty(slotData.equippedActiveSkillIds[i]))
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// 패시브 스킬 장착
    /// </summary>
    private void EquipPassiveSkill(SkillInstance skill)
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null) return;
        
        if (skill.isEquipped)
        {
            return;
        }
        
        // 빈 슬롯 찾기
        int emptySlotIndex = FindEmptyPassiveSlot(slotData);
        
        // 빈 슬롯이 없으면 슬롯 0에 덮어씌우기
        if (emptySlotIndex == -1)
        {
            emptySlotIndex = 0;
            var oldSkill = GetEquippedPassiveSkill(slotData, 0);
        }
        
        // 기존 장착 스킬 해제
        string oldSkillID = slotData.equippedPassiveSkillIds[emptySlotIndex];
        if (!string.IsNullOrEmpty(oldSkillID))
        {
            UnequipSkillByID(slotData, oldSkillID);
        }
        
        // 새 스킬 장착
        skill.isEquipped = true;
        slotData.equippedPassiveSkillIds[emptySlotIndex] = skill.skillData.skillID;
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(slotData, skill);
        
        // SelectedPlayerData도 동기화
        if (playerDataManager.selectedPlayerData != null)
        {
            UpdateSelectedPlayerDataSkills(playerDataManager.selectedPlayerData, slotData);
        }
        
        playerDataManager.SaveCurrentSlot();
        
    }
    
    /// <summary>
    /// 빈 패시브 슬롯 찾기
    /// </summary>
    private int FindEmptyPassiveSlot(PlayerSlotData slotData)
    {
        for (int i = 0; i < slotData.equippedPassiveSkillIds.Length; i++)
        {
            if (string.IsNullOrEmpty(slotData.equippedPassiveSkillIds[i]))
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// 스킬 장착 해제
    /// </summary>
    public void UnequipSkill(SkillInstance skill)
    {
        if (skill == null) return;
        
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null) return;
        
        skill.isEquipped = false;
        
        // 슬롯에서 제거
        if (skill.IsActiveSkill)
        {
            for (int i = 0; i < slotData.equippedActiveSkillIds.Length; i++)
            {
                if (slotData.equippedActiveSkillIds[i] == skill.skillData.skillID)
                {
                    slotData.equippedActiveSkillIds[i] = null;
                }
            }
        }
        else if (skill.IsPassiveSkill)
        {
            for (int i = 0; i < slotData.equippedPassiveSkillIds.Length; i++)
            {
                if (slotData.equippedPassiveSkillIds[i] == skill.skillData.skillID)
                {
                    slotData.equippedPassiveSkillIds[i] = null;
                }
            }
        }
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(slotData, skill);
        
        // SelectedPlayerData도 동기화
        if (playerDataManager.selectedPlayerData != null)
        {
            UpdateSelectedPlayerDataSkills(playerDataManager.selectedPlayerData, slotData);
        }
        
        playerDataManager.SaveCurrentSlot();
        
        // UI 갱신
        RefreshUI();
        
    }
    
    /// <summary>
    /// skillID로 장착 해제
    /// </summary>
    private void UnequipSkillByID(PlayerSlotData slotData, string skillID)
    {
        var saveData = slotData.skills.Find(s => s.skillID == skillID);
        if (saveData != null)
        {
            saveData.isEquipped = false;
        }
    }
    
    /// <summary>
    /// SP 여유 확인 (SkillListItemUI에서 호출)
    /// </summary>
    public bool CanAffordSP(int amount)
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null) return false;
        
        int availableSP = slotData.totalSP - slotData.usedSP;
        return availableSP >= amount;
    }
    
    /// <summary>
    /// 빈 슬롯 찾기 (SkillListItemUI에서 호출)
    /// </summary>
    /// <param name="isActive">true = 액티브 스킬, false = 패시브 스킬</param>
    /// <returns>빈 슬롯 인덱스 (-1이면 슬롯이 가득 참)</returns>
    public int FindEmptySlot(bool isActive)
    {
        var slotData = playerDataManager.GetCurrentSlotData();
        if (slotData == null) return -1;
        
        if (isActive)
        {
            return FindEmptyActiveSlot(slotData);
        }
        else
        {
            return FindEmptyPassiveSlot(slotData);
        }
    }
    
    /// <summary>
    /// 탭이 활성화될 때 호출 (SkillBookPanelUI에서)
    /// </summary>
    public void OnTabActivated()
    {
        RefreshUI();
        
    }
    
    /// <summary>
    /// 탭이 비활성화될 때 호출
    /// </summary>
    public void OnTabDeactivated()
    {
        // 선택 해제
        if (currentSelectedItem != null)
        {
            currentSelectedItem.SetSelected(false);
            currentSelectedItem = null;
        }
        
        // 상세 패널 숨기기
        if (skillDetailPanel != null)
        {
            skillDetailPanel.Hide();
        }
        
    }
    
    /// <summary>
    /// SelectedPlayerData에 스킬 데이터 동기화
    /// </summary>
    private void UpdateSelectedPlayerDataSkills(SelectedPlayerData selectedData, PlayerSlotData slotData)
    {
        selectedData.skills = slotData.skills != null
            ? new List<SkillInstanceSaveData>(slotData.skills)
            : new List<SkillInstanceSaveData>();
        
        selectedData.equippedActiveSkillIds = slotData.equippedActiveSkillIds != null
            ? (string[])slotData.equippedActiveSkillIds.Clone()
            : new string[2];
        
        selectedData.equippedPassiveSkillIds = slotData.equippedPassiveSkillIds != null
            ? (string[])slotData.equippedPassiveSkillIds.Clone()
            : new string[3];
        
        selectedData.totalSP = slotData.totalSP;
        selectedData.usedSP = slotData.usedSP;
        
    }
    
    /// <summary>
    /// 경고 메시지 표시 (슬롯이 가득 찼을 때)
    /// </summary>
    public void ShowWarningMessage(string message)
    {
        if (warningMessageObject == null || warningMessageText == null)
        {
            Debug.LogWarning("[SkillTabController] 경고 메시지 UI가 설정되지 않았습니다!");
            return;
        }
        
        // 기존 코루틴 중지
        StopAllCoroutines();
        
        // 메시지 설정 및 표시
        warningMessageText.text = message;
        warningMessageObject.SetActive(true);
        
        // 일정 시간 후 자동 숨김
        StartCoroutine(HideWarningMessageAfterDelay());
        
    }
    
    /// <summary>
    /// 경고 메시지 자동 숨김 (코루틴)
    /// </summary>
    private System.Collections.IEnumerator HideWarningMessageAfterDelay()
    {
        yield return new WaitForSeconds(warningDisplayDuration);
        
        if (warningMessageObject != null)
        {
            warningMessageObject.SetActive(false);
        }
    }
}
