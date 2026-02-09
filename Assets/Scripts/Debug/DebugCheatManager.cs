using UnityEngine;
using System;
using System.Linq;

namespace DebugTools
{
    /// <summary>
    /// 디버그 치트 관리자 (싱글톤)
    /// - 텍스트 명령어 파싱
    /// - 명령어 실행 (CheatService 호출)
    /// - UI/콘솔/버튼에서 공통 재사용
    /// </summary>
    public class DebugCheatManager : MonoBehaviour
    {
        private static DebugCheatManager _instance;
        public static DebugCheatManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 씬에서 찾기
                    _instance = FindObjectOfType<DebugCheatManager>();
                    
                    // 없으면 생성
                    if (_instance == null)
                    {
                        var go = new GameObject("[DebugCheatManager]");
                        _instance = go.AddComponent<DebugCheatManager>();
                        DontDestroyOnLoad(go);
                        
                        Debug.Log("🎮 [DebugCheatManager] 싱글톤 생성 완료");
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        // ========================================
        // 명령어 실행
        // ========================================
        
        /// <summary>
        /// 치트 명령어 실행 (텍스트 기반)
        /// </summary>
        /// <param name="commandText">명령어 텍스트 (예: "add Sword_C 3 enhance:5")</param>
        /// <returns>실행 결과</returns>
        public CheatResult ExecuteCommand(string commandText)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                return CheatResult.Fail("❌ 명령어가 비어있습니다.");
            }
            
            Debug.Log($"🎮 [CheatManager] 명령어 실행 시도: {commandText}");
            
            try
            {
                // 1. 명령어 파싱
                var command = ParseCommand(commandText);
                
                Debug.Log($"📋 [CheatManager] 파싱 결과: {command}");
                
                // 2. 명령어 타입별 실행
                CheatResult result = command.commandType switch
                {
                    "add" => CheatService.CreateItem(
                        command.targetId,
                        command.count,
                        command.GetEnhanceLevel(),
                        command.IsBound()
                    ),
                    "addmaterial" => CheatService.AddMaterial(
                        ParseMaterialType(command.targetId),
                        command.count
                    ),
                    "addgold" => CheatService.AddGold(
                        int.Parse(command.targetId)
                    ),
                    "setenhance" => CheatService.SetEnhanceLevel(
                        ParseInstanceId(command.targetId),
                        command.count // count를 새 레벨로 사용
                    ),
                    _ => CheatResult.Fail($"❌ 알 수 없는 명령어: {command.commandType}")
                };
                
                Debug.Log($"📊 [CheatManager] 실행 결과: {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                string errorMsg = $"❌ 명령어 실행 오류: {ex.Message}";
                Debug.LogError($"[CheatManager] {errorMsg}\n{ex.StackTrace}");
                return CheatResult.Fail(errorMsg);
            }
        }
        
        // ========================================
        // 명령어 파싱
        // ========================================
        
        /// <summary>
        /// 명령어 텍스트 → CheatCommand 변환
        /// 형식: <command> <id> [count] [param:value] [param:value]
        /// </summary>
        private CheatCommand ParseCommand(string text)
        {
            // 공백 기준 분리
            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length < 2)
            {
                throw new Exception("명령어 형식 오류. 사용법: <command> <id> [count] [param:value]");
            }
            
            var command = new CheatCommand
            {
                commandType = parts[0].ToLower(),
                targetId = parts[1]
            };
            
            // 2번 인덱스부터 파라미터 파싱
            for (int i = 2; i < parts.Length; i++)
            {
                var part = parts[i];
                
                // key:value 형식 (예: enhance:5, bound:true)
                if (part.Contains(":"))
                {
                    var colonIndex = part.IndexOf(':');
                    var key = part.Substring(0, colonIndex);
                    var value = part.Substring(colonIndex + 1);
                    
                    command.parameters[key] = value;
                }
                // 숫자만 있으면 count
                else if (int.TryParse(part, out var count))
                {
                    command.count = count;
                }
                // 그 외는 무시
                else
                {
                    Debug.LogWarning($"⚠️ [CheatManager] 알 수 없는 파라미터 무시: {part}");
                }
            }
            
            return command;
        }
        
