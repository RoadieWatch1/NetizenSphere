using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace NetizenSphere.Networking
{
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("Connection")]
        [SerializeField] private string address = "127.0.0.1";
        [SerializeField] private ushort port    = 7777;

        [Header("UI")]
        [SerializeField] private bool showOverlay = true;

        private UnityTransport _transport;
        private string _statusMessage = string.Empty;

        private void Awake()
        {
            _transport = GetComponent<UnityTransport>();
            if (_transport == null)
            {
                Debug.LogError("NetworkBootstrap: No UnityTransport component found on this GameObject.", this);
                _statusMessage = "Missing UnityTransport on NetworkBootstrap object.";
                return;
            }

            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("NetworkBootstrap: No NetworkManager.Singleton found in scene.", this);
                _statusMessage = "Missing NetworkManager in scene.";
                return;
            }

            ApplyConnectionSettings();
            NetworkManager.Singleton.NetworkConfig.NetworkTransport = _transport;
        }

        private void ApplyConnectionSettings()
        {
            if (_transport == null) return;
            _transport.SetConnectionData(address, port);
        }

        private void OnGUI()
        {
            if (!showOverlay || NetworkManager.Singleton == null)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 180), GUI.skin.box);

            GUILayout.Label("NetizenSphere Network");
            GUILayout.Label($"Address: {address}");
            GUILayout.Label($"Port: {port}");

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(6);
                GUILayout.Label("Status: " + _statusMessage);
            }

            GUILayout.Space(8);

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                if (GUILayout.Button("Host"))   TryStartHost();
                if (GUILayout.Button("Client")) TryStartClient();
                if (GUILayout.Button("Server")) TryStartServer();
            }
            else
            {
                GUILayout.Label("Mode: " + (NetworkManager.Singleton.IsHost   ? "Host" :
                                            NetworkManager.Singleton.IsServer ? "Server" : "Client"));
                GUILayout.Label("Connected: " + NetworkManager.Singleton.ConnectedClientsIds.Count);

                GUILayout.Space(8);

                if (GUILayout.Button("Shutdown"))
                    ShutdownNetwork();
            }

            GUILayout.EndArea();
        }

        private void TryStartHost()
        {
            if (!CanStartNetwork()) return;

            ApplyConnectionSettings();

            bool started = NetworkManager.Singleton.StartHost();
            if (!started)
            {
                _statusMessage = $"Failed to start Host on port {port}. Port may already be in use.";
                Debug.LogError($"[NetworkBootstrap] Host start failed on {address}:{port}. Port may already be in use.");
                SafeShutdownAfterFailedStart();
                return;
            }

            _statusMessage = $"Hosting on {address}:{port}";
            Debug.Log($"[NetworkBootstrap] Host started on {address}:{port}");
        }

        private void TryStartClient()
        {
            if (!CanStartNetwork()) return;

            ApplyConnectionSettings();

            bool started = NetworkManager.Singleton.StartClient();
            if (!started)
            {
                _statusMessage = $"Failed to start Client for {address}:{port}.";
                Debug.LogError($"[NetworkBootstrap] Client start failed for {address}:{port}");
                SafeShutdownAfterFailedStart();
                return;
            }

            _statusMessage = $"Client connecting to {address}:{port}";
            Debug.Log($"[NetworkBootstrap] Client starting for {address}:{port}");
        }

        private void TryStartServer()
        {
            if (!CanStartNetwork()) return;

            ApplyConnectionSettings();

            bool started = NetworkManager.Singleton.StartServer();
            if (!started)
            {
                _statusMessage = $"Failed to start Server on port {port}. Port may already be in use.";
                Debug.LogError($"[NetworkBootstrap] Server start failed on {address}:{port}. Port may already be in use.");
                SafeShutdownAfterFailedStart();
                return;
            }

            _statusMessage = $"Server listening on {address}:{port}";
            Debug.Log($"[NetworkBootstrap] Server started on {address}:{port}");
        }

        private bool CanStartNetwork()
        {
            if (_transport == null)
            {
                _statusMessage = "Cannot start network: UnityTransport missing.";
                Debug.LogError("[NetworkBootstrap] Cannot start network because UnityTransport is missing.");
                return false;
            }

            if (NetworkManager.Singleton == null)
            {
                _statusMessage = "Cannot start network: NetworkManager missing.";
                Debug.LogError("[NetworkBootstrap] Cannot start network because NetworkManager.Singleton is missing.");
                return false;
            }

            return true;
        }

        private void SafeShutdownAfterFailedStart()
        {
            if (NetworkManager.Singleton == null) return;

            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
                NetworkManager.Singleton.Shutdown();
        }

        private void ShutdownNetwork()
        {
            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.Shutdown();
            _statusMessage = "Network shut down.";
            Debug.Log("[NetworkBootstrap] Network shut down.");
        }
    }
}
