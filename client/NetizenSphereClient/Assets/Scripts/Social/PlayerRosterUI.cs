using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using NetizenSphere.Social;

namespace NetizenSphere.UI
{
    /// <summary>
    /// Renders the live player roster panel in the top-right corner.
    /// Added automatically by PlayerPresenceManager.Bootstrap().
    /// Only visible when a network session is active.
    /// </summary>
    public class PlayerRosterUI : MonoBehaviour
    {
        private readonly List<PlayerRosterEntry> _displayRoster = new();

        // Panel layout constants
        private const float PanelWidth    = 210f;
        private const float HeaderHeight  = 28f;
        private const float RowHeight     = 22f;
        private const float BottomPad     = 8f;
        private const float ScreenMargin  = 10f;

        // GUIStyles — initialized inside OnGUI so GUI.skin is valid
        private GUIStyle _headerStyle;
        private GUIStyle _nameStyle;
        private GUIStyle _localNameStyle;
        private bool _stylesReady;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            PlayerPresenceManager.OnRosterChanged += SyncRoster;
        }

        private void OnDisable()
        {
            PlayerPresenceManager.OnRosterChanged -= SyncRoster;
        }

        private void SyncRoster()
        {
            _displayRoster.Clear();
            if (PlayerPresenceManager.Instance != null)
                _displayRoster.AddRange(PlayerPresenceManager.Instance.Roster);
        }

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            // Header: "ONLINE • N PLAYERS" in cyan
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize   = 11,
                fontStyle  = FontStyle.Bold,
                alignment  = TextAnchor.MiddleLeft,
                normal     = { textColor = new Color(0.20f, 0.90f, 0.90f) }
            };

            // Normal player name: light blue-white
            _nameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = new Color(0.82f, 0.90f, 0.93f) }
            };

            // Local player name: neon green-cyan + bold
            _localNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal    = { textColor = new Color(0.35f, 1.00f, 0.75f) }
            };
        }

        private void OnGUI()
        {
            // Only render during an active network session
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return;

            InitStyles();

            int count = _displayRoster.Count;

            // Panel height grows with player count (minimum 1 row so it never collapses)
            float panelHeight = HeaderHeight + RowHeight * Mathf.Max(count, 1) + BottomPad;
            float x = Screen.width - PanelWidth - ScreenMargin;
            float y = ScreenMargin;

            // ── Background ──────────────────────────────────────────────────────
            var bgRect = new Rect(x - 8f, y - 4f, PanelWidth + 16f, panelHeight + 4f);
            GUI.color = new Color(0.04f, 0.06f, 0.10f, 0.87f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Top accent bar (cyan) ────────────────────────────────────────────
            GUI.color = new Color(0.20f, 0.90f, 0.90f, 0.85f);
            GUI.DrawTexture(new Rect(bgRect.x, bgRect.y, bgRect.width, 2f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // ── Header ──────────────────────────────────────────────────────────
            string countLabel = count == 1 ? "1 PLAYER" : $"{count} PLAYERS";
            GUI.Label(new Rect(x, y + 4f, PanelWidth, HeaderHeight),
                $"ONLINE  \u2022  {countLabel}", _headerStyle);

            // ── Player rows ──────────────────────────────────────────────────────
            if (count == 0)
            {
                GUI.Label(new Rect(x, y + HeaderHeight, PanelWidth, RowHeight),
                    "\u2014", _nameStyle);   // em-dash placeholder
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var entry  = _displayRoster[i];
                    float rowY = y + HeaderHeight + i * RowHeight;
                    string label = entry.IsLocal
                        ? $"{entry.DisplayName}  (You)"
                        : entry.DisplayName;

                    GUI.Label(new Rect(x, rowY, PanelWidth, RowHeight),
                        label, entry.IsLocal ? _localNameStyle : _nameStyle);
                }
            }
        }
    }
}
