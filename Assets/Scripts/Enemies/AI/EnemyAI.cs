using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private float roamChangeDirFloat = 2f;
    [SerializeField] private float attackRange = 0f;
    [SerializeField] private MonoBehaviour enemyType;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private bool stopMovingWhileAttacking = false;

    private bool canAttack = true;

    private enum State 
    {
        Roaming, 
        Attacking
    }   
    
    private Vector2 roamPosition;
    private float timeRoaming = 0f;

    private State state;
    private EnemyPathfinding enemyPathfinding;
    private PlayerController cachedPlayerController; // 캐시된 플레이어 참조

    private void Awake() 
    {
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        state = State.Roaming;
    }

    private void Start() 
    {
        roamPosition = GetRoamingPosition();
        // 플레이어 참조 캐싱 (씬 시작 시 한 번만)
        StartCoroutine(FindPlayerCoroutine());
    }

    private IEnumerator FindPlayerCoroutine()
    {
        // 플레이어가 스폰될 때까지 대기
        while (cachedPlayerController == null)
        {
            cachedPlayerController = FindObjectOfType<PlayerController>();
            if (cachedPlayerController == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        Debug.Log($"[EnemyAI] {gameObject.name}이 플레이어를 찾았습니다.");
    }

    private void Update() 
    {
        // 플레이어가 아직 스폰되지 않았으면 대기
        if (cachedPlayerController == null)
            return;

        MovementStateControl();
    }

    private void MovementStateControl() 
    {
        switch (state)
        {
            default:
            case State.Roaming:
                Roaming();
            break;

            case State.Attacking:
                Attacking();
            break;
        }
    }

    private void Roaming() 
    {
        timeRoaming += Time.deltaTime;

        if (enemyPathfinding != null)
        {
            enemyPathfinding.MoveTo(roamPosition);
        }

        // 캐시된 플레이어 참조 사용
        if (cachedPlayerController != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayerController.transform.position);
            if (distanceToPlayer < attackRange) 
            {
                state = State.Attacking;
            }
        }

        if (timeRoaming > roamChangeDirFloat) 
        {
            roamPosition = GetRoamingPosition();
        }
    }

    private void Attacking() 
    {
        // 캐시된 플레이어 참조 사용
        if (cachedPlayerController != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayerController.transform.position);
            if (distanceToPlayer > attackRange)
            {
                state = State.Roaming;
            }

            if (attackRange != 0 && canAttack) 
            {
                canAttack = false;
                
                // enemyType이 null이 아닌지 확인
                if (enemyType != null && enemyType is IEnemy enemy)
                {
                    enemy.Attack(this);
                }
                else
                {
                    Debug.LogWarning($"[EnemyAI] {gameObject.name}의 enemyType이 설정되지 않았거나 IEnemy를 구현하지 않습니다.");
                }

                if (stopMovingWhileAttacking && enemyPathfinding != null) 
                {
                    enemyPathfinding.StopMoving();
                } 
                else if (enemyPathfinding != null)
                {
                    enemyPathfinding.MoveTo(roamPosition);
                }

                StartCoroutine(AttackCooldownRoutine());
            }
        }
    }

    private IEnumerator AttackCooldownRoutine() 
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private Vector2 GetRoamingPosition() 
    { 
        timeRoaming = 0f;
        return new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;        
    }

    public int GetProjectileDamage()
    {
        return projectileDamage;
    }

    // 외부에서 플레이어 참조를 강제로 새로고침할 수 있는 메서드
    public void RefreshPlayerReference()
    {
        cachedPlayerController = FindObjectOfType<PlayerController>();
    }
} 