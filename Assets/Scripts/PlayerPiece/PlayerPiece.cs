using DG.Tweening;
//using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerPiece : MonoBehaviour
{
    public bool isReady;
    public bool canMove;

    public bool moveNow;
    public int numberOfStepsAlreadyMoved;

    private Coroutine moveStepsCoroutine;

    public PathObjectsParent pathsParent;
    public PathPoint previousPathPoint;
    public PathPoint currentPathPoint;
   // public PhotonView photonView;
    public SpriteRenderer spriteRenderer;
    [SerializeField] private float blinkInterval = 0.4f;
    [field: SerializeField] public DiceColor SelfDiceColor { get; private set; }

    public byte selfId;
    private Coroutine blinkCoroutine;

    public bool isFirstPlayer;

    protected bool hasClicked;
    protected float cooldownTime;
    
    #region MonoBehaviour Methods
    private void Awake()
    {
        pathsParent = FindAnyObjectByType<PathObjectsParent>();
      //  photonView = GetComponent<PhotonView>();

    }

    private void OnMouseDown()
    {
        // Ensure the player can only interact when it's their turn in multiplayer
        if (DataManager.Instance.GameType == GameType.Multiplayer && DataManager.Instance.ActiveDiceColor != DataManager.Instance.OwnDiceColor)
        {
            return;
        }

        // Check if interaction is allowed based on cooldown
        if (hasClicked) return;
        hasClicked = true; // Prevent multiple clicks during cooldown

        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            OnPieceIsTappedToMoveRPC(); // Multiplayer logic
        }
        // else
        // {
        //     OnPieceIsTappedToMove(); // Single-player logic
        // }
        
        OnPieceIsTappedToMove();

        StartCoroutine(ClickCooldown()); // Start cooldown
    }

    // Coroutine to handle the cooldown period
    private IEnumerator ClickCooldown()
    {
        yield return new WaitForSeconds(cooldownTime);
        hasClicked = false; // Reset click ability after cooldown
    }

    #endregion //MonoBehaviour Methods

    protected void SetSelfDiceColor(DiceColor color)
    {
        SelfDiceColor = color;
    }
    
    public void StartBlinking()
    {
        blinkCoroutine ??= StartCoroutine(Blink());
    }

    public void StopBlinking()
    {
        if (blinkCoroutine == null) return;
        
        StopCoroutine(blinkCoroutine);
        blinkCoroutine = null;
        spriteRenderer.enabled = true;  // Ensure sprite is visible when stopping
    }

    private IEnumerator Blink()
    {
        while (blinkCoroutine != null)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;  // Toggle visibility
            yield return new WaitForSeconds(blinkInterval);    // Wait for the interval
        }
    }

    public void SetSelfId(byte selfIdValue)
    {
        selfId = selfIdValue;
    }

    public void MoveSteps(PathPoint[] pathPointsToMoveOn)
    {
        Debug.LogError($"MoveStep,CanMove: {canMove}");
        if (canMove)
        {
            moveStepsCoroutine = StartCoroutine(MoveSteps_Enum(pathPointsToMoveOn));
            //GameManager.Instance.transferDice = false;
        }
    }

    public void MakePlayerReadyToMove(PathPoint[] pathPointsToMoveOn)
    {
        isReady = true;
        transform.position = pathPointsToMoveOn[0].transform.position;
        numberOfStepsAlreadyMoved = 1;
        
        previousPathPoint = pathPointsToMoveOn[0];
        currentPathPoint = pathPointsToMoveOn[0];
        currentPathPoint.AddPlayerPiece(this);
        GameManager.Instance.RemovePathPoint(previousPathPoint);
        GameManager.Instance.AddPathPoint(currentPathPoint);
        GameManager.Instance.canDiceRoll = true;
        
        //GameManager.Instance.selfDice = true;
        
        GameManager.Instance.transferDice = false;
    }

    private IEnumerator MoveSteps_Enum(PathPoint[] pathPointsToMoveOn)
    {
        GameManager.Instance.transferDice = false;
        yield return new WaitForSeconds(0.25f);
        int numOfStepsToMove = GameManager.Instance.numOfStepsToMove;

        if (canMove)
        {
            previousPathPoint.RescaleAndRepositionAllPlayerPieces();
            for (int i = numberOfStepsAlreadyMoved; i < (numberOfStepsAlreadyMoved + numOfStepsToMove); i++)
            {
                if (IsPathPointsAvailableToMove(numOfStepsToMove, numberOfStepsAlreadyMoved, pathPointsToMoveOn))
                {
                    transform.position = pathPointsToMoveOn[i].transform.position;
                    yield return new WaitForSeconds(0.25f);
                }
            }
        }
        
        var value = IsPathPointsAvailableToMove(numOfStepsToMove, numberOfStepsAlreadyMoved, pathPointsToMoveOn);
        Debug.Log($"MoveStep_Enum, Color: {SelfDiceColor}, CanMove: {canMove}, IsPathPointsAvailableToMove: {value}");
        // if (IsPathPointsAvailableToMove(numOfStepsToMove, numberOfStepsAlreadyMoved, pathPointsToMoveOn))
        if (value)
        {
            numberOfStepsAlreadyMoved += numOfStepsToMove;

            GameManager.Instance.RemovePathPoint(previousPathPoint);
            previousPathPoint.RemovePlayerPiece(this);
            currentPathPoint = pathPointsToMoveOn[numberOfStepsAlreadyMoved - 1];

            if (currentPathPoint.AddPlayerPiece(this))
            {
                if (numberOfStepsAlreadyMoved == 57) // Assuming 57 is the final position
                {
                    GameManager.Instance.selfDice = true;
                   // PlayerReachedHome();
                }
                else
                {
                    if (GameManager.Instance.numOfStepsToMove != 6)
                    { 
                        //GameManager.Instance.transferDice = isFirstPlayer != true;
                        GameManager.Instance.transferDice = true;
                    }
                    else
                    {
                        GameManager.Instance.selfDice = true;
                    }
                }
            }
            else
            {
                GameManager.Instance.selfDice = true;
            }

            GameManager.Instance.AddPathPoint(currentPathPoint);
            previousPathPoint = currentPathPoint;
            GameManager.Instance.numOfStepsToMove = 0;
        }

        canMove = true;

        if (DataManager.Instance.GameType == GameType.Multiplayer)
        {
            Debug.Log($"From MoveSteps: Own: {DataManager.Instance.OwnDiceColor}, Self: {SelfDiceColor}");
            if(DataManager.Instance.OwnDiceColor == SelfDiceColor )
                GameManager.Instance.RollingDiceManager();
        }
        else
        {
            GameManager.Instance.RollingDiceManager();
        }
        
        moveStepsCoroutine = null;
        // if (moveStepsCoroutine != null)
        // {
        //     StopCoroutine(moveStepsCoroutine);
        // }
    }

    private bool IsPathPointsAvailableToMove(int numOfStepsToMove, int numOfStepsAlreadyMoved, PathPoint[] pathPointsToMoveOn)
    {
        int leftNumOfPathPoints = pathPointsToMoveOn.Length - numOfStepsAlreadyMoved;
        return leftNumOfPathPoints >= numOfStepsToMove;
    }

    public void OnPieceIsTappedToMoveRPC()
    {
       // photonView.RPC(nameof(OnPieceIsTappedToMove), RpcTarget.Others);
    }

  //  [PunRPC]
    public virtual void OnPieceIsTappedToMove()
    {

    }
}