using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 기존 LoadingScreen에 최소 변경을 적용:
/// - Show(LoadingType) 추가 (로딩 텍스트 자동 설정)
/// - SetProgress(progress, type) 오버로드 추가 (type은 현재 표시용)
/// - 기존 Show()/Hide()/SetProgress(float)와 호환 유지
/// </summary>
public enum LoadingType
{
    StartLogin,    // 앱 시작시 Firebase 초기화 대기
    SceneLoading   // 씬 전환용 로딩
}

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
    [SerializeField] private bool useRealtimeForTips = false;

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

    // 외부에서 빠르게 접근 가능하도록 public으로(원하면 프로퍼티로 변경)
    public TextMeshProUGUI LoadingText => loadingText;
    public TextMeshProUGUI TipText => tipText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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

        // loadingText는 첫번째 TMP로 잡지만 inspector에서 명확히 지정하는 것을 권장
        if (loadingText == null) loadingText = GetComponentInChildren<TextMeshProUGUI>();
        if (tipText == null) tipText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (loadingDots == null || loadingDots.Length == 0)
        {
            var dots = GetComponentsInChildren<Image>(true);
            if (dots != null && dots.Length > 0)
            {
                loadingDots = dots;
            }
        }
    }

    #region Show/Hide / Text API

    // 기존 Show 유지 (기본 동작)
    public void Show()
    {
        ShowInternal();
    }

    // 새로운 Show(LoadingType) — 텍스트를 타입에 맞게 변경 후 표시
    public void Show(LoadingType type)
    {
        // 타입에 따른 기본 메시지 설정
        if (loadingText != null)
        {
            switch (type)
            {
                case LoadingType.StartLogin:
                    loadingText.text = "로그인중...";
                    break;
                case LoadingType.SceneLoading:
                    loadingText.text = "로딩 중...";
                    break;
                default:
                    loadingText.text = "로딩 중...";
                    break;
            }
        }

        ShowInternal();
    }

    private void ShowInternal()
    {
        if (isShowing) return;

        AutoAssignIfNull();

        gameObject.SetActive(true);
        isShowing = true;

        StopCoroutineIfRunning(ref animationCoroutine);
        StopCoroutineIfRunning(ref tipCoroutine);

        StartCoroutine(FadeIn());

        if (balanceIcon != null) balanceIcon.Play();

        animationCoroutine = StartCoroutine(AnimateLoading());
        tipCoroutine = StartCoroutine(ChangeTipsRoutine());
    }

    public void Hide()
    {
        if (!isShowing) return;

        isShowing = false;

        if (balanceIcon != null) balanceIcon.Stop();

        StopCoroutineIfRunning(ref animationCoroutine);
        StopCoroutineIfRunning(ref tipCoroutine);

        StartCoroutine(FadeOut());
    }

    // 기존 SetProgress 유지
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

    // 오버로드: type 전달받아 필요하면 다른 문구를 보여줄 수 있게 함 (현재 동일 동작)
    public void SetProgress(float progress, LoadingType type)
    {
        // 예: SceneLoading에서는 퍼센트, StartLogin에서는 퍼센트 + 텍스트 유지
        SetProgress(progress);
    }

    // 직접 텍스트 설정용 유틸
    public void SetLoadingText(string text)
    {
        if (loadingText != null) loadingText.text = text;
    }

    #endregion

    #region Fade / Animations

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeSpeed)
        {
            if (canvasGroup == null) yield break;
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / fadeSpeed);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

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

    private IEnumerator AnimateLoading()
    {
        float time = 0f;

        while (isShowing)
        {
            time += Time.deltaTime;

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
    }

    private IEnumerator ChangeTipsRoutine()
    {
        int currentIndex = 0;

        while (isShowing)
        {
            if (tipText != null && loadingTips.Length > 0)
            {
                tipText.text = loadingTips[currentIndex];
                yield return StartCoroutine(FadeTipText(0, 1, 0.5f));

                if (useRealtimeForTips)
                    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, tipChangeInterval - 1f));
                else
                    yield return new WaitForSeconds(Mathf.Max(0.1f, tipChangeInterval - 1f));

                yield return StartCoroutine(FadeTipText(1, 0, 0.5f));
            }
            else
            {
                // tipText가 없으면 대기만 함
                if (useRealtimeForTips)
                    yield return new WaitForSecondsRealtime(tipChangeInterval);
                else
                    yield return new WaitForSeconds(tipChangeInterval);
            }

            // 기본은 순환. 원하면 random으로 바꾸려면 여기서 변경 가능.
            currentIndex = (currentIndex + 1) % loadingTips.Length;
        }
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
