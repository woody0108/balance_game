using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [SerializeField] private float minDisplayTime = 1.2f;

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
        }
    }

    /// <summary>
    /// 로딩 화면과 함께 씬 로드
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // 1) 로딩 UI 열기
        LoadingScreen.Instance.Show();

        float timer = 0f;

        // 2) 씬 비동기 로드 시작 (활성화는 잠시 보류)
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;

        // -----------------------------------------------
        // 3) 로딩 진행도 업데이트
        // -----------------------------------------------
        while (!async.isDone)
        {
            timer += Time.deltaTime;

            // Unity: progress는 최대 0.9f 까지 도달함
            float progress = Mathf.Clamp01(async.progress / 0.9f);

            // 로딩바 업데이트 (여기가 작동해야 로딩바가 움직임)
            LoadingScreen.Instance.SetProgress(progress);

            // 🔥 씬 준비 완료 + 최소 표시 시간 확보
            if (async.progress >= 0.9f && timer >= minDisplayTime)
            {
                async.allowSceneActivation = true;
            }

            yield return null;
        }

        // 4) 씬 로딩 끝나면 UI 닫기
        LoadingScreen.Instance.Hide();
    }
}
