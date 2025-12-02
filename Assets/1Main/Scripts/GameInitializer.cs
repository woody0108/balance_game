using UnityEngine;

/// <summary>
/// 게임 초기화
/// StartScene에서 Manager들을 생성
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Manager Prefabs")]
    [SerializeField] private GameObject sceneManagerPrefab;
    [SerializeField] private GameObject uiManagerPrefab;

    private void Awake()
    {
        InitializeManagers();
    }

    /// <summary>
    /// 모든 매니저 초기화
    /// </summary>
    private void InitializeManagers()
    {
        Debug.Log("[GameInitializer] 매니저 초기화 시작");

        // SceneManager 생성 (없으면)
        if (GameSceneManager.Instance == null && sceneManagerPrefab != null)
        {
            Instantiate(sceneManagerPrefab);
            Debug.Log("[GameInitializer] SceneManager 생성");
        }

        // UIManager 생성 (없으면)
        if (UIManager.Instance == null && uiManagerPrefab != null)
        {
            Instantiate(uiManagerPrefab);
            Debug.Log("[GameInitializer] UIManager 생성");
        }

        Debug.Log("[GameInitializer] 매니저 초기화 완료");
    }

    #region Debug

#if UNITY_EDITOR
    [ContextMenu("📋 매니저 상태 확인")]
    private void DebugManagerStatus()
    {
        Debug.Log("===== 매니저 상태 =====");
        Debug.Log($"SceneManager: {(GameSceneManager.Instance != null ? "✅ 활성" : "❌ 없음")}");
        Debug.Log($"UIManager: {(UIManager.Instance != null ? "✅ 활성" : "❌ 없음")}");
        Debug.Log("=====================");
    }
#endif

    #endregion
}