using UnityEngine;

/// <summary>
/// EquipmentData 템플릿 로딩 인터페이스
/// - Resources, Addressables, AssetBundle 등 다양한 로딩 방식 지원
/// - Dependency Injection으로 구현체 교체 가능
/// </summary>
public interface IEquipmentResolver
{
    /// <summary>
    /// 템플릿 이름으로 EquipmentData 로드
    /// </summary>
    /// <param name="templateName">템플릿 이름 (예: "Sword_S_Equipment")</param>
    /// <returns>로드된 EquipmentData, 실패 시 null</returns>
    EquipmentData Resolve(string templateName);
    
    /// <summary>
    /// 비동기 로드 지원 여부
    /// </summary>
    bool SupportsAsync { get; }
    
    /// <summary>
    /// 비동기 로드 (지원 시)
    /// </summary>
    void ResolveAsync(string templateName, System.Action<EquipmentData> onComplete);
}

