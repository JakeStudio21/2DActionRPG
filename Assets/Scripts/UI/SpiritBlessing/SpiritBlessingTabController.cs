using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 정령의 가호 탭 컨트롤러
/// Phase 2: Resistance System UI
/// 책임: 정령의 가호 UI 로직 관리 (좌측 종합 스탯, 우측 리스트, 하단 상세)
/// </summary>
public class SpiritBlessingTabController : MonoBehaviour
{
    [Header("=== 좌측: 종합 스탯 ===")]
    [SerializeField] private TMP_Text bindResistText;
    [SerializeField] private TMP_Text poisonResistText;
    [SerializeField] private TMP_Text burnResistText;
    [SerializeField] private TMP_Text slowResistText;
    
    [Header("=== 우측: 정령 리스트 ===")]
    [SerializeField] private Transform blessingListParent;      // Scroll View/Content
    [SerializeField] private GameObject blessingItemPrefab;     // SpiritBlessingItemUI
    [SerializeField] private List<SpiritBlessingData> blessingDatabase;
    
    [Header("=== 하단: 상세 정보 (표시 전용) ===")]
    [SerializeField] private GameObject bottomPanel;
    [SerializeField] private Image blessingIcon;
    [SerializeField] private TMP_Text blessingNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text costText;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // 현재 선택된 가호
    private SpiritBlessingData selectedBlessing;
    private SpiritBlessingItemUI selectedItemUI;
    
    // 생성된 리스트 아이템 추적
    private List<SpiritBlessingItemUI> instantiatedItems = new List<SpiritBlessingItemUI>();
    
    // 로비에서는 Player GameObject가 없으므로 SelectedPlayerData에서 직접 읽기
    private SelectedPlayerData selectedPlayerData;
    
    void Awake()
    {
        if (enableDebugLogs)
            Debug.Log("🌟 [SpiritBlessingTabController] Awake()");
    }
    
    void Start()
    {
        // SelectedPlayerData 가져오기
        GetSelectedPlayerData();
    }
    
    /// <summary>
    /// SelectedPlayerData 가져오기
    /// </summary>
    private void GetSelectedPlayerData()
    {
        if (selectedPlayerData == null)
        {
            if (PlayerDataManager.Instance != null)
            {
                selectedPlayerData = PlayerDataManager.Instance.selectedPlayerData;
                
                if (selectedPlayerData == null)
                {
                    Debug.LogError("🔴 [SpiritBlessingTabController] SelectedPlayerData를 찾을 수 없습니다!");
                }
                else if (enableDebugLogs)
                {
                    Debug.Log("✅ [SpiritBlessingTabController] SelectedPlayerData 가져오기 완료");
                }
            }
            else
            {
                Debug.LogError("🔴 [SpiritBlessingTabController] PlayerDataManager.Instance가 null입니다!");
            }
        }
    }
    
    /// <summary>
    /// 탭 활성화 시 호출 (외부에서)
    /// </summary>
    public void OnTabActivated()
    {
        if (enableDebugLogs)
            Debug.Log("🌟 [SpiritBlessingTabController] 탭 활성화");
        
        RefreshLeftPanelStats();     // 좌측 종합 스탯 갱신
        PopulateBlessingList();      // 우측 리스트 생성
        ClearBottomPanel();          // 하단 초기화
    }
    
    /// <summary>
    /// 탭 비활성화 시 호출 (외부에서)
    /// </summary>
    public void OnTabDeactivated()
    {
        if (enableDebugLogs)
            Debug.Log("🌟 [SpiritBlessingTabController] 탭 비활성화");
        
        // 정리 작업
        selectedBlessing = null;
        selectedItemUI = null;
    }
    
