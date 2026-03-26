using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using NetizenSphere.Player;
using NetizenSphere.UI;

namespace NetizenSphere.Social
{
    public struct PlayerRosterEntry
    {
        public string DisplayName;
        public bool IsLocal;
        public ulong ClientId;
    }

    /// <summary>
    /// Tracks active PlayerIdentity instances and maintains a live roster.
    /// Self-bootstraps via RuntimeInitializeOnLoadMethod — no scene setup required.
    /// </summary>
    public class PlayerPresenceManager : MonoBehaviour
    {
        public static PlayerPresenceManager Instance { get; private set; }

        /// <summary>Raised whenever the roster list changes.</summary>
        public static event Action OnRosterChanged;

        private readonly List<PlayerRosterEntry> _roster = new();
        private bool _subscribedToNGO;

        public IReadOnlyList<PlayerRosterEntry> Roster => _roster;

        // ── Bootstrap ────────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var go = new GameObject("[PlayerPresence]");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerPresenceManager>();
            go.AddComponent<PlayerRosterUI>();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(PollRoster());
        }

        private void OnDestroy()
        {
            if (_subscribedToNGO && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientEvent;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientEvent;
            }
        }

        // ── NGO callbacks ────────────────────────────────────────────────────────

        private void OnClientEvent(ulong _) => StartCoroutine(DelayedRefresh());

        private IEnumerator DelayedRefresh()
        {
            // Wait a short moment so the spawned PlayerIdentity has time to appear
            yield return new WaitForSeconds(0.3f);
            RefreshRoster();
        }

        // ── Poll loop ────────────────────────────────────────────────────────────

        private IEnumerator PollRoster()
        {
            while (true)
            {
                // Subscribe to NGO as soon as NetworkManager is available
                if (!_subscribedToNGO && NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback  += OnClientEvent;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientEvent;
                    _subscribedToNGO = true;
                }

                RefreshRoster();
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ── Roster refresh ───────────────────────────────────────────────────────

        private void RefreshRoster()
        {
            _roster.Clear();

            var identities = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

            foreach (var identity in identities)
            {
                if (!identity.IsSpawned) continue;

                string name = identity.DisplayName.Value.ToString();
                if (string.IsNullOrWhiteSpace(name)) name = "Netizen";

                _roster.Add(new PlayerRosterEntry
                {
                    DisplayName = name,
                    IsLocal    = identity.IsOwner,
                    ClientId   = identity.OwnerClientId
                });
            }

            // Local player first, then alphabetical
            _roster.Sort((a, b) =>
            {
                if (a.IsLocal != b.IsLocal) return a.IsLocal ? -1 : 1;
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });

            OnRosterChanged?.Invoke();
        }
    }
}
