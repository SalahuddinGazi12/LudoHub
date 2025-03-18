using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Network;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.Events;
using SecureDataSaver;
using UnityEngine.Serialization;

public class MenuManager : MonoBehaviour
{
    [Header("Common Section"), Space(20)]
    [SerializeField] private UserType userType;

    [SerializeField] private PopUp errorPanelForMenu;
    [SerializeField] private PopUp coinLoading;
    public static MenuManager Instance { get; private set; }
    private int playerCount = 2;
    private const int GameID = 1;
    
    [Space(10)]
    [SerializeField] private GameObject menuUIPanel;
    [SerializeField] private GameObject userProfilePanel;
    [SerializeField] private GameObject userIdCopySuccessPanel;
    [SerializeField] private GameObject teamSelectionPanel;
    [SerializeField] private GameObject footerPanel;
    [SerializeField] private GameObject commonGameMemberPanel;
    [SerializeField] private GameObject gameModePanel;
    [SerializeField] private GameObject privateModePanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject freeOrTrainingPanel;
    [SerializeField] private GameObject localMultiPlayerPanel;
    [SerializeField] private PopUp popUp;
    [SerializeField] private PopUp loadingPopUp;
    [SerializeField] private Button registrationBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button userIdCopyBtn;
    [SerializeField] private Button vsAiBtn;
    [SerializeField] private Sprite defaultUserAvatarSprite;
    [SerializeField] private List<Toggle> privatePlayerSelectionToggles;
    [SerializeField] private List<Toggle> localPlayerSelectionToggles;
    [FormerlySerializedAs("personalPrizeTxt")] public TextMeshProUGUI privatePrizeTxt;
    [FormerlySerializedAs("personalEntryFeeTxt")] [SerializeField] private TextMeshProUGUI privateEntryFeeTxt;
    [SerializeField] private Sprite[] boardCngBtnBgs;
    [SerializeField] private GameObject boardChangePanel;
    [SerializeField] private Image boardBg;
    [SerializeField] private PopUp noInternetPopUp;
    [SerializeField] private Button onlineBtn;
    [SerializeField] private Button privateBtn;
    
    [Space(20), Header("Own Player Info"), Space(10)]
    [SerializeField] private Image userAvatar;
    [SerializeField] private TextMeshProUGUI userNameForProfileTxt;
    [SerializeField] private TextMeshProUGUI userIdTxt;
    [SerializeField] private TextMeshProUGUI userIdCopySuccessTxt;
    [SerializeField] private TextMeshProUGUI coinTxt;


    [Space(10)] [Header("Prize Selection")]
    [SerializeField] private GameObject publicPrizeSelectionPanel;
    [SerializeField] private GameObject privatePrizeSelectionPanel;
    [SerializeField] private Transform publicPrizeParent;
    [SerializeField] private PublicPrizeItem publicPrizeItem;
    [SerializeField] private Button privatePlayBtn;
    [SerializeField] private TMP_Dropdown entryFeeSelectionDropDown;
    [SerializeField] private TextMeshProUGUI prizeSelectionTitleTxt;
    [SerializeField] private TextMeshProUGUI prizeSelectionCoinTxt;

    [Space(10)] [Header("Member Type Selection Section")] [Space(10)]
    [SerializeField] private GameObject memberTypeSelectionPanel;
    [SerializeField] private GameObject logRegSelectionPanel;

    [Space(30)][Header("Login Section")] [Space(10)]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField logInEmailField;
    [SerializeField] private TMP_InputField logInPasswordField;
    
    [Space(30)][Header("Registration Section")] [Space(10)]
    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private TMP_InputField regNameField;
    [SerializeField] private TMP_InputField regEmailField;
    [SerializeField] private TMP_InputField regPasswordField;
    [SerializeField] private TMP_InputField regRetypePasswordField;

    [Space(30), Header("User Detail"), Space(10)]
    [SerializeField] private GameObject userDetailPanel;
    [SerializeField] private TextMeshProUGUI userDetailUserNameTxt;
    [SerializeField] private TextMeshProUGUI userDetailUserIdTxt;
    [SerializeField] private TextMeshProUGUI teamWinTxt;
    [SerializeField] private TextMeshProUGUI totalGamesWonTxt;
    [SerializeField] private TextMeshProUGUI userDetailCoinTxt;
    [SerializeField] private TextMeshProUGUI winRateTxt;
    [SerializeField] private TextMeshProUGUI currentWinStreakTxt;
    [SerializeField] private TextMeshProUGUI totalWinSumTxt;
    [SerializeField] private TextMeshProUGUI uCoinsTxt;
    [SerializeField] private Button userDetailButton;
    [SerializeField] private Image userDetailAvatarImage;
    public List<Image> avatarImages;
    [SerializeField] private GameObject avatarSelectionPanel;


    [Space(30), Header("Game History"), Space(10)]
    [SerializeField] private GameObject userStatsPanel;
    [SerializeField] private HistoryItem historyItem;
    [SerializeField] private GameObject historyItemParent;
    [SerializeField] private GameObject noHistoryContentPanel;
    private bool isDownloadingHistoryData;
    
    [Space(30), Header("Custom Menu"), Space(10)]
    [SerializeField] private GameObject customMenuPanel;
    [SerializeField] private TMP_Dropdown customMenuDropDown;
    public bool isCustomMenuEnable;

    private const string nameParam = "name";
    private const string emailParam = "email";
    private const string passwordParam = "password";
    private const string passwordConfirmParam = "password_confirmation";
    private const string userTypeParam = "user_type";
    private const string gameNameParam = "game_name";
    private const string userIdParam = "user_id";
    private const string referralIdParam = "referral_id";
    private const string gameIdParam = "game_id";
    
    private ReadOnlyCollection<string> gamePrizes;
    private UnityAction backAction;
    
    private Stack<Action> backBtnActionQueue = new Stack<Action>();
    private PhotonView photonView;
    #region Monobehaviour Callbacks and Common Section

    private void Start()
    {
        if (isCustomMenuEnable)
        {
            customMenuPanel.SetActive(true);
            return;
        }
        
        InitializeMenu();
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        photonView = GetComponent<PhotonView>();
    }

    public void GoWithCustomData()
    {
        string emailValue = customMenuDropDown.options[customMenuDropDown.value].text;
        var emailData = emailValue.Split('@');
        
        ApiResponse response = new ApiResponse
        {
            user = new User
            {
                email = emailValue,
                name = emailData[0]
            }
        };
        
        InitializeMenu(response);
        customMenuPanel.SetActive(false);
    }
    