    /// <summary>
    /// 좌측 패널: 종합 스탯 갱신
    /// </summary>
    private void RefreshLeftPanelStats()
    {
        if (selectedPlayerData == null)
        {
            GetSelectedPlayerData();
            if (selectedPlayerData == null)
            {
                Debug.LogError("🔴 [SpiritBlessingTabController] SelectedPlayerData를 찾을 수 없습니다!");
                return;
            }
        }
        
        // SelectedPlayerData에서 저항 수치 읽어오기
        float bindResist = selectedPlayerData.GetResistanceStat(EStatusEffectType.Bind);
        float poisonResist = selectedPlayerData.GetResistanceStat(EStatusEffectType.Poison);
        float burnResist = selectedPlayerData.GetResistanceStat(EStatusEffectType.Burn);
        float slowResist = selectedPlayerData.GetResistanceStat(EStatusEffectType.Slow);
        
        // UI 텍스트 업데이트
        if (bindResistText != null)
            bindResistText.text = $"🌳 숲의 가호 (속박 내성): {bindResist * 100:F0}%";
        else
            Debug.LogError("🔴 [SpiritBlessingTabController] bindResistText가 null입니다! Inspector에서 연결하세요!");
        
        if (poisonResistText != null)
            poisonResistText.text = $"☠️ 독의 가호 (독 내성): {poisonResist * 100:F0}%";
        else
            Debug.LogError("🔴 [SpiritBlessingTabController] poisonResistText가 null입니다! Inspector에서 연결하세요!");
        
        if (burnResistText != null)
            burnResistText.text = $"🔥 불의 가호 (화상 내성): {burnResist * 100:F0}%";
        else
            Debug.LogError("🔴 [SpiritBlessingTabController] burnResistText가 null입니다! Inspector에서 연결하세요!");
        
        if (slowResistText != null)
            slowResistText.text = $"❄️ 얼음의 가호 (둔화 내성): {slowResist * 100:F0}%";
        else
            Debug.LogError("🔴 [SpiritBlessingTabController] slowResistText가 null입니다! Inspector에서 연결하세요!");
        
        if (enableDebugLogs)
            Debug.Log($"✅ [SpiritBlessingTabController] 좌측 스탯 갱신 완료 - Bind: {bindResist * 100:F0}%, Poison: {poisonResist * 100:F0}%");
    }
    
