using UnityEngine;
using UnityEngine.UI;

public class UI_LoadingScreen : MonoBehaviour
{
    private static UI_LoadingScreen instance;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider progressBar;

    private void Awake()
    {
        instance = this;
        Hide();
    }

    public static void Show()
    {
        instance.canvasGroup.alpha = 1;
        instance.canvasGroup.blocksRaycasts = true;
    }

    public static void Hide()
    {
        instance.canvasGroup.alpha = 0;
        instance.canvasGroup.blocksRaycasts = false;
    }

    public static void UpdateProgress(float progress)
    {
        if (instance.progressBar != null)
            instance.progressBar.value = progress;
    }
}