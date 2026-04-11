#if DEVELOPMENT_BUILD || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DebugTools
{
    /// <summary>
    /// 모바일용 플레이어 디버그 패널
    /// — PlayerDebugTools(EditorWindow)의 모든 기능을 런타임 MonoBehaviour로 포팅
    /// — DEVELOPMENT_BUILD 또는 UNITY_EDITOR 에서만 컴파일/동작
    /// </summary>
    public class MobilePlayerDebugPanel : MonoBehaviour
    {
        // ================================================
        // 📊 현재 상태 표시
        // ================================================
        [Header("📊 상태 표시 텍스트")]
        [SerializeField] private TextMeshProUGUI statusText;

        // ================================================
        // 🔢 입력 필드 (커스텀 수량)
        // ================================================
        [Header("🔢 커스텀 수량 입력")]
        [SerializeField] private TMP_InputField expInputField;
        [SerializeField] private TMP_InputField goldInputField;
        [SerializeField] private TMP_InputField staminaInputField;
        [SerializeField] private TMP_InputField ticketInputField;
        [SerializeField] private TMP_InputField spInputField;
        [SerializeField] private TMP_InputField levelInputField;
        [SerializeField] private TMP_InputField runeFragInputField;
        [SerializeField] private TMP_InputField materialInputField;
        [SerializeField] private TMP_InputField stageIdInputField;
        [SerializeField] private TMP_InputField equipItemIdInputField;

        // ================================================
        // 기본 수량 (입력 필드가 비었을 때 사용)
        // ================================================
        private int defaultExp        = 500;
        private int defaultGold       = 10000;
        private int defaultStamina    = 10;
        private int defaultTicket     = 5;
        private int defaultSP         = 10;
        private int defaultLevel      = 10;
        private int defaultRuneFrag   = 100;
        private int defaultMaterial   = 100;

        // ================================================
        // 라이프사이클
        // ================================================
        private void OnEnable()
        {
            RefreshStatus();
        }

        // ================================================
        // 📊 상태 갱신
        // ================================================
        public void RefreshStatus()
        {
            if (statusText == null) return;

            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                statusText.text = "❌ 캐릭터가 선택되지 않았습니다.";
                return;
            }

            var slot     = PlayerDataManager.Instance.GetCurrentSlotData();
            var selected = PlayerDataManager.Instance.selectedPlayerData;

            if (slot == null || selected == null)
            {
                statusText.text = "❌ 슬롯 데이터를 불러올 수 없습니다.";
                return;
            }

            int stamina    = 0;
            int maxStamina = 50;
            int tickets    = 0;

            if (ContentEntryManager.Instance != null)
            {
                stamina    = ContentEntryManager.Instance.GetCurrentStamina();
                maxStamina = ContentEntryManager.Instance.GetMaxStamina();
                tickets    = ContentEntryManager.Instance.GetTicketCount();
            }
            else if (AccountDataManager.Instance != null)
            {
                var acc = AccountDataManager.Instance.GetAccountData();
                if (acc != null) { stamina = acc.currentStamina; tickets = acc.dailyDungeonTickets; }
            }

            statusText.text =
                $"👤 {slot.playerName} ({slot.playerType})\n" +
                $"Lv.{slot.level}  EXP {selected.currentExp}/{selected.expToNextLevel}\n" +
                $"골드: {AccountDataManager.Instance?.CurrentGold ?? 0}G\n" +
                $"SP: {slot.usedSP}/{slot.totalSP}\n" +
                $"스태미나: {stamina}/{maxStamina}  티켓: {tickets}장";
        }

        // ================================================
        // 💎 경험치
        // ================================================
        public void AddExpFromInput()  => AddExp(ParseInput(expInputField, defaultExp));
        public void AddExp100()        => AddExp(100);
        public void AddExp500()        => AddExp(500);
        public void AddExp1000()       => AddExp(1000);

        private void AddExp(int amount)
        {
            if (!CheckReady()) return;
            PlayerDataManager.Instance.AddExp(amount);
            Debug.Log($"✅ [MobileDebug] 경험치 +{amount}");
            RefreshStatus();
        }

        // ================================================
        // 💰 골드
        // ================================================
        public void AddGoldFromInput() => AddGold(ParseInput(goldInputField, defaultGold));
        public void AddGold1000()      => AddGold(1000);
        public void AddGold10000()     => AddGold(10000);
        public void AddGold100000()    => AddGold(100000);

        private void AddGold(int amount)
        {
            if (!CheckReady()) return;
            AccountDataManager.Instance.AddGold(amount);
            Debug.Log($"✅ [MobileDebug] 골드 +{amount}");
            RefreshStatus();
        }

        // ================================================
        // ⚡ 스태미나
        // ================================================
        public void AddStaminaFromInput() => AddStamina(ParseInput(staminaInputField, defaultStamina));
        public void AddStamina5()         => AddStamina(5);
        public void AddStamina10()        => AddStamina(10);
        public void SetStaminaMax()
        {
            if (AccountDataManager.Instance == null) return;
            var acc = AccountDataManager.Instance.GetAccountData();
            if (acc == null) return;
            acc.currentStamina       = 50;
            acc.lastStaminaUpdateTime = "";
            AccountDataManager.Instance.Save();
            Debug.Log("✅ [MobileDebug] 스태미나 MAX(50)");
            RefreshStatus();
        }

        private void AddStamina(int amount)
        {
            if (AccountDataManager.Instance == null) return;
            var acc = AccountDataManager.Instance.GetAccountData();
            if (acc == null) return;
            acc.currentStamina = Mathf.Min(50, acc.currentStamina + amount);
            if (acc.currentStamina >= 50) acc.lastStaminaUpdateTime = "";
            AccountDataManager.Instance.Save();
            Debug.Log($"✅ [MobileDebug] 스태미나 +{amount} (현재: {acc.currentStamina}/50)");
            RefreshStatus();
        }

        // ================================================
        // 🎫 던전 티켓
        // ================================================
        public void AddTicketsFromInput() => AddDungeonTickets(ParseInput(ticketInputField, defaultTicket));
        public void AddTickets5()         => AddDungeonTickets(5);
        public void AddTickets10()        => AddDungeonTickets(10);
        public void AddTickets50()        => AddDungeonTickets(50);

        private void AddDungeonTickets(int amount)
        {
            if (ContentEntryManager.Instance == null) { Debug.LogWarning("[MobileDebug] ContentEntryManager 없음"); return; }
            ContentEntryManager.Instance.AddDungeonTickets(amount);
            Debug.Log($"✅ [MobileDebug] 던전 티켓 +{amount}장");
            RefreshStatus();
        }

        // ================================================
        // 📚 SP
        // ================================================
        public void AddSPFromInput() => AddSP(ParseInput(spInputField, defaultSP));
        public void AddSP10()        => AddSP(10);
        public void AddSP30()        => AddSP(30);
        public void ResetSP()
        {
            if (!CheckReady()) return;
            var slot     = PlayerDataManager.Instance.GetCurrentSlotData();
            var selected = PlayerDataManager.Instance.selectedPlayerData;
            slot.totalSP = slot.level;
            slot.usedSP  = 0;
            if (selected != null) { selected.totalSP = slot.totalSP; selected.usedSP = 0; }
            PlayerDataManager.Instance.SaveCurrentSlot();
            Debug.Log($"✅ [MobileDebug] SP 초기화 (레벨과 동기화: {slot.totalSP})");
            RefreshStatus();
        }

        private void AddSP(int amount)
        {
            if (!CheckReady()) return;
            var slot     = PlayerDataManager.Instance.GetCurrentSlotData();
            var selected = PlayerDataManager.Instance.selectedPlayerData;
            slot.totalSP += amount;
            if (selected != null) selected.totalSP = slot.totalSP;
            PlayerDataManager.Instance.SaveCurrentSlot();
            Debug.Log($"✅ [MobileDebug] SP +{amount} (현재: {slot.totalSP})");
            RefreshStatus();
        }

        // ================================================
        // 🆙 레벨 설정
        // ================================================
        public void SetLevelFromInput() => SetLevel(ParseInput(levelInputField, defaultLevel));
        public void SetLevel5()         => SetLevel(5);
        public void SetLevel10()        => SetLevel(10);
        public void SetLevel20()        => SetLevel(20);
        public void SetLevel30()        => SetLevel(30);

        private void SetLevel(int level)
        {
            if (!CheckReady()) return;
            var selected = PlayerDataManager.Instance.selectedPlayerData;
            var slot     = PlayerDataManager.Instance.GetCurrentSlotData();

            selected.currentLevel    = level;
            selected.currentExp      = 0;
            selected.expToNextLevel  = CalculateExpForLevel(level);
            selected.totalSP         = level;

            slot.level        = level;
            slot.exp          = 0;
            slot.expToNextLevel = selected.expToNextLevel;
            slot.totalSP      = level;

            PlayerDataManager.Instance.TriggerLevelChanged(level);
            PlayerDataManager.Instance.SaveCurrentSlot();
            Debug.Log($"✅ [MobileDebug] 레벨 → {level} (SP: {level})");
            RefreshStatus();
        }

        // ================================================
        // 🔷 룬 조각 (8종)
        // ================================================
        public void AddRuneBossHunter()     => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER);
        public void AddRuneDefenseBreaker() => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_DEFENSE_BREAKER);
        public void AddRuneHighHpHunter()   => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_HIGH_HP_HUNTER);
        public void AddRuneExecutioner()    => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_EXECUTIONER);
        public void AddRuneBossDefender()   => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_BOSS_DEFENDER);
        public void AddRuneSurvivor()       => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_SURVIVOR);
        public void AddRuneAreaDefender()   => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_AREA_DEFENDER);
        public void AddRuneVampire()        => AddRuneFrag(MaterialType.RUNE_FRAG_RUNE_VAMPIRE);
        public void AddAllRuneFragments100()  => AddAllRuneFragments(100);
        public void AddAllRuneFragments1000() => AddAllRuneFragments(1000);

        private void AddRuneFrag(MaterialType type)
        {
            int amount = ParseInput(runeFragInputField, defaultRuneFrag);
            if (AccountDataManager.Instance == null) return;
            AccountDataManager.Instance.AddMaterial(type, amount);
            AccountDataManager.Instance.Save();
            Debug.Log($"✅ [MobileDebug] {type} +{amount}");
        }

        private void AddAllRuneFragments(int amount)
        {
            if (AccountDataManager.Instance == null) return;
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER,     amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_BOSS_DEFENDER,   amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_DEFENSE_BREAKER, amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_HIGH_HP_HUNTER,  amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_EXECUTIONER,     amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_SURVIVOR,        amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_AREA_DEFENDER,   amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_VAMPIRE,         amount);
            AccountDataManager.Instance.Save();
            Debug.Log($"✅ [MobileDebug] 모든 룬 조각 +{amount} (8종)");
        }

        // ================================================
        // 📦 강화 재료 (9종)
        // ================================================
        public void AddWeaponFragment()     => AddMat(MaterialType.WeaponFragment);
        public void AddWeaponCrystal()      => AddMat(MaterialType.WeaponCrystal);
        public void AddWeaponCore()         => AddMat(MaterialType.WeaponCore);
        public void AddArmorFragment()      => AddMat(MaterialType.ArmorFragment);
        public void AddArmorCrystal()       => AddMat(MaterialType.ArmorCrystal);
        public void AddArmorCore()          => AddMat(MaterialType.ArmorCore);
        public void AddAccessoryFragment()  => AddMat(MaterialType.AccessoryFragment);
        public void AddAccessoryCrystal()   => AddMat(MaterialType.AccessoryCrystal);
        public void AddAccessoryCore()      => AddMat(MaterialType.AccessoryCore);
        public void AddAllMaterials100()    => AddAllMaterials(100);
        public void AddAllMaterials1000()   => AddAllMaterials(1000);

        private void AddMat(MaterialType type)
        {
            int amount = ParseInput(materialInputField, defaultMaterial);
            if (AccountDataManager.Instance == null) return;
            AccountDataManager.Instance.AddMaterial(type, amount);
            AccountDataManager.Instance.Save();
            Debug.Log($"✅ [MobileDebug] {type} +{amount}");
        }

        private void AddAllMaterials(int amount)
        {
            if (AccountDataManager.Instance == null) return;
            AccountDataManager.Instance.AddMaterial(MaterialType.WeaponFragment,    amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.WeaponCrystal,     amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.WeaponCore,        amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.ArmorFragment,     amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCrystal,      amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCore,         amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryFragment, amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryCrystal,  amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryCore,     amount);
            AccountDataManager.Instance.Save();
            Debug.Log($"✅ [MobileDebug] 모든 강화 재료 +{amount} (9종)");
        }

        // ================================================
        // 🌟 정령의 정수 (4종)
        // ================================================
        public void AddSpiritForest() => AddSpirit(MaterialType.SPIRIT_ESSENCE_FOREST);
        public void AddSpiritFlame()  => AddSpirit(MaterialType.SPIRIT_ESSENCE_FLAME);
        public void AddSpiritEarth()  => AddSpirit(MaterialType.SPIRIT_ESSENCE_EARTH);
        public void AddSpiritWater()  => AddSpirit(MaterialType.SPIRIT_ESSENCE_WATER);
        public void AddAllSpiritEssences100()  => AddAllSpiritEssences(100);
        public void AddAllSpiritEssences1000() => AddAllSpiritEssences(1000);

        private void AddSpirit(MaterialType type)
        {
            int amount = ParseInput(materialInputField, defaultMaterial);
            if (AccountDataManager.Instance == null) return;
            AccountDataManager.Instance.AddMaterial(type, amount);
            AccountDataManager.Instance.Save();
            Debug.Log($"✅ [MobileDebug] {type} +{amount}");
        }

        private void AddAllSpiritEssences(int amount)
        {
            if (AccountDataManager.Instance == null) return;
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_FOREST, amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_FLAME,  amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_EARTH,  amount);
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_WATER,  amount);
            AccountDataManager.Instance.Save();
            Debug.Log($"✅ [MobileDebug] 모든 정령의 정수 +{amount} (4종)");
        }

        // ================================================
        // 🎯 스테이지 클리어
        // ================================================
        public void CompleteStageFromInput() => CompleteStageDebug(stageIdInputField != null ? stageIdInputField.text : "");
        public void CompleteCH01ST01() => CompleteStageDebug("CH01_ST01");
        public void CompleteCH01ST02() => CompleteStageDebug("CH01_ST02");
        public void CompleteCH01ST03() => CompleteStageDebug("CH01_ST03");
        public void CompleteCH01ST05() => CompleteStageDebug("CH01_ST05");
        public void CompleteCH01ST10() => CompleteStageDebug("CH01_ST10");
        public void CompleteCH02ST01() => CompleteStageDebug("CH02_ST01");
        public void CompleteCH02ST05() => CompleteStageDebug("CH02_ST05");
        public void CompleteCH02ST10() => CompleteStageDebug("CH02_ST10");

        private void CompleteStageDebug(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) { Debug.LogError("❌ [MobileDebug] 스테이지 ID가 비어있습니다."); return; }
            var pm = StageSystem.StageProgressManager.Instance;
            if (pm == null) { Debug.LogError("❌ [MobileDebug] StageProgressManager 없음"); return; }

            if (!pm.IsStageUnlocked(stageId)) { pm.UnlockStage(stageId); Debug.Log($"🔓 [MobileDebug] {stageId} 강제 해금"); }

            if (!pm.IsStageCompleted(stageId))
            {
                pm.CompleteStage(stageId, completionTime: 60f, isFirstClear: true);
                Debug.Log($"✅ [MobileDebug] {stageId} 클리어 처리 완료");
            }
            else
            {
                Debug.LogWarning($"⚠️ [MobileDebug] {stageId} 이미 클리어된 상태");
            }

            PlayerDataManager.Instance?.SaveCurrentSlot();
            AccountDataManager.Instance?.Save();
        }

        // ================================================
        // ⚔️ 장비 획득
        // ================================================
        public void AcquireEquipFromInput()
        {
            string itemId = equipItemIdInputField != null ? equipItemIdInputField.text.Trim() : "";
            AcquireEquipmentForDebug(itemId);
        }

        private void AcquireEquipmentForDebug(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId)) { Debug.LogError("❌ [MobileDebug] 아이템 ID가 비어있습니다."); return; }
            if (AccountDataManager.Instance == null) { Debug.LogError("❌ [MobileDebug] AccountDataManager 없음"); return; }

            var newId = AccountDataManager.Instance.RegisterNewInstance(itemId);
            if (newId.IsEmpty) { Debug.LogError($"❌ [MobileDebug] 인스턴스 등록 실패: {itemId}"); return; }

            EquipmentData equipData = ItemTemplateResolver.Load(itemId);
            if (equipData != null)
            {
                var dyn = DynamicEquipmentGenerator.Generate(equipData, equipData.itemGrade);
                if (dyn != null)
                {
                    var inst = AccountDataManager.Instance.GetInstance(newId);
                    if (inst != null) EquipmentInstanceConverter.ApplyDynamicStats(inst, dyn);
                }
            }

            bool addedToShared = AccountDataManager.Instance.TryAddToShared(newId);
            if (!addedToShared) AccountDataManager.Instance.MoveToMailbox(newId);

            AccountDataManager.Instance.Save();
            PlayerDataManager.Instance?.TriggerInventoryChanged();

            string dest = addedToShared ? "공유 창고" : "우편함";
            Debug.Log($"✅ [MobileDebug] 장비 획득 성공: {itemId} → {dest}");
        }

        // ================================================
        // 💾 저장
        // ================================================
        public void SaveAll()
        {
            PlayerDataManager.Instance?.SaveCurrentSlot();
            AccountDataManager.Instance?.Save();
            Debug.Log("✅ [MobileDebug] 저장 완료");
            RefreshStatus();
        }

        // ================================================
        // 🔒 닫기
        // ================================================
        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        // ================================================
        // 유틸리티
        // ================================================
        private bool CheckReady()
        {
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                Debug.LogWarning("⚠️ [MobileDebug] 캐릭터가 선택되지 않았습니다.");
                return false;
            }
            return true;
        }

        private int ParseInput(TMP_InputField field, int fallback)
        {
            if (field == null) return fallback;
            if (int.TryParse(field.text, out int result) && result > 0) return result;
            return fallback;
        }

        private static int CalculateExpForLevel(int level)
        {
            return 100 + (level - 1) * 50;
        }
    }
}
#endif
