using UnityEngine;
using System.Collections; // ← 꼭 추가!


public class FinalBossSpawner : MonoBehaviour
{
    public GameObject finalBossAPrefab;
    public GameObject finalBossBPrefab;
    public GameObject finalBossCPrefab;

    private GameObject currentBoss;
    private ResultPopupController resultPopup;

    void Start()
    {
        // Scene 시작 시 FinalBossA 스폰
        SpawnBoss(finalBossAPrefab, transform.position);

        // ResultPopupController 미리 참조
        resultPopup = FindObjectOfType<ResultPopupController>();
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
        Debug.Log("모든 보스 처치! 엔딩 연출 등");

        StartCoroutine(ShowVictoryPopupAfterDelay(1.5f));
    // FindObjectOfType<ResultPopupController>().Show(true); // Victory
    }
}

    private IEnumerator ShowVictoryPopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        var popup = FindObjectOfType<ResultPopupController>();
        if (popup != null && popup.gameObject != null)
        {
            popup.Show(true);
        }
        else
        {
            Debug.LogError("ResultPopupController를 찾을 수 없습니다!");
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