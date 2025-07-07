using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpSpawner : MonoBehaviour
{
    [SerializeField] private GameObject goldCoin, healthGlobe, staminaGlobe;

    public void DropItems() {
        int randomNum = Random.Range(1, 5);

        if (randomNum == 1) {
             GamePoolManager.Instance.SpawnFromPool("Health", transform.position, Quaternion.identity);    
        }

        if (randomNum == 2) {
             GamePoolManager.Instance.SpawnFromPool("Stamina", transform.position, Quaternion.identity);
        }

        if (randomNum == 3) {
            int RandomAmountGold = Random.Range(1, 4);

            for (int i = 0; i < RandomAmountGold; i++)
            {
                GamePoolManager.Instance.SpawnFromPool("Gold Coin", transform.position, Quaternion.identity);
            }
        }
    }
}