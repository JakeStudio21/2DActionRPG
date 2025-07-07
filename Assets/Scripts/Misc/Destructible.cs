using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    [SerializeField] private GameObject destroyVFX;

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.GetComponent<DamageSource>() || other.gameObject.GetComponent<Projectile>()) {
            GetComponent<PickUpSpawner>().DropItems();
            GamePoolManager.Instance.SpawnFromPool("DestroyVFX", transform.position, Quaternion.identity);
            gameObject.SetActive(false); // 파괴된 오브젝트를 풀로 반환
        }
    }
}