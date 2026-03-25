using Unity.Netcode;
using UnityEngine;

namespace NetizenSphere.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("GameManager: playerPrefab is NOT assigned — drag Player prefab onto GameManager in Inspector.", this);
                return;
            }

            Debug.Log("GameManager: playerPrefab is assigned. Waiting for network start.");
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

            Debug.Log($"GameManager: Client {clientId} connected. Spawning player.");

            Vector3 position = spawnPoint != null ? spawnPoint.position : new Vector3(0f, 1f, 0f);
            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
            NetworkObject netObj = player.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError("GameManager: Player prefab is missing a NetworkObject component!", this);
                return;
            }

            netObj.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"GameManager: Player spawned for client {clientId}.");
        }
    }
}
