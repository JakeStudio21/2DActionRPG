using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public EnemyState currentState = EnemyState.Idle;
    protected EnemyState previousState;

    protected virtual void Update()
    {
        StateUpdate();
    }

    protected abstract void StateUpdate();

    public virtual void ChangeState(EnemyState newState)
    {
        if (currentState != newState)
        {
            previousState = currentState;
            currentState = newState;
            OnStateChanged(previousState, newState);
        }
    }
    
    /// <summary>
    /// 상태 변경 시 호출되는 가상 메서드
    /// </summary>
    protected virtual void OnStateChanged(EnemyState from, EnemyState to)
    {
        // 자식 클래스에서 오버라이드하여 상태 변경 시 추가 처리
    }
} 