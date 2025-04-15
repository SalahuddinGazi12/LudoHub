using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;

public class NetworkManager : MonoBehaviourPunCallbacks
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
    PhotonView pv;
    
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

        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ConfigurePhotonSettings();
    }

    private void ConfigurePhotonSettings()
    {
        // Version and region
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = preferredRegion;

        // Network optimizations for Bangladesh
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 15;

        // Timeout and reliability settings
        var peer = PhotonNetwork.NetworkingClient.LoadBalancingPeer;
        peer.DisconnectTimeout = 30000; // 30 seconds
        peer.QuickResendAttempts = 3;
        peer.SentCountAllowance = 10;
    }


    private void Start()
    {
        //PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 30000;
        uiManager = UIManager.Instance;
        menuManager = MenuManager.Instance;
        pv = GetComponent<PhotonView>();
    }

    private void OnDestroy()
    {
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();
        
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
        PhotonNetwork.LocalPlayer.NickName = DataManager.Instance.CurrentUser.name;

        if (DataManager.Instance.CurrentUserType != UserType.APP)
            return;

        ExitGames.Client.Photon.Hashtable hashTable = new ExitGames.Client.Photon.Hashtable()
        {
            { UserUid, DataManager.Instance.CurrentUser.id }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(hashTable);
    }

    #region Random Room
    public void PlayWithRandomPlayer()
    {
        uiManager.CloseAllMultiplayerPanel();
        StartToDisplayPhotonStateCoroutine();

        //Connect();
        OnOnlineButtonClick();
    }

    private void JoinRandomRoom()
    {
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4); //Setting max player to 4

        int entryFee = DataManager.Instance.CurrentEntryFee;
        byte maxPlayer = DataManager.Instance.MaxPlayerNumberForCurrentBoard;

        userType = DataManager.Instance.CurrentUserType;

        Debug.Log($"Looking for room with {Enum.GetName(typeof(UserType), userType)} where entry fee is {entryFee} and maxPlayer is {maxPlayer}");

        ExitGames.Client.Photon.Hashtable roomProperties = new()
        {
            { UserTypeKey, userType },
            { EntryFeesKey,  entryFee},
        };

        PhotonNetwork.JoinRandomRoom(roomProperties, maxPlayer);
    }

    private void CreateRandomRoom()
    {
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4); //Setting max player to 4

        int entryFee = DataManager.Instance.CurrentEntryFee;
        byte maxPlayer = DataManager.Instance.MaxPlayerNumberForCurrentBoard;

        userType = DataManager.Instance.CurrentUserType;

        Debug.Log($"Creating Looking for room with {Enum.GetName(typeof(UserType), userType)} where entry fee is {entryFee} and maxPlayer is {maxPlayer}");

        RoomOptions roomOptions = new RoomOptions
        {
            CustomRoomPropertiesForLobby = new[] { UserTypeKey, EntryFeesKey },
            MaxPlayers = maxPlayer,

            IsOpen = true,
            IsVisible = true,

            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { UserTypeKey, userType }, { EntryFeesKey, entryFee } }
        };


        PhotonNetwork.CreateRoom(null, roomOptions);
    }
    #endregion\ Random Room

    private void ShowOnlinePlayerCount()
    {
        uiManager.ShowOnlinePlayerCount(PhotonNetwork.PlayerList.Length);
    }
    
    public void Connect()
    {
        userType = DataManager.Instance.CurrentUserType;

        InvokeRepeating(nameof(ShowOnlinePlayerCount), 1f, 1f);
        
        //StartToDisplayPhotonStateCoroutine();
        uiManager.RemoveAllJoinedPlayers();

        switch (PhotonNetwork.IsConnected)
        {
            case true when DataManager.Instance.CurrentRoomType == RoomType.Random:
                Debug.Log("JoinRandomRoom");
                JoinRandomRoom();
                break;

            case true when DataManager.Instance.CurrentRoomType == RoomType.Private && DataManager.Instance.CurrentRoomMode == RoomMode.Create:
                CreateCustomRoom();
                break;

            case true when DataManager.Instance.CurrentRoomType == RoomType.Private && DataManager.Instance.CurrentRoomMode == RoomMode.Join:
                JoinPrivateRoom(uiManager.GetInputtedRoomId());
                break;

            default:
                PhotonNetwork.ConnectUsingSettings();
                break;
        }

    }

    private void SetAndShowConnectingStatus(string message)
    {
        ConnectingPanel.SetActive(true);
        ConnectingStatusText.text = message;
    }

    private void HideConnectingStatus()
    {
        ConnectingPanel.SetActive(false);
        ConnectingStatusText.text = "";
    }

    public void OnOnlineButtonClick()
    {
        UIManager.Instance.menuManager.HideBackButton();
        SetAndShowConnectingStatus("Connecting to server...");
        
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        Connect();
    }

    private void CheckBackBalanceAndRequestToGetDiceColor()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(EntryFeesKey)) 
            return;
        
        var feeObj = PhotonNetwork.CurrentRoom.CustomProperties[EntryFeesKey];
        int entryFee = Convert.ToInt32(feeObj);

        Debug.Log($"EntryFees: {entryFee}, Coins: {DataManager.Instance.Coins}, feeObj: {feeObj}");

        if (entryFee > DataManager.Instance.Coins)
        {
            canJoin = false;
            DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);
            DataManager.Instance.SetCurrentRoomType(RoomType.Private);
            ShowUnableToJoinRoomDueToLowBalance();
            SetAndShowConnectingStatus("");
            Debug.Log("Returning due to no coins");
            return;
        }

        DataManager.Instance.SetCurrentEntryFees(entryFee);
        RequestMasterPlayerToAssignDiceColor();
    }

    #region Photon Callbacks
    public override void OnConnectedToMaster()
    {
        //JoinOrCreateRandomRoom();

        switch (DataManager.Instance.CurrentRoomType)
        {
            case RoomType.Random:
                SetAndShowConnectingStatus("Connected to server. Joining room...");
                JoinRandomRoom();
                break;

            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Create:
                SetAndShowConnectingStatus("Connected to server. Creating new room...");
                CreateCustomRoom();
                break;

            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Join:
                SetAndShowConnectingStatus("Connected to server. Joining room...");
                JoinPrivateRoom(UIManager.Instance.GetInputtedRoomId());
                break;
        }
    }


    //private void JoinOrCreateRandomRoom()
    //{
    //    PhotonNetwork.JoinRandomRoom();
    //}

    public override void OnLeftRoom()
    {
        //MainPanel.SetActive(true);
        //GamePanel.SetActive(false);
        //ConnectingPanel.SetActive(false);
        PhotonNetwork.Disconnect();
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        uiManager.RemoveAllJoinedPlayers();
        Debug.Log($"Disconnected: {cause}");

        if (DataManager.Instance.CurrentGameState == GameState.Init)
        {
            CancelInvoke(nameof(CountdownToStart));
            menuManager.ShowNoInternetPopUp();
        }
        else if (DataManager.Instance.CurrentGameState != GameState.Play)
        {
            HandleDisconnection(cause);
        }
        
        
        // if (cause == DisconnectCause.Exception || cause == DisconnectCause.ExceptionOnConnect)
        // {
        //     // Start a 30-second timer to wait for reconnection
        //     if (rejoinTimerCoroutine != null)
        //     {
        //         StopCoroutine(rejoinTimerCoroutine);
        //     }
        //     rejoinTimerCoroutine = StartCoroutine(RejoinTimer());
        //     
        //     // Try to reconnect and rejoin
        //     PhotonNetwork.ReconnectAndRejoin();
        // }
    }
    
    private void HandleDisconnection(DisconnectCause cause)
    {
        switch (cause)
        {
            case DisconnectCause.ClientTimeout:
            case DisconnectCause.ServerTimeout:
                // Debug.Log("Timeout occurred. Attempting to reconnect...");
                // PhotonNetwork.ReconnectAndRejoin();
                // break;
            case DisconnectCause.DisconnectByServerLogic:
                Debug.Log("Disconnected by server logic. Returning to main menu.");
                // Load main menu or show appropriate UI
                uiManager.OnGameFinished(DiceColor.Unknown);
                break;

            default:
                Debug.Log("Unhandled disconnection cause. Returning to main menu.");
                // Load main menu or show appropriate UI
                uiManager.OnGameFinished(DiceColor.Unknown);
                break;
        }
    }
    
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
            PhotonNetwork.LeaveRoom();
            
            // Optionally, you can set a flag or use PlayerPrefs to ensure
            // the player cannot attempt to join the room again
            PlayerPrefs.SetInt("CanRejoinRoom", 0);
        }
    }

    public override void OnErrorInfo(ErrorInfo errorInfo)
    {
        Debug.Log($"OnErrorInfo: {errorInfo}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {

    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"OnJoinRoomFailed, Error Code: {returnCode}, Message: {message}");
        uiManager.errorPopUp.ShowMessagePanel(message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        SetAndShowConnectingStatus("No available rooms. Creating a new room...");

        CreateRandomRoom();
    }
    
    public override void OnJoinedRoom()
    {
        if (DataManager.Instance.CurrentGameState == GameState.Play)
        {
            // If the player successfully rejoins, stop the timer
            hasRejoined = true;
            if (rejoinTimerCoroutine != null)
            {
                StopCoroutine(rejoinTimerCoroutine);
            }
        
            Debug.Log("Successfully rejoined the room.");
            return;
        }
        
        Debug.Log("OnJoinedRoom");
        ConnectingPanel.SetActive(true);
        GamePanel.SetActive(false);
        ConnectingStatusText.text = "Waiting for players to join...";

        switch (DataManager.Instance.CurrentRoomType)
        {
            case RoomType.Random when PhotonNetwork.IsMasterClient:
                UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Red, PhotonNetwork.LocalPlayer);
                photonView.RPC(nameof(StartCountDown), RpcTarget.All, maxJoinWaitingTime);
                return;
            
            case RoomType.Random when !PhotonNetwork.IsMasterClient:
                RequestMasterPlayerToAssignDiceColor();
                return;
            
            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Join:
                CheckBackBalanceAndRequestToGetDiceColor();
                return;
            
            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Create:
                UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Red, PhotonNetwork.LocalPlayer);
                return;
        }
    }

    //public override void OnJoinedLobby()
    //{
    //    Debug.Log("OnJoinedLobby");
    //    JoinOrCreateRandomRoom();
    //}
    #endregion Photon Callbacks
    private void RequestMasterPlayerToAssignDiceColor()
    {
        photonView.RPC(nameof(RequestMasterPlayerToAssignDiceColorRPC), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    private void RequestMasterPlayerToAssignDiceColorRPC(int actorNumber)
    {
        Player player = PhotonNetwork.CurrentRoom.Players[actorNumber];
        
        AssignDiceColorToJoinedPlayer(player);
    }

    private void AssignDiceColorToJoinedPlayer(Player newPlayer)
    {
        Debug.Log($"NewPlayerJoined: {newPlayer.NickName}");

        //if (DataManager.Instance.MaxPlayerNumberForCurrentBoard == 2)
        //{
        //    UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Yellow, newPlayer);
        //    return;
        //}

        switch (PhotonNetwork.CurrentRoom.PlayerCount)
        {
            default:
            case 2:
                UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Yellow, newPlayer);
                break;

            case 3:
                UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Blue, newPlayer);
                break;

            case 4:
                UpdateJoinedPlayerDiceColorCustomProperties(DiceColor.Green, newPlayer);
                break;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (DataManager.Instance.CurrentGameState == GameState.Play)
        {
            Debug.Log($"OnPlayerLeft, PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, LeftPlayerName: {otherPlayer.NickName}");

            // if (DataManager.Instance.MaxPlayerNumberForCurrentBoard == 2 && DataManager.Instance.CurrentGameState == GameState.Play)
            // {
            //     GameManager.Instance.RaiseGameOverCustomEvent(DataManager.Instance.OwnDiceColor);
            //     return;
            // }
            Debug.Log($"OnPlayerLeft, PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, LeftPlayerName: {otherPlayer.NickName}, LocalPlayer: {PhotonNetwork.LocalPlayer.NickName}");
            switch (PhotonNetwork.CurrentRoom.PlayerCount)
            {
                case < 2:
                    GameManager.Instance.RaiseGameOverCustomEvent(DataManager.Instance.OwnDiceColor);
                    return;
                case >= 2 when otherPlayer.CustomProperties.ContainsKey(DICE_COLOR_KEY):
                    DiceColor color = (DiceColor)otherPlayer.CustomProperties[DICE_COLOR_KEY];
                    GameManager.Instance.RemovePlayerToCurrentRoomPlayersList(color);
                    return;
            }
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            AbandonedRoomDueToInsufficientMembers();
        }

        if (otherPlayer.CustomProperties.ContainsKey(DICE_COLOR_KEY))
        {
            UIManager.Instance.RemoveJoinedPlayer((DiceColor)otherPlayer.CustomProperties[DICE_COLOR_KEY]);
        }
    }

    private void HandlePlayerDisconnection(Player disconnectedPlayer)
    {
        // Handle any logic needed when a remote player disconnects
        // e.g., update UI, redistribute resources, etc.
    }

    private void HandleSelfDisconnection(DisconnectCause cause)
    {
        // Handle any logic needed when the local player disconnects
        // e.g., show a reconnect UI or return to the main menu
        if (cause == DisconnectCause.ClientTimeout )
        {
            Debug.Log("Disconnected due to timeout. Returning to the main menu.");
            //PhotonNetwork.LoadLevel("MainMenu"); // Example: Return to the main menu
        }
    }

    public void UpdateJoinedPlayerDiceColorCustomProperties(DiceColor diceColor, Player newPlayer)
    {
        Debug.Log($"UpdateJoinedPlayerDiceColorCustomProperties for {newPlayer.NickName}, {diceColor}");

        // Define the new custom properties for Player2
        ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable() { { DICE_COLOR_KEY, (int)diceColor } };

        // Update Player2's custom properties
        newPlayer.SetCustomProperties(customProperties);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        DiceColor diceColor = DiceColor.Unknown;
        
        if (changedProps.ContainsKey(DICE_COLOR_KEY))
        {
            diceColor = (DiceColor)changedProps[DICE_COLOR_KEY];
            // Apply the new health value to Player2's data
            Debug.Log($"DiceColor: {diceColor}, PlayerName: {targetPlayer.NickName}, CanJoin: {canJoin}");
        }
        
        // Check if the custom properties updated are for Player2 (this player)
        if (targetPlayer == PhotonNetwork.LocalPlayer)
        {
            // Example: Handle the custom properties that have been updated
            DataManager.Instance.SetOwnDiceColor(diceColor);
            DataManager.Instance.SetMaxPlayerNumberForCurrentBoard((byte)PhotonNetwork.CurrentRoom.MaxPlayers);
        }

        switch (PhotonNetwork.IsMasterClient)
        {
            case true when diceColor != DiceColor.Unknown && DataManager.Instance.CurrentRoomType == RoomType.Private:
                UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenPrivateJoinedPlayerPanel);
                return;
            
            case true when diceColor != DiceColor.Unknown && DataManager.Instance.CurrentRoomType == RoomType.Random:
                UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenJoinMultiplayerPanel);
                return;
            
            case true when DataManager.Instance.CurrentRoomType == RoomType.Private && PhotonNetwork.CurrentRoom.PlayerCount == DataManager.Instance.MaxPlayerNumberForCurrentBoard:
                UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer);
                Invoke(nameof(StartGame), 4);
                break;
        }
        
        switch (canJoin)
        {
            case true when DataManager.Instance.CurrentRoomType == RoomType.Random:
                UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenJoinMultiplayerPanel);
                break;
            case true when DataManager.Instance.CurrentRoomType == RoomType.Private:
                UIManager.Instance.InstantiateJoinedPlayer(diceColor, targetPlayer, OpenPrivateJoinedPlayerPanel);
                break;
        }
    }

    private void OpenJoinMultiplayerPanel()
    {
        UIManager.Instance.OpenJoinedPlayerPanel(currentRoomId);
    }

    private void OpenPrivateJoinedPlayerPanel()
    {
        UIManager.Instance.OpenPrivateJoinedPlayerPanel(currentRoomId);
    }

    [PunRPC]
    public void StartCountDown(int time)
    {
        timer = time;
        uiManager.OpenJoinedPlayerPanelForOnlineMatch();
        UpdateCountdownDisplay(time);
        InvokeRepeating(nameof(CountdownToStart), 1f, 1f);
    }


    private void CountdownToStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            timer -= 1f;
            pv.RPC(nameof(UpdateCountdownDisplay), RpcTarget.AllBuffered, timer);
        }

        if (!(timer <= 0) && PhotonNetwork.CurrentRoom.PlayerCount != DataManager.Instance.MaxPlayerNumberForCurrentBoard) 
            return;
        
        CancelInvoke(nameof(CountdownToStart));

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            StartGame();
        }
        else
        {
            CancelAndReturnToMainMenu();
        }
    }

    [PunRPC]
    private void UpdateCountdownDisplay(float time)
    {
        uiManager.UpdateCountDownTimerText($"Game starting in {Mathf.Ceil(time)} seconds...");
    }

    private void AbandonedRoomDueToInsufficientMembers()
    {
        PhotonNetwork.Disconnect();
        Time.timeScale = 0;
        UIManager.Instance.errorPopUp.ShowMessagePanel("The room has been abandoned, try to create or join an another room.");
    }
    
    public void StartGame()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        
        if (DataManager.Instance.CurrentUserType == UserType.APP)
        {
            InitiateSessionAnsStartGame();
            return;
        }

        pv.RPC(nameof(OnGameStart), RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void OnGameStart()
    {
        StartCoroutine(Animtionplay());

    }
    
    [PunRPC]
    private void OnGameStartWithGameSession(string session)
    {
        DataManager.Instance.SetSessionId(session);
        StartCoroutine(Animtionplay());
    }
    
    IEnumerator Animtionplay()
    {

        string[] playerColors = { "Red", "Blue", "Green", "Yellow" };
        List<Transform> validStartPositions = new List<Transform>();
        foreach (string color in playerColors)
        {
            GameObject startObject = GameObject.Find($"JoinedPlayer_{color}");
            if (startObject != null)
            {
                validStartPositions.Add(startObject.transform);
                CoinMove.instance.MoveCoins(startObject.transform);
                yield return new WaitForSeconds(1f);
                startObject = null;
            }
        }

        GameObject.Find("VS Image").GetComponent<Animator>().enabled = true;

        totalvalue = PhotonNetwork.CurrentRoom.PlayerCount * DataManager.Instance.CurrentEntryFee;

        if (DataManager.Instance.CurrentRoomType == RoomType.Random)
        {       
            uiManager.publictotalmatchfee.text = $"{Helper.GetReadableNumber(totalvalue)}";
        }
        else if (DataManager.Instance.CurrentRoomType == RoomType.Private)
        {
            uiManager.privatetotalmatchfee.text = $"{Helper.GetReadableNumber(totalvalue)}"; 

        }
        yield return new WaitForSeconds(5f);

        InitiateGame();
        MainPanel.SetActive(false);
        ConnectingPanel.SetActive(false);
        GamePanel.SetActive(true);

        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard((byte)PhotonNetwork.CurrentRoom.PlayerCount);

        DataManager.Instance.SetCurrentGameState(GameState.Play);
        uiManager.StartMultiplayerGame();

    }

    private void CancelAndReturnToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
        uiManager.UpdateCountDownTimerText("Not enough players joined. Returning to the main menu...");
        uiManager.AddBackButtonAction();
        CancelInvoke(nameof(ShowOnlinePlayerCount));
    }

    #region Game Session
    private void InitiateSessionAnsStartGame()
    {
        Debug.Log("InitiateSessionAnsStartGame");
        uiManager.popUp.ShowMessagePanel("Creating game session, please wait...");
        WWWForm wwwForm = new WWWForm();

        wwwForm.AddField(GameId, DataManager.Instance.GameId);
        wwwForm.AddField(HostId, PhotonNetwork.MasterClient.CustomProperties[UserUid].ToString());

        foreach (var player in PhotonNetwork.PlayerList)
        {
            //uids.Add(player.CustomProperties[UserUid].ToString());
            wwwForm.AddField("users[]", player.CustomProperties[UserUid].ToString());
            Debug.Log($"Name: {player.NickName}, Id; {player.CustomProperties[UserUid]}");
        }

        wwwForm.AddField(RoomId, PhotonNetwork.CurrentRoom.Name);
        wwwForm.AddField(BoardAmount, DataManager.Instance.CurrentEntryFee);


        APIHandler.Instance.InitiateSession(wwwForm, DataManager.Instance.Token, response =>
        {
            var byteData = wwwForm.data;
            
            string data = Encoding.UTF8.GetString(byteData);
            
            Debug.Log(response + ", CurrentBoardPrize: " + DataManager.Instance.CurrentEntryFee + "Session Response: " + response + " RoomId: " + PhotonNetwork.CurrentRoom.Name);
            SessionInitResponse sessionInitResponse = JsonUtility.FromJson<SessionInitResponse>(response);

            if (sessionInitResponse is { status: true })
            {
                DataManager.Instance.SetSessionId(sessionInitResponse.game_session);
                
                Debug.Log($"GameSession: {sessionInitResponse.game_session}, Data: {DataManager.Instance.SessionId}");

                pv.RPC(nameof(OnGameStartWithGameSession), RpcTarget.AllBuffered, sessionInitResponse.game_session);

                return;
            }

            LogOutResponse res = JsonUtility.FromJson<LogOutResponse>(response);
            if (res == null)
            {
                uiManager.errorPopUp.ShowMessagePanel("Something went wrong, please try again later.");
                uiManager.popUp.CloseMessagePanel();
                return;
            }
            // if (res.status == false && !string.IsNullOrEmpty(res.message))
            // {
            //     Debug.Log("Log");
            //     uiManager.errorPopUp.ShowMessagePanel(res.message);
            // }
            // else
            {
                Debug.LogError($"Error: {res}");
                uiManager.errorPopUp.ShowMessagePanel("Something went wrong, please try again later.");
            }

            uiManager.popUp.CloseMessagePanel();
        });
    }
    #endregion Game Session

    #region Create Room
    private void CreateCustomRoom()
    {
        int entryFee = DataManager.Instance.CurrentEntryFee;
        userType = DataManager.Instance.CurrentUserType;

        Debug.Log($"Creating Custom room for {userType} with {entryFee} fee with MaxPlayer: {DataManager.Instance.MaxPlayerNumberForCurrentBoard}");

        RoomOptions roomOptions = new RoomOptions
        {
            CustomRoomPropertiesForLobby = new[] { UserTypeKey, EntryFeesKey },
            MaxPlayers = DataManager.Instance.MaxPlayerNumberForCurrentBoard,

            IsOpen = true,
            IsVisible = false,

            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable() { { UserTypeKey, userType }, { EntryFeesKey, entryFee } }
        };

        currentRoomId = UnityEngine.Random.Range(1000, 10000).ToString();
        Debug.Log($"RoomID: {currentRoomId}, Fee: {entryFee}");
        PhotonNetwork.CreateRoom(currentRoomId.ToString(), roomOptions);
    }
    #endregion Create Room

    #region Join Room
    private void JoinPrivateRoom(string roomId)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            uiManager.errorPopUp.ShowMessagePanel("Invalid Room id. Please enter correct room id.");
            return;
        }

        currentRoomId = roomId;
        PhotonNetwork.JoinRoom(roomId);
    }

    private void ShowUnableToJoinRoomDueToLowBalance()
    {
        uiManager.CloseAllMultiplayerPanel();
        uiManager.OpenUnableToJoinRoomPanel();
    }
    #endregion Join Room

    #region Common Methods for Private Or Random Room

    private void CloseCurrentRoom()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false; // No new players can join
        PhotonNetwork.CurrentRoom.IsVisible = false; // Room is not visible in the lobby
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


    internal bool IsRoomFull()
    {
        return PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    #endregion Common Methods for Private Or Random Room
}
