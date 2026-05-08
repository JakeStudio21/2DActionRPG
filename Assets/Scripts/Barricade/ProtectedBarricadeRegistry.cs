using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 내 isProtectTarget=true Barricade 목록을 관리하는 싱글턴
/// Barricade가 Start()/OnDisable()에서 자동 등록/해제하며,
/// SimpleMob이 공격 목표를 쿼리할 때 사용합니다.
/// </summary>
public class ProtectedBarricadeRegistry : MonoBehaviour
{
    public static ProtectedBarricadeRegistry Instance { get; private set; }

    private readonly List<Barricade> _protectTargets = new List<Barricade>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ====================================================
    // 등록 / 해제
    // ====================================================

    public void Register(Barricade barricade)
    {
        if (barricade != null && !_protectTargets.Contains(barricade))
            _protectTargets.Add(barricade);
    }

    public void Unregister(Barricade barricade)
    {
        _protectTargets.Remove(barricade);
    }

    // ====================================================
    // 쿼리
    // ====================================================

    /// <summary>
    /// 살아있는 보호 오브젝트 중 from 위치와 가장 가까운 것을 반환합니다.
    /// 거리 제한 없음. 없으면 null을 반환합니다.
    /// </summary>
    public Barricade GetNearest(Vector3 from)
    {
        Barricade nearest = null;
        float minDist = float.MaxValue;

        foreach (var b in _protectTargets)
        {
            if (b == null || b.IsBroken) continue;

            float dist = Vector2.Distance(from, b.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = b;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 살아있는 보호 오브젝트 수 (IsBroken=false 기준)
    /// </summary>
    public int AliveCount
    {
        get
        {
            int count = 0;
            foreach (var b in _protectTargets)
            {
                if (b != null && !b.IsBroken)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// 살아있는 보호 오브젝트가 하나라도 있는지 여부
    /// </summary>
    public bool HasAnyAlive() => AliveCount > 0;
}
