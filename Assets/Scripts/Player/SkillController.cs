using UnityEngine;
using System.Collections;

public class SkillController : MonoBehaviour
{
    public GameObject arrowPrefab;
    [HideInInspector] public float cooldownTime = 2f;
    public float arrowSpeed = 10f;
    public int arrowCount = 5;
    public float spreadAngle = 15f;

    private float lastSkillTime = -Mathf.Infinity;
    public System.Action<float> OnSkillCooldownChanged;

    // Bow 방향 및 Arrow Spawn Point 참조
    private Transform bowTransform;
    private Transform firePoint;

    // 쿨다운 시간 외부 접근용 프로퍼티
    public float CooldownTime => cooldownTime;

    void Awake()
    {
        // SkillUIController의 쿨다운 값을 우선 적용
        var skillUI = FindObjectOfType<SkillUIController>();
        if (skillUI != null)
        {
            cooldownTime = skillUI.cooldownTime;
        }
    }

    void Start()
    {
        // Bow 오브젝트를 ActiveWeapon에서 찾아 참조
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
        {
            bowTransform = activeWeapon.CurrentActiveWeapon.transform;
            // Arrow Spawn Point 찾기
            firePoint = bowTransform.Find("Arrow Spawn Point");
        }
        // Start에서도 한 번 더 동기화(혹시 Awake 타이밍 이슈 대비)
        var skillUI2 = FindObjectOfType<SkillUIController>();
        if (skillUI2 != null)
        {
            cooldownTime = skillUI2.cooldownTime;
        }
    }

    void Update()
    {
        // Bow 오브젝트가 동적으로 바뀔 수 있으므로 매 프레임 체크
        if (bowTransform == null || firePoint == null)
        {
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
            {
                bowTransform = activeWeapon.CurrentActiveWeapon.transform;
                firePoint = bowTransform.Find("Arrow Spawn Point");
            }
        }

        // S키 입력 시 스킬 발사
        if (Input.GetKeyDown(KeyCode.S) && Time.time >= lastSkillTime + cooldownTime)
        {
            FireSkill();
            lastSkillTime = Time.time;
            if (OnSkillCooldownChanged != null)
                OnSkillCooldownChanged(cooldownTime);
        }
    }

    void FireSkill()
    {
        if (bowTransform == null || firePoint == null) return;
        Vector2 baseDir = bowTransform.right;
        float startAngle = -spreadAngle * (arrowCount - 1) / 2f;
        for (int i = 0; i < arrowCount; i++)
        {
            float angle = startAngle + spreadAngle * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
            GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
            arrow.GetComponent<Rigidbody2D>().velocity = dir.normalized * arrowSpeed;
            arrow.transform.right = dir;
        }
    }

    public float GetCooldownRemaining()
    {
        float elapsed = Time.time - lastSkillTime;
        return Mathf.Clamp(cooldownTime - elapsed, 0, cooldownTime);
    }
} 