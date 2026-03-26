using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using NetizenSphere.Player;

namespace NetizenSphere.Chat
{
    public enum ChatMessageType { Player, System }

    public readonly struct ChatMessage
    {
        public readonly ChatMessageType Type;
        public readonly string SenderName; // empty for system messages
        public readonly string Body;

        public ChatMessage(ChatMessageType type, string senderName, string body)
        {
            Type       = type;
            SenderName = senderName;
            Body       = body;
        }
    }

    public class ChatManager : NetworkBehaviour
    {
        public static ChatManager Instance { get; private set; }

        private readonly List<ChatMessage> _messages   = new();
        private readonly Dictionary<ulong, string> _clientNames = new(); // server-only

        public delegate void OnMessageReceived(ChatMessage message);
        public event OnMessageReceived MessageReceived;

        private const int MaxMessages = 50;

        // ── Singleton ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Network lifecycle ─────────────────────────────────────────────────────

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            NetworkManager.OnClientConnectedCallback  += OnServerClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnServerClientDisconnected;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer) return;

            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback  -= OnServerClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnServerClientDisconnected;
            }
        }

        // ── Server-side join / leave ──────────────────────────────────────────────

        private void OnServerClientConnected(ulong clientId)
        {
            StartCoroutine(AnnounceJoinWhenReady(clientId));
        }

        private void OnServerClientDisconnected(ulong clientId)
        {
            _clientNames.TryGetValue(clientId, out string name);
            _clientNames.Remove(clientId);

            string displayName = string.IsNullOrWhiteSpace(name) ? "A player" : name;
            BroadcastSystemMessageClientRpc($"{displayName} left the hub");
        }

        /// <summary>
        /// Waits for the connecting client's PlayerIdentity to receive its synced
        /// display name, then broadcasts a system join message.
        /// </summary>
        private IEnumerator AnnounceJoinWhenReady(ulong clientId)
        {
            // Minimum wait: lets the player object spawn and SubmitDisplayNameServerRpc
            // travel back to the server before we read the name.
            yield return new WaitForSeconds(0.8f);

            string name    = null;
            float elapsed  = 0.8f;
            const float Timeout = 5f;

            while (elapsed < Timeout)
            {
                var identities = FindObjectsByType<PlayerIdentity>();
                foreach (var id in identities)
                {
                    if (!id.IsSpawned || id.OwnerClientId != clientId) continue;
                    name = id.DisplayName.Value.ToString();
                    if (string.IsNullOrWhiteSpace(name)) name = "Netizen";
                    break;
                }

                if (name != null) break;

                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }

            if (name == null) name = "Netizen";
            _clientNames[clientId] = name;
            BroadcastSystemMessageClientRpc($"{name} joined the hub");
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void SendMessage(string senderName, string text)
        {
            if (!IsSpawned) return;

            string trimmed = text.Trim();
            if (string.IsNullOrEmpty(trimmed)) return;

            SendMessageServerRpc(senderName, trimmed);
        }

        public IReadOnlyList<ChatMessage> GetMessages() => _messages;

        // ── RPCs ──────────────────────────────────────────────────────────────────

        [ServerRpc(RequireOwnership = false)]
        private void SendMessageServerRpc(string senderName, string body)
        {
            BroadcastMessageClientRpc(senderName, body);
        }

        [ClientRpc]
        private void BroadcastMessageClientRpc(string senderName, string body)
        {
            AddMessage(new ChatMessage(ChatMessageType.Player, senderName, body));
        }

        [ClientRpc]
        private void BroadcastSystemMessageClientRpc(string text)
        {
            AddMessage(new ChatMessage(ChatMessageType.System, string.Empty, text));
        }

        // ── Internal ──────────────────────────────────────────────────────────────

        private void AddMessage(ChatMessage msg)
        {
            _messages.Add(msg);

            if (_messages.Count > MaxMessages)
                _messages.RemoveAt(0);

            MessageReceived?.Invoke(msg);
            Debug.Log($"[Chat] [{msg.Type}] {(msg.Type == ChatMessageType.Player ? msg.SenderName + ": " : string.Empty)}{msg.Body}");
        }
    }
}
