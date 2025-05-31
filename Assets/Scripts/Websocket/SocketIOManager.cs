using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SocketIOManager : MonoBehaviour
{
    public static SocketIOManager Instance { get; private set; }

    [SerializeField] private string serverUrl = "https://game-api.soft-360.com";

    private SocketIO _socket;
    private string _playerId;
    private string _currentRoomId;
    private bool _isMasterClient = false;

    // Events
    public event Action OnConnected;
    public event Action<string> OnDisconnected;
    public event Action<string> OnError;
    public event Action<string> OnRoomCreated;
    public event Action<string> OnRoomJoined;
    public event Action<List<PlayerData>> OnPlayersUpdated;
    public event Action<GameStartData> OnGameStarted;
    public event Action<string, DiceColor> OnPlayerLeft;
    public event Action<string, Dictionary<string, object>> OnPlayerPropertiesUpdated;
    public event Action<float> OnCountdownUpdated;

    private Dictionary<string, PlayerData> _players = new Dictionary<string, PlayerData>();

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
                //EIO = 4
            });

            SetupEventHandlers();
            await _socket.ConnectAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection failed: " + e.Message);
            OnError?.Invoke(e.Message);
        }
    }

    private void SetupEventHandlers()
    {
        _socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Socket.IO connected!");
            OnConnected?.Invoke();
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
            _isMasterClient = true;
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
            UpdatePlayersList(data.players);
            OnPlayersUpdated?.Invoke(data.players);
        });

        _socket.On("game_started", response =>
        {
            var data = response.GetValue<GameStartData>();
            OnGameStarted?.Invoke(data);
        });

        _socket.On("player_left", response =>
        {
            var data = response.GetValue<PlayerLeftData>();
            OnPlayerLeft?.Invoke(data.playerId, data.diceColor);
        });

        _socket.On("player_properties_updated", response =>
        {
            var data = response.GetValue<PlayerPropertiesData>();
            OnPlayerPropertiesUpdated?.Invoke(data.playerId, data.properties);
        });

        _socket.On("countdown_updated", response =>
        {
            var time = response.GetValue<float>();
            OnCountdownUpdated?.Invoke(time);
        });

        _socket.On("dice_color_assigned", response =>
        {
            var data = response.GetValue<DiceColorAssignment>();
            UpdatePlayerDiceColor(data.playerId, data.diceColor);
        });
    }

    private void UpdatePlayersList(List<PlayerData> players)
    {
        _players.Clear();
        foreach (var player in players)
        {
            _players[player.playerId] = player;
            if (player.playerId == _playerId && player.isMasterClient)
            {
                _isMasterClient = true;
            }
        }
    }

    private void UpdatePlayerDiceColor(string playerId, DiceColor diceColor)
    {
        if (_players.TryGetValue(playerId, out PlayerData player))
        {
            player.diceColor = diceColor;
        }
    }

    #region Room Management
    public async void JoinRandomMatch(string matchFee, int roomSize)
    {
        if (!IsConnected()) return;

        await _socket.EmitAsync("join_random", new
        {
            match_fee = matchFee,
            room_size = roomSize
        });
    }

    public async void CreatePrivateRoom(string matchFee, int roomSize, string roomCode = null)
    {
        if (!IsConnected()) return;

        await _socket.EmitAsync("create_private", new
        {
            match_fee = matchFee,
            room_size = roomSize,
            room_code = roomCode
        });
    }

    public async void JoinPrivateRoom(string roomCode)
    {
        if (!IsConnected()) return;

        await _socket.EmitAsync("join_private", new
        {
            room_code = roomCode
        });
    }

    public async void LeaveRoom()
    {
        if (!IsConnected()) return;

        await _socket.EmitAsync("leave_room");
        _currentRoomId = null;
        _isMasterClient = false;
    }

    public async void CloseRoom()
    {
        if (!IsConnected() || !_isMasterClient) return;

        await _socket.EmitAsync("close_room");
    }
    #endregion

    #region Player Management
    public async void RequestDiceColor(string playerId)
    {
        if (!IsConnected()) return;

        await _socket.EmitAsync("request_dice_color", new
        {
            player_id = playerId
        });
    }

    public async void UpdatePlayerProperties(string playerId, Dictionary<string, object> properties)
    {
        if (!IsConnected()) return;

        await _socket.EmitAsync("update_player_properties", new
        {
            player_id = playerId,
            properties = properties
        });
    }

    public async void AssignDiceColor(string playerId, DiceColor color)
    {
        if (!IsConnected() || !_isMasterClient) return;

        await _socket.EmitAsync("assign_dice_color", new
        {
            player_id = playerId,
            dice_color = (int)color
        });
    }
    #endregion

    #region Game Flow
    public async void StartGame()
    {
        if (!IsConnected() || !_isMasterClient) return;

        await _socket.EmitAsync("start_game");
    }

    public async void UpdateCountdown(float time)
    {
        if (!IsConnected() || !_isMasterClient) return;

        await _socket.EmitAsync("update_countdown", new
        {
            time = time
        });
    }
    #endregion

    #region Utility Methods
    public bool IsConnected()
    {
        return _socket != null && _socket.Connected;
    }

    public bool IsMasterClient(string playerId = null)
    {
        if (playerId != null)
        {
            return _players.TryGetValue(playerId, out PlayerData player) && player.isMasterClient;
        }
        return _isMasterClient;
    }

    public int GetPlayerCount()
    {
        return _players.Count;
    }

    public List<PlayerData> GetPlayers()
    {
        return new List<PlayerData>(_players.Values);
    }
    #endregion

    private async void OnApplicationQuit()
    {
        if (IsConnected())
        {
            await _socket.DisconnectAsync();
        }
    }
}

#region Data Classes
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
    public bool isMasterClient;
    public Dictionary<string, object> properties;
}

[Serializable]
public class PlayerLeftData
{
    public string playerId;
    public DiceColor diceColor;
}

[Serializable]
public class PlayerPropertiesData
{
    public string playerId;
    public Dictionary<string, object> properties;
}

[Serializable]
public class DiceColorAssignment
{
    public string playerId;
    public DiceColor diceColor;
}

[Serializable]
public class GameStartData
{
    public string sessionId;
    public int entryFee;
    public List<PlayerData> players;
    public DiceColor startingPlayer;
}
#endregion