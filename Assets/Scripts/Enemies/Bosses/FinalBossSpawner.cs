using UnityEngine;
using System.Collections; // ← 꼭 추가!


public class FinalBossSpawner : MonoBehaviour
{
    public GameObject finalBossAPrefab;
    public GameObject finalBossBPrefab;
    public GameObject finalBossCPrefab;

    private GameObject currentBoss;

    void Start()
    {
        // Scene 시작 시 FinalBossA 스폰
        SpawnBoss(finalBossAPrefab, transform.position);
    }

    // 보스가 죽을 때마다 호출

    public void OnBossDeath(string bossId, Vector3 deathPosition)
{
    if (bossId == "FinalBossA")
    {
        StartCoroutine(SpawnBossWithDelay(finalBossBPrefab, deathPosition + new Vector3(1, 0, 0), 1.5f)); // 딜레이 적용!
    }
    else if (bossId == "FinalBossB")
    {
        StartCoroutine(SpawnBossWithDelay(finalBossCPrefab, deathPosition + new Vector3(1, 0, 0), 1.5f)); // 딜레이 적용!
    }
    else if (bossId == "FinalBossC")
    {
        // Victory 처리는 StageManager → FSMStageController.TriggerVictory() 경로에서 담당
        // (직접 호출 시 팝업과 사운드가 중복 재생되고 보상 데이터도 누락되는 문제 방지)
    }
}

    private IEnumerator SpawnBossWithDelay(GameObject bossPrefab, Vector3 pos, float delay) // ★
    {
        yield return new WaitForSeconds(delay);
        SpawnBoss(bossPrefab, pos);
    }

    private void SpawnBoss(GameObject bossPrefab, Vector3 pos)
    {
        if (bossPrefab == null) return;
        currentBoss = GamePoolManager.Instance.SpawnFromPool(bossPrefab.name, pos, Quaternion.identity);

        EnemyHealth eh = currentBoss.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.bossSpawner = this;
        }
    }

} 