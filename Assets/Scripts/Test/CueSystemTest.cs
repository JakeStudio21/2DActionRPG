using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem;

public class CueSystemTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(KeyCode.T)) TestDirectCall();
        if (Input.GetKeyDown(KeyCode.Y)) TestCueSystem();
        if (Input.GetKeyDown(KeyCode.U)) PrintStats();
        if (Input.GetKeyDown(KeyCode.I)) TestCongestionControl();
        if (Input.GetKeyDown(KeyCode.E)) TestExtensionMethods();
#endif
    }
    
    void TestDirectCall()
    {
        // ✅ 수정: 직참조 차단 테스트를 로그로만 확인
        Debug.Log("🧪 [CueSystemTest] 직참조 차단 테스트: GamePoolWrapper.SpawnFromPool()은 Obsolete로 차단됨");
        Debug.Log("   → 이는 정상적인 동작입니다. 컴파일 에러/경고가 발생하면 차단이 성공한 것입니다.");
        
        // 대신 정상적인 방법으로 테스트
        TestCueSystem();
    }
    
    void TestCueSystem()
    {
        // ✅ 안전성 체크 추가
        if (!CheckCueSystemReady())
        {
            Debug.LogError("🔴 [CueSystemTest] Cue 시스템이 준비되지 않았습니다!");
            return;
        }
        
        // Cue 시스템 테스트
        var context = CueContext.At(transform.position);
        bool success = CuePlayer.Instance.Play("test.effect", "Global", context);
        
        Debug.Log($"🧪 [CueSystemTest] Cue 시스템 테스트 결과: {success}");
    }
    
    void PrintStats()
    {
        if (!CheckCueSystemReady()) return;
        
        Debug.Log("🧪 [CueSystemTest] === 통계 출력 ===");
        CuePlayer.Instance.PrintStats();
        CueRegistry.Instance.PrintStats();
    }

    void TestCongestionControl()
    {
        if (!CheckCueSystemReady()) return;
        
        var context = CueContext.At(Vector3.zero);
        
        Debug.Log("🧪 [CueSystemTest] 혼잡 제어 테스트 시작 - 35개 연속 재생");
        
        // 35개 연속 재생 (상한 30개)
        int successCount = 0;
        for (int i = 0; i < 35; i++)
        {
            bool success = CuePlayer.Instance.Play("stress.test", "Global", context);
            if (success) successCount++;
        }
        
        Debug.Log($"🧪 [CueSystemTest] 혼잡 제어 테스트 완료 - 성공: {successCount}/35");
        CuePlayer.Instance.PrintStats();
    }

    void TestExtensionMethods()
    {
        if (!CheckCueSystemReady()) return;
        
        Debug.Log("🧪 [CueSystemTest] 확장 메서드 테스트 시작");
        
        // Transform 확장 메서드
        bool result1 = this.transform.Emit("test.transform", "Global", 1.5f);
        
        // MonoBehaviour 확장 메서드
        bool result2 = this.Emit("test.behaviour", "Global", 2.0f);
        
        // GameObject 확장 메서드
        bool result3 = this.gameObject.Emit("test.gameobject", "Global", 0.5f);
        
        // 정적 메서드
        bool result4 = CueEmitter.EmitAt(Vector3.up, "test.static", "Global", 3.0f);
        
        Debug.Log($"🧪 [CueSystemTest] 확장 메서드 테스트 완료 - Transform:{result1}, Behaviour:{result2}, GameObject:{result3}, Static:{result4}");
    }
    
    /// <summary>
    /// ✅ Cue 시스템 준비 상태 체크
    /// </summary>
    private bool CheckCueSystemReady()
    {
        if (CuePlayer.Instance == null)
        {
            Debug.LogError("🔴 [CueSystemTest] CuePlayer.Instance가 null입니다! CuePlayer 컴포넌트를 씬에 추가해주세요.");
            return false;
        }
        
        if (CueRegistry.Instance == null)
        {
            Debug.LogError("🔴 [CueSystemTest] CueRegistry.Instance가 null입니다! CueRegistry 컴포넌트를 씬에 추가해주세요.");
            return false;
        }
        
        return true;
    }
}
