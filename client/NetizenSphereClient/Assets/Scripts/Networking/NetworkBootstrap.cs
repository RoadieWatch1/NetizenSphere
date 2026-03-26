using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace NetizenSphere.Networking
{
    public class NetworkBootstrap : MonoBehaviour
    {
        private string _nameInput = "";

        private void Awake()
        {
            var transport = GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("NetworkBootstrap: No UnityTransport component found on this GameObject.", this);
                return;
            }

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;

            _nameInput = PlayerPrefs.GetString("DisplayName", "");
        }

        private void OnGUI()
        {
            if (NetworkManager.Singleton == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 220, 140));

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                GUILayout.Label("Display Name:");
                _nameInput = GUILayout.TextField(_nameInput, 24, GUILayout.Width(200));

                if (GUILayout.Button("Host"))
                {
                    SaveName();
                    NetworkManager.Singleton.StartHost();
                }

                if (GUILayout.Button("Client"))
                {
                    SaveName();
                    NetworkManager.Singleton.StartClient();
                }

                if (GUILayout.Button("Server"))
                {
                    SaveName();
                    NetworkManager.Singleton.StartServer();
                }
            }
            else
            {
                GUILayout.Label("Mode: " + (NetworkManager.Singleton.IsHost ? "Host" :
                                            NetworkManager.Singleton.IsServer ? "Server" : "Client"));
                GUILayout.Label("Connected: " + NetworkManager.Singleton.ConnectedClientsIds.Count);
            }

            GUILayout.EndArea();
        }

        private void SaveName()
        {
            string name = _nameInput.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;

            PlayerPrefs.SetString("DisplayName", name);
            PlayerPrefs.Save();
        }
    }
}
