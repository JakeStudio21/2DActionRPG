using UnityEngine;

/// <summary>
/// 에디터 전용 디버그 로그 래퍼.
/// UNITY_EDITOR 심볼이 없는 빌드에서는 호출부 코드 자체가 제거되어 GC Alloc 및 CPU 비용 없음.
/// </summary>
public static class Dbg
{
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(string msg) => Debug.Log(msg);

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(string msg) => Debug.LogWarning(msg);
}
