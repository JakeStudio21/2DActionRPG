using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemy
{
    Transform transform { get; }
    EnemyAnimationController AnimationController { get; }
    EnemyFSMController FSMController { get; }
    PlayerController TargetPlayer { get; }
    float AttackRange { get; }
    
    // �� 기존 FSM에서 사용하는 메서드들
    float DetectionRange { get; }
    float MoveSpeed { get; } // 🔑 추가
    bool EnableDebugLogs { get; }
    string name { get; }
    
    bool IsPlayerInRange(float range);
    bool IsWithinPatrolRange();
    Vector3 GetRandomPatrolPosition();
    void ChangeState(IEnemyState newState);
    
    void Attack();
} 