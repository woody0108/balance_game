using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class IntroLogo : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public float showTime = 1.5f;   // 로고 보여주는 시간
    public float fadeTime = 0.6f;   // 페이드아웃 시간

    private void Start()
    {
        StartCoroutine(LogoRoutine());
    }

    public IEnumerator LogoRoutine()
    {
        canvasGroup.alpha = 1f;

        // 1) 로고 유지 시간
        yield return new WaitForSeconds(showTime);

        // 2) 페이드 아웃
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // 3) 로고 제거
        gameObject.SetActive(false);

        // 4) 기존 startloading 실행
        GameSceneManager.Instance.StartLoginLoading();
    }
}
