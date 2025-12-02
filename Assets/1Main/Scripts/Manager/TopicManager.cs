using System;
using UnityEngine;
using Firebase.Firestore;
using TMPro;

/// <summary>
/// 주제 데이터 관리 매니저
/// - FirebaseManager를 사용해 데이터 로드
/// - UI에 이벤트로 알림
/// - 투표 로직 처리
/// </summary>
public class TopicManager : MonoBehaviour
{
    #region Singleton
    [SerializeField] private TextMeshProUGUI debugLog;
    private static TopicManager _instance;
    public static TopicManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<TopicManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TopicManager");
                    _instance = go.AddComponent<TopicManager>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Firebase Settings
    [Header("=== Firebase Path ===")]
    [SerializeField] private string collectionPath = "Main";
    [SerializeField] private string documentId = "Main-1";
    #endregion

    #region Current Data
    public TopicData CurrentTopic { get; private set; }
    #endregion

    #region Real-time Listener
    private ListenerRegistration currentListener;
    #endregion

    #region Events
    /// <summary>주제가 처음 로드됨</summary>
    public event Action<TopicData> OnTopicLoaded;

    /// <summary>실시간 업데이트 (투표 결과 변경)</summary>
    public event Action<TopicData> OnTopicUpdated;

    /// <summary>투표 완료</summary>
    public event Action<TopicData> OnVoteComplete;

