using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace NetizenSphere.Networking
{
    public class NetworkBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var transport = GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("NetworkBootstrap: No UnityTransport component found on this GameObject.", this);
                return;
            }

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;
        }

        private void OnGUI()
        {
            if (NetworkManager.Singleton == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 220, 100));

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Host"))   NetworkManager.Singleton.StartHost();
                if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
                if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
            }
            else
            {
                GUILayout.Label("Mode: " + (NetworkManager.Singleton.IsHost ? "Host" :
                                            NetworkManager.Singleton.IsServer ? "Server" : "Client"));
                GUILayout.Label("Connected: " + NetworkManager.Singleton.ConnectedClientsIds.Count);
            }

            GUILayout.EndArea();
        }
    }
}
