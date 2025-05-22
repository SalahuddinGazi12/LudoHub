using UnityEngine;
using NativeWebSocket;
using System;
using UnityEditor.MemoryProfiler;

public class GameChat : MonoBehaviour, IMessageObserver
{
    [Header("WebSocket Settings")]
    [SerializeField] private string _serverUrl = "wss://game-api.soft-360.com";

    [Header("Dependencies")]
    [SerializeField] private ChatUI _chatUI;
    [SerializeField] private ConnectionUI _connectionUI;

    private WebSocket _websocket;
    private MessageSubject _messageSubject = new();
    private string _playerId;
    private bool _isConnected = false;

    private void Awake()
    {
        // Validate dependencies
        if (_chatUI == null)
            Debug.LogError("ChatUI reference not set in GameChat!");

        if (_connectionUI == null)
            Debug.LogError("ConnectionUI reference not set in GameChat!");
    }

    private void Start()
    {
        // Register observers
        _messageSubject.AddObserver(this);
        _messageSubject.AddObserver(_chatUI);

        // Setup UI events
        _connectionUI.OnConnectClicked += HandleConnectRequest;
        _chatUI.OnMessageSent += HandleOutgoingMessage;
    }

    private void OnDestroy()
    {
        // Clean up WebSocket
        Disconnect();

        // Unsubscribe from events
        if (_connectionUI != null)
            _connectionUI.OnConnectClicked -= HandleConnectRequest;

        if (_chatUI != null)
            _chatUI.OnMessageSent -= HandleOutgoingMessage;
    }

    private async void HandleConnectRequest(string playerName)
    {
        if (_isConnected) return;

        _playerId = playerName.Trim();

        try
        {
            _websocket = new WebSocket(_serverUrl);

            _websocket.OnOpen += () =>
            {
                _isConnected = true;
                _messageSubject.NotifyObservers(new SystemMessage("Connected to server!"));

                // Register player with server
                SendWebSocketMessage("set_id", new { id = _playerId });
                SendWebSocketMessage("join", new { room = "default" });
            };

            _websocket.OnMessage += (bytes) =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                HandleIncomingMessage(message);
            };

            _websocket.OnError += (error) =>
            {
                _messageSubject.NotifyObservers(new SystemMessage($"Error: {error}"));
            };

            _websocket.OnClose += (code) =>
            {
                _isConnected = false;
                _messageSubject.NotifyObservers(new SystemMessage("Disconnected from server"));
            };

            await _websocket.Connect();
        }
        catch (Exception ex)
        {
            _messageSubject.NotifyObservers(new SystemMessage($"Connection failed: {ex.Message}"));
        }
    }

    private void HandleIncomingMessage(string json)
    {
        try
        {
            var msg = JsonUtility.FromJson<NetworkMessage>(json);

            ChatMessage chatMessage = msg.type switch
            {
                "message" => new TextMessage(msg.sender, msg.content),
                "player_joined" => new JoinLeaveMessage(msg.playerId, true),
                "player_left" => new JoinLeaveMessage(msg.playerId, false),
                _ => new SystemMessage($"Unknown message type: {msg.type}")
            };

            _messageSubject.NotifyObservers(chatMessage);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse message: {ex.Message}");
        }
    }

    private void HandleOutgoingMessage(string message)
    {
        if (!_isConnected || string.IsNullOrWhiteSpace(message)) return;

        SendWebSocketMessage("message", new
        {
            message = message,
            sender = _playerId,
            room = "default"
        });
    }

    private void SendWebSocketMessage(string type, object payload)
    {
        if (!_isConnected) return;

        var message = new { type, payload };
        string json = JsonUtility.ToJson(message);
        _websocket.SendText(json);
    }

    private async void Disconnect()
    {
        if (_websocket != null && _websocket.State == WebSocketState.Open)
        {
            await _websocket.Close();
        }
    }

    public void OnMessageReceived(ChatMessage message)
    {
        // GameChat-specific message handling
        switch (message)
        {
            case JoinLeaveMessage joinLeave:
                Debug.Log($"Player {joinLeave.Sender} {(joinLeave.IsJoining ? "joined" : "left")}");
                break;

            case SystemMessage systemMsg:
                Debug.Log($"System event: {systemMsg.Event}");
                break;
        }
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        _websocket?.DispatchMessageQueue();
#endif
    }
}