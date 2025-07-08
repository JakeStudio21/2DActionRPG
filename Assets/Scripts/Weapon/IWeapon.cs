using UnityEngine;

interface IWeapon {
    public void Attack();
    public WeaponInfo GetWeaponInfo();
    public void UpdateDirection(Vector2 direction, bool facingLeft);
}