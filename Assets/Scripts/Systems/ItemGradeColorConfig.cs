using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 아이템 등급별 배경 색상 설정
    /// - ScriptableObject로 Inspector에서 색상 자유롭게 설정 가능
    /// - 모든 UI에서 중앙 집중식으로 사용
    /// </summary>
    [CreateAssetMenu(fileName = "ItemGradeColorConfig", menuName = "Config/Item Grade Colors", order = 1)]
    public class ItemGradeColorConfig : ScriptableObject
    {
        /// <summary>
        /// 등급-색상 엔트리
        /// </summary>
        [System.Serializable]
        public class GradeColorEntry
        {
            [Header("등급")]
            public ItemGrade grade;
            
            [Header("배경 색상")]
            public Color color;
            
            public GradeColorEntry(ItemGrade grade, Color color)
            {
                this.grade = grade;
                this.color = color;
            }
        }
        
        [Header("🎨 등급별 색상 설정 (8등급)")]
        [Tooltip("각 등급의 배경 색상을 설정합니다. 모든 아이템 UI에 적용됩니다.")]
        public GradeColorEntry[] gradeColors = new GradeColorEntry[8]
        {
            new GradeColorEntry(ItemGrade.D, new Color(0.5f, 0.5f, 0.5f, 1f)),      // 회색
            new GradeColorEntry(ItemGrade.C, new Color(1f, 1f, 1f, 1f)),            // 흰색
            new GradeColorEntry(ItemGrade.B, new Color(0f, 1f, 0f, 1f)),            // 초록색
            new GradeColorEntry(ItemGrade.A, new Color(0f, 0.4f, 1f, 1f)),          // 파란색
            new GradeColorEntry(ItemGrade.S, new Color(1f, 0.86f, 0f, 1f)),         // 노란색
            new GradeColorEntry(ItemGrade.SS, new Color(1f, 0.5f, 0f, 1f)),         // 주황색
            new GradeColorEntry(ItemGrade.EX, new Color(1f, 0f, 1f, 1f)),           // 보라색
            new GradeColorEntry(ItemGrade.TR, new Color(1f, 0f, 0f, 1f))            // 빨간색
        };
        
        /// <summary>
        /// 등급별 색상 가져오기
        /// </summary>
        public Color GetGradeColor(ItemGrade grade)
        {
            // 배열에서 해당 등급 찾기
            for (int i = 0; i < gradeColors.Length; i++)
            {
                if (gradeColors[i].grade == grade)
                {
                    return gradeColors[i].color;
                }
            }
            
            // 기본값: 흰색
                Debug.LogWarning($"[ItemGradeColorConfig] {grade} 등급 색상 없음 - 기본값(흰색) 반환");
            
            return Color.white;
        }
        
        /// <summary>
        /// 등급 색상 존재 여부 확인
        /// </summary>
        public bool HasGradeColor(ItemGrade grade)
        {
            for (int i = 0; i < gradeColors.Length; i++)
            {
                if (gradeColors[i].grade == grade)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// 모든 등급 색상 검증 (에디터 전용)
        /// </summary>
        private void OnValidate()
        {
            #if UNITY_EDITOR
            // 중복 등급 체크
            for (int i = 0; i < gradeColors.Length; i++)
            {
                for (int j = i + 1; j < gradeColors.Length; j++)
                {
                    if (gradeColors[i].grade == gradeColors[j].grade)
                    {
                        Debug.LogWarning($"[ItemGradeColorConfig] 중복된 등급 발견: {gradeColors[i].grade}");
                    }
                }
            }
            
            // 8개 등급 모두 있는지 확인
            ItemGrade[] allGrades = new ItemGrade[] 
            { 
                ItemGrade.D, ItemGrade.C, ItemGrade.B, ItemGrade.A, 
                ItemGrade.S, ItemGrade.SS, ItemGrade.EX, ItemGrade.TR 
            };
            
            foreach (var grade in allGrades)
            {
                if (!HasGradeColor(grade))
                {
                    Debug.LogWarning($"[ItemGradeColorConfig] {grade} 등급 색상이 설정되지 않았습니다.");
                }
            }
            #endif
        }
    }
}