        /// <summary>
        /// MaterialType 문자열 → enum 변환
        /// </summary>
        private MaterialType ParseMaterialType(string text)
        {
            // enum 파싱 시도
            if (Enum.TryParse<MaterialType>(text, true, out var materialType))
            {
                return materialType;
            }
            
            // 별칭 지원 (예: "fragment" → WeaponFragment)
            string lowerText = text.ToLower();
            if (lowerText.Contains("weapon") && lowerText.Contains("fragment"))
                return MaterialType.WeaponFragment;
            if (lowerText.Contains("weapon") && lowerText.Contains("crystal"))
                return MaterialType.WeaponCrystal;
            if (lowerText.Contains("weapon") && lowerText.Contains("core"))
                return MaterialType.WeaponCore;
            if (lowerText.Contains("armor") && lowerText.Contains("fragment"))
                return MaterialType.ArmorFragment;
            if (lowerText.Contains("armor") && lowerText.Contains("crystal"))
                return MaterialType.ArmorCrystal;
            if (lowerText.Contains("armor") && lowerText.Contains("core"))
                return MaterialType.ArmorCore;
            if (lowerText.Contains("accessory") && lowerText.Contains("fragment"))
                return MaterialType.AccessoryFragment;
            if (lowerText.Contains("accessory") && lowerText.Contains("crystal"))
                return MaterialType.AccessoryCrystal;
            if (lowerText.Contains("accessory") && lowerText.Contains("core"))
                return MaterialType.AccessoryCore;
            
            throw new Exception($"유효하지 않은 재료 타입: {text}");
        }
        
        /// <summary>
        /// InstanceId 문자열 → ItemInstanceId 변환
        /// GUID 문자열 형식 지원 (예: "a1b2c3d4-e5f6-...")
        /// </summary>
        private ItemInstanceId ParseInstanceId(string text)
        {
            // "#" 접두사 제거 (선택사항)
            if (text.StartsWith("#"))
            {
                text = text.Substring(1);
            }
            
            // 빈 문자열 체크
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new Exception("인스턴스 ID가 비어있습니다.");
            }
            
            // GUID 문자열로 ItemInstanceId 생성
            return new ItemInstanceId { id = text };
        }
        
        // ========================================
        // ⭐ 프리셋 명령어 (UI 버튼용)
        // ========================================
        
        /// <summary>
        /// 간편 아이템 추가
        /// </summary>
        public CheatResult QuickAddItem(string templateId, int count = 1, int enhance = 0, bool bound = false)
        {
            string boundParam = bound ? " bound:true" : "";
            return ExecuteCommand($"add {templateId} {count} enhance:{enhance}{boundParam}");
        }
        
        /// <summary>
        /// 간편 재료 추가
        /// </summary>
        public CheatResult QuickAddMaterial(MaterialType materialType, int count = 100)
        {
            return ExecuteCommand($"addmaterial {materialType} {count}");
        }
        
        /// <summary>
        /// 간편 골드 추가
        /// </summary>
        public CheatResult QuickAddGold(int amount = 100000)
        {
            return ExecuteCommand($"addgold {amount}");
        }
        
        /// <summary>
        /// 간편 강화 레벨 설정
        /// </summary>
        public CheatResult QuickSetEnhance(ItemInstanceId instanceId, int level)
        {
            return ExecuteCommand($"setenhance {instanceId.id} {level}");
        }
        
        // ========================================
        // 헬퍼 메서드
        // ========================================
        
        /// <summary>
        /// 사용 가능한 명령어 목록 반환 (도움말용)
        /// </summary>
        public string GetHelpText()
        {
            return @"🎮 치트 명령어 도움말

📦 아이템 생성:
  add <templateId> [count] [enhance:0~15] [bound:true/false]
  
  무기 예시:
  · add ITEM_SWORD_C 1           (C등급 검)
  · add ITEM_SWORD_A 1 enhance:5 (A등급 검 +5)
  · add ITEM_SWORD_S 1 enhance:14 (S등급 검 +14)
  · add ITEM_BOW_B 1             (B등급 활)
  
  방어구 예시:
  · add ITEM_ARMOR_ASSASIN_C 1
  · add ITEM_ARMOR_WARRIOR_S 1 enhance:7
  
  악세사리 예시:
  · add ITEM_BELT_ASSASIN_A 1
  · add ITEM_BOOTS_WIZARD_C 1

💎 재료 추가:
  addmaterial <materialType> [count]
  · addmaterial WeaponFragment 100  (무기 파편)
  · addmaterial WeaponCrystal 50    (무기 결정)
  · addmaterial ArmorFragment 100   (방어구 파편)
  · addmaterial ArmorCrystal 50     (방어구 결정)
  · addmaterial AccessoryFragment 100

💰 골드 추가:
  addgold <amount>
  · addgold 10000    (1만 골드)
  · addgold 100000   (10만 골드)
  · addgold 1000000  (100만 골드)

⚡ 강화 레벨 설정:
  setenhance <instanceId> <level>
  · setenhance #a1b2c3d4-e5f6-... 7
  ※ instanceId는 GUID (보관창고에서 확인, # 접두어 선택)
";
        }
    }
}

