using UnityEngine;
using UnityEngine.SceneManagement;

// 이 스크립트는 Init 씬에서 GameDataManager를 생성한 후, 
// 바로 다음 씬(Lobby)으로 넘어가는 역할을 합니다.
public class SceneLoader : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // 다음 프레임에 Lobby 씬을 로드합니다.
        // 이렇게 하면 GameDataManager가 먼저 생성되고 DontDestroyOnLoad가 설정될 시간을 확보할 수 있습니다.
        SceneManager.LoadScene("Lobby");
    }
} 