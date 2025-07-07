using UnityEngine;
using UnityEngine.UI;

public class SkillUIController : MonoBehaviour
{
    public Image cooldownImage;
    [Tooltip("스킬 쿨다운 시간(초) - 여기서만 입력!")]
    public float cooldownTime = 2f; // 쿨다운 시간은 여기서만 입력!
    private SkillController skillController;

    void Start()
    {
        Debug.Log("[SkillUI] Start 실행됨");
        skillController = FindObjectOfType<SkillController>();
        if (skillController == null)
            Debug.LogWarning("[SkillUI] SkillController를 찾지 못함!");
        else
            Debug.Log("[SkillUI] SkillController를 정상적으로 찾음!");

        if (skillController != null)
        {
            // SkillController의 쿨다운 시간은 항상 UI에서 입력한 값으로 동기화
            skillController.cooldownTime = cooldownTime;
        }
    }

    void Update()
    {
        if (skillController != null && cooldownImage != null)
        {
            float remain = skillController.GetCooldownRemaining();
            cooldownImage.fillAmount = remain / skillController.CooldownTime;
            Debug.Log($"[SkillUI] remain: {remain}, fill: {cooldownImage.fillAmount}, cooldown: {skillController.CooldownTime}");
        }
    }
} 