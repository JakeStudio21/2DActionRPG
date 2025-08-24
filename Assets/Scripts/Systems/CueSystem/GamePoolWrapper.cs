using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CueSystem
{
    /// <summary>
    /// 🚫 GamePoolManager 직참조 차단 래퍼
    /// SpawnFromPool 직접 호출을 컴파일 레벨에서 방지
    /// </summary>
    public static class GamePoolWrapper
    {
        /// <summary>
        /// ❌ 직접 호출 금지 - CuePlayer를 통해서만 사용
        /// </summary>
        [System.Obsolete("직접 호출 금지! CuePlayer.Play()를 사용하세요.", true)]
        public static GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            #if UNITY_EDITOR
            Debug.LogError("🔴 [GamePoolWrapper] 직참조 호출 감지! CuePlayer.Play()를 사용하세요.");
            #endif
            
            // ✅ 수정: 조건부 컴파일로 변경
            #if ENABLE_STRICT_POOL_BLOCKING
            #error "GamePoolManager.SpawnFromPool() 직접 호출 금지! CuePlayer.Play(eventKey, domain, context)를 사용하세요."
            #else
            // 개발 단계에서는 경고만 출력
            Debug.LogWarning("⚠️ [GamePoolWrapper] 직참조 호출됨! 나중에 CuePlayer.Play()로 마이그레이션하세요.");
            return GamePoolManager.Instance?.SpawnFromPool(tag, position, rotation);
            #endif
        }
        
        /// <summary>
        /// 🔒 내부 전용 - CuePlayer에서만 호출 가능
        /// </summary>
        internal static GameObject InternalSpawnFromPool(string poolKey, Vector3 position, Quaternion rotation)
        {
            if (GamePoolManager.Instance != null)
            {
                return GamePoolManager.Instance.SpawnFromPool(poolKey, position, rotation);
            }
            
            Debug.LogError("🔴 [GamePoolWrapper] GamePoolManager가 없습니다!");
            return null;
        }
        
        /// <summary>
        /// 🔒 내부 전용 - 풀 반환
        /// </summary>
        internal static void InternalReturnToPool(string poolKey, GameObject obj)
        {
            if (GamePoolManager.Instance != null)
            {
                GamePoolManager.Instance.ReturnToPool(poolKey, obj);
            }
        }
    }
}
