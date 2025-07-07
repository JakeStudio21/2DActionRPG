using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInventory : MonoBehaviour
{
    private int activeSlotIndexNum = 0;

    private PlayerControls playerControls;

    private void Awake() {
        playerControls = new PlayerControls();
    }

    private void OnEnable() {
        // playerControls.Enable(); // 키보드 입력을 비활성화하므로 주석 처리
        // playerControls.Inventory.Keyboard.performed += ToggleActiveSlot; // 키보드 입력을 비활성화하므로 주석 처리
    }

    private void OnDisable() {
        // playerControls.Inventory.Keyboard.performed -= ToggleActiveSlot; // 키보드 입력을 비활성화하므로 주석 처리
        // playerControls.Disable(); // 키보드 입력을 비활성화하므로 주석 처리
    }

    public void EquipStartingweapon() {
        ToggleActiveHighlight(0);
    }

    private void ToggleActiveSlot(UnityEngine.InputSystem.InputAction.CallbackContext ctx) {
        int numValue = (int)ctx.ReadValue<float>();
        ToggleActiveHighlight(numValue - 1);
    }

    private void ToggleActiveHighlight(int indexNum) {
        activeSlotIndexNum = indexNum;

        foreach (Transform inventorySlot in this.transform)
        {
            inventorySlot.GetChild(0).gameObject.SetActive(false);
        }

        this.transform.GetChild(indexNum).GetChild(0).gameObject.SetActive(true);

        ChangeActiveWeapon();
    }

    private void ChangeActiveWeapon() {

        var activeWeapon = FindObjectOfType<ActiveWeapon>();

        if (activeWeapon.CurrentActiveWeapon != null) {
            Destroy(activeWeapon.CurrentActiveWeapon.gameObject);
        }
        
        Transform childTransform = transform.GetChild(activeSlotIndexNum);
        InventorySlot inventorySlot = childTransform.GetComponentInChildren<InventorySlot>();
        WeaponInfo weaponInfo = inventorySlot.GetWeaponInfo();

        if (weaponInfo == null) {
            activeWeapon.WeaponNull();
            return;
        }

        // 풀링 시스템 적용: 무기 이름을 Tag로 사용
        GameObject newWeapon = GamePoolManager.Instance.SpawnFromPool(weaponInfo.weaponPrefab.name, activeWeapon.transform.position, activeWeapon.transform.rotation);
        newWeapon.transform.SetParent(activeWeapon.transform);

        activeWeapon.NewWeapon(newWeapon.GetComponent<MonoBehaviour>());
    }
}
