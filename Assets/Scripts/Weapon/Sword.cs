using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private EquipmentData equipmentData;  // WeaponInfo → EquipmentData

    // ⭐ Sword 자체 Animator 참조 복원
    private Animator myAnimator;
    private Transform weaponCollider;
    private ActiveWeapon activeWeapon;

    private GameObject slashAnim;

    // private float baseSwordX = 0.2f; // 오른손 기준 위치 [미사용]
    // private float baseColliderX = 0.2f; // 오른손 기준 위치 [미사용]

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false; // Inspector에서 조절 가능

    private void Awake() {
        // ⭐ Sword 자체 Animator 참조 복원
        myAnimator = GetComponent<Animator>();
        if (myAnimator == null)
        {
            Debug.LogWarning("🟡 [Sword] Animator 컴포넌트를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log("✅ [Sword] Animator 컴포넌트 연결됨");
        }
    }

    private void Start() {
        weaponCollider = FindObjectOfType<PlayerController>().GetWeaponCollider();
        slashSpawnPoint = GameObject.Find("SlashSpawnPoint").transform;
        activeWeapon = FindObjectOfType<ActiveWeapon>();
    }

    public EquipmentData GetEquipmentData()  // WeaponInfo → EquipmentData
    {
        return equipmentData;
    }

    public void Attack() {
        Debug.Log("🔵 [Sword] Attack() 시작 - 순수 공격 로직");

        // ⭐ Sword 애니메이션 트리거 복원
        if (myAnimator != null)
        {
            myAnimator.SetTrigger("Attack");
            Debug.Log("🎬 [Sword] SwingDown 애니메이션 트리거 실행");
        }
        else
        {
            Debug.LogWarning("🟡 [Sword] myAnimator가 null입니다!");
        }
        
        // ⭐ [Phase B] Warrior 감지 및 전용 기능 적용
        var warrior = GetComponentInParent<Warrior>();
        if (warrior != null && warrior.IsActiveClass)
        {
            Debug.Log("⚔️ [Sword] Warrior 감지! 전용 기능 활성화");
            PerformWarriorSwordAttack(warrior);
        }
        else
        {
            // 기본 공격 로직
            PerformSwordAttack();
        }
    }
    
    /// <summary>
    /// ⭐ [Phase B] Warrior 전용 검 공격 (버서커 모드, 반격 등 고려)
    /// </summary>
    private void PerformWarriorSwordAttack(Warrior warrior)
    {
        Debug.Log("⚔️ [Sword] Warrior 전용 공격 실행!");
        
        // 기본 공격 로직 실행
        PerformSwordAttack();
        
        // Warrior 전용 추가 효과
        if (warrior.IsInBerserkerMode())
        {
            Debug.Log("🔥 [Sword] 버서커 모드! 추가 공격 효과");
            
            // 버서커 모드 시 추가 슬래시 이펙트
            if (slashSpawnPoint != null)
            {
                var berserkerSlash = GamePoolManager.Instance.SpawnFromPool("Slash Prefab", 
                    slashSpawnPoint.position + Vector3.up * 0.5f, Quaternion.identity);
                if (berserkerSlash != null)
                {
                    berserkerSlash.transform.parent = this.transform.parent;
                    // 버서커 이펙트는 빨간색으로 변경
                    var spriteRenderer = berserkerSlash.GetComponent<SpriteRenderer>();
                    if (spriteRenderer != null)
                    {
                        spriteRenderer.color = Color.red;
                    }
                }
            }
        }
        
        // 블록 확률과 반격 확률 정보 출력 (디버깅용)
        Debug.Log($"🛡️ [Sword] Warrior 상태 - 블록: {warrior.GetBlockChance() * 100:F1}%, 반격: {warrior.GetCounterAttackChance() * 100:F1}%");
    }
    
    /// <summary>
    /// 검 공격 실행 (Animation Event에서도 호출 가능)
    /// </summary>
    public void PerformSwordAttack()
    {
        // 무기 콜라이더 활성화
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(true);
            Debug.Log("�� [Sword] 무기 콜라이더 활성화");
        }
        
        // 슬래시 이펙트 생성
        if (slashSpawnPoint != null)
        {
            slashAnim = GamePoolManager.Instance.SpawnFromPool("Slash Prefab", slashSpawnPoint.position, Quaternion.identity);
            if (slashAnim != null)
            {
                slashAnim.transform.parent = this.transform.parent;
                Debug.Log("🟢 [Sword] 슬래시 이펙트 생성");
            }
        }
        
        // ⭐ [Phase B] WeaponDamage 참조 제거 (존재하지 않는 클래스)
        // 데미지는 DamageSource.cs에서 이미 Warrior 배율을 적용하므로 여기서는 필요 없음
        Debug.Log("🟢 [Sword] 기본 공격 로직 완료");
    }

    public void DoneAttackingAnimEnvet() {
        weaponCollider.gameObject.SetActive(false);
    }

    public void SwingUpFlipAnimEvent() {
        // 🛡️ null 체크 추가
        if (slashAnim == null) 
        {
            Debug.LogWarning("🟡 [Sword] slashAnim이 null입니다. SwingUpFlipAnimEvent 건너뜀");
            return;
        }
        
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(-180, 0, 0);

        if (FindObjectOfType<PlayerController>().FacingLeft) { 
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void SwingDownFlipAnimEvent() {
        // 🛡️ null 체크 추가
        if (slashAnim == null) 
        {
            Debug.LogWarning("🟡 [Sword] slashAnim이 null입니다. SwingDownFlipAnimEvent 건너뜀");
            return;
        }
        
        slashAnim.gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);

        if (FindObjectOfType<PlayerController>().FacingLeft)
        {
            slashAnim.GetComponent<SpriteRenderer>().flipX = true;
        }
    }

    public void UpdateDirection(Vector2 direction, bool facingLeft)
    {
        // ⚔️ 캐릭터와 정확히 동일한 방식 (PlayerController.AdjustPlayerFacingDirection()와 동일)
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // PlayerController와 정확히 동일한 로직:
            // mySpriteRender.flipX = facingLeft;
            spriteRenderer.flipX = facingLeft;
            
            if (showDebugLogs)
            {
                Debug.Log($"⚔️ [Sword] 캐릭터와 동일한 방향 전환:");
                Debug.Log($"   - facingLeft: {facingLeft}");
                Debug.Log($"   - spriteRenderer.flipX: {facingLeft}");
            }
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("🟡 [Sword] SpriteRenderer를 찾을 수 없습니다!");
        }
        
        // 중요: localScale 건드리지 않음 (위아래 전환 방지)
        // 중요: rotation 건드리지 않음 (Bow와의 차이점)
        // 오직 flipX만 사용 (캐릭터와 100% 동일)
    }
} 