using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveWeapon : Singleton<ActiveWeapon>
{
    public MonoBehaviour CurrentActiveWeapon {get; private set; }

    private PlayerControls playerControls;
    private float timeBetweenAttacks;

    private bool isAttacking = false;

    public AttackJoystickInput attackJoystickInput; // 인스펙터에서 할당

    protected override void Awake() {
        base.Awake();
        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        // playerControls.Enable(); // 키보드/마우스 입력을 비활성화하므로 주석 처리
    }

    private void Start()
    {
        // playerControls.Combat.Attack.started += _ => StartAttacking(); // 키보드/마우스 입력을 비활성화하므로 주석 처리
        // playerControls.Combat.Attack.canceled += _ => StopAttacking(); // 키보드/마우스 입력을 비활성화하므로 주석 처리

        AttackCooldown();
    }

    private void Update() {
        Attack();
        // [변경] 무기 방향 처리: IWeapon의 UpdateDirection 호출
        // ✅ 조이스틱 방향은 무기 방향 조절용으로 사용 (공격 감지와 분리)
        Vector2 dir = attackJoystickInput != null ? attackJoystickInput.GetAttackDirection() : Vector2.zero;
        var playerController = FindObjectOfType<PlayerController>();
        bool facingLeft = playerController != null && playerController.FacingLeft;
        if (CurrentActiveWeapon is IWeapon weapon)
        {
            weapon.UpdateDirection(dir, facingLeft);
        }
        // [백업: 이전 transform.rotation 처리]
        // if (dir.magnitude > 0.1f)
        // {
        //     float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        //     transform.rotation = Quaternion.Euler(0, 0, angle);
        // }
    }

    public void NewWeapon(MonoBehaviour newWeapon) {
        Debug.Log("🔵 [ActiveWeapon] NewWeapon 호출 - 새 무기: " + (newWeapon != null ? newWeapon.name : "NULL"));

        CurrentActiveWeapon = newWeapon;

        AttackCooldown();
        timeBetweenAttacks = (CurrentActiveWeapon as IWeapon).GetWeaponInfo().weaponCooldown;
        Debug.Log("🟢 [ActiveWeapon] 무기 쿨다운 설정됨: " + timeBetweenAttacks + "초");
    }

    public void WeaponNull() {
        CurrentActiveWeapon = null;
    }

    // WeaponInfo를 받아 해당 무기를 장착하는 새로운 공용 메서드입니다.
    public void EquipWeapon(WeaponInfo weaponInfo)
    {
        // 현재 무기가 있다면 파괴합니다.
        if (CurrentActiveWeapon != null)
        {
            Destroy(CurrentActiveWeapon.gameObject);
        }

        // WeaponInfo나 그 안의 프리팹이 유효한지 확인합니다.
        if (weaponInfo == null || weaponInfo.weaponPrefab == null)
        {
            WeaponNull(); // 유효하지 않으면 무기 없는 상태로 설정합니다.
            return;
        }

        // 새 무기 프리팹을 생성하고, ActiveWeapon의 자식으로 만듭니다.
        GameObject newWeapon = Instantiate(weaponInfo.weaponPrefab, transform);
        
        // 새로 생성된 무기를 현재 활성화된 무기로 설정합니다.
        NewWeapon(newWeapon.GetComponent<MonoBehaviour>());
    }

    private void AttackCooldown() {
        isAttacking = true;
        Debug.Log("🔵 [ActiveWeapon] AttackCooldown 시작 - timeBetweenAttacks: " + timeBetweenAttacks);
        StopAllCoroutines();
        StartCoroutine(TimeBetweenAttacksRoutine());
    }

// Unity에서 공격 쿨타임(재사용 대기시간)을 구현하기 위한 코루틴
    private IEnumerator TimeBetweenAttacksRoutine() {
        Debug.Log("🔵 [ActiveWeapon] 쿨다운 코루틴 시작 - 대기시간: " + timeBetweenAttacks + "초");
        yield return new WaitForSeconds(timeBetweenAttacks);
        isAttacking = false;
        Debug.Log("🟢 [ActiveWeapon] 쿨다운 완료! isAttacking = false");
    }

    private void Attack() {
        // ⭐ 추가: 여러 입력 방식으로 기본공격 감지
        bool shouldAttack = false;
        
        // 🔴 1. AttackJoystickInput을 통한 조이스틱 공격 - 완전 비활성화
        // if (attackJoystickInput != null)
        // {
        //     Vector2 attackDirection = attackJoystickInput.GetAttackDirection();
        //     if (attackDirection.magnitude > 0.1f)
        //     {
        //         shouldAttack = true;
        //     }
        // }
        
        // 2. GameControl을 통한 통합 입력 (키보드/마우스 포함) - 이것도 비활성화됨
        var gameControl = GameControl.Instance;
        if (gameControl != null && gameControl.AttackPressed)
        {
            shouldAttack = true;
        }
        
        // 🔴 3. 백업 입력은 GameControl.cs에서 처리 (중복 방지) - 모두 비활성화됨
        // 이제 오직 PlayerAttackInput.cs의 A키만 PerformAttack()을 직접 호출
        
        // ⭐ 공격 실행 - shouldAttack는 항상 false가 되어 실행되지 않음
        if (shouldAttack)
        {
            PerformAttack();
        }
    }

    // 이 함수는 UI 버튼에서 직접 호출할 수 있도록 public으로 만듭니다.
    public void PerformAttack()
    {
        Debug.Log("🔵 [ActiveWeapon] PerformAttack() 시작");
        Debug.Log("🔵 [ActiveWeapon] isAttacking: " + isAttacking);
        Debug.Log("🔵 [ActiveWeapon] CurrentActiveWeapon: " + (CurrentActiveWeapon != null ? CurrentActiveWeapon.name : "NULL"));
        
        if (!isAttacking && CurrentActiveWeapon)
        {
            Debug.Log("🟢 [ActiveWeapon] 공격 조건 만족, 공격 실행!");
            AttackCooldown();
            (CurrentActiveWeapon as IWeapon).Attack();
            Debug.Log("🟢 [ActiveWeapon] IWeapon.Attack() 호출 완료");
        }
        else
        {
            if (isAttacking)
            {
                Debug.LogWarning("🟡 [ActiveWeapon] 이미 공격 중입니다 (쿨다운)");
            }
            if (CurrentActiveWeapon == null)
            {
                Debug.LogError("🔴 [ActiveWeapon] CurrentActiveWeapon이 null입니다!");
            }
        }
    }
}

