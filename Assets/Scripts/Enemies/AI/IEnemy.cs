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
    void Attack();
    // 필요시 추가: MoveTo, TakeDamage 등
} 