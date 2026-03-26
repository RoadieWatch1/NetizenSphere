using UnityEngine;

namespace NetizenSphere.Services
{
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        public string DisplayName { get; private set; } = string.Empty;
        public bool IsSignedIn { get; private set; }

        private const string DisplayNameKey = "DisplayName";

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

        public void SignIn(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return;
            }

            DisplayName = displayName.Trim();
            IsSignedIn = true;

            PlayerPrefs.SetString(DisplayNameKey, DisplayName);
            PlayerPrefs.Save();
        }

        public void SignOut()
        {
            DisplayName = string.Empty;
            IsSignedIn = false;

            PlayerPrefs.DeleteKey(DisplayNameKey);
            PlayerPrefs.Save();
        }

        private void LoadSession()
        {
            string savedName = PlayerPrefs.GetString(DisplayNameKey, string.Empty);

            if (!string.IsNullOrWhiteSpace(savedName))
            {
                DisplayName = savedName;
                IsSignedIn = true;
            }
        }
    }
}
