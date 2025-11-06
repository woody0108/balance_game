using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using TMPro;
using System.Threading.Tasks;
using Firebase.Extensions;


#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>
/// GPGS 20.x + Firebase 통합 로그인 (최적화 버전)
/// - GPGS 20.x 신규 API 사용
/// - 에러 핸들링 강화
/// - async/await 패턴 개선
/// </summary>
public class SimpleGoogleLogin : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statusText;

    [Header("Scene Settings")]
    [SerializeField] private string mainSceneName = "MainScene";

    private FirebaseAuth auth;
    private bool isLoggingIn = false;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeServices();
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지
        auth = null;
    }

    #endregion

    #region Initialization

    private async void InitializeServices()
    {
        UpdateStatus("초기화 중...");

        // Firebase 초기화
        await InitializeFirebase();

        // GPGS 초기화 (20.x 버전은 자동으로 설정됨)
        InitializeGPGS();

        // 자동 로그인 체크
        CheckAutoLogin();
    }

    private async Task InitializeFirebase()
    {
        try
        {
            // Firebase 의존성 체크
            var dependencyStatus = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                UpdateStatus("Firebase 준비 완료");
            }
            else
            {
                UpdateStatus($"Firebase 초기화 실패: {dependencyStatus}");
            }
        }
        catch (System.Exception e)
        {
            UpdateStatus($"Firebase 오류: {e.Message}");
        }
    }

    private void InitializeGPGS()
    {
#if UNITY_ANDROID
        // GPGS 20.x는 Activate()만 호출하면 됨
        // Android Resolver가 자동으로 설정 처리
        PlayGamesPlatform.Activate();
        string authCode;
        PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
        {
            authCode = code;
            auth = FirebaseAuth.DefaultInstance;
            Credential credential = PlayGamesAuthProvider.GetCredential(authCode);

            auth.SignInAndRetrieveDataWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    UpdateStatus("성공");
                }
            });
        });

        UpdateStatus("Google Play Games 준비 완료");
#else
        UpdateStatus("Android 전용 기능입니다");
#endif
    }

    private void CheckAutoLogin()
    {
        if (auth != null && auth.CurrentUser != null)
        {
            UpdateStatus($"자동 로그인됨\n{auth.CurrentUser.DisplayName ?? "사용자"}");
            statusText.text = $"환영합니다!\n{auth.CurrentUser.DisplayName}";
        }
        else
        {
            UpdateStatus("Google Play Games로 로그인");
        }
    }

    #endregion

    #region Login Flow

    public void OnClickLogin()
    {


        if (isLoggingIn)
        {
            UpdateStatus("로그인 진행 중입니다...");
            return;
        }

        // 이미 로그인되어 있으면 바로 씬 이동
        if (auth != null && auth.CurrentUser != null)
        {
            LoadMainScene();
            return;
        }

        //StartGPGSLogin();
    }

    private void StartGPGSLogin()
    {
#if UNITY_ANDROID
        isLoggingIn = true;
        UpdateStatus("Google Play Games 인증 중...");

        // GPGS 20.x: ManuallyAuthenticate 사용
        PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessGPGSAuthentication);
#else
        UpdateStatus("Android 기기에서만 작동합니다");
#endif
    }

    private void ProcessGPGSAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
#if UNITY_ANDROID
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            string userID = PlayGamesPlatform.Instance.GetUserId();

            UpdateStatus($"GPGS 로그인 성공\n{displayName}");
            // ✅ GPGS 20.x: RequestServerSideAccess 사용
            // 첫 번째 파라미터 false = Auth Code 요청 (ID Token 아님)
            //PlayGamesPlatform.Instance.RequestServerSideAccess(false, SignInFirebase);
