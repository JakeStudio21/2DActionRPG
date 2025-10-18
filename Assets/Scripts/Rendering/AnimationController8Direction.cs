using UnityEngine;

public class AnimationController8Direction : MonoBehaviour
{
    [Header("Animation Control")]
    public Animator animator;
    public string[] animationStates;
    public float animationSpeed = 1.0f;
    
    [Header("Movement Control")]
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 90f; // 초당 회전 각도
    
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isAnimating = false;
    
    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    public void PlayAnimation(int animationIndex = 0)
    {
        if (animationIndex < animationStates.Length && animator != null)
        {
            animator.speed = animationSpeed;
            animator.Play(animationStates[animationIndex], 0, 0f);
            isAnimating = true;
        }
    }
    
    public void ResetToInitialState()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        isAnimating = false;
        
        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
        }
    }
    
    void Update()
    {
        if (isAnimating)
        {
            // 전진 이동
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            
            // 회전
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }
    }
    
    // Animation Event에서 호출 가능
    public void OnAnimationComplete()
    {
        isAnimating = false;
    }
}