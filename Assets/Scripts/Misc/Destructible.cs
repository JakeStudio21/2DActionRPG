using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour
{
    [SerializeField] private GameObject destroyVFX;

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.GetComponent<DamageSource>() || other.gameObject.GetComponent<Projectile>()) {
            GetComponent<PickUpSpawner>().DropItems();
            
            // ⭐ 수정: Inspector에서 할당된 VFX 프리팹을 직접 사용
            if (destroyVFX != null)
            {
                GameObject vfxInstance = Instantiate(destroyVFX, transform.position, Quaternion.identity);
                
                // VFX가 자동으로 파괴되지 않는 경우를 위한 안전장치
                Destroy(vfxInstance, 3f); // 3초 후 자동 파괴
            }
            else
            {
                Debug.LogWarning($"[Destructible] {gameObject.name}에 destroyVFX가 할당되지 않았습니다!");
            }
            
            gameObject.SetActive(false); // 파괴된 오브젝트를 풀로 반환
        }
    }
}