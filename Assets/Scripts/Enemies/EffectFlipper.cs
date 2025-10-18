using UnityEngine;

public class EffectFlipper : MonoBehaviour
{
    // dir.x가 음수면 (왼쪽이면) X스케일 반전
    public void SetDirection(Vector2 dir)
    {
        Vector3 scale = transform.localScale;
        scale.x = (dir.x < 0) ? Mathf.Abs(scale.x) * -1 : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}
