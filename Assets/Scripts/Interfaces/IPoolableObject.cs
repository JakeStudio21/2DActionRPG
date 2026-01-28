/// <summary>
/// 오브젝트 풀링 표준 인터페이스
/// 모든 풀링 가능한 오브젝트는 이 인터페이스를 구현해야 함
/// </summary>
public interface IPoolableObject
{
    /// <summary>
    /// 풀에서 꺼내질 때 호출 (초기화)
    /// </summary>
    void OnSpawnFromPool();
    
    /// <summary>
    /// 풀로 반환될 때 호출 (정리)
    /// </summary>
    void OnReturnToPool();
}

