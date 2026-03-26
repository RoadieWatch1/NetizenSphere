using NetizenSphere.Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetizenSphere.UI
{
    public class LoginUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private string bootSceneName = "Boot";

        private void Start()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.IsSignedIn)
            {
                nameInput.text = SessionManager.Instance.DisplayName;
            }

            if (errorText != null)
            {
                errorText.text = string.Empty;
            }
        }

        public void OnContinuePressed()
        {
            if (nameInput == null)
            {
                Debug.LogError("Name input is not assigned.", this);
                return;
            }

            string enteredName = nameInput.text.Trim();

            if (string.IsNullOrWhiteSpace(enteredName))
            {
                ShowError("Please enter a display name.");
                return;
            }

            if (enteredName.Length > 24)
            {
                enteredName = enteredName.Substring(0, 24);
            }

            if (SessionManager.Instance == null)
            {
                ShowError("Session manager not found.");
                return;
            }

            SessionManager.Instance.SignIn(enteredName);
            SceneManager.LoadScene(bootSceneName);
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
            }
            else
            {
                Debug.LogWarning(message, this);
            }
        }
    }
}
