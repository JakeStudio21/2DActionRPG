using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private bool followRotation;
    
    public void Setup(Transform target, Vector3 offset, bool followRotation = false)
    {
        this.target = target;
        this.offset = offset;
        this.followRotation = followRotation;
    }
    
    void LateUpdate()
    {
        if (target != null)
        {
            // 위치 따라가기
            transform.position = target.position + offset;
            
            // 회전 따라가기 (선택사항)
            if (followRotation)
            {
                transform.rotation = target.rotation;
            }
            else
            {
                // 타겟을 바라보기만 함 (카메라 각도 고정)
                transform.LookAt(target);
            }
        }
    }
}

