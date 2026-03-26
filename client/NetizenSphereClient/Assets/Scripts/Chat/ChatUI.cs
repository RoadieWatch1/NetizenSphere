using NetizenSphere.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NetizenSphere.Chat
{
    public class ChatUI : MonoBehaviour
    {
        private string _input = "";
        private Vector2 _scroll;

        private const float Width = 350f;
        private const float Height = 200f;
        private const float InputHeight = 30f;
        private const float Margin = 10f;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame && !string.IsNullOrWhiteSpace(_input))
                Submit();
        }

        private void OnGUI()
        {
            if (ChatManager.Instance == null || !ChatManager.Instance.IsSpawned)
                return;

            float x = Margin;
            float y = Screen.height - Height - InputHeight - Margin * 2;

            // Message scroll area
            GUI.Box(new Rect(x, y, Width, Height), "");
            GUILayout.BeginArea(new Rect(x + 4, y + 4, Width - 8, Height - 8));
            _scroll = GUILayout.BeginScrollView(_scroll);

            foreach (string msg in ChatManager.Instance.GetMessages())
                GUILayout.Label(msg);

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Input field
            float inputY = y + Height + Margin;
            GUI.SetNextControlName("ChatInput");
            _input = GUI.TextField(new Rect(x, inputY, Width - 60, InputHeight), _input, 128);

            if (GUI.Button(new Rect(x + Width - 56, inputY, 56, InputHeight), "Send"))
                Submit();
        }

        private void Submit()
        {
            string name = GetLocalName();
            ChatManager.Instance.SendMessage(name, _input);
            _input = "";
        }

        private string GetLocalName()
        {
            string saved = PlayerPrefs.GetString("DisplayName", "");
            return string.IsNullOrWhiteSpace(saved) ? "Netizen" : saved;
        }
    }
}
