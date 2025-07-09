using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpSpawner : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private bool canDropHealth = true;
    [SerializeField] private bool canDropGold = true;
    
    [Header("Health Drop")]
    [SerializeField] [Range(0f, 100f)] private float healthDropChance = 50f;
    [SerializeField] private int healthDropAmount = 1;
    
    [Header("Gold Drop")]
    [SerializeField] [Range(0f, 100f)] private float goldDropChance = 50f;
    [SerializeField] private int goldDropMinAmount = 1;
    [SerializeField] private int goldDropMaxAmount = 3;

    public void DropItems() {
        // Health 드랍 체크
        if (canDropHealth && Random.Range(0f, 100f) <= healthDropChance) {
            for (int i = 0; i < healthDropAmount; i++) {
                GamePoolManager.Instance.SpawnFromPool("Health", transform.position, Quaternion.identity);
            }
        }

        // Gold 드랍 체크  
        if (canDropGold && Random.Range(0f, 100f) <= goldDropChance) {
            int goldAmount = Random.Range(goldDropMinAmount, goldDropMaxAmount + 1);
            
            for (int i = 0; i < goldAmount; i++) {
                GamePoolManager.Instance.SpawnFromPool("Gold Coin", transform.position, Quaternion.identity);
            }
        }
    }
}