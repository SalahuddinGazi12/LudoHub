using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Photon.Pun;
using  UnityEngine.UI;

public class RollingDice : MonoBehaviour
{
    [SerializeField] int numberGot;
    [SerializeField] GameObject rollingDiceAnim;
    [SerializeField] GameObject profileGameObject;
    [SerializeField] GameObject castDiceGameObject;
    [SerializeField] SpriteRenderer numberedSpriteHolder;
    [SerializeField] Sprite[] numberedSprites;
    [SerializeField] private GameObject availableTurnsCanIgnoreParent;
    [SerializeField] private Image powerBarFillImg;
    [SerializeField] private float fillSpeed;
    [SerializeField] private GameObject blinkingSprite;
    [SerializeField] private Slider turnTimerSlider;
    [SerializeField] private Image selfAvatarSprite;
    [SerializeField] private Transform normalRotAndPos;
    [SerializeField] private Transform sideRotAndPos;
    [SerializeField] private CircleCollider2D selfCollider;
    [SerializeField] private Transform parentTransform;
    [SerializeField] private Transform z_90RotationTransform;
    [SerializeField] private Transform z_180RotationTransform;
    [SerializeField] private Transform y_180RotationTransform;
    private float blinkingWaitTime = 0.3f;
    Coroutine generateRandNumOnDice_Coroutine;
    public int outPieces;
    public PathObjectsParent pathParent;
    PlayerPiece[] currentPlayerPieces;
    PathPoint[] pathPointToMoveOn_;
    Coroutine moveSteps_Coroutine;
    PlayerPiece outPlayerPiece;

    public Dice DiceSound;
    public DiceColor SelfDiceColor => diceColor;
    public DiceColor diceColor;
    [SerializeField] private PhotonView photonView;
    private bool onMouseDown;
    private Coroutine powerBarFillCoroutine;
    private Coroutine blinkingCoroutine;
    private Coroutine timerCoroutine;
    private int timeSinceTurnTimerStarted;
    [SerializeField] private int turnIgnored;
    private bool validRoll;
    private int sixInARow;
    private bool isIdol;
    
    private void Awake()
    {
        pathParent = FindAnyObjectByType<PathObjectsParent>();

        diceColor = GetDiceColor(gameObject);
        
        InitializeTurnSlider();
    }

    public void SetSelfAvatar(Sprite avatarSprite)
    {
        selfAvatarSprite.sprite = avatarSprite;
    }
    
    public void SetUpDiceRotation()
    {
        if (DataManager.Instance.GameType != GameType.Multiplayer)
        {
            switch (SelfDiceColor)
            {
                case DiceColor.Blue:
                    transform.localEulerAngles = new Vector3(180f, 0f, 0f);
                    break;
                case DiceColor.Yellow:
                    transform.localEulerAngles = new Vector3(0, 0, 180);
                    break;

                case DiceColor.Green:
                    transform.localEulerAngles = new Vector3(0f, 180f, 0f);
                    break;

                default:
                    transform.localEulerAngles = Vector3.zero;
                    break;
            }
            return;
        }

        switch (DataManager.Instance.LocalRotation)
        {
            case LocalRotation.Y180:
                parentTransform.localEulerAngles = new Vector3(0f, -180f, 0f);
                transform.localEulerAngles = y_180RotationTransform.localEulerAngles;
                transform.localPosition = y_180RotationTransform.localPosition;
                break;

            case LocalRotation.Z90:
                parentTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
                transform.localEulerAngles = z_90RotationTransform.localEulerAngles;
                transform.localPosition = z_90RotationTransform.localPosition;
                break;

            case LocalRotation.Z180:
                parentTransform.localEulerAngles = new Vector3(0f, 0f, -180f);
                transform.localEulerAngles = z_180RotationTransform.localEulerAngles;
                transform.localPosition = z_180RotationTransform.localPosition;
                break;
        }
    }

    private DiceColor GetDiceColor(GameObject targetGameObject)
    {
        // Loop through the enum values
        foreach (DiceColor color in Enum.GetValues(typeof(DiceColor)))
        {
            if (targetGameObject.name.Contains(color.ToString()))
            {
                return color;
            }
        }

        // Return Unknown if no match is found
        return DiceColor.Unknown;
    }

    public void OnMouseDown()
    {
        if(!GameManager.Instance.canDiceRoll)
            return;

        validRoll = true;
        if(DataManager.Instance.GameType == GameType.Multiplayer)
        {
            if (!GameManager.Instance.IsMyTurn())
                return;
        }
        
        //GenerateNumberAndRollTheDice();
        StartPowerBarFillEffect();
    }
    
