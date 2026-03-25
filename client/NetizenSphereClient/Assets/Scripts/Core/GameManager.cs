using UnityEngine;
using NetizenSphere.Player;

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

            if (Camera.main != null)
            {
                Debug.Log("GameManager: Camera.main found — " + Camera.main.gameObject.name);
                CameraFollow cameraFollow = Camera.main.GetComponent<CameraFollow>();
                if (cameraFollow == null)
                {
                    Debug.Log("GameManager: Adding CameraFollow component.");
                    cameraFollow = Camera.main.gameObject.AddComponent<CameraFollow>();
                }
                cameraFollow.SetTarget(_spawnedPlayer.transform);
                Debug.Log("GameManager: CameraFollow target set to " + _spawnedPlayer.name);
            }
            else
            {
                Debug.LogError("GameManager: Camera.main is NULL — Main Camera tag missing?");
            }

            Debug.Log("GameManager: Local player spawned.");
        }

        public GameObject GetLocalPlayer() => _spawnedPlayer;
    }
}
