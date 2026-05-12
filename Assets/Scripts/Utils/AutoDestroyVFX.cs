using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Instantiate로 생성된 VFX 프리팹이 파티클 재생 종료 후 자동 삭제되도록 처리
    /// 
    /// 사용 위치: VFX 프리팹 루트 오브젝트에 부착
    /// 
    /// IsAlive() 대신 시간 기반 삭제를 사용하는 이유:
    /// UIParticleSystem은 LateUpdate에서 Simulate()를 매 프레임 호출하며,
    /// 파티클이 종료된 stopped 상태에서 Simulate() 호출 시 새 시뮬레이션이 시작되어
    /// IsAlive()가 항상 true를 반환하는 호환성 문제가 있음
    /// </summary>
    public class AutoDestroyVFX : MonoBehaviour
    {
        [Tooltip("파티클 종료 후 삭제까지 추가 대기 시간 (초)")]
        [SerializeField] private float delayAfterFinish = 0f;

        private void Awake()
        {
            var ps = GetComponentInChildren<ParticleSystem>();

            if (ps == null)
            {
                Destroy(gameObject, 3f);
                return;
            }

            // duration: 파티클 방출 시간
            // startLifetime: 방출된 파티클이 살아있는 시간
            // 둘을 합산해야 마지막 파티클까지 완전히 사라지는 시점이 됨
            var main = ps.main;
            float totalLifetime = main.duration + main.startLifetime.constantMax + delayAfterFinish;
            Destroy(gameObject, totalLifetime);
        }
    }
}
