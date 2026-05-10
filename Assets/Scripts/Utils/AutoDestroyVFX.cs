using UnityEngine;

namespace Utils
{
    /// <summary>
    /// Instantiate로 생성된 VFX 프리팹이 파티클 재생 종료 후 자동 삭제되도록 처리
    /// 
    /// 사용 위치: VFX 프리팹 루트 오브젝트에 부착
    /// 대상: EnhancementMessageUI의 SuccessVFXPrefab / FailVFXPrefab
    /// </summary>
    public class AutoDestroyVFX : MonoBehaviour
    {
        [Tooltip("파티클 종료 후 삭제까지 추가 대기 시간 (초)")]
        [SerializeField] private float delayAfterFinish = 0f;

        private ParticleSystem ps;

        private void Awake()
        {
            ps = GetComponentInChildren<ParticleSystem>();

            if (ps == null)
                Destroy(gameObject, 3f);
        }

        private void Update()
        {
            if (ps == null) return;

            if (!ps.IsAlive())
                Destroy(gameObject, delayAfterFinish);
        }
    }
}
