using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Text;
using Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;

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
    public event Action<int> OnTimerUpdated;

    private int _playerCount = 0;
    private bool _isRoomFull = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }    public async void Connect(string playerId)
    {
        Debug.Log($"[SocketIOManager] Connect() called with playerId: {playerId}");
        Debug.Log($"[SocketIOManager] Server URL: {serverUrl}");
        
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("[SocketIOManager] Connect failed: Player ID is null or empty");
            Debug.LogError("[SocketIOManager] Connection cannot proceed without a valid player ID");
            OnError?.Invoke("Invalid player ID");
            return;
        }
        
        _playerId = playerId;
        Debug.Log($"[SocketIOManager] Player ID set to: {_playerId}");

        try
        {
            // Check if already connected
            if (_socket != null)
            {
                Debug.Log($"[SocketIOManager] Socket instance exists. Connected: {_socket.Connected}, Transport: {_socket.Options.Transport}");
                
                if (_socket.Connected)
                {
                    Debug.Log("[SocketIOManager] Already connected to Socket.IO server. Disconnecting first.");
                    Debug.Log("[SocketIOManager] Calling DisconnectAsync()...");
                    await _socket.DisconnectAsync();
                    Debug.Log("[SocketIOManager] DisconnectAsync() completed");
                }
            }
            else
            {
                Debug.Log("[SocketIOManager] No existing socket instance");
            }            Debug.Log($"[SocketIOManager] Creating SocketIO instance for serverUrl: {serverUrl}");
            var options = new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 5,
                ReconnectionDelay = 1000,
                //EIO = 4 // Protocol version
            };
            
            Debug.Log($"[SocketIOManager] Socket.IO options: Reconnection={options.Reconnection}, " +
                     $"ReconnectionAttempts={options.ReconnectionAttempts}, ReconnectionDelay={options.ReconnectionDelay}ms, " +
                     $"Transport={options.Transport}, Path={options.Path}");
                     
            _socket = new SocketIO(new Uri(serverUrl), options);
            
            Debug.Log($"[SocketIOManager] SocketIO instance created with Namespace: {_socket.Namespace}");
            Debug.Log("[SocketIOManager] Setting up event handlers");

            // Setup event handlers
            _socket.OnConnected += (sender, e) =>
            {
                Debug.Log("[SocketIOManager] Socket.IO connected successfully!");
                Debug.Log($"[SocketIOManager] Connection established with Transport: {_socket.Options.Transport}, Namespace: {_socket.Namespace}");
                
                OnConnected?.Invoke();

                // Send player ID to server
                Debug.Log($"[SocketIOManager] Emitting set_id with playerId: {_playerId}");
                _ = _socket.EmitAsync("set_id", new { playerId = _playerId });
            };

            _socket.OnDisconnected += (sender, e) =>
            {
                Debug.Log($"[SocketIOManager] Socket.IO disconnected! Reason: {e}");
                Debug.Log("[SocketIOManager] Clearing room data due to disconnection");
                _currentRoomId = null;
                _playerCount = 0;
                _isRoomFull = false;
                OnDisconnected?.Invoke(e);
            };

            _socket.OnError += (sender, e) =>
            {
                Debug.LogError($"[SocketIOManager] Socket.IO error occurred: {e}");
                Debug.LogError($"[SocketIOManager] Error details - Connected: {_socket?.Connected}, Transport: {_socket?.Options.Transport}");
                OnError?.Invoke(e);
            };

            // Custom event handlers
            _socket.On("room_created", response =>
            {
                try
                {
                    Debug.Log($"[SocketIOManager] Received room_created event. Raw response: {response}");
                    var data = response.GetValue<RoomCreatedData>();
                    Debug.Log($"[SocketIOManager] Room created with roomId: {data.roomId}, roomCode: {data.roomCode}");
                    
                    if (string.IsNullOrEmpty(data.roomId))
                    {
                        Debug.LogWarning("[SocketIOManager] Room created but roomId is null or empty");
                    }
                    
                    _currentRoomId = data.roomId;
                    OnRoomCreated?.Invoke(data.roomCode);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIOManager] Error processing room_created event: {ex.Message}");
                    Debug.LogError($"[SocketIOManager] Exception details: {ex}");
                }
            });

            _socket.On("room_joined", response =>
            {
                try
                {
                    Debug.Log($"[SocketIOManager] Received room_joined event. Raw response: {response}");
                    var data = response.GetValue<RoomJoinedData>();
                    Debug.Log($"[SocketIOManager] Joined room with roomId: {data.roomId}");
                    
                    if (string.IsNullOrEmpty(data.roomId))
                    {
                        Debug.LogWarning("[SocketIOManager] Joined room but roomId is null or empty");
                    }
                    
                    _currentRoomId = data.roomId;
                    OnRoomJoined?.Invoke(data.roomId);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIOManager] Error processing room_joined event: {ex.Message}");
                    Debug.LogError($"[SocketIOManager] Exception details: {ex}");
                }
            });

            _socket.On("players_updated", response =>
            {
                try
                {
                    Debug.Log($"[SocketIOManager] Received players_updated event. Raw response: {response}");
                    var data = response.GetValue<PlayersUpdatedData>();
                    
                    if (data.players == null)
                    {
                        Debug.LogWarning("[SocketIOManager] players_updated event received but players list is null");
                        return;
                    }
                    
                    Debug.Log($"[SocketIOManager] Players updated. Count: {data.players.Count}, RoomSize: {data.roomSize}");
                    
                    // Log player details
                    for (int i = 0; i < data.players.Count; i++)
                    {
                        var player = data.players[i];
                        Debug.Log($"[SocketIOManager] Player[{i}] - ID: {player.playerId}, Name: {player.playerName}, Color: {player.diceColor}");
                    }
                    
                    // Update internal player count
                    UpdatePlayerCount(data.players);
                    
                    // Check if room is full based on expected room size
                    if (data.roomSize > 0 && data.players != null && data.players.Count >= data.roomSize)
                    {
                        _isRoomFull = true;
                        Debug.Log($"[SocketIOManager] Room is now full. Players: {data.players.Count}/{data.roomSize}");
                    }
                    else
                    {
                        Debug.Log($"[SocketIOManager] Room is not full yet. Players: {data.players.Count}/{data.roomSize}");
                    }
                    
                    OnPlayersUpdated?.Invoke(data.players);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIOManager] Error processing players_updated event: {ex.Message}");
                    Debug.LogError($"[SocketIOManager] Exception details: {ex}");
                }
            });

            _socket.On("game_started", response =>
            {
                try
                {
                    Debug.Log($"[SocketIOManager] Received game_started event. Raw response: {response}");
                    var data = response.GetValue<GameStartData>();
                    Debug.Log($"[SocketIOManager] Game started with SessionId: {data.sessionId}, EntryFee: {data.entryFee}, Starting player color: {data.startingPlayer}");
                    
                    // Log player details if available
                    if (data.players != null && data.players.Count > 0)
                    {
                        Debug.Log($"[SocketIOManager] Game started with {data.players.Count} players:");
                        for (int i = 0; i < data.players.Count; i++)
                        {
                            var player = data.players[i];
                            Debug.Log($"[SocketIOManager] Player[{i}] - ID: {player.playerId}, Name: {player.playerName}, Color: {player.diceColor}");
                        }
                    }
                    
                    OnGameStarted?.Invoke(data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIOManager] Error processing game_started event: {ex.Message}");
                    Debug.LogError($"[SocketIOManager] Exception details: {ex}");
                }
            });

            _socket.On("timer", HandleTimerEvent);

            _socket.OnAny((eventName, response) =>
            {
                Debug.Log($"[SocketIOManager] Received generic event: '{eventName}' with data: {response}");
                
                // Process events using simple string approach
                try                
                {
                    // Extract the raw response as a string for safe parsing
                    string rawResponse = response.ToString();
                    
                    switch (eventName)
                    {
                        case "system":
                            Debug.Log($"[SocketIOManager] System event details: {rawResponse}");
                            // Extract useful information if present
                            if (rawResponse.Contains("Joined new room:")) 
                            {
                                Debug.Log("[SocketIOManager] System event: Room join notification detected");
                                int roomCodeStart = rawResponse.IndexOf("Joined new room:") + 15;
                                if (roomCodeStart > 15) 
                                {
                                    string roomCode = rawResponse.Substring(roomCodeStart).Trim().Trim('"', ']', '[');
                                    Debug.Log($"[SocketIOManager] System: Joined room with code: {roomCode}");
                                }
                            }
                            if (rawResponse.Contains("Your ID set to")) 
                            {
                                Debug.Log("[SocketIOManager] System event: ID set notification detected");
                                int idStart = rawResponse.IndexOf("Your ID set to") + 13;
                                if (idStart > 13) 
                                {
                                    string playerId = rawResponse.Substring(idStart).Trim().Trim('"', ']', '[');
                                    Debug.Log($"[SocketIOManager] System: Player ID set to: {playerId}");
                                }
                            }
                            break;
                            
                        case "join":
                            Debug.Log($"[SocketIOManager] Join event details: {rawResponse}");
                            // Extract game code if present
                            if (rawResponse.Contains("\"game_code\"")) 
                            {
                                Debug.Log("[SocketIOManager] Join event contains game_code");
                                if (ExtractValueFromJson(rawResponse, "game_code", out string gameCode))
                                {
                                    Debug.Log($"[SocketIOManager] Join event game code: {gameCode}");
                                }
                            }
                            
                            // Extract player count if present
                            if (rawResponse.Contains("\"player_online\"")) 
                            {
                                Debug.Log("[SocketIOManager] Join event contains player_online count");
                                if (ExtractValueFromJson(rawResponse, "player_online", out string playerCount))
                                {
                                    Debug.Log($"[SocketIOManager] Join event player count: {playerCount}");
                                    if (int.TryParse(playerCount, out int count))
                                    {
                                        _playerCount = count;
                                    }
                                }
                            }
                            break;
                            
                        // Add more cases for other event types as needed
                        default:
                            if (eventName == "timer") return;

                            Debug.Log($"[SocketIOManager] Received generic event: '{eventName}' with data: {response}");
                            // Still log some key information if we can find it
                            if (rawResponse.Contains("\"id\":"))
                            {
                                ExtractValueFromJson(rawResponse, "id", out string id);
                                Debug.Log($"[SocketIOManager] Found ID in unprocessed event: {id}");
                            }
                            
                            if (rawResponse.Contains("\"status\":"))
                            {
                                ExtractValueFromJson(rawResponse, "status", out string status);
                                Debug.Log($"[SocketIOManager] Found status in unprocessed event: {status}");
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIOManager] Error processing event '{eventName}': {ex.Message}");
                    Debug.LogError($"[SocketIOManager] Exception details: {ex}");
                }
            });
            Debug.Log("[SocketIOManager] Connecting to server...");
            Debug.Log($"[SocketIOManager] Current network reachability: {Application.internetReachability}");
            
            try {
                await _socket.ConnectAsync();
                Debug.Log("[SocketIOManager] ConnectAsync completed successfully!");
                Debug.Log($"[SocketIOManager] Connection details - Connected: {_socket.Connected}, " +
                         $"Transport: {_socket.Options.Transport}, Namespace: {_socket.Namespace}");
            }
            catch (Exception connectEx) {
                Debug.LogError($"[SocketIOManager] ConnectAsync threw exception: {connectEx.Message}");
                Debug.LogError($"[SocketIOManager] ConnectAsync exception details: {connectEx}");
                throw; // Re-throw to be caught by outer try-catch
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SocketIOManager] Connection failed: " + e.Message);
            OnError?.Invoke(e.Message);
        }
    }
    public async void JoinRandomMatch(string matchFee, int roomSize)
    {
        Debug.Log($"[SocketIOManager] JoinRandomMatch() called with matchFee: {matchFee}, roomSize: {roomSize}");
        Debug.Log($"[SocketIOManager] JoinRandomMatch(): Room size source value from DataManager.MaxPlayerNumberForCurrentBoard = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
        Debug.Log($"[SocketIOManager] JoinRandomMatch(): DataManager.CurrentRoomType = {DataManager.Instance.CurrentRoomType}, RoomMode = {DataManager.Instance.CurrentRoomMode}");

        if (string.IsNullOrEmpty(matchFee))
        {
            Debug.LogError("[SocketIOManager] Cannot join random match: matchFee is null or empty");
            OnError?.Invoke("Invalid match fee");
            return;
        }

        if (roomSize <= 0)
        {
            Debug.LogError($"[SocketIOManager] Cannot join random match: Invalid roomSize: {roomSize}");
            Debug.LogError($"[SocketIOManager] Stack trace for invalid roomSize: {Environment.StackTrace}");

            // Use a default room size instead of failing
            roomSize = 4;
            Debug.Log($"[SocketIOManager] Using default roomSize of {roomSize} to continue join attempt");
            // OnError?.Invoke("Invalid room size");
            // return;
        }

        if (_socket == null || !_socket.Connected)
        {
            Debug.LogError("[SocketIOManager] Cannot join random match: Socket is not connected");
            Debug.LogError($"[SocketIOManager] Socket status: {(_socket == null ? "null" : _socket.Connected ? "connected" : "disconnected")}");
            OnError?.Invoke("Socket not connected");
            return;
        }

        try
        {
            Debug.Log($"[SocketIOManager] Emitting 'join' event to server with game_mode: Online, match_fee: {matchFee}, room_size: {roomSize}");
            await _socket.EmitAsync("join", new
            {
                game_mode = "Online",
                match_fee = matchFee,
                room_size = roomSize
            });
            Debug.Log("[SocketIOManager] Successfully sent join request for random match");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIOManager] Error joining random match: {ex.Message}");
            Debug.LogError($"[SocketIOManager] Exception details: {ex}");
            OnError?.Invoke($"Error joining match: {ex.Message}");
        }
    }

    public async void CreatePrivateRoom(string matchFee, int roomSize)
    {
        Debug.Log($"[SocketIOManager] CreatePrivateRoom() called with matchFee: {matchFee}, roomSize: {roomSize}");
        
        if (string.IsNullOrEmpty(matchFee))
        {
            Debug.LogError("[SocketIOManager] Cannot create private room: matchFee is null or empty");
            OnError?.Invoke("Invalid match fee");
            return;
        }
        
        if (roomSize <= 0)
        {
            Debug.LogError($"[SocketIOManager] Cannot create private room: Invalid roomSize: {roomSize}");
            OnError?.Invoke("Invalid room size");
            return;
        }
        
        if (_socket == null || !_socket.Connected) 
        {
            Debug.LogError("[SocketIOManager] Cannot create private room: Socket is not connected");
            Debug.LogError($"[SocketIOManager] Socket status: {(_socket == null ? "null" : _socket.Connected ? "connected" : "disconnected")}");
            OnError?.Invoke("Socket not connected");
            return;
        }

        try
        {
            Debug.Log($"[SocketIOManager] Emitting 'create_room' event to server with game_mode: Private, match_fee: {matchFee}, room_size: {roomSize}");
            await _socket.EmitAsync("create_room", new
            {
                game_mode = "Private",
                match_fee = matchFee,
                room_size = roomSize
            });
            Debug.Log("[SocketIOManager] Successfully sent create_room request");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIOManager] Error creating private room: {ex.Message}");
            Debug.LogError($"[SocketIOManager] Exception details: {ex}");
            OnError?.Invoke($"Error creating room: {ex.Message}");
        }
    }

    public async void JoinPrivateRoom(string roomCode)
    {
        Debug.Log($"[SocketIOManager] JoinPrivateRoom() called with roomCode: {roomCode}");
        
        if (string.IsNullOrEmpty(roomCode))
        {
            Debug.LogError("[SocketIOManager] Cannot join private room: roomCode is null or empty");
            OnError?.Invoke("Invalid room code");
            return;
        }
        
        if (_socket == null || !_socket.Connected) 
        {
            Debug.LogError("[SocketIOManager] Cannot join private room: Socket is not connected");
            Debug.LogError($"[SocketIOManager] Socket status: {(_socket == null ? "null" : _socket.Connected ? "connected" : "disconnected")}");
            OnError?.Invoke("Socket not connected");
            return;
        }

        try
        {
            Debug.Log($"[SocketIOManager] Emitting 'join_with_code' event to server with game_code: {roomCode}");
            await _socket.EmitAsync("join_with_code", new
            {
                game_code = roomCode
            });
            Debug.Log("[SocketIOManager] Successfully sent join_with_code request");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIOManager] Error joining private room: {ex.Message}");
            Debug.LogError($"[SocketIOManager] Exception details: {ex}");
            OnError?.Invoke($"Error joining room: {ex.Message}");
        }
    }    public bool IsConnected()
    {
        bool connected = _socket != null && _socket.Connected;
        Debug.Log($"[SocketIOManager] IsConnected() called, returning: {connected}, Socket instance exists: {_socket != null}");
        if (_socket != null) {
            Debug.Log($"[SocketIOManager] IsConnected() details - Socket options: Transport={_socket.Options.Transport}, Reconnection={_socket.Options.Reconnection}, ReconnectionAttempts={_socket.Options.ReconnectionAttempts}");
        }
        return connected;
    }

    private async void OnApplicationQuit()
    {
        Debug.Log("[SocketIOManager] OnApplicationQuit() called");
        
        if (_socket != null && _socket.Connected)
        {
            Debug.Log("[SocketIOManager] Disconnecting from Socket.IO server on application quit");
            try
            {
                await _socket.DisconnectAsync();
                Debug.Log("[SocketIOManager] Successfully disconnected from Socket.IO server");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketIOManager] Error disconnecting from server: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("[SocketIOManager] No active connection to disconnect");
        }
    }    public bool IsRoomFull()
    {
        Debug.Log($"[SocketIOManager] IsRoomFull() called, returning: {_isRoomFull}");
        return _isRoomFull;
    }
    
    public int GetPlayerCount()
    {
        Debug.Log($"[SocketIOManager] GetPlayerCount() called, returning: {_playerCount}");
        return _playerCount;
    }
    
    // Update player count when we receive players_updated event
    private void UpdatePlayerCount(List<PlayerData> players)
    {
        Debug.Log($"[SocketIOManager] UpdatePlayerCount() called with {(players == null ? "null" : players.Count.ToString())} players");
        
        if (players == null)
        {
            Debug.LogWarning("[SocketIOManager] UpdatePlayerCount called with null players list");
            return;
        }
        
        _playerCount = players.Count;
        Debug.Log($"[SocketIOManager] Player count updated to: {_playerCount}");
        
        // Log each player's info
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            Debug.Log($"[SocketIOManager] Player[{i}] - ID: {player.playerId}, Name: {player.playerName}, Color: {player.diceColor}");
            
            // Check if this is the local player
            if (player.playerId == _playerId)
            {
                Debug.Log($"[SocketIOManager] Found local player with dice color: {player.diceColor}");
            }
        }
    }
    private void HandleTimerEvent(SocketIOResponse response)
    {
        try
        {
            if (response == null)
            {
                Debug.LogWarning("[SocketIOManager] Timer event received null response");
                return;
            }

            string jsonString = response.ToString();
            Debug.Log($"[SocketIOManager] Raw timer data: {jsonString}");

            // Parse the timer data
            int secondsRemaining = 15;
            bool isOpen = true;
            int playerCount = _playerCount;

            try
            {
                JToken token = JToken.Parse(jsonString);

                if (token is JArray array && array.Count > 0)
                {
                    if (array[0]["roomInfo"] is JObject roomInfo)
                    {
                        secondsRemaining = roomInfo["timer"]?.Value<int>() ?? secondsRemaining;
                        isOpen = roomInfo["open"]?.Value<bool>() ?? isOpen;
                        playerCount = roomInfo["players"]?.Count() ?? playerCount;
                    }
                }
                else if (token is JObject obj)
                {
                    secondsRemaining = obj["timer"]?.Value<int>() ?? secondsRemaining;
                    isOpen = obj["open"]?.Value<bool>() ?? isOpen;
                    playerCount = obj["players"]?.Count() ?? playerCount;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SocketIOManager] Error parsing timer data: {ex}");
            }

            // Clamp values
            secondsRemaining = Mathf.Clamp(secondsRemaining, 0, 15);
            playerCount = Mathf.Max(0, playerCount);

            // Log the values we're about to dispatch
            Debug.Log($"[SocketIOManager] Dispatching timer update - Seconds: {secondsRemaining}, Open: {isOpen}, Players: {playerCount}");

            // Dispatch to main thread with error handling
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                try
                {
                    Debug.Log($"[SocketIOManager] Main thread executing timer update");

                    // Update internal state
                    _isRoomFull = !isOpen;
                    _playerCount = playerCount;

                    // Invoke the event
                    if (OnTimerUpdated != null)
                    {
                        Debug.Log($"[SocketIOManager] Invoking OnTimerUpdated with {secondsRemaining} seconds");
                        OnTimerUpdated.Invoke(secondsRemaining);
                    }
                    else
                    {
                        Debug.LogWarning("[SocketIOManager] OnTimerUpdated event has no subscribers");
                    }

                    if (secondsRemaining == 0)
                    {
                        Debug.Log("[SocketIOManager] Timer has reached zero!");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketIOManager] Error in main thread timer handler: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIOManager] Error processing timer event: {ex}");
        }
    }

    private bool ExtractValueFromJson(string jsonString, string key, out string value)
    {
        value = string.Empty;
        try
        {
            Debug.Log($"[SocketIOManager] ExtractValueFromJson: Attempting to extract key '{key}' from JSON");
            
            if (string.IsNullOrEmpty(jsonString)) 
            {
                Debug.LogWarning("[SocketIOManager] ExtractValueFromJson: JSON string is null or empty");
                return false;
            }
            
            string searchKey = $"\"{key}\":";
            int keyIndex = jsonString.IndexOf(searchKey);
            
            if (keyIndex < 0)
            {
                Debug.LogWarning($"[SocketIOManager] ExtractValueFromJson: Could not find key '{key}' in JSON string");
                Debug.Log($"[SocketIOManager] ExtractValueFromJson: First 100 chars of JSON: {(jsonString.Length > 100 ? jsonString.Substring(0, 100) + "..." : jsonString)}");
                return false;
            }
            
            Debug.Log($"[SocketIOManager] ExtractValueFromJson: Found key '{key}' at index {keyIndex}");
            int valueStart = keyIndex + searchKey.Length;
            
            // Skip whitespace
            while (valueStart < jsonString.Length && char.IsWhiteSpace(jsonString[valueStart]))
            {
                valueStart++;
            }
            
            // Check if the value is a string (starts with quote)
            bool isString = valueStart < jsonString.Length && jsonString[valueStart] == '"';
            Debug.Log($"[SocketIOManager] ExtractValueFromJson: Value for key '{key}' is {(isString ? "a string" : "not a string")}");
            
            // If it's a string, skip the opening quote
            if (isString)
            {
                valueStart++;
            }
            
            // Extract the value
            int valueEnd = valueStart;
            
            if (isString)
            {
                Debug.Log("[SocketIOManager] ExtractValueFromJson: Extracting string value");
                // For strings, find the closing quote
                while (valueEnd < jsonString.Length && jsonString[valueEnd] != '"')
                {
                    // Handle escaped quotes
                    if (jsonString[valueEnd] == '\\' && valueEnd + 1 < jsonString.Length)
                    {
                        valueEnd += 2;
                    }
                    else
                    {
                        valueEnd++;
                    }
                }
            }
            else
            {
                Debug.Log("[SocketIOManager] ExtractValueFromJson: Extracting non-string value (number, boolean, etc.)");
                // For non-strings (numbers, booleans, etc.), find the end by looking for comma, closing brace, or bracket
                while (valueEnd < jsonString.Length && 
                      jsonString[valueEnd] != ',' && 
                      jsonString[valueEnd] != '}' && 
                      jsonString[valueEnd] != ']')
                {
                    valueEnd++;
                }
            }
            
            // Extract the value substring
            if (valueEnd > valueStart)
            {
                value = jsonString.Substring(valueStart, valueEnd - valueStart);
                Debug.Log($"[SocketIOManager] ExtractValueFromJson: Successfully extracted value for key '{key}': {value}");
                
                // Additional validation for the extracted value
                if (isString && string.IsNullOrEmpty(value))
                {
                    Debug.LogWarning($"[SocketIOManager] ExtractValueFromJson: Extracted empty string value for key '{key}'");
                }
                else if (!isString)
                {
                    // For numeric values, validate that it's actually a number
                    if (int.TryParse(value, out int numVal))
                    {
                        Debug.Log($"[SocketIOManager] ExtractValueFromJson: Extracted numeric value {numVal} for key '{key}'");
                    }
                    else if (value == "true" || value == "false")
                    {
                        Debug.Log($"[SocketIOManager] ExtractValueFromJson: Extracted boolean value {value} for key '{key}'");
                    }
                    else
                    {
                        Debug.LogWarning($"[SocketIOManager] ExtractValueFromJson: Extracted non-numeric, non-boolean value for key '{key}': {value}");
                    }
                }
                
                return true;
            }
            
            Debug.LogWarning($"[SocketIOManager] ExtractValueFromJson: Found key '{key}' but could not extract value");
            Debug.LogWarning($"[SocketIOManager] ExtractValueFromJson: JSON context: {jsonString.Substring(Math.Max(0, keyIndex - 20), Math.Min(50, jsonString.Length - Math.Max(0, keyIndex - 20)))}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SocketIOManager] ExtractValueFromJson: Error extracting value for key '{key}': {ex.Message}");
            Debug.LogError($"[SocketIOManager] ExtractValueFromJson: Exception stack trace: {ex.StackTrace}");
            return false;
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
    public int roomSize;
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
[Serializable]
public class TimerData
{
    // For array format [timer, open, player_count]
    [JsonConstructor]
    public TimerData(JArray array)
    {
        if (array != null && array.Count > 0)
        {
            timer = array[0].Type == JTokenType.Integer ? array[0].Value<int>() : 0;
            open = array.Count > 1 && array[1].Type == JTokenType.Boolean ? array[1].Value<bool>() : true;
            player_count = array.Count > 2 && array[2].Type == JTokenType.Integer ? array[2].Value<int>() : 0;
        }
    }

    // For object format {"timer":x,"open":y,"player_count":z}
    [JsonProperty("timer")]
    public int timer { get; set; }

    [JsonProperty("open")]
    public bool open { get; set; }

    [JsonProperty("player_count")]
    public int player_count { get; set; }
}
