using NetizenSphere.Data;
using UnityEngine;

namespace NetizenSphere.Services
{
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        // ── Phase 2 legacy properties — preserved, unchanged ──────────────────────

        public string DisplayName { get; private set; } = string.Empty;
        public bool   IsSignedIn  { get; private set; }

        // ── Phase 3 authenticated identity ────────────────────────────────────────

        public bool   IsAuthenticated           { get; private set; }
        public string CurrentUserId             { get; private set; }
        public string CurrentDisplayName        { get; private set; }
        public string CurrentAvatarPrimaryColor { get; private set; }
        public string CurrentAvatarAccentColor  { get; private set; }
        public string CurrentAvatarPreset       { get; private set; }

        private const string DisplayNameKey = "DisplayName";

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSession();
        }

        // ── Phase 2 legacy API — preserved, unchanged ─────────────────────────────

        /// <summary>
        /// Sets display name and IsSignedIn. Persists to PlayerPrefs.
        /// Called by Phase 3 ApplyAuthenticatedProfile and also directly by legacy paths.
        /// </summary>
        public void SignIn(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return;

            DisplayName = displayName.Trim();
            IsSignedIn  = true;

            PlayerPrefs.SetString(DisplayNameKey, DisplayName);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Clears display name and IsSignedIn. Removes PlayerPrefs key.
        /// Called by ClearAuthenticatedState and by legacy logout paths.
        /// </summary>
        public void SignOut()
        {
            DisplayName = string.Empty;
            IsSignedIn  = false;

            PlayerPrefs.DeleteKey(DisplayNameKey);
            PlayerPrefs.Save();
        }

        // ── Phase 3 authenticated identity API ────────────────────────────────────

        /// <summary>
        /// Applies the fully loaded Supabase profile as the app's runtime identity.
        /// Sets all Phase 3 authenticated fields and bridges display name into the
        /// Phase 2 chain so PlayerIdentity, ChatUI, and all existing systems continue
        /// working without modification.
        /// </summary>
        public void ApplyAuthenticatedProfile(UserProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning("[SessionManager] ApplyAuthenticatedProfile called with null profile — ignoring.");
                return;
            }

            IsAuthenticated           = true;
            CurrentUserId             = profile.UserId;
            CurrentDisplayName        = profile.DisplayName;
            CurrentAvatarPrimaryColor = profile.AvatarPrimaryColor;
            CurrentAvatarAccentColor  = profile.AvatarAccentColor;
            CurrentAvatarPreset       = profile.AvatarPreset;

            Debug.Log(
                $"[SessionManager] Authenticated profile applied — " +
                $"userId={profile.UserId} | " +
                $"displayName={profile.DisplayName} | " +
                $"primaryColor={profile.AvatarPrimaryColor} | " +
                $"accentColor={profile.AvatarAccentColor} | " +
                $"preset={profile.AvatarPreset ?? "none"}");

            // Bridge into Phase 2 chain — keeps PlayerIdentity, ChatUI, PlayerPresenceManager,
            // PlayerNameplate, and all other existing systems working unchanged.
            SignIn(profile.DisplayName);
        }

        /// <summary>
        /// Resets authenticated runtime identity on logout or session loss.
        /// Also clears Phase 2 display name so all systems return to a login-safe state.
        /// </summary>
        public void ClearAuthenticatedState()
        {
            IsAuthenticated           = false;
            CurrentUserId             = null;
            CurrentDisplayName        = null;
            CurrentAvatarPrimaryColor = null;
            CurrentAvatarAccentColor  = null;
            CurrentAvatarPreset       = null;

            Debug.Log("[SessionManager] Authenticated state cleared.");

            // Also clear Phase 2 chain so legacy systems don't retain stale identity.
            SignOut();
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void LoadSession()
        {
            string savedName = PlayerPrefs.GetString(DisplayNameKey, string.Empty);

            if (!string.IsNullOrWhiteSpace(savedName))
            {
                DisplayName = savedName;
                IsSignedIn  = true;
            }
        }
    }
}
