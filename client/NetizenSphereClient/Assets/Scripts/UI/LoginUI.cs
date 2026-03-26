using NetizenSphere.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetizenSphere.UI
{
    public class LoginUI : MonoBehaviour
    {
        [SerializeField] private string bootSceneName = "Boot";

        private string _nameInput = "";
        private string _error = "";

        private void Start()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.IsSignedIn)
                _nameInput = SessionManager.Instance.DisplayName;
        }

        private void OnGUI()
        {
            float w = 300f;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height / 2f - 60f;

            GUILayout.BeginArea(new Rect(x, y, w, 160f));
            GUILayout.Label("Display Name:");
            _nameInput = GUILayout.TextField(_nameInput, 24, GUILayout.Width(280f));

            if (!string.IsNullOrEmpty(_error))
                GUILayout.Label(_error);

            if (GUILayout.Button("Enter World", GUILayout.Width(280f), GUILayout.Height(40f)))
                TryContinue();

            GUILayout.EndArea();
        }

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
                _error = "Session manager not found.";
                return;
            }

            SessionManager.Instance.SignIn(name);
            SceneManager.LoadScene(bootSceneName);
        }

        // Called by ContinueButton in scene if still wired
        public void OnContinuePressed() => TryContinue();
    }
}
