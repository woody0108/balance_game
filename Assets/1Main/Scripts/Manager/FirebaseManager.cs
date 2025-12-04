using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// Firebase Firestore 전담 매니저 (완성본)
/// - 초기화
/// - 문서 읽기
/// - 트랜잭션 (투표)
/// - 실시간 리스너
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    #region Singleton
    private static FirebaseManager _instance;
    public static FirebaseManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<FirebaseManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("FirebaseManager");
                    _instance = go.AddComponent<FirebaseManager>();
                }
            }
            return _instance;
        }
    }
    #endregion

    #region Properties
    private FirebaseFirestore db;
    private bool isInitialized = false;

    public bool IsReady => isInitialized && db != null;
    #endregion

    #region Events
    public event Action OnInitialized;
    public event Action<string> OnError;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 싱글톤 중복 방지
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning("[FirebaseManager] 중복 인스턴스 파괴!");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[FirebaseManager] ✅ 생성 완료 (DontDestroyOnLoad)");
    }

    private async void Start()
    {
        // 자동 초기화
        await InitializeAsync();
    }

    private void OnDestroy()
    {
        Debug.Log("[FirebaseManager] OnDestroy 호출");

        if (_instance == this)
        {
            _instance = null;
        }
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Firebase 초기화 (비동기)
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        if (isInitialized)
        {
            Debug.Log("[FirebaseManager] 이미 초기화됨");
            return true;
        }

        Debug.Log("[FirebaseManager] 🔄 Firebase 초기화 시작...");

        try
        {
            // Firebase 종속성 체크
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firestore 인스턴스 가져오기
                db = FirebaseFirestore.DefaultInstance;
                isInitialized = true;

                Debug.Log("[FirebaseManager] ✅✅✅ Firebase 초기화 성공!");
                Debug.Log($"[FirebaseManager] Firestore DB: {db != null}");

                OnInitialized?.Invoke();
                return true;
            }
            else
            {
                string error = $"Firebase 종속성 오류: {dependencyStatus}";
                Debug.LogError($"[FirebaseManager] ❌ {error}");
                OnError?.Invoke(error);
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] ❌ 초기화 예외: {e.Message}");
            Debug.LogError($"[FirebaseManager] Stack Trace: {e.StackTrace}");
            OnError?.Invoke(e.Message);
            return false;
        }
    }
    #endregion

    #region Document Read
    /// <summary>
    /// 문서 하나 가져오기
    /// 예: GetDocumentAsync("Main", "Main-1")
    /// </summary>
    public async Task<DocumentSnapshot> GetDocumentAsync(string collectionPath, string documentId)
    {
        if (!IsReady)
        {
            Debug.LogError("[FirebaseManager] ❌ 초기화되지 않음! GetDocument 실패");
            return null;
        }

        Debug.Log($"[FirebaseManager] 📥 문서 가져오기 시작: {collectionPath}/{documentId}");

        try
        {
            DocumentReference docRef = db.Collection(collectionPath).Document(documentId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Debug.Log($"[FirebaseManager] ✅ 문서 가져오기 성공!");
                Debug.Log($"[FirebaseManager] Document ID: {snapshot.Id}");

                // 필드 출력 (디버깅용)
                foreach (var field in snapshot.ToDictionary())
                {
                    Debug.Log($"  - {field.Key}: {field.Value}");
                }
            }
            else
            {
                Debug.LogWarning($"[FirebaseManager] ⚠️ 문서가 존재하지 않음: {collectionPath}/{documentId}");
            }

            return snapshot;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] ❌ GetDocument 실패: {e.Message}");
            Debug.LogError($"[FirebaseManager] Stack Trace: {e.StackTrace}");
            return null;
        }
    }
    #endregion

    #region Transaction (투표용)
    /// <summary>
    /// 특정 필드 값 증가 (트랜잭션 사용)
    /// 투표 등 동시성 처리가 필요한 경우 사용
    /// </summary>
    public async Task IncrementFieldAsync(
        string collectionPath,
        string documentId,
        string fieldName,
        int incrementValue = 1)
    {
        if (!IsReady)
        {
            Debug.LogError("[FirebaseManager] ❌ 초기화되지 않음!");
            return;
        }

        Debug.Log($"[FirebaseManager] 🔄 트랜잭션 시작: {fieldName} +{incrementValue}");

        try
        {
            DocumentReference docRef = db.Collection(collectionPath).Document(documentId);

            await db.RunTransactionAsync(transaction =>
            {
                return transaction.GetSnapshotAsync(docRef).ContinueWith(task =>
                {
                    DocumentSnapshot snapshot = task.Result;

                    if (!snapshot.Exists)
                    {
                        Debug.LogWarning($"[FirebaseManager] ⚠️ 문서 없음: {documentId}");
                        return;
                    }

                    // 현재 값 가져오기
                    int currentValue = snapshot.ContainsField(fieldName)
                        ? snapshot.GetValue<int>(fieldName)
                        : 0;

                    // 업데이트
                    var updates = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { fieldName, currentValue + incrementValue }
                    };

                    transaction.Update(docRef, updates);

                    Debug.Log($"[FirebaseManager] ✅ 트랜잭션 완료: {fieldName} = {currentValue + incrementValue}");
                });
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] ❌ 트랜잭션 실패: {e.Message}");
            Debug.LogError($"[FirebaseManager] Stack Trace: {e.StackTrace}");
        }
    }
    #endregion

    #region Real-time Listener
    /// <summary>
    /// 문서 실시간 리스너 등록
    /// </summary>
    public ListenerRegistration ListenToDocument(
        string collectionPath,
        string documentId,
        Action<DocumentSnapshot> onUpdate)
    {
        if (!IsReady)
        {
            Debug.LogError("[FirebaseManager] ❌ 초기화되지 않음! Listener 등록 실패");
            return null;
        }

        Debug.Log($"[FirebaseManager] 👂 실시간 리스너 등록: {collectionPath}/{documentId}");

        try
        {
            DocumentReference docRef = db.Collection(collectionPath).Document(documentId);

            ListenerRegistration listener = docRef.Listen(snapshot =>
            {
                if (snapshot.Exists)
                {
                    Debug.Log($"[FirebaseManager] 🔄 실시간 업데이트 감지");
                    onUpdate?.Invoke(snapshot);
                }
                else
                {
                    Debug.LogWarning($"[FirebaseManager] ⚠️ 문서가 사라짐: {documentId}");
                }
            });

            return listener;
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] ❌ 리스너 등록 실패: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 리스너 중지
    /// </summary>
    public void StopListener(ListenerRegistration listener)
    {
        if (listener != null)
        {
            listener.Stop();
            Debug.Log("[FirebaseManager] 🛑 리스너 중지");
        }
        else
        {
            Debug.LogWarning("[FirebaseManager] ⚠️ 중지할 리스너가 null입니다");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// DocumentSnapshot에서 안전하게 값 가져오기
    /// </summary>
    public T GetValueSafe<T>(DocumentSnapshot snapshot, string fieldName, T defaultValue = default)
    {
        if (snapshot == null)
        {
            Debug.LogWarning($"[FirebaseManager] Snapshot이 null입니다");
            return defaultValue;
        }

        try
        {
            if (snapshot.ContainsField(fieldName))
            {
                T value = snapshot.GetValue<T>(fieldName);
                Debug.Log($"[FirebaseManager] 필드 읽기 성공: {fieldName} = {value}");
                return value;
            }
            else
            {
                Debug.LogWarning($"[FirebaseManager] ⚠️ 필드 없음: {fieldName}, 기본값 사용: {defaultValue}");
                return defaultValue;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FirebaseManager] ❌ 필드 읽기 실패: {fieldName}");
            Debug.LogError($"[FirebaseManager] 오류: {e.Message}");
            return defaultValue;
        }
    }
    #endregion

    #region Debug Menu
    /// <summary>
    /// Inspector에서 우클릭 → Print Firebase Status로 확인 가능
    /// </summary>
    [ContextMenu("Print Firebase Status")]
    public void PrintStatus()
    {
        Debug.Log("==================== Firebase Status ====================");
        Debug.Log($"Initialized: {isInitialized}");
        Debug.Log($"DB is null: {db == null}");
        Debug.Log($"IsReady: {IsReady}");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Scene: {gameObject.scene.name}");
        Debug.Log("========================================================");
    }

    /// <summary>
    /// 테스트용: Main/Main-1 문서 읽기
    /// Inspector에서 컴포넌트 우클릭 → Test: Load Main-1 Document
    /// </summary>
    [ContextMenu("Test: Load Main-1 Document")]
    public async void TestLoadMainDocument()
    {
        Debug.Log("[FirebaseManager] 🧪 테스트 시작: Main/Main-1 로드");

        if (!IsReady)
        {
            Debug.LogError("[FirebaseManager] ❌ Firebase가 초기화되지 않음!");
            return;
        }

        var snapshot = await GetDocumentAsync("Main", "Main-1");

        if (snapshot != null && snapshot.Exists)
        {
            Debug.Log("[FirebaseManager] ✅✅✅ 테스트 성공!");
        }
        else
        {
            Debug.LogError("[FirebaseManager] ❌ 테스트 실패!");
        }
    }
    #endregion
}