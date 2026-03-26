using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace NetizenSphere.Services
{
    /// <summary>
    /// Wraps Supabase Auth (email + password).
    /// Exposes UserId and AccessToken for downstream services.
    /// Does NOT call SessionManager — ProfileManager does that after profile load.
    /// </summary>
    public class AuthService : MonoBehaviour
    {
        public static AuthService Instance { get; private set; }

        // ── State ─────────────────────────────────────────────────────────────────

        public bool   IsAuthenticated { get; private set; }
        public string UserId          { get; private set; }
        public string AccessToken     { get; private set; }

        private const string RefreshTokenKey = "NS_RefreshToken";

        private const string SupabaseUrl     = "https://yigcvvncjzvexyewjwfa.supabase.co";
        private const string SupabasePublishableKey = "sb_publishable_6dcise-41fZ6oKQBor4Afg_bCOVtBLA";

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("AuthService");
            DontDestroyOnLoad(go);
            go.AddComponent<AuthService>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Signs up a new user. Returns success/error result.
        /// On success, auth state is set (UserId, AccessToken).
        /// </summary>
        public async Task<AuthResult> SignUpAsync(string email, string password)
        {
            string url  = $"{SupabaseUrl}/auth/v1/signup";
            string body = $"{{\"email\":\"{EscapeJson(email)}\",\"password\":\"{EscapeJson(password)}\"}}";

            var response = await PostAuthAsync(url, body);
            if (!response.Success) return response;

            ApplySession(response.UserId, response.AccessToken, response.RefreshToken);
            return response;
        }

        /// <summary>
        /// Signs in with email + password. Returns success/error result.
        /// On success, auth state is set (UserId, AccessToken).
        /// </summary>
        public async Task<AuthResult> SignInAsync(string email, string password)
        {
            string url  = $"{SupabaseUrl}/auth/v1/token?grant_type=password";
            string body = $"{{\"email\":\"{EscapeJson(email)}\",\"password\":\"{EscapeJson(password)}\"}}";

            var response = await PostAuthAsync(url, body);
            if (!response.Success) return response;

            ApplySession(response.UserId, response.AccessToken, response.RefreshToken);
            return response;
        }

        /// <summary>
        /// Attempts to restore a session from a saved refresh token.
        /// Returns true if a valid session was restored.
        /// </summary>
        public async Task<bool> RestoreSessionAsync()
        {
            string saved = PlayerPrefs.GetString(RefreshTokenKey, string.Empty);
            if (string.IsNullOrEmpty(saved)) return false;

            string url  = $"{SupabaseUrl}/auth/v1/token?grant_type=refresh_token";
            string body = $"{{\"refresh_token\":\"{EscapeJson(saved)}\"}}";

            var response = await PostAuthAsync(url, body);
            if (!response.Success)
            {
                ClearSavedSession();
                return false;
            }

            ApplySession(response.UserId, response.AccessToken, response.RefreshToken);
            return true;
        }

        /// <summary>
        /// Full logout chain: clears Supabase auth state, then cascades to
        /// ProfileManager and SessionManager so all runtime identity is reset.
        /// </summary>
        public void SignOut()
        {
            Debug.Log("[AuthService] Signing out — clearing auth, profile, and session state.");

            IsAuthenticated = false;
            UserId          = null;
            AccessToken     = null;
            ClearSavedSession();

            ProfileManager.Instance?.ClearActiveProfile();
            SessionManager.Instance?.ClearAuthenticatedState();
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void ApplySession(string userId, string accessToken, string refreshToken)
        {
            UserId          = userId;
            AccessToken     = accessToken;
            IsAuthenticated = true;

            if (!string.IsNullOrEmpty(refreshToken))
            {
                PlayerPrefs.SetString(RefreshTokenKey, refreshToken);
                PlayerPrefs.Save();
            }
        }

        private void ClearSavedSession()
        {
            PlayerPrefs.DeleteKey(RefreshTokenKey);
            PlayerPrefs.Save();
        }

        private static async Task<AuthResult> PostAuthAsync(string url, string body)
        {
            using var req = new UnityWebRequest(url, "POST")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("apikey",       SupabasePublishableKey);
            req.SetRequestHeader("Content-Type", "application/json");

            var tcs = new TaskCompletionSource<bool>();
            req.SendWebRequest().completed += _ => tcs.TrySetResult(true);
            await tcs.Task;

            string text = req.downloadHandler.text;

            if (req.result != UnityWebRequest.Result.Success)
            {
                string errorMsg = ParseAuthError(text);
                Debug.LogWarning($"[AuthService] Auth request failed ({req.responseCode}): {errorMsg}");
                return AuthResult.Fail(errorMsg);
            }

            var parsed = JsonUtility.FromJson<AuthResponse>(text);
            if (parsed == null || string.IsNullOrEmpty(parsed.access_token))
                return AuthResult.Fail("Unexpected response from server.");

            return AuthResult.Ok(parsed.user?.id, parsed.access_token, parsed.refresh_token);
        }

        private static string ParseAuthError(string json)
        {
            if (string.IsNullOrEmpty(json)) return "No response from server.";
            try
            {
                var err = JsonUtility.FromJson<AuthErrorResponse>(json);
                if (!string.IsNullOrEmpty(err?.error_description)) return err.error_description;
                if (!string.IsNullOrEmpty(err?.msg))               return err.msg;
            }
            catch { /* fall through */ }
            return "Authentication failed.";
        }

        private static string EscapeJson(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        // ── Wire types ────────────────────────────────────────────────────────────

        [Serializable]
        private class AuthResponse
        {
            public string access_token;
            public string refresh_token;
            public AuthUser user;
        }

        [Serializable]
        private class AuthUser
        {
            public string id;
            public string email;
        }

        [Serializable]
        private class AuthErrorResponse
        {
            public string error;
            public string error_description;
            public string msg;
        }
    }

    // ── Result type ───────────────────────────────────────────────────────────────

    public sealed class AuthResult
    {
        public bool   Success      { get; private set; }
        public string UserId       { get; private set; }
        public string AccessToken  { get; private set; }
        public string RefreshToken { get; private set; }
        public string Error        { get; private set; }

        public static AuthResult Ok(string userId, string accessToken, string refreshToken) =>
            new AuthResult { Success = true, UserId = userId, AccessToken = accessToken, RefreshToken = refreshToken };

        public static AuthResult Fail(string error) =>
            new AuthResult { Success = false, Error = error };
    }
}
