using UnityEngine;

namespace NetizenSphere.UI
{
    /// <summary>
    /// Shared visual palette for all NetizenSphere UI panels.
    /// Static constants only — no MonoBehaviour, no dependencies.
    /// </summary>
    public static class UITheme
    {
        // ── Background ────────────────────────────────────────────────────────────

        /// <summary>Full-screen login/scene background.</summary>
        public static readonly Color ScreenBg      = new Color(0.02f, 0.03f, 0.06f, 1.00f);

        /// <summary>In-world overlay panels (chat, roster) — slightly transparent.</summary>
        public static readonly Color PanelBg       = new Color(0.04f, 0.06f, 0.10f, 0.87f);

        /// <summary>Hero panels (login) — near-opaque.</summary>
        public static readonly Color PanelBgSolid  = new Color(0.04f, 0.06f, 0.10f, 0.96f);

        // ── Accent ────────────────────────────────────────────────────────────────

        public static readonly Color Cyan          = new Color(0.20f, 0.90f, 0.90f, 1.00f);
        public static readonly Color CyanDim       = new Color(0.20f, 0.90f, 0.90f, 0.85f);
        public static readonly Color CyanFaint     = new Color(0.20f, 0.90f, 0.90f, 0.15f);
        public static readonly Color LocalPlayer   = new Color(0.35f, 1.00f, 0.75f, 1.00f);

        // ── Text ──────────────────────────────────────────────────────────────────

        public static readonly Color TextPrimary   = new Color(0.85f, 0.92f, 0.96f, 1.00f);
        public static readonly Color TextMuted     = new Color(0.52f, 0.62f, 0.70f, 1.00f);
        public static readonly Color TextError     = new Color(1.00f, 0.38f, 0.38f, 1.00f);

        // ── Controls ─────────────────────────────────────────────────────────────

        public static readonly Color InputBg       = new Color(0.08f, 0.10f, 0.15f, 1.00f);
        public static readonly Color ButtonBg      = new Color(0.10f, 0.22f, 0.28f, 1.00f);
        public static readonly Color ButtonHover   = new Color(0.14f, 0.30f, 0.36f, 1.00f);

        // ── Utility ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a 1×1 pixel Texture2D of the given color.
        /// Caller is responsible for calling Destroy() when done.
        /// </summary>
        public static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