    public void OnMouseUp()
    {
        if(!validRoll)
            return;

        validRoll = false;
        
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            if (!GameManager.Instance.IsMyTurn())
                return;
        }

        //StopTimer();
        StopPowerBarFillEffect();
    }

    public void FirstTurn()
    {
        ResetDice();
    }
    
    private void DiceNumberSpriteHandlerForRpc()
    {
        turnTimerSlider.gameObject.SetActive(DataManager.Instance.GameType == GameType.Multiplayer);
        
        if (DataManager.Instance.OwnDiceColor == DataManager.Instance.ActiveDiceColor)
        {
            castDiceGameObject.SetActive(true);
            selfCollider.enabled = true;
            ResetAndShowPowerBar();
            StartTurnTimerInvokation();
            return;
        }
        
        castDiceGameObject.SetActive(false);
        powerBarFillImg.transform.parent.gameObject.SetActive(false);
        selfCollider.enabled = false;
    }

    private void ResetDice()
    {
        isIdol = false;
        castDiceGameObject.SetActive(false);
        powerBarFillImg.transform.parent.gameObject.SetActive(false);
        powerBarFillImg.fillAmount = 0;
        turnTimerSlider.value = 0;
        turnTimerSlider.gameObject.SetActive(DataManager.Instance.GameType == GameType.Multiplayer);
        
        availableTurnsCanIgnoreParent.SetActive(DataManager.Instance.GameType == GameType.Multiplayer && DataManager.Instance.CurrentUserType == UserType.APP);
        selfCollider.enabled = false;
    }

    public void ActiveDiceAndStartBlinking()
    {
        StartBlinkingAnimation();

        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            isIdol = true;
            InitializeTurnSlider();
            TurnChangeHandler();
        }
        else
        {
            castDiceGameObject.SetActive(true);
            selfCollider.enabled = true;
            turnTimerSlider.gameObject.SetActive(false);
        }
        
    }

    private void TurnChangeHandler()
    {
        photonView.RPC(nameof(TurnChangeHandlerRPC), RpcTarget.Others);
        
        TurnChangeHandlerRPC();
    }

    [PunRPC]
    private void TurnChangeHandlerRPC()
    {
        DiceNumberSpriteHandlerForRpc();
    }

    private void StartTurnTimerInvokation()
    {
        turnTimerSlider.value = turnTimerSlider.minValue;
        timeSinceTurnTimerStarted = (int) turnTimerSlider.minValue;
        turnTimerSlider.gameObject.SetActive(DataManager.Instance.GameType == GameType.Multiplayer);

        timerCoroutine ??= StartCoroutine(TimerCoroutine());
       // StartTurnTimer();
    }
    
    private void StopTimerCoroutine()
    {
        if (timerCoroutine == null) return;
        
        StopCoroutine(timerCoroutine);
        timerCoroutine = null;
    }

    private void StopTimer()
    {
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            photonView.RPC(nameof(StopTimerRPC), RpcTarget.AllBuffered);
        }
        else
        {
            StopTimerRPC();
        }
    }

    [PunRPC]
    private void StopTimerRPC()
    {
        StopTimerCoroutine();
        
        turnTimerSlider.value = turnTimerSlider.minValue;
        turnTimerSlider.gameObject.SetActive(DataManager.Instance.GameType == GameType.Multiplayer);
    }

    private IEnumerator TimerCoroutine()
    {
        var waitForTime = new WaitForSeconds(1);

        while (timeSinceTurnTimerStarted < turnTimerSlider.maxValue && DataManager.Instance.CurrentGameState == GameState.Play )
        { 
            photonView.RPC(nameof(UpdateTurnSliderValue), RpcTarget.Others, timeSinceTurnTimerStarted);
            UpdateTurnSliderValue(timeSinceTurnTimerStarted);
            yield return waitForTime;
            timeSinceTurnTimerStarted += 1;
        }
        
        Debug.Log("Turn Time is over!");
        timeSinceTurnTimerStarted = (int) turnTimerSlider.minValue;
        timerCoroutine = null;

        ShiftDice();
    }

    public void ShiftDice()
    {
        if (isIdol)
        {
            turnIgnored++;
            photonView.RPC(nameof(ReducedAvailableTurnIndicatorsRPC), RpcTarget.Others, turnIgnored);
            ReducedAvailableTurnIndicators();
        }
        
        
        if (turnIgnored < DataManager.Instance.MaxTurnCanIgnore)
        {
            if(PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount > 1)
                //photonView.RPC(nameof(RollingDiceManagerRPC), RpcTarget.AllBuffered);
                RollingDiceManagerRPC();
            
            return;
        }

        UIManager.Instance.OnGameFinished(DiceColor.Unknown);
        DisconnectAfterCertainTime();
        //Invoke(nameof(DisconnectAfterCertainTime), 3);
    }

    
    private void ReducedAvailableTurnIndicators()
    {
        if (availableTurnsCanIgnoreParent.transform.childCount == 0 || availableTurnsCanIgnoreParent.transform.childCount < turnIgnored) return;
        
        for (int i = 0; i < turnIgnored; i++)
        {
            availableTurnsCanIgnoreParent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
    
    [PunRPC]
    private void ReducedAvailableTurnIndicatorsRPC(int currentlyIgnoredTurns)
    {
        if (availableTurnsCanIgnoreParent.transform.childCount == 0 || availableTurnsCanIgnoreParent.transform.childCount < turnIgnored) return;
        
        for (int i = 0; i < currentlyIgnoredTurns; i++)
        {
            availableTurnsCanIgnoreParent.transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void DisconnectAfterCertainTime()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect();
    }

    [PunRPC]
    private void UpdateTurnSliderValue(int value)
    {
        turnTimerSlider.value = value;
    }

    private void InitializeTurnSlider()
    {
        turnTimerSlider.minValue = 0;
        turnTimerSlider.maxValue = DataManager.Instance.MaxTurnTime;
        turnTimerSlider.value = turnTimerSlider.minValue;
        
        turnTimerSlider.gameObject.SetActive(DataManager.Instance.GameType == GameType.Multiplayer);
        availableTurnsCanIgnoreParent.SetActive(DataManager.Instance.GameType == GameType.Multiplayer && DataManager.Instance.CurrentUserType == UserType.APP);
    }
    
    public void DisableAndStopBlinking()
    {
        StopBlinkingAnimation();
        gameObject.SetActive(false);
    }

    public void StartBlinkingAnimation()
    {
        //Debug.Log($"Active Dice: {DataManager.Instance.ActiveDiceColor}, OwnDiceColor: {DataManager.Instance.OwnDiceColor}");
        blinkingCoroutine ??= StartCoroutine(BlinkingAnimation());
      //  GameManager.Instance.StartStopBlinking(SelfDiceColor, true);
    }

    public void StopBlinkingAnimation()
    {
        if (blinkingCoroutine != null)
        {
            StopCoroutine(blinkingCoroutine);
            blinkingCoroutine = null;
            blinkingSprite.gameObject.SetActive(false);
        }

        sixInARow = 0;
        StopTimerRPC();
        
        ResetDice();

        if (DataManager.Instance.GameType != GameType.LocalMultiplayer) return;
        
        gameObject.SetActive(true);
        selfAvatarSprite.gameObject.SetActive(true);

    }

    private IEnumerator BlinkingAnimation()
    {
        var waitTime = new WaitForSeconds(blinkingWaitTime);
        
        while (gameObject.activeSelf && DataManager.Instance.CurrentGameState != GameState.Finished)
        {
            blinkingSprite.gameObject.SetActive(true);
            yield return waitTime;
            blinkingSprite.gameObject.SetActive(false);
            yield return waitTime;
        }
    }

    private void ResetAndShowPowerBar()
    {
        powerBarFillImg.fillAmount = 0;
        powerBarFillImg.transform.parent.gameObject.SetActive(true);
    }
    private void HidePowerBar() => powerBarFillImg.transform.parent.gameObject.SetActive(false);

    private void StartPowerBarFillEffect()
    {
        onMouseDown = true;
        StartPowerBarFillEffectRPC();
    }
    
    private void StartPowerBarFillEffectRPC()
    {
        if (powerBarFillCoroutine != null)
        {
            return;
        }

        onMouseDown = true;
        powerBarFillCoroutine = StartCoroutine(PowerBarFillEffect());
    }


    private void StopPowerBarFillEffect()
    {
        onMouseDown = false;
        
        int randomNum = GetRandomNumber();
        
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            photonView.RPC(nameof(StopPowerBarFillEffectRPC), RpcTarget.Others, randomNum);
        }

        StopPowerBarFillEffectRPC(randomNum);
    }
    
    [PunRPC]
    private void StopPowerBarFillEffectRPC(int randomNum)
    {
        if (powerBarFillCoroutine != null)
        {
            StopCoroutine(powerBarFillCoroutine);
        }
        
        onMouseDown = false;
        powerBarFillCoroutine = null;
        powerBarFillImg.fillAmount = 0;
        
        GenerateNumberAndRollTheDice(randomNum);
    }

    private IEnumerator PowerBarFillEffect()
    {
        float directionIndicator = 0;

        ResetAndShowPowerBar();

        while (onMouseDown)
        {
            if (directionIndicator == 0)
            {
                powerBarFillImg.fillAmount += fillSpeed * Time.deltaTime;
                if (powerBarFillImg.fillAmount >= 1)
                    directionIndicator = 1;
            }
            else
            {
                powerBarFillImg.fillAmount -= fillSpeed * Time.deltaTime;
                if (powerBarFillImg.fillAmount <= 0)
                    directionIndicator = 0;
            }

            yield return null;
        }
    }

    private int GetRandomNumber()
    {
        int randomNum = Random.Range(0, 6);

        if (DataManager.Instance.GameType != GameType.Multiplayer || !GameManager.Instance.IsDebuggingOn() || !GameManager.Instance.CanUseFixDice)
            return CheckIfSixComesThreeTimesInRow(randomNum) ? Random.Range(0, 6) : randomNum;

        randomNum = GameManager.Instance.Debug_GetDesiredNum();
        randomNum = (randomNum < 0) ? Random.Range(0, 6) : randomNum;
        GameManager.Instance.UseFixedDice();
        
        //return CheckIfSixComesThreeTimesInRow(randomNum)? Random.Range(0, 6) : randomNum;;
        return randomNum;
    }


    public void MouseRoll()
    {
        int number = GetRandomNumber();
        
        GenerateNumberAndRollTheDice(number);
    }

    private bool CheckIfSixComesThreeTimesInRow(int num)
    {
        sixInARow = (num >= 5)? sixInARow + 1 : sixInARow;
        return sixInARow > 2;
    }

    private void GenerateNumberAndRollTheDice(int generatedNum)
    {
        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            //photonView.RPC(nameof(FakeRollTheDice), RpcTarget.Others, generatedNum);
            if (DataManager.Instance.OwnDiceColor == SelfDiceColor)
            {
                isIdol = false;
                RollTheDice(generatedNum);
            }
            else
            {
                FakeRollTheDice(generatedNum);
            }
            
            return;
        }

        RollTheDice(generatedNum);
    }
    
    private void FakeRollTheDice(int generatedNum)
    {
        StartCoroutine(FakeRollTheDiceCoroutine_Enum(generatedNum));
    }
    
    public void RollTheDice(int generatedNum)
    {
        generateRandNumOnDice_Coroutine = StartCoroutine(GenerateRandomNumberOnDice_Enum(generatedNum));
    }

    IEnumerator GenerateRandomNumberOnDice_Enum(int generatedNum)
    {
        DiceSound.PlaySound();
        GameManager.Instance.transferDice = false;
        
        yield return new WaitForEndOfFrame();

        if (!GameManager.Instance.canDiceRoll) yield break;
        
        GameManager.Instance.canDiceRoll = false;

        numberGot = generatedNum;
        GameManager.Instance.diceAnimationController.AnimateAndShowGotNumber(generatedNum, 0.5f);
        yield return new WaitForSeconds(0.7f);
        
        numberGot += 1;

        GameManager.Instance.numOfStepsToMove = numberGot;
        GameManager.Instance.rolledDice = this;

        yield return new WaitForEndOfFrame();


        int numGot = GameManager.Instance.numOfStepsToMove;

        if (PlayerCannotMove())
        {
            //Debug.Log($"PlayerCannotMove, NumberGot: {numberGot}");
            
            yield return new WaitForSeconds(0.5f);

            GameManager.Instance.transferDice = numGot != 6;
            GameManager.Instance.selfDice = numGot == 6;
        }
        else
        {
            switch (GameManager.Instance.rolledDice.SelfDiceColor)
            {
                case DiceColor.Red:
                    outPieces = GameManager.Instance.redOutPlayers;
                    break;
                
                case DiceColor.Blue:
                    outPieces = GameManager.Instance.blueOutPlayers;
                    break;
                
                case DiceColor.Yellow:
                    outPieces = GameManager.Instance.yellowOutPlayers;
                    break;
                
                case DiceColor.Green:
                    outPieces = GameManager.Instance.greenOutPlayers;
                    break;
            }

            if (outPieces == 0 && numGot != 6)
            {
                //Debug.Log("Transferring Dice");
                yield return new WaitForSeconds(0.5f);
                GameManager.Instance.transferDice = true;
            }
            else
            {
                if (outPieces == 0 && numGot == 6)
                {
                    //Debug.Log("Outing opening piece");
                    MakePlayerReadyToMove(0);
                }
                else if (outPieces == 1 && numGot != 6 && GameManager.Instance.canMove)
                {
                    int playerPiecePosition = CheckOutPlayer();

                    if (playerPiecePosition >= 0)
                    {
                        MoveSteps(playerPiecePosition);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.5f);

                        GameManager.Instance.transferDice = numberGot != 6;
                        GameManager.Instance.selfDice = numberGot == 6;
                    }
                }
                //For AI
                else if (GameManager.Instance.TotalPlayerCanPlay == 1 && GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[2])
                {
                    //Debug.Log("AI");

                    if (numberGot == 6 && outPieces < 4)
                    {
                        MakePlayerReadyToMove(OutPlayerToMove());
                    }
                    else
                    {
                        int playerPiecePosition = CheckOutPlayer();
                        if (playerPiecePosition >= 0)
                        {
                            // GameManager.Instance.canMove = false;
                            // moveSteps_Coroutine = StartCoroutine(MoveSteps_Enum(playerPiecePosition));
                            MoveSteps(playerPiecePosition);
                        }
                        else
                        {
                            yield return new WaitForSeconds(0.5f);
                            if (numGot != 6)
                            {
                                GameManager.Instance.transferDice = true;
                            }
                            else
                            {
                                GameManager.Instance.selfDice = true;
                            }

                            GameManager.Instance.transferDice = numGot != 6;
                            GameManager.Instance.selfDice = numGot == 6;

                        }
                    }
                }
                else
                {
                    if (CheckOutPlayer() < 0)
                    {
                        yield return new WaitForSeconds(0.5f);
                        if (numGot != 6)
                        {
                            GameManager.Instance.transferDice = true;
                        }
                        else
                        {
                            GameManager.Instance.selfDice = true;
                        }
                        //Debug.Log($"CheckOutPlayer() < 0, NumberGot: {numberGot}");
                    }
                }
            }  
        }
        
        RollingDiceManager();
    }
    
    IEnumerator FakeRollTheDiceCoroutine_Enum(int generatedNum)
    {
        DiceSound.PlaySound();
        GameManager.Instance.transferDice = false;
        
        yield return new WaitForEndOfFrame();

        numberGot = generatedNum;
        GameManager.Instance.diceAnimationController.AnimateAndShowGotNumber(generatedNum, 0.5f);
        yield return new WaitForSeconds(0.7f);
        
        numberGot += 1;

        GameManager.Instance.numOfStepsToMove = numberGot;
        GameManager.Instance.rolledDice = this;

        yield return new WaitForEndOfFrame();


        int numGot = GameManager.Instance.numOfStepsToMove;

        if (PlayerCannotMove())
        {
            //Debug.Log($"PlayerCannotMove, NumberGot: {numberGot}");
            
            yield return new WaitForSeconds(0.5f);
            
            if (numGot != 6)
            {
                GameManager.Instance.transferDice = true;
            }
            else
            {
                GameManager.Instance.selfDice = true;
            }
        }
        else
        {
            //Debug.Log("PlayerCanMove Else", gameObject);

            if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[0])
            {
                outPieces = GameManager.Instance.redOutPlayers;
            }
            else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[1])
            {
                outPieces = GameManager.Instance.blueOutPlayers;
            }
            else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[2])
            {
                outPieces = GameManager.Instance.yellowOutPlayers;
            }
            else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[3])
            {
                outPieces = GameManager.Instance.greenOutPlayers;
            }

            if (outPieces == 0 && numGot != 6)
            {
                //Debug.Log("Transferring Dice");
                yield return new WaitForSeconds(0.5f);
                GameManager.Instance.transferDice = true;
            }
            else
            {
                if (outPieces == 0 && numGot == 6)
                {
                    //Debug.Log("Outing opening piece");
                    MakePlayerReadyToMove(0);
                }
                else if (outPieces == 1 && numGot != 6 && GameManager.Instance.canMove)
                {
                    int playerPiecePosition = CheckOutPlayer();

                    if (playerPiecePosition >= 0)
                    {
                        //Debug.Log($"playerPiecePosition >= 0, PlayerPiecePosition: {playerPiecePosition}");
                        // GameManager.Instance.canMove = false;
                        // moveSteps_Coroutine = StartCoroutine(MoveSteps_Enum(playerPiecePosition));

                        MoveSteps(playerPiecePosition);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.5f);

                        if (numberGot != 6)
                        {
                            GameManager.Instance.transferDice = true;
                        }
                        else
                        {
                            //Debug.Log($"Else and numberGot is not 6, NumberGot: {numberGot}");
                            GameManager.Instance.selfDice = true;
                        }
                    }
                }
                //For AI
                else if (GameManager.Instance.TotalPlayerCanPlay == 1 && GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[2])
                {
                    //Debug.Log("AI");

                    if (numberGot == 6 && outPieces < 4)
                    {
                        MakePlayerReadyToMove(OutPlayerToMove());
                    }
                    else
                    {
                        int playerPiecePosition = CheckOutPlayer();
                        if (playerPiecePosition >= 0)
                        {
                            // GameManager.Instance.canMove = false;
                            // moveSteps_Coroutine = StartCoroutine(MoveSteps_Enum(playerPiecePosition));
                            MoveSteps(playerPiecePosition);
                        }
                        else
                        {
                            yield return new WaitForSeconds(0.5f);
                            if (numGot != 6)
                            {
                                GameManager.Instance.transferDice = true;
                            }
                            else
                            {
                                GameManager.Instance.selfDice = true;
                            }
                        }
                    }
                }
                else
                {
                    if (CheckOutPlayer() < 0)
                    {
                        yield return new WaitForSeconds(0.5f);
                        GameManager.Instance.transferDice = numGot != 6;
                        GameManager.Instance.selfDice = numGot == 6;
                        
                        // if (numGot != 6)
                        // {
                        //     GameManager.Instance.transferDice = true;
                        // }
                        // else
                        // {
                        //     GameManager.Instance.selfDice = true;
                        // }
                        //Debug.Log($"CheckOutPlayer() < 0, NumberGot: {numberGot}");
                    }
                }
            }  
        }
    }

    [PunRPC]
    private void RollingDiceManager()
    {
        GameManager.Instance.RollingDiceManager();

        if (generateRandNumOnDice_Coroutine != null)
        {
            //StopCoroutine(GenerateRandomNumberOnDice_Enum());
            StopCoroutine(generateRandNumOnDice_Coroutine);
        }
    }
    
    [PunRPC]
    private void RollingDiceManagerRPC()
    {
        Debug.Log("From RollingDiceManagerRPC");
        GameManager.Instance.transferDice = true;
        GameManager.Instance.rolledDice = this;
        GameManager.Instance.numOfStepsToMove = 0;
        
        GameManager.Instance.RollingDiceManager();

        if (generateRandNumOnDice_Coroutine != null)
        {
            //StopCoroutine(GenerateRandomNumberOnDice_Enum());
            StopCoroutine(generateRandNumOnDice_Coroutine);
        }
    }

    private int OutPlayerToMove()
    {
        for (int i = 0; i < 4; i++) 
        {
            if(!GameManager.Instance.yellowPlayerPiece[i].isReady)
            {
                return i;
            }
        }
        return 0;
    }

    private int CheckOutPlayer()
    {
        switch (diceColor)
        {
            case DiceColor.Red:
                currentPlayerPieces = GameManager.Instance.redPlayerPiece;
                pathPointToMoveOn_ = pathParent.redPathPoints;
                break;

            case DiceColor.Blue:
                currentPlayerPieces = GameManager.Instance.bluePlayerPiece;
                pathPointToMoveOn_ = pathParent.bluePathPoints;
                break;
            case DiceColor.Yellow:
                currentPlayerPieces = GameManager.Instance.yellowPlayerPiece;
                pathPointToMoveOn_ = pathParent.yellowPathPoints;
                break;
            case DiceColor.Green:
                currentPlayerPieces = GameManager.Instance.greenPlayerPiece;
                pathPointToMoveOn_ = pathParent.greenPathPoints;
                break;
        }
        for (int i = 0; i < currentPlayerPieces.Length; i++)
        {
            if (currentPlayerPieces[i].isReady && IsPathPointsAvailableToMove(GameManager.Instance.numOfStepsToMove, currentPlayerPieces[i].numberOfStepsAlreadyMoved, pathPointToMoveOn_))
            {
                //Debug.Log($"#currentPlayerPiecesName: {currentPlayerPieces[i]}, index: {i}, Length: {currentPlayerPieces.Length}, Returning False!", currentPlayerPieces[i].gameObject);
                return i;
            }
        }

        return -1;
    }

    private bool PlayerCannotMove()
    {
        if (outPieces <= 0) return false;
        
        bool cannotMove = false;

        switch (GameManager.Instance.rolledDice.SelfDiceColor)
        {
            case DiceColor.Red:
                currentPlayerPieces = GameManager.Instance.redPlayerPiece;
                pathPointToMoveOn_ = pathParent.redPathPoints;
                break;
            
            case DiceColor.Blue:
                currentPlayerPieces = GameManager.Instance.bluePlayerPiece;
                pathPointToMoveOn_ = pathParent.bluePathPoints;
                break;
            
            case DiceColor.Yellow:
                currentPlayerPieces = GameManager.Instance.yellowPlayerPiece;
                pathPointToMoveOn_ = pathParent.yellowPathPoints;
                break;
            
            case DiceColor.Green:
                currentPlayerPieces = GameManager.Instance.greenPlayerPiece;
                pathPointToMoveOn_ = pathParent.greenPathPoints;
                break;
        }

        // if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[0])
        // {
        //     currentPlayerPieces = GameManager.Instance.redPlayerPiece;
        //     pathPointToMoveOn_ = pathParent.redPathPoints;
        // }
        // else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[1])
        // {
        //     currentPlayerPieces = GameManager.Instance.bluePlayerPiece;
        //     pathPointToMoveOn_ = pathParent.bluePathPoints;
        // }
        // else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[2])
        // {
        //     currentPlayerPieces = GameManager.Instance.yellowPlayerPiece;
        //     pathPointToMoveOn_ = pathParent.yellowPathPoints;
        // }
        // else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[3])
        // {
        //     currentPlayerPieces = GameManager.Instance.greenPlayerPiece;
        //     pathPointToMoveOn_ = pathParent.greenPathPoints;
        // }
            
        foreach (var playerPiece in currentPlayerPieces)
        {
            if (playerPiece.isReady)
            {
                //Debug.Log($"#Name: {currentPlayerPieces[i].name}, isReady: {currentPlayerPieces[i].isReady}, CannotMove: {cannotMove}", currentPlayerPieces[i].gameObject);
                if (IsPathPointsAvailableToMove(GameManager.Instance.numOfStepsToMove, playerPiece.numberOfStepsAlreadyMoved, pathPointToMoveOn_))
                {
                    return false;
                }
            }
            else
            {
                if (!cannotMove)
                {
                    cannotMove = true;
                }
            }
        }
        
        return cannotMove;
    }

    private bool IsPathPointsAvailableToMove(int numOfStepsToMove, int numOfStepsAlreadyMoved, PathPoint[] pathPointsToMoveOn)
    {
        int leftNumOfPathPoints = pathPointsToMoveOn.Length - numOfStepsAlreadyMoved;
        return leftNumOfPathPoints >= numOfStepsToMove;
    }

    public void MakePlayerReadyToMove(int outPlayer)
    {
        Debug.LogError("#PlayerIsReadyToMove");
        
        switch (diceColor)
        {
            case DiceColor.Red:
                outPlayerPiece = GameManager.Instance.redPlayerPiece[outPlayer];
                pathPointToMoveOn_ = pathParent.redPathPoints;
                GameManager.Instance.redOutPlayers++;
                break;
            
            case DiceColor.Blue:
                outPlayerPiece = GameManager.Instance.bluePlayerPiece[outPlayer];
                pathPointToMoveOn_ = pathParent.bluePathPoints;
                GameManager.Instance.blueOutPlayers++;
                break;

            case DiceColor.Yellow:
                outPlayerPiece = GameManager.Instance.yellowPlayerPiece[outPlayer];
                pathPointToMoveOn_ = pathParent.yellowPathPoints;
                GameManager.Instance.yellowOutPlayers++;
                break;
            
            case DiceColor.Green:
                outPlayerPiece = GameManager.Instance.greenPlayerPiece[outPlayer];
                pathPointToMoveOn_ = pathParent.greenPathPoints;
                GameManager.Instance.greenOutPlayers++;
                break;
        }

        // The rest of your existing MakePlayerReadyToMove logic...
        outPlayerPiece.isReady = true;
        outPlayerPiece.transform.position = pathPointToMoveOn_[0].transform.position;
        outPlayerPiece.numberOfStepsAlreadyMoved = 1;

        outPlayerPiece.previousPathPoint = pathPointToMoveOn_[0];
        outPlayerPiece.currentPathPoint = pathPointToMoveOn_[0];
        outPlayerPiece.currentPathPoint.AddPlayerPiece(outPlayerPiece);

        GameManager.Instance.RemovePathPoint(outPlayerPiece.previousPathPoint);
        GameManager.Instance.AddPathPoint(outPlayerPiece.currentPathPoint);
        GameManager.Instance.canDiceRoll = true;
        GameManager.Instance.selfDice = true;
        GameManager.Instance.transferDice = false;
        GameManager.Instance.numOfStepsToMove = 0;
        
        GameManager.Instance.SelfRoll();
    }

    private void MoveSteps(int movePlayer)
    {
        GameManager.Instance.canMove = false;
        moveSteps_Coroutine = StartCoroutine(MoveSteps_Enum(movePlayer));
    }
    
    IEnumerator MoveSteps_Enum(int movePlayer)
    {
        switch (GameManager.Instance.rolledDice.SelfDiceColor)
        {
            case DiceColor.Red:
                outPlayerPiece = GameManager.Instance.redPlayerPiece[movePlayer];
                pathPointToMoveOn_ = pathParent.redPathPoints;
                break;
            
            case DiceColor.Blue:
                outPlayerPiece = GameManager.Instance.bluePlayerPiece[movePlayer];
                pathPointToMoveOn_ = pathParent.bluePathPoints;
                break;

            case DiceColor.Yellow:
                outPlayerPiece = GameManager.Instance.yellowPlayerPiece[movePlayer];
                pathPointToMoveOn_ = pathParent.yellowPathPoints;
                break;
            
            case DiceColor.Green:
                outPlayerPiece = GameManager.Instance.greenPlayerPiece[movePlayer];
                pathPointToMoveOn_ = pathParent.greenPathPoints;
                break;
        }
        
        // if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[0])
        // {
        //     outPlayerPiece = GameManager.Instance.redPlayerPiece[movePlayer];
        //     pathPointToMoveOn_ = pathParent.redPathPoints;
        //     
        // }
        // else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[1])
        // {
        //     outPlayerPiece = GameManager.Instance.bluePlayerPiece[movePlayer];
        //     pathPointToMoveOn_ = pathParent.bluePathPoints;
        //     
        // }
        // else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[2])
        // {
        //     outPlayerPiece = GameManager.Instance.yellowPlayerPiece[movePlayer];
        //     pathPointToMoveOn_ = pathParent.yellowPathPoints;
        //    
        // }
        // else if (GameManager.Instance.rolledDice == GameManager.Instance.manageRollingDice[3])
        // {
        //     outPlayerPiece = GameManager.Instance.greenPlayerPiece[movePlayer];
        //     pathPointToMoveOn_ = pathParent.greenPathPoints;
        //    
        // }

        GameManager.Instance.transferDice = false;
        yield return new WaitForSeconds(0.25f);
        int numOfStepsToMove = GameManager.Instance.numOfStepsToMove;
        
        outPlayerPiece.currentPathPoint.RescaleAndRepositionAllPlayerPieces();
        
        for (int i = outPlayerPiece.numberOfStepsAlreadyMoved; i < (outPlayerPiece.numberOfStepsAlreadyMoved + numOfStepsToMove); i++)
        {
            if (IsPathPointsAvailableToMove(numOfStepsToMove, outPlayerPiece.numberOfStepsAlreadyMoved, pathPointToMoveOn_))
            {
                outPlayerPiece.transform.position = pathPointToMoveOn_[i].transform.position;

                yield return new WaitForSeconds(0.25f);
            }
        }

        if (IsPathPointsAvailableToMove(numOfStepsToMove, outPlayerPiece.numberOfStepsAlreadyMoved, pathPointToMoveOn_))
        {
            outPlayerPiece.numberOfStepsAlreadyMoved += numOfStepsToMove;

            GameManager.Instance.RemovePathPoint(outPlayerPiece.previousPathPoint);
            outPlayerPiece.previousPathPoint.RemovePlayerPiece(outPlayerPiece);
            outPlayerPiece.currentPathPoint = pathPointToMoveOn_[outPlayerPiece.numberOfStepsAlreadyMoved - 1];

            if (outPlayerPiece.currentPathPoint.AddPlayerPiece(outPlayerPiece))
            {
                if (outPlayerPiece.numberOfStepsAlreadyMoved == 57)
                {
                    GameManager.Instance.selfDice = true;
                }
                else
                {
                    GameManager.Instance.transferDice = GameManager.Instance.numOfStepsToMove != 6;
                    GameManager.Instance.selfDice = GameManager.Instance.numOfStepsToMove == 6;

                    // if (GameManager.Instance.numOfStepsToMove != 6)
                    // {
                    //     GameManager.Instance.transferDice = true;
                    // }
                    // else
                    // {
                    //     GameManager.Instance.selfDice = true;
                    // }
                }
            }
            else
            {
                GameManager.Instance.selfDice = true;
            }
            
            GameManager.Instance.MovePlayerToken(outPlayerPiece);
            GameManager.Instance.AddPathPoint(outPlayerPiece.currentPathPoint);
            outPlayerPiece.previousPathPoint = outPlayerPiece.currentPathPoint;
            
            GameManager.Instance.numOfStepsToMove = 0;
        }
        
        GameManager.Instance.canMove = true;
        
        if(DataManager.Instance.OwnDiceColor == SelfDiceColor)
            GameManager.Instance.RollingDiceManager();
        
        if (moveSteps_Coroutine != null)
        {
            StopCoroutine(nameof(moveSteps_Coroutine));
        }
    }
}