using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 밸런스 저울 아이콘 애니메이터
/// 독립적으로 사용 가능한 프리팹 컴포넌트
/// </summary>
public class BalanceIconAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform leftCircle;
    [SerializeField] private RectTransform rightCircle;

    [Header("Animation Settings")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float animationSpeed = 2f; // 애니메이션 속도
    [SerializeField] private float moveDistance = 10f; // 위아래 이동 거리
    [SerializeField] private AnimationType animationType = AnimationType.Opposite; // 애니메이션 타입

    private bool isPlaying = false;
    private float animationTime = 0f;
    private Vector2 leftCircleOriginalPos;
    private Vector2 rightCircleOriginalPos;

    public enum AnimationType
    {
        Opposite,   // 반대 방향 (하나 올라가면 하나 내려감)
        Same,       // 같은 방향
        LeftOnly,   // 왼쪽만
        RightOnly   // 오른쪽만
    }

    #region Unity Lifecycle

    private void Awake()
    {
        // 초기 위치 저장
        if (leftCircle != null)
        {
            leftCircleOriginalPos = leftCircle.anchoredPosition;
        }

        if (rightCircle != null)
        {
            rightCircleOriginalPos = rightCircle.anchoredPosition;
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (isPlaying)
        {
            AnimateBalance();
        }
    }

    #endregion

    #region Animation Control

    /// <summary>
    /// 애니메이션 시작
    /// </summary>
    public void Play()
    {
        isPlaying = true;
        Debug.Log("[BalanceIcon] 애니메이션 시작");
    }

    /// <summary>
    /// 애니메이션 정지
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        ResetPosition();
        Debug.Log("[BalanceIcon] 애니메이션 정지");
    }

    /// <summary>
    /// 애니메이션 일시정지
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// 애니메이션 재개
    /// </summary>
    public void Resume()
    {
        isPlaying = true;
    }

    /// <summary>
    /// 원래 위치로 복귀
    /// </summary>
    public void ResetPosition()
    {
        if (leftCircle != null)
        {
            leftCircle.anchoredPosition = leftCircleOriginalPos;
        }

        if (rightCircle != null)
        {
            rightCircle.anchoredPosition = rightCircleOriginalPos;
        }

        animationTime = 0f;
    }

    #endregion

    #region Animation Logic

    /// <summary>
    /// 저울 애니메이션 로직
    /// </summary>
    private void AnimateBalance()
    {
        animationTime += Time.deltaTime * animationSpeed;

        float leftY = 0f;
        float rightY = 0f;

        switch (animationType)
        {
            case AnimationType.Opposite:
                // 반대 방향 (기본)
                leftY = Mathf.Sin(animationTime) * moveDistance;
                rightY = Mathf.Sin(animationTime + Mathf.PI) * moveDistance;
                break;

            case AnimationType.Same:
                // 같은 방향
                leftY = Mathf.Sin(animationTime) * moveDistance;
                rightY = Mathf.Sin(animationTime) * moveDistance;
                break;

            case AnimationType.LeftOnly:
                // 왼쪽만
                leftY = Mathf.Sin(animationTime) * moveDistance;
                rightY = 0f;
                break;

            case AnimationType.RightOnly:
                // 오른쪽만
                leftY = 0f;
                rightY = Mathf.Sin(animationTime) * moveDistance;
                break;
        }

        // 위치 적용
        if (leftCircle != null)
        {
            Vector2 newPos = leftCircleOriginalPos;
            newPos.y += leftY;
            leftCircle.anchoredPosition = newPos;
        }

        if (rightCircle != null)
        {
            Vector2 newPos = rightCircleOriginalPos;
            newPos.y += rightY;
            rightCircle.anchoredPosition = newPos;
        }
    }

    #endregion

    #region Public Setters

    /// <summary>
    /// 애니메이션 속도 변경
    /// </summary>
    public void SetSpeed(float speed)
    {
        animationSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// 이동 거리 변경
    /// </summary>
    public void SetMoveDistance(float distance)
    {
        moveDistance = Mathf.Max(0f, distance);
    }

    /// <summary>
    /// 애니메이션 타입 변경
    /// </summary>
    public void SetAnimationType(AnimationType type)
    {
        animationType = type;
    }

    #endregion

    #region Validation

    private void OnValidate()
    {
        // Inspector 값 변경 시 검증
        animationSpeed = Mathf.Max(0f, animationSpeed);
        moveDistance = Mathf.Max(0f, moveDistance);
    }

    #endregion

    #region Debug Helpers

#if UNITY_EDITOR
    [ContextMenu("▶️ Play Animation")]
    private void DebugPlay()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play 모드에서만 작동합니다!");
            return;
        }
        Play();
    }

    [ContextMenu("⏸️ Stop Animation")]
    private void DebugStop()
    {
        if (!Application.isPlaying) return;
        Stop();
    }

    [ContextMenu("🔄 Reset Position")]
    private void DebugReset()
    {
        if (!Application.isPlaying) return;
        ResetPosition();
    }

    [ContextMenu("📋 Check Setup")]
    private void DebugCheckSetup()
    {
        Debug.Log("===== BalanceIcon 설정 확인 =====");
        Debug.Log($"Left Circle: {(leftCircle != null ? "✅" : "❌ 필요!")}");
        Debug.Log($"Right Circle: {(rightCircle != null ? "✅" : "❌ 필요!")}");
        Debug.Log($"Play On Start: {playOnStart}");
        Debug.Log($"Animation Speed: {animationSpeed}");
        Debug.Log($"Move Distance: {moveDistance}");
        Debug.Log($"Animation Type: {animationType}");
        Debug.Log("================================");
    }
#endif

    #endregion
}