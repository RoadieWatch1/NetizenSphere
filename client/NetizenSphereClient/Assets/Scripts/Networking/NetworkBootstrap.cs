using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace NetizenSphere.Networking
{
    public class NetworkBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            // Wire up the transport to NetworkConfig in code — hand-written scene
            // YAML cannot reliably reach inside the nested NetworkConfig object.
            var transport = GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("NetworkBootstrap: No UnityTransport component found on this GameObject.", this);
                return;
            }

            NetworkManager.Singleton.NetworkConfig.NetworkTransport = transport;
            Debug.Log("NetworkBootstrap: UnityTransport assigned to NetworkConfig.");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Host"))
                    NetworkManager.Singleton.StartHost();

                if (GUILayout.Button("Client"))
                    NetworkManager.Singleton.StartClient();

                if (GUILayout.Button("Server"))
                    NetworkManager.Singleton.StartServer();
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
