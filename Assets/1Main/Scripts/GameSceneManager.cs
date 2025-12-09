using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 통합된 LoadRoutine 구현:
/// - LoadingType에 따라 StartLogin(=앱 시작시 Firebase 대기) 또는 SceneLoading(씬 전환) 처리
/// - 최소 로딩시간(minLoadingTime) / 최대 대기시간(maxLoadingTime) 적용
/// - 성공 시 LoadingScreen.Hide(), 실패 시 실패 메시지 표시(그리고 Hide 호출하지 않음)
/// </summary>
public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [Header("Loading Timers")]
    [SerializeField] private float minLoadingTime = 3f;  // 최소 표시 시간
    [SerializeField] private float maxLoadingTime = 10f; // 최대 대기 시간 (타임아웃)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    /// <summary>
    /// 외부에서 씬 전환 요청
    /// </summary>
    public void LoadMainScene()
    {
        StartCoroutine(LoadRoutine(LoadingType.SceneLoading));
    }

    /// <summary>
    /// StartLogin 전용 시작 (원하면 Start()에서 호출)
    /// </summary>
    public void StartLoginLoading()
    {
        StartCoroutine(LoadRoutine(LoadingType.StartLogin));
    }

    private IEnumerator LoadRoutine(LoadingType type)
    {
        // 공통: 로딩화면 표시 (Show 타입전달으로 텍스트 변경 가능)
        LoadingScreen.Instance.Show(type);

        float timer = 0f;
        float progressBar = 0f;

        AsyncOperation op = null;
        bool isSceneLoadMode = (type == LoadingType.SceneLoading);

        if (isSceneLoadMode)
        {
            op = SceneManager.LoadSceneAsync("MainScene");
            op.allowSceneActivation = false;
        }

        bool timedOut = false;
        bool success = false;

        while (true)
        {
            timer += Time.deltaTime;

            // ========== SceneLoading ==========
            if (isSceneLoadMode)
            {
                float actualProgress = Mathf.Clamp01(op.progress / 0.9f); // op.progress stays <= 0.9 until ready
                float targetProgress = Mathf.Clamp01(timer / minLoadingTime);
                progressBar = Mathf.Min(targetProgress, actualProgress);

                LoadingScreen.Instance.SetProgress(progressBar, type);

                // 성공 조건 (둘 다 완료)
                if (progressBar >= 1f && actualProgress >= 1f)
                {
                    // 씬 활성화
                    op.allowSceneActivation = true;
                    success = true;
                    break;
                }

                // timeout: (예: Firebase가 필요하고 아직 준비 안 됐거나 네트워크 문제)
                if (timer >= maxLoadingTime && !FirebaseManager.Instance.IsReady)
                {
                    timedOut = true;
                    break;
                }
            }
            // ========== StartLogin (Firebase 초기화 대기) ==========
            else
            {
                float targetProgress = Mathf.Clamp01(timer / minLoadingTime);
                progressBar = targetProgress;
                LoadingScreen.Instance.SetProgress(progressBar, type);

                // Firebase 초기화가 되었고 최소 시간 지났으면 성공
                if (FirebaseManager.Instance.IsReady && timer >= minLoadingTime)
                {
                    success = true;
                    StartManager.Instance.touchPanel.interactable = true;
                    StartManager.Instance.touchPanel.onClick.AddListener(StartManager.Instance.OnTouchPanelClicked);
                    break;
                }

                if (timer >= maxLoadingTime)
                {
                    timedOut = true;
                    break;
                }
            }

            yield return null;
        }

        // 결과 처리
        if (success)
        {
            // 성공 -> 로딩 숨김
            LoadingScreen.Instance.Hide();
        }
        else if (timedOut)
        {
            // 실패 -> 실패 메시지 보여주고 Hide는 호출하지 않음(요청사항)
            if (LoadingScreen.Instance != null)
            {
                LoadingScreen.Instance.SetLoadingText("로딩 실패!\n인터넷 환경을 확인해주세요.");
            }
        }

        yield break;
    }
}
