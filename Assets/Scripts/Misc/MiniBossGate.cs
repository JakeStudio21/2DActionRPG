using UnityEngine;
using System.Linq;

public class MiniBossGate : MonoBehaviour
{
    [SerializeField] private string targetBossId = "BossA"; // Inspector에서 설정
    [SerializeField] private GameObject gateObject; // 닫힐 문 오브젝트

    private void Update()
    {
        // 특정 ID를 가진 중간보스 찾기 - ✅ 메서드 호출로 변경
        EnemyHealth miniBoss = FindObjectsOfType<EnemyHealth>()
            .FirstOrDefault(e => e.IsMiniBoss() && e.GetBossId() == targetBossId);

        // 해당 중간보스가 없거나(이미 파괴됨) 죽었으면 문 열기
        if ((miniBoss == null) || (miniBoss != null && miniBoss.isDead))
        {
            if (gateObject != null && gateObject.activeSelf)
            {
                gateObject.SetActive(false);
            }
        }
    }
} 