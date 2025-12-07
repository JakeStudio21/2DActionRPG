using UnityEngine;

/// <summary>
/// 스킬 AOE 형태 정의
/// Phase 3: 스킬 범위 공격 시스템
/// </summary>
public enum SkillAOEShape
{
    Circle,      // 원형 (플레이어 중심)
    Rectangle,   // 직사각형 (전방 직선)
    Fan,         // 부채꼴 (전방 부채형)
    Line         // 직선 (관통형)
}