    private void InitializeMenu(ApiResponse customResponse = null)
    {
        Time.timeScale = 1;
        
        CloseTopBar();
        DataManager.Instance.SetCurrentGameState(GameState.Init);

        //InternetConnectivityManager.Instance.onNetConnected += OnInternetConnected;
        //InternetConnectivityManager.Instance.onNetDisconnected += OnInternetDisconnected;

        List<string> boardPrizeOption = new List<string>()
        {
            "10K", "20K", "50K", "100K", "200K", "5M", "10M", "20M", "50M"
        };

        var prizes = new List<string>() { "5K", "7K", "10K", "20K", "50K", "100K", "200K", "5M", "10M", "20M", "50M" };
        
        gamePrizes = new ReadOnlyCollection<string>(prizes);
        
        entryFeeSelectionDropDown.options.Clear();
        entryFeeSelectionDropDown.AddOptions(boardPrizeOption);
        
        GeneratedPublicPrizeItems();
        ApiResponse response = customResponse ?? JsonUtility.FromJson<ApiResponse>(DataSaver.ReadData(PlayerPrefs.GetString("userData", string.Empty)));

        //Debug.Log($"Response: {PlayerPrefs.GetString("userData", string.Empty)}, User: {response.user.name}, Email: {response.user.email}");
        if (response != null)
        {
            bool isLoggedIn = DataManager.Instance.IsLoggedIn;
            DataManager.Instance.SetCurrentUser(response);

            if (!string.IsNullOrEmpty(response.token))
            {
                if (!isLoggedIn)
                {
                    ShowCoinLoadingWithMessage("Getting user coins, please wait.");
        
                    APIHandler.Instance.GetUserTotalCoin(DataManager.Instance.Token, res =>
                    {
                        CoinResponse coinResponse = JsonUtility.FromJson<CoinResponse>(res);
                        if (coinResponse != null)
                        {
                            DataManager.Instance.SetCoins(Convert.ToInt32(coinResponse.coin));
                            UpdateCoinText();
                            CloseCoinLoadingPanel();
                        }
                        else
                        {
                            MessageResponse errorResponse = JsonUtility.FromJson<MessageResponse>(res);
                            errorPanelForMenu.ShowMessagePanel(errorResponse.message);
                        }
                    });
                }
        
                string key = PlayerPrefs.GetString("userData") + "_photo";
                Texture2D texture2D = ByteArrayToTexture2D(DataSaver.ReadImageData(key));

                if (texture2D != null)
                {
                    DataManager.Instance.SetPlayerAvatar(CreateSpriteFromTexture(texture2D));
                    userAvatar.sprite = CreateSpriteFromTexture(texture2D);
                }

                userType = UserType.APP;
                DataManager.Instance.SetCurrentUserType(UserType.APP);
                DataManager.Instance.SetFeePercentage(PlayerPrefs.GetInt(string.Concat(key, "_config")));
                //popUp.CloseMessagePanel();
            }
            else
            {
                userType = UserType.NORMAL;
                DataManager.Instance.SetCurrentUserType(UserType.NORMAL);
                DataManager.Instance.SetCoins(response.user.coins);
            }
        }

        if (DataManager.Instance == null || !DataManager.Instance.IsLoggedIn)
        {
            OpenMemberTypeSelectionPanel();
            return;
        }
        
        CloseAllPanel();
        SetUserProfileData();
        backBtn.gameObject.SetActive(false);
        userIdCopyBtn.onClick.RemoveAllListeners();
        userIdCopyBtn.onClick.AddListener(CopyUserId);
        OpenGameModePanel();
        
        ChangeBoardButtonBg(PlayerPrefs.GetInt("bordbg_index", 0));
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) 
            return;
        
        //backAction?.Invoke();
        //backAction = null;
        if(backBtnActionQueue.Count > 0)
            backBtnActionQueue.Pop()?.Invoke();
        
