using UnityEngine;
using UnityEngine.InputSystem;

namespace NetizenSphere.Chat
{
    public class ChatUI : MonoBehaviour
    {
        // ── Layout ────────────────────────────────────────────────────────────────

        private const float PanelWidth   = 360f;
        private const float MsgAreaH     = 200f;  // scroll area height
        private const float HeaderH      = 22f;
        private const float InputH       = 30f;
        private const float Margin       = 10f;
        private const float InputGap     = 6f;

        // ── State ─────────────────────────────────────────────────────────────────

        private string  _input           = "";
        private Vector2 _scroll;
        private int     _lastMsgCount;
        private bool    _scrollToBottom  = true;

        // ── Styles (created inside OnGUI so GUI.skin is valid) ────────────────────

        private GUIStyle _headerStyle;
        private GUIStyle _msgStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _btnStyle;
        private bool     _stylesReady;

        // Custom textures — created once, destroyed with component
        private Texture2D _inputBgTex;
        private Texture2D _btnBgTex;
        private Texture2D _btnHoverTex;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (_inputBgTex  != null) Destroy(_inputBgTex);
            if (_btnBgTex    != null) Destroy(_btnBgTex);
            if (_btnHoverTex != null) Destroy(_btnHoverTex);
        }

        // ── Update ────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (ChatManager.Instance == null || !ChatManager.Instance.IsSpawned)
                return;

            // Auto-scroll when new messages arrive
            int count = ChatManager.Instance.GetMessages().Count;
            if (count != _lastMsgCount)
            {
                _scrollToBottom = true;
                _lastMsgCount   = count;
            }

            // Enter key submit
            if (Keyboard.current != null
                && Keyboard.current.enterKey.wasPressedThisFrame
                && !string.IsNullOrWhiteSpace(_input))
            {
                Submit();
            }
        }

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (ChatManager.Instance == null || !ChatManager.Instance.IsSpawned)
                return;

            InitStyles();

            // ── Coordinates ──────────────────────────────────────────────────────
            float panelH = HeaderH + MsgAreaH;
            float x      = Margin;
            float panelY = Screen.height - panelH - InputH - InputGap - Margin;
            float inputY = panelY + panelH + InputGap;

            // ── Panel background ─────────────────────────────────────────────────
            var bgRect = new Rect(x - 4f, panelY - 4f, PanelWidth + 8f, panelH + 8f);
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.87f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Top accent bar (cyan) ─────────────────────────────────────────────
            GUI.color = new Color(0.20f, 0.90f, 0.90f, 0.85f);
            GUI.DrawTexture(new Rect(bgRect.x, bgRect.y, bgRect.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Header ───────────────────────────────────────────────────────────
            GUI.Label(new Rect(x, panelY + 2f, PanelWidth, HeaderH), "CHAT", _headerStyle);

            // ── Separator ────────────────────────────────────────────────────────
            GUI.color = new Color(0.20f, 0.90f, 0.90f, 0.18f);
            GUI.DrawTexture(new Rect(x, panelY + HeaderH, PanelWidth - 8f, 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Message scroll area ───────────────────────────────────────────────
            float msgAreaY = panelY + HeaderH + 1f;

            if (_scrollToBottom)
            {
                _scroll.y      = float.MaxValue;
                _scrollToBottom = false;
            }

            GUILayout.BeginArea(new Rect(x, msgAreaY, PanelWidth - 4f, MsgAreaH));
            _scroll = GUILayout.BeginScrollView(
                _scroll, false, false, GUIStyle.none, GUIStyle.none);

            var messages = ChatManager.Instance.GetMessages();
            foreach (var msg in messages)
                GUILayout.Label(FormatMessage(msg), _msgStyle);

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // ── Input row ────────────────────────────────────────────────────────
            float fieldW = PanelWidth - 64f;

            GUI.SetNextControlName("ChatInput");
            _input = GUI.TextField(
                new Rect(x, inputY, fieldW, InputH), _input, 128, _inputStyle);

            if (GUI.Button(new Rect(x + fieldW + 4f, inputY, 56f, InputH), "SEND", _btnStyle))
                Submit();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void Submit()
        {
            if (ChatManager.Instance == null || !ChatManager.Instance.IsSpawned)
                return;

            string trimmed = _input.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            ChatManager.Instance.SendMessage(GetLocalName(), trimmed);
            _input = "";
        }

        private string GetLocalName()
        {
            string saved = PlayerPrefs.GetString("DisplayName", "");
            return string.IsNullOrWhiteSpace(saved) ? "Netizen" : saved;
        }

        /// <summary>
        /// Builds a rich-text display string for a message.
        /// Player names and bodies are escaped to prevent tag injection.
        /// </summary>
        private static string FormatMessage(ChatMessage msg)
        {
            if (msg.Type == ChatMessageType.System)
                return $"<color=#446688><i>{Esc(msg.Body)}</i></color>";

            // Player message: coloured bold name then plain body
            return $"<b><color=#2ee6e6>{Esc(msg.SenderName)}</color></b>  {Esc(msg.Body)}";
        }

        /// <summary>Breaks rich-text tag recognition in user-supplied strings.</summary>
        private static string Esc(string s) => s.Replace("<", "<\u200b");

        // ── Style init ────────────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            // Header: "CHAT" in cyan
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = new Color(0.20f, 0.90f, 0.90f) }
            };

            // Message body — rich text enabled, word wrap on, no extra background
            _msgStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                wordWrap  = true,
                richText  = true,
                alignment = TextAnchor.UpperLeft,
                padding   = new RectOffset(2, 2, 1, 1),
                normal    = { textColor = new Color(0.82f, 0.90f, 0.93f), background = null }
            };

            // Input field — dark background, light text
            _inputBgTex = MakeTex(new Color(0.08f, 0.10f, 0.15f, 1f));
            _inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 11,
                normal   = { textColor = new Color(0.85f, 0.92f, 0.96f), background = _inputBgTex },
                focused  = { textColor = new Color(0.92f, 0.97f, 1.00f), background = _inputBgTex },
                hover    = { textColor = new Color(0.85f, 0.92f, 0.96f), background = _inputBgTex }
            };

            // Send button — dark with cyan text
            _btnBgTex   = MakeTex(new Color(0.10f, 0.22f, 0.28f, 1f));
            _btnHoverTex = MakeTex(new Color(0.14f, 0.30f, 0.36f, 1f));
            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = new Color(0.20f, 0.90f, 0.90f), background = _btnBgTex   },
                hover     = { textColor = new Color(0.40f, 1.00f, 1.00f), background = _btnHoverTex },
                active    = { textColor = new Color(0.20f, 0.90f, 0.90f), background = _btnBgTex   }
            };
        }

        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
