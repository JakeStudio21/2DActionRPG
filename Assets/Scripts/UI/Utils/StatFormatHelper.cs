using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UI.Utils
{
    /// <summary>
    /// 스탯 포맷팅 헬퍼 유틸리티
    /// - 동적 스탯(주옵션/부옵션) 표시를 위한 통합 포맷팅
    /// - StatDefinitions 기반 자동 단위 변환 (Flat/Percent)
    /// - HTML 태그 기반 색상 적용
    /// </summary>
    public static class StatFormatHelper
    {
        #region 색상 상수
        
        /// <summary>
        /// 주옵션 색상 (주황색)
        /// </summary>
        private const string MainStatColor = "#FFA500";
        
        /// <summary>
        /// 부옵션 색상 (하늘색)
        /// </summary>
        private const string SubStatColor = "#00FFFF";
        
        /// <summary>
        /// 구분선 색상 (회색)
        /// </summary>
        private const string SeparatorColor = "#AAAAAA";
        
        #endregion
        
        #region 핵심 포맷팅 메서드
        
        /// <summary>
        /// 스탯 타입과 값을 받아서 포맷된 문자열 반환
        /// </summary>
        /// <param name="statType">스탯 타입 (EStatType)</param>
        /// <param name="value">스탯 값</param>
        /// <param name="includeLabel">스탯 이름 포함 여부 (true: "방어력 +45", false: "+45")</param>
        /// <returns>포맷된 스탯 문자열</returns>
        public static string FormatStat(EStatType statType, float value, bool includeLabel = true)
        {
            // StatDefinitions에서 정의 가져오기
            var definition = StatDefinitions.Get(statType);
            if (definition == null)
            {
                Debug.LogWarning($"[StatFormatHelper] StatDefinition을 찾을 수 없습니다: {statType}");
                return includeLabel ? $"{statType} +{value:F1}" : $"+{value:F1}";
            }
            
            // 단위별 포맷팅
            string formattedValue = FormatValueByUnit(value, definition.unit);
            
            // 라벨 포함 여부에 따라 반환
            if (includeLabel)
            {
                return $"{definition.displayName} {formattedValue}";
            }
            else
            {
                return formattedValue;
            }
        }
        
        /// <summary>
        /// 단위(Unit)에 따라 값 포맷팅
        /// </summary>
        private static string FormatValueByUnit(float value, StatUnit unit)
        {
            switch (unit)
            {
                case StatUnit.Flat:
                    // 고정값: 정수 표시 (소수점 버림)
                    return $"+{Mathf.FloorToInt(value)}";
                
                case StatUnit.Percent:
                    // 백분율: 소수점 첫째 자리 + % 기호
                    // ⚠️ 중요: DynamicEquipmentGenerator는 이미 백분율 단위로 값을 생성함 (2.4 = 2.4%)
                    // 따라서 100을 곱하지 않고 그대로 표시
                    return $"+{value:F1}%";
                
                case StatUnit.Bool:
                    // 불린: "활성화" 또는 "비활성화"
                    return value > 0 ? "활성화" : "비활성화";
                
                default:
                    // 기본: 소수점 첫째 자리
                    return $"+{value:F1}";
            }
        }
        
        #endregion
        
        #region 주옵션 포맷팅
        
        /// <summary>
        /// 주옵션 HTML 태그 적용 (색상: 주황색)
        /// </summary>
        /// <param name="statType">스탯 타입</param>
        /// <param name="value">스탯 값</param>
        /// <returns>HTML 태그가 적용된 주옵션 문자열</returns>
        public static string FormatMainStat(EStatType statType, float value)
        {
            string formattedStat = FormatStat(statType, value, includeLabel: true);
            return $"<color={MainStatColor}>주옵션: {formattedStat}</color>";
        }
        
        #endregion
        
        #region 부옵션 포맷팅
        
        /// <summary>
        /// 부옵션 HTML 태그 적용 (색상: 하늘색)
        /// </summary>
        /// <param name="statType">스탯 타입</param>
        /// <param name="value">스탯 값</param>
        /// <returns>HTML 태그가 적용된 부옵션 문자열</returns>
        public static string FormatSubStat(EStatType statType, float value)
        {
            string formattedStat = FormatStat(statType, value, includeLabel: true);
            return $"<color={SubStatColor}>{formattedStat}</color>";
        }
        
        /// <summary>
        /// 부옵션 목록을 HTML 태그 적용된 문자열로 변환
        /// </summary>
        /// <param name="subStats">부옵션 딕셔너리 (EStatType → float)</param>
        /// <returns>구분선 + 부옵션 목록 (줄바꿈 포함)</returns>
        public static string FormatSubStats(Dictionary<EStatType, float> subStats)
        {
            if (subStats == null || subStats.Count == 0)
                return "";
            
            // StringBuilder 사용 (성능 최적화)
            var sb = new StringBuilder();
            
            // 구분선 추가
            sb.AppendLine($"<color={SeparatorColor}>--- 부옵션 ---</color>");
            
            // 부옵션 순회
            foreach (var kvp in subStats)
            {
                sb.AppendLine(FormatSubStat(kvp.Key, kvp.Value));
            }
            
            // 마지막 줄바꿈 제거
            if (sb.Length > 0)
            {
                sb.Length -= System.Environment.NewLine.Length;
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        #region 전체 스탯 포맷팅 (주옵션 + 부옵션 통합)
        
        /// <summary>
        /// 주옵션 + 부옵션 전체를 하나의 문자열로 포맷팅
        /// </summary>
        /// <param name="mainStatType">주옵션 스탯 타입</param>
        /// <param name="mainStatValue">주옵션 값</param>
        /// <param name="subStats">부옵션 딕셔너리</param>
        /// <returns>주옵션 + 부옵션 전체 (줄바꿈 포함)</returns>
        public static string FormatAllStats(EStatType mainStatType, float mainStatValue, Dictionary<EStatType, float> subStats)
        {
            var sb = new StringBuilder();
            
            // 주옵션 추가
            sb.AppendLine(FormatMainStat(mainStatType, mainStatValue));
            
            // 부옵션이 있으면 추가
            if (subStats != null && subStats.Count > 0)
            {
                sb.AppendLine(); // 빈 줄 추가 (구분용)
                sb.Append(FormatSubStats(subStats));
            }
            
            return sb.ToString();
        }
        
        #endregion
        
        #region 간단한 스탯 표시 (슬롯 미리보기용)
        
        /// <summary>
        /// 간단한 스탯 표시 (슬롯 미리보기용, 색상 없음)
        /// </summary>
        /// <param name="statType">스탯 타입</param>
        /// <param name="value">스탯 값</param>
        /// <returns>간단한 스탯 문자열 (예: "방어력 +52")</returns>
        public static string FormatStatSimple(EStatType statType, float value)
        {
            return FormatStat(statType, value, includeLabel: true);
        }
        
        #endregion
        
        #region 디버깅 및 로깅
        
        /// <summary>
        /// 디버그용 스탯 정보 출력
        /// </summary>
        public static void LogStatInfo(EStatType statType, float value)
        {
            var definition = StatDefinitions.Get(statType);
            if (definition == null)
            {
                Debug.LogWarning($"[StatFormatHelper] StatDefinition을 찾을 수 없습니다: {statType}");
                return;
            }
            
            Debug.Log($"[StatFormatHelper] 스탯 정보:\n" +
                     $"  - 타입: {statType}\n" +
                     $"  - 이름: {definition.displayName}\n" +
                     $"  - 단위: {definition.unit}\n" +
                     $"  - 값: {value}\n" +
                     $"  - 포맷: {FormatStat(statType, value)}");
        }
        
        #endregion
    }
}

