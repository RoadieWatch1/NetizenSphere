using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace NetizenSphere.Chat
{
    public class ChatManager : NetworkBehaviour
    {
        public static ChatManager Instance { get; private set; }

        private readonly List<string> _messages = new();

        public delegate void OnMessageReceived(string message);
        public event OnMessageReceived MessageReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SendMessage(string senderName, string text)
        {
            if (!IsSpawned)
                return;

            string trimmed = text.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            SendMessageServerRpc(senderName, trimmed);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendMessageServerRpc(string senderName, string text)
        {
            string formatted = $"{senderName}: {text}";
            BroadcastMessageClientRpc(formatted);
        }

        [ClientRpc]
        private void BroadcastMessageClientRpc(string formatted)
        {
            _messages.Add(formatted);

            if (_messages.Count > 50)
                _messages.RemoveAt(0);

            MessageReceived?.Invoke(formatted);
            Debug.Log($"[Chat] {formatted}");
        }

        public IReadOnlyList<string> GetMessages() => _messages;
    }
}
