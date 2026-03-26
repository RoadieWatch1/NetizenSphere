using Unity.Netcode;
using UnityEngine;

namespace NetizenSphere.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private Transform spawnPoint;

        private GameObject _playerPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Screen.SetResolution(960, 540, FullScreenMode.Windowed);

            _playerPrefab = Resources.Load<GameObject>("Player");

            if (_playerPrefab == null)
                Debug.LogError("GameManager: Could not load 'Player' from Resources. Make sure Player.prefab is in Assets/Resources/.", this);
            else
                Debug.Log("GameManager: Player prefab loaded from Resources.");
        }

        private void Start()
        {
            if (_playerPrefab == null)
                return;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            Debug.Log($"GameManager: Client {clientId} connected — spawning player.");

            Vector3 position = spawnPoint != null ? spawnPoint.position : new Vector3(0f, 1f, 0f);
            GameObject player = Instantiate(_playerPrefab, position, Quaternion.identity);
            NetworkObject netObj = player.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError("GameManager: Player prefab is missing a NetworkObject component!", this);
                Destroy(player);
                return;
            }

            netObj.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"GameManager: Player spawned for client {clientId}.");
        }
    }
}
