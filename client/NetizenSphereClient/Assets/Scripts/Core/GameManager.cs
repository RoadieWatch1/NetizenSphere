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

            if (playerPrefab == null)
            {
                Debug.LogError("GameManager: playerPrefab is not assigned.", this);
                return;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : new Vector3(0f, 1f, 0f);
            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
            NetworkObject networkObject = player.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId, true);

            Debug.Log($"GameManager: Spawned player for client {clientId}.");
        }
    }
}