#endif
        }
        else
        {
            isLoggingIn = false;

            string errorMsg = status switch
            {
                SignInStatus.Canceled => "로그인이 취소되었습니다",
                SignInStatus.InternalError => "내부 오류가 발생했습니다\n앱을 재시작해주세요",
                _ => $"로그인 실패: {status}"
            };

            UpdateStatus(errorMsg);
        }
    }


    /*private async void SignInFirebase(string authCode)
    {
        try
        {
            // Firebase Credential 생성
            Credential credential = PlayGamesAuthProvider.GetCredential(authCode);

            // Firebase 로그인 (반환값이 FirebaseUser)
            FirebaseUser user = await auth.SignInWithCredentialAsync(credential);

            if (user != null)
            {
                UpdateStatus($"✅ 로그인 완료!\n{user.DisplayName ?? "사용자"}님 환영합니다");

                // 1초 후 메인 씬 이동
                await Task.Delay(1000);
                LoadMainScene();
            }
            else
            {
                UpdateStatus("Firebase 인증 실패\n사용자 정보를 가져올 수 없습니다");
                isLoggingIn = false;
            }
        }
        catch (System.Exception e)
        {
            isLoggingIn = false;
            HandleFirebaseError(e);
        }
    }*/

    #endregion

    #region Error Handling

    private void HandleFirebaseError(System.Exception e)
    {
        string errorMsg = "❌ Firebase 인증 실패\n\n";

        // 에러 타입별 상세 메시지
        if (e.Message.Contains("INVALID_IDP_RESPONSE") ||
            e.Message.Contains("IDENTITY_PROVIDER_CONFIGURATION_NOT_FOUND"))
        {
            errorMsg +=
                "📋 체크리스트:\n\n" +
                "1️⃣ Google Cloud Console\n" +
                "   - '웹 애플리케이션' OAuth 생성\n" +
                "   - Client ID 복사\n\n" +
                "2️⃣ Firebase Console\n" +
                "   - Authentication → Sign-in\n" +
                "   - Google 활성화\n" +
                "   - Web Client ID 입력\n\n" +
                "3️⃣ google-services.json\n" +
                "   - 최신 파일로 교체\n" +
                "   - Assets/ 폴더에 위치";
        }
        else if (e.Message.Contains("INVALID_CUSTOM_TOKEN"))
        {
            errorMsg +=
                "⚠️ 토큰 오류\n\n" +
                "Google Cloud Console에서:\n" +
                "- '웹 애플리케이션' OAuth가\n" +
                "  올바르게 생성되었는지 확인\n" +
                "- Firebase에 정확히 등록되었는지 확인";
        }
        else if (e.Message.Contains("network") || e.Message.Contains("Network"))
        {
            errorMsg += "🌐 네트워크 오류\n인터넷 연결을 확인해주세요";
        }
        else if (e.Message.Contains("API key") || e.Message.Contains("api-key"))
        {
            errorMsg +=
                "🔑 API 키 오류\n\n" +
                "- google-services.json 확인\n" +
                "- 올바른 Firebase 프로젝트인지 확인\n" +
                "- Assets/ 폴더에 위치 확인";
        }
        else if (e.Message.Contains("disabled"))
        {
            errorMsg +=
                "🚫 제공업체 비활성화\n\n" +
                "Firebase Console:\n" +
                "Authentication → Sign-in method\n" +
                "→ Google 제공업체 활성화";
        }
        else
        {
            errorMsg += $"상세 오류:\n{e.Message}\n\n원인을 모르겠다면\n로그를 확인해주세요";
        }
        UpdateStatus("최종 오류 : " + e.Message);

    }

    #endregion

    #region Scene Management

    private void LoadMainScene()
    {
        isLoggingIn = false;

        if (!string.IsNullOrEmpty(mainSceneName))
        {
            UpdateStatus($"'{mainSceneName}' 씬으로 이동 중...");
            SceneManager.LoadScene(mainSceneName);
        }
        else
        {
            UpdateStatus("⚠️ 메인 씬 이름이 설정되지 않았습니다!");
            UpdateStatus("씬 이름이 설정되지 않았습니다");
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Firebase 로그인 상태 확인
    /// </summary>
    public bool IsSignedIn()
    {
        return auth != null && auth.CurrentUser != null;
    }

    /// <summary>
    /// 현재 로그인된 사용자
    /// </summary>
    public FirebaseUser GetCurrentUser()
    {
        return auth?.CurrentUser;
    }

    /// <summary>
    /// 현재 사용자 UID
    /// </summary>
    public string GetUserUID()
    {
        return auth?.CurrentUser?.UserId;
    }

    /// <summary>
    /// 현재 사용자 이름
    /// </summary>
    public string GetUserDisplayName()
    {
        return auth?.CurrentUser?.DisplayName ?? "Guest";
    }

    /// <summary>
    /// 로그아웃
    /// </summary>
    #endregion

    #region UI Helper

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text += message;
        }
    }

    #endregion



    [ContextMenu("🔍 Firebase 상태 확인")]
    private void DebugFirebaseStatus()
    {
        if (auth == null)
        {
            UpdateStatus("FirebaseAuth가 초기화되지 않았습니다");
            return;
        }
        var user = auth.CurrentUser;
        if (user != null)
        {
            UpdateStatus($"✅ 로그인됨\n" +
                     $"UID: {user.UserId}\n" +
                     $"이름: {user.DisplayName}\n" +
                     $"이메일: {user.Email}");
        }
        else
        {
            UpdateStatus("❌ 로그인되지 않음");
        }
    }
}