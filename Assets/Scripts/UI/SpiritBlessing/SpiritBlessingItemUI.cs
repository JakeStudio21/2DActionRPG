using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 정령의 가호 리스트 아이템 UI
/// Phase 2: Resistance System UI
/// 책임: Right Panel의 개별 리스트 아이템 표시 및 상호작용
/// </summary>
public class SpiritBlessingItemUI : MonoBehaviour
{
    [Header("=== UI 참조 ===")]
    [SerializeField] private Image blessingIcon;
    [SerializeField] private TMP_Text blessingNameText;
    [SerializeField] private TMP_Text effectTypeText;
    [SerializeField] private TMP_Text currentValueText;
    [SerializeField] private Button itemButton;              // 아이템 전체 클릭용
    [SerializeField] private Button receiveButton;           // [가호 받기] 버튼
    [SerializeField] private TMP_Text receiveButtonText;
    [SerializeField] private GameObject maxLevelOverlay;     // 최대치 달성 오버레이
    [SerializeField] private TMP_Text maxLevelText;
    
    [Header("=== Grayscale 처리 (룬/스킬 시스템과 동일) ===")]
    [Tooltip("미활성화 상태에 적용할 Grayscale Material (Assets/Materials/UI/UI_Grayscale)")]
    [SerializeField] private Material grayscaleMaterial;
    
    [Tooltip("전체 Prefab (텍스트+아이콘+배경+버튼)을 회색으로 처리")]
    [SerializeField] private bool applyGrayscaleToAllChildren = true;
    
    [Header("🎚️ 밝기 & 투명도 조절")]
    [Tooltip("비활성화 상태의 밝기 (0.0 = 완전히 어두움, 1.0 = 원본 밝기)")]
    [SerializeField] [Range(0f, 1f)] private float lockedBrightness = 0.6f;
    
    [Tooltip("비활성화 상태의 투명도 (0.0 = 완전히 투명, 1.0 = 불투명)")]
    [SerializeField] [Range(0f, 1f)] private float lockedOpacity = 1f;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // 데이터
    private SpiritBlessingData blessingData;
    private SpiritBlessingTabController controller;
    