    /// <summary>
    /// 우측 패널: 정령 리스트 생성
    /// </summary>
    private void PopulateBlessingList()
    {
        // 기존 리스트 아이템 제거
        ClearBlessingList();
        
        if (blessingListParent == null)
        {
            Debug.LogError("🔴 [SpiritBlessingTabController] blessingListParent가 null입니다!");
            return;
        }
        
        if (blessingItemPrefab == null)
        {
            Debug.LogError("🔴 [SpiritBlessingTabController] blessingItemPrefab이 null입니다!");
            return;
        }
        
        if (blessingDatabase == null || blessingDatabase.Count == 0)
        {
            Debug.LogError($"🔴 [SpiritBlessingTabController] blessingDatabase가 비어있습니다! (Count: {(blessingDatabase == null ? "null" : blessingDatabase.Count.ToString())})");
            Debug.LogError("⚠️ Inspector에서 Blessing Database에 4개 ScriptableObject 에셋을 연결해주세요!");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"📋 [SpiritBlessingTabController] 리스트 생성 시작 - Database Count: {blessingDatabase.Count}");
        
        // 데이터베이스 순회하며 리스트 아이템 생성
        foreach (var blessingData in blessingDatabase)
        {
            if (blessingData == null)
            {
                Debug.LogWarning("⚠️ [SpiritBlessingTabController] blessingData가 null입니다 (스킵)");
                continue;
            }
            
            if (!blessingData.IsValid())
            {
                Debug.LogWarning($"⚠️ [SpiritBlessingTabController] 유효하지 않은 데이터: {blessingData.name}");
                continue;
            }
            
            // 프리팹 생성
            GameObject itemObj = Instantiate(blessingItemPrefab, blessingListParent);
            SpiritBlessingItemUI itemUI = itemObj.GetComponent<SpiritBlessingItemUI>();
            
            if (itemUI != null)
            {
                itemUI.Setup(blessingData, this);
                instantiatedItems.Add(itemUI);
                
                if (enableDebugLogs)
                    Debug.Log($"✅ [SpiritBlessingTabController] 리스트 아이템 생성: {blessingData.blessingName}");
            }
            else
            {
                Debug.LogError($"🔴 [SpiritBlessingTabController] SpiritBlessingItemUI 컴포넌트가 없습니다: {itemObj.name}");
                Destroy(itemObj);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"✅ [SpiritBlessingTabController] 리스트 생성 완료 - 총 {instantiatedItems.Count}개");
    }
    
    /// <summary>
    /// 기존 리스트 아이템 제거
    /// </summary>
    private void ClearBlessingList()
    {
        foreach (var item in instantiatedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }
        
        instantiatedItems.Clear();
        
        if (enableDebugLogs)
            Debug.Log("🗑️ [SpiritBlessingTabController] 리스트 아이템 제거 완료");
    }
    
    /// <summary>
    /// 리스트 아이템 클릭 시 (외부에서 호출)
    /// </summary>
    public void OnBlessingItemClicked(SpiritBlessingData data, SpiritBlessingItemUI itemUI)
    {
        selectedBlessing = data;
        selectedItemUI = itemUI;
        
        ShowBottomPanelDetails(data);
        
        if (enableDebugLogs)
            Debug.Log($"🎯 [SpiritBlessingTabController] 정령 선택: {data.blessingName}");
    }
    
    /// <summary>
    /// 하단 패널: 상세 정보 표시
    /// </summary>
    private void ShowBottomPanelDetails(SpiritBlessingData data)
    {
        if (bottomPanel != null)
            bottomPanel.SetActive(true);
        
        if (blessingIcon != null)
            blessingIcon.sprite = data.blessingIcon;
        
        if (blessingNameText != null)
            blessingNameText.text = data.blessingName;
        
        if (descriptionText != null)
            descriptionText.text = data.description;
        
        // 현재 → 다음 수치 표시
        float currentResist = selectedPlayerData.GetResistanceStat(data.targetEffectType);
        float nextResist = Mathf.Min(currentResist + data.incrementPerLevel, data.maxResistance);
        
        if (effectText != null)
        {
            effectText.text = $"다음 가호 받기 시: {data.GetEffectTypeName()} {currentResist * 100:F0}% → {nextResist * 100:F0}%";
        }
        
        // 재화 표시 (Phase 9: 실제 보유량 표시)
        if (costText != null)
        {
            var account = AccountDataManager.Instance;
            if (account != null)
            {
                MaterialType materialType = data.requiredMaterialType;
                int current = account.GetMaterialCount(materialType);
                int required = data.costPerLevel;
                string materialName = materialType.GetDisplayName();
                
                // 보유량에 따라 색상 변경
                string colorTag = current >= required ? "<color=#00FF00>" : "<color=#FF0000>";
                costText.text = $"필요 재료: {materialName}\n{colorTag}{current}</color> / {required}개";
            }
            else
            {
                costText.text = $"필요 재료: {data.requiredMaterialType.GetDisplayName()} ?/{data.costPerLevel}개";
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"✅ [SpiritBlessingTabController] 하단 패널 표시: {data.blessingName}");
    }
    
    /// <summary>
    /// 하단 패널 초기화
    /// </summary>
    private void ClearBottomPanel()
    {
        if (bottomPanel != null)
            bottomPanel.SetActive(false);
        
        selectedBlessing = null;
        selectedItemUI = null;
        
        if (enableDebugLogs)
            Debug.Log("🗑️ [SpiritBlessingTabController] 하단 패널 초기화");
    }
    
    /// <summary>
    /// 가호 받기 처리 (RightPanel의 리스트 아이템에서 호출)
    /// </summary>
    public void ProcessBlessingUpgrade(SpiritBlessingData data)
    {
        if (data == null)
        {
            Debug.LogError("🔴 [SpiritBlessingTabController] data가 null입니다!");
            return;
        }
        
        Debug.Log($"🎯 [SpiritBlessingTabController] [가호 받기] 처리 시작: {data.blessingName}");
        
        // 1. 최대치 체크
        float currentResist = selectedPlayerData.GetResistanceStat(data.targetEffectType);
        if (currentResist >= data.maxResistance)
        {
            Debug.LogWarning($"⚠️ [SpiritBlessingTabController] 이미 최대치입니다: {data.blessingName}");
            return;
        }
        
        // 2. 재료 소모 검사 및 실행
        if (!CheckAndConsumeMaterial(data))
        {
            Debug.LogWarning($"⚠️ [SpiritBlessingTabController] 재료 부족: {data.requiredMaterialType.GetDisplayName()}");
            return;
        }
        
        // 3. SelectedPlayerData에 저항 추가
        selectedPlayerData.AddResistanceStat(data.targetEffectType, data.incrementPerLevel);
        float newResist = selectedPlayerData.GetResistanceStat(data.targetEffectType);
        
        Debug.Log($"✅ [SpiritBlessingTabController] 저항 증가: {data.GetEffectTypeName()} {currentResist * 100:F0}% → {newResist * 100:F0}%");
        
        // 4. PlayerDataManager.SaveOnMeaningfulEvent() 호출하여 즉시 JSON 저장
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("BlessingUpgraded");
            Debug.Log("✅ [SpiritBlessingTabController] 데이터 저장 완료");
        }
        
        // 5. UI 전체 갱신 (재료 소모 + 저항 증가로 Grayscale 상태 변경될 수 있음)
        RefreshAllUI();
    }
    
    /// <summary>
    /// 재료 검사 및 소모 (Phase 9: 실제 인벤토리 연동)
    /// </summary>
    private bool CheckAndConsumeMaterial(SpiritBlessingData data)
    {
        if (data == null)
        {
            Debug.LogError("🔴 [SpiritBlessingTabController] SpiritBlessingData가 null입니다!");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        if (account == null)
        {
            Debug.LogError("🔴 [SpiritBlessingTabController] AccountDataManager가 null입니다!");
            return false;
        }
        
        MaterialType materialType = data.requiredMaterialType;
        int required = data.costPerLevel;
        int current = account.GetMaterialCount(materialType);
        
        // 1. 재료 충분한지 확인
        if (!account.HasMaterial(materialType, required))
        {
            // 재료 부족 알림
            string materialName = materialType.GetDisplayName();
            ShowInsufficientMaterialNotification(materialName, required, current);
            
            if (enableDebugLogs)
                Debug.LogWarning($"⚠️ [SpiritBlessingTabController] {materialName} 부족 (필요: {required}, 보유: {current})");
            
            return false;
        }
        
        // 2. 재료 소모
        bool success = account.RemoveMaterial(materialType, required);
        
        if (!success)
        {
            Debug.LogError($"🔴 [SpiritBlessingTabController] 재료 소모 실패: {materialType}");
            return false;
        }
        
        // 3. 계정 데이터 저장 (재료 변경사항)
        account.Save();
        
        if (enableDebugLogs)
        {
            string materialName = materialType.GetDisplayName();
            Debug.Log($"✅ [SpiritBlessingTabController] 재료 소모 성공: {materialName} -{required}개 (남은: {account.GetMaterialCount(materialType)}개)");
        }
        
        return true;
    }
    
    /// <summary>
    /// 재료 부족 알림 표시
    /// </summary>
    private void ShowInsufficientMaterialNotification(string materialName, int required, int current)
    {
        // NotificationManager가 있으면 사용 (인게임)
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowNotification($"{materialName} 부족! ({current}/{required})");
        }
        else
        {
            // 로비에서는 Debug.Log만 (나중에 로비 전용 팝업으로 교체 가능)
            Debug.Log($"💬 [알림] {materialName}이(가) 부족합니다! (보유: {current}개, 필요: {required}개)");
        }
    }
    
    /// <summary>
    /// 전체 UI 갱신
    /// </summary>
    private void RefreshAllUI()
    {
        // 좌측 종합 스탯 갱신
        RefreshLeftPanelStats();
        
        // 우측 리스트 아이템 전체 갱신 (모든 아이템의 현재값 업데이트)
        foreach (var item in instantiatedItems)
        {
            if (item != null)
            {
                item.RefreshCurrentValue();
            }
        }
        
        // 하단 패널 갱신 (선택된 아이템이 있을 경우)
        if (selectedBlessing != null)
        {
            ShowBottomPanelDetails(selectedBlessing);
        }
        
        Debug.Log("🔄 [SpiritBlessingTabController] 전체 UI 갱신 완료");
    }
}

