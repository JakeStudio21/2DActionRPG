using UnityEngine;
using System.Collections;

public class AttackJoystickInput : MonoBehaviour
{
    public Joystick attackJoystick;
    private bool joystickFound = false;

    void Start()
    {
        StartCoroutine(FindJoystickCoroutine());
    }

    // ⭐ 새로 추가: PlayerController와 동일한 실시간 체크 로직
    private void Update()
    {
        // ⭐ 조이스틱 연결 상태 실시간 체크 (1초마다)
        if ((!joystickFound || attackJoystick == null) && Time.frameCount % 60 == 0)
        {
            var joystickInScene = FindObjectOfType<FixedJoystick>();
            if (joystickInScene != null)
            {
                Debug.Log("[AttackJoystickInput] Update에서 조이스틱 재연결 시도");
                attackJoystick = joystickInScene;
                joystickFound = true;
            }
            else
            {
                Debug.LogWarning($"[AttackJoystickInput] 조이스틱 없음 - joystickFound: {joystickFound}, attackJoystick: {attackJoystick}");
            }
        }
    }

    /// <summary>
    /// 안전한 조이스틱 찾기 코루틴
    /// </summary>
    private IEnumerator FindJoystickCoroutine()
    {
        float timeout = 5f; // 5초 타임아웃
        float elapsed = 0f;

        while (!joystickFound && elapsed < timeout)
        {
            attackJoystick = FindObjectOfType<FixedJoystick>();
            if (attackJoystick != null)
            {
                joystickFound = true;
                Debug.Log("[AttackJoystickInput] 공격 조이스틱을 찾았습니다!");
                break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (!joystickFound)
        {
            Debug.LogWarning("[AttackJoystickInput] 공격 조이스틱을 찾을 수 없습니다.");
        }
    }

    public Vector2 GetAttackDirection()
    {
        var direction = (joystickFound && attackJoystick != null) ? attackJoystick.Direction : Vector2.zero;
        
        // ⭐ 디버그: 방향 값 출력 (1초마다)
        // if (Time.frameCount % 60 == 0 && direction.magnitude > 0.1f)
        // {
        //     Debug.Log($"[AttackJoystickInput] GetAttackDirection: {direction}");
        // }
        
        return direction;
    }

    /// <summary>
    /// 외부에서 조이스틱 참조를 다시 설정할 수 있는 메서드 (강제 재연결)
    /// </summary>
    public void RefreshJoystickReference()
    {
        // ⭐ 핵심 수정: 무조건 강제로 초기화 후 재탐색
        joystickFound = false;
        attackJoystick = null;
        StartCoroutine(FindJoystickCoroutine());
        
        Debug.Log("[AttackJoystickInput] 조이스틱 강제 재연결 시도 - joystickFound를 false로 초기화");
    }
}