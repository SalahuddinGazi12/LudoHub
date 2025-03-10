using TMPro;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class JoinedPlayer : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI selfNameText;
    public DiceColor SelfDiceColor { get; private set; }

    // Sets the player's information and activates the GameObject
    public void SetJoinedPlayerInfo(string playerName, DiceColor diceColor)
    {
        selfNameText.text = Helper.GetPascalCaseString(playerName);
        SelfDiceColor = diceColor;
        gameObject.name = $"JoinedPlayer_{diceColor}";
        gameObject.SetActive(true);
    }

    // Callback when a player leaves the room
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        Debug.Log($"Player {otherPlayer.NickName} has left the room.");

        if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
        {
            // Update UI to show remaining players
            UpdatePlayerList();
        }
        else
        {
            // Handle game end if only one player remains
            HandleGameEndDueToDisconnection();
        }
    }

    // Updates the list of players remaining in the game
    private void UpdatePlayerList()
    {
        // Logic to update the player list UI or other player-related data
        Debug.Log("Updating player list for remaining players.");
        // Code to update UI elements as needed
    }

    // Handles the game ending if all players except one have disconnected
    private void HandleGameEndDueToDisconnection()
    {
        // Show a message and handle end-game logic
        UIManager.Instance.popUp.ShowMessagePanel("The game has ended as all players left.");

        // Leave the room and return to lobby or main menu
        PhotonNetwork.LeaveRoom();
    }
}
