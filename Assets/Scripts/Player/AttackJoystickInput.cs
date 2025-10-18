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
                attackJoystick = joystickInScene;
                joystickFound = true;
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
                break;
            }

            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

    }

    public Vector2 GetAttackDirection()
    {
        return (joystickFound && attackJoystick != null) ? attackJoystick.Direction : Vector2.zero;
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
        
    }
}