/// <summary>
/// 저장소 인터페이스
/// 구현체 교체 가능 (JSON File, PlayerPrefs, Cloud 등)
/// </summary>
public interface IStorage
{
    /// <summary>
    /// 데이터 로드
    /// </summary>
    /// <param name="key">저장 키</param>
    /// <returns>저장된 데이터 (없으면 null 또는 빈 문자열)</returns>
    string Load(string key);
    
    /// <summary>
    /// 데이터 저장
    /// </summary>
    /// <param name="key">저장 키</param>
    /// <param name="data">저장할 데이터</param>
    void Save(string key, string data);
    
    /// <summary>
    /// 데이터 존재 여부 확인
    /// </summary>
    /// <param name="key">저장 키</param>
    /// <returns>존재 여부</returns>
    bool Exists(string key);
    
    /// <summary>
    /// 데이터 삭제
    /// </summary>
    /// <param name="key">저장 키</param>
    void Delete(string key);
}

