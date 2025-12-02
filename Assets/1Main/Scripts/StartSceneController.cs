using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

/// <summary>
/// 시작 화면 컨트롤러 (UIManager, SceneManager 사용)
/// </summary>
public class StartSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button touchPanel;
    [SerializeField] private TextMeshProUGUI touchToStartText;
    [SerializeField] private CanvasGroup startUI;

    [Header("Settings")]
    [SerializeField] private string mainSceneName = "MainScene";
    [SerializeField] private float textBlinkSpeed = 1f;

    private bool isStarted = false;
    private Coroutine blinkCoroutine;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeUI();
        CheckManagers();
    }

    private void OnDestroy()
    {
        if (touchPanel != null)
        {
            touchPanel.onClick.RemoveListener(OnScreenTouched);
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
    }

    #endregion

    #region Initialization

    private void InitializeUI()
    {
        if (touchPanel != null)
        {
            touchPanel.onClick.AddListener(OnScreenTouched);
        }

        if (touchToStartText != null)
        {
            blinkCoroutine = StartCoroutine(BlinkText());
        }
    }

    private void CheckManagers()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[StartScene] SceneManager가 없습니다!");
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("[StartScene] UIManager가 없습니다!");
        }
    }

    #endregion

    #region Input Handling

    private void OnScreenTouched()
    {
        if (isStarted) return;

        Debug.Log("[StartScene] 터치 감지!");

        if (touchPanel != null)
        {
            touchPanel.interactable = false;
        }

        isStarted = true;

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        StartCoroutine(StartGame());
    }

    #endregion

    #region Game Start

    private IEnumerator StartGame()
    {
        // UI 페이드 아웃
        if (startUI != null)
        {
            yield return StartCoroutine(FadeOutUI());
        }

        // SceneManager를 통해 씬 전환
        if (GameSceneManager.Instance != null)
        {
            GameSceneManager.Instance.LoadMainScene();
        }
        else
        {
            Debug.LogError("[StartScene] SceneManager가 없어 씬 전환 불가!");
        }
    }

    #endregion

    #region UI Animations

    private IEnumerator BlinkText()
    {
        if (touchToStartText == null) yield break;

        while (!isStarted)
        {
            float alpha = 1f;
            while (alpha > 0.3f && !isStarted)
            {
                alpha -= Time.deltaTime * textBlinkSpeed;
                SetTextAlpha(alpha);
                yield return null;
            }

            while (alpha < 1f && !isStarted)
            {
                alpha += Time.deltaTime * textBlinkSpeed;
                SetTextAlpha(alpha);
                yield return null;
            }
        }

        SetTextAlpha(0f);
    }

    private void SetTextAlpha(float alpha)
    {
        if (touchToStartText == null) return;
        Color color = touchToStartText.color;
        color.a = Mathf.Clamp01(alpha);
        touchToStartText.color = color;
    }

    private IEnumerator FadeOutUI()
    {
        if (startUI == null) yield break;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            startUI.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }

        startUI.alpha = 0;
    }

    #endregion
}