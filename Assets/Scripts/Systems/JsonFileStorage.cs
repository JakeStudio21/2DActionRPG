using System.IO;
using UnityEngine;

/// <summary>
/// JSON 파일 기반 저장소 (기본 구현)
/// persistentDataPath 사용
/// </summary>
public class JsonFileStorage : IStorage
{
    private readonly string basePath;
    
    public JsonFileStorage()
    {
        basePath = Application.persistentDataPath;
        
        // 저장 폴더 확인/생성
        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }
    }
    
    public string Load(string key)
    {
        string filePath = GetFilePath(key);
        
        if (!File.Exists(filePath))
        {
            return null;
        }
        
        try
        {
            string data = File.ReadAllText(filePath);
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"💥 [JsonFileStorage] 로드 실패: {key} - {e.Message}");
            return null;
        }
    }
    
    public void Save(string key, string data)
    {
        string filePath = GetFilePath(key);
        
        try
        {
            File.WriteAllText(filePath, data);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"💥 [JsonFileStorage] 저장 실패: {key} - {e.Message}");
        }
    }
    
    public bool Exists(string key)
    {
        string filePath = GetFilePath(key);
        return File.Exists(filePath);
    }
    
    public void Delete(string key)
    {
        string filePath = GetFilePath(key);
        
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"💥 [JsonFileStorage] 삭제 실패: {key} - {e.Message}");
            }
        }
    }
    
    private string GetFilePath(string key)
    {
        return Path.Combine(basePath, $"{key}.json");
    }
    
    /// <summary>
    /// 저장 폴더 경로 반환 (디버깅용)
    /// </summary>
    public string GetBasePath()
    {
        return basePath;
    }
}

