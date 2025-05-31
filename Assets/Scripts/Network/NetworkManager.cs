//using Photon.Pun;
//using Photon.Realtime;
using UnityEngine;
using TMPro;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using Network;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    [Header("Connection Settings")]
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private string preferredRegion = "asia";
    private const string UserTypeKey = "userType";
    private const string EntryFeesKey = "entryFees";
    private const string UserUid = "userUid";

    private const string GameId = "game_id";
    private const string HostId = "host_id";
    private const string RoomId = "room_id";
    private const string BoardAmount = "board_amount";

    private string currentRoomId = null;
    private UserType userType;

    [SerializeField] private int maxJoinWaitingTime = 30;
    [SerializeField] protected int maxStartGameDelayTime = 1;
    private const byte MAX_PLAYERS = 4;
    public const string DICE_COLOR_KEY = "diceColorKey";



    private Coroutine stateUpdateCoroutine;
    private Coroutine waitingCoroutine;

    public GameObject MainPanel;
    public GameObject GamePanel;
    public GameObject ConnectingPanel;
    public TextMeshProUGUI ConnectingStatusText;

    private float startTime = 100.0f;
    private float timer;
    private bool countdownStarted = false;
    private bool canJoin = true;

    [SerializeField] private UIManager uiManager;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private int totalvalue;
    //  PhotonView pv;

    private bool hasRejoined = false;
    private Coroutine rejoinTimerCoroutine;

    #region Monobehaviour Callbacks and Initializations

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize WebSocket events
        SocketIOManager.Instance.OnConnected += OnWebSocketConnected;
        SocketIOManager.Instance.OnDisconnected += OnWebSocketDisconnected;
        SocketIOManager.Instance.OnRoomCreated += OnPrivateRoomCreated;
        SocketIOManager.Instance.OnRoomJoined += OnRoomJoined;
        SocketIOManager.Instance.OnPlayersUpdated += OnPlayersUpdated;
        SocketIOManager.Instance.OnGameStarted += OnGameStarted;
        SocketIOManager.Instance.OnTimerUpdated += StartCountdownFromServer;
        SocketIOManager.Instance.OnError += OnSocketError;

        Debug.Log("[NetworkManager] Initialized with all Socket.IO event handlers");
    }

    private void OnSocketError(string errorMessage)
    {
        Debug.LogError($"[NetworkManager] Socket.IO error: {errorMessage}");
        uiManager.errorPopUp.ShowMessagePanel($"Connection error: {errorMessage}");
    }
    public void Connect()
    {
        Debug.Log("[NetworkManager] Connect() called");
        Debug.Log($"[NetworkManager] Connect: MaxPlayerNumberForCurrentBoard = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
        Debug.Log($"[NetworkManager] Connect: CurrentRoomType = {DataManager.Instance.CurrentRoomType}, CurrentRoomMode = {DataManager.Instance.CurrentRoomMode}");
        Debug.Log($"[NetworkManager] Connect: CurrentEntryFee = {DataManager.Instance.CurrentEntryFee}");

        // Only connect if not already connected
        if (!SocketIOManager.Instance.IsConnected())
        {
            Debug.Log("[NetworkManager] Not connected. Attempting to connect with user ID: " + DataManager.Instance.CurrentUser.id);

            // Ensure MaxPlayerNumberForCurrentBoard is valid
            if (DataManager.Instance.MaxPlayerNumberForCurrentBoard <= 0)
            {
                Debug.LogWarning("[NetworkManager] Connect: MaxPlayerNumberForCurrentBoard is invalid (≤ 0), defaulting to 4");
                DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4);
            }

            SocketIOManager.Instance.Connect(DataManager.Instance.CurrentUser.id.ToString());
        }
        else
        {
            Debug.Log("[NetworkManager] Already connected. Proceeding to OnWebSocketConnected()");

            // Ensure MaxPlayerNumberForCurrentBoard is valid
            if (DataManager.Instance.MaxPlayerNumberForCurrentBoard <= 0)
            {
                Debug.LogWarning("[NetworkManager] Connect: MaxPlayerNumberForCurrentBoard is invalid (≤ 0) when already connected, defaulting to 4");
                DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4);
            }

            // If already connected, proceed directly to room joining
            OnWebSocketConnected();
        }
    }
    private void OnWebSocketConnected()
    {
        Debug.Log($"[NetworkManager] WebSocket connected! RoomType: {DataManager.Instance.CurrentRoomType}, RoomMode: {DataManager.Instance.CurrentRoomMode}");
        Debug.Log($"[NetworkManager] OnWebSocketConnected: MaxPlayerNumberForCurrentBoard = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
        Debug.Log($"[NetworkManager] OnWebSocketConnected: CurrentEntryFee = {DataManager.Instance.CurrentEntryFee}");
        Debug.Log($"[NetworkManager] OnWebSocketConnected: CurrentUserType = {DataManager.Instance.CurrentUserType}, CurrentGameState = {DataManager.Instance.CurrentGameState}");

        // Ensure MaxPlayerNumberForCurrentBoard is valid
        if (DataManager.Instance.MaxPlayerNumberForCurrentBoard <= 0)
        {
            Debug.LogWarning("[NetworkManager] OnWebSocketConnected: MaxPlayerNumberForCurrentBoard is invalid (≤ 0), defaulting to 4");
            DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4);
        }

        switch (DataManager.Instance.CurrentRoomType)
        {
            case RoomType.Random:
                Debug.Log("[NetworkManager] Joining random room...");
                Debug.Log($"[NetworkManager] OnWebSocketConnected: MaxPlayerNumberForCurrentBoard before calling JoinRandomRoom = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
                JoinRandomRoom();
                break;

            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Create:
                Debug.Log("[NetworkManager] Creating custom (private) room...");
                Debug.Log($"[NetworkManager] OnWebSocketConnected: MaxPlayerNumberForCurrentBoard before CreateCustomRoom = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
                CreateCustomRoom();
                break;

            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Join:
                Debug.Log("[NetworkManager] Joining private room with ID: " + uiManager.GetInputtedRoomId());
                JoinPrivateRoom(uiManager.GetInputtedRoomId());
                break;
        }
    }

    private void OnWebSocketDisconnected(string reason)
    {
        Debug.Log($"[NetworkManager] WebSocket disconnected: {reason}, CurrentGameState: {DataManager.Instance.CurrentGameState}");

        if (DataManager.Instance.CurrentGameState == GameState.Init)
        {
            Debug.Log("[NetworkManager] Showing no internet popup.");
            menuManager.ShowNoInternetPopUp();
        }
        else if (DataManager.Instance.CurrentGameState != GameState.Play)
        {
            Debug.Log("[NetworkManager] Game not in play state. Calling OnGameFinished.");
            uiManager.OnGameFinished(DiceColor.Unknown);
        }
    }
    private void JoinRandomRoom()
    {
        Debug.Log("[NetworkManager] JoinRandomRoom() called");
        Debug.Log($"[NetworkManager] JoinRandomRoom: MaxPlayerNumberForCurrentBoard = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
        Debug.Log($"[NetworkManager] JoinRandomRoom: CurrentRoomType = {DataManager.Instance.CurrentRoomType}, CurrentRoomMode = {DataManager.Instance.CurrentRoomMode}");
        Debug.Log($"[NetworkManager] JoinRandomRoom: CurrentGameState = {DataManager.Instance.CurrentGameState}");

        if (SocketIOManager.Instance.IsConnected())
        {
            string matchFee = Helper.GetReadableNumber(DataManager.Instance.CurrentEntryFee);
            Debug.Log($"[NetworkManager] JoinRandomRoom: Using matchFee = {matchFee}");

            // Check for valid room size
            if (DataManager.Instance.MaxPlayerNumberForCurrentBoard <= 0)
            {
                Debug.LogWarning("[NetworkManager] JoinRandomRoom: MaxPlayerNumberForCurrentBoard is invalid (≤ 0)");
                Debug.Log("[NetworkManager] JoinRandomRoom: Setting default MaxPlayerNumberForCurrentBoard to 4");
                DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4);
            }

            Debug.Log($"[NetworkManager] JoinRandomRoom: Final MaxPlayerNumberForCurrentBoard = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
            SocketIOManager.Instance.JoinRandomMatch(matchFee, DataManager.Instance.MaxPlayerNumberForCurrentBoard);
        }
        else
        {
            Debug.LogError("[NetworkManager] WebSocket not connected when trying to join random room");
            // Optionally try to reconnect
            Connect();
        }
    }

    private void CreateCustomRoom()
    {
        string matchFee = Helper.GetReadableNumber(DataManager.Instance.CurrentEntryFee);
        SocketIOManager.Instance.CreatePrivateRoom(matchFee, DataManager.Instance.MaxPlayerNumberForCurrentBoard);
    }

    private void JoinPrivateRoom(string roomId)
    {
        SocketIOManager.Instance.JoinPrivateRoom(roomId);
    }

    private void OnPrivateRoomCreated(string roomCode)
    {
        Debug.Log($"[NetworkManager] Private room created with code: {roomCode}");
        currentRoomId = roomCode;
        SetAndShowConnectingStatus("Room created. Waiting for players to join...");
        uiManager.OpenPrivateJoinedPlayerPanel(roomCode);
    }

    private void OnRoomJoined(string roomId)
    {
        Debug.Log($"[NetworkManager] Joined room with ID: {roomId}");
        currentRoomId = roomId;
        SetAndShowConnectingStatus("Room joined. Waiting for players...");

        // Show the joined player panel immediately with default timer
        uiManager.OpenJoinedPlayerPanelForOnlineMatch();
        UpdateCountdownDisplay(15); // Show initial 15 second countdown

        CheckBackBalanceAndRequestToGetDiceColor();
    }

    private void OnPlayersUpdated(List<PlayerData> players)
    {
        Debug.Log($"[NetworkManager] OnPlayersUpdated called with {players.Count} players");
        uiManager.RemoveAllJoinedPlayers();

        foreach (var player in players)
        {
            if (player.playerId == DataManager.Instance.CurrentUser.id.ToString())
            {
                Debug.Log($"[NetworkManager] Setting own dice color to: {player.diceColor}");
                DataManager.Instance.SetOwnDiceColor(player.diceColor);
            }

            if (DataManager.Instance.CurrentRoomType == RoomType.Random)
            {
                UIManager.Instance.InstantiateJoinedPlayer(player.diceColor, OpenJoinMultiplayerPanel);
            }
            else
            {
                UIManager.Instance.InstantiateJoinedPlayer(player.diceColor, OpenPrivateJoinedPlayerPanel);
            }
        }

        if (players.Count >= DataManager.Instance.MaxPlayerNumberForCurrentBoard)
        {
            Debug.Log("[NetworkManager] Room is full, starting game");
            StartGame();
        }
    }

    private void OnGameStarted(GameStartData gameData)
    {
        Debug.Log($"[NetworkManager] Game started with session ID: {gameData.sessionId}, entry fee: {gameData.entryFee}");

        // Set session ID and entry fee
        DataManager.Instance.SetSessionId(gameData.sessionId);
        DataManager.Instance.SetCurrentEntryFees(gameData.entryFee);

        // Set the starting player if provided
        if (gameData.startingPlayer != DiceColor.Unknown)
        {
            Debug.Log($"[NetworkManager] Starting player has dice color: {gameData.startingPlayer}");
            // You can use this information to determine who goes first
        }

        // Start the game animation
        StartCoroutine(Animtionplay());
    }

    private void Start()
    {
        //PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 30000;
        uiManager = UIManager.Instance;
        menuManager = MenuManager.Instance;
        //pv = GetComponent<PhotonView>();
    }

    private void OnDestroy()
    {
        //if (PhotonNetwork.IsConnected)
        //    PhotonNetwork.Disconnect();

        CancelInvoke(nameof(ShowOnlinePlayerCount));
    }

    #endregion Monobehaviour Callbacks and Initializations

    #region Massage Display
    private void StartToDisplayPhotonStateCoroutine()
    {
        stateUpdateCoroutine ??= StartCoroutine(UpdatePhotonStateText());
    }

    private IEnumerator UpdatePhotonStateText()
    {
        uiManager.popUp.ShowMessagePanel("Process has been started...");

        do
        {
            //GameUIManager.Instance.popUp.ShowMessagePanel(PhotonNetwork.NetworkClientState.ToString());
            uiManager.popUp.ShowMessagePanel("Loading......");
            yield return null;
        } while (stateUpdateCoroutine != null);

        stateUpdateCoroutine = null;
    }

    private IEnumerator WaitForOtherPlayer()
    {
        yield return new WaitForSeconds(maxJoinWaitingTime);
        waitingCoroutine = null;
        StopStateUpdateCoroutine();
        uiManager.ShowWaitingTimeOutPanel();
    }

    private void StopStateUpdateCoroutine()
    {
        if (stateUpdateCoroutine != null)
            StopCoroutine(stateUpdateCoroutine);

        uiManager.popUp.CloseMessagePanel();
        stateUpdateCoroutine = null;
    }

    private void StopWaitingCoroutine()
    {
        if (waitingCoroutine != null)
            StopCoroutine(waitingCoroutine);

        uiManager.popUp.CloseMessagePanel();
        waitingCoroutine = null;
    }

    #endregion Massage Display

    public void SetPlayerNameAndIDCustomProperties()
    {
        //PhotonNetwork.LocalPlayer.NickName = DataManager.Instance.CurrentUser.name;

        //if (DataManager.Instance.CurrentUserType != UserType.APP)
        //    return;

        //ExitGames.Client.Photon.Hashtable hashTable = new ExitGames.Client.Photon.Hashtable()
        //{
        //    { UserUid, DataManager.Instance.CurrentUser.id }
        //};

        //PhotonNetwork.LocalPlayer.SetCustomProperties(hashTable);
    }

    #region Random Room
    public void PlayWithRandomPlayer()
    {
        Debug.Log("[NetworkManager] PlayWithRandomPlayer() - Preparing random match");
        Debug.Log($"[NetworkManager] Current entry fee: {DataManager.Instance.CurrentEntryFee}, Max players: {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");

        // Ensure MaxPlayerNumberForCurrentBoard is set to a valid value
        if (DataManager.Instance.MaxPlayerNumberForCurrentBoard <= 0)
        {
            Debug.LogWarning("[NetworkManager] PlayWithRandomPlayer: MaxPlayerNumberForCurrentBoard is invalid (≤ 0), setting to 4");
            DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4);
            Debug.Log($"[NetworkManager] PlayWithRandomPlayer: MaxPlayerNumberForCurrentBoard set to {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
        }

        // Check if we need to update and explicitly set the room type
        if (DataManager.Instance.CurrentRoomType != RoomType.Random)
        {
            Debug.Log($"[NetworkManager] PlayWithRandomPlayer: Setting CurrentRoomType from {DataManager.Instance.CurrentRoomType} to Random");
            DataManager.Instance.SetCurrentRoomType(RoomType.Random);
        }

        uiManager.CloseAllMultiplayerPanel();
        StartToDisplayPhotonStateCoroutine();

        Debug.Log("[NetworkManager] Initiating online connection");
        Debug.Log($"[NetworkManager] PlayWithRandomPlayer before OnOnlineButtonClick: MaxPlayerNumberForCurrentBoard = {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
        OnOnlineButtonClick();
    }


    private void CreateRandomRoom()
    {
        Debug.Log("[NetworkManager] CreateRandomRoom() called - Setting max players to 4");
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4); //Setting max player to 4

        int entryFee = DataManager.Instance.CurrentEntryFee;
        byte maxPlayer = DataManager.Instance.MaxPlayerNumberForCurrentBoard;

        userType = DataManager.Instance.CurrentUserType;

        Debug.Log($"[NetworkManager] Creating random room with UserType: {Enum.GetName(typeof(UserType), userType)}, EntryFee: {entryFee}, MaxPlayers: {maxPlayer}");

        // Photon code commented out
        //RoomOptions roomOptions = new RoomOptions
        //{
        //    CustomRoomPropertiesForLobby = new[] { UserTypeKey, EntryFeesKey },
        //    MaxPlayers = maxPlayer,

        //    IsOpen = true,
        //    IsVisible = true,

        //    CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { UserTypeKey, userType }, { EntryFeesKey, entryFee } }
        //};


        //PhotonNetwork.CreateRoom(null, roomOptions);
    }
    #endregion\ Random Room

    private void ShowOnlinePlayerCount()
    {
        // uiManager.ShowOnlinePlayerCount(PhotonNetwork.PlayerList.Length);
    }



    private void SetAndShowConnectingStatus(string message)
    {
        Debug.Log($"[NetworkManager] SetAndShowConnectingStatus: '{message}'");
        ConnectingPanel.SetActive(true);
        ConnectingStatusText.text = message;
    }

    private void HideConnectingStatus()
    {
        Debug.Log("[NetworkManager] HideConnectingStatus: Hiding connecting panel");
        ConnectingPanel.SetActive(false);
        ConnectingStatusText.text = "";
    }

    public void OnOnlineButtonClick()
    {
        Debug.Log("[NetworkManager] OnOnlineButtonClick() - Starting online connection process");
        UIManager.Instance.menuManager.HideBackButton();
        SetAndShowConnectingStatus("Connecting to server...");

        Debug.Log($"[NetworkManager] Connection status before connecting: Socket.IO connected = {SocketIOManager.Instance.IsConnected()}");
        Connect();
    }

    private void CheckBackBalanceAndRequestToGetDiceColor()
    {
        Debug.Log("[NetworkManager] CheckBackBalanceAndRequestToGetDiceColor() called");

        // In Socket.IO-based implementation, entry fee is sent by the server with player updates
        // We can either use that or what we already have from DataManager
        int entryFee = DataManager.Instance.CurrentEntryFee;

        Debug.Log($"[NetworkManager] EntryFees: {entryFee}, Coins: {DataManager.Instance.Coins}");

        if (entryFee > DataManager.Instance.Coins)
        {
            Debug.Log("[NetworkManager] Not enough coins to join room");
            canJoin = false;
            DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);
            DataManager.Instance.SetCurrentRoomType(RoomType.Private);
            ShowUnableToJoinRoomDueToLowBalance();
            SetAndShowConnectingStatus("");
            return;
        }

        // No need to request dice color - the server assigns it and sends it in player updates
    }

    #region Photon Callbacks
    //public override void OnConnectedToMaster()
    //{
    //    //JoinOrCreateRandomRoom();

    //    switch (DataManager.Instance.CurrentRoomType)
    //    {
    //        case RoomType.Random:
    //            SetAndShowConnectingStatus("Connected to server. Joining room...");
    //            JoinRandomRoom();
    //            break;

    //        case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Create:
    //            SetAndShowConnectingStatus("Connected to server. Creating new room...");
    //            CreateCustomRoom();
    //            break;

    //        case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Join:
    //            SetAndShowConnectingStatus("Connected to server. Joining room...");
    //            JoinPrivateRoom(UIManager.Instance.GetInputtedRoomId());
    //            break;
    //    }
    //}


    //private void JoinOrCreateRandomRoom()
    //{
    //    PhotonNetwork.JoinRandomRoom();
    //}

    //public override void OnLeftRoom()
    //{
    //    //MainPanel.SetActive(true);
    //    //GamePanel.SetActive(false);
    //    //ConnectingPanel.SetActive(false);
    //    PhotonNetwork.Disconnect();
    //    //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    //}

    // public override void OnDisconnected(DisconnectCause cause)
    //{
    //    uiManager.RemoveAllJoinedPlayers();
    //    Debug.Log($"Disconnected: {cause}");

    //    if (DataManager.Instance.CurrentGameState == GameState.Init)
    //    {
    //        CancelInvoke(nameof(CountdownToStart));
    //        menuManager.ShowNoInternetPopUp();
    //    }
    //    else if (DataManager.Instance.CurrentGameState != GameState.Play)
    //    {
    //        HandleDisconnection(cause);
    //    }


    //    // if (cause == DisconnectCause.Exception || cause == DisconnectCause.ExceptionOnConnect)
    //    // {
    //    //     // Start a 30-second timer to wait for reconnection
    //    //     if (rejoinTimerCoroutine != null)
    //    //     {
    //    //         StopCoroutine(rejoinTimerCoroutine);
    //    //     }
    //    //     rejoinTimerCoroutine = StartCoroutine(RejoinTimer());
    //    //     
    //    //     // Try to reconnect and rejoin
    //    //     PhotonNetwork.ReconnectAndRejoin();
    //    // }
    //}

    //private void HandleDisconnection(DisconnectCause cause)
    //{
    //    switch (cause)
    //    {
    //        case DisconnectCause.ClientTimeout:
    //        case DisconnectCause.ServerTimeout:
    //            // Debug.Log("Timeout occurred. Attempting to reconnect...");
    //            // PhotonNetwork.ReconnectAndRejoin();
    //            // break;
    //        case DisconnectCause.DisconnectByServerLogic:
    //            Debug.Log("Disconnected by server logic. Returning to main menu.");
    //            // Load main menu or show appropriate UI
    //            uiManager.OnGameFinished(DiceColor.Unknown);
    //            break;

    //        default:
    //            Debug.Log("Unhandled disconnection cause. Returning to main menu.");
    //            // Load main menu or show appropriate UI
    //            uiManager.OnGameFinished(DiceColor.Unknown);
    //            break;
    //    }
    //}

    private IEnumerator RejoinTimer()
    {
        int waitingTime = 30;
        var waitForSeconds = new WaitForSeconds(1);

        while (waitingTime > 0)
        {
            UIManager.Instance.ShowWaitingTime(waitingTime);
            yield return waitForSeconds;
            waitingTime--;
        }

        if (!hasRejoined)
        {
            // If the player hasn't rejoined in 30 seconds, disconnect them
            Debug.Log("Rejoin timeout. Disconnecting from the room.");
            //   PhotonNetwork.LeaveRoom();

            // Optionally, you can set a flag or use PlayerPrefs to ensure
            // the player cannot attempt to join the room again
            PlayerPrefs.SetInt("CanRejoinRoom", 0);
        }
    }

    //public override void OnErrorInfo(ErrorInfo errorInfo)
    //{
    //    Debug.Log($"OnErrorInfo: {errorInfo}");
    //}

    //public override void OnPlayerEnteredRoom(Player newPlayer)
    //{

    //}

    //public override void OnJoinRoomFailed(short returnCode, string message)
    //{
    //    Debug.LogError($"OnJoinRoomFailed, Error Code: {returnCode}, Message: {message}");
    //    uiManager.errorPopUp.ShowMessagePanel(message);
    //}

    //public override void OnJoinRandomFailed(short returnCode, string message)
    //{
    //    SetAndShowConnectingStatus("No available rooms. Creating a new room...");

    //    CreateRandomRoom();
    //}

    //public override void OnJoinedRoom()
    //{
    //    if (DataManager.Instance.CurrentGameState == GameState.Play)
    //    {
    //        // If the player successfully rejoins, stop the timer
    //        hasRejoined = true;
    //        if (rejoinTimerCoroutine != null)
    //        {
    //            StopCoroutine(rejoinTimerCoroutine);
    //        }

    //        Debug.Log("Successfully rejoined the room.");
    //        return;
    //    }

    //    Debug.Log("OnJoinedRoom");
    //    ConnectingPanel.SetActive(true);
    //    GamePanel.SetActive(false);
    //    ConnectingStatusText.text = "Waiting for players to join...";

    //    switch (DataManager.Instance.CurrentRoomType)
    //    {
    //        case RoomType.Random when PhotonNetwork.IsMasterClient:
    //            UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Red, PhotonNetwork.LocalPlayer);
    //            photonView.RPC(nameof(StartCountDown), RpcTarget.All, maxJoinWaitingTime);
    //            return;

    //        case RoomType.Random when !PhotonNetwork.IsMasterClient:
    //            RequestMasterPlayerToAssignDiceColor();
    //            return;

    //        case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Join:
    //            CheckBackBalanceAndRequestToGetDiceColor();
    //            return;

    //        case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Create:
    //            UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Red, PhotonNetwork.LocalPlayer);
    //            return;
    //    }
    //}

    //public override void OnJoinedLobby()
    //{
    //    Debug.Log("OnJoinedLobby");
    //    JoinOrCreateRandomRoom();
    //}
    #endregion Photon Callbacks
    //private void RequestMasterPlayerToAssignDiceColor()
    //{
    //    photonView.RPC(nameof(RequestMasterPlayerToAssignDiceColorRPC), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    //}

    //[PunRPC]
    //private void RequestMasterPlayerToAssignDiceColorRPC(int actorNumber)
    //{
    //    Player player = PhotonNetwork.CurrentRoom.Players[actorNumber];

    //    AssignDiceColorToJoinedPlayer(player);
    //}

    //private void AssignDiceColorToJoinedPlayer(Player newPlayer)
    //{
    //    Debug.Log($"NewPlayerJoined: {newPlayer.NickName}");

    //    //if (DataManager.Instance.MaxPlayerNumberForCurrentBoard == 2)
    //    //{
    //    //    UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Yellow, newPlayer);
    //    //    return;
    //    //}

    //    switch (PhotonNetwork.CurrentRoom.PlayerCount)
    //    {
    //        default:
    //        case 2:
    //            UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Yellow, newPlayer);
    //            break;

    //        case 3:
    //            UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Blue, newPlayer);
    //            break;

    //        case 4:
    //            UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Green, newPlayer);
    //            break;
    //    }
    //}

    //public override void OnPlayerLeftRoom(Player otherPlayer)
    //{
    //    if (DataManager.Instance.CurrentGameState == GameState.Play)
    //    {
    //        Debug.Log($"OnPlayerLeft, PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, LeftPlayerName: {otherPlayer.NickName}");

    //        // if (DataManager.Instance.MaxPlayerNumberForCurrentBoard == 2 && DataManager.Instance.CurrentGameState == GameState.Play)
    //        // {
    //        //     GameManager.Instance.RaiseGameOverCustomEvent(DataManager.Instance.OwnDiceColor);
    //        //     return;
    //        // }
    //        Debug.Log($"OnPlayerLeft, PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, LeftPlayerName: {otherPlayer.NickName}, LocalPlayer: {PhotonNetwork.LocalPlayer.NickName}");
    //        switch (PhotonNetwork.CurrentRoom.PlayerCount)
    //        {
    //            case < 2:
    //                GameManager.Instance.RaiseGameOverCustomEvent(DataManager.Instance.OwnDiceColor);
    //                return;
    //            case >= 2 when otherPlayer.CustomProperties.ContainsKey(DICE_COLOR_KEY):
    //                DiceColor color = (DiceColor)otherPlayer.CustomProperties[DICE_COLOR_KEY];
    //                GameManager.Instance.RemovePlayerToCurrentRoomPlayersList(color);
    //                return;
    //        }
    //    }

    //    if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
    //    {
    //        AbandonedRoomDueToInsufficientMembers();
    //    }

    //    if (otherPlayer.CustomProperties.ContainsKey(DICE_COLOR_KEY))
    //    {
    //        UIManager.Instance.RemoveJoinedPlayer((DiceColor)otherPlayer.CustomProperties[DICE_COLOR_KEY]);
    //    }
    //}

    //private void HandlePlayerDisconnection(Player disconnectedPlayer)
    //{
    //    // Handle any logic needed when a remote player disconnects
    //    // e.g., update UI, redistribute resources, etc.
    //}

    //private void HandleSelfDisconnection(DisconnectCause cause)
    //{
    //    // Handle any logic needed when the local player disconnects
    //    // e.g., show a reconnect UI or return to the main menu
    //    if (cause == DisconnectCause.ClientTimeout )
    //    {
    //        Debug.Log("Disconnected due to timeout. Returning to the main menu.");
    //        //PhotonNetwork.LoadLevel("MainMenu"); // Example: Return to the main menu
    //    }
    //}

    //public void UpdateJoinedPlayerDiceColorCustomProperties(DiceColor diceColor, Player newPlayer)
    //{
    //    Debug.Log($"UpdateJoinedPlayerDiceColorCustomProperties for {newPlayer.NickName}, {diceColor}");

    //    // Define the new custom properties for Player2
    //   // ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable() { { DICE_COLOR_KEY, (int)diceColor } };

    //    // Update Player2's custom properties
    //    newPlayer.SetCustomProperties(customProperties);
    //}

    //public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    //{
    //    DiceColor diceColor = DiceColor.Unknown;

    //    if (changedProps.ContainsKey(DICE_COLOR_KEY))
    //    {
    //        diceColor = (DiceColor)changedProps[DICE_COLOR_KEY];
    //        // Apply the new health value to Player2's data
    //        Debug.Log($"DiceColor: {diceColor}, PlayerName: {targetPlayer.NickName}, CanJoin: {canJoin}");
    //    }

    //    // Check if the custom properties updated are for Player2 (this player)
    //    if (targetPlayer == PhotonNetwork.LocalPlayer)
    //    {
    //        // Example: Handle the custom properties that have been updated
    //        DataManager.Instance.SetOwnDiceColor(diceColor);
    //        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard((byte)PhotonNetwork.CurrentRoom.MaxPlayers);
    //    }

    //    switch (PhotonNetwork.IsMasterClient)
    //    {
    //        case true when diceColor != DiceColor.Unknown && DataManager.Instance.CurrentRoomType == RoomType.Private:
    //            UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenPrivateJoinedPlayerPanel);
    //            return;

    //        case true when diceColor != DiceColor.Unknown && DataManager.Instance.CurrentRoomType == RoomType.Random:
    //            UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenJoinMultiplayerPanel);
    //            return;

    //        case true when DataManager.Instance.CurrentRoomType == RoomType.Private && PhotonNetwork.CurrentRoom.PlayerCount == DataManager.Instance.MaxPlayerNumberForCurrentBoard:
    //            UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer);
    //            Invoke(nameof(StartGame), 4);
    //            break;
    //    }

    //    switch (canJoin)
    //    {
    //        case true when DataManager.Instance.CurrentRoomType == RoomType.Random:
    //            UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenJoinMultiplayerPanel);
    //            break;
    //        case true when DataManager.Instance.CurrentRoomType == RoomType.Private:
    //            UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenPrivateJoinedPlayerPanel);
    //            break;
    //    }
    //}

    private void OpenJoinMultiplayerPanel()
    {
        UIManager.Instance.OpenJoinedPlayerPanel(currentRoomId);
    }

    private void OpenPrivateJoinedPlayerPanel()
    {
        UIManager.Instance.OpenPrivateJoinedPlayerPanel(currentRoomId);
    }

    //  [PunRPC]
    public void StartCountDown(int time)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            Debug.Log($"[NetworkManager] Starting countdown with {time} seconds");
            timer = time;
            uiManager.OpenJoinedPlayerPanelForOnlineMatch();
            UpdateCountdownDisplay(timer);

            if (IsInvoking(nameof(CountdownToStart)))
            {
                CancelInvoke(nameof(CountdownToStart));
            }

            InvokeRepeating(nameof(CountdownToStart), 1f, 1f);
        });
    }
    private void CountdownToStart()
    {
        if (timer <= 0) return;

        float previousTimer = timer;
        timer -= 1f;

        Debug.Log($"[NetworkManager] Countdown: {previousTimer} -> {timer}");

        UpdateCountdownDisplay(timer);

        if (timer <= 0)
        {
            CancelInvoke(nameof(CountdownToStart));
            CheckRoomStatusAndStartGame();
        }
    }
    private void CheckRoomStatusAndStartGame()
    {
        int playerCount = SocketIOManager.Instance.GetPlayerCount();

        if (playerCount >= 2)
        {
            Debug.Log($"[NetworkManager] Starting game with {playerCount} players");
            StartGame();
        }
        else
        {
            Debug.Log($"[NetworkManager] Only {playerCount} player(s) - not enough to start");
            CancelAndReturnToMainMenu();
        }
    }

    // [PunRPC]
    private void UpdateCountdownDisplay(float time)
    {
        string message;

        if (time <= 0)
        {
            message = "Starting game...";
        }
        else
        {
            message = $"Game starting in {Mathf.Ceil(time)} seconds...";

            // Add player count information if available
            int playerCount = SocketIOManager.Instance.GetPlayerCount();
            int maxPlayers = DataManager.Instance.MaxPlayerNumberForCurrentBoard;
            message += $"\nPlayers: {playerCount}/{maxPlayers}";
        }

        Debug.Log($"[NetworkManager] UpdateCountdownDisplay: {message}");
        uiManager.UpdateCountDownTimerText(message);
    }

    private void AbandonedRoomDuetoInsufficientMembers()
    {
        // PhotonNetwork.Disconnect();
        Time.timeScale = 0;
        UIManager.Instance.errorPopUp.ShowMessagePanel("The room has been abandoned, try to create or join an another room.");
    }
    public void StartGame()
    {
        Debug.Log("[NetworkManager] StartGame() called");
        Debug.Log($"[NetworkManager] StartGame: Current user type: {DataManager.Instance.CurrentUserType}, Player count: {SocketIOManager.Instance.GetPlayerCount()}");
        Debug.Log($"[NetworkManager] StartGame: Room type: {DataManager.Instance.CurrentRoomType}, Room mode: {DataManager.Instance.CurrentRoomMode}");
        Debug.Log($"[NetworkManager] StartGame: Max players: {DataManager.Instance.MaxPlayerNumberForCurrentBoard}, Current entry fee: {DataManager.Instance.CurrentEntryFee}");
        Debug.Log($"[NetworkManager] StartGame: Current room ID: {currentRoomId}, Own dice color: {DataManager.Instance.OwnDiceColor}");

        // Close the room to new players using Socket.IO
        if (SocketIOManager.Instance.IsConnected())
        {
            Debug.Log("[NetworkManager] StartGame: Room will be closed to new players by server");
            Debug.Log($"[NetworkManager] StartGame: Socket.IO connection status: Connected");
            // No need to explicitly close the room - the server will handle this
        }
        else
        {
            Debug.LogWarning("[NetworkManager] StartGame: Socket.IO not connected when trying to start game");
            Debug.LogError("[NetworkManager] StartGame: Cannot start game properly without Socket.IO connection");
        }

        // Check if we have enough coins
        Debug.Log($"[NetworkManager] StartGame: Player coins: {DataManager.Instance.Coins}, Required entry fee: {DataManager.Instance.CurrentEntryFee}");
        if (DataManager.Instance.Coins < DataManager.Instance.CurrentEntryFee)
        {
            Debug.LogWarning($"[NetworkManager] StartGame: Not enough coins to start game! Coins: {DataManager.Instance.Coins}, Required: {DataManager.Instance.CurrentEntryFee}");
        }

        if (DataManager.Instance.CurrentUserType == UserType.APP)
        {
            Debug.Log("[NetworkManager] StartGame: Initiating session for APP user");
            Debug.Log("[NetworkManager] StartGame: Will create game session on server");
            InitiateSessionAnsStartGame();
            return;
        }

        // For non-APP users, the game can start directly
        Debug.Log("[NetworkManager] StartGame: Starting game directly for non-APP user");
        Debug.Log("[NetworkManager] StartGame: Waiting for game_started event from server");
        // The server will broadcast the game_started event
    }

    // [PunRPC]
    private void OnGameStart()
    {
        StartCoroutine(Animtionplay());

    }

    // [PunRPC]
    private void OnGameStartWithGameSession(string session)
    {
        DataManager.Instance.SetSessionId(session);
        StartCoroutine(Animtionplay());
    }

    IEnumerator Animtionplay()
    {
        Debug.Log("[NetworkManager] Animtionplay: Starting game animation sequence");

        string[] playerColors = { "Red", "Blue", "Green", "Yellow" };
        List<Transform> validStartPositions = new List<Transform>();

        foreach (string color in playerColors)
        {
            GameObject startObject = GameObject.Find($"JoinedPlayer_{color}");
            if (startObject != null)
            {
                Debug.Log($"[NetworkManager] Animtionplay: Found player object for color {color}");
                validStartPositions.Add(startObject.transform);
                CoinMove.instance.MoveCoins(startObject.transform);
                yield return new WaitForSeconds(1f);
                startObject = null;
            }
            else
            {
                Debug.Log($"[NetworkManager] Animtionplay: No player object found for color {color}");
            }
        }

        Debug.Log("[NetworkManager] Animtionplay: Enabling VS Image animator");
        GameObject vsImage = GameObject.Find("VS Image");
        if (vsImage != null)
        {
            vsImage.GetComponent<Animator>().enabled = true;
        }
        else
        {
            Debug.LogWarning("[NetworkManager] Animtionplay: Could not find VS Image object");
        }

        // Calculate total match fee based on entry fee and player count
        int playerCount = SocketIOManager.Instance.GetPlayerCount();
        totalvalue = playerCount * DataManager.Instance.CurrentEntryFee;
        Debug.Log($"[NetworkManager] Animtionplay: Total match fee: {totalvalue} for {playerCount} players (EntryFee: {DataManager.Instance.CurrentEntryFee})");

        // Update UI with total match fee
        if (DataManager.Instance.CurrentRoomType == RoomType.Random)
        {
            string readableValue = Helper.GetReadableNumber(totalvalue);
            Debug.Log($"[NetworkManager] Animtionplay: Updating public match fee UI: {readableValue}");
            uiManager.publictotalmatchfee.text = readableValue;
        }
        else if (DataManager.Instance.CurrentRoomType == RoomType.Private)
        {
            string readableValue = Helper.GetReadableNumber(totalvalue);
            Debug.Log($"[NetworkManager] Animtionplay: Updating private match fee UI: {readableValue}");
            uiManager.privatetotalmatchfee.text = readableValue;
        }

        Debug.Log("[NetworkManager] Animtionplay: Waiting 5 seconds before proceeding to game");
        yield return new WaitForSeconds(5f);

        Debug.Log("[NetworkManager] Animtionplay: Initiating actual game");
        InitiateGame();

        Debug.Log("[NetworkManager] Animtionplay: Activating game panels");
        MainPanel.SetActive(false);
        ConnectingPanel.SetActive(false);
        GamePanel.SetActive(true);

        // Set max player count for the current board based on actual player count
        Debug.Log($"[NetworkManager] Animtionplay: Setting max player count to {playerCount}");
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard((byte)playerCount);

        Debug.Log("[NetworkManager] Animtionplay: Setting game state to PLAY");
        DataManager.Instance.SetCurrentGameState(GameState.Play);

        Debug.Log("[NetworkManager] Animtionplay: Starting multiplayer game in UI");
        uiManager.StartMultiplayerGame();
    }

    private void CancelAndReturnToMainMenu()
    {
        Debug.Log("[NetworkManager] CancelAndReturnToMainMenu() called");

        // Disconnect from the Socket.IO server
        if (SocketIOManager.Instance.IsConnected())
        {
            Debug.Log("[NetworkManager] Disconnecting from Socket.IO server");
            // The disconnect will happen in OnApplicationQuit in SocketIOManager
        }

        uiManager.UpdateCountDownTimerText("Not enough players joined. Returning to the main menu...");
        uiManager.AddBackButtonAction();
        CancelInvoke(nameof(ShowOnlinePlayerCount));
    }

    #region Game Session    
    private void InitiateSessionAnsStartGame()
    {
        Debug.Log("[NetworkManager] InitiateSessionAnsStartGame - Creating game session");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: GameID: {DataManager.Instance.GameId}, RoomID: {currentRoomId}, EntryFee: {DataManager.Instance.CurrentEntryFee}");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Current player count: {SocketIOManager.Instance.GetPlayerCount()}, Max players: {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Own dice color: {DataManager.Instance.OwnDiceColor}, User type: {DataManager.Instance.CurrentUserType}");

        uiManager.popUp.ShowMessagePanel("Creating game session, please wait...");
        Debug.Log("[NetworkManager] InitiateSessionAnsStartGame: Displayed 'Creating game session' popup");

        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField(GameId, DataManager.Instance.GameId);
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Added GameId field: {DataManager.Instance.GameId}");

        // Use current room ID and host player from Socket.IO
        if (string.IsNullOrEmpty(currentRoomId))
        {
            Debug.LogError("[NetworkManager] InitiateSessionAnsStartGame: Room ID is null, cannot create game session");
            Debug.LogError("[NetworkManager] InitiateSessionAnsStartGame: Socket.IO connectivity status: " + (SocketIOManager.Instance.IsConnected() ? "Connected" : "Disconnected"));
            uiManager.errorPopUp.ShowMessagePanel("Error creating game session: Invalid room ID");
            uiManager.popUp.CloseMessagePanel();
            return;
        }

        // Add player IDs to form
        string currentUserId = DataManager.Instance.CurrentUser.id.ToString();
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Adding current user ID: {currentUserId}");
        wwwForm.AddField("users[]", currentUserId);
        wwwForm.AddField(RoomId, currentRoomId);
        wwwForm.AddField(BoardAmount, DataManager.Instance.CurrentEntryFee);

        // Log all form data
        Debug.Log("[NetworkManager] InitiateSessionAnsStartGame: Form data summary:");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: - GameId: {DataManager.Instance.GameId}");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: - Users[]: {currentUserId}");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: - RoomId: {currentRoomId}");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: - BoardAmount: {DataManager.Instance.CurrentEntryFee}");

        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Creating session with roomId: {currentRoomId}, boardAmount: {DataManager.Instance.CurrentEntryFee}");
        Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Current API token length: {(string.IsNullOrEmpty(DataManager.Instance.Token) ? 0 : DataManager.Instance.Token.Length)}");
        Debug.Log("[NetworkManager] InitiateSessionAnsStartGame: Calling APIHandler.InitiateSession...");

        APIHandler.Instance.InitiateSession(wwwForm, DataManager.Instance.Token, response =>
        {
            var byteData = wwwForm.data;
            string data = Encoding.UTF8.GetString(byteData);

            Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Session API response received: {response}");

            SessionInitResponse sessionInitResponse = JsonUtility.FromJson<SessionInitResponse>(response);

            if (sessionInitResponse is { status: true })
            {
                Debug.Log($"[NetworkManager] InitiateSessionAnsStartGame: Session created successfully with ID: {sessionInitResponse.game_session}");
                DataManager.Instance.SetSessionId(sessionInitResponse.game_session);

                // Start the game and notify all clients
                StartCoroutine(Animtionplay());
                return;
            }

            LogOutResponse res = JsonUtility.FromJson<LogOutResponse>(response);
            if (res == null)
            {
                Debug.LogError("[NetworkManager] InitiateSessionAnsStartGame: Failed to parse error response");
                uiManager.errorPopUp.ShowMessagePanel("Something went wrong, please try again later.");
                uiManager.popUp.CloseMessagePanel();
                return;
            }

            Debug.LogError($"[NetworkManager] InitiateSessionAnsStartGame: Session creation error: {res}");
            uiManager.errorPopUp.ShowMessagePanel("Something went wrong, please try again later.");
            uiManager.popUp.CloseMessagePanel();
        });
    }
    #endregion Game Session

    #region Create Room

    #endregion Create Room

    #region Join Room


    private void ShowUnableToJoinRoomDueToLowBalance()
    {
        uiManager.CloseAllMultiplayerPanel();
        uiManager.OpenUnableToJoinRoomPanel();
    }
    #endregion Join Room

    #region Common Methods for Private Or Random Room

    private void CloseCurrentRoom()
    {
        // PhotonNetwork.CurrentRoom.IsOpen = false; // No new players can join
        // PhotonNetwork.CurrentRoom.IsVisible = false; // Room is not visible in the lobby
    }

    public void StartGameWithDelayTime()
    {
        Invoke(nameof(InitiateGame), maxStartGameDelayTime);
    }


    private void InitiateGame()
    {
        CancelInvoke(nameof(ShowOnlinePlayerCount));
        StopWaitingCoroutine();
        uiManager.popUp.CloseMessagePanel();
        uiManager.CloseAllMultiplayerPanel();
        uiManager.AddBackButtonAction();

        //StartGame();
    }

    public void StartWaitingForOtherPlayer()
    {
        uiManager.CloseAllMultiplayerPanel();
        uiManager.popUp.ShowMessagePanel("Waiting other player to join...");
        waitingCoroutine ??= StartCoroutine(WaitForOtherPlayer());
    }


    //internal bool IsRoomFull()
    //{
    //    //return PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers;
    //}

    #endregion Common Methods for Private Or Random Room

    // This can be called when we receive a timer event from Socket.IO
    public void StartCountdownFromServer(int seconds)
    {
        Debug.Log($"[NetworkManager] StartCountdownFromServer: Starting {seconds} second countdown from server event");

        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
           
            uiManager.OpenJoinedPlayerPanelForOnlineMatch();
            

            // Start or update the countdown
            timer = seconds;
            UpdateCountdownDisplay(timer);

            if (IsInvoking(nameof(CountdownToStart)))
            {
                CancelInvoke(nameof(CountdownToStart));
            }

            InvokeRepeating(nameof(CountdownToStart), 1f, 1f);
        });
    }

}