    // Grayscale 원본 상태 저장용
    private System.Collections.Generic.Dictionary<Image, Material> originalImageMaterials = new System.Collections.Generic.Dictionary<Image, Material>();
    private System.Collections.Generic.Dictionary<Image, Color> originalImageColors = new System.Collections.Generic.Dictionary<Image, Color>();
    private System.Collections.Generic.Dictionary<TMP_Text, Color> originalTextColors = new System.Collections.Generic.Dictionary<TMP_Text, Color>();
    private bool isGrayscaleApplied = false;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Setup(SpiritBlessingData data, SpiritBlessingTabController ctrl)
    {
        blessingData = data;
        controller = ctrl;
        
        if (blessingData == null)
        {
            Debug.LogError("🔴 [SpiritBlessingItemUI] blessingData가 null입니다!");
            return;
        }
        
        // UI 업데이트
        if (blessingIcon != null)
            blessingIcon.sprite = data.blessingIcon;
        
        if (blessingNameText != null)
            blessingNameText.text = data.blessingName;
        
        if (effectTypeText != null)
            effectTypeText.text = data.GetEffectTypeName();
        
        // 원본 색상 저장 (Grayscale 복구용)
        SaveOriginalColors();
        
        RefreshCurrentValue();
        
        // 버튼 이벤트 연결
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnItemButtonClicked);
        }
        
        if (receiveButton != null)
        {
            receiveButton.onClick.RemoveAllListeners();
            receiveButton.onClick.AddListener(OnReceiveButtonClicked);
        }
        
        if (enableDebugLogs)
            Debug.Log($"✅ [SpiritBlessingItemUI] Setup 완료: {data.blessingName}");
    }
    
    /// <summary>
    /// 현재 저항값 갱신
    /// </summary>
    public void RefreshCurrentValue()
    {
        if (blessingData == null)
            return;
        
        // 컨트롤러를 통해 현재 저항값 읽기 (컨트롤러가 SelectedPlayerData 처리)
        float currentResist = GetCurrentResistance();
        
        if (currentValueText != null)
            currentValueText.text = $"현재: {currentResist * 100:F0}%";
        
        // 최대치 체크
        bool isMaxLevel = currentResist >= blessingData.maxResistance;
        
        if (maxLevelOverlay != null)
            maxLevelOverlay.SetActive(isMaxLevel);
        
        if (receiveButton != null)
            receiveButton.interactable = !isMaxLevel;
        
        if (isMaxLevel && receiveButtonText != null)
            receiveButtonText.text = "최대치";
        else if (receiveButtonText != null)
            receiveButtonText.text = "가호 받기";
        
        // 상태 판단 및 Grayscale 적용
        DetermineStateAndApplyVisuals();
        
        if (enableDebugLogs)
            Debug.Log($"🔄 [SpiritBlessingItemUI] 값 갱신: {blessingData.blessingName} - {currentResist * 100:F0}% (최대치: {isMaxLevel})");
    }
    
    /// <summary>
    /// 아이템 버튼 클릭 시 (전체 클릭)
    /// </summary>
    private void OnItemButtonClicked()
    {
        if (controller == null)
        {
            Debug.LogError("🔴 [SpiritBlessingItemUI] controller가 null입니다!");
            return;
        }
        
        if (blessingData == null)
        {
            Debug.LogError("🔴 [SpiritBlessingItemUI] blessingData가 null입니다!");
            return;
        }
        
        // 컨트롤러에 선택 알림
        controller.OnBlessingItemClicked(blessingData, this);
        
        if (enableDebugLogs)
            Debug.Log($"🎯 [SpiritBlessingItemUI] 아이템 클릭: {blessingData.blessingName}");
    }
    
    /// <summary>
    /// [가호 받기] 버튼 클릭 시 (RightPanel에서 직접 실행)
    /// </summary>
    private void OnReceiveButtonClicked()
    {
        if (controller == null)
        {
            Debug.LogError("🔴 [SpiritBlessingItemUI] controller가 null입니다!");
            return;
        }
        
        if (blessingData == null)
        {
            Debug.LogError("🔴 [SpiritBlessingItemUI] blessingData가 null입니다!");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"🎯 [SpiritBlessingItemUI] [가호 받기] 클릭: {blessingData.blessingName}");
        
        // 1. 먼저 아이템 선택 (하단 패널 갱신)
        controller.OnBlessingItemClicked(blessingData, this);
        
        // 2. 가호 받기 처리 (컨트롤러에 위임)
        controller.ProcessBlessingUpgrade(blessingData);
    }
    
    /// <summary>
    /// 현재 저항값 읽기 (SelectedPlayerData에서)
    /// </summary>
    private float GetCurrentResistance()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.selectedPlayerData == null)
            return 0f;
        
        var selectedPlayerData = PlayerDataManager.Instance.selectedPlayerData;
        
        // 헬퍼 메서드 사용
        return selectedPlayerData.GetResistanceStat(blessingData.targetEffectType);
    }
    
    #region Grayscale 처리
    
    /// <summary>
    /// 원본 Material과 Color 저장 (복구용)
    /// </summary>
    private void SaveOriginalColors()
    {
        // 모든 Image 컴포넌트 저장
        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (!originalImageMaterials.ContainsKey(img))
                originalImageMaterials[img] = img.material;
            if (!originalImageColors.ContainsKey(img))
                originalImageColors[img] = img.color;
        }
        
        // 모든 TextMeshProUGUI 컴포넌트 저장
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (!originalTextColors.ContainsKey(text))
                originalTextColors[text] = text.color;
        }
    }
    
    /// <summary>
    /// 전체 UI에 Grayscale 적용
    /// </summary>
    private void ApplyGrayscaleToAllChildren()
    {
        // 모든 Image에 Grayscale Material 적용
        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img != null && grayscaleMaterial != null)
            {
                img.material = grayscaleMaterial;
            }
            
            // 밝기와 투명도 조절
            Color adjustedColor = img.color;
            adjustedColor.r *= lockedBrightness;
            adjustedColor.g *= lockedBrightness;
            adjustedColor.b *= lockedBrightness;
            adjustedColor.a *= lockedOpacity;
            img.color = adjustedColor;
        }
        
        // 모든 텍스트에 회색 적용
        var texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (text != null)
            {
                Color adjustedColor = text.color;
                adjustedColor.r *= lockedBrightness;
                adjustedColor.g *= lockedBrightness;
                adjustedColor.b *= lockedBrightness;
                adjustedColor.a *= lockedOpacity;
                text.color = adjustedColor;
            }
        }
    }
    
    /// <summary>
    /// 원본 색상으로 복구
    /// </summary>
    private void RestoreOriginalColors()
    {
        // 모든 Image 복구
        foreach (var kvp in originalImageMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.material = kvp.Value;
            }
        }
        
        foreach (var kvp in originalImageColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }
        
        // 모든 텍스트 복구
        foreach (var kvp in originalTextColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }
    }
    
    /// <summary>
    /// [가호 받기] 버튼만 원본 색상으로 복구 (활성화 가능 상태 강조)
    /// </summary>
    private void RestoreReceiveButtonColor()
    {
        if (receiveButton == null)
            return;
        
        // 버튼의 Image 컴포넌트 복구
        var buttonImage = receiveButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (originalImageMaterials.ContainsKey(buttonImage))
                buttonImage.material = originalImageMaterials[buttonImage];
            if (originalImageColors.ContainsKey(buttonImage))
                buttonImage.color = originalImageColors[buttonImage];
        }
        
        // 버튼의 텍스트 복구
        if (receiveButtonText != null && originalTextColors.ContainsKey(receiveButtonText))
        {
            receiveButtonText.color = originalTextColors[receiveButtonText];
        }
    }
    
    /// <summary>
    /// [가호 받기] 버튼만 Grayscale 적용 (재료 부족 상태)
    /// </summary>
    private void ApplyGrayscaleToReceiveButtonOnly()
    {
        if (receiveButton == null)
            return;
        
        // 버튼의 Image 컴포넌트에 Grayscale 적용
        var buttonImage = receiveButton.GetComponent<Image>();
        if (buttonImage != null && grayscaleMaterial != null)
        {
            buttonImage.material = grayscaleMaterial;
            
            // 밝기와 투명도 조절
            Color adjustedColor = buttonImage.color;
            adjustedColor.r *= lockedBrightness;
            adjustedColor.g *= lockedBrightness;
            adjustedColor.b *= lockedBrightness;
            adjustedColor.a *= lockedOpacity;
            buttonImage.color = adjustedColor;
        }
        
        // 버튼의 텍스트에 회색 적용
        if (receiveButtonText != null)
        {
            Color adjustedColor = receiveButtonText.color;
            adjustedColor.r *= lockedBrightness;
            adjustedColor.g *= lockedBrightness;
            adjustedColor.b *= lockedBrightness;
            adjustedColor.a *= lockedOpacity;
            receiveButtonText.color = adjustedColor;
        }
    }
    
    /// <summary>
    /// 상태 판단 및 Grayscale 적용
    /// 1. 비활성화: 저항 0% AND 재료 < 10개 → 전체 Grayscale
    /// 2. 활성화 가능: 저항 0% + 재료 >= 10개 → 전체 Grayscale + [가호 받기] 버튼만 컬러
    /// 2-1. 활성화 + 버튼 비활성화: 저항 >= 1% + 재료 < 10개 → 전체 원래색 + [가호 받기] 버튼만 Grayscale
    /// 3. 활성화: 저항 >= 1% + 재료 >= 10개 → 전체 원래색
    /// </summary>
    private void DetermineStateAndApplyVisuals()
    {
        if (blessingData == null)
            return;
        
        float currentResist = GetCurrentResistance();
        int currentMaterial = GetCurrentMaterialCount();
        int requiredMaterial = blessingData.costPerLevel;
        
        // 1️⃣ 비활성화: 저항 0% AND 재료 부족
        bool isInactive = (currentResist <= 0f) && (currentMaterial < requiredMaterial);
        
        // 2️⃣ 활성화 가능: 저항 0% + 재료 충분
        bool isActivatable = (currentResist <= 0f) && (currentMaterial >= requiredMaterial);
        
        // 2️⃣-1️⃣ 활성화 + 버튼 비활성화: 저항 1% 이상 + 재료 부족 ⭐ 신규
        bool isActiveButMaterialInsufficient = (currentResist > 0f) && (currentMaterial < requiredMaterial);
        
        // 3️⃣ 완전 활성화: 저항 1% 이상 + 재료 충분
        bool isFullyActive = (currentResist > 0f) && (currentMaterial >= requiredMaterial);
        
        if (isInactive)
        {
            // 전체 Grayscale 적용
            if (applyGrayscaleToAllChildren && !isGrayscaleApplied)
            {
                ApplyGrayscaleToAllChildren();
                isGrayscaleApplied = true;
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [SpiritBlessingItemUI] {blessingData.blessingName}: 전체 Grayscale (비활성화 - 저항 0% AND 재료 부족)");
            }
        }
        else if (isActivatable)
        {
            // 전체 Grayscale + [가호 받기] 버튼만 컬러 ⭐
            if (applyGrayscaleToAllChildren && !isGrayscaleApplied)
            {
                ApplyGrayscaleToAllChildren();
                isGrayscaleApplied = true;
            }
            
            // [가호 받기] 버튼만 원본 색상으로 복구 (강조)
            RestoreReceiveButtonColor();
            
            if (enableDebugLogs)
                Debug.Log($"🎨 [SpiritBlessingItemUI] {blessingData.blessingName}: 전체 Grayscale + [가호 받기] 버튼만 컬러 (활성화 가능 - 저항 0% + 재료 충분)");
        }
        else if (isActiveButMaterialInsufficient)
        {
            // 전체 정상 색상 + [가호 받기] 버튼만 Grayscale ⭐ 신규
            if (isGrayscaleApplied)
            {
                RestoreOriginalColors();
                isGrayscaleApplied = false;
            }
            
            // [가호 받기] 버튼만 Grayscale 적용
            ApplyGrayscaleToReceiveButtonOnly();
            
            if (enableDebugLogs)
                Debug.Log($"🎨 [SpiritBlessingItemUI] {blessingData.blessingName}: 전체 원래색 + [가호 받기] 버튼만 Grayscale (활성화 - 저항 {currentResist * 100:F0}% + 재료 부족)");
        }
        else if (isFullyActive)
        {
            // 전체 정상 색상
            if (isGrayscaleApplied)
            {
                RestoreOriginalColors();
                isGrayscaleApplied = false;
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [SpiritBlessingItemUI] {blessingData.blessingName}: 전체 원래색 (완전 활성화 - 저항 {currentResist * 100:F0}% + 재료 충분)");
            }
        }
    }
    
    /// <summary>
    /// 현재 재료 보유량 읽기 (AccountDataManager에서)
    /// </summary>
    private int GetCurrentMaterialCount()
    {
        if (blessingData == null)
            return 0;
        
        var account = AccountDataManager.Instance;
        if (account == null)
            return 0;
        
        return account.GetMaterialCount(blessingData.requiredMaterialType);
    }
    
    #endregion
    
    void OnDestroy()
    {
        // 이벤트 리스너 정리
        if (itemButton != null)
            itemButton.onClick.RemoveAllListeners();
        
        if (receiveButton != null)
            receiveButton.onClick.RemoveAllListeners();
    }
}

