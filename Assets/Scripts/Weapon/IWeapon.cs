using UnityEngine;

interface IWeapon {
    public void Attack();
    public EquipmentData GetEquipmentData();  // WeaponInfo → EquipmentData
    public void UpdateDirection(Vector2 direction, bool facingLeft);
}