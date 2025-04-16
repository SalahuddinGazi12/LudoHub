using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
//using Photon.Pun;
using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine.U2D;
using System.Collections.Generic;
using System.Linq;
//using Photon.Realtime;

using Action = System.Action;


public class UIManager : MonoBehaviour
{
    public GameObject MainPanel;
    public GameObject GamePanel;

    public static UIManager Instance { get; private set; }

    [Space(30), Header("Dependencies"), Space(10)]
    public MenuManager menuManager;
    public PopUp popUp;
    public PopUp errorPopUp;
    private TeamColor ownTeamColor;

    [Space(30), Header("Gameplay UI"), Space(10)]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject trophyImage;
    [SerializeField] private TextMeshProUGUI resultText;
    public Transform boardGameObject;
    public SpriteRenderer boardSpriteRenderer;

    [Space(30), Header("Multiplayer UI"), Space(10)]
    [SerializeField] private GameObject chatBtn;
    [SerializeField] private GameObject matchCodePanel;
    [SerializeField] private GameObject multiplayerJoinPanel;
    [SerializeField] private GameObject privatePlayerJoinPanel;
    [SerializeField] private GameObject privateRoomJoinPanel;
    [SerializeField] private GameObject timeOutPanel;
    [SerializeField] private GameObject unableToJoinPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TextMeshProUGUI publicPlayerOnlineTxt;
    [SerializeField] private TextMeshProUGUI privatePlayerOnlineTxt;
    [SerializeField] private TextMeshProUGUI waitingTimeTxt;

    [SerializeField] private TMP_InputField roomIdInputFiled;
    [SerializeField] SpriteAtlas gameSpritesAtlas;

