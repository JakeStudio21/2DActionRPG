/// <summary>
/// 풀에서 스폰될 때 태그를 주입받는 인터페이스
/// GamePoolManager.SpawnFromPool이 스폰 시점에 올바른 풀 태그를 직접 설정한다.
/// Inspector 수동 설정이나 이름 기반 추측에 의존하지 않도록 한다.
/// </summary>
public interface IPoolTagReceiver
{
    void SetPoolTag(string tag);
}
