using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // LINQ 사용


public class AreaExit : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private string SceneTransitionName;
    [SerializeField] private GameObject portalGate; // 문 오브젝트 연결

    private float waitToLoadTime = 1f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.GetComponent<PlayerController>())
        {
            SceneManagement.Instance.SetTransitionName(SceneTransitionName);
            // UIFade.Instance.FadeToBlack();
            StartCoroutine(LoadSceneRoutine());
        }

        if (other.CompareTag("Player"))
        {
            // 씬에 있는 모든 EnemyHealth 중 isBoss == true인 오브젝트 찾기
            EnemyHealth boss = FindObjectsOfType<EnemyHealth>()
                .FirstOrDefault(e => e.isBoss);

            // 보스가 null(이미 Destroy됨) 이거나, isDead==true면 씬 이동
            if ((boss == null) || (boss != null && boss.isDead))
            {
                Debug.Log("보스가 죽었으니 씬 이동!");
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.Log("보스를 먼저 처치해야 합니다!");
            }
        }
    }

    private void Update()
    {
        EnemyHealth boss = FindObjectsOfType<EnemyHealth>()
            .FirstOrDefault(e => e.isBoss);

        // 보스가 없거나(이미 Destroy됨), 죽었으면 문을 비활성화
        if ((boss == null) || (boss != null && boss.isDead))
        {
            if (portalGate != null && portalGate.activeSelf)
            {
                Debug.Log("문 비활성화!");
                portalGate.SetActive(false);
            }
        }
        else
        {
            if (portalGate != null && !portalGate.activeSelf)
            {
                Debug.Log("문 활성화!");
                portalGate.SetActive(true);
            }
        }
    }


    private IEnumerator LoadSceneRoutine()
    {
        while (waitToLoadTime >= 0)
        {
            waitToLoadTime -= Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}













// public class AreaExit : MonoBehaviour
// {
//     [SerializeField] private string sceneToLoad;
//     [SerializeField] private string SceneTransitionName;

//     private float waitToLoadTime = 1f;

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         if (other.gameObject.GetComponent<PlayerController>())
//         {
//             SceneManagement.Instance.SetTransitionName(SceneTransitionName);
//             UIFade.Instance.FadeToBlack();
//             StartCoroutine(LoadSceneRoutine());
//         }
        
//         if (other.CompareTag("Player"))
//         {
//             // 씬에 있는 모든 EnemyHealth 중 isBoss == true인 오브젝트 찾기
//             EnemyHealth boss = FindObjectsOfType<EnemyHealth>()
//                 .FirstOrDefault(e => e.isBoss);

//             if (boss != null && boss.isDead)
//             {
//                 // 보스가 죽었으면 씬 이동
//                 SceneManager.LoadScene(sceneToLoad);
//             }
//             else
//             {
//                 Debug.Log("보스를 먼저 처치해야 합니다!");
//             }
//         }


//     }

//     private IEnumerator LoadSceneRoutine() {
//         while (waitToLoadTime >= 0)
//         {
//             waitToLoadTime -= Time.deltaTime;
//             yield return null;
//         }

//         SceneManager.LoadScene(sceneToLoad);
//     }
// }
