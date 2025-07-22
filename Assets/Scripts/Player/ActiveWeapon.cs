using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveWeapon : Singleton<ActiveWeapon>
{
    public MonoBehaviour CurrentActiveWeapon {get; private set; }

    private PlayerControls playerControls;
    private float timeBetweenAttacks;

    // ⭐ 기존 isAttacking 제거 - PlayerAnimationController에서 관리
    // private bool isAttacking = false;

    public AttackJoystickInput attackJoystickInput; // 인스펙터에서 할당
    
    // ⭐ 새 Animation Controller 참조 추가
    private PlayerAnimationController playerAnimationController;

    protected override void Awake() {
        base.Awake();
        playerControls = new PlayerControls();
        
        // ⭐ PlayerAnimationController 참조 가져오기 (더 넓은 범위에서 검색)
        playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerAnimationController == null)
        {
            // 같은 GameObject에 없으면 부모/자식에서 찾기
            playerAnimationController = GetComponentInParent<PlayerAnimationController>();
            if (playerAnimationController == null)
            {
                playerAnimationController = GetComponentInChildren<PlayerAnimationController>();
            }
            
            if (playerAnimationController == null)
            {
                Debug.LogWarning("🟡 [ActiveWeapon] PlayerAnimationController가 없습니다. 기존 방식으로 동작합니다.");
            }
            else
            {
                Debug.Log("🟢 [ActiveWeapon] PlayerAnimationController를 찾았습니다!");
            }
        }
    }

    private void OnEnable()
    {
        // playerControls.Enable(); // 키보드/마우스 입력을 비활성화하므로 주석 처리
    }

    private void Start()
    {
        // playerControls.Combat.Attack.started += _ => StartAttacking(); // 키보드/마우스 입력을 비활성화하므로 주석 처리
        // playerControls.Combat.Attack.canceled += _ => StopAttacking(); // 키보드/마우스 입력을 비활성화하므로 주석 처리

        // ⭐ 기존 AttackCooldown() 제거 - PlayerAnimationController에서 관리
        // AttackCooldown();
    }

    private void Update() {
        Attack();
        
        // 🛡️ 안전성 검사 강화: CurrentActiveWeapon이 유효한지 확인
        if (CurrentActiveWeapon == null)
        {
            // CurrentActiveWeapon이 null이면 무기 방향 업데이트 건너뜀
            return;
        }
        
        // 🛡️ 추가 안전성 검사: 게임오브젝트가 파괴되었는지 확인
        if (CurrentActiveWeapon.gameObject == null)
        {
            Debug.LogWarning("🟡 [ActiveWeapon] CurrentActiveWeapon의 GameObject가 파괴되었습니다. 참조 정리 중...");
            CurrentActiveWeapon = null;
            return;
        }
        
        // [변경] 무기 방향 처리: IWeapon의 UpdateDirection 호출
        // ✅ 조이스틱 방향은 무기 방향 조절용으로 사용 (공격 감지와 분리)
        Vector2 dir = attackJoystickInput != null ? attackJoystickInput.GetAttackDirection() : Vector2.zero;
        var playerController = FindObjectOfType<PlayerController>();
        bool facingLeft = playerController != null && playerController.FacingLeft;
        
        // 🛡️ 안전한 IWeapon 캐스팅 및 호출
        if (CurrentActiveWeapon is IWeapon weapon)
        {
            try
            {
                weapon.UpdateDirection(dir, facingLeft);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🔴 [ActiveWeapon] UpdateDirection 호출 중 에러: {e.Message}");
                // 에러 발생 시 무기 참조 정리
                CurrentActiveWeapon = null;
            }
        }
    }

    public void NewWeapon(MonoBehaviour newWeapon) {
        Debug.Log("🔵 [ActiveWeapon] NewWeapon 호출 - 새 무기: " + (newWeapon != null ? newWeapon.name : "NULL"));

        // 🔑 1단계: newWeapon null 체크
        if (newWeapon == null) {
            Debug.LogError("🔴 [ActiveWeapon] newWeapon이 null입니다!");
            return;
        }

        CurrentActiveWeapon = newWeapon;

        // 🔑 2단계: IWeapon 인터페이스 체크
        IWeapon weaponInterface = CurrentActiveWeapon as IWeapon;
        if (weaponInterface == null) {
            Debug.LogError($"🔴 [ActiveWeapon] {newWeapon.name}이 IWeapon을 구현하지 않습니다!");
            return;
        }

        // 🔑 3단계: WeaponInfo 안전성 체크
        WeaponInfo weaponInfo = weaponInterface.GetWeaponInfo();
        if (weaponInfo == null) {
            Debug.LogError($"🔴 [ActiveWeapon] {newWeapon.name}의 WeaponInfo가 null입니다!");
            return;
        }

        // 🔑 4단계: 쿨다운 안전하게 설정
        timeBetweenAttacks = weaponInfo.weaponCooldown;
        
        // PlayerAnimationController에 쿨다운 정보 전달
        if (playerAnimationController != null)
        {
            playerAnimationController.UpdateWeaponCooldown(timeBetweenAttacks);
        }
        
        Debug.Log("�� [ActiveWeapon] 무기 교체 성공: " + newWeapon.name + " (쿨다운: " + timeBetweenAttacks + "초)");
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

    // ⭐ 기존 AttackCooldown() 메서드 제거 - PlayerAnimationController에서 관리
    // private void AttackCooldown() { ... }
    // private IEnumerator TimeBetweenAttacksRoutine() { ... }

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
        
        // ⭐ PlayerAnimationController 사용 시 (AttackType 제거)
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerAttack(); // AttackType 매개변수 제거
            
            if (success)
            {
                Debug.Log("🟢 [ActiveWeapon] PlayerAnimationController 공격 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [ActiveWeapon] PlayerAnimationController 공격 실패!");
            }
            
            return;
        }
        
        // ⭐ 기존 방식 (fallback) - PlayerAnimationController가 없을 때
        Debug.LogWarning("🟡 [ActiveWeapon] PlayerAnimationController 없음 - 기존 방식 사용");
        
        if (CurrentActiveWeapon != null)
        {
            Debug.Log("🟢 [ActiveWeapon] 공격 조건 만족, 공격 실행!");
            (CurrentActiveWeapon as IWeapon).Attack();
            Debug.Log("🟢 [ActiveWeapon] IWeapon.Attack() 호출 완료");
        }
        else
        {
            Debug.LogError("🔴 [ActiveWeapon] CurrentActiveWeapon이 null입니다!");
        }
    }
}

