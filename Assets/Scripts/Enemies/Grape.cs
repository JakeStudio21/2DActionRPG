using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grape : MonoBehaviour, IEnemy
{
    [SerializeField] private GameObject grapeProjectilePrefab;

    private Animator myAnimator;
    private SpriteRenderer spriteRenderer;
    private EnemyAI enemyAI;

    readonly int ATTACK_HASH = Animator.StringToHash("Attack");

    private void Awake() {
        myAnimator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Attack(EnemyAI enemyAI) {
        this.enemyAI = enemyAI;
        myAnimator.SetTrigger(ATTACK_HASH);

        if (transform.position.x - FindObjectOfType<PlayerController>().transform.position.x < 0) {
            spriteRenderer.flipX = false;
        } else {
            spriteRenderer.flipX = true;
        }
    }

    public void SpawnProjectileAnimEvent() {
        if (grapeProjectilePrefab == null) {
            return;
        }
        
        GameObject proj = GamePoolManager.Instance.SpawnFromPool("GrapeProjectile", transform.position, Quaternion.identity);

        if (proj.TryGetComponent(out GrapeProjectile grapeProjectile))
        {
            if (enemyAI != null)
            {
                grapeProjectile.SetDamage(enemyAI.GetProjectileDamage());
            }
        }
    }
}
