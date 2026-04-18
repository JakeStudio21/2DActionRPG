using UnityEngine;
using UnityEditor;

/// <summary>
/// Account.json 정리 도구
/// - 상점 전시용 중복 아이템 제거
/// - JSON 파일 용량 최적화
/// </summary>
public class AccountDataCleanupTool
{
    [MenuItem("Tools/Account/🧹 데이터 정합성 검증 및 자동 정리")]
    public static void ValidateAndCleanup()
    {
        // AccountDataManager 초기화
        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize();
        }
        
        // 정리 전 통계
        AccountDataManager.Instance.PrintStats();
        
        // 자동 정리 실행
        AccountDataManager.Instance.AutoCleanup();
        
        // 결과 다이얼로그
        EditorUtility.DisplayDialog(
            "정리 완료!",
            "✅ 데이터 정합성 검증 및 자동 정리가 완료되었습니다.\n\n" +
            "- 고아 아이템 제거\n" +
            "- 무효 귀속 정보 제거\n\n" +
            "Console 창에서 상세 결과를 확인하세요.",
            "확인"
        );
    }
    
    [MenuItem("Tools/Account/🗑️ Legacy 상점 아이템 제거 (1회성)")]
    public static void CleanupLegacyShopItems()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Legacy 상점 아이템 제거",
            "과거에 저장된 상점 전시용 아이템을 제거합니다.\n\n" +
            "⚠️ 이 작업은 1회만 실행하면 됩니다.\n\n" +
            "실제 플레이어 소유 아이템은 보호됩니다.\n\n" +
            "계속하시겠습니까?",
            "실행",
            "취소"
        );
        
        if (!confirm)
        {
            return;
        }
        
        // AccountDataManager 초기화
        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize();
        }
        
        // 정리 전 통계
        AccountDataManager.Instance.PrintStats();
        
        // Legacy 상점 아이템 제거
        int removedCount = AccountDataManager.Instance.CleanupLegacyShopItems();
        
        // 정리 후 통계
        AccountDataManager.Instance.PrintStats();
        
        // 결과 다이얼로그
        if (removedCount > 0)
        {
            EditorUtility.DisplayDialog(
                "정리 완료!",
                $"✅ {removedCount}개의 Legacy 상점 아이템을 제거했습니다.\n\n" +
                $"Account.json 파일 용량이 크게 감소했습니다.\n\n" +
                $"Console 창에서 상세 결과를 확인하세요.",
                "확인"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "정리 완료",
                "Legacy 상점 아이템이 없습니다. (정상 상태)\n\n" +
                "이미 최적화되어 있습니다.",
                "확인"
            );
        }
    }
    
    [MenuItem("Tools/Account/📊 Account 데이터 통계 보기")]
    public static void ShowAccountStats()
    {
        // AccountDataManager 초기화
        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize();
        }
        
        AccountDataManager.Instance.PrintStats();
        
        // JSON 파일 크기 확인
        string filePath = AccountDataManager.Instance.GetSavePath();
        if (System.IO.File.Exists(filePath))
        {
            long fileSize = new System.IO.FileInfo(filePath).Length;
            float fileSizeMB = fileSize / 1024f / 1024f;
            if (fileSizeMB > 1.0f)
            {
                Debug.LogWarning($"⚠️ 파일 크기가 큽니다! ({fileSizeMB:F2} MB)");
                Debug.LogWarning($"   💡 'Tools → Account → 상점 중복 아이템 제거'를 실행하세요.");
            }
            else
            {
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ JSON 파일을 찾을 수 없습니다: {filePath}");
        }
    }
    
    [MenuItem("Tools/Account/📂 Account.json 파일 열기")]
    public static void OpenAccountJsonFile()
    {
        string filePath = AccountDataManager.Instance.GetSavePath();
        
        if (System.IO.File.Exists(filePath))
        {
            // 기본 텍스트 에디터로 열기
            System.Diagnostics.Process.Start(filePath);
        }
        else
        {
            EditorUtility.DisplayDialog(
                "파일 없음",
                $"Account.json 파일을 찾을 수 없습니다.\n\n경로: {filePath}",
                "확인"
            );
        }
    }
    
    [MenuItem("Tools/Account/🔍 Account.json 실제 gold 값 확인 (JSON 직접 읽기)")]
    public static void CheckRealGoldValueInJsonFile()
    {
        // AccountDataManager 초기화
        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize();
        }
        
        string filePath = AccountDataManager.Instance.GetSavePath();
        
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError($"❌ Account.json 파일을 찾을 수 없습니다: {filePath}");
            return;
        }
        
        // JSON 파일 직접 읽기
        string jsonContent = System.IO.File.ReadAllText(filePath);
        
        // AccountData로 파싱
        AccountData accountDataFromFile = JsonUtility.FromJson<AccountData>(jsonContent);
        
        // 메모리의 AccountData
        AccountData accountDataInMemory = AccountDataManager.Instance.GetAccountData();
        if (accountDataFromFile.gold != accountDataInMemory.gold)
        {
            Debug.LogError($"❌ 불일치 발견!");
            Debug.LogError($"   JSON 파일: {accountDataFromFile.gold:N0}");
            Debug.LogError($"   메모리: {accountDataInMemory.gold:N0}");
            Debug.LogError($"   차이: {accountDataFromFile.gold - accountDataInMemory.gold:N0}");
            
            EditorUtility.DisplayDialog(
                "❌ 골드 값 불일치!",
                $"JSON 파일과 메모리의 골드 값이 다릅니다!\n\n" +
                $"JSON 파일: {accountDataFromFile.gold:N0}\n" +
                $"메모리: {accountDataInMemory.gold:N0}\n\n" +
                $"💡 원인: AccountDataManager.Load() 시 다른 파일을 읽었거나\n" +
                $"메모리가 오염되었을 가능성이 있습니다.",
                "확인"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "✅ 골드 값 일치",
                $"JSON 파일과 메모리의 골드 값이 일치합니다.\n\n" +
                $"골드: {accountDataFromFile.gold:N0}",
                "확인"
            );
        }
    }
    
    [MenuItem("Tools/Account/🧹 Legacy 골드 오염 정리 (PlayerSlot)")]
    public static void CleanupLegacyGoldInSlots()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Legacy 골드 오염 정리",
            "PlayerSlot JSON 파일의 legacy gold 필드를 0으로 초기화합니다.\n\n" +
            "⚠️ V2 시스템은 AccountData.gold만 사용합니다.\n\n" +
            "계속하시겠습니까?",
            "실행",
            "취소"
        );
        
        if (!confirm)
        {
            return;
        }
        
        // PlayerDataManager 초기화
        if (!PlayerDataManager.Instance)
        {
            Debug.LogError("❌ PlayerDataManager.Instance를 찾을 수 없습니다!");
            return;
        }
        
        int cleanedCount = 0;
        
        for (int i = 0; i < 3; i++)
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(i);
            
            if (slotData != null && !string.IsNullOrEmpty(slotData.playerName))
            {
                if (slotData.gold != 0)
                {
                    slotData.gold = 0;
                    PlayerDataManager.Instance.SaveSlotData(slotData);
                    cleanedCount++;
                }
                else
                {
                }
            }
        }
        
        if (cleanedCount > 0)
        {
            EditorUtility.DisplayDialog(
                "정리 완료!",
                $"✅ {cleanedCount}개 슬롯의 legacy gold를 정리했습니다.\n\n" +
                $"이제 V2 시스템(AccountData.gold)만 사용합니다.\n\n" +
                $"Console 창에서 상세 결과를 확인하세요.",
                "확인"
            );
        }
        else
        {
            EditorUtility.DisplayDialog(
                "정리 완료",
                "Legacy gold가 없습니다. (정상 상태)\n\n" +
                "이미 최적화되어 있습니다.",
                "확인"
            );
        }
    }
}