    /// <summary>에러 발생</summary>
    public event Action<string> OnError;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Log("✅ TopicManager 생성 완료 (DontDestroyOnLoad)");
    }

    private void Start()
    {
        Log("TOPICMANAGER START ");
        // Firebase 초기화 대기
        if (FirebaseManager.Instance.IsReady)
        {
            LoadTopicAsync();
        }
        else
        {
            Log("FirebaseManagerNotReady");
            FirebaseManager.Instance.OnInitialized += LoadTopicAsync;
        }
    }

    private void OnDestroy()
    {
        StopListening();

        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Load Topic
    /// <summary>
    /// 주제 로드
    /// </summary>
    public async void LoadTopicAsync()
    {
        Log($"[TopicManager] 📥 주제 로딩 시작: {collectionPath}/{documentId}");

        try
        {
            // Firebase에서 문서 가져오기
            DocumentSnapshot snapshot = await FirebaseManager.Instance.GetDocumentAsync(
                collectionPath,
                documentId
            );

            if (snapshot == null || !snapshot.Exists)
            {
                string error = "주제 문서를 찾을 수 없습니다";
                Debug.LogError($"[TopicManager] ❌ {error}");
                debugLog.text += error;
                OnError?.Invoke(error);
                return;
            }

            // DocumentSnapshot을 TopicData로 변환
            CurrentTopic = ParseTopicData(snapshot);

            Debug.Log($"[TopicManager] ✅ 주제 로드 성공!");
            Debug.Log($"  질문: {CurrentTopic.question}");
            Debug.Log($"  옵션A: {CurrentTopic.optionA} ({CurrentTopic.votesA}표)");
            Debug.Log($"  옵션B: {CurrentTopic.optionB} ({CurrentTopic.votesB}표)");

            debugLog.text += $"[TopicManager] ✅ 주제 로드 성공!";
            debugLog.text += $"  질문: {CurrentTopic.question}";
            debugLog.text += $"  옵션A: {CurrentTopic.optionA} ({CurrentTopic.votesA}표)";
            debugLog.text += $"  옵션B: {CurrentTopic.optionB} ({CurrentTopic.votesB}표)";


            // 이벤트 발생 → UIManager가 받음
            OnTopicLoaded?.Invoke(CurrentTopic);

            // 실시간 리스너 시작
            StartListening();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TopicManager] ❌ 로드 실패: {e.Message}");
            OnError?.Invoke($"주제 로드 실패: {e.Message}");
            debugLog.text += e.Message;
        }
    }
    #endregion

    #region Parse Data
    /// <summary>
    /// Firestore DocumentSnapshot → TopicData 변환
    /// </summary>
    private TopicData ParseTopicData(DocumentSnapshot snapshot)
    {
        var data = new TopicData
        {
            topicId = snapshot.Id,
            question = FirebaseManager.Instance.GetValueSafe<string>(snapshot, "question", "질문 없음"),
            optionA = FirebaseManager.Instance.GetValueSafe<string>(snapshot, "optionA", "선택A"),
            optionB = FirebaseManager.Instance.GetValueSafe<string>(snapshot, "optionB", "선택B"),
            votesA = FirebaseManager.Instance.GetValueSafe<int>(snapshot, "votesA", 0),
            votesB = FirebaseManager.Instance.GetValueSafe<int>(snapshot, "votesB", 0)
        };

        data.CalculateTotalVotes();

        return data;
    }
    #endregion

    #region Vote
    /// <summary>
    /// 투표 처리
    /// </summary>
    public async void Vote(string option)
    {
        if (CurrentTopic == null)
        {
            Debug.LogWarning("[TopicManager] ⚠️ 주제가 로드되지 않음");
            Log("[TopicManager] ⚠️ 주제가 로드되지 않음");
            OnError?.Invoke("주제가 로드되지 않았습니다");
            return;
        }

        if (option != "A" && option != "B")
        {
            Debug.LogError($"[TopicManager] ❌ 잘못된 옵션: {option}");
            OnError?.Invoke("잘못된 선택입니다");
            return;
        }

        Debug.Log($"[TopicManager] 🗳️ 투표 시작: 옵션 {option}");

        try
        {
            // Firebase에 투표 반영 (기존 Poll.cs의 Vote 로직)
            string voteField = option == "A" ? "votesA" : "votesB";

            await FirebaseManager.Instance.IncrementFieldAsync(
                collectionPath,
                documentId,
                voteField,
                1
            );

            Debug.Log($"[TopicManager] ✅ 투표 완료: {option}");
            Log($"[TopicManager] ✅ 투표 완료: {option}");

            // 투표 완료 이벤트 (리스너가 자동 업데이트하지만 즉시 피드백용)
            OnVoteComplete?.Invoke(CurrentTopic);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TopicManager] ❌ 투표 실패: {e.Message}");
            OnError?.Invoke($"투표 실패: {e.Message}");
        }
    }
    #endregion

    #region Real-time Listener
    /// <summary>
    /// 실시간 리스너 시작
    /// </summary>
    private void StartListening()
    {
        StopListening(); // 기존 리스너 정리

        Debug.Log("[TopicManager] 👂 실시간 리스너 시작");
        Log("[TopicManager] 👂 실시간 리스너 시작");

        currentListener = FirebaseManager.Instance.ListenToDocument(
            collectionPath,
            documentId,
            OnDocumentUpdated
        );
    }

    /// <summary>
    /// 문서 업데이트 콜백
    /// </summary>
    private void OnDocumentUpdated(DocumentSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.Exists)
        {
            Debug.LogWarning("[TopicManager] ⚠️ 문서가 사라짐");
            return;
        }

        // 데이터 업데이트
        CurrentTopic = ParseTopicData(snapshot);

        Debug.Log($"[TopicManager] 🔄 실시간 업데이트: {CurrentTopic.votesA} vs {CurrentTopic.votesB}");
        Log($"[TopicManager] 🔄 실시간 업데이트: {CurrentTopic.votesA} vs {CurrentTopic.votesB}");

        // UI 업데이트 이벤트
        OnTopicUpdated?.Invoke(CurrentTopic);
    }

    /// <summary>
    /// 리스너 중지
    /// </summary>
    private void StopListening()
    {
        if (currentListener != null)
        {
            FirebaseManager.Instance.StopListener(currentListener);
            currentListener = null;
            Debug.Log("[TopicManager] 🛑 리스너 중지");
            Log("[TopicManager] 🛑 리스너 중지");
        }
    }
    #endregion

    public static void Log(String msg)
    {
        if (Instance.debugLog != null)
        {
            Instance.debugLog.text += "\n" + msg;
        }

        // 콘솔에도 출력 (모바일 디버깅용)
        Debug.Log($"[TopicManager] {msg}");
    }


    #region Debug
    [ContextMenu("Print Current Topic")]
    public void PrintCurrentTopic()
    {
        if (CurrentTopic == null)
        {
            Debug.Log("[TopicManager] 주제 없음");
            return;
        }

        Debug.Log("==================== Current Topic ====================");
        Debug.Log($"질문: {CurrentTopic.question}");
        Debug.Log($"옵션A: {CurrentTopic.optionA} ({CurrentTopic.PercentageA:F1}%)");
        Debug.Log($"옵션B: {CurrentTopic.optionB} ({CurrentTopic.PercentageB:F1}%)");
        Debug.Log($"총 투표: {CurrentTopic.totalVotes}");
        Debug.Log("======================================================");
    }
    #endregion
}