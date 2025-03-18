using TMPro;
using UnityEngine;

public class PrivateJoinPlayer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameTxt;
    public DiceColor selfDiceColor;

    public void SetPrivatePlayerInfo(string playerName, DiceColor diceColor)
    {
        playerNameTxt.text = Helper.GetPascalCaseString(playerName);
        selfDiceColor = diceColor;
    }
}
