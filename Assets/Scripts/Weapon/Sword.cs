using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem; // 🆕 Cue 시스템 네임스페이스 추가

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
        
        Vector3 effectPosition = slashSpawnPoint != null ? slashSpawnPoint.position : transform.position;
        Quaternion effectRotation = GetWeaponColliderRotation();
        Vector2 effectDirection = GetWeaponFacingDirection();

        Debug.Log("🔵 [Sword] Attack() 시작 - 순수 공격 로직");
        // 🔍 디버깅: CueContext 전달값 확인 (중복 방지)
        Debug.Log($"🎯 [Sword] HitSpark 생성 - 위치: {effectPosition}, 회전: {effectRotation.eulerAngles.z:F1}도, 방향: {effectDirection}");
        


        // 🆕 Cue 이벤트 발행 - 공격 시작 시점
        var context = new CueContext
        {
            position = effectPosition,          // ✅ SlashSpawnPoint 위치
            rotation = effectRotation,          // ✅ WeaponCollider 동적 회전
            actorType = ActorType.Player,
            magnitude = 1.0f,
            surfaceType = SurfaceType.Default,
            facingDir = effectDirection         // ✅ WeaponCollider 방향
        };
        
        bool cueSuccess = CueEmitter.Emit("attack.player.melee", "Player", context);
        Debug.Log($"🎬 [Sword] HitSpark Cue 발행 결과: {cueSuccess}");

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
            
            // 🆕 버서커 모드 전용 Cue 이벤트 발행
            var berserkerContext = new CueContext
            {
                position = transform.position,
                rotation = transform.rotation,
                actorType = ActorType.Player,
                magnitude = 2.0f, // 버서커 모드는 더 강한 효과
                isCritical = true,
                surfaceType = SurfaceType.Default
            };
            
            bool berserkerCueSuccess = CueEmitter.Emit("attack.player.critical", "Player", berserkerContext);
            Debug.Log($"🔥 [Sword] 버서커 Cue 발행 결과: {berserkerCueSuccess}");
            
            // ✅ [완전 삭제] 중복 버서커 슬래시 제거 (방안 1)
            // CueSystem의 "attack.player.critical" → PlayerCritVFX가 이미 완벽한 크리티컬 이펙트 제공
        }
        
        // 블록 확률과 반격 확률 정보 출력 (디버깅용)
        Debug.Log($"🛡️ [Sword] Warrior 상태 - 블록: {warrior.GetBlockChance() * 100:F1}%, 반격: {warrior.GetCounterAttackChance() * 100:F1}%");
    }
    
    /// <summary>
    /// 검 공격 실행 (Animation Event에서도 호출 가능)
    /// </summary>
    public void PerformSwordAttack()
    {
        Debug.Log("🎯 [Sword] PerformSwordAttack() - CueSystem 전용 모드");
        
        // WeaponCollider 활성화 (히트박스 관리)
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(true);
            Debug.Log("✅ [Sword] WeaponCollider 활성화");
            
            // 일정 시간 후 자동 비활성화
            StartCoroutine(DeactivateWeaponAfterDelay());
        }
        
        // ❌ [완전 제거] 중복 Slash Prefab 생성 코드 삭제
        // CueSystem의 HitSpark가 attack.player.melee 이벤트로 이미 생성되므로 불필요
        // WeaponCollider 회전 방향도 CueSystem에서 자동 적용됨
        
        Debug.Log("🎬 [Sword] CueSystem 전용 공격 완료");
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
    
    /// <summary>
    /// 🧭 WeaponCollider에서 방향값만 가져오는 헬퍼 메서드
    /// </summary>
    private Quaternion GetWeaponColliderRotation()
    {
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            Transform weaponColliderTransform = playerController.GetWeaponCollider();
            if (weaponColliderTransform != null)
            {
                Debug.Log($"🧭 [GetWeaponColliderRotation] 콜라이더 회전: {weaponColliderTransform.eulerAngles.z:F1}도");
                return weaponColliderTransform.rotation;
            }
            else
            {
                Debug.LogWarning("⚠️ [GetWeaponColliderRotation] WeaponCollider가 null입니다!");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [GetWeaponColliderRotation] PlayerController를 찾을 수 없습니다!");
        }
        
        // Fallback: 기본 회전값
        Debug.Log("🔄 [GetWeaponColliderRotation] Fallback: Quaternion.identity 사용");
        return Quaternion.identity;
    }
    
    /// <summary>
    /// 🧭 WeaponCollider 방향 벡터 계산
    /// </summary>
    private Vector2 GetWeaponFacingDirection()
    {
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            Transform weaponColliderTransform = playerController.GetWeaponCollider();
            if (weaponColliderTransform != null)
            {
                Vector2 facingDir = weaponColliderTransform.right;
                Debug.Log($"🧭 [Sword] WeaponCollider 방향: {facingDir}");
                return facingDir;
            }
        }
        
        // Fallback
        return Vector2.right;
    }
    
    /// <summary>
    /// ⏰ WeaponCollider 비활성화 딜레이 코루틴
    /// </summary>
    private IEnumerator DeactivateWeaponAfterDelay()
    {
        // 0.3초 후 WeaponCollider 비활성화 (공격 지속 시간)
        yield return new WaitForSeconds(0.3f);
        
        if (weaponCollider != null)
        {
            weaponCollider.gameObject.SetActive(false);
            Debug.Log("⏰ [Sword] WeaponCollider 자동 비활성화 완료");
        }
        else
        {
            Debug.LogWarning("⚠️ [Sword] weaponCollider가 null이어서 비활성화할 수 없습니다!");
        }
    }
} 