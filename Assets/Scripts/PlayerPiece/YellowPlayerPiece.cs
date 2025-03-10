using Photon.Pun;
using UnityEngine;
using System.Collections;

public class YellowPlayerPiece : PlayerPiece
{
    public RollingDice yellowHomeRollingDice;
    //private bool hasClicked = false;
    //private float cooldownTime = 3.1f;

    private void Start()
    {
        // Optional: Initialize anything here
        SetSelfDiceColor(DiceColor.Yellow);
        
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
    //     if (hasClicked) return;
    //     hasClicked = true; // Prevent multiple interactions during cooldown
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
    //     hasClicked = false; // Reset interaction ability after cooldown
    // }

    [PunRPC]
    public override void OnPieceIsTappedToMove()
    {
        if (GameManager.Instance.rolledDice != null)
        {
            // Get the two yellow dice components
            RollingDice yellowHomeDice = GameObject.Find("Yellow").transform.GetChild(0).GetComponent<RollingDice>();
            RollingDice yellowHomeDiceM = GameObject.Find("Yellow").transform.GetChild(1).GetComponent<RollingDice>();

            yellowHomeRollingDice = DataManager.Instance.GameType == GameType.Multiplayer ? yellowHomeDiceM : yellowHomeDice;

            // Debug.LogError($"YellowPlayerPiece, RolledDice: {GameManager.Instance.rolledDice.name}, isEqual: {GameManager.Instance.rolledDice == yellowHomeRollingDice}, isReady: {isReady} CanMove: {canMove}");

            // Handle player readiness and movement
            if (!isReady)
            {
                if (GameManager.Instance.rolledDice == yellowHomeRollingDice && GameManager.Instance.numOfStepsToMove == 6)
                {
                    GameManager.Instance.yellowOutPlayers += 1;
                    MakePlayerReadyToMove(pathsParent.yellowPathPoints);
                    GameManager.Instance.numOfStepsToMove = 0;

                    return;
                }
            }

            // Allow movement if ready and dice match
            if (GameManager.Instance.rolledDice == yellowHomeRollingDice && isReady)
            {
                canMove = true;

                isFirstPlayer = GameManager.Instance.yellowOutPlayers == 1;
                MoveSteps(pathsParent.yellowPathPoints);
            }
        }
    }
}