        //backAction?.Invoke();
    }

    private void OnInternetConnected()
    {
        noInternetPopUp.CloseMessagePanel();
        ActiveOrDeactivatedMultiplayer(true);
    }

    private void OnInternetDisconnected()
    {
        if (DataManager.Instance.GameType == GameType.Null)
        {

            ActiveOrDeactivatedMultiplayer(false);
            noInternetPopUp.ShowMessagePanelWithCustomAction("No Internet Connection!\nPlease connect to the internet to use all features", noInternetPopUp.CloseMessagePanel);
            return;
        }
        
    }

    public void ShowNoInternetPopUp()
    {
        noInternetPopUp.ShowMessagePanelWithCustomAction("No Internet Connection!\nPlease connect to the internet to use all features", UIManager.Instance.BackToMainMenu);
    }

    private void ActiveOrDeactivatedMultiplayer(bool active)
    {
        onlineBtn.interactable = active;
        privateBtn.interactable = active;
    }
    
    private void GeneratedPublicPrizeItems()
    {
        foreach (string prize in gamePrizes)
        {
            PublicPrizeItem instantiatedItem = Instantiate(publicPrizeItem, publicPrizeParent);
            instantiatedItem.SetPublicPrizeItem(prize, SelectPublicEntryFee);
            instantiatedItem.name = $"{prize} Public Prize Item";
        }
    }

    private void SelectPublicEntryFee(string entryFee)
    {
        SelectEntryFeeAndPlay(DataManager.Instance.GetIntValueFromFormattedPrizeValue(entryFee));
    }
    
    private void ClearInputFieldTexts()
    {
        logInEmailField.text = string.Empty;
        logInPasswordField.text = string.Empty;

        regEmailField.text = string.Empty;
        regNameField.text = string.Empty;
        regPasswordField.text = string.Empty;
        regRetypePasswordField.text = string.Empty;
    }

    private void ShowLoadingPanelWithMessage(string message)
    {
        loadingPopUp.ShowMessagePanel(message);
    }

    private void ShowCoinLoadingWithMessage(string message)
    {
        coinLoading.ShowMessagePanel(message);
    }

    private void CloseLoadingPanel()
    {
        loadingPopUp.CloseMessagePanel();
    }

    private void CloseCoinLoadingPanel()
    {
        coinLoading.CloseMessagePanel();
    }

    private void OpenCloseFooterPanel(bool value)
    {
        footerPanel.SetActive(value);
    }

    private void SetInputFieldContentType(TMP_InputField inputField, TMP_InputField.ContentType type)
    {
        inputField.contentType = type;
    }

    public void CloseAllUIPanel()
    {
        commonGameMemberPanel.SetActive(false);
        CloseAllPanel();
    }

    public void EnableDefaultMemberSelectionPanel()
    {
        commonGameMemberPanel.SetActive(true);
        OpenGameModePanel();
    }
    
    private void CloseAllPanel()
    {
        memberTypeSelectionPanel.SetActive(false);
        logRegSelectionPanel.SetActive(false);
        loginPanel.SetActive(false);
        registrationPanel.SetActive(false);
        gameModePanel.SetActive(false);
        publicPrizeSelectionPanel.SetActive(false);
        privatePrizeSelectionPanel.SetActive(false);
        privateModePanel.SetActive(false);
        loadingPopUp.gameObject.SetActive(false);
        userDetailPanel.SetActive(false);
        userStatsPanel.SetActive(false);
        freeOrTrainingPanel.SetActive(false);
        teamSelectionPanel.SetActive(false);
        localMultiPlayerPanel.SetActive(false);
    }

    public void AddNewActionToBackBtn(Action action)
    {
        backBtn.onClick.RemoveAllListeners();
        backBtn.onClick.AddListener(()=> action?.Invoke());
        
        //backAction = action;
        backBtnActionQueue.Push(action);
    }

    private void OpenGameModePanel()
    {
        CloseAllPanel();

        OpenTopBar();
        gameModePanel.SetActive(true);
        vsAiBtn.gameObject.SetActive(DataManager.Instance.CurrentUserType == UserType.NORMAL);
        DataManager.Instance.ResetCurrentMatchData();
    }

    public void OpenTopBar()
    {
        userProfilePanel.SetActive(true);
    }

    private void CloseTopBar()
    {
        userProfilePanel.SetActive(false);
    }

    public void OpenLocalMultiplayerPanel()
    {
        CloseAllPanel();
        //managepowerandcastdiceLocalMultipayer();
        AddNewActionToBackBtn(()=> 
        {
            OpenGameModePanel();
            DataManager.Instance.ResetCurrentMatchData();
        });
       
        DataManager.Instance.SetCurrentRoomType(RoomType.Free);
        DataManager.Instance.SetGameType(GameType.LocalMultiplayer);
        backBtn.gameObject.SetActive(true);
        localMultiPlayerPanel.SetActive(true);
        SelectTwoPlayer();
    }

    public void StartLocalMultiplayerGame()
    {
        UIManager.Instance.StartMultiplayerGame();
       
        localMultiPlayerPanel.SetActive(false);
    }

    private void OpenPublicPrizeSelectionPanel()
    {
        if (DataManager.Instance.CurrentRoomType != RoomType.Random &&
            DataManager.Instance.GameType != GameType.Single) return;
        
        prizeSelectionCoinTxt.text = UIManager.FormatLargeNumber(DataManager.Instance.Coins);
        prizeSelectionTitleTxt.text = (DataManager.Instance.CurrentRoomType == RoomType.Random) ? "Online" : "VS AI";
        publicPrizeSelectionPanel.SetActive(true);
    }

    public void SelectSinglePlayerAndOpenPrizeSelection()
    {
        DataManager.Instance.SetGameType(GameType.Single);
        DataManager.Instance.SetCurrentRoomType(RoomType.AI);
        DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);
        
        CloseAllPanel();
        AddNewActionToBackBtn(() =>
        {
            backBtn.gameObject.SetActive(false);
            OpenFreeOrTrainingPanel();
        });
        
        backBtn.gameObject.SetActive(true);
        OpenPublicPrizeSelectionPanel();
    }
    
    public void SelectOnlineModeAndOpenPrizeSelection()
    {
        DataManager.Instance.SetGameType(GameType.Multiplayer);
        DataManager.Instance.SetCurrentRoomType(RoomType.Random);
        DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);
       
        CloseAllPanel();
        AddNewActionToBackBtn(() =>
        {
            //CloseAllPanel();
            backBtn.gameObject.SetActive(false);
            OpenGameModePanel();
        });
        managepowerandcastdiceOnline();
        backBtn.gameObject.SetActive(true);
        OpenPublicPrizeSelectionPanel();
    }

     
    public void managepowerandcastdiceOnline()
    {
        GameManager.Instance.manageRollingDice[0] = GameManager.Instance.manageRollingDiceM[0];
        GameManager.Instance.manageRollingDice[1] = GameManager.Instance.manageRollingDiceM[1];
        GameManager.Instance.manageRollingDice[2] = GameManager.Instance.manageRollingDiceM[2];
        GameManager.Instance.manageRollingDice[3] = GameManager.Instance.manageRollingDiceM[3];
    }
    //public void managepowerandcastdiceLocalMultipayer()
    //{
    //    GameManager.Instance.manageRollingDice[0] = GameObject.Find("RollingDice_Red").GetComponent<RollingDice>();
    //    GameManager.Instance.manageRollingDice[1] = GameObject.Find("RollingDice_Blue").GetComponent<RollingDice>();
    //    GameManager.Instance.manageRollingDice[2] = GameObject.Find("RollingDice_Yellow").GetComponent<RollingDice>();
    //    GameManager.Instance.manageRollingDice[3] = GameObject.Find("RollingDice_Green").GetComponent<RollingDice>();
    //}
    public void SelectCreateRoomModeAndOpenTeamSelection()
    {
        DataManager.Instance.SetCurrentRoomMode(RoomMode.Create);
        CommonPrivateModeSelectionTask();
    }

    //public void SelectJoinRoomModeAndOpenTeamSelection()
    //{
    //    DataManager.Instance.SetCurrentRoomMode(RoomMode.Join);
    //    CommonPrivateModeSelectionTask();
    //}


    private void CommonPrivateModeSelectionTask()
    {
        DataManager.Instance.SetCurrentRoomType(RoomType.Private);

        AddNewActionToBackBtn(() =>
        {
            DataManager.Instance.SetCurrentEntryFees(0);
            OpenPrivateModePanel();
        });

        OpenPublicPrizeSelectionPanel();
    }
    
    public void OpenPrivateModePanel()
    {
        DataManager.Instance.SetGameType(GameType.Multiplayer);
        DataManager.Instance.SetCurrentRoomType(RoomType.Private);
        DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);
        //photonView.RPC(nameof(managepowerandcastdiceOnline), RpcTarget.AllBuffered);
        managepowerandcastdiceOnline();

        CloseAllPanel();
        AddNewActionToBackBtn(() =>
        {
            //CloseAllPanel();
            DataManager.Instance.SetGameType(GameType.Null);
            DataManager.Instance.SetCurrentRoomType(RoomType.Null);
            DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);
            backBtn.gameObject.SetActive(false);
            OpenGameModePanel();
        });
        
        backBtn.gameObject.SetActive(true);
        privateModePanel.SetActive(true);
    }

    public void OpenFreeOrTrainingPanel()
    {
        CloseAllPanel();
        AddNewActionToBackBtn(() =>
        {
            //CloseAllPanel();
            backBtn.gameObject.SetActive(false);
            DataManager.Instance.ResetCurrentMatchData();
            OpenGameModePanel();
        });

        backBtn.gameObject.SetActive(true);
        OpenCloseFooterPanel(true);

        freeOrTrainingPanel.SetActive(true);
    }

    public void SelectFreeSingleMode()
    {
        DataManager.Instance.SetGameType(GameType.Single);
        DataManager.Instance.SetCurrentRoomType(RoomType.Free);
        DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);

        AddNewActionToBackBtn(() =>
        {
            UIManager.Instance.BackToMainMenu();
        });

        UIManager.Instance.PlayWithAI();
    }

    public void HideBackButton()
    {
        backBtn.gameObject.SetActive(false);
    }

    public void ShowBackButton()
    {
        backBtn.gameObject.SetActive(true);
    }

    public void SelectBoardPrizeAndContinue()
    {
        SetBoardPrizeAndLoadScene(DataManager.Instance.CurrentEntryFee);
    }

    private void SetUserProfileData()
    {
        // SelectBoardPrize(0);
        userNameForProfileTxt.text = Helper.GetPascalCaseString(DataManager.Instance.CurrentUser.name);
        userIdTxt.text = DataManager.Instance.CurrentUser.user_id;
        userIdTxt.transform.parent.gameObject.SetActive(userType == UserType.APP);
        userIdCopyBtn.gameObject.SetActive(userType == UserType.APP);
        coinTxt.text = "" + UIManager.FormatLargeNumber(DataManager.Instance.Coins);
        OpenTopBar();
        NetworkManager.Instance.SetPlayerNameAndIDCustomProperties();
    }

    public void CopyUserId()
    {
        GUIUtility.systemCopyBuffer = userIdTxt.text;
        userIdCopySuccessTxt.text = $"Your user id <b><i>{userIdTxt.text}</i></b> which is successfully copied into clipboard and ready to share.";
        userIdCopySuccessPanel.SetActive(true);
    }
    
    private void EnableDisableLoadingPanel(bool value)
    {
        loadingPanel.SetActive(value);
    }
    
    private void SetUserType(UserType type)
    {
        userType = type;
        DataManager.Instance.SetCurrentUserType(type);
    }

    private void EnableLogRegSelectionPanel()
    {
        CloseAllPanel();
        AddNewActionToBackBtn(OpenMemberTypeSelectionPanel);
        registrationBtn.gameObject.SetActive(userType == UserType.NORMAL);
        backBtn.gameObject.SetActive(true);
        logRegSelectionPanel.SetActive(true);
    }

    private void OpenMemberTypeSelectionPanel()
    {
        CloseAllPanel();
        backBtn.gameObject.SetActive(false);
        memberTypeSelectionPanel.SetActive(true);
    }

    private void SetBoardPrizeAndLoadScene(int entryFees)
    {

        if (entryFees > DataManager.Instance.Coins)
        {
            popUp.ShowMessagePanel("You don't have enough coins!");
            Debug.Log("You don't have enough coins!");
            return;
        }
        
        DataManager.Instance.SetCurrentEntryFees(entryFees);
        //StartRandomMultiplayer();

        switch (DataManager.Instance.CurrentRoomType)
        {
            case RoomType.AI:
                AddNewActionToBackBtn(() =>
                {
                    //SelectSinglePlayerAndOpenPrizeSelection();
                    UIManager.Instance.BackToMainMenu();
                });

                UIManager.Instance.PlayWithAI();
                break;
            case RoomType.Random:
                AddNewActionToBackBtn(() =>
                {
                    UIManager.Instance.BackToMainMenu();
                });

                CloseAllPanel();
                CloseMenuUI();
                NetworkManager.Instance.OnOnlineButtonClick();
                return;
            case RoomType.Private:
                AddNewActionToBackBtn(OpenPrivateModePanel);
            
                //if(DataManager.Instance.CurrentRoomMode == RoomMode.Create)
                //{
                //    CommonTeamSelectionTasks();
                //    return;
                //}
                break;
        }

        OpenTeamSelectionPanel();

        //Need to open color selection panel;
    }

    public void SelectEntryFeeAndPlay(int entryFee)
    {
        SetBoardPrizeAndLoadScene(entryFee);
    }

    public void Select5KBoardPrizeAndPlay()
    {
        SetBoardPrizeAndLoadScene(5000);
    }

    public void Select10KBoardPrizeAndPlay()
    {
        SetBoardPrizeAndLoadScene(10000);
    }

    public void Select50KBoardPrizeAndPlay()
    {
        SetBoardPrizeAndLoadScene(50000);
    }

    public void OpenTeamSelectionPanel()
    {
        CloseAllPanel();
        teamSelectionPanel.SetActive(true);
    }

    private void CommonTeamSelectionTasks()
    {
        AddNewActionToBackBtn(SelectSinglePlayerAndOpenPrizeSelection);

        switch (DataManager.Instance.CurrentRoomType)
        {
            case RoomType.AI:
            case RoomType.Free:
                StartSinglePlayerGame();
                break;

            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Create:
                Debug.Log("Private");
                
                break;
            
            case RoomType.Private when DataManager.Instance.CurrentRoomMode == RoomMode.Join:
                Debug.Log("Join");
                //CreateCustomRoom();
                CloseAllPanel();
                //ChessUIManager.Instance.networkManager.Connect();
                break;

            default:
                CloseAllUIPanel();
                
                AddNewActionToBackBtn(() =>
                {
                    CloseAllPanel();
                    OpenPublicPrizeSelectionPanel();

                    AddNewActionToBackBtn(() =>
                    {
                        OpenGameModePanel();
                        backBtn.gameObject.SetActive(false);
                    });
                });
                break;
        }

        OpenTeamSelectionPanel();
    }

    public void SetPlayerCountToTwoAndConnect()
    {
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(2);
        CommonTaskForPlayerCountSelection();
    }

    public void SetPlayerCountToThreeAndConnect()
    {
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(3);
        CommonTaskForPlayerCountSelection();
    }

    public void SetPlayerCountToFourAndConnect()
    {
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(4);
        CommonTaskForPlayerCountSelection();
    }

    private void CommonTaskForPlayerCountSelection()
    {
        //CloseAllPanel();
        if(DataManager.Instance.CurrentRoomMode == RoomMode.Create)
        {
            CloseAllPanel();

            OpenPublicPrizeSelectionPanel();

            AddNewActionToBackBtn(() =>
            {
                DataManager.Instance.SetGameType(GameType.Multiplayer);
                DataManager.Instance.SetCurrentRoomType(RoomType.Private);
                DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);

                SelectCreateRoomModeAndLoadPrizeSelection();
            });

            backBtn.gameObject.SetActive(true);
            return;
        }

        CloseMenuUI();
        NetworkManager.Instance.OnOnlineButtonClick();
    }

    public void CreateRoomAndContinue()
    {
        CloseMenuUI();
        NetworkManager.Instance.OnOnlineButtonClick();
    }

    public void JoinAPrivateRoom()
    {
        if (string.IsNullOrEmpty(UIManager.Instance.GetInputtedRoomId()) || UIManager.Instance.GetInputtedRoomId().Length < 4)
        {
            UIManager.Instance.errorPopUp.ShowMessagePanel("Invalid Room id. Please enter correct room id.");
            return;
        }

        CloseAllPanel();
        CreateRoomAndContinue();
    }

    public void CloseTeamSelectionPanel()
    {
        teamSelectionPanel.SetActive(false);
    }

    private void OpenMenuUI()
    {
        menuUIPanel.SetActive(true);
    }

    public void CloseMenuUI()
    {
        menuUIPanel.SetActive(false);
    }

    public void UpdateCoinText()
    {
        coinTxt.text = UIManager.FormatLargeNumber( DataManager.Instance.Coins);
    }

    public void ActiveBackButtonAndLeaveRoom()
    {
        backBtn.gameObject.SetActive(true);
        AddNewActionToBackBtn(() =>
        {
            //ChessUIManager.Instance.back();
        });
    }

    #endregion / Monobehaviour Callbacks and Common Section

    #region Board Change

    public void ChangeBoardButtonBg(int index)
    {
        if(index < 0 || index >= boardCngBtnBgs.Length)
            return;
        
        PlayerPrefs.SetInt("bordbg_index", index);
        boardBg.sprite = boardCngBtnBgs[index];
    }
    public void OpenBoardChangePanel()
    {
        boardChangePanel.SetActive(true);
    }

    #endregion
    
    #region General Member Section

    public void OpenLogRegPanelForGeneralMember()
    {
        SetUserType(UserType.NORMAL);
        EnableLogRegSelectionPanel();
    }

    #endregion / General Member Section
    
    #region App/Hub Member Section

    public void OpenLogRegPanelForAppMember()
    {
        SetUserType(UserType.APP);
        EnableLogRegSelectionPanel();
    }

    #endregion / App/Hub Member Section

    #region Game Mode Selection

    public void StartSinglePlayerGame()
    {
        CloseMenuUI();
        //gameInitializer.StartSinglePlayerGame();
        UIManager.Instance.PlayWithAI();
    }


    private void InitializationMultiplayerUI()
    {
        //ChessUIManager.Instance.OnMultiPlayerModeSelected();
        //NetworkManager.Instance.PlayWithRandomPlayer();
    }

    #endregion/Game Mode Selection

    #region Private Match

    public void SelectTwoPlayer()
    {
        SelectPlayers(0, 2);
    }
    
    public void SelectThreePlayer()
    {
        SelectPlayers(1, 3);
    }
    
    public void SelectFourPlayer()
    {
        SelectPlayers(2, 4);
    }

    private void SelectPlayers(int indexToActive, byte playerCountForCurrentMatch)
    {
        ActivateOnlySelectedToggle(indexToActive);
        DataManager.Instance.SetMaxPlayerNumberForCurrentBoard(playerCountForCurrentMatch);
        
        if(DataManager.Instance.GameType == GameType.Multiplayer)
            CommonPrizeSelectionTask(currentPrizeIndex);
    }
    
    private void ActivateOnlySelectedToggle(int selectedIndex)
    {
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            // Ensure the index is within the range of the list
            if (selectedIndex < 0 || selectedIndex >= privatePlayerSelectionToggles.Count)
            {
                Debug.LogError("Invalid index passed.");
                return;
            }
        
            // if(playerSelectionToggles[selectedIndex].isOn)

            // Loop through all the toggles in the list
            for (int i = 0; i < privatePlayerSelectionToggles.Count; i++)
            {
                // Deactivate all toggles except the one with the matching index
                privatePlayerSelectionToggles[i].isOn = (i == selectedIndex);
            }
            return;
        }
        
        // Ensure the index is within the range of the list
        if (selectedIndex < 0 || selectedIndex >= localPlayerSelectionToggles.Count)
        {
            Debug.LogError("Invalid index passed.");
            return;
        }
        
        // if(playerSelectionToggles[selectedIndex].isOn)

        // Loop through all the toggles in the list
        for (int i = 0; i < localPlayerSelectionToggles.Count; i++)
        {
            // Deactivate all toggles except the one with the matching index
            localPlayerSelectionToggles[i].isOn = (i == selectedIndex);
        }
    }
    
    public void SelectCreateRoomModeAndLoadPrizeSelection()
    {
        DataManager.Instance.SetGameType(GameType.Multiplayer);
        DataManager.Instance.SetCurrentRoomType(RoomType.Private);
        DataManager.Instance.SetCurrentRoomMode(RoomMode.Create);

        //CommonTaskForCreateOrJoinRoom();

        AddNewActionToBackBtn(() =>
        {
            OpenTopBar();
            UIManager.Instance.ClosePrivateRoomJoinPanel();
            OpenPrivateModePanel();
        });

        backBtn.gameObject.SetActive(true);

        OpenPrivatePrizeSelectionPanel();
    }

    public void SelectJoinRoomModeAndContinue()
    {
        DataManager.Instance.SetGameType(GameType.Multiplayer);
        DataManager.Instance.SetCurrentRoomType(RoomType.Private);
        DataManager.Instance.SetCurrentRoomMode(RoomMode.Join);

        AddNewActionToBackBtn(() =>
        {
            UIManager.Instance.ClosePrivateRoomJoinPanel();
            OpenPrivateModePanel();
        });

        backBtn.gameObject.SetActive(true);
        UIManager.Instance.OpenPrivateRoomJoinPanel();
    }


    public void JoinRoomWithRoomId()
    {
        //string roomId = ChessUIManager.Instance.GetInputtedRoomId();
        string roomId = string.Empty;
        Debug.Log("RoomID: " + roomId);
        if (string.IsNullOrEmpty(roomId))
        {
            popUp.ShowMessagePanel("Invalid room id! Please enter valid room id");
            return;
        }

        //ChessUIManager.Instance.networkManager.JoinPrivateRoom();
    }
    
    private void CommonTaskForCreateOrJoinRoom()
    {
        CloseAllPanel();

        AddNewActionToBackBtn(() =>
        {
            CloseAllPanel();
            DataManager.Instance.SetCurrentEntryFees(0);
            DataManager.Instance.SetGameType(GameType.Multiplayer);
            DataManager.Instance.SetCurrentRoomType(RoomType.Private);
            DataManager.Instance.SetCurrentRoomMode(RoomMode.Null);
            SelectTwoPlayer();
        });

        OpenPrivatePrizeSelectionPanel();
    }

    private void OpenPrivatePrizeSelectionPanel()
    {
        SelectTwoPlayer();
        CloseAllPanel();
        CloseTopBar();
        privatePrizeSelectionPanel.SetActive(true);
    }

    private string GetPrizeAt(int index)
    {
        return gamePrizes[index];
    }

    private int currentPrizeIndex;
    
    public void SelectNextBoardPrize()
    {
        currentPrizeIndex++;
        if (currentPrizeIndex >= gamePrizes.Count)
            currentPrizeIndex = 0;
        
        CommonPrizeSelectionTask(currentPrizeIndex);
    }

    public void SelectPreviousBoardPrize()
    {
        currentPrizeIndex--;
        if (currentPrizeIndex < 0)
            currentPrizeIndex = gamePrizes.Count - 1;
        
        CommonPrizeSelectionTask(currentPrizeIndex);
    }
    public int prizeValue;
    private void CommonPrizeSelectionTask(int currentIndex)
    {
        int entryFee = DataManager.Instance.GetIntValueFromFormattedPrizeValue(GetPrizeAt(currentPrizeIndex));
        prizeValue = entryFee * DataManager.Instance.MaxPlayerNumberForCurrentBoard;
        
        privateEntryFeeTxt.text = $"Entry: {GetPrizeAt(currentPrizeIndex)}";
        privatePrizeTxt.text = $"{Helper.GetReadableNumber(prizeValue)}";
        
        DataManager.Instance.SetCurrentEntryFees(entryFee);
    }

    public void CreatePrivateRoom()
    {
        if (DataManager.Instance.CurrentEntryFee > DataManager.Instance.Coins)
        {
            popUp.ShowMessagePanel("You don't have enough coins!");
            Debug.Log("You don't have enough coins!");
            return;
        }
        
        OpenTopBar();
        CreateRoomAndContinue();
    }
    #endregion Private Match

    #region Registration Section

    public void OpenRegistrationPanel()
    {
        CloseAllPanel();

        AddNewActionToBackBtn(EnableLogRegSelectionPanel);
        registrationPanel.SetActive(true);
    }

    public void Registration()
    {
        ShowLoadingPanelWithMessage("Registering user, please wait.");

        if (userType == UserType.APP)
        {
            RegistrationForAppUser();
        }
        else
        {
            RegistrationForGeneralMember();
        }
    }

    private void RegistrationForAppUser()
    {
        APIHandler.Instance.AppMemberRegistrationPostRequest(CommonRegistrationTasks(), OnRegistrationSuccess, OnRegistrationFailed);
    }

    private void RegistrationForGeneralMember()
    {
        APIHandler.Instance.GeneralMemberRegistrationPostRequest(CommonRegistrationTasks(), OnRegistrationSuccess, OnRegistrationFailed);
    }

    private WWWForm CommonRegistrationTasks()
    {
        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField(nameParam, regNameField.text);
        wwwForm.AddField(emailParam, regEmailField.text);
        wwwForm.AddField(passwordParam, regPasswordField.text);
        wwwForm.AddField(passwordConfirmParam, regRetypePasswordField.text);
        wwwForm.AddField(gameIdParam, GameID);
        wwwForm.AddField(gameIdParam, DataManager.Instance.GameId);

        return wwwForm;
    }

    private void OnRegistrationSuccess(string jsonData)
    {
        ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonData);

        if (response.status)
        {
            logInEmailField.text = regEmailField.text;
            CloseLoadingPanel();
            OpenLogInPanel();

            return;
        }

        OnRegistrationFailed(response.message);
    }

    private void OnRegistrationFailed(string message)
    {
        MessageResponse response = JsonUtility.FromJson<MessageResponse>(message);
        CloseLoadingPanel();
        popUp.ShowMessagePanel(response != null ? response.message : message);
    }

    #endregion/Registration Section

    #region LogIn Section

    public void OpenLogInPanel()
    {
        CloseAllPanel();

        if (userType == UserType.APP)
        {
            AddNewActionToBackBtn(OpenMemberTypeSelectionPanel);
        }
        else
        {
            AddNewActionToBackBtn(OpenLogRegPanelForGeneralMember);
        }

        loginPanel.SetActive(true);
    }

    public void LogIn()
    {
        ShowLoadingPanelWithMessage("Logging in user, please wait.");

        if (userType == UserType.APP)
        {
            LogInForAppUser();
        }
        else
        {
            LogInForGeneralMember();
        }
    }

    private void LogInForAppUser()
    {
        APIHandler.Instance.AppLogInPostRequest(CommonLogInTasks(), OnLogInSuccess, OnLogInFailed);
    }
    
    private void LogInForGeneralMember()
    {
        APIHandler.Instance.GameLogInPostRequest(CommonLogInTasks(), OnLogInSuccess, OnLogInFailed);
    }

    private WWWForm CommonLogInTasks()
    {
        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField(emailParam, logInEmailField.text);
        wwwForm.AddField(passwordParam, logInPasswordField.text);
        wwwForm.AddField(userTypeParam, Enum.GetName(typeof(UserType), userType));
        wwwForm.AddField(gameIdParam, DataManager.Instance.GameId);

        return wwwForm;
    }
    
    private void OnLogInSuccess(string jsonData)
    {
        ApiResponse response = JsonUtility.FromJson<ApiResponse>(jsonData);

        if (userType == UserType.NORMAL)
        {
            response.user = new User
            {
                email = logInEmailField.text,
                name = logInEmailField.text.Split('@')[0]
            };
            
            Debug.Log($"Email: {response.user.email}, ID: {logInEmailField.text}");
        }
        
        DataManager.Instance.SetCurrentUser(response);

        if (DataManager.Instance.CurrentUserType == UserType.APP)
        {
            APIHandler.Instance.GetUserTotalCoin(DataManager.Instance.Token, res =>
            {
                CoinResponse coinResponse = JsonUtility.FromJson<CoinResponse>(res);

                if (coinResponse != null)
                {
                    DataManager.Instance.SetCoins(Convert.ToInt32(coinResponse.coin));
                    response.user.coins = DataManager.Instance.Coins;
                    Debug.Log($"Coin Res: {res}");
                    Invoke(nameof(CommonLogInSuccessTask), 2);
                    return;
                }
                
                Debug.LogError($"Response: {res}");
                popUp.ShowMessagePanel("Something went wrong, please try again");
            });

            APIHandler.Instance.DownloadImageAsync(response.user.photo, texture2D =>
            {
                if (texture2D == null)
                {
                    popUp.ShowMessagePanel("Something went wrong, please try again while downloading profile picture");
                    return;
                }

                Sprite sprite = CreateSpriteFromTexture(texture2D);
                userAvatar.sprite = sprite;

                byte[] imageData = texture2D.EncodeToJPG();
                Debug.Log($"FileName: {string.Concat(response.user.email, "_photo")}");
                DataSaver.WriteAllBytes(imageData, string.Concat(response.user.email, "_photo"));
                DataManager.Instance.SetPlayerAvatar(CreateSpriteFromTexture(texture2D));
            });

            APIHandler.Instance.GetConfig(DataManager.Instance.Token, configResponse =>
            {
                MessageResponse config = JsonUtility.FromJson<MessageResponse>(configResponse);

                if (configResponse == null || !config.status)
                    return;

                DataManager.Instance.SetFeePercentage(Convert.ToInt32(config.message));
                PlayerPrefs.SetInt(string.Concat(response.user.email, "_config"), Convert.ToInt32(config.message));
            });
        }
        else
        {
            Invoke(nameof(CommonLogInSuccessTask), 1);
        }

        Debug.Log($"Res: {JsonUtility.ToJson(response)}");
        PlayerPrefs.SetString("userData", response.user.email);
        DataSaver.WriteData(response, response.user.email);
    }

    private void GetAppUserCoin()
    {
        popUp.ShowMessagePanel("Getting user coins, please wait.");
        
        APIHandler.Instance.GetUserTotalCoin(DataManager.Instance.Token, res =>
        {
            CoinResponse coinResponse = JsonUtility.FromJson<CoinResponse>(res);

            if (coinResponse != null)
            {
                DataManager.Instance.SetCoins(Convert.ToInt32(coinResponse.coin));
                return;
            }
                
            Debug.LogError($"Response: {res}");
            popUp.ShowMessagePanel("Something went wrong, please try again");
        });
        
        popUp.CloseMessagePanel();
    }

    
    private void CommonLogInSuccessTask()
    {
        CloseAllPanel();
        SetUserProfileData();

        backBtn.gameObject.SetActive(false);
        
        //CloseAllUIPanel();
        OpenGameModePanel();
    }

    private void OnLogInFailed(string message)
    {
        try
        {
            MessageResponse response = JsonUtility.FromJson<MessageResponse>(message);
            
            CloseLoadingPanel();

            popUp.ShowMessagePanel(response != null ? response.message : message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            CloseLoadingPanel();
            popUp.ShowMessagePanel(message);
        }
    }

    #endregion/LogIn Section

    #region LogOut

    public void LogOut()
    {
        if (DataManager.Instance.CurrentUserType == UserType.APP)
        {
            ShowLoadingPanelWithMessage("Logging out, please wait.");
            APIHandler.Instance.LogOut(DataManager.Instance.Token, response =>
            {
                Debug.Log($"LogOur: {response}");

                LogOutResponse logOutResponse = JsonUtility.FromJson<LogOutResponse>(response);
                if (logOutResponse is { status: true })
                {
                    DataSaver.DeleteFile(PlayerPrefs.GetString("userData") + "_photo");
                    DataSaver.DeleteFile(PlayerPrefs.GetString("userData"));
                }
                else
                {
                    popUp.ShowMessagePanel("Can't log out! Please try again.");
                }
            });

            CloseLoadingPanel();
        }

        userAvatar.sprite = defaultUserAvatarSprite;
        userDetailAvatarImage.sprite = defaultUserAvatarSprite;
        CloseTopBar();

        ClearInputFieldTexts();
        DataSaver.DeleteFile(PlayerPrefs.GetString("userData"));
        PlayerPrefs.DeleteKey("userData");
        DataManager.Instance.ReInitialize();
        
        if(isCustomMenuEnable)
            customMenuPanel.SetActive(true);
        else
            OpenMemberTypeSelectionPanel();
    }

    #endregion/LogOut

    #region Game History
    public void OpenGameHistoryPanel()
    {
        if (userType != UserType.APP || isDownloadingHistoryData)
            return;

        ShowLoadingPanelWithMessage("Loading game, history please wait.");

        isDownloadingHistoryData = true;

        APIHandler.Instance.GetGameHistoryData(DataManager.Instance.Token, apiResponse =>
        {
            GameHistoryResponse gameHistoryResponse = JsonUtility.FromJson<GameHistoryResponse>(apiResponse);

            Debug.Log(apiResponse);

            if (gameHistoryResponse == null || gameHistoryResponse.status == false ||
                gameHistoryResponse.data.Count <= 0)
            {
                noHistoryContentPanel.SetActive(true);
                userStatsPanel.SetActive(true);
                isDownloadingHistoryData = false;
                CloseLoadingPanel();

                return;
            }

            for (int i = 0; i < gameHistoryResponse.data.Count; i++)
            {
                gameHistoryResponse.data[i].game_session_id = i.ToString();
                HistoryItem item = Instantiate(historyItem, historyItemParent.transform);
                item.SetHistoryItemData(gameHistoryResponse.data[i]);
            }

            noHistoryContentPanel.SetActive(false);
            userStatsPanel.SetActive(true);
            isDownloadingHistoryData = false;

            CloseLoadingPanel();
        });
    }

    #endregion / Game History

    #region User Detail

    public void OpenAvatarSelectionPanel()
    {
        avatarSelectionPanel.SetActive(true);
    }

    public void CloseAvatarSelectionPanel()
    {
        avatarSelectionPanel.SetActive(false);
    }

    public void OpenUserDetailPanel()
    {
        if (DataManager.Instance.CurrentGameState != GameState.Init)
            return;
        
       // ChangeAvatar(PlayerPrefs.GetInt("userAvatar", 0));
        
        userDetailUserNameTxt.text = Helper.GetPascalCaseString(DataManager.Instance.CurrentUser.name);

         userDetailCoinTxt.text =  Helper.GetReadableNumber(DataManager.Instance.Coins);
         uCoinsTxt.text = userDetailCoinTxt.text;
        
        if (DataManager.Instance.CurrentUserType == UserType.NORMAL)
        {
            userDetailUserNameTxt.text = Helper.GetPascalCaseString(DataManager.Instance.CurrentUser.name);
            userDetailUserIdTxt.text = userDetailUserNameTxt.text;
            totalWinSumTxt.text = Helper.GetReadableNumber(DataManager.Instance.Coins);
            userDetailPanel.SetActive(true);
            userDetailButton.interactable = true;
        }
        else
        {
            if (DataManager.Instance.GetDefaultAvatarSprite())
                userDetailAvatarImage.sprite = DataManager.Instance.GetDefaultAvatarSprite();

            ShowLoadingPanelWithMessage("Loading user data, please wait.");

            APIHandler.Instance.GetUserDetails(DataManager.Instance.Token, response =>
            {
                if (string.IsNullOrEmpty(response))
                {
                    CloseLoadingPanel();
                    popUp.ShowMessagePanel("Server Error. Please try later.");
                    return;
                }

                Debug.Log($"StatusResponse: {response}");
                StatsResponse statsResponse = JsonUtility.FromJson<StatsResponse>(response);
                
                if (statsResponse is not { status: true })
                {
                    CloseLoadingPanel();
                    return;
                }

                totalGamesWonTxt.text = $"{statsResponse.total_win_game} of {statsResponse.total_game}"; //string.Concat("Games Won: ", statsResponse.total_win_game);

                winRateTxt.text = $"{statsResponse.win_rate}%";
                
                currentWinStreakTxt.text = $"{statsResponse.current_streak}";
                
                totalWinSumTxt.text = statsResponse.total_win_sum;
                
                userDetailUserNameTxt.text = Helper.GetPascalCaseString(DataManager.Instance.CurrentUser.name);
                userDetailUserIdTxt.text = DataManager.Instance.CurrentUser.user_id;
                
                CloseLoadingPanel();

                userDetailPanel.SetActive(true);
                //userDetailButton.interactable = true;
            });
        }

        //userDetailPanel.SetActive(true);
        //userDetailButton.interactable = true;
    }

    public void ChangeAvatar(int avatarIndex)
    {
        if (avatarIndex < 0 || avatarIndex > avatarImages.Count)
        {
            Debug.LogError($"{avatarIndex} is a invalid avatar index, so skipping it");
            return;
        }
        
        userAvatar.sprite = avatarImages[avatarIndex].sprite;
        userDetailAvatarImage.sprite = avatarImages[avatarIndex].sprite;
        DataManager.Instance.SetPlayerAvatar(avatarImages[avatarIndex].sprite);
        for (int i = 0; i < avatarImages.Count; i++)
        {
            avatarImages[i].transform.GetChild(0).gameObject.SetActive(avatarIndex == i);
        }

        PlayerPrefs.SetInt("userAvatar", avatarIndex);
    }


    #endregion \ User Detail

    #region Image Creation

    private Sprite CreateSpriteFromTexture(Texture2D texture2D)
    {
        return Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), Vector2.zero);
    }

    private Texture2D ByteArrayToTexture2D(byte[] byteArray)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(byteArray);
        return texture;
    }

    #endregion/ Image Creation

    #region Debug
    [SerializeField] private int Debug_Coins = 10000;

    [ContextMenu(nameof(Debug_Add100KCoins))]
    public void Debug_Add100KCoins()
    {
        switch (DataManager.Instance.CurrentUserType)
        {
            case UserType.NORMAL:
                DataManager.Instance.UpdateNormalUserCoins(Debug_Coins, false);
                break;
            case UserType.APP:
                loadingPopUp.ShowMessagePanel($"Please wait, giving user {Debug_Coins} coins");
                APIHandler.Instance.GiveCoinToUser(DataManager.Instance.CurrentUser.id.ToString(), Debug_Coins, GiveAppUserDebugCoin);
                break;
        }

        coinTxt.text = Helper.GetReadableNumber(DataManager.Instance.Coins).ToString();
    }

    private void GiveAppUserDebugCoin(string res)
    {
        Debug.Log($"Debug_Coin_API_Res: {res}");
        loadingPopUp.CloseMessagePanel();

        MessageResponse messageRes = JsonUtility.FromJson<MessageResponse>(res);
        if (messageRes != null)
        {
            if (messageRes.status)
            {
                DataManager.Instance.UpdateAppUserCoin(Debug_Coins);
                popUp.ShowMessagePanel($"Congratulation! You've got {Debug_Coins} coins.");
                coinTxt.text = Helper.GetReadableNumber(DataManager.Instance.Coins).ToString();
                return;
            }

            popUp.ShowMessagePanel($"{messageRes.message}");
        }

        popUp.ShowMessagePanel($"Something went wrong. Please try again later.");
    }

    [ContextMenu(nameof(Debug_RemoveAllCoins))]
    public void Debug_RemoveAllCoins()
    {
        DataManager.Instance.UpdateNormalUserCoins(0);
        coinTxt.text = Helper.GetReadableNumber(DataManager.Instance.Coins).ToString();
    }

    #endregion

    #region Team Selection and Game Initialization
    public void OnVSComputer()
    {
        //gamePlayPreference.SetActive(true);
    }
    #endregion Team Selection and Game Initialization
}
