using Unity.Collections;
using Unity.Netcode;
using NetizenSphere.Services;
using UnityEngine;

namespace NetizenSphere.Player
{
    public class PlayerIdentity : NetworkBehaviour
    {
        public NetworkVariable<FixedString64Bytes> DisplayName = new(
            "Netizen",
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [SerializeField] private GameObject nameplatePrefab;

        private PlayerNameplate _nameplateInstance;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                SubmitDisplayNameServerRpc(GetLocalDisplayName());
            }

            CreateNameplate();
        }

        public override void OnNetworkDespawn()
        {
            DisplayName.OnValueChanged -= OnNameChanged;

            if (_nameplateInstance != null)
            {
                Destroy(_nameplateInstance.gameObject);
            }
        }

        private void CreateNameplate()
        {
            if (nameplatePrefab == null)
            {
                Debug.LogWarning("PlayerIdentity: nameplatePrefab not assigned.", this);
                return;
            }

            GameObject instance = Instantiate(nameplatePrefab);
            _nameplateInstance = instance.GetComponent<PlayerNameplate>();

            if (_nameplateInstance != null)
            {
                _nameplateInstance.SetTarget(transform);
                _nameplateInstance.SetName(DisplayName.Value.ToString());
            }

            DisplayName.OnValueChanged += OnNameChanged;
        }

        private void OnNameChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
        {
            if (_nameplateInstance != null)
            {
                _nameplateInstance.SetName(newName.ToString());
            }
        }

        private string GetLocalDisplayName()
        {
            if (SessionManager.Instance != null && SessionManager.Instance.IsSignedIn)
            {
                string sessionName = SessionManager.Instance.DisplayName;

                if (!string.IsNullOrWhiteSpace(sessionName))
                {
                    return sessionName.Trim();
                }
            }

            string savedName = PlayerPrefs.GetString("DisplayName", string.Empty);

            if (string.IsNullOrWhiteSpace(savedName))
            {
                savedName = $"Netizen{Random.Range(1000, 9999)}";
                PlayerPrefs.SetString("DisplayName", savedName);
                PlayerPrefs.Save();
            }

            return savedName.Trim();
        }

        [ServerRpc]
        private void SubmitDisplayNameServerRpc(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return;

            string trimmedName = newName.Trim();

            if (trimmedName.Length > 24)
                trimmedName = trimmedName.Substring(0, 24);

            DisplayName.Value = trimmedName;
        }
    }
}