    [Space(10), Header("Joined Player")]
    [SerializeField] private Transform joinedPlayerListParent;
    [SerializeField] private Transform joinedMasterPlayerListParent;
    [SerializeField] private Transform privateJoinedPlayerListParent;
    [SerializeField] private Transform privateJoinedMasterPlayerListParent;
    [SerializeField] private JoinedPlayer joinedPlayer;
    [SerializeField] private PrivateJoinPlayer privateJoinedPlayer;
    [SerializeField] private List<JoinedPlayer> joinedPlayers = new List<JoinedPlayer>();
    [SerializeField] private List<PrivateJoinPlayer> privateJoinedPlayers = new List<PrivateJoinPlayer>();
    [SerializeField] private List<Sprite> joinedDiceColorSprite = new List<Sprite>();
    [SerializeField] private TextMeshProUGUI joinedPlayerEntryFeeTxt;
    [SerializeField] private TextMeshProUGUI matchStartingCountDownTxt;
    [SerializeField] private TextMeshProUGUI privateMatchRoomCodeText;
    [SerializeField] private TextMeshProUGUI privateMatchEntryText;
    [SerializeField] private GameObject clockGlassObj;
    public TextMeshProUGUI publictotalmatchfee;
    public TextMeshProUGUI privatetotalmatchfee;
   // private PhotonView photonView;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
      //  photonView = GetComponent<PhotonView>();
        RemoveAllJoinedPlayers();
    }

    private void OpenPausePanel()
    {
        pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);
        AddBackButtonAction();
    }
    public static string FormatLargeNumber(int number)
    {
        if (number >= 1000000000)
            return (number / 1000000000f).ToString("0.#") + "B";
        else if (number >= 1000000)
            return (number / 1000000f).ToString("0.#") + "M";
        else if (number >= 1000)
            return (number / 1000f).ToString("0.#") + "K";
        else
            return number.ToString("0");
    }
    public void AddBackButtonAction()
    {
        menuManager.ShowBackButton();
        menuManager.AddNewActionToBackBtn(OpenPausePanel);
    }

    public void StartMultiplayerGame()
    {
        switch (DataManager.Instance.OwnDiceColor)
        {
            default:
            case DiceColor.Red:
                boardGameObject.localEulerAngles = Vector3.zero;
                DataManager.Instance.SetLocalRotation(LocalRotation.Null); 
                break;
            
            case DiceColor.Blue:
                boardGameObject.localEulerAngles = new Vector3(0, 0, 90);
                DataManager.Instance.SetLocalRotation(LocalRotation.Z90);
                break;
            
            case DiceColor.Yellow:
                boardGameObject.localEulerAngles = new Vector3(0, 0, 180);
                DataManager.Instance.SetLocalRotation(LocalRotation.Z180);        
                break;
                
            case DiceColor.Green:
                boardGameObject.localEulerAngles = new Vector3(0f, 180f, 0f);
                DataManager.Instance.SetLocalRotation(LocalRotation.Y180);
                break;
        }

        GameManager.Instance.RotateAllPieces(boardGameObject.transform);

        switch (DataManager.Instance.MaxPlayerNumberForCurrentBoard)
        {
            default:
            case 2:
                Game1();
                break;

            case 3:
                  Game2();
                break;

            case 4:
                Game3();
                break;
        }

        GameManager.Instance.SetDefaultRolledDice();
        UIManager.Instance.CloseAllMultiplayerPanel();
        ShowChatButton();
        if (DataManager.Instance.CurrentUserType == UserType.NORMAL)
        {
            DataManager.Instance.ReduceNormalUserCoins(DataManager.Instance.CurrentEntryFee, !menuManager.isCustomMenuEnable);
        }
        else
        {
            DataManager.Instance.UpdateAppUserCoin(DataManager.Instance.CurrentEntryFee * -1);
        }

        menuManager.UpdateCoinText();
        menuManager.OpenTopBar();
    }

    public void ShowChatButton()
    {
        chatBtn.gameObject.SetActive(true);
    }

    public void HidChatButton()
    {
        chatBtn.gameObject.SetActive(false);
    }

    public void ChangeBoardBgAndAvatars()
    {
        int boardBgIndex = PlayerPrefs.GetInt("bordbg_index", 0);
        int defaultAvatarIndex = PlayerPrefs.GetInt("userAvatar", 0);
        
        Debug.Log($"Sprite: {DataManager.Instance.boardGraphics[boardBgIndex].boardSprite.name}");
        boardSpriteRenderer.sprite = DataManager.Instance.boardGraphics[boardBgIndex].boardSprite;
        GameManager.Instance.ChangeAllPieceSprite(boardBgIndex);
        GameManager.Instance.ChangeAllblinkSprite(boardBgIndex);


        List<Sprite> avatarSprites = menuManager.avatarImages.Select(image => image.sprite).ToList();
        Sprite defaultAvatarSprite = avatarSprites[defaultAvatarIndex];
        avatarSprites.RemoveAt(defaultAvatarIndex);
        
        int avatarIndex = 0;

        foreach (DiceColor diceColor in Enum.GetValues(typeof(DiceColor)))
        {
            if(diceColor == DiceColor.Unknown)
                continue;
            
            if (diceColor == DataManager.Instance.OwnDiceColor && DataManager.Instance.CurrentUserType == UserType.APP)
            {
                GameManager.Instance.SetPlayerAvatar(DataManager.Instance.GetDefaultAvatarSprite() != null ? DataManager.Instance.GetDefaultAvatarSprite() : defaultAvatarSprite, diceColor);
            }
            else if(diceColor == DataManager.Instance.OwnDiceColor && DataManager.Instance.CurrentUserType == UserType.NORMAL)
            {
                GameManager.Instance.SetPlayerAvatar(defaultAvatarSprite, diceColor);
            }
            else if(diceColor == DiceColor.Red && DataManager.Instance.CurrentUserType == UserType.NORMAL && DataManager.Instance.GameType != GameType.Multiplayer)
            {
                GameManager.Instance.SetPlayerAvatar(defaultAvatarSprite, diceColor);
            }
            else
            {
               //if(DataManager.Instance.SetCurrentUserType(UserType.NORMAL))
               // {

               // }
                //GameManager.Instance.SetPlayerAvatar(avatarSprites[avatarIndex], diceColor);
                //avatarIndex++;
            }
        }
    }
    
    public void Game1()
    {
        ChangeBoardBgAndAvatars();
        GamePanel.SetActive(true);

        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Red);
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Yellow);
        }
        
        CommonTurnChangeTask(DiceColor.Red);
        
        GameManager.Instance.SetUpBoardAndTotalPlayers(2);
        MainPanel.SetActive(false);
        Game1Setting();
        AddBackButtonAction();
    }
  
    public void Game2()
    {
        ChangeBoardBgAndAvatars();
        GamePanel.SetActive(true);
        
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Red);
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Blue);
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Yellow);
        }
        
        CommonTurnChangeTask(DiceColor.Red);
        
        GameManager.Instance.SetUpBoardAndTotalPlayers(3);
        MainPanel.SetActive(false);
        Game2Setting();
        AddBackButtonAction();
    }

    public void Game3()
    {
        ChangeBoardBgAndAvatars();
        GamePanel.SetActive(true);
        
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Red);
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Blue);
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Yellow);
            GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Green);
        }
        
        CommonTurnChangeTask(DiceColor.Red);
        
        GameManager.Instance.SetUpBoardAndTotalPlayers(4);
        MainPanel.SetActive(false);
        AddBackButtonAction();
    }
    public void PlayWithAI()
    {
        GamePanel.SetActive(true);
        
        GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Red);
        GameManager.Instance.AddPlayerToCurrentRoomPlayersList(DiceColor.Yellow);
        
        CommonTurnChangeTask(DiceColor.Red);
        DataManager.Instance.SetOwnDiceColor(DiceColor.Red);
        
        HidChatButton();
        GameManager.Instance.SetUpBoardAndTotalPlayers(1);
        MainPanel.SetActive(false);

        if(DataManager.Instance.CurrentRoomType == RoomType.AI)
        {
            DataManager.Instance.ReduceNormalUserCoins(DataManager.Instance.CurrentEntryFee);
            menuManager.UpdateCoinText();
        }

        Game1Setting();
    }

    void Game1Setting()
    {
        ChangeBoardBgAndAvatars();

        if(DataManager.Instance.CurrentRoomType == RoomType.Random)
        {
            bool exists = joinedPlayers.Any(player => player.SelfDiceColor == DiceColor.Blue);

            if(!exists)
            {
                HidePlayer(GameManager.Instance.bluePlayerPiece);
            }

            exists = joinedPlayers.Any(player => player.SelfDiceColor == DiceColor.Yellow);

            if (!exists)
            {
                HidePlayer(GameManager.Instance.yellowPlayerPiece);
            }

            exists = joinedPlayers.Any(player => player.SelfDiceColor == DiceColor.Green);

            if (!exists)
            {
                HidePlayer(GameManager.Instance.greenPlayerPiece);
            }

            ShowChatButton();
            return;
        }
        else if(DataManager.Instance.CurrentRoomType == RoomType.Private)
        {
            bool exists = privateJoinedPlayers.Any(player => player.selfDiceColor == DiceColor.Blue);

            if(!exists)
            {
                HidePlayer(GameManager.Instance.bluePlayerPiece);
            }

            exists = privateJoinedPlayers.Any(player => player.selfDiceColor == DiceColor.Yellow);

            if (!exists)
            {
                HidePlayer(GameManager.Instance.yellowPlayerPiece);
            }

            exists = privateJoinedPlayers.Any(player => player.selfDiceColor == DiceColor.Green);

            if (!exists)
            {
                HidePlayer(GameManager.Instance.greenPlayerPiece);
            }

            ShowChatButton();
            return;
        }

        HidChatButton();
        HidePlayer(GameManager.Instance.bluePlayerPiece);
        HidePlayer(GameManager.Instance.greenPlayerPiece);
    }

    public void HideLeftPlayer(DiceColor diceColor)
    {
        // GameManager.Instance.RunMethodInRPC(, RpcTarget.AllBuffered, (int)diceColor);
    }

   // [PunRPC]
    private void HideLeftPlayerRPC(int diceColor)
    {
        DiceColor color = (DiceColor)diceColor;
        GameManager.Instance.RemovePlayerToCurrentRoomPlayersList(color);
    }
    void Game2Setting()
    {
        HidePlayer(GameManager.Instance.greenPlayerPiece);
    }

    void HidePlayer(PlayerPiece[] playerPieces_)
    {
        for (int i = 0; i < playerPieces_.Length; i++)
        {
            playerPieces_[i].gameObject.SetActive(false);
        }
    }

    public void ShowWaitingTime(int time)
    {
        waitingTimeTxt.text = Mathf.Max(0, time).ToString();
    }


    //New Code
    public void BackToMainMenu()
    {
        RemoveAllJoinedPlayers();
        DataManager.Instance.ResetCurrentMatchData();

        //if (PhotonNetwork.IsConnected && DataManager.Instance.GameType == GameType.Multiplayer)
        //{
        //    try
        //    {
        //        PhotonNetwork.LeaveRoom();
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogException(e);
        //    }
        //}

        DataManager.Instance.SetCurrentGameState(GameState.Init);
        SceneManager.LoadScene(0);
    }

    public void ChangeTurn(DiceColor activeDiceColor)
    {
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            //GameManager.Instance.RaiseTurnChangeEvent(activeDiceColor);
          //  photonView.RPC(nameof(ChangeTurnForMultiplayerRPC), RpcTarget.Others, (int) activeDiceColor);
        }
        // else
        // {
        //     CommonTurnChangeTask(activeDiceColor);
        // }
        
        CommonTurnChangeTask(activeDiceColor);
    }

  //  [PunRPC]
    public void ChangeTurnForMultiplayerRPC(int diceColor)
    {
        //ChangeTurn(diceColor);
        CommonTurnChangeTask((DiceColor) diceColor);
    }
    
    public void ChangeTurnForMultiplayer(DiceColor diceColor)
    {
        //ChangeTurn(diceColor);
        CommonTurnChangeTask(diceColor);
    }

    private void CommonTurnChangeTask(DiceColor diceColor)
    {
        //Debug.Log($"Active Dice: {diceColor}");
        DataManager.Instance.SetActiveDiceColor(diceColor);
        GameManager.Instance.StartDiceBlinking(diceColor);
        
        GameManager.Instance.ChangeActiveDiceColor(diceColor);
    }

    public void SetOpponentPlayerDataAndInitializeUI()
    {
        if (DataManager.Instance.CurrentRoomType == RoomType.AI || DataManager.Instance.CurrentRoomType == RoomType.Free)
        {
            //opponentNameTxt.text = DataManager.Instance.OpponentTeamColor.ToString();
            //         opponentAvatar.sprite = defaultOpponentAvatarSprite;
            CommonGameplayUIInitialization();
            return;
        }

        //opponentNameTxt.text = GetPascalCaseString(DataManager.Instance.OpponentPlayer.NickName);
        CommonGameplayUIInitialization();
    }

    private void CommonGameplayUIInitialization()
    {
        //GameOverScreen.SetActive(false);
        //TeamSelectionScreen.SetActive(false);
        //ConnectScreen.SetActive(false);
        //GameModeSelectionScreen.SetActive(true);

        //ownTeamColor = DataManager.Instance.OwnTeamColor;

        chatBtn.SetActive(DataManager.Instance.CurrentRoomType == RoomType.Private || DataManager.Instance.CurrentRoomType == RoomType.Random);

        ChangeTurn(DiceColor.Red);
    }

    public void OnSinglePlayerModeSelected()
    {
        //GameOverScreen.SetActive(false);
        //TeamSelectionScreen.SetActive(false);
        //ConnectScreen.SetActive(false);
        //GameModeSelectionScreen.SetActive(false);

        //DataManager.Instance.SetOwnAndOpponentTeamColor(ownTeamColor);
        SetOpponentPlayerDataAndInitializeUI();
    }

    public void OpenMatchCodePanel(string roomId)
    {
        CloseAllMultiplayerPanel();
        //matchCodeTxt.text = roomId;
        matchCodePanel.SetActive(true);
    }

    private void UpdateSession(CoinType coinType, string userId)
    {
        popUp.ShowMessagePanel("Calculating result, please wait...");

        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField("coin_type", Enum.GetName(typeof(CoinType), coinType));
        wwwForm.AddField("game_session", DataManager.Instance.SessionId);
        wwwForm.AddField("coin", DataManager.Instance.CurrentEntryFee * 2);
        wwwForm.AddField("remark", Enum.GetName(typeof(CoinType), coinType));
        wwwForm.AddField("user_id", userId);

        APIHandler.Instance.UpdateSession(wwwForm, DataManager.Instance.Token, response =>
        {
            popUp.CloseMessagePanel();

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    SessionResponse sessionResponse = JsonUtility.FromJson<SessionResponse>(response);
                    
                    Debug.Log($"GameOverResponse: {sessionResponse}");
                    
                    if (sessionResponse is { status: true })
                    {
                        int previousCoin = DataManager.Instance.Coins;
                        int earnedCoins = Convert.ToInt32(sessionResponse.grand_total);
                        DataManager.Instance.UpdateAppUserCoin(earnedCoins);

                        resultText.text = $"You Win!\nEarned Coins: {FormatLargeNumber(earnedCoins)}" +
                                          $"\nPrevious Coins: {FormatLargeNumber(previousCoin)}" +
                                          $"\nTotal Coins: {FormatLargeNumber(DataManager.Instance.Coins)}";
                        gameOverPanel.SetActive(true);
                    }
                    else
                    {
                        Debug.Log($"Failed to update session response: {response}");
                        errorPopUp.ShowMessagePanel("Could not complete session update, please try again.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error parsing response: {ex.Message}");
                    errorPopUp.ShowMessagePanel("Something went wrong. Please try again later.");
                }
            }
            else
            {
                errorPopUp.ShowMessagePanel("Failed to update session. Please try again later.");
                Debug.LogError($"Error parsing session update response: {response}");
            }
        });
    }

    private void CalculateResultForSinglePlayer(DiceColor winner)
    {
        int previousCoins = DataManager.Instance.Coins;
        int earnOrDeductedCoins = DataManager.Instance.CurrentEntryFee;

        if (DataManager.Instance.OwnDiceColor == winner)
        {
            earnOrDeductedCoins *= 2;
            DataManager.Instance.UpdateNormalUserCoins(earnOrDeductedCoins, !menuManager.isCustomMenuEnable);
            
            resultText.text = $"You Win!" +
                              $"\nEarned Coins: {FormatLargeNumber(earnOrDeductedCoins)}" +
                              $"\nPrevious Coins: {FormatLargeNumber(previousCoins)}" +
                              $"\nTotal Coins: {FormatLargeNumber(DataManager.Instance.Coins)}";
            
            trophyImage.SetActive(true);
        }
        else
        {
            resultText.text = $"You Lose!\nCoin Deducted: {FormatLargeNumber(earnOrDeductedCoins)}" +
                              $"\nPrevious Coins: {FormatLargeNumber(previousCoins + earnOrDeductedCoins)}" +
                              $"\nTotal Coins: {FormatLargeNumber(DataManager.Instance.Coins)}";
            trophyImage.SetActive(false);
        }

        gameOverPanel.SetActive(true);
    }

    private void CalculateResultForNormalMultiplayerMode(DiceColor winner)
    {
        int previousCoins = DataManager.Instance.Coins;
        int earnOrDeductedCoins = DataManager.Instance.CurrentEntryFee;

        if (DataManager.Instance.OwnDiceColor == winner)
        {
            earnOrDeductedCoins *= 2;
            DataManager.Instance.UpdateNormalUserCoins(earnOrDeductedCoins, !menuManager.isCustomMenuEnable);
            resultText.text = $"You Win!" +
                              $"\nEarned Coins: {FormatLargeNumber(earnOrDeductedCoins)}" +
                              $"\nPrevious Coins: {FormatLargeNumber(previousCoins)}" +
                              $"\nTotal Coins: {FormatLargeNumber(DataManager.Instance.Coins)}";
            trophyImage.SetActive(true);
        }
        else
        {
            resultText.text = $"You Lose!" +
                              $"\nCoin Deducted: {FormatLargeNumber(earnOrDeductedCoins)}" +
                              $"\nPrevious Coins: {FormatLargeNumber(previousCoins + earnOrDeductedCoins)}" +
                              $"\nTotal Coins: {FormatLargeNumber(DataManager.Instance.Coins)}";
            
            trophyImage.SetActive(false);
        }

        gameOverPanel.SetActive(true);
    }

    private void CalculateResultForAppUser(DiceColor winner)
    {
        if (DataManager.Instance.CurrentUserType != UserType.APP)
            return;

        Debug.Log($"Winner: {winner}, OwnColor: {DataManager.Instance.OwnDiceColor}");
        int previousCoins = DataManager.Instance.Coins;
        int earnOrDeductedCoins = DataManager.Instance.CurrentEntryFee;

        if (DataManager.Instance.OwnDiceColor == winner)
        {
            // Win scenario
            //if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("userUid"))
            //{
            //    UpdateSession(CoinType.WIN, PhotonNetwork.LocalPlayer.CustomProperties["userUid"].ToString());
            //}
           
                Debug.LogWarning("User ID not found in CustomProperties");
            
            
            // resultText.text = $"You Win!\nEarned Coins: {FormatLargeNumber(earnOrDeductedCoins)}\nPrevious Coins: {FormatLargeNumber(previousCoins)}\nTotal Coins: {FormatLargeNumber(DataManager.Instance.Coins)}";
            trophyImage.SetActive(true);
            gameOverPanel.SetActive(true);
        }
        else
        {
            // Loss scenario
            resultText.text = $"You Lose!\nCoin Deducted: {FormatLargeNumber(earnOrDeductedCoins)}" +
                              $"\nPrevious Coins: {FormatLargeNumber(previousCoins + earnOrDeductedCoins)}" +
                              $"\nTotal Coins: {FormatLargeNumber(DataManager.Instance.Coins)}";
            
            trophyImage.SetActive(false);
            gameOverPanel.SetActive(true);
        }
    }



    public void OnGameFinished(DiceColor winner)
    {
        StopCurrentTimerCoroutine();

        if (DataManager.Instance.CurrentRoomType == RoomType.AI || DataManager.Instance.CurrentRoomType == RoomType.Free)
        {
            CalculateResultForSinglePlayer(winner);
        }
        switch (DataManager.Instance.CurrentUserType)
        {
            case UserType.NORMAL:
                CalculateResultForNormalMultiplayerMode(winner);
                break;
            case UserType.APP:
                CalculateResultForAppUser(winner);
                break;
        }

        if (DataManager.Instance.GameType != GameType.Multiplayer) 
            return;
        
        //if (PhotonNetwork.InRoom)
        //{
        //    PhotonNetwork.LeaveRoom();
        //}

        //PhotonNetwork.Disconnect();

    }


    public void SetConnectionStatusText(string status)
    {
        //connectionStatus.text = status;
    }

    public void OpenPrivateRoomJoinPanel()
    {
        privateRoomJoinPanel.SetActive(true);
    }

    public void ClosePrivateRoomJoinPanel()
    {
        privateRoomJoinPanel.SetActive(false);
    }

    #region Multiplayer Controll Methods

    public void CloseAllMultiplayerPanel()
    {
        multiplayerJoinPanel.SetActive(false);
        matchCodePanel.SetActive(false);
        privatePlayerJoinPanel.SetActive(false);
        privateRoomJoinPanel.SetActive(false);
        timeOutPanel.SetActive(false);
        unableToJoinPanel.SetActive(false);
    }

    public void OpenUnableToJoinRoomPanel()
    {
        CloseAllMultiplayerPanel();
        unableToJoinPanel.SetActive(true);
    }

    public string GetInputtedRoomId()
    {
        return roomIdInputFiled.text;
    }

    public void ShowOnlinePlayerCount(int playerCount)
    {
        publicPlayerOnlineTxt.text = $"Player Online: {playerCount}";
        privatePlayerOnlineTxt.text = $"Player Online: {playerCount}";
    }

    public void ShowWaitingTimeOutPanel()
    {
        timeOutPanel.SetActive(true);
    }

    public void CloseWaitingTimeOutPanel()
    {
        timeOutPanel.SetActive(false);
    }

    public void SetMatchCodeText(string matchCodeText)
    {
        //matchCodeTxt.text = matchCodeText;
    }

    public void OpenJoinedPlayerPanel(string matchCode = null)
    {
        if (!string.IsNullOrEmpty(matchCode))
        {
            //matchCodeTxt.text = $"Your room code is {matchCode}. Please share the code with others to join the room.";
            //matchCodeTxt.gameObject.SetActive(true);
            matchStartingCountDownTxt.gameObject.SetActive(false);
            //joinPlayerPlayBtn.gameObject.SetActive(true);
        }

        CommonJoinedPlayerPanelTasks();
    }
    
    public void OpenPrivateJoinedPlayerPanel(string matchCode = null)
    {
        if (!string.IsNullOrEmpty(matchCode))
        {
            privateMatchRoomCodeText.text = $"Match Code: {matchCode}";
            privateMatchEntryText.text = $"Match Fee: {Helper.GetReadableNumber(DataManager.Instance.CurrentEntryFee)}";
        }

        CloseAllMultiplayerPanel();
        privatePlayerJoinPanel.SetActive(true);
    }

    public void OpenJoinedPlayerPanelForOnlineMatch()
    {
        //joinPlayerPlayBtn.gameObject.SetActive(false);
        //matchCodeTxt.gameObject.SetActive(false);
        matchStartingCountDownTxt.gameObject.SetActive(true);

        CommonJoinedPlayerPanelTasks();
    }

    private void CommonJoinedPlayerPanelTasks()
    {
        joinedPlayerEntryFeeTxt.text = $"Match Fee: {Helper.GetReadableNumber(DataManager.Instance.CurrentEntryFee)}";

        CloseAllMultiplayerPanel();
        multiplayerJoinPanel.SetActive(true);
    }

    public void UpdateCountDownTimerText(string countDownTimerText)
    {
        matchStartingCountDownTxt.gameObject.SetActive(true);
        matchStartingCountDownTxt.text = countDownTimerText;
    }

    public void SpawnJoinPlayerPrefab(string playerName, DiceColor diceColor, JoinedPlayerType joinedUser)
    {
        JoinedPlayer spawnedJoinedPlayer = Instantiate(joinedPlayer, joinedPlayerListParent);
        //spawnedJoinedPlayer.SetJoinedPlayerInfo(playerName, diceColor, joinedUser);
    }

    public Sprite GetGameSpriteByName(DiceColor diceColor)
    {
        Debug.Log($"Enu: {diceColor}");
        return joinedDiceColorSprite[(int) diceColor];
        //return gameSpritesAtlas.GetSprite(spriteName);
    }

    [ContextMenu(nameof(PrivateJoinPlayer))]
    public void PrintJoinPlayer()
    {
        //var playerList = PhotonNetwork.PlayerList;
        
       // foreach (var player in playerList)
        {
           // Debug.Log($"playerName: {player.NickName}");

            //if (!player.CustomProperties.ContainsKey(NetworkManager.DICE_COLOR_KEY)) 
            //    continue;
            
          //  DiceColor diceColor = (DiceColor)player.CustomProperties[NetworkManager.DICE_COLOR_KEY];
            //bool exists = joinedPlayers.Any(onlinePlayer => onlinePlayer.SelfDiceColor == diceColor);
            
           // Debug.Log($"Exist: {exists}, DiceColor: {diceColor}, PlayerName: {player.NickName}");
        }
    }

    public void InstantiateJoinedPlayer(Action callback = null)
    {
        //var playerList = PhotonNetwork.PlayerList;

        //if (DataManager.Instance.CurrentRoomType == RoomType.Random)
        //{
        //    foreach (var player in playerList)
        //    {
        //        Debug.Log($"playerName: {player.NickName}");

        //        if (player.CustomProperties.ContainsKey(NetworkManager.DICE_COLOR_KEY))
        //        {
        //            iceColor diceColor = (DiceColor)player.CustomProperties[NetworkManager.DICE_COLOR_KEY];
        //            bool exists = joinedPlayers.Any(onlinePlayer => onlinePlayer.SelfDiceColor == diceColor);

        //            if (diceColor != DiceColor.Unknown && !exists)
        //            {
        //                InstantiateJoinedPlayer(diceColor, player);
        //            }

        //            Debug.Log($"Exist: {exists}, DiceColor: {diceColor}, PlayerName: {player.NickName}");
        //        }
        //    }
        //}
        //else
        //{
        //    foreach (var player in playerList)
        //    {
        //        Debug.Log($"playerName: {player.NickName}");

        //        if (!player.CustomProperties.ContainsKey(NetworkManager.DICE_COLOR_KEY))
        //            continue;

        //        DiceColor diceColor = (DiceColor)player.CustomProperties[NetworkManager.DICE_COLOR_KEY];
        //        bool exists = joinedPlayers.Any(privatePlayer => privatePlayer.SelfDiceColor == diceColor);

        //        if (diceColor != DiceColor.Unknown && !exists)
        //        {
        //            InstantiateJoinedPlayer(diceColor, player);
        //        }

        //        Debug.Log($"Exist: {exists}, DiceColor: {diceColor}");
        //    }
        //}

        //callback?.Invoke();
    }

    public void InstantiateJoinedPlayer(DiceColor diceColor, /*//Player targetedPlayer*/Action callback = null)
    {
        Debug.Log($"InstantiateJoinedPlayer, {diceColor}");
        if (diceColor == DiceColor.Unknown)
        {
            Debug.Log("Returning becasue dice color is unknown");
            return;
        }
        
        switch (DataManager.Instance.CurrentRoomType)
        {
            case RoomType.Random:
            {
                if(joinedPlayers.Count == 0)
                {
                    //if (PhotonNetwork.MasterClient.CustomProperties.ContainsKey(NetworkManager.DICE_COLOR_KEY))
                    //{
                    //    DiceColor masterClientDiceColor = (DiceColor) PhotonNetwork.MasterClient.CustomProperties[NetworkManager.DICE_COLOR_KEY];
                    //    if(masterClientDiceColor != DiceColor.Unknown)
                    //    {
                    //        JoinedPlayer instantiatedJoinedPlayer = Instantiate(joinedPlayer, joinedMasterPlayerListParent);
                    //        instantiatedJoinedPlayer.SetJoinedPlayerInfo(PhotonNetwork.MasterClient.NickName, masterClientDiceColor);
                    //        joinedPlayers.Add(instantiatedJoinedPlayer);
                    //    }
                    //}
                
                   // var playerList = PhotonNetwork.PlayerList;
        
                    //foreach (var player in playerList)
                    //{
                    //    Debug.Log($"playerName: {player.NickName}");

                    //    if (!player.CustomProperties.ContainsKey(NetworkManager.DICE_COLOR_KEY)) 
                    //        continue;
            
                    //    DiceColor color = (DiceColor)player.CustomProperties[NetworkManager.DICE_COLOR_KEY];
                    //    bool existPlayer = joinedPlayers.Any(onlinePlayer => onlinePlayer.SelfDiceColor == color);
            
                    //    if (!existPlayer)
                    //    {
                    //        JoinedPlayer instantiatedJoinedPlayer = Instantiate(joinedPlayer, joinedPlayerListParent);
                    //        instantiatedJoinedPlayer.SetJoinedPlayerInfo(player.NickName, color);
                    //        joinedPlayers.Add(instantiatedJoinedPlayer);
                    //    }
                    
                    //    Debug.Log($"Exist: {existPlayer}, DiceColor: {color}, PlayerName: {player.NickName}");
                    //}
                }
            
                bool exists = joinedPlayers.Any(player => player.SelfDiceColor == diceColor);

                if (!exists)
                {
                    JoinedPlayer instantiatedJoinedPlayer = Instantiate(joinedPlayer, joinedPlayerListParent);
                   // instantiatedJoinedPlayer.SetJoinedPlayerInfo(targetedPlayer.NickName, diceColor);
                    joinedPlayers.Add(instantiatedJoinedPlayer);
                }

                break;
            }
            case RoomType.Private:
            {
                // Check if private room players are not yet populated
                if (privateJoinedPlayers.Count == 0)
                {
                    AddMasterClientToPrivateRoom();

                  //  var playerList = PhotonNetwork.PlayerList;

                    //foreach (var player in playerList)
                    //{
                    //    if (!player.CustomProperties.ContainsKey(NetworkManager.DICE_COLOR_KEY))
                    //        continue;

                    //    DiceColor color = (DiceColor)player.CustomProperties[NetworkManager.DICE_COLOR_KEY];

                    //    // Check if this player has already been added based on DiceColor
                    //    bool existPlayer = privateJoinedPlayers.Any(onlinePlayer => onlinePlayer.selfDiceColor == color);

                    //    if (!existPlayer)
                    //    {
                    //        InstantiateAndAddPlayer(player, color);
                    //    }

                    //    Debug.Log($"Exist: {existPlayer}, DiceColor: {color}, PlayerName: {player.NickName}");
                    //}
                }

                // Ensure targeted player is added if not already
                if (!privateJoinedPlayers.Any(player => player.selfDiceColor == diceColor))
                {
                   // InstantiateAndAddPlayer(targetedPlayer, diceColor);
                }

                break;
            }
        }

        callback?.Invoke();

        // Helper Method to Add Master Client
        void AddMasterClientToPrivateRoom()
        {
            //if (PhotonNetwork.MasterClient.CustomProperties.ContainsKey(NetworkManager.DICE_COLOR_KEY))
            //{
            //    DiceColor masterClientDiceColor = (DiceColor)PhotonNetwork.MasterClient.CustomProperties[NetworkManager.DICE_COLOR_KEY];

            //    if (masterClientDiceColor != DiceColor.Unknown)
            //    {
            //        PrivateJoinPlayer instantiatedJoinedPlayer = Instantiate(privateJoinedPlayer, privateJoinedMasterPlayerListParent);
            //        instantiatedJoinedPlayer.SetPrivatePlayerInfo(PhotonNetwork.MasterClient.NickName, masterClientDiceColor);
            //        privateJoinedPlayers.Add(instantiatedJoinedPlayer);
            //    }
            //}
        }

        // Helper Method to Instantiate and Add Player
        //void InstantiateAndAddPlayer(Player player, DiceColor color)
        //{
        //    PrivateJoinPlayer instantiatedJoinedPlayer = Instantiate(privateJoinedPlayer, privateJoinedPlayerListParent);
        //    instantiatedJoinedPlayer.SetPrivatePlayerInfo(player.NickName, color);
        //    privateJoinedPlayers.Add(instantiatedJoinedPlayer);
        //}
    }

    public void RemoveJoinedPlayer(DiceColor diceColor)
    {
        int index = joinedPlayers.FindIndex(player => player.SelfDiceColor == diceColor);

        if (index >= 0)
        {
            var player = joinedPlayers[index];
            joinedPlayers.RemoveAt(index);
            Destroy(player.gameObject);
        }
    }

    public void RemoveAllJoinedPlayers()
    {
        foreach(var player in joinedPlayers)
        {
            Debug.Log($"Destroying: {player.name}");
            Destroy(player.gameObject);
        }

        joinedPlayers.Clear();
    }

    #endregion

    #region Turn Management
    public void SetPlayerInTurn(DiceColor activeDiceColor)
    {
        //Debug.Log($"Now Turn for {playerInTurn.Name}, Opponent: {GameManager.Instance.opponentPhotonPlayer.NickName}");
        //playerInTurnText.text = $"Turn for {playerInTurn.Name}";
        //playerInTurnIconImage.sprite = playerInTurn.Icon;

        //if (activeDiceColor == ownTeamColor)
        //{
        //    StartTurnTimerForOwnPlayer();
        //}
        //else
        //{
        //    StartTurnTimerForOpponentPlayer();
        //}

        ChangeTurn(activeDiceColor);
    }
    #endregion Turn Management

    #region VS Section

    public void OpenVSPlane()
    {
        Debug.Log("OpenVSPlane");
        //vsPanel.SetActive(true);

        //vsOwnPlayerNameTxt.text = GetPascalCaseString(DataManager.Instance.CurrentUser.name);
        //vsCoinTxt.text = DataManager.Instance.Coins.ToString();

        //Sprite ownAvatar = DataManager.Instance.GetDefaultAvatarSprite();
        //if (ownAvatar != null) 
        //    vsOwnAvatarSprite.sprite = ownAvatar;

        //vsOpponentPlayerNameTxt.text = (DataManager.Instance.CurrentRoomType == RoomType.AI)? "AI": GetPascalCaseString(DataManager.Instance.OpponentPlayer.NickName);

        //vsOpponentFeeTxt.text = DataManager.Instance.CurrentEntryFee.ToString();
        //vsOwnFeeTxt.text = vsOpponentFeeTxt.text;

        //vsTotalMatchFeeTxt.text = $"{DataManager.Instance.CurrentEntryFee * 2}";
        //MoveLocalYTween(0, moveYTweenEase);
    }

    //[ContextMenu("Test")]
    //private void Test()
    //{
    //    OpenVSPlane();
    //    CloseVSPanelWithDelayAndCallback(null);
    //}

    public async void CloseVSPanelWithDelayAndCallback(Action callBack)
    {
        await Task.Delay(1000);
        //await Task.Delay((int)vsPanelActiveTime * 1000);

        //MoveLocalYTween(-2000, moveYTweenEase, ()=>
        //{
        //    vsPanel.SetActive(false);
        //    callBack();
        //});
    }

    private void MoveLocalYTween(float endValue, Ease ease, Action callBack = null)
    {
        //Debug.Log($"Before, MoveLocalYTween, Pos: {vsPanelContent.localPosition}");

        //vsPanelContent.DOLocalMoveY(endValue, moveYTweenTime)
        //    .SetEase(ease)
        //    .OnComplete(() =>
        //    {
        //        Debug.Log($"After, MoveLocalYTween, Pos: {vsPanelContent.localPosition}");
        //        callBack?.Invoke();
        //    });
    }
    #endregion VS Section

    #region Timer

    [SerializeField] private const float TotalTurnTimeInSeconds = 60;
    private bool isTimerRunning;
    private float timeRemaining;
    private float minutes;
    private float seconds;
    private readonly WaitForSeconds waitForSecondsRealtime = new WaitForSeconds(1f);
    private Coroutine timerCoroutine;

    private void StartTurnTimerForOwnPlayer()
    {
        Debug.Log("StartTurnTimerForOwnPlayer");
        StopCurrentTimerCoroutine();
        timeRemaining = TotalTurnTimeInSeconds;
        //DisplayTime(timeRemaining, ownTurnTimerTxt);

        //ownTurnTimerSection.SetActive(true);
        //opponentTurnTimerSection.SetActive(false);

        //timerCoroutine ??= StartCoroutine(TimeCounter(ownTurnTimerTxt));
    }

    private void StartTurnTimerForOpponentPlayer()
    {
        Debug.Log("StartTurnTimerForOpponentPlayer");
        StopCurrentTimerCoroutine();

        timeRemaining = TotalTurnTimeInSeconds;
        //DisplayTime(timeRemaining, opponentTurnTimerTxt);

        //ownTurnTimerSection.SetActive(false);
        //opponentTurnTimerSection.SetActive(true);

        //timerCoroutine ??= StartCoroutine(TimeCounter(opponentTurnTimerTxt));
    }

    public void StopCurrentTimerCoroutine()
    {
        if (timerCoroutine == null)
            return;

        StopCoroutine(timerCoroutine);
        timerCoroutine = null;
        isTimerRunning = false;

        //ownTurnTimerSection.SetActive(false);
        //opponentTurnTimerSection.SetActive(false);
        //isTimerRunning = false;
        // if (timerCoroutine != null)
        // {
        //     StopCoroutine(timerCoroutine);
        //     
        // }
    }


    private IEnumerator TimeCounter(TMP_Text timeText)
    {
        isTimerRunning = true;

        while (isTimerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= 1f;
                DisplayTime(timeRemaining, timeText);
            }
            else
            {
                //timeText.enabled = true;
                timeRemaining = 0;
                isTimerRunning = false;

                //GameManager.Instance.ChangePlayerTurn();
                //gameController.EndTurn();
            }

            yield return waitForSecondsRealtime;
        }

        Debug.Log(timeText.gameObject.transform.parent.gameObject.name);
        //timeText.gameObject.transform.parent.gameObject.SetActive(false);
        timerCoroutine = null;
    }

    private void DisplayTime(float timeToDisplay, TMP_Text timeText)
    {
        minutes = (int)timeToDisplay / 60;
        seconds = (int)timeToDisplay % 60;
        timeText.text = $"{minutes:00}:{seconds:00}";
    }

    #endregion / Timer
}
