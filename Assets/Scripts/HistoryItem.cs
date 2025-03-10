using UnityEngine;
using TMPro;

public class HistoryItem : MonoBehaviour
{
    [SerializeField] private TMP_Text matchSerialText;
    [SerializeField] private TMP_Text winLossText;
    [SerializeField] private TMP_Text feeText;
    [SerializeField] private TMP_Text amountText;

    public void SetHistoryItemData(GameHistoryData gameHistoryData)
    {
        matchSerialText.text = gameHistoryData.game_session_id;
        winLossText.text = (string.Equals("1", gameHistoryData.win_count)) ? "Win" : "Lost";
        amountText.text = gameHistoryData.win_count;
        feeText.text = gameHistoryData.fee_coin;
    }
}