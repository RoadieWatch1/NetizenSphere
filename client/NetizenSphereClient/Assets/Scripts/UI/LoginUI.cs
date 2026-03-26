using System;
using System.Collections;
using NetizenSphere.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetizenSphere.UI
{
    public class LoginUI : MonoBehaviour
    {
        [SerializeField] private string bootSceneName = "Boot";

        // ── State ─────────────────────────────────────────────────────────────────

        private enum LoginState { Idle, Loading, FadingOut }

        private LoginState _state    = LoginState.Idle;
        private bool       _isSignUp = false;

        private string _emailInput    = "";
        private string _passwordInput = "";
        private string _nameInput     = "";   // always collected in both modes
        private string _error         = "";
        private float  _fadeAlpha     = 0f;

        // ── Styles ────────────────────────────────────────────────────────────────

        private GUIStyle _titleStyle;
        private GUIStyle _taglineStyle;
        private GUIStyle _fieldLabelStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _btnSecondaryStyle;
        private GUIStyle _errorStyle;
        private GUIStyle _loadingStyle;
        private bool     _stylesReady;

        private Texture2D _inputBgTex;
        private Texture2D _btnBgTex;
        private Texture2D _btnHoverTex;
        private Texture2D _btnSecBgTex;

        // ── Panel layout ──────────────────────────────────────────────────────────

        private const float PanelW  = 380f;
        private const float PanelH  = 430f;  // always 3 fields: email + password + name
        private const float Pad     = 20f;
        private const float AccentH = 3f;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            TryRestoreSessionAsync();
        }

        private async void TryRestoreSessionAsync()
        {
            if (AuthService.Instance == null) return;

            _state = LoginState.Loading;
            try
            {
                bool restored = await AuthService.Instance.RestoreSessionAsync();
                if (restored && ProfileManager.Instance != null)
                {
                    bool ok = await ProfileManager.Instance.LoadOrCreateProfileAsync(
                        AuthService.Instance.UserId, "");
                    if (ok)
                    {
                        // Pre-fill display name field so user can see/edit it on next login
                        _nameInput = ProfileManager.Instance.ActiveProfile?.DisplayName ?? "";
                        StartCoroutine(FadeAndLoad());
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoginUI] Session restore failed: {e.Message}");
            }
            finally
            {
                if (_state == LoginState.Loading)
                    _state = LoginState.Idle;
            }
        }

        private void OnDestroy()
        {
            if (_inputBgTex  != null) Destroy(_inputBgTex);
            if (_btnBgTex    != null) Destroy(_btnBgTex);
            if (_btnHoverTex != null) Destroy(_btnHoverTex);
            if (_btnSecBgTex != null) Destroy(_btnSecBgTex);
        }

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();

            // ── Full-screen background ────────────────────────────────────────────
            GUI.color = UITheme.ScreenBg;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Panel ─────────────────────────────────────────────────────────────
            float px = (Screen.width  - PanelW) * 0.5f;
            float py = (Screen.height - PanelH) * 0.5f;

            GUI.color = UITheme.PanelBgSolid;
            GUI.DrawTexture(new Rect(px, py, PanelW, PanelH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.color = UITheme.CyanDim;
            GUI.DrawTexture(new Rect(px, py, PanelW, AccentH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Content ───────────────────────────────────────────────────────────
            float cx = px + Pad;
            float cw = PanelW - Pad * 2f;
            float cy = py + Pad;

            // Title
            GUI.Label(new Rect(px, cy, PanelW, 44f), "NetizenSphere", _titleStyle);
            cy += 42f;

            // Tagline
            string tagline = _isSignUp ? "Create your account." : "Enter the network.";
            GUI.Label(new Rect(px, cy, PanelW, 22f), tagline, _taglineStyle);
            cy += 30f;

            // Separator
            GUI.color = UITheme.CyanFaint;
            GUI.DrawTexture(new Rect(cx, cy, cw, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            cy += 14f;

            bool interactive = _state == LoginState.Idle;

            // ── Email ─────────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx, cy, cw, 18f), "EMAIL", _fieldLabelStyle);
            cy += 22f;
            GUI.SetNextControlName("EmailInput");
            _emailInput = GUI.TextField(new Rect(cx, cy, cw, 36f), _emailInput, 128, _inputStyle);
            cy += 42f;

            // ── Password ──────────────────────────────────────────────────────────
            GUI.Label(new Rect(cx, cy, cw, 18f), "PASSWORD", _fieldLabelStyle);
            cy += 22f;
            GUI.SetNextControlName("PasswordInput");
            _passwordInput = GUI.PasswordField(new Rect(cx, cy, cw, 36f), _passwordInput, '*', 128, _inputStyle);
            cy += 42f;

            // ── Display name — always shown in both modes ─────────────────────────
            GUI.Label(new Rect(cx, cy, cw, 18f), "DISPLAY NAME", _fieldLabelStyle);
            cy += 22f;
            GUI.SetNextControlName("NameInput");
            _nameInput = GUI.TextField(new Rect(cx, cy, cw, 36f), _nameInput, 24, _inputStyle);
            cy += 42f;

            // ── Error / Loading ───────────────────────────────────────────────────
            if (_state == LoginState.Loading)
                GUI.Label(new Rect(cx, cy, cw, 20f), "Connecting…", _loadingStyle);
            else
                GUI.Label(new Rect(cx, cy, cw, 20f), _error, _errorStyle);
            cy += 28f;

            // ── Primary button ────────────────────────────────────────────────────
            string btnLabel = _isSignUp ? "CREATE ACCOUNT" : "SIGN IN";
            if (interactive && GUI.Button(new Rect(cx, cy, cw, 46f), btnLabel, _btnStyle))
                TryContinue();
            else if (!interactive)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.35f);
                GUI.Button(new Rect(cx, cy, cw, 46f), btnLabel, _btnStyle);
                GUI.color = Color.white;
            }
            cy += 54f;

            // ── Mode toggle ───────────────────────────────────────────────────────
            string toggleLabel = _isSignUp ? "Already have an account? Sign in" : "No account? Create one";
            if (interactive && GUI.Button(new Rect(cx, cy, cw, 22f), toggleLabel, _btnSecondaryStyle))
            {
                _isSignUp = !_isSignUp;
                _error    = "";
            }

            // ── Enter key shortcut ────────────────────────────────────────────────
            if (interactive
                && Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                TryContinue();
                Event.current.Use();
            }

            // ── Fade overlay ──────────────────────────────────────────────────────
            if (_fadeAlpha > 0f)
            {
                GUI.color = new Color(0f, 0f, 0f, _fadeAlpha);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }

        // ── Logic ─────────────────────────────────────────────────────────────────

        private void TryContinue()
        {
            _error = "";

            string email    = _emailInput.Trim();
            string password = _passwordInput;
            string name     = _nameInput.Trim();

            if (string.IsNullOrWhiteSpace(email))    { _error = "Please enter your email.";        return; }
            if (string.IsNullOrWhiteSpace(password)) { _error = "Please enter your password.";     return; }
            if (string.IsNullOrWhiteSpace(name))     { _error = "Please enter a display name.";    return; }

            if (_isSignUp)
                DoSignUpAsync(email, password, name);
            else
                DoSignInAsync(email, password, name);
        }

        private async void DoSignInAsync(string email, string password, string displayName)
        {
            if (AuthService.Instance == null)
            {
                _error = "Auth service not ready. Please restart.";
                Debug.LogError("[LoginUI] AuthService.Instance is null in DoSignInAsync.");
                return;
            }
            if (ProfileManager.Instance == null)
            {
                _error = "Profile service not ready. Please restart.";
                Debug.LogError("[LoginUI] ProfileManager.Instance is null in DoSignInAsync.");
                return;
            }

            _state = LoginState.Loading;
            try
            {
                var result = await AuthService.Instance.SignInAsync(email, password);
                if (!result.Success)
                {
                    _error = result.Error;
                    _state = LoginState.Idle;
                    return;
                }

                // Pass display name as fallback — used only if no profile exists yet.
                bool ok = await ProfileManager.Instance.LoadOrCreateProfileAsync(
                    result.UserId, displayName);
                if (!ok)
                {
                    _error = "Could not load profile. Please try again.";
                    _state = LoginState.Idle;
                    return;
                }

                // If user entered a different name than what's stored, update it.
                var active = ProfileManager.Instance.ActiveProfile;
                if (active != null
                    && !string.IsNullOrWhiteSpace(displayName)
                    && displayName != active.DisplayName)
                {
                    await ProfileManager.Instance.UpdateDisplayNameAsync(displayName);
                }

                _state = LoginState.FadingOut;
                StartCoroutine(FadeAndLoad());
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoginUI] Sign-in error: {e}");
                _error = "Connection error. Check your network.";
                _state = LoginState.Idle;
            }
        }

        private async void DoSignUpAsync(string email, string password, string displayName)
        {
            if (AuthService.Instance == null)
            {
                _error = "Auth service not ready. Please restart.";
                Debug.LogError("[LoginUI] AuthService.Instance is null in DoSignUpAsync.");
                return;
            }
            if (ProfileManager.Instance == null)
            {
                _error = "Profile service not ready. Please restart.";
                Debug.LogError("[LoginUI] ProfileManager.Instance is null in DoSignUpAsync.");
                return;
            }

            _state = LoginState.Loading;
            try
            {
                var result = await AuthService.Instance.SignUpAsync(email, password);
                if (!result.Success)
                {
                    _error = result.Error;
                    _state = LoginState.Idle;
                    return;
                }

                bool ok = await ProfileManager.Instance.LoadOrCreateProfileAsync(
                    result.UserId, displayName);
                if (!ok)
                {
                    _error = "Account created but profile setup failed. Try signing in.";
                    _isSignUp = false;
                    _state    = LoginState.Idle;
                    return;
                }

                _state = LoginState.FadingOut;
                StartCoroutine(FadeAndLoad());
            }
            catch (Exception e)
            {
                Debug.LogError($"[LoginUI] Sign-up error: {e}");
                _error = "Connection error. Check your network.";
                _state = LoginState.Idle;
            }
        }

        private IEnumerator FadeAndLoad()
        {
            _state = LoginState.FadingOut;
            const float Duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < Duration)
            {
                _fadeAlpha  = Mathf.Clamp01(elapsed / Duration);
                elapsed    += Time.deltaTime;
                yield return null;
            }

            _fadeAlpha = 1f;
            SceneManager.LoadScene(bootSceneName);
        }

        public void OnContinuePressed() => TryContinue();

        // ── Style init ────────────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = UITheme.Cyan }
            };

            _taglineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = UITheme.TextMuted }
            };

            _fieldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = UITheme.Cyan }
            };

            _inputBgTex = UITheme.MakeTex(UITheme.InputBg);
            _inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                padding  = new RectOffset(10, 10, 0, 0),
                normal   = { textColor = UITheme.TextPrimary,             background = _inputBgTex },
                focused  = { textColor = new Color(0.95f, 0.98f, 1.00f), background = _inputBgTex },
                hover    = { textColor = UITheme.TextPrimary,             background = _inputBgTex }
            };

            _btnBgTex    = UITheme.MakeTex(UITheme.ButtonBg);
            _btnHoverTex = UITheme.MakeTex(UITheme.ButtonHover);
            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = UITheme.Cyan,                    background = _btnBgTex    },
                hover     = { textColor = new Color(0.40f, 1.00f, 1.00f), background = _btnHoverTex },
                active    = { textColor = UITheme.Cyan,                    background = _btnBgTex    }
            };

            _btnSecBgTex = UITheme.MakeTex(new Color(0f, 0f, 0f, 0f));
            _btnSecondaryStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = UITheme.CyanDim, background = _btnSecBgTex },
                hover     = { textColor = UITheme.Cyan,    background = _btnSecBgTex },
                active    = { textColor = UITheme.CyanDim, background = _btnSecBgTex },
                border    = new RectOffset(0, 0, 0, 0)
            };

            _errorStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = UITheme.TextError }
            };

            _loadingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = UITheme.CyanDim }
            };
        }
    }
}
