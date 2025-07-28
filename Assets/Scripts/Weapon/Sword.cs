using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour, IWeapon
{
    [SerializeField] private GameObject slashPrefab;
    [SerializeField] private Transform slashSpawnPoint;
    [SerializeField] private EquipmentData equipmentData;  // WeaponInfo → EquipmentData

    // ⭐ Animator는 PlayerAnimationController에서 관리하므로 제거
    // private Animator myAnimator;
    private Transform weaponCollider;
    private ActiveWeapon activeWeapon;

    private GameObject slashAnim;

    // private float baseSwordX = 0.2f; // 오른손 기준 위치 [미사용]
    // private float baseColliderX = 0.2f; // 오른손 기준 위치 [미사용]

    private void Awake() {
        // ⭐ Animator 참조 제거 - PlayerAnimationController에서 관리
        // myAnimator = GetComponent<Animator>();
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

        // ⭐ 애니메이션 트리거 제거 - PlayerAnimationController에서 관리
        // myAnimator.SetTrigger("Attack");
        
        // ⭐ [Phase B] Warrior 감지 및 전용 기능 적용
        var warrior = GetComponentInParent<Warrior>();
        if (warrior != null && warrior.IsActiveClass)  // ⭐ 수정: isActive → IsActiveClass
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
        // 검 SpriteRenderer flipX (이중 반전 방지 위해 주석처리)
        // var sr = GetComponent<SpriteRenderer>();
        // if (sr != null) sr.flipX = facingLeft;

        // [신규] ActiveWeapon의 localScale.x를 ±1로 반전
        if (transform.parent != null)
        {
            Vector3 scale = transform.parent.localScale;
            scale.x = facingLeft ? -1 : 1;
            transform.parent.localScale = scale;
            // Debug.Log($"[ActiveWeapon] localScale.x: {transform.parent.localScale.x}, facingLeft: {facingLeft}");
        }

        // [백업: 기존 위치 하드코딩 방식]
        // transform.localPosition = new Vector3(facingLeft ? -baseSwordX : baseSwordX, transform.localPosition.y, transform.localPosition.z);
        // Debug.Log($"[Sword] {gameObject.name} localPosition.x: {transform.localPosition.x}, facingLeft: {facingLeft}");
        // if (transform.parent != null)
        // {
        //     var weaponCollider = transform.parent.Find("WeaponCollider");
        //     if (weaponCollider != null)
        //     {
        //         weaponCollider.localPosition = new Vector3(facingLeft ? -baseColliderX : baseColliderX, weaponCollider.localPosition.y, weaponCollider.localPosition.z);
        //         Debug.Log($"[WeaponCollider] {weaponCollider.name} localPosition.x: {weaponCollider.localPosition.x}, facingLeft: {facingLeft}");
        //     }
        //     else
        //     {
        //         Debug.LogWarning("[WeaponCollider] WeaponCollider 오브젝트를 찾지 못했습니다.");
        //     }
        // }
    }
} 