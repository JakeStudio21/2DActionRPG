using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CueSystem
{
    /// <summary>
    /// 🎯 Cue 재생 시 필요한 컨텍스트 정보
    /// 위치, 방향, 크기, 액터 정보 등을 포함
    /// </summary>
    [System.Serializable]
    public struct CueContext
    {
        [Header("🌍 위치 & 방향")]
        public Vector3 position;
        public Quaternion rotation;        // ✅ 수정: Vector3 → Quaternion
        public Vector3 normal;
        public Vector2 facingDir;          // ✅ 추가: 2D 전용 방향
        
        [Header("🎭 액터 정보")]
        public Transform follow;           // 따라다닐 Transform (optional)
        public ActorType actorType;
        public SurfaceType surfaceType;
        
        [Header("⚡ 이펙트 강도")]
        public float magnitude;            // 이펙트 강도 (0.0 ~ 1.0)
        public bool isCritical;           // 크리티컬 여부
        public float scale;               // 스케일 배율
        
        [Header("🎮 게임플레이")]
        public int damage;                // 데미지 (optional)
        public string customData;         // 커스텀 데이터 (JSON 등)
        
        /// <summary>
        /// 기본 컨텍스트 생성 (위치만 지정)
        /// </summary>
        public static CueContext At(Vector3 pos)
        {
            return new CueContext
            {
                position = pos,
                rotation = Quaternion.identity,
                facingDir = Vector2.right,
                magnitude = 1.0f,
                scale = 1.0f,
                actorType = ActorType.Unknown,
                surfaceType = SurfaceType.Default
            };
        }
        
        /// <summary>
        /// 액터 기반 컨텍스트 생성
        /// </summary>
        public static CueContext From(Transform actor, float magnitude = 1.0f)
        {
            return new CueContext
            {
                position = actor.position,
                rotation = actor.rotation,
                follow = actor,
                facingDir = actor.right,
                magnitude = magnitude,
                scale = 1.0f,
                actorType = ActorType.Unknown,
                surfaceType = SurfaceType.Default
            };
        }
    }
    
    /// <summary>
    /// 액터 타입 (도메인 구분용)
    /// </summary>
    public enum ActorType
    {
        Unknown = 0,
        Player = 1,
        Enemy = 2,
        Environment = 3,
        UI = 4,
        Projectile = 5
    }
    
    /// <summary>
    /// 표면 타입 (히트 이펙트 구분용)
    /// </summary>
    public enum SurfaceType
    {
        Default = 0,
        Metal = 1,
        Wood = 2,
        Stone = 3,
        Flesh = 4,
        Water = 5,
        Magic = 6
    }
}
