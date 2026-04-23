using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PC 전용 액션 HUD — 대시 / 기본공격 / 스킬1 / 스킬2 쿨다운 표시
///
/// ■ 에디터에서 해야 할 작업
///   1. UICanvas 하단 중앙에 PCActionHUD 패널 생성 후 이 컴포넌트 추가
///   2. 슬롯 4개 (Dash / Attack / Skill1 / Skill2) 각각에 아래 필드 연결:
///      - IconImage     : 스킬/공격 아이콘 Image
///      - CooldownOverlay: Image (Type=Filled, FillMethod=Radial360, FillOrigin=Top)
///      - CooldownText  : TMP_Text (슬롯 중앙, 쿨다운 중 "1.2s" 표시)
///   3. 스킬1/스킬2 IconImage 는 코드에서 자동 설정 가능
///      (BaseSkillData.icon 이 있으면 덮어씀, 없으면 에디터 설정 유지)
///
/// ■ 코드가 자동으로 처리
///   - fillAmount 갱신 (Radial 쿨다운 시각화)
///   - 잔여시간 텍스트 "1.2s" 표시 / 쿨다운 완료 시 숨김
///   - 스킬 장착 변경 시 아이콘 자동 갱신
///   - PC/에디터 빌드에서만 활성화 (모바일은 자동 비활성)
/// </summary>
public class PCActionHUDController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  슬롯 데이터 구조
    // ─────────────────────────────────────────────────────────────

    [System.Serializable]
    public class ActionSlot
    {
        [Tooltip("스킬/공격 아이콘 Image (에디터에서 스프라이트 설정)")]
        public Image iconImage;

        [Tooltip("쿨다운 오버레이 Image (Type=Filled, FillMethod=Radial360)")]
        public Image cooldownOverlay;

        [Tooltip("잔여시간 텍스트 (TMP_Text, 슬롯 중앙 배치)")]
        public TMP_Text cooldownText;
    }

    // ─────────────────────────────────────────────────────────────
    //  Inspector 필드
    // ─────────────────────────────────────────────────────────────

    [Header("액션 슬롯 — 에디터에서 연결")]
    [SerializeField] private ActionSlot dashSlot;
    [SerializeField] private ActionSlot attackSlot;
    [SerializeField] private ActionSlot skill1Slot;
    [SerializeField] private ActionSlot skill2Slot;

    [Header("갱신 주기 (초, 기본 0.05 = 20fps)")]
    [SerializeField] private float updateInterval = 0.05f;

    [Header("쿨다운 텍스트 표시 최소 시간 (이 값 이하면 숨김)")]
    [SerializeField] private float textHideThreshold = 0.05f;

    // ─────────────────────────────────────────────────────────────
    //  내부 레퍼런스
    // ─────────────────────────────────────────────────────────────

    private PlayerController         playerController;
    private PlayerAnimationController animController;
    private PlayerSkillManager        skillManager;

    private float _nextUpdateTime;

    // ─────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Start()
    {
#if !(UNITY_EDITOR || UNITY_STANDALONE)
        gameObject.SetActive(false);
        return;
#endif
        // 레퍼런스는 PlayerSpawner 스폰 완료 후 SetupPlayer() 로 주입받거나
        // 한 프레임 뒤 자동 탐색으로 폴백
        StartCoroutine(AutoFindReferencesCoroutine());
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Time.time < _nextUpdateTime) return;
        _nextUpdateTime = Time.time + updateInterval;
        UpdateAllCooldowns();
#endif
    }

    // ─────────────────────────────────────────────────────────────
    //  외부 API — PlayerSpawner 에서 스폰 완료 후 호출
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// PlayerSpawner 가 플레이어 스폰 완료 후 호출합니다.
    /// 레퍼런스를 직접 주입하고 스킬 아이콘을 갱신합니다.
    /// </summary>
    public void SetupPlayer(PlayerController pc,
                            PlayerAnimationController pac,
                            PlayerSkillManager psm)
    {
        playerController = pc;
        animController   = pac;
        skillManager     = psm;

        RefreshSkillIcons();
    }

    /// <summary>
    /// 스킬 장착 변경 시 호출하면 아이콘을 새로 가져옵니다.
    /// (에디터에서 아이콘을 직접 설정한 경우에는 호출 불필요)
    /// </summary>
    public void RefreshSkillIcons()
    {
        if (skillManager == null) return;

        SetSlotIcon(skill1Slot, skillManager.GetEquippedActiveSkill(0));
        SetSlotIcon(skill2Slot, skillManager.GetEquippedActiveSkill(1));
    }

    // ─────────────────────────────────────────────────────────────
    //  쿨다운 갱신
    // ─────────────────────────────────────────────────────────────

    private void UpdateAllCooldowns()
    {
        // 대시
        if (playerController != null)
        {
            playerController.GetDashCooldownInfo(out float rem, out float total);
            UpdateSlot(dashSlot, rem, total);
        }

        if (animController != null)
        {
            // 기본공격
            animController.GetAttackCooldownInfo(out float attackRem, out float attackTotal);
            UpdateSlot(attackSlot, attackRem, attackTotal);

            // 스킬1
            animController.GetSkill1CooldownInfo(out float sk1Rem, out float sk1Total);
            UpdateSlot(skill1Slot, sk1Rem, sk1Total);

            // 스킬2
            animController.GetSkill2CooldownInfo(out float sk2Rem, out float sk2Total);
            UpdateSlot(skill2Slot, sk2Rem, sk2Total);
        }
    }

    /// <summary>
    /// 슬롯 하나의 fillAmount 와 타이머 텍스트를 갱신합니다.
    /// remaining = 0 이면 쿨다운 없음(사용 가능) 상태입니다.
    /// </summary>
    private void UpdateSlot(ActionSlot slot, float remaining, float total)
    {
        if (slot == null) return;

        float fill = (total > 0f && remaining > 0f)
            ? Mathf.Clamp01(remaining / total)
            : 0f;

        // ── Radial 오버레이 ──────────────────────────────────────
        if (slot.cooldownOverlay != null)
            slot.cooldownOverlay.fillAmount = fill;

        // ── 잔여시간 텍스트 ───────────────────────────────────────
        if (slot.cooldownText != null)
        {
            if (remaining > textHideThreshold)
                slot.cooldownText.text = remaining.ToString("F1") + "s";
            else
                slot.cooldownText.text = string.Empty;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  아이콘 설정
    // ─────────────────────────────────────────────────────────────

    private void SetSlotIcon(ActionSlot slot, SkillInstance instance)
    {
        if (slot?.iconImage == null) return;
        if (instance?.skillData?.icon == null) return; // 아이콘 없으면 에디터 설정 유지

        slot.iconImage.sprite = instance.skillData.icon;
    }

    // ─────────────────────────────────────────────────────────────
    //  자동 레퍼런스 탐색 (폴백)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// SetupPlayer() 가 호출되기 전 Start() 에서 폴백으로 실행됩니다.
    /// 1프레임 대기 후 FindObjectOfType 으로 자동 탐색합니다.
    /// </summary>
    private IEnumerator AutoFindReferencesCoroutine()
    {
        yield return null; // PlayerSpawner 스폰 완료까지 대기

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (animController == null)
            animController = FindObjectOfType<PlayerAnimationController>();

        if (skillManager == null)
            skillManager = FindObjectOfType<PlayerSkillManager>();

        RefreshSkillIcons();
    }
}
