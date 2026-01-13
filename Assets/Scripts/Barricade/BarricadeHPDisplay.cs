using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ⭐ TextMeshPro 추가

/// <summary>
/// 바리케이드 HP/타수 표시 UI
/// HpBarMode에 따라 동적으로 표시 조절
/// TextMeshPro 지원
/// </summary>
public class BarricadeHPDisplay : MonoBehaviour
{
    // ========================================
    // UI 요소
    // ========================================
    [Header("==== HP 바 요소 ====")]
    [SerializeField] private GameObject hpBarRoot;     // HP 바 전체 루트
    [SerializeField] private Image fillImage;          // HP 바 채움 이미지
    
    [Header("==== 텍스트 (둘 중 하나 선택) ====")]
    [Tooltip("TextMeshPro 사용 (권장)")]
    [SerializeField] private TMP_Text hitsTextTMP;     // 타수/HP 텍스트 (TextMeshPro)
    
    [Tooltip("기존 Text 사용 (호환성)")]
    [SerializeField] private Text hitsText;            // 타수/HP 텍스트 (Legacy)
    
    // ========================================
    // 설정
    // ========================================
    private BarricadePreset preset;
    private Canvas canvas;
    private Camera mainCamera;
    
    // ========================================
    // 초기화
    // ========================================
    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main;
        
        if (canvas == null)
        {
            Debug.LogError("[BarricadeHPDisplay] Canvas 컴포넌트가 없습니다!");
        }
    }
    
    /// <summary>
    /// 프리셋 기반 초기화
    /// </summary>
    public void Initialize(BarricadePreset preset)
    {
        this.preset = preset;
        
        if (preset == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // HpBarMode에 따라 표시 설정
        ApplyHpBarMode();
    }
    
    private void ApplyHpBarMode()
    {
        switch (preset.hpBarMode)
        {
            case HpBarMode.Off:
                // 완전히 숨김
                gameObject.SetActive(false);
                break;
                
            case HpBarMode.Small:
                // 숫자만 표시
                gameObject.SetActive(true);
                if (hpBarRoot != null) hpBarRoot.SetActive(false);
                SetTextActive(true);
                break;
                
            case HpBarMode.Full:
                // 전체 표시
                gameObject.SetActive(true);
                if (hpBarRoot != null) hpBarRoot.SetActive(true);
                SetTextActive(true);
                break;
                
            case HpBarMode.OnlyImportant:
                // Important 카테고리만 표시
                bool isImportant = preset.category == BarricadeCategory.Important ||
                                   preset.category == BarricadeCategory.Boss ||
                                   preset.category == BarricadeCategory.Secret;
                
                if (isImportant)
                {
                    gameObject.SetActive(true);
                    if (hpBarRoot != null) hpBarRoot.SetActive(true);
                    SetTextActive(true);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                break;
        }
    }
    
    // ========================================
    // 업데이트
    // ========================================
    /// <summary>
    /// HP/타수 표시 업데이트
    /// </summary>
    /// <param name="currentValue">현재 값 (타수 또는 HP)</param>
    /// <param name="maxValue">최대 값</param>
    public void UpdateDisplay(int currentValue, int maxValue)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }
        
        // 타수 텍스트 업데이트
        UpdateText(currentValue, maxValue);
        
        // HP 바 업데이트
        UpdateFillBar(currentValue, maxValue);
    }
    
    private void UpdateText(int currentValue, int maxValue)
    {
        string displayText = "";
        
        if (preset.showRemainingHits)
        {
            // 남은 타수만 표시 (예: "3")
            int remaining = maxValue - currentValue;
            displayText = $"{remaining}";
        }
        else
        {
            // 현재/최대 형식 (예: "2/5")
            displayText = $"{currentValue}/{maxValue}";
        }
        
        // TextMeshPro 우선 사용
        if (hitsTextTMP != null && hitsTextTMP.gameObject.activeSelf)
        {
            hitsTextTMP.text = displayText;
        }
        // Legacy Text fallback
        else if (hitsText != null && hitsText.gameObject.activeSelf)
        {
            hitsText.text = displayText;
        }
    }
    
    /// <summary>
    /// 텍스트 활성화 (TMP 우선)
    /// </summary>
    private void SetTextActive(bool active)
    {
        if (hitsTextTMP != null)
        {
            hitsTextTMP.gameObject.SetActive(active);
        }
        else if (hitsText != null)
        {
            hitsText.gameObject.SetActive(active);
        }
    }
    
    private void UpdateFillBar(int currentValue, int maxValue)
    {
        if (fillImage == null || !fillImage.gameObject.activeSelf) return;
        
        // Fill Amount 계산
        float fillAmount = 1f - ((float)currentValue / maxValue);
        
        // BreakMode에 따라 반전
        if (preset.breakMode == BreakMode.HP)
        {
            fillAmount = (float)currentValue / maxValue;
        }
        
        fillImage.fillAmount = fillAmount;
        
        // 색상 변경 (초록 → 노랑 → 빨강)
        fillImage.color = GetHealthColor(fillAmount);
    }
    
    private Color GetHealthColor(float fillAmount)
    {
        if (fillAmount > 0.6f)
        {
            return new Color(0f, 1f, 0f, 1f); // 초록색
        }
        else if (fillAmount > 0.3f)
        {
            return new Color(1f, 1f, 0f, 1f); // 노란색
        }
        else
        {
            return new Color(1f, 0f, 0f, 1f); // 빨간색
        }
    }
    
    // ========================================
    // 카메라 빌보드
    // ========================================
    private void LateUpdate()
    {
        // 카메라를 향하도록 회전 (빌보드 효과)
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
}

