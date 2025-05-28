using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;

public class SocketIOManager : MonoBehaviour
{
    public static SocketIOManager Instance { get; private set; }

    [SerializeField] private string serverUrl = "https://game-api.soft-360.com";

    private SocketIO _socket;
    private string _playerId;
    private string _currentRoomId;

    // Events
    public event Action OnConnected;
    public event Action<string> OnDisconnected;
    public event Action<string> OnError;
    public event Action<string> OnRoomCreated;
    public event Action<string> OnRoomJoined;
    public event Action<List<PlayerData>> OnPlayersUpdated;
    public event Action<GameStartData> OnGameStarted;

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

    public async void Connect(string playerId)
    {
        _playerId = playerId;

        try
        {
            _socket = new SocketIO(new Uri(serverUrl), new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 5,
                ReconnectionDelay = 1000,
                //EIO = 4 // Protocol version
            });

            // Setup event handlers
            _socket.OnConnected += (sender, e) =>
            {
                Debug.Log("Socket.IO connected!");
                OnConnected?.Invoke();

                // Send player ID to server
                _ = _socket.EmitAsync("set_id", new { playerId = _playerId });
            };

            _socket.OnDisconnected += (sender, e) =>
            {
                Debug.Log("Socket.IO disconnected!");
                OnDisconnected?.Invoke(e);
            };

            _socket.OnError += (sender, e) =>
            {
                Debug.LogError("Socket.IO error: " + e);
                OnError?.Invoke(e);
            };

            // Custom event handlers
            _socket.On("room_created", response =>
            {
                var data = response.GetValue<RoomCreatedData>();
                _currentRoomId = data.roomId;
                OnRoomCreated?.Invoke(data.roomCode);
            });

            _socket.On("room_joined", response =>
            {
                var data = response.GetValue<RoomJoinedData>();
                _currentRoomId = data.roomId;
                OnRoomJoined?.Invoke(data.roomId);
            });

            _socket.On("players_updated", response =>
            {
                var data = response.GetValue<PlayersUpdatedData>();
                OnPlayersUpdated?.Invoke(data.players);
            });

            _socket.On("game_started", response =>
            {
                var data = response.GetValue<GameStartData>();
                OnGameStarted?.Invoke(data);
            });

            await _socket.ConnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection failed: " + e.Message);
            OnError?.Invoke(e.Message);
        }
    }

    public async void JoinRandomMatch(string matchFee, int roomSize)
    {
        if (_socket == null || !_socket.Connected) return;

        await _socket.EmitAsync("join", new
        {
            game_mode = "Online",
            match_fee = matchFee,
            room_size = roomSize
        });
    }

    public async void CreatePrivateRoom(string matchFee, int roomSize)
    {
        if (_socket == null || !_socket.Connected) return;

        await _socket.EmitAsync("create_room", new
        {
            game_mode = "Private",
            match_fee = matchFee,
            room_size = roomSize
        });
    }

    public async void JoinPrivateRoom(string roomCode)
    {
        if (_socket == null || !_socket.Connected) return;

        await _socket.EmitAsync("join_with_code", new
        {
            game_code = roomCode
        });
    }

    public bool IsConnected()
    {
        return _socket != null && _socket.Connected;
    }

    private async void OnApplicationQuit()
    {
        if (_socket != null && _socket.Connected)
        {
            await _socket.DisconnectAsync();
        }
    }
}

[Serializable]
public class RoomCreatedData
{
    public string roomId;
    public string roomCode;
}

[Serializable]
public class RoomJoinedData
{
    public string roomId;
}

[Serializable]
public class PlayersUpdatedData
{
    public List<PlayerData> players;
}

[Serializable]
public class PlayerData
{
    public string playerId;
    public string playerName;
    public DiceColor diceColor;
}


public class Player_Root
{
    public string playerId { get; set; }
}

//Join For Online

public class Game_Online
{
    public string game_mode { get; set; }
    public string match_fee { get; set; }
    public int room_size { get; set; }
}
//Join For private
public class Game_Private
{
    public string game_mode { get; set; }
    public string match_fee { get; set; }
    public int room_size { get; set; }
}
public class RoomCode
{
    public string game_code { get; set; }
}


[Serializable]
public class GameStartData
{
    public string sessionId;
    public int entryFee;
    public List<PlayerData> players;
    public DiceColor startingPlayer;
}