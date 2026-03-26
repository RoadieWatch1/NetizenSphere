using System.Threading.Tasks;
using NetizenSphere.Data;
using UnityEngine;

namespace NetizenSphere.Services
{
    /// <summary>
    /// Loads or creates a Supabase profile after auth completes.
    /// Caches the active profile at runtime and feeds DisplayName into SessionManager
    /// so all existing Phase 2 identity code continues to work unchanged.
    /// </summary>
    public class ProfileManager : MonoBehaviour
    {
        public static ProfileManager Instance { get; private set; }

        public UserProfile ActiveProfile { get; private set; }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("ProfileManager");
            DontDestroyOnLoad(go);
            go.AddComponent<ProfileManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fetches the profile for <paramref name="userId"/> from Supabase.
        /// If no profile exists, creates one with <paramref name="fallbackDisplayName"/>.
        /// Feeds the resulting display name into SessionManager so Phase 2 code
        /// picks it up automatically.
        /// </summary>
        public async Task<bool> LoadOrCreateProfileAsync(string userId, string fallbackDisplayName)
        {
            Debug.Log($"[ProfileManager] Step 1 — GET profile for userId={userId}");
            UserProfile profile = await BackendClient.Instance.GetProfileAsync(userId);

            if (profile != null)
            {
                Debug.Log($"[ProfileManager] Step 1 result — profile found: {profile.DisplayName}");
            }
            else
            {
                Debug.Log("[ProfileManager] Step 1 result — no profile found, will create.");
                string name = SanitizeName(fallbackDisplayName, userId);
                Debug.Log($"[ProfileManager] Step 2 — POST create profile with name={name}");
                profile = await BackendClient.Instance.CreateProfileAsync(new UserProfile
                {
                    UserId             = userId,
                    DisplayName        = name,
                    AvatarPrimaryColor = "#00FFFF",
                    AvatarAccentColor  = "#FFFFFF",
                    AvatarPreset       = null
                });

                if (profile != null)
                    Debug.Log($"[ProfileManager] Step 2 result — profile created: {profile.DisplayName}");
                else
                    Debug.LogError("[ProfileManager] Step 2 result — create failed. See BackendClient logs above.");
            }

            if (profile == null)
            {
                Debug.LogError("[ProfileManager] Both GET and POST failed — cannot continue.");
                return false;
            }

            ActiveProfile = profile;
            Debug.Log($"[ProfileManager] Step 3 — SessionManager.SignIn({profile.DisplayName})");
            SessionManager.Instance.SignIn(profile.DisplayName);

            Debug.Log("[ProfileManager] Step 4 — profile chain complete, Boot scene will load.");
            _ = BackendClient.Instance.UpdateLastLoginAsync(userId);

            return true;
        }

        /// <summary>
        /// Updates the display name on the backend and keeps SessionManager in sync.
        /// </summary>
        public async Task<bool> UpdateDisplayNameAsync(string newName)
        {
            if (ActiveProfile == null) return false;

            string sanitized = SanitizeName(newName, ActiveProfile.UserId);
            bool ok = await BackendClient.Instance.UpdateDisplayNameAsync(ActiveProfile.UserId, sanitized);
            if (!ok) return false;

            ActiveProfile.DisplayName = sanitized;
            SessionManager.Instance.SignIn(sanitized);
            return true;
        }

        /// <summary>
        /// Updates avatar customization fields on the backend and in the local cache.
        /// </summary>
        public async Task<bool> UpdateAvatarAsync(string primaryColor, string accentColor, string preset)
        {
            if (ActiveProfile == null) return false;

            bool ok = await BackendClient.Instance.UpdateAvatarAsync(
                ActiveProfile.UserId, primaryColor, accentColor, preset);

            if (!ok) return false;

            ActiveProfile.AvatarPrimaryColor = primaryColor;
            ActiveProfile.AvatarAccentColor  = accentColor;
            ActiveProfile.AvatarPreset        = preset;
            return true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string SanitizeName(string name, string userId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Netizen_" + userId.Substring(0, Mathf.Min(4, userId.Length));

            name = name.Trim();
            return name.Length > 24 ? name.Substring(0, 24) : name;
        }
    }
}
