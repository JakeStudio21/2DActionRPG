using UnityEngine;
using System.Collections;

public class SkillController : MonoBehaviour
{
    [Header("스킬1: Multi-Arrow")]
    public GameObject arrowPrefab;
    [HideInInspector] public float cooldownTime = 2f;
    public float arrowSpeed = 10f;
    public int arrowCount = 5;
    public float spreadAngle = 15f;

    [Header("스킬2: Power Arrow")]
    public GameObject powerArrowPrefab; // 스킬2용 강력한 화살 프리팹
    [HideInInspector] public float skill2CooldownTime = 3f; // 스킬2 쿨다운 (더 김)
    public float powerArrowSpeed = 15f; // 더 빠른 속도
    public float powerArrowScale = 1.5f; // 더 큰 크기

    private float lastSkillTime = -Mathf.Infinity;
    private float lastSkill2Time = -Mathf.Infinity; // ⭐ 스킬2 쿨다운 추가
    
    public System.Action<float> OnSkillCooldownChanged;
    public System.Action<float> OnSkill2CooldownChanged; // ⭐ 스킬2 쿨다운 이벤트

    // Bow 방향 및 Arrow Spawn Point 참조
    private Transform bowTransform;
    private Transform firePoint;

    // 쿨다운 시간 외부 접근용 프로퍼티
    public float CooldownTime => cooldownTime;
    public float Skill2CooldownTime => skill2CooldownTime; // ⭐ 스킬2 쿨다운 프로퍼티

