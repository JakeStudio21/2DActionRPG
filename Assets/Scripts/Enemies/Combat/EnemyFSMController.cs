using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFSMController : MonoBehaviour
{
    private IEnemyState currentState;

    /// <summary>
    /// 현재 AI 상태 읽기 전용 노출 (EnemyDebugGizmosDrawer 등 외부 참조용)
    /// </summary>
    public IEnemyState CurrentState => currentState;

    public void ChangeState(IEnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    private void Update()
    {
        currentState?.Execute();
    }
}
