using System;
using UnityEngine;
using Firebase.Auth;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>
/// Google 로그인 전담 클래스 (최신 GPGS 버전 대응)
/// 
/// 주요 수정사항:
/// 1. SignInStatus enum 3종류만 처리 (Success, Canceled, InternalError)
/// 2. FirebaseUser vs AuthResult 형식 오류 수정
/// 3. GoogleAuthProvider 올바른 사용법
/// </summary>
public class GPGSManager : MonoBehaviour
{
    #region Events
    public event Action<FirebaseUser> OnGoogleLoginSuccess;
    public event Action<string> OnGoogleLoginFailed;
    #endregion

    #region Properties
    private FirebaseAuth auth;
    private bool isInitialized = false;
    #endregion

    #region Initialization
    private void Awake()
    {
        InitializeFirebaseAuth();
    }

    private void Start()
    {
#if UNITY_ANDROID
        InitializeGooglePlayGames();
#endif
    }

    /// <summary>
    /// Firebase Auth 초기화
    /// </summary>
    private void InitializeFirebaseAuth()
    {
        auth = FirebaseAuth.DefaultInstance;
        
        if (auth != null)
        {
            isInitialized = true;
            Debug.Log("[GoogleLogin] ✅ Firebase Auth 초기화 완료");
        }
        else
        {
            Debug.LogError("[GoogleLogin] ❌ Firebase Auth 초기화 실패");
        }
    }

#if UNITY_ANDROID
    /// <summary>
    /// Google Play Games 초기화 (최신 API)
    /// </summary>
    private void InitializeGooglePlayGames()
    {
        Debug.Log("[GoogleLogin] 🎮 Google Play Games 초기화...");

        try
        {
            PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
        }
        catch (Exception e)
        {
            Debug.LogError($"[GoogleLogin] ❌ Google Play Games 초기화 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 인증 결과 처리 (자동 로그인)
    /// </summary>
    private void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("[GoogleLogin] ✅ Google Play Games 자동 인증 성공");
            Debug.Log($"[GoogleLogin] Display Name: {PlayGamesPlatform.Instance.localUser.userName}");
            Debug.Log($"[GoogleLogin] User ID: {PlayGamesPlatform.Instance.localUser.id}");
        }
        else
        {
            Debug.Log($"[GoogleLogin] Google Play Games 자동 인증 실패: {status}");
        }
    }
#endif
    #endregion

    #region Sign In
    /// <summary>
    /// Google 로그인 시작
    /// UI 버튼에서 호출
    /// </summary>
    public void SignIn()
    {
        if (!isInitialized)
        {
            Debug.LogError("[GoogleLogin] ❌ Firebase Auth 초기화 안 됨");
            OnGoogleLoginFailed?.Invoke("시스템이 준비되지 않았습니다");
            return;
        }

        Debug.Log("[GoogleLogin] 🔑 Google 로그인 시작...");

#if UNITY_ANDROID
        SignInWithGooglePlayGames();
#elif UNITY_EDITOR
        Debug.LogWarning("[GoogleLogin] ⚠️ 에디터에서는 Google 로그인 불가");
        OnGoogleLoginFailed?.Invoke("에디터에서는 Google 로그인을 테스트할 수 없습니다");
#else
        Debug.LogWarning("[GoogleLogin] ⚠️ 지원하지 않는 플랫폼");
        OnGoogleLoginFailed?.Invoke("지원하지 않는 플랫폼입니다");
#endif
    }

#if UNITY_ANDROID
    /// <summary>
    /// Google Play Games로 로그인 (수동 인증)
    /// </summary>
    private void SignInWithGooglePlayGames()
    {
        Debug.Log("[GoogleLogin] 🎮 Google Play Games 수동 로그인 시도...");
        PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessManualAuthentication);
    }

    /// <summary>
    /// 수동 인증 결과 처리
    /// </summary>
    private void ProcessManualAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("[GoogleLogin] ✅ Google Play Games 수동 인증 성공");

            string displayName = PlayGamesPlatform.Instance.localUser.userName;
            string userId = PlayGamesPlatform.Instance.localUser.id;

            Debug.Log($"[GoogleLogin] 사용자 정보:");
            Debug.Log($"  Display Name: {displayName}");
            Debug.Log($"  User ID: {userId}");

            RequestServerSideAccess();
        }
        else
        {
            HandleGooglePlayGamesError(status);
        }
    }

    /// <summary>
    /// Server-Side Access 요청 (Auth Code 가져오기)
    /// </summary>
    private void RequestServerSideAccess()
    {
        Debug.Log("[GoogleLogin] 🔑 Auth Code 요청 중...");

        PlayGamesPlatform.Instance.RequestServerSideAccess(
            /* forceRefreshToken= */ false,
            (string authCode) =>
            {
                if (!string.IsNullOrEmpty(authCode))
                {
                    Debug.Log($"[GoogleLogin] ✅ Auth Code 획득 성공");
                    Debug.Log($"[GoogleLogin] Auth Code 길이: {authCode.Length}자");
                    
                    // Firebase 인증
                    SignInWithFirebase(authCode);
                }
                else
                {
                    Debug.LogError("[GoogleLogin] ❌ Auth Code 획득 실패: authCode가 null");
                    OnGoogleLoginFailed?.Invoke("Google 인증 코드를 가져올 수 없습니다");
                }
            }
        );
    }

    /// <summary>
    /// Google Play Games 에러 처리
    /// ✅ 수정: 최신 GPGS는 3가지 상태만 존재
    /// - Success
    /// - Canceled
    /// - InternalError
    /// </summary>
    private void HandleGooglePlayGamesError(SignInStatus status)
    {
        Debug.LogError($"[GoogleLogin] ❌ Google Play Games 로그인 실패: {status}");

        string errorMessage;
        
        // 최신 GPGS는 3가지 상태만 존재
        if (status == SignInStatus.Canceled)
        {
            errorMessage = "사용자가 로그인을 취소했습니다";
        }
        else if (status == SignInStatus.InternalError)
        {
            errorMessage = "Google Play Games 내부 오류가 발생했습니다\n다음을 확인해주세요:\n" +
                          "1. SHA-1 인증서 등록 확인\n" +
                          "2. OAuth 클라이언트 ID 설정\n" +
                          "3. 네트워크 연결 상태";
        }
        else
        {
            errorMessage = $"알 수 없는 오류: {status}";
        }

        OnGoogleLoginFailed?.Invoke(errorMessage);
    }
#endif

    /// <summary>
    /// Firebase에 Google 계정으로 로그인
    /// ✅ 수정: AuthResult와 FirebaseUser 타입 명확히 분리
    /// </summary>
    private async void SignInWithFirebase(string authCode)
    {
        if (string.IsNullOrEmpty(authCode))
        {
            Debug.LogError("[GoogleLogin] ❌ Auth Code가 null입니다");
            OnGoogleLoginFailed?.Invoke("Google 인증 코드를 가져올 수 없습니다");
            return;
        }

        Debug.Log("[GoogleLogin] 🔄 Firebase 인증 중...");

        try
        {
            // ✅ GoogleAuthProvider로 Credential 생성
            // Play Games Auth Code는 두 번째 파라미터에 전달
            Credential credential = GoogleAuthProvider.GetCredential(null, authCode);
            
            
            // ✅ Firebase 로그인 - AuthResult 반환
           // AuthResult authResult = await auth.SignInWithCredentialAsync(credential).Result;

            // ✅ AuthResult.User로 FirebaseUser 추출
            FirebaseUser user = await auth.SignInWithCredentialAsync(credential);

            if (user != null)
            {
                Debug.Log($"[GoogleLogin] ✅✅✅ Firebase Google 로그인 성공!");
                Debug.Log($"  User ID: {user.UserId}");
                Debug.Log($"  Email: {user.Email ?? "없음"}");
                Debug.Log($"  Display Name: {user.DisplayName ?? "없음"}");
                Debug.Log($"  Photo URL: {user.PhotoUrl?.ToString() ?? "없음"}");

                // 이벤트 발행 - FirebaseUser 전달
                OnGoogleLoginSuccess?.Invoke(user);
            }
            else
            {
                Debug.LogError("[GoogleLogin] ❌ AuthResult.User가 null입니다");
                OnGoogleLoginFailed?.Invoke("Firebase 사용자 정보를 가져올 수 없습니다");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GoogleLogin] ❌ Firebase 인증 실패: {e.Message}");
            Debug.LogError($"[GoogleLogin] Stack Trace: {e.StackTrace}");
            
            // 자세한 에러 메시지
            string errorMsg = e.Message;
            if (errorMsg.Contains("INVALID_IDP_RESPONSE"))
            {
                errorMsg = "Google 인증 실패\n다음을 확인하세요:\n" +
                          "1. Firebase Console에서 Google 로그인 활성화\n" +
                          "2. OAuth 클라이언트 ID 올바르게 등록\n" +
                          "3. SHA-1 인증서 정확히 등록";
            }
            else if (errorMsg.Contains("network"))
            {
                errorMsg = "네트워크 오류가 발생했습니다\n인터넷 연결을 확인해주세요";
            }
            
            OnGoogleLoginFailed?.Invoke($"Firebase 인증 실패:\n{errorMsg}");
        }
    }
    #endregion

    #region Public Helpers
    /// <summary>
    /// Google Play Games 인증 상태 확인
    /// </summary>
    public bool IsAuthenticated()
    {
#if UNITY_ANDROID
        return PlayGamesPlatform.Instance != null && 
               PlayGamesPlatform.Instance.localUser != null && 
               PlayGamesPlatform.Instance.localUser.authenticated;
#else
        return false;
#endif
    }

    /// <summary>
    /// 현재 Google 사용자 이름
    /// </summary>
    public string GetUserDisplayName()
    {
#if UNITY_ANDROID
        return PlayGamesPlatform.Instance?.localUser?.userName ?? "";
#else
        return "";
#endif
    }

    /// <summary>
    /// 현재 Google 사용자 ID
    /// </summary>
    public string GetUserId()
    {
#if UNITY_ANDROID
        return PlayGamesPlatform.Instance?.localUser?.id ?? "";
#else
        return "";
#endif
    }

    /// <summary>
    /// Firebase 사용자 정보
    /// </summary>
    public FirebaseUser GetFirebaseUser()
    {
        return auth?.CurrentUser;
    }

    /// <summary>
    /// Firebase 로그인 상태 확인
    /// </summary>
    public bool IsFirebaseSignedIn()
    {
        return auth?.CurrentUser != null;
    }
    #endregion

    #region Debug
    [ContextMenu("Test: Sign In")]
    public void TestSignIn()
    {
        SignIn();
    }

    [ContextMenu("Print Auth Status")]
    public void PrintAuthStatus()
    {
        Debug.Log("==================== Google Login Status ====================");
        Debug.Log($"GPGS Authenticated: {IsAuthenticated()}");
        Debug.Log($"Firebase Signed In: {IsFirebaseSignedIn()}");
        
#if UNITY_ANDROID
        if (IsAuthenticated())
        {
            Debug.Log($"GPGS Display Name: {GetUserDisplayName()}");
            Debug.Log($"GPGS User ID: {GetUserId()}");
        }
#endif

        if (IsFirebaseSignedIn())
        {
            var user = GetFirebaseUser();
            Debug.Log($"Firebase User ID: {user.UserId}");
            Debug.Log($"Firebase Email: {user.Email ?? "없음"}");
            Debug.Log($"Firebase Display Name: {user.DisplayName ?? "없음"}");
        }
        
        Debug.Log("===========================================================");
    }

    [ContextMenu("Test: Print SignInStatus Values")]
    public void PrintSignInStatusValues()
    {
#if UNITY_ANDROID
        Debug.Log("==================== SignInStatus Enum Values ====================");
        Debug.Log($"Success: {(int)SignInStatus.Success}");
        Debug.Log($"Canceled: {(int)SignInStatus.Canceled}");
        Debug.Log($"InternalError: {(int)SignInStatus.InternalError}");
        Debug.Log("=================================================================");
#endif
    }
    #endregion
}