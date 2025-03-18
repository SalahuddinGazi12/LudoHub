using Photon.Pun;
using UnityEngine;
using System.Collections;

public class BluePlayerPiece : PlayerPiece
{
    public RollingDice blueHomeRollingDice;
    //private bool hasClicked = false;
    //private float cooldownTime = 3.1f;

    private void Start()
    {
        // Optionally initialize anything here if needed
        SetSelfDiceColor(DiceColor.Blue);
    }

    // private void OnMouseDown()
    // {
    //     // Ensure the player can only interact when it's their turn in multiplayer
    //     if (DataManager.Instance.GameType == GameType.Multiplayer && DataManager.Instance.ActiveDiceColor != DataManager.Instance.OwnDiceColor)
    //     {
    //         return;
    //     }
    //
    //     // Check if the cooldown allows interaction
    //     if (!hasClicked)
    //     {
    //         hasClicked = true; // Prevent multiple clicks during cooldown
    //
    //         if (DataManager.Instance.GameType == GameType.Multiplayer)
    //         {
    //             OnPieceIsTappedToMoveRPC(); // Use RPC for multiplayer interactions
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
    //     hasClicked = false; // Reset click ability after cooldown
    // }

    [PunRPC]
    public override void OnPieceIsTappedToMove()
    {
        if (GameManager.Instance.rolledDice != null)
        {
            RollingDice bluehomedice = GameObject.Find("Blue").transform.GetChild(0).GetComponent<RollingDice>();
            RollingDice bluehomedice1 = GameObject.Find("Blue").transform.GetChild(1).GetComponent<RollingDice>();
            
            blueHomeRollingDice = DataManager.Instance.GameType == GameType.Multiplayer ? bluehomedice1 : bluehomedice;
            
            //Debug.LogError($"BluePlayerPiece, RolledDice: {GameManager.Instance.rolledDice.name}, isEqual: {GameManager.Instance.rolledDice == blueHomeRollingDice}, isReady: {isReady} CanMove: {canMove}");

            if (!isReady)
            {
                if (GameManager.Instance.rolledDice == blueHomeRollingDice && GameManager.Instance.numOfStepsToMove == 6)
                {
                    GameManager.Instance.blueOutPlayers += 1;
                    MakePlayerReadyToMove(pathsParent.bluePathPoints);
                    GameManager.Instance.numOfStepsToMove = 0;
                    
                    return;
                }
            }

            if (GameManager.Instance.rolledDice != blueHomeRollingDice || !isReady) return;
            
            canMove = true;
            MoveSteps(pathsParent.bluePathPoints);
        }
    }
}
