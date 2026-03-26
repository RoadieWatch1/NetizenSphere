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

        private string _nameInput = "";
        private string _error     = "";
        private float  _fadeAlpha = 0f;
        private bool   _fading    = false;

        // ── Styles ────────────────────────────────────────────────────────────────

        private GUIStyle _titleStyle;
        private GUIStyle _taglineStyle;
        private GUIStyle _fieldLabelStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _errorStyle;
        private bool     _stylesReady;

        private Texture2D _inputBgTex;
        private Texture2D _btnBgTex;
        private Texture2D _btnHoverTex;

        // ── Panel layout constants ────────────────────────────────────────────────

        private const float PanelW   = 380f;
        private const float PanelH   = 260f;
        private const float Pad      = 20f;    // horizontal content padding
        private const float AccentH  = 3f;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.IsSignedIn)
                _nameInput = SessionManager.Instance.DisplayName;
        }

        private void OnDestroy()
        {
            if (_inputBgTex  != null) Destroy(_inputBgTex);
            if (_btnBgTex    != null) Destroy(_btnBgTex);
            if (_btnHoverTex != null) Destroy(_btnHoverTex);
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

            // Top accent bar
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
            GUI.Label(new Rect(px, cy, PanelW, 22f), "Enter the network.", _taglineStyle);
            cy += 30f;

            // Separator
            GUI.color = UITheme.CyanFaint;
            GUI.DrawTexture(new Rect(cx, cy, cw, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            cy += 14f;

            // Field label
            GUI.Label(new Rect(cx, cy, cw, 18f), "DISPLAY NAME", _fieldLabelStyle);
            cy += 22f;

            // Input field
            GUI.SetNextControlName("NameInput");
            _nameInput = GUI.TextField(new Rect(cx, cy, cw, 36f), _nameInput, 24, _inputStyle);
            cy += 42f;

            // Error message (space always reserved — empty string renders nothing)
            GUI.Label(new Rect(cx, cy, cw, 20f), _error, _errorStyle);
            cy += 24f;

            // Continue button (disabled while fading)
            if (!_fading && GUI.Button(new Rect(cx, cy, cw, 46f), "CONTINUE", _btnStyle))
                TryContinue();

            // Enter / Return key shortcut
            if (!_fading
                && Event.current.type == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                TryContinue();
                Event.current.Use();
            }

            // ── Fade overlay (drawn last so it covers everything) ─────────────────
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
            string name = _nameInput.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                _error = "Please enter a display name.";
                return;
            }

            if (name.Length > 24)
                name = name.Substring(0, 24);

            if (SessionManager.Instance == null)
            {
                _error = "Session error — please restart.";
                return;
            }

            _error = "";
            SessionManager.Instance.SignIn(name);
            StartCoroutine(FadeAndLoad());
        }

        private IEnumerator FadeAndLoad()
        {
            _fading = true;
            const float Duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < Duration)
            {
                _fadeAlpha = Mathf.Clamp01(elapsed / Duration);
                elapsed   += Time.deltaTime;
                yield return null;
            }

            _fadeAlpha = 1f;
            SceneManager.LoadScene(bootSceneName);
        }

        // Called by ContinueButton in scene if still wired
        public void OnContinuePressed() => TryContinue();

        // ── Style init ────────────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            // Title: large centered cyan bold
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = UITheme.Cyan }
            };

            // Tagline: centered muted text
            _taglineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = UITheme.TextMuted }
            };

            // Field label: small uppercase cyan
            _fieldLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = UITheme.Cyan }
            };

            // Input: dark bg, light text
            _inputBgTex = UITheme.MakeTex(UITheme.InputBg);
            _inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                padding  = new RectOffset(10, 10, 0, 0),
                normal   = { textColor = UITheme.TextPrimary,               background = _inputBgTex },
                focused  = { textColor = new Color(0.95f, 0.98f, 1.00f),   background = _inputBgTex },
                hover    = { textColor = UITheme.TextPrimary,               background = _inputBgTex }
            };

            // Button: dark teal bg, cyan text
            _btnBgTex   = UITheme.MakeTex(UITheme.ButtonBg);
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

            // Error: small left-aligned warm red
            _errorStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = UITheme.TextError }
            };
        }
    }
}
