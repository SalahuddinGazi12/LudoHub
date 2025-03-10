using Photon.Pun;
using UnityEngine;
using System.Collections;

public class RedPlayerPiece : PlayerPiece
{
    public RollingDice redHomeRollingDice;
    //private bool hasClicked;
    //private float cooldownTime = 3.1f;

    private void Start()
    {
        // Optionally initialize anything here
        SetSelfDiceColor(DiceColor.Red);
    }

    // private void OnMouseDown()
    // {
    //     // Ensure the player can only interact when it's their turn in multiplayer
    //     if (DataManager.Instance.GameType == GameType.Multiplayer && DataManager.Instance.ActiveDiceColor != DataManager.Instance.OwnDiceColor)
    //     {
    //         return;
    //     }
    //
    //     // Check if interaction is allowed based on cooldown
    //     if (hasClicked) return;
    //     hasClicked = true; // Prevent multiple clicks during cooldown
    //
    //     if (DataManager.Instance.GameType == GameType.Multiplayer)
    //     {
    //         OnPieceIsTappedToMoveRPC(); // Multiplayer logic
    //     }
    //     else
    //     {
    //         OnPieceIsTappedToMove(); // Single-player logic
    //     }
    //
    //     StartCoroutine(ClickCooldown()); // Start cooldown
    // }
    //
    // // Coroutine to handle the cooldown period
    // private IEnumerator ClickCooldown()
    // {
    //     yield return new WaitForSeconds(cooldownTime);
    //     hasClicked = false; // Reset click ability after cooldown
    // }

    [PunRPC]
    public override void OnPieceIsTappedToMove()
    {
        if (GameManager.Instance.rolledDice != null)
        {
            // Get the two red dice components
            RollingDice redhomedice = GameObject.Find("Red").transform.GetChild(0).GetComponent<RollingDice>();
            RollingDice redhomedice1 = GameObject.Find("Red").transform.GetChild(1).GetComponent<RollingDice>();

            
            redHomeRollingDice = DataManager.Instance.GameType == GameType.Multiplayer ? redhomedice1 : redhomedice;
            
            //Debug.LogError($"RedPlayerPiece, RolledDice: {GameManager.Instance.rolledDice.name}, isEqual: {GameManager.Instance.rolledDice == redHomeRollingDice}, isReady: {isReady} CanMove: {canMove}");


            // // Determine which dice is active
            // if (redhomedice.isActiveAndEnabled)
            // {
            //     redHomeRollingDice = redhomedice;
            // }
            // else if (redhomedice1.isActiveAndEnabled)
            // {
            //     redHomeRollingDice = redhomedice1;
            // }

            // Handle player readiness and movement
            if (!isReady)
            {
                Debug.LogWarning($"GameManager rolledDice: {GameManager.Instance.rolledDice}");
                Debug.LogWarning($"Red Player rolledDice: {redHomeRollingDice}");
                Debug.LogWarning($"Are they equal? {GameManager.Instance.rolledDice == redHomeRollingDice}");

                if (GameManager.Instance.rolledDice == redHomeRollingDice && GameManager.Instance.numOfStepsToMove == 6)
                {
                    GameManager.Instance.redOutPlayers += 1;
                    MakePlayerReadyToMove(pathsParent.redPathPoints);
                    GameManager.Instance.numOfStepsToMove = 0;

                    return;
                }
            }

            // Allow movement if ready and dice match
            if (GameManager.Instance.rolledDice != redHomeRollingDice || !isReady) return;
            
            canMove = true;
            isFirstPlayer = GameManager.Instance.redOutPlayers == 1;
            MoveSteps(pathsParent.redPathPoints);
        }
    }
}