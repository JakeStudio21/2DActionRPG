using UnityEngine;

public class EnemyFSM : EnemyBase
{
    public float moveSpeed = 2f;
    public Transform target;

    protected override void StateUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                // 대기 로직
                break;
            case EnemyState.Patrol:
                // 순찰 로직
                break;
            case EnemyState.Chase:
                // 추적 로직
                if (target != null)
                {
                    transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
                }
                break;
            case EnemyState.Attack:
                // 공격 로직
                break;
            case EnemyState.Die:
                // 사망 처리
                break;
        }
    }
} 