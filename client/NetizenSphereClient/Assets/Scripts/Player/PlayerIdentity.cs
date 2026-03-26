using Unity.Collections;
using Unity.Netcode;
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

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                SubmitDisplayNameServerRpc(GetLocalDisplayName());
            }
        }

        private string GetLocalDisplayName()
        {
            string savedName = PlayerPrefs.GetString("DisplayName", string.Empty);

            if (string.IsNullOrWhiteSpace(savedName))
            {
                savedName = $"Netizen{Random.Range(1000, 9999)}";
                PlayerPrefs.SetString("DisplayName", savedName);
                PlayerPrefs.Save();
            }

            return savedName;
        }

        [ServerRpc]
        private void SubmitDisplayNameServerRpc(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            string trimmedName = newName.Trim();

            if (trimmedName.Length > 24)
            {
                trimmedName = trimmedName.Substring(0, 24);
            }

            DisplayName.Value = trimmedName;
        }
    }
}
