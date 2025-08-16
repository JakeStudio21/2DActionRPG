using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace StageSystem
{
    /// <summary>
    /// CSV 데이터 로더 베이스 클래스
    /// 제공된 CSV 파일들을 파싱하여 검증하는 유틸리티
    /// </summary>
    public static class CsvDataLoader
    {
        /// <summary>
        /// CSV 파일에서 특정 컬럼 데이터 추출
        /// </summary>
        public static string[] ExtractColumnData(string csvContent, string columnName)
        {
            if (string.IsNullOrEmpty(csvContent)) return new string[0];
            
            string[] lines = csvContent.Split('\n');
            if (lines.Length < 2) return new string[0];
            
            // 헤더에서 컬럼 인덱스 찾기
            string[] headers = lines[0].Split(',');
            int columnIndex = -1;
            
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i].Trim() == columnName)
                {
                    columnIndex = i;
                    break;
                }
            }
            
            if (columnIndex == -1)
            {
                Debug.LogWarning($"[CsvDataLoader] 컬럼 '{columnName}'을 찾을 수 없습니다.");
                return new string[0];
            }
            
            // 데이터 추출 (헤더와 타입 라인 제외)
            List<string> columnData = new List<string>();
            
            for (int i = 2; i < lines.Length; i++) // 2번째 줄부터 (0=헤더, 1=타입)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                string[] values = lines[i].Split(',');
                if (values.Length > columnIndex)
                {
                    string value = values[columnIndex].Trim();
                    if (!string.IsNullOrEmpty(value))
                    {
                        columnData.Add(value);
                    }
                }
            }
            
            return columnData.ToArray();
        }
        
        /// <summary>
        /// 제공된 CSV 파일들의 ID 무결성 검증
        /// </summary>
        public static void ValidateProvidedCsvFiles()
        {
            Debug.Log("🔍 [CsvDataLoader] 제공된 CSV 파일 검증 시작...");
            
            // 실제 검증은 티켓 1에서 구현
            // 현재는 구조만 준비
            
            Debug.Log("✅ [CsvDataLoader] CSV 검증 구조 준비 완료 (티켓 1에서 실제 구현)");
        }
        
        /// <summary>
        /// CSV 라인을 파싱하여 Dictionary로 변환
        /// </summary>
        public static Dictionary<string, string> ParseCsvLine(string[] headers, string line)
        {
            var result = new Dictionary<string, string>();
            string[] values = line.Split(',');
            
            for (int i = 0; i < headers.Length && i < values.Length; i++)
            {
                result[headers[i].Trim()] = values[i].Trim();
            }
            
            return result;
        }
    }
}
