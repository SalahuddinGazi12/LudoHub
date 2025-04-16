//using Photon.Pun;
using UnityEngine;
using System.Collections;

public class GreenPlayerPiece : PlayerPiece
{
    public RollingDice greenHomeRollingDice;
    //private bool hasClicked = false;
    //private float cooldownTime = 3.1f;

    private void Start()
    {
        // Optional: Initialize anything here if necessary
        SetSelfDiceColor(DiceColor.Green);
    }

    // private void OnMouseDown()
    // {
    //     // Ensure the player can only interact when it's their turn in multiplayer
    //     if (DataManager.Instance.GameType == GameType.Multiplayer &&
    //         DataManager.Instance.ActiveDiceColor != DataManager.Instance.OwnDiceColor)
    //     {
    //         return;
    //     }
    //
    //     // Check if interaction is allowed based on cooldown
    //     if (!hasClicked)
    //     {
    //         hasClicked = true; // Prevent multiple interactions during cooldown
    //
    //         if (DataManager.Instance.GameType == GameType.Multiplayer)
    //         {
    //             OnPieceIsTappedToMoveRPC(); // Multiplayer logic
    //         }
    //         else
    //         {
    //             OnPieceIsTappedToMove(); // Single-player logic
    //         }
    //
    //         StartCoroutine(ClickCooldown()); // Start cooldown
    //     }
    // }
    //
    // // Coroutine to handle the cooldown period
    // private IEnumerator ClickCooldown()
    // {
    //     yield return new WaitForSeconds(cooldownTime);
    //     hasClicked = false; // Reset interaction ability after cooldown
    // }

   // [PunRPC]
    public override void OnPieceIsTappedToMove()
    {
        if (GameManager.Instance.rolledDice != null)
        {
            // Get the two green dice components
            RollingDice greenhomedice = GameObject.Find("Green").transform.GetChild(0).GetComponent<RollingDice>();
            RollingDice greenhomedice1 = GameObject.Find("Green").transform.GetChild(1).GetComponent<RollingDice>();

            greenHomeRollingDice = DataManager.Instance.GameType == GameType.Multiplayer ? greenhomedice1 : greenhomedice;
            Debug.LogError($"GreenPlayerPiece, RolledDice: {GameManager.Instance.rolledDice.name}, isEqual: {GameManager.Instance.rolledDice == greenHomeRollingDice}, isReady: {isReady}, CanMove: {canMove}");

            // Determine which dice is active
            // if (greenhomedice.isActiveAndEnabled)
            // {
            //     greenHomeRollingDice = greenhomedice;
            // }
            // else if (greenhomedice1.isActiveAndEnabled)
            // {
            //     greenHomeRollingDice = greenhomedice1;
            // }

            // Handle player readiness and movement
            if (!isReady)
            {
                if (GameManager.Instance.rolledDice == greenHomeRollingDice && GameManager.Instance.numOfStepsToMove == 6)
                {
                    GameManager.Instance.greenOutPlayers += 1;
                    MakePlayerReadyToMove(pathsParent.greenPathPoints);
                    GameManager.Instance.numOfStepsToMove = 0;
                    return;
                }
            }

            // Allow movement if ready and dice match
            if (GameManager.Instance.rolledDice == greenHomeRollingDice && isReady)
            {
                canMove = true;
                MoveSteps(pathsParent.greenPathPoints);
            }
        }
    }
}
