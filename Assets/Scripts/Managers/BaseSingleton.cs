using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseSingleton : Singleton<BaseSingleton>
{
    private bool isDestroying = false; // 파괴 중인지 표시

    protected override void Awake()
    {
        // 실제 중복 매니저들이 있는지 체크 (같은 GameObject 내 매니저는 제외)
        if (HasDuplicateManagersFromOtherSources())
        {
            Debug.Log("[BaseSingleton] 다른 소스의 매니저들이 감지되어 중복 Managers 프리팹을 파괴합니다.");
            isDestroying = true;
            
            // 자식 매니저들의 Singleton 등록을 차단하기 위해 즉시 파괴
            DestroyImmediate(gameObject);
            return;
        }

        // 기존 매니저들이 없으면 정상적으로 초기화
        base.Awake();
        
        // 자신이 살아남은 경우에만 초기화 메시지 출력
        if (instance == this)
        {
            Debug.Log("[BaseSingleton] Managers 프리팹이 정상적으로 초기화되었습니다.");
        }
    }

    /// <summary>
    /// 다른 소스(다른 GameObject)에서 온 중복 매니저들이 있는지 체크
    /// </summary>
    private bool HasDuplicateManagersFromOtherSources()
    {
        // 1. BaseSingleton 자체 중복 체크 (다른 GameObject의 BaseSingleton)
        if (BaseSingleton.Instance != null && BaseSingleton.Instance.gameObject != this.gameObject)
        {
            Debug.Log("[BaseSingleton] 다른 GameObject의 BaseSingleton이 이미 존재합니다.");
            return true;
        }

        // 2. 다른 GameObject에서 온 매니저들 체크
        if (IsManagerFromDifferentGameObject<GameManager>() ||
            IsManagerFromDifferentGameObject<PlayerManager>() ||
            IsManagerFromDifferentGameObject<SaveManager>())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 특정 매니저가 다른 GameObject에서 온 것인지 확인
    /// </summary>
    private bool IsManagerFromDifferentGameObject<T>() where T : MonoBehaviour
    {
        // Singleton<T>를 통해 Instance에 접근
        var instanceProperty = typeof(T).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (instanceProperty != null)
        {
            var existingInstance = instanceProperty.GetValue(null) as T;
            if (existingInstance != null && existingInstance.gameObject != this.gameObject)
            {
                Debug.Log($"[BaseSingleton] 다른 GameObject의 {typeof(T).Name}이 이미 존재합니다: {existingInstance.gameObject.name}");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 파괴 중인지 확인하는 공개 메서드 (다른 매니저들이 참조 가능)
    /// </summary>
    public bool IsDestroying => isDestroying;
}
