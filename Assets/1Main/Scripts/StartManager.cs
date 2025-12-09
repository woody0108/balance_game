using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Threading.Tasks;

/// <summary>
/// StartScene UI 관리자
/// - StartCanvas UI 관리
/// - LoadingCanvas를 UIObjectPoolingManager에서 가져옴
/// - MainScene으로 넘어갈 때 LoadingCanvas를 DontDestroyOnLoad로 유지
/// </summary>
public class StartManager : MonoBehaviour
{
    public static StartManager Instance { get; private set; }

    [Header("StartScene UI")]
    [SerializeField] public Button touchPanel;
    [SerializeField] private TextMeshProUGUI touchToStartText;
    [SerializeField] private CanvasGroup startCanvasGroup;

    [SerializeField] private float textBlinkSpeed = 1f;
    private bool isStarted = false;
    private Coroutine blinkCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        touchPanel.interactable = false;
    }

    private async Task Start()
    {
        await FirebaseManager.Instance.InitializeAsync();

        if (FirebaseManager.Instance.IsReady)
        {
            TopicManager.Instance.LoadTopicAsync();
        }
        else
        {
            FirebaseManager.Instance.OnInitialized += TopicManager.Instance.LoadTopicAsync;
        }


        blinkCoroutine = StartCoroutine(BlinkText());
    }

    public void OnTouchPanelClicked()
    {
        if (isStarted) return;
        isStarted = true;

        touchPanel.interactable = false;

        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);

        StartCoroutine(StartGame());
    }


    private IEnumerator StartGame()
    {
        yield return StartCoroutine(FadeOutStartCanvas());

        // 씬 로딩 요청
        GameSceneManager.Instance.LoadMainScene();
    }

    private IEnumerator BlinkText()
    {
        float alpha = 1f;

        while (!isStarted)
        {
            alpha = Mathf.PingPong(Time.time * textBlinkSpeed, 1f);
            var c = touchToStartText.color;
            c.a = alpha;
            touchToStartText.color = c;
            yield return null;
        }
    }

    private IEnumerator FadeOutStartCanvas()
    {
        float t = 0;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            startCanvasGroup.alpha = Mathf.Lerp(1, 0, t / 0.3f);
            yield return null;
        }
        startCanvasGroup.alpha = 0;
    }
}
