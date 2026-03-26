using System;
using System.Text;
using System.Threading.Tasks;
using NetizenSphere.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace NetizenSphere.Services
{
    /// <summary>
    /// Sends authenticated REST requests to Supabase on behalf of the signed-in user.
    /// Requires AuthService.Instance.AccessToken to be set before use.
    /// </summary>
    public class BackendClient : MonoBehaviour
    {
        public static BackendClient Instance { get; private set; }

        // ── Supabase config ───────────────────────────────────────────────────────

        private const string SupabaseUrl          = "https://yigcvvncjzvexyewjwfa.supabase.co";
        private const string SupabasePublishableKey = "sb_publishable_6dcise-41fZ6oKQBor4Afg_bCOVtBLA";

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("BackendClient");
            DontDestroyOnLoad(go);
            go.AddComponent<BackendClient>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the profile row for <paramref name="userId"/>, or null if not found.
        /// </summary>
        public async Task<UserProfile> GetProfileAsync(string userId)
        {
            string url = $"{SupabaseUrl}/rest/v1/profiles?id=eq.{userId}&select=*";
            using var req = MakeGet(url);
            await SendAsync(req);

            LogResponse("GetProfile", req);

            if (req.result != UnityWebRequest.Result.Success)
                return null;

            var rows = ParseProfileArray(req.downloadHandler.text);
            return rows.Length == 0 ? null : ToUserProfile(rows[0]);
        }

        /// <summary>
        /// Inserts a new profile row and returns the fetched result.
        /// Does a POST to insert, then a GET to retrieve — avoids relying on
        /// Prefer: return=representation which can return empty on some configs.
        /// </summary>
        public async Task<UserProfile> CreateProfileAsync(UserProfile profile)
        {
            string url  = $"{SupabaseUrl}/rest/v1/profiles";
            string body = BuildProfileJson(profile);

            Debug.Log($"[BackendClient] CreateProfile body: {body}");
            Debug.Log($"[BackendClient] AccessToken present: {!string.IsNullOrEmpty(AuthService.Instance?.AccessToken)}");

            using var req = MakePost(url, body);
            await SendAsync(req);

            LogResponse("CreateProfile", req);

            if (req.result != UnityWebRequest.Result.Success)
                return null;

            // Re-fetch rather than parsing the POST response body,
            // which may be empty depending on Supabase key config.
            return await GetProfileAsync(profile.UserId);
        }

        /// <summary>
        /// Patches the display_name column for <paramref name="userId"/>.
        /// </summary>
        public async Task<bool> UpdateDisplayNameAsync(string userId, string displayName)
        {
            string url  = $"{SupabaseUrl}/rest/v1/profiles?id=eq.{userId}";
            string body = $"{{\"display_name\":\"{EscapeJson(displayName)}\"}}";

            using var req = MakePatch(url, body);
            await SendAsync(req);

            LogResponse("UpdateDisplayName", req);
            return req.result == UnityWebRequest.Result.Success;
        }

        /// <summary>
        /// Patches the three avatar columns for <paramref name="userId"/>.
        /// </summary>
        public async Task<bool> UpdateAvatarAsync(string userId,
                                                   string primaryColor,
                                                   string accentColor,
                                                   string preset)
        {
            string url  = $"{SupabaseUrl}/rest/v1/profiles?id=eq.{userId}";
            var    body = new AvatarPatch
            {
                avatar_primary_color = primaryColor,
                avatar_accent_color  = accentColor,
                avatar_preset        = preset ?? ""
            };

            using var req = MakePatch(url, JsonUtility.ToJson(body));
            await SendAsync(req);

            LogResponse("UpdateAvatar", req);
            return req.result == UnityWebRequest.Result.Success;
        }

        /// <summary>
        /// Sets last_login_at to now() for <paramref name="userId"/>.
        /// </summary>
        public async Task UpdateLastLoginAsync(string userId)
        {
            string url  = $"{SupabaseUrl}/rest/v1/profiles?id=eq.{userId}";
            string body = $"{{\"last_login_at\":\"{DateTime.UtcNow:O}\"}}";

            using var req = MakePatch(url, body);
            await SendAsync(req);

            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[BackendClient] UpdateLastLogin failed ({req.responseCode}): {req.error}");
        }

        // ── Request builders ──────────────────────────────────────────────────────

        private UnityWebRequest MakeGet(string url)
        {
            var req = UnityWebRequest.Get(url);
            AddCommonHeaders(req);
            req.SetRequestHeader("Accept", "application/json");
            return req;
        }

        private UnityWebRequest MakePost(string url, string json)
        {
            var req = new UnityWebRequest(url, "POST")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            AddCommonHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept",       "application/json");
            return req;
        }

        private UnityWebRequest MakePatch(string url, string json)
        {
            var req = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            AddCommonHeaders(req);
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept",       "application/json");
            return req;
        }

        private void AddCommonHeaders(UnityWebRequest req)
        {
            req.SetRequestHeader("apikey",        SupabasePublishableKey);
            req.SetRequestHeader("Authorization", $"Bearer {AuthService.Instance.AccessToken}");
        }

        // ── Async helper ──────────────────────────────────────────────────────────

        private static Task SendAsync(UnityWebRequest req)
        {
            var tcs = new TaskCompletionSource<bool>();
            req.SendWebRequest().completed += _ => tcs.TrySetResult(true);
            return tcs.Task;
        }

        // ── JSON helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the profile insert JSON manually to guarantee snake_case field names
        /// and correct null handling that JsonUtility cannot provide.
        /// </summary>
        private static string BuildProfileJson(UserProfile p)
        {
            string preset = string.IsNullOrEmpty(p.AvatarPreset)
                ? "null"
                : $"\"{EscapeJson(p.AvatarPreset)}\"";

            return "{"
                + $"\"id\":\"{EscapeJson(p.UserId)}\","
                + $"\"display_name\":\"{EscapeJson(p.DisplayName)}\","
                + $"\"avatar_primary_color\":\"{EscapeJson(p.AvatarPrimaryColor)}\","
                + $"\"avatar_accent_color\":\"{EscapeJson(p.AvatarAccentColor)}\","
                + $"\"avatar_preset\":{preset}"
                + "}";
        }

        private static ProfileRow[] ParseProfileArray(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Trim() == "[]") return Array.Empty<ProfileRow>();
            try
            {
                string wrapped = "{\"items\":" + json + "}";
                var result = JsonUtility.FromJson<ProfileRowArray>(wrapped);
                Debug.Log($"[BackendClient] ParseProfileArray parsed {result?.items?.Length ?? 0} row(s)");
                return result?.items ?? Array.Empty<ProfileRow>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendClient] ParseProfileArray failed: {e.Message} | json: {json}");
                return Array.Empty<ProfileRow>();
            }
        }

        private static UserProfile ToUserProfile(ProfileRow row) => new UserProfile
        {
            UserId             = row.id,
            DisplayName        = row.display_name,
            AvatarPrimaryColor = row.avatar_primary_color,
            AvatarAccentColor  = row.avatar_accent_color,
            AvatarPreset       = row.avatar_preset
        };

        private static void LogResponse(string op, UnityWebRequest req)
        {
            if (req.result != UnityWebRequest.Result.Success)
                Debug.LogWarning($"[BackendClient] {op} failed | code={req.responseCode} error={req.error} body={req.downloadHandler?.text}");
            else
                Debug.Log($"[BackendClient] {op} ok | code={req.responseCode} body={req.downloadHandler?.text}");
        }

        private static string EscapeJson(string s) =>
            s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

        // ── Wire types ────────────────────────────────────────────────────────────

        [Serializable] private class ProfileRowArray { public ProfileRow[] items; }

        [Serializable]
        private class ProfileRow
        {
            public string id;
            public string display_name;
            public string avatar_primary_color;
            public string avatar_accent_color;
            public string avatar_preset;
            public string last_login_at;
            public string created_at;
            public string updated_at;
        }

        [Serializable]
        private class AvatarPatch
        {
            public string avatar_primary_color;
            public string avatar_accent_color;
            public string avatar_preset;
        }
    }
}
