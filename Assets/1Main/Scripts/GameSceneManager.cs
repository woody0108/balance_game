using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    [SerializeField] private float minLoadingTime = 3f; // 최소 로딩시간

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void LoadMainScene()
    {
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        // 로딩 시작 시 로딩 UI 표시
        LoadingScreen.Instance.Show();

        AsyncOperation op = SceneManager.LoadSceneAsync("MainScene");
        op.allowSceneActivation = false;

        float timer = 0f;
        float progressBar = 0f;

        while (!op.isDone)
        {
            timer += Time.deltaTime;

            // 실제 씬 로딩 진행도 (0~0.9)
            float actualProgress = Mathf.Clamp01(op.progress / 0.9f);

            // 로딩바는 3초 동안 0 → 1로 선형 증가
            float targetProgress = Mathf.Clamp01(timer / minLoadingTime);

            // 실제 진척도보다 로딩바가 더 빨리 가면 안됨
            progressBar = Mathf.Min(targetProgress, actualProgress);

            LoadingScreen.Instance.SetProgress(progressBar);

            // 둘 다 100% 도달하면 씬 전환
            if (progressBar >= 1f && actualProgress >= 1f)
            {
                op.allowSceneActivation = true;
            }

            yield return null;
        }

        // 씬 전환 후 로딩 UI 닫기
        LoadingScreen.Instance.Hide();
    }
}
