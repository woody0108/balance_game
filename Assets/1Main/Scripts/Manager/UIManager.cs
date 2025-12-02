using UnityEngine;

/// <summary>
/// UI 통합 관리 매니저
/// - TopicManager의 이벤트를 구독
/// - UI 컴포넌트들에 데이터 전달
/// - DontDestroyOnLoad 사용 안 함 (씬마다 새로 생성)
/// </summary>
public class UIManager : MonoBehaviour
{
    #region Singleton (씬 종속)
    private static UIManager _instance;
    public static UIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<UIManager>();
            }
            return _instance;
        }
    }
    #endregion

    #region UI Components (Inspector 연결)
    [Header("=== UI Components ===")]
    [SerializeField] private TopicCardUI topicCardUI;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 싱글톤 (DontDestroyOnLoad 없음!)
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        Debug.Log("[UIManager] ✅ 생성 완료 (씬 종속)");
    }

    private void Start()
    {
        // TopicManager 이벤트 구독
        SubscribeEvents();
        
        // TopicCardUI 버튼 이벤트 연결
        SetupButtonListeners();
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        UnsubscribeEvents();

        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Event Subscribe
    /// <summary>
    /// TopicManager 이벤트 구독
    /// </summary>
    private void SubscribeEvents()
    {
        if (TopicManager.Instance != null)
        {
            TopicManager.Instance.OnTopicLoaded += HandleTopicLoaded;
            TopicManager.Instance.OnTopicUpdated += HandleTopicUpdated;
            TopicManager.Instance.OnVoteComplete += HandleVoteComplete;
            TopicManager.Instance.OnError += HandleError;

            Debug.Log("[UIManager] ✅ TopicManager 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning("[UIManager] ⚠️ TopicManager를 찾을 수 없음");
        }
    }

    /// <summary>
    /// 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (TopicManager.Instance != null)
        {
            TopicManager.Instance.OnTopicLoaded -= HandleTopicLoaded;
            TopicManager.Instance.OnTopicUpdated -= HandleTopicUpdated;
            TopicManager.Instance.OnVoteComplete -= HandleVoteComplete;
            TopicManager.Instance.OnError -= HandleError;

            Debug.Log("[UIManager] 이벤트 구독 해제");
        }
    }
    #endregion

    #region Button Setup
    /// <summary>
    /// TopicCardUI 버튼 이벤트 연결
    /// </summary>
    private void SetupButtonListeners()
    {
        if (topicCardUI != null)
        {
            topicCardUI.SetButtonListeners(
                onVoteA: () => OnVoteButtonClicked("A"),
                onVoteB: () => OnVoteButtonClicked("B")
            );

            Debug.Log("[UIManager] ✅ 버튼 리스너 설정 완료");
        }
        else
        {
            Debug.LogWarning("[UIManager] ⚠️ TopicCardUI가 연결되지 않음!");
        }
    }
    #endregion

    #region Event Handlers
    /// <summary>
    /// 주제 로드 완료 → UI 업데이트
    /// </summary>
    private void HandleTopicLoaded(TopicData data)
    {
        Debug.Log($"[UIManager] 📥 주제 로드 이벤트 수신: {data.question}");

        if (topicCardUI != null)
        {
            topicCardUI.UpdateUI(data);
        }
    }

    /// <summary>
    /// 실시간 업데이트 → 결과 바만 업데이트
    /// </summary>
    private void HandleTopicUpdated(TopicData data)
    {
        Debug.Log($"[UIManager] 🔄 실시간 업데이트 이벤트 수신: {data.votesA} vs {data.votesB}");

        if (topicCardUI != null)
        {
            topicCardUI.UpdateResultBar(data, animated: true);
        }
    }

    /// <summary>
    /// 투표 완료 → 즉시 피드백
    /// </summary>
    private void HandleVoteComplete(TopicData data)
    {
        Debug.Log($"[UIManager] ✅ 투표 완료 이벤트 수신");

        // 버튼 잠시 비활성화 (중복 투표 방지)
        if (topicCardUI != null)
        {
            topicCardUI.SetButtonsInteractable(false);
            
            // 1초 후 다시 활성화 (테스트용, 실제로는 사용자별 투표 제한 필요)
            Invoke(nameof(EnableVoteButtons), 1f);
        }
    }

    /// <summary>
    /// 에러 처리
    /// </summary>
    private void HandleError(string errorMessage)
    {
        Debug.LogError($"[UIManager] ❌ 에러: {errorMessage}");
        
        // TODO: 에러 팝업 표시
    }
    #endregion

    #region Button Click Handlers
    /// <summary>
    /// 투표 버튼 클릭
    /// </summary>
    private void OnVoteButtonClicked(string option)
    {
        Debug.Log($"[UIManager] 🗳️ 투표 버튼 클릭: {option}");

        // TopicManager에 투표 요청
        if (TopicManager.Instance != null)
        {
            TopicManager.Instance.Vote(option);
        }
    }

    /// <summary>
    /// 투표 버튼 다시 활성화
    /// </summary>
    private void EnableVoteButtons()
    {
        if (topicCardUI != null)
        {
            topicCardUI.SetButtonsInteractable(true);
            Debug.Log("[UIManager] 투표 버튼 재활성화");
        }
    }
    #endregion

    #region Debug
    [ContextMenu("Refresh UI")]
    public void RefreshUI()
    {
        if (TopicManager.Instance != null && TopicManager.Instance.CurrentTopic != null)
        {
            HandleTopicLoaded(TopicManager.Instance.CurrentTopic);
            Debug.Log("[UIManager] UI 강제 새로고침");
        }
        else
        {
            Debug.LogWarning("[UIManager] 새로고침할 데이터 없음");
        }
    }
    #endregion
}