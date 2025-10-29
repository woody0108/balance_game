using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using TMPro;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class SimpleGoogleLogin : MonoBehaviour
{
    public TextMeshProUGUI bt;
    private FirebaseAuth auth;

    private void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

#if UNITY_ANDROID
        // ✅ 구글 플레이 게임즈 초기화
        PlayGamesPlatform.Activate();
        Debug.Log("[GoogleLogin] GPGS 초기화 완료");
#endif
    }

    // 🔘 버튼에서 이 함수를 연결하세요 (OnClick)
    public void OnClickLogin()
    {
#if UNITY_ANDROID
        PlayGamesPlatform.Instance.ManuallyAuthenticate(ProcessLogin);
#else
        Debug.LogWarning("Google 로그인은 Android에서만 지원됩니다.");
#endif
    }

#if UNITY_ANDROID
    private void ProcessLogin(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("[GoogleLogin] ✅ GPGS 로그인 성공");
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, SignInFirebase);
        }
        else
        {
            Debug.LogError($"[GoogleLogin] ❌ 로그인 실패: {status}");
        }
    }

    private async void SignInFirebase(string authCode)
    {
        if (string.IsNullOrEmpty(authCode))
        {
            Debug.LogError("[GoogleLogin] ❌ AuthCode가 비어있습니다.");
            return;
        }

        Debug.Log("[GoogleLogin] 🔄 Firebase 로그인 시도 중...");
         bt.text = "로그인 시도중";
        Credential credential = GoogleAuthProvider.GetCredential(null, authCode);

        try
        {
            // ✅ 최신 Firebase SDK 기준 (AuthResult 사용)
       //     var result = await auth.SignInWithCredentialAsync(credential);
           FirebaseUser user = await auth.SignInWithCredentialAsync(credential);

            Debug.Log($"[Firebase] ✅ 로그인 성공: {user.DisplayName}, {user.Email}");
             SceneManager.LoadScene("MainScene");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firebase] ❌ 로그인 실패: {e.Message}");
            bt.text = "로그인 실패";
        }
    }
#endif
}
