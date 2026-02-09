using System.Collections.Generic;

namespace DebugTools
{
    /// <summary>
    /// 치트 명령어 데이터 구조
    /// - 텍스트 명령어를 파싱한 결과 저장
    /// - UI/콘솔/버튼에서 공통으로 사용
    /// </summary>
    [System.Serializable]
    public class CheatCommand
    {
        /// <summary>
        /// 명령어 타입 ("add", "addmaterial", "addgold", "setenhance")
        /// </summary>
        public string commandType;
        
        /// <summary>
        /// 대상 ID (TemplateId, MaterialType, InstanceId 등)
        /// </summary>
        public string targetId;
        
        /// <summary>
        /// 개수 (기본값: 1)
        /// </summary>
        public int count = 1;
        
        /// <summary>
        /// 추가 파라미터 (key-value 형식)
        /// 예: { "enhance": "5", "bound": "true" }
        /// </summary>
        public Dictionary<string, string> parameters = new Dictionary<string, string>();
        
        // ========================================
        // 헬퍼 메서드
        // ========================================
        
        /// <summary>
        /// 강화 레벨 파라미터 조회 (기본값: 0)
        /// </summary>
        public int GetEnhanceLevel()
        {
            if (parameters.TryGetValue("enhance", out var val) && int.TryParse(val, out var level))
            {
                return level;
            }
            return 0;
        }
        
        /// <summary>
        /// 귀속 여부 파라미터 조회 (기본값: false)
        /// </summary>
        public bool IsBound()
        {
            if (parameters.TryGetValue("bound", out var val))
            {
                return val.ToLower() == "true" || val == "1";
            }
            return false;
        }
        
        /// <summary>
        /// 특정 파라미터 값 조회 (없으면 빈 문자열)
        /// </summary>
        public string GetParameter(string key, string defaultValue = "")
        {
            return parameters.TryGetValue(key, out var val) ? val : defaultValue;
        }
        
        /// <summary>
        /// 디버그용 문자열 출력
        /// </summary>
        public override string ToString()
        {
            var paramStr = parameters.Count > 0 
                ? string.Join(", ", parameters) 
                : "없음";
            
            return $"[CheatCommand] {commandType} {targetId} x{count} | 파라미터: {paramStr}";
        }
    }
}