    void Awake()
    {
        // SkillUIController의 쿨다운 값을 우선 적용
        var skillUI = FindObjectOfType<SkillUIController>();
        if (skillUI != null)
        {
            cooldownTime = skillUI.cooldownTime;
        }
        
        // ⭐ 스킬2 UI 컨트롤러 찾기 (향후 구현 예정)
        var skill2UI = FindObjectOfType<Skill2UIController>();
        if (skill2UI != null)
        {
            skill2CooldownTime = skill2UI.cooldownTime;
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
        
        // ⭐ powerArrowPrefab이 없으면 기본 arrowPrefab 사용
        if (powerArrowPrefab == null)
        {
            powerArrowPrefab = arrowPrefab;
            Debug.LogWarning("[SkillController] powerArrowPrefab이 할당되지 않아 기본 arrowPrefab을 사용합니다.");
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

        // S키 입력은 PlayerAttackInput에서 처리하고 TriggerSkill() 호출
        // (중복 방지를 위해 주석처리)
    }

    /// <summary>
    /// 스킬1 실행 요청 (PlayerAttackInput에서 호출)
    /// </summary>
    public void TriggerSkill()
    {
        if (Time.time >= lastSkillTime + cooldownTime)
        {
            FireSkill();
            lastSkillTime = Time.time;
            if (OnSkillCooldownChanged != null)
                OnSkillCooldownChanged(cooldownTime);
            
            Debug.Log($"🟢 [SkillController] 스킬1(Multi-Arrow) 실행!");
        }
        else
        {
            Debug.LogWarning($"🟡 [SkillController] 스킬1 쿨다운 중! 남은 시간: {GetCooldownRemaining():F1}초");
        }
    }

    /// <summary>
    /// ⭐ 스킬2 실행 요청 (PlayerAttackInput에서 호출) - 신규 추가
    /// </summary>
    public void TriggerSkill2()
    {
        if (Time.time >= lastSkill2Time + skill2CooldownTime)
        {
            FireSkill2();
            lastSkill2Time = Time.time;
            if (OnSkill2CooldownChanged != null)
                OnSkill2CooldownChanged(skill2CooldownTime);
                
            Debug.Log($"🟢 [SkillController] 스킬2(Power Arrow) 실행!");
        }
        else
        {
            Debug.LogWarning($"🟡 [SkillController] 스킬2 쿨다운 중! 남은 시간: {GetSkill2CooldownRemaining():F1}초");
        }
    }

    /// <summary>
    /// 스킬1: Multi-Arrow 실행
    /// </summary>
    void FireSkill()
    {
        if (bowTransform == null || firePoint == null) return;
        
        // ⭐ GamePoolManager null 체크 추가
        if (GamePoolManager.Instance == null)
        {
            Debug.LogError("[SkillController] GamePoolManager.Instance가 null입니다!");
            return;
        }
        
        Vector2 baseDir = bowTransform.right;
        float startAngle = -spreadAngle * (arrowCount - 1) / 2f;
        
        for (int i = 0; i < arrowCount; i++)
        {
            float angle = startAngle + spreadAngle * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;
            
            // ⭐ 핵심 수정: Instantiate 대신 GamePoolManager 사용
            GameObject arrow = GamePoolManager.Instance.SpawnFromPool("Arrow", firePoint.position, Quaternion.identity);
            
            if (arrow != null)
            {
                // ⭐ 중요: 스킬1은 항상 기본 크기로 리셋 (스킬2에서 키운 크기 문제 해결)
                arrow.transform.localScale = Vector3.one;
                
                // ⭐ 핵심 수정: Rigidbody2D.velocity 대신 Projectile 컴포넌트 사용
                arrow.transform.right = dir;
                
                // Projectile 컴포넌트가 있으면 속도 설정
                if (arrow.TryGetComponent(out Projectile projectile))
                {
                    projectile.UpdateMoveSpeed(arrowSpeed);
                }
                else
                {
                    // 백업: Rigidbody2D가 있으면 velocity 설정
                    if (arrow.TryGetComponent(out Rigidbody2D rb))
                    {
                        rb.velocity = dir.normalized * arrowSpeed;
                    }
                }
                
                Debug.Log($"[SkillController] 스킬1 화살 생성: {arrow.name} (크기: {arrow.transform.localScale})");
            }
            else
            {
                Debug.LogError($"[SkillController] GamePoolManager에서 Arrow 생성 실패!");
            }
        }
    }

    /// <summary>
    /// ⭐ 스킬2: Power Arrow 실행 - 기존대로 유지
    /// </summary>
    void FireSkill2()
    {
        if (bowTransform == null || firePoint == null) return;
        
        if (GamePoolManager.Instance == null)
        {
            Debug.LogError("[SkillController] GamePoolManager.Instance가 null입니다!");
            return;
        }
        
        Vector2 dir = bowTransform.right;
        
        // 강력한 화살 1개 생성 (Arrow 대신 PowerArrow 풀을 사용할 수도 있음)
        GameObject powerArrow = GamePoolManager.Instance.SpawnFromPool("Arrow", firePoint.position, Quaternion.identity);
        
        if (powerArrow != null)
        {
            // 방향 설정
            powerArrow.transform.right = dir;
            
            // ⭐ 크기 증가 (더 강력해 보이게) - 스킬2 전용
            powerArrow.transform.localScale = Vector3.one * powerArrowScale;
            
            // Projectile 컴포넌트가 있으면 강화된 속도 설정
            if (powerArrow.TryGetComponent(out Projectile projectile))
            {
                projectile.UpdateMoveSpeed(powerArrowSpeed);
                // 향후 데미지도 증가시킬 수 있음
            }
            else
            {
                // 백업: Rigidbody2D가 있으면 velocity 설정
                if (powerArrow.TryGetComponent(out Rigidbody2D rb))
                {
                    rb.velocity = dir.normalized * powerArrowSpeed;
                }
            }
            
            Debug.Log($"[SkillController] 스킬2 강력한 화살 생성: {powerArrow.name} (속도: {powerArrowSpeed}, 크기: {powerArrowScale})");
        }
        else
        {
            Debug.LogError($"[SkillController] GamePoolManager에서 PowerArrow 생성 실패!");
        }
    }

    /// <summary>
    /// 스킬1 쿨다운 남은 시간
    /// </summary>
    public float GetCooldownRemaining()
    {
        float elapsed = Time.time - lastSkillTime;
        return Mathf.Clamp(cooldownTime - elapsed, 0, cooldownTime);
    }

    /// <summary>
    /// ⭐ 스킬2 쿨다운 남은 시간 - 신규 추가
    /// </summary>
    public float GetSkill2CooldownRemaining()
    {
        float elapsed = Time.time - lastSkill2Time;
        return Mathf.Clamp(skill2CooldownTime - elapsed, 0, skill2CooldownTime);
    }
} 