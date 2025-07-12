using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    protected static T instance;
    public static T Instance { get { return instance; } }

    protected virtual void Awake() 
    {
        if (instance != null && instance != this) 
        {
            // Debug.LogWarning($"[Singleton] 중복 {typeof(T).Name}가 생성되어 파괴됩니다. (이름: {gameObject.name}, 인스턴스ID: {GetInstanceID()})");
            Destroy(gameObject);
            return;
        }
        instance = (T)this;
        
        if (!gameObject.transform.parent) 
        {
            DontDestroyOnLoad(gameObject);
        }
        
        // Debug.Log($"[Singleton] {typeof(T).Name} 생성됨 (이름: {gameObject.name}, 인스턴스ID: {GetInstanceID()})");
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            // Debug.Log($"[Singleton] {typeof(T).Name} 파괴됨 (이름: {gameObject.name}, 인스턴스ID: {GetInstanceID()})");
            instance = null;
        }
    }
}
