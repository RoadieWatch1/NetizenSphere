using UnityEngine;

namespace NetizenSphere.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;

        private GameObject _spawnedPlayer;

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
            SpawnLocalPlayer();
        }

        public void SpawnLocalPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("GameManager: playerPrefab is not assigned.", this);
                return;
            }

            Vector3 position = spawnPoint != null ? spawnPoint.position : new Vector3(0f, 1f, 0f);
            _spawnedPlayer = Instantiate(playerPrefab, position, Quaternion.identity);
            _spawnedPlayer.name = "LocalPlayer";

            Debug.Log("GameManager: Local player spawned.");
        }

        public GameObject GetLocalPlayer() => _spawnedPlayer;
    }
}
