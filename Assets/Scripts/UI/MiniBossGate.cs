using UnityEngine;
using System.Linq;

public class MiniBossGate : MonoBehaviour
{
    [SerializeField] private GameObject gateObject; // 문 오브젝트
    [SerializeField] private string targetBossId; // "BossA" 또는 "BossB"
    
    private void Update()
    {
        // 특정 ID를 가진 중간보스 찾기
        EnemyHealth miniBoss = FindObjectsOfType<EnemyHealth>()
            .FirstOrDefault(e => e.isMiniBoss && e.bossId == targetBossId);

        // 해당 중간보스가 없거나(이미 파괴됨) 죽었으면 문 열기
        if ((miniBoss == null) || (miniBoss != null && miniBoss.isDead))
        {
            if (gateObject != null && gateObject.activeSelf)
            {
                Debug.Log($"{targetBossId} 처치! 문 열림!");
                gateObject.SetActive(false);
            }
        }
        else
        {
            if (gateObject != null && !gateObject.activeSelf)
            {
                Debug.Log($"{targetBossId}의 문이 닫혀있습니다.");
                gateObject.SetActive(true);
            }
        }
    }
}