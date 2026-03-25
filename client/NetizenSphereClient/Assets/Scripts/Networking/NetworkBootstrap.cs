using Unity.Netcode;
using UnityEngine;

namespace NetizenSphere.Networking
{
    public class NetworkBootstrap : MonoBehaviour
    {
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
