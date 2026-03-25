using Unity.Netcode;
using UnityEngine;

namespace NetizenSphere.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;

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
                Debug.LogError("GameManager: playerPrefab is not assigned.", this);
                return;
            }

            // Assign the player prefab to NGO's NetworkConfig so auto-spawn works.
            // The PlayerPrefab YAML field sits inside NetworkConfig, which we cannot
            // reliably hand-edit; setting it here in code is the safe path.
            NetworkManager.Singleton.NetworkConfig.PlayerPrefab = playerPrefab;

            Debug.Log("GameManager: PlayerPrefab assigned to NetworkConfig.");
        }
    }
}
