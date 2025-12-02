using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private BalanceIconAnimator balanceIcon;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private Image[] loadingDots; // 3개의 점

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 0.5f;
    [SerializeField] private float tipChangeInterval = 4f;
    [SerializeField] private bool useRealtimeForTips = false; // 일시정지(Time.timeScale=0) 상태에서도 팁을 돌릴지

    private string[] loadingTips = new string[]
    {
        "💡 당신의 선택이 통계를 만듭니다",
        "🎯 정답은 없습니다. 솔직한 선택만 있을 뿐!",
        "🤔 다른 사람들은 어떻게 선택했을까요?",
        "⚖️ 완벽한 밸런스는 존재할까요?",
        "✨ 매일 새로운 주제가 기다립니다"
    };

    private Coroutine animationCoroutine;
    private Coroutine tipCoroutine;
    private bool isShowing = false;

    #region Initialization

    private void Awake()
    {
        // 싱글톤 안전 처리
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("[LoadingScreen] Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }

        // 자동 레퍼런스 보정 (Inspector 비어있을 때 대비)
        AutoAssignIfNull();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    private void AutoAssignIfNull()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? GetComponentInChildren<CanvasGroup>();
        if (progressBarFill == null)
        {
            var imgs = GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.name.Contains("Fill"))
                {
                    progressBarFill = img;
                    break;
                }
            }
        }

        if (loadingText == null) loadingText = GetComponentInChildren<TextMeshProUGUI>();
        if (tipText == null)
        {
            // tipText는 필수는 아니므로 로그만 남김
            tipText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (loadingDots == null || loadingDots.Length == 0)
        {
            // 시도: 이름이 "LoadingDot"인 것 찾아서 배열로 구성
            var dots = GetComponentsInChildren<Image>(true);
            if (dots != null && dots.Length > 0)
            {
                // 임시: dots 중 색이나 이름으로 필터링 가능. 여기선 전체 중 일부만 사용하지 않음.
                loadingDots = dots;
            }
        }
    }

    #endregion

    #region Public Methods

    public void Show()
    {
        if (isShowing) return;

        Debug.Log("[LoadingScreen] Show called");
        AutoAssignIfNull(); // 다시 체크

        gameObject.SetActive(true);
        isShowing = true;

        // 페이드 인
        StopCoroutineIfRunning(ref animationCoroutine);
        StopCoroutineIfRunning(ref tipCoroutine);

        StartCoroutine(FadeIn());

        if (balanceIcon != null)
        {
            balanceIcon.Play();
        }
        else
        {
            Debug.Log("[LoadingScreen] balanceIcon is null");
        }

        // 애니메이션 시작 (점)
        animationCoroutine = StartCoroutine(AnimateLoading());

        // 팁 변경 시작
        tipCoroutine = StartCoroutine(ChangeTipsRoutine());

        Debug.Log("[LoadingScreen] Coroutines started -> animation: " + (animationCoroutine != null) + " tip: " + (tipCoroutine != null));
    }

    public void Hide()
    {
        if (!isShowing) return;

        Debug.Log("[LoadingScreen] Hide called");

        isShowing = false;

        if (balanceIcon != null)
        {
            balanceIcon.Stop();
        }

        StopCoroutineIfRunning(ref animationCoroutine);
        StopCoroutineIfRunning(ref tipCoroutine);

        StartCoroutine(FadeOut());
    }

    public void SetProgress(float progress)
    {
        if (progressBarFill != null)
            progressBarFill.fillAmount = progress;

        if (loadingText != null)
        {
            int pct = Mathf.RoundToInt(progress * 100f);
            loadingText.text = pct >= 100 ? "완료!" : $"로딩 중... {pct}%";
        }
    }


    #endregion

    #region Fade In/Out

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeSpeed)
        {
            if (canvasGroup == null) yield break; // 안전 체크
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeSpeed);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        canvasGroup.blocksRaycasts = false;
        float elapsed = 0f;

        while (elapsed < fadeSpeed)
        {
            if (canvasGroup == null) yield break;
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / fadeSpeed);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    #endregion

    #region Animations

    private IEnumerator AnimateLoading()
    {
        Debug.Log("[LoadingScreen] AnimateLoading started");
        float time = 0f;

        while (isShowing)
        {
            time += Time.deltaTime;

            // 안전: dots가 없으면 그냥 대기
            if (loadingDots != null && loadingDots.Length > 0)
            {
                for (int i = 0; i < loadingDots.Length; i++)
                {
                    if (loadingDots[i] == null) continue;

                    float delay = i * 0.2f;
                    float scale = 0.8f + Mathf.Sin(time * 4f - delay) * 0.4f;
                    scale = Mathf.Clamp(scale, 0.6f, 1.2f);

                    loadingDots[i].transform.localScale = Vector3.one * scale;

                    Color color = loadingDots[i].color;
                    color.a = 0.5f + Mathf.Sin(time * 4f - delay) * 0.5f;
                    loadingDots[i].color = color;
                }
            }

            yield return null;
        }

        Debug.Log("[LoadingScreen] AnimateLoading ended");
    }

    private IEnumerator ChangeTipsRoutine()
    {
        Debug.Log("[LoadingScreen] ChangeTipsRoutine started - tipText null? " + (tipText == null));
        int currentIndex = 0;

        while (isShowing)
        {
            // 안전: tipText가 없으면 단순히 대기만 하되, 루프는 계속 돈다.
            if (tipText != null)
            {
                tipText.text = loadingTips[currentIndex];
                yield return StartCoroutine(FadeTipText(0, 1, 0.5f));
                // WaitForSeconds vs WaitForSecondsRealtime
                if (useRealtimeForTips)
                    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, tipChangeInterval - 1f));
                else
                    yield return new WaitForSeconds(Mathf.Max(0.1f, tipChangeInterval - 1f));

                yield return StartCoroutine(FadeTipText(1, 0, 0.5f));
            }
            else
            {
                // tipText가 없으면 로그 후 그냥 대기
                Debug.Log("[LoadingScreen] tipText is null - skipping tip update");
                if (useRealtimeForTips)
                    yield return new WaitForSecondsRealtime(tipChangeInterval);
                else
                    yield return new WaitForSeconds(tipChangeInterval);
            }

            currentIndex = (currentIndex + 1) % loadingTips.Length;
        }

        Debug.Log("[LoadingScreen] ChangeTipsRoutine ended");
    }

    private IEnumerator FadeTipText(float from, float to, float duration)
    {
        if (tipText == null) yield break;

        float elapsed = 0f;
        Color color = tipText.color;

        while (elapsed < duration)
        {
            if (tipText == null) yield break;
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(from, to, elapsed / duration);
            tipText.color = color;
            yield return null;
        }

        if (tipText != null)
        {
            color.a = to;
            tipText.color = color;
        }
    }

    #endregion

    #region Helpers

    private void StopCoroutineIfRunning(ref Coroutine c)
    {
        if (c != null)
        {
            StopCoroutine(c);
            c = null;
        }
    }

    #endregion
}
