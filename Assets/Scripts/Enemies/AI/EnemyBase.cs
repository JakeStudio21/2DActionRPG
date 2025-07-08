using UnityEngine;

public abstract class EnemyBase : MonoBehaviour
{
    public EnemyState currentState = EnemyState.Idle;

    protected virtual void Update()
    {
        StateUpdate();
    }

    protected abstract void StateUpdate();

    public virtual void ChangeState(EnemyState newState)
    {
        currentState = newState;
    }
} 