using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSource : MonoBehaviour
{
    private int damageAmount;

    private void Start() {

        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        MonoBehaviour currentActiveweapon = activeWeapon.CurrentActiveWeapon;
        damageAmount = (currentActiveweapon as IWeapon).GetWeaponInfo().weaponDamage;
    }

    private void OnTriggerEnter2D(Collider2D other) {
            EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
            enemyHealth?.TakeDamage(damageAmount);
        }
}
