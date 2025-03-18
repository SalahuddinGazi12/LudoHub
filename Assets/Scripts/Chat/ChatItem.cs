using TMPro;
using UnityEngine;

public class ChatItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI chatText;

    public void SetChatText(string message)
    {
        chatText.text = message;
    }
}
