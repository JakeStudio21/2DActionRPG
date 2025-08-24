using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleCueTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("🧪 [SimpleCueTest] 테스트 시작");
        
        // 5초 후 테스트 실행
        Invoke("RunTest", 5f);
    }

    // Update is called once per frame
    void Update()
    {
        // 간단한 키 테스트
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("🧪 [SimpleCueTest] 스페이스바 테스트");
            
            // FindObjectOfType으로 안전하게 접근
            var cuePlayer = FindObjectOfType<CueSystem.CuePlayer>();
            if (cuePlayer != null)
            {
                Debug.Log("✅ [SimpleCueTest] CuePlayer 접근 성공");
            }
        }
    }

    void RunTest()
    {
        Debug.Log("🧪 [SimpleCueTest] 5초 후 테스트 실행");
        
        // CuePlayer 존재 확인
        var cuePlayer = FindObjectOfType<CueSystem.CuePlayer>();
        if (cuePlayer != null)
        {
            Debug.Log("✅ [SimpleCueTest] CuePlayer 발견됨");
        }
        else
        {
            Debug.LogError("🔴 [SimpleCueTest] CuePlayer를 찾을 수 없음");
        }
        
        // CueRegistry 존재 확인
        var cueRegistry = FindObjectOfType<CueSystem.CueRegistry>();
        if (cueRegistry != null)
        {
            Debug.Log("✅ [SimpleCueTest] CueRegistry 발견됨");
        }
        else
        {
            Debug.LogError("🔴 [SimpleCueTest] CueRegistry를 찾을 수 없음");
        }
    }
}
