using System.Collections.Generic;

namespace DebugTools
{
    /// <summary>
    /// 치트 명령어 실행 결과
    /// - 성공/실패 여부
    /// - 결과 메시지
    /// - 생성된 아이템 인스턴스 ID 목록
    /// </summary>
    [System.Serializable]
    public class CheatResult
    {
        /// <summary>
        /// 명령어 실행 성공 여부
        /// </summary>
        public bool success;
        
        /// <summary>
        /// 결과 메시지 (UI 표시용)
        /// </summary>
        public string message;
        
        /// <summary>
        /// 생성된 아이템 인스턴스 ID 목록 (add 명령어 전용)
        /// </summary>
        public List<ItemInstanceId> createdInstanceIds = new List<ItemInstanceId>();
        
        /// <summary>
        /// 추가 데이터 (확장용)
        /// </summary>
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
        
        // ========================================
        // 정적 팩토리 메서드
        // ========================================
        
        /// <summary>
        /// 성공 결과 생성
        /// </summary>
        public static CheatResult Success(string msg)
        {
            return new CheatResult
            {
                success = true,
                message = msg
            };
        }
        
        /// <summary>
        /// 실패 결과 생성
        /// </summary>
        public static CheatResult Fail(string msg)
        {
            return new CheatResult
            {
                success = false,
                message = msg
            };
        }
        
        /// <summary>
        /// 성공 결과 + 생성된 아이템 ID 포함
        /// </summary>
        public static CheatResult SuccessWithItems(string msg, List<ItemInstanceId> itemIds)
        {
            return new CheatResult
            {
                success = true,
                message = msg,
                createdInstanceIds = itemIds ?? new List<ItemInstanceId>()
            };
        }
        
        // ========================================
        // 헬퍼 메서드
        // ========================================
        
        /// <summary>
        /// 메타데이터 추가
        /// </summary>
        public CheatResult WithMetadata(string key, object value)
        {
            metadata[key] = value;
            return this;
        }
        
        /// <summary>
        /// 디버그용 문자열 출력
        /// </summary>
        public override string ToString()
        {
            var status = success ? "✅ 성공" : "❌ 실패";
            var itemCount = createdInstanceIds.Count > 0 
                ? $" (아이템 {createdInstanceIds.Count}개 생성)" 
                : "";
            
            return $"[CheatResult] {status}: {message}{itemCount}";
        }
    }
}

