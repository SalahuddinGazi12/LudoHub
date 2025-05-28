//using ExitGames.Client.Photon;
//using Photon.Pun;
//using Photon.Realtime;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public RollingDice rolledDice;
    public int numOfStepsToMove;
    public bool canMove = true;
    public DiceColor activeDiceColor = DiceColor.Unknown;
    public DiceAnimationController diceAnimationController;

    List<PathPoint> playerOnPathPointsList = new List<PathPoint>();
    public bool canDiceRoll = true;
    public bool transferDice = false;
    public bool selfDice = false;

    public int blueOutPlayers;
    public int redOutPlayers;
    public int greenOutPlayers;
    public int yellowOutPlayers;

    public int blueCompletePlayer;
    public int redCompletePlayer;
    public int greenCompletePlayer;
    public int yellowCompletePlayer;

    public RollingDice[] manageRollingDice;
    public RollingDice[] manageRollingDiceM;
    public PlayerPiece[] redPlayerPiece;
    public PlayerPiece[] bluePlayerPiece;
    public PlayerPiece[] yellowPlayerPiece;
    public PlayerPiece[] greenPlayerPiece;
    public SpriteRenderer[] blinksprite;
    public int TotalPlayerCanPlay { get; private set; }

    public AudioSource audioSource;
    public GameObject endgame;
    
    // Booleans to track which player can move
    private bool blueCanMove = true;
    private bool redCanMove = true;
    private bool greenCanMove = true;
    private bool yellowCanMove = true;

   // [SerializeField] private PhotonView photonView;

    private const byte GAME_OVER_EVENT_CODE = 1;
    private const byte CURRENT_TURN_EVENT_CODE = 2;

    [SerializeField] private List<DiceColor> currentBoardsPlayers = new List<DiceColor>();
    

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        
        currentBoardsPlayers = new List<DiceColor>();
        //DontDestroyOnLoad(gameObject);

        //if(DataManager.Instance.GameType == GameType.Multiplayer)
        //{
        //    SetPieceId(redPlayerPiece);
        //    SetPieceId(bluePlayerPiece);
        //    SetPieceId(yellowPlayerPiece);
        //    SetPieceId(greenPlayerPiece);
        //}
    }

    public void SetPlayerAvatar(Sprite avatarSprite, DiceColor diceColor)
    {
        manageRollingDice[(int)diceColor].SetSelfAvatar(avatarSprite);
    }

    public void RotateAllPieces(Transform localTrans)
    {
        Vector3 localRot = localTrans.localEulerAngles * -1;

        // Group all player pieces into one list of IEnumerable to support different collection types
        List<IEnumerable<PlayerPiece>> allPlayerPieces = new List<IEnumerable<PlayerPiece>>
        {
            redPlayerPiece,
            bluePlayerPiece,
            yellowPlayerPiece,
            greenPlayerPiece
        };
        
        foreach (IEnumerable<PlayerPiece> playerPieces in allPlayerPieces)
        {
            foreach (PlayerPiece playerPiece in playerPieces)
            {
                // Rotate the player piece
                playerPiece.transform.localEulerAngles = localRot;

                // Example condition for red player piece (you can customize this logic)
                switch (playerPiece.SelfDiceColor)
                {
                    case DiceColor.Red:
                        playerPiece.transform.localPosition = new Vector3(playerPiece.transform.localPosition.x, playerPiece.transform.localPosition.y, playerPiece.transform.localPosition.z);
                        break;

                    case DiceColor.Blue:
                        playerPiece.transform.localPosition = new Vector3(playerPiece.transform.localPosition.x+0.3f, playerPiece.transform.localPosition.y, playerPiece.transform.localPosition.z);
                        break;

                    case DiceColor.Yellow:
                        playerPiece.transform.localPosition = new Vector3(playerPiece.transform.localPosition.x-0.2f, playerPiece.transform.localPosition.y - 0.2f, playerPiece.transform.localPosition.z);
                        break;

                    case DiceColor.Green:
                        playerPiece.transform.localPosition = new Vector3(playerPiece.transform.localPosition.x-0.1f, playerPiece.transform.localPosition.y , playerPiece.transform.localPosition.z);
                        break;
                }

                // Ensure the localPosition is explicitly set (if needed for some reason)
               
            
            }
        }
    }

    public void ChangeAllPieceSprite(int index)
    {
        foreach (PlayerPiece playerPiece in redPlayerPiece)
        {
            playerPiece.spriteRenderer.sprite = DataManager.Instance.boardGraphics[index].redPieceSprite;
        }
        
        foreach (PlayerPiece playerPiece in bluePlayerPiece)
        {
            playerPiece.spriteRenderer.sprite = DataManager.Instance.boardGraphics[index].bluePieceSprite;
        }
        
        foreach (PlayerPiece playerPiece in yellowPlayerPiece)
        {
            playerPiece.spriteRenderer.sprite = DataManager.Instance.boardGraphics[index].yellowPieceSprite;
        }
        
        foreach (PlayerPiece playerPiece in greenPlayerPiece)
        {
            playerPiece.spriteRenderer.sprite = DataManager.Instance.boardGraphics[index].greenPieceSprite;
        }
    }
    public void ChangeAllblinkSprite(int index)
    {
        // Set the sprites
        blinksprite[0].sprite = DataManager.Instance.boardGraphics[index].redBlinkSprite;
        blinksprite[1].sprite = DataManager.Instance.boardGraphics[index].blueBlinkSprite;
        blinksprite[2].sprite = DataManager.Instance.boardGraphics[index].yellowBlinkSprite;
        blinksprite[3].sprite = DataManager.Instance.boardGraphics[index].greenBlinkSprite;

        // Define the default color
        Color defaultColor = Color.white; // Or whatever your default color is
        Color blinkColor = new Color(0.5f, 1.0f, 0.5f, 1.0f); // Blink color when index is 0

        // Apply the appropriate color based on index
        foreach (var sprite in blinksprite)
        {
            var spriteRenderer = sprite.GetComponent<SpriteRenderer>();
            spriteRenderer.color = (index == 0) ? blinkColor : defaultColor;
        }
    }
    public void StartOrStopRedPieceBlinking(bool shouldStart)
    {
        if (shouldStart)
        {
            foreach (PlayerPiece playerPiece in redPlayerPiece)
            {
                playerPiece.StartBlinking();
            }
            
            return;
        }
        
        foreach (PlayerPiece playerPiece in redPlayerPiece)
        {
            playerPiece.StopBlinking();
        }
    }
    
    public void StartOrStopBluePieceBlinking(bool shouldStart)
    {
        if (shouldStart)
        {
            foreach (PlayerPiece playerPiece in bluePlayerPiece)
            {
                playerPiece.StartBlinking();
            }
            
            return;
        }
        
        foreach (PlayerPiece playerPiece in bluePlayerPiece)
        {
            playerPiece.StopBlinking();
        }
    }
    
    public void StartOrStopYellowPieceBlinking(bool shouldStart)
    {
        if (shouldStart)
        {
            foreach (PlayerPiece playerPiece in yellowPlayerPiece)
            {
                playerPiece.StartBlinking();
            }
            
            return;
        }
        
        foreach (PlayerPiece playerPiece in yellowPlayerPiece)
        {
            playerPiece.StopBlinking();
        }
    }
    
    public void StartOrStopGreenPieceBlinking(bool shouldStart)
    {
        if (shouldStart)
        {
            foreach (PlayerPiece playerPiece in greenPlayerPiece)
            {
                playerPiece.StartBlinking();
            }
            
            return;
        }
        
        foreach (PlayerPiece playerPiece in greenPlayerPiece)
        {
            playerPiece.StopBlinking();
        }
    }

    public void StartStopBlinking(DiceColor color, bool shouldStart)
    {
        switch (color)
        {
            case DiceColor.Red:
                StartOrStopRedPieceBlinking(shouldStart);
                break;
            case DiceColor.Blue:
                StartOrStopBluePieceBlinking(shouldStart);
                break;
            case DiceColor.Yellow:
                StartOrStopYellowPieceBlinking(shouldStart);
                break;
            case DiceColor.Green:
                StartOrStopGreenPieceBlinking(shouldStart);
                break;
        }
    }
    
    private void Start()
    {
        // Register the event handler for receiving events
     //   PhotonNetwork.NetworkingClient.EventReceived += OnPhotonGameEventReceived;
    }

    private void OnDestroy()
    {
        // Unregister the event handler to avoid memory leaks
      //  PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonGameEventReceived;
    }

    public void SetUpBoardAndTotalPlayers(int totalPlayers)
    {
        TotalPlayerCanPlay = totalPlayers;
        
        foreach (RollingDice rollingDice in manageRollingDice)
        {
            rollingDice.SetUpDiceRotation();
        }
        
        Debug_CheckAndSetIfAnyDebugIsOnOrNot();
    }

    public void AddPlayerToCurrentRoomPlayersList(DiceColor diceColor)
    {
        if (!currentBoardsPlayers.Contains(diceColor))
        {
            currentBoardsPlayers.Add(diceColor);
        }
    }

    public void RemovePlayerToCurrentRoomPlayersList(DiceColor diceColor)
    {
        if (!currentBoardsPlayers.Contains(diceColor)) return;
        
        currentBoardsPlayers.Remove(diceColor);
            
        //if(rolledDice.SelfDiceColor == diceColor && PhotonNetwork.IsMasterClient)
            UIManager.Instance.ChangeTurn(ShiftDice());
    }

    public void SetDefaultRolledDice()
    {
        rolledDice = manageRollingDice[0];
    }
    
    private void SetPieceId(PlayerPiece[] playerPieces)
    {
        for (int i = 0; i < playerPieces.Length; i++)
        {
            playerPieces[i].SetSelfId((byte)i);
        }
    }

    public void ChangeActiveDiceColor(DiceColor diceColor)
    {
        activeDiceColor = diceColor;

        if (DataManager.Instance.GameType == GameType.Multiplayer && DataManager.Instance.OwnDiceColor == activeDiceColor)
        {
            ManuallyMoveGameControl();
        }
    }

    private void ManuallyMoveGameControl()
    {
        transferDice = true;
        canDiceRoll = true;
    }


    private Action actionToRunInRPC = null;

    //public void RunMethodInRPC(Action action, //RpcTarget rpcTarget = RpcTarget.AllBuffered, params object[] parameters)
    //{
    //   // photonView.RPC(nameof(RPC_MethodRunner), rpcTarget, parameters);
    //    actionToRunInRPC = action;
    //}

    //[PunRPC]
    private void RPC_MethodRunner()
    {
        actionToRunInRPC?.Invoke();
        actionToRunInRPC = null;
    }

    public void StartDiceBlinking(DiceColor diceColor)
    {
        diceAnimationController.HideDice();
        
        if (DataManager.Instance.GameType != GameType.Multiplayer)
        {
            //Debug.Log($"ActiveDiceColor: {diceColor}, OwnColor: {DataManager.Instance.OwnDiceColor}");
            foreach (var rollingDice in manageRollingDice)
            {
                if (rollingDice.SelfDiceColor == diceColor)
                {
                    rollingDice.gameObject.SetActive(true);
                    rollingDice.ActiveDiceAndStartBlinking();
                }
                else
                {
                    rollingDice.StopBlinkingAnimation();
                }
            }
            
            return;
        }
        
        foreach (var rollingDice in manageRollingDice)
        {
            if(!currentBoardsPlayers.Contains(rollingDice.SelfDiceColor))
                continue;
            
            if (rollingDice.SelfDiceColor == diceColor && diceColor == DataManager.Instance.OwnDiceColor)
            {
                rollingDice.gameObject.SetActive(true);
                rollingDice.ActiveDiceAndStartBlinking();
            }
            else if(rollingDice.SelfDiceColor == diceColor)
            {
                rollingDice.gameObject.SetActive(true);
                rollingDice.FirstTurn();
            }
            else
            {
                rollingDice.gameObject.SetActive(true);
                rollingDice.StopBlinkingAnimation();
            }
        }
    }
    
    public void AddPathPoint(PathPoint pathPoint_)
    {
        playerOnPathPointsList.Add(pathPoint_);
    }

    public void RemovePathPoint(PathPoint pathPoint_)
    {
        if (playerOnPathPointsList.Contains(pathPoint_))
        {
            playerOnPathPointsList.Remove(pathPoint_);
        }
        else
        {
            Debug.LogWarning("Path Point is not Found to be removed.");
        }
    }
    
   // [PunRPC]
    public void RollingDiceManager()
    {
        if (transferDice)
        {
            if (numOfStepsToMove != 6)
            {
                ShiftDiceMechanism();
            }

            canDiceRoll = true;
            //Debug.Log($"RollingDiceManager, TransferDice: {transferDice}, numOfStepsToMove: {numOfStepsToMove}, CanDiceRoll: {canDiceRoll}, SelfDice: {selfDice}");
        }
        else if (selfDice)
        {
            selfDice = false;
            canDiceRoll = true;
            SelfRoll();
            
            //Debug.Log($"RollingDiceManager, SelfDice: {selfDice}, numOfStepsToMove: {numOfStepsToMove}, CanDiceRoll: {canDiceRoll}, TransferDice: {transferDice}");
        }
    }

    public void SelfRoll()
    {
        if (TotalPlayerCanPlay == 1 && rolledDice == manageRollingDice[2])
        {
            Invoke(nameof(Rolled), 0.6f);
        }
    }

    private void Rolled()
    {
        manageRollingDice[2].MouseRoll();
    }

    private DiceColor ShiftDice()
    {
        int nextDice;
        
        if (TotalPlayerCanPlay == 3)
        {
            if (rolledDice.SelfDiceColor == DiceColor.Yellow)
                nextDice = 0;
            else
                nextDice = (int) rolledDice.SelfDiceColor + 1;
            
            nextDice = GetValidTurn(nextDice);

        }
        else
        {
            if (rolledDice.SelfDiceColor == DiceColor.Green)
                nextDice = 0;
            else
                nextDice = (int) rolledDice.SelfDiceColor + 1;
            
            nextDice = GetValidTurn(nextDice);
        }
        
        return (DiceColor) nextDice;
    }

    private void ShiftDiceMechanism()
    {
        int nextDice;

        if (TotalPlayerCanPlay == 1)
        {
            switch (rolledDice.SelfDiceColor)
            {
                case DiceColor.Red:
                    UIManager.Instance.ChangeTurn(DiceColor.Yellow);
                    manageRollingDice[2].MouseRoll();
                    break;
                
                case DiceColor.Yellow:
                    UIManager.Instance.ChangeTurn(DiceColor.Red);
                    break;
            }
        }
        //else if (TotalPlayerCanPlay == 2 && DataManager.Instance.GameType != GameType.Multiplayer)
        else if (TotalPlayerCanPlay == 2)
        {
            switch (rolledDice.SelfDiceColor)
            {
                case DiceColor.Red:
                    UIManager.Instance.ChangeTurn(DiceColor.Yellow);
                    break;
                
                case DiceColor.Yellow:
                    UIManager.Instance.ChangeTurn(DiceColor.Red);
                    break;
            }
        }
        else if (TotalPlayerCanPlay == 3)
        {
            if (rolledDice.SelfDiceColor == DiceColor.Yellow)
                nextDice = 0;
            else
                nextDice = (int) rolledDice.SelfDiceColor + 1;
            
            if(DataManager.Instance.GameType == GameType.Multiplayer)
                nextDice = GetValidTurn(nextDice);
            
            UIManager.Instance.ChangeTurn((DiceColor)nextDice);
        }
        else
        {
            if (rolledDice.SelfDiceColor == DiceColor.Green)
                nextDice = 0;
            else
                nextDice = (int) rolledDice.SelfDiceColor + 1;
            
            if(DataManager.Instance.GameType == GameType.Multiplayer)
                nextDice = GetValidTurn(nextDice);
            
            UIManager.Instance.ChangeTurn((DiceColor)nextDice);
        }
    }

    private int GetValidTurn(int inputTurn)
    {
        DiceColor color = (DiceColor)inputTurn;

        if (currentBoardsPlayers.Contains(color))
            return (int) color;

        while (!currentBoardsPlayers.Contains(color))
        {
            inputTurn += 1;
            if(inputTurn >= currentBoardsPlayers.Count)
                inputTurn = 0;
            
            color = (DiceColor)inputTurn;
        }
        
        return (int)color;
    }
    
    public void UpdatePlayerMovement(int activeDiceIndex)
    {
        // Reset all movement permissions to false
        blueCanMove = redCanMove = greenCanMove = yellowCanMove = false;

        // Enable movement based on active dice
        switch (activeDiceIndex)
        {
            case 0: // Blue Dice
                blueCanMove = true;
                break;
            case 1: // Red Dice
                redCanMove = true;
                break;
            case 2: // Green Dice
                greenCanMove = true;
                break;
            case 3: // Yellow Dice
                yellowCanMove = true;
                break;
        }
    }
    int PassOut(int i)
    {
        if (i == 0) 
        { 
            if (blueCompletePlayer == 4) 
            { 
                return (i + 1);
            } 
        }
        else if (i == 1) 
        { 
            if (redCompletePlayer == 4) 
            {
                return (i + 1);
            }
        }
        else if (i == 2) 
        { 
            if (greenCompletePlayer == 4) 
            {
                return (i + 1);
            }
        }
        else if (i == 3) 
        { 
            if (yellowCompletePlayer == 4) 
            {
                return (i + 1);
            }
        }

        return i;
    }

    public bool IsMyTurn()
    {
        return DataManager.Instance.OwnDiceColor == DataManager.Instance.ActiveDiceColor;
    }

    // Method to check if a player has won the game
    public void CheckForWinner()
    {
        if (blueCompletePlayer == 4)
        {
            if(DataManager.Instance.GameType == GameType.Single || DataManager.Instance.GameType == GameType.LocalMultiplayer)
                EndGame(DiceColor.Blue);
            else
                RaiseGameOverCustomEvent(DiceColor.Blue);
        }
        else if (redCompletePlayer == 4)
        {
            if(DataManager.Instance.GameType == GameType.Single)
                EndGame(DiceColor.Red);
            else
                RaiseGameOverCustomEvent (DiceColor.Red);
        }
        else if (greenCompletePlayer == 4)
        {
            if (DataManager.Instance.GameType == GameType.Single)
                EndGame(DiceColor.Green);
            else
                RaiseGameOverCustomEvent(DiceColor.Green);
        }
        else if (yellowCompletePlayer == 4)
        {
            if (DataManager.Instance.GameType == GameType.Single|| DataManager.Instance.GameType == GameType.LocalMultiplayer)
                EndGame(DiceColor.Yellow);
            else
                RaiseGameOverCustomEvent(DiceColor.Yellow);
        }
    }

    public void RaiseGameOverCustomEvent(DiceColor winingDiceColor)
    {
        // Event data you want to send (it can be any serializable object)
        object content = winingDiceColor;

        // Define who will receive the event (all players in the room)
        //RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        //{
        //    Receivers = ReceiverGroup.All // You can use ReceiverGroup.MasterClient, Others, etc.
        //};

        // Send options (whether to send reliably or not)
        //SendOptions sendOptions = new SendOptions
        //{
        //    Reliability = true // If true, event is sent reliably
        //};

        //// Raise the event
        //PhotonNetwork.RaiseEvent(GAME_OVER_EVENT_CODE, content, raiseEventOptions, sendOptions);
    }

    //private void OnPhotonGameEventReceived(EventData photonEvent)
    //{
    //    // Check if the event code matches the custom event
    //    if (photonEvent.Code == GAME_OVER_EVENT_CODE)
    //    {
    //        // Get the data from the event (it will match the type of data you sent)
    //        object eventData = photonEvent.CustomData;

    //        // Cast it to a string (in this case, because we sent a string)
    //        DiceColor winingDiceColor = (DiceColor) eventData;

    //        // Handle the received event (you can do anything here, for example, log the message)
    //        Debug.Log($"Received custom event message: {winingDiceColor}");

    //        EndGame(winingDiceColor);
    //    }
    //    else if(photonEvent.Code == CURRENT_TURN_EVENT_CODE)
    //    {
    //        // Get the data from the event (it will match the type of data you sent)
    //        object eventData = photonEvent.CustomData;

    //        // Cast it to a string (in this case, because we sent a string)
    //        DiceColor newActiveDiceColor = (DiceColor) eventData;

    //        // Handle the received event (you can do anything here, for example, log the message)
    //        Debug.LogError($"Received custom event message of Current Turn: {newActiveDiceColor}");
    //        activeDiceColor = newActiveDiceColor;
    //        DataManager.Instance.SetActiveDiceColor(newActiveDiceColor);
    //        UIManager.Instance.ChangeTurnForMultiplayer(newActiveDiceColor);
    //    }
    //}

    public void RaiseTurnChangeEvent(DiceColor diceColor)
    {
        // Event data you want to send (it can be any serializable object)
        object content = diceColor;

        // Define who will receive the event (all players in the room)
        //RaiseEventOptions raiseEventOptions = new RaiseEventOptions
        //{
        //    Receivers = ReceiverGroup.All // You can use ReceiverGroup.MasterClient, Others, etc.
        //};

        // Send options (whether to send reliably or not)
        //SendOptions sendOptions = new SendOptions
        //{
        //    Reliability = true // If true, event is sent reliably
        //};

        //// Raise the event
        //Debug.LogError($"Raising Turn Change: {diceColor}, ViewID: {photonView.ViewID}");
        //PhotonNetwork.RaiseEvent(CURRENT_TURN_EVENT_CODE, content, raiseEventOptions, sendOptions);
    }

    // Method to end the game and declare the winner
    public void EndGame(DiceColor winningColor)
    {
        canMove = false;
        canDiceRoll = false;
        Debug.Log(winningColor + " has won the game!");

        // Optional: Play a victory sound or show a victory screen
        //audioSource.PlayOneShot(victorySound);

        // Optional: Transition to a victory screen, restart the game, or return to the main menu
        //ShowVictoryScreen(winningColor);
        // SceneManager.LoadScene("MainMenu");
        DataManager.Instance.SetCurrentGameState(GameState.Finished);
        UIManager.Instance.OnGameFinished(winningColor);


        //if(DataManager.Instance.CurrentRoomType == RoomType.AI || DataManager.Instance.CurrentRoomType == RoomType.Free)
        //    UIManager.Instance.OnGameFinished(winningColor);
        //else
        //{
        //    endgame.SetActive(true);
        //    wontext.text = winningColor + " has won the game!";
        //}
    }

    // Example method where you should call CheckForWinner after a token reaches home
    public void MovePlayerToken(PlayerPiece player)
    {
        // Your existing logic for moving the player token

        // After the move, check if the player has won
        CheckForWinner();
    }

    public void RestartGamne()
    {
       UIManager.Instance.BackToMainMenu();
    }

    #region Debug
    [Space(30), Header("Debug Region")]
    [SerializeField] private GameObject debugSection;
    [SerializeField] private Toggle fixNumberToggle;
    [SerializeField] private Slider debugSlider;
    [SerializeField] private TextMeshProUGUI debugSliderValueTxt;
    [SerializeField] private TextMeshProUGUI fixDiceCountTxt;
    [SerializeField] private TextMeshProUGUI fixDiceCostTxt;
    [SerializeField] private int debugDesiredNum;
    [SerializeField] private bool isDebuggingOn;
    [SerializeField] private bool showDebugOption;
    [SerializeField] private int fixDiceCostPercentage = 5;
    private int fixNumberUseCount = 0;
    private int fixNumberUseCost = 0;
    public bool IsDebuggingOn() => isDebuggingOn;
    public bool CanUseFixDice => (fixNumberUseCount < DataManager.Instance.MaxNumberOfFixTurns && fixNumberUseCost <= DataManager.Instance.Coins);
    
    private void Debug_CheckAndSetIfAnyDebugIsOnOrNot()
    {
        if (DataManager.Instance.GameType == GameType.Multiplayer && DataManager.Instance.CurrentUserType == UserType.APP)
        {
            debugSection.SetActive(true);
            InitializeDebugSlider();
            return;
        }
        
        debugSection.SetActive(false);
    }

    private void InitializeDebugSlider()
    {
        debugSlider.minValue = 1;
        debugSlider.maxValue = 6;
        debugSlider.value = debugSlider.minValue;
        fixNumberToggle.isOn = true;
        
        fixNumberToggle.isOn = false;
        fixDiceCostPercentage = Helper.CalculatePercentage(DataManager.Instance.CurrentEntryFee, fixDiceCostPercentage);
        fixDiceCostTxt.SetText($"{Helper.GetReadableNumber(fixDiceCostPercentage)} coins cost per use.");

        //OnDebugToggleValueChanged(false);
        
        UpdateDebugDesiredNumAndSliderTxt();
    }
    
    public int Debug_GetDesiredNum() 
    {
        return debugDesiredNum >= 6 ? 5 : debugDesiredNum;
    }

    public void OnDebugToggleValueChanged(bool isOn)
    {
        isOn = isOn && fixDiceCostPercentage <= DataManager.Instance.Coins;
        
        isDebuggingOn = isOn;
        debugSlider.gameObject.SetActive(isDebuggingOn);
        fixDiceCountTxt.SetText($"Use Desire Num {fixNumberUseCount}/{DataManager.Instance.MaxNumberOfFixTurns}");
        UpdateDebugDesiredNumAndSliderTxt();
    }

    public void OnDebugSliderValueChange(float value)
    {
        UpdateDebugDesiredNumAndSliderTxt();
    }

    private void UpdateDebugDesiredNumAndSliderTxt()
    {
        debugDesiredNum = (int)(debugSlider.value - 1);
        debugSliderValueTxt.SetText($"{(int)debugSlider.value}");
    }

    public void UseFixedDice()
    {
        fixNumberUseCount += 1;
        
        fixNumberToggle.isOn = false;
        fixNumberToggle.interactable = CanUseFixDice;
        OnDebugToggleValueChanged(false);
        fixDiceCountTxt.SetText($"Use Desire Num {fixNumberUseCount}/{DataManager.Instance.MaxNumberOfFixTurns}");
        
        APIHandler.Instance.FixDiceCoinDeduction((res =>
        {
            try
            {
                WishCoinResponse response = JsonUtility.FromJson<WishCoinResponse>(res);
                if (response != null)
                {
                    DataManager.Instance.UpdateAppUserCoin(fixDiceCostPercentage * -1);
                    UIManager.Instance.menuManager.UpdateCoinText();
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                DataManager.Instance.UpdateAppUserCoin(fixDiceCostPercentage * -1);
                UIManager.Instance.menuManager.UpdateCoinText();
                APIHandler.Instance.GiveCoinToUser(DataManager.Instance.CurrentUser.id.ToString(),
                    DataManager.Instance.Coins);
            }
            finally
            {
                fixNumberToggle.isOn = false;
                fixNumberToggle.interactable = CanUseFixDice;
            }
        } ));
    }
    #endregion Debug

}