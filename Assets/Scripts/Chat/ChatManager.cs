using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private GameObject messageCountGobj;
    [SerializeField] private TextMeshProUGUI messageCountTxt;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private ChatItem blueChatItem;
    [SerializeField] private ChatItem redChatItem;
    [SerializeField] private RectTransform chatItemParent;
    [SerializeField] private Button chatBtn;
    private int unreadMessageCount;
    private PhotonView photonView;


    private void Start()
    {
        messageCountGobj.SetActive(false);
        photonView = GetComponent<PhotonView>();
    }

    public void OpenChatBox()
    {
        unreadMessageCount = 0;
        
        messageCountGobj.SetActive(false);
        chatBtn.gameObject.SetActive(false);
        chatPanel.gameObject.SetActive(true);
    }

    public void CloseChatBox()
    {
        chatBtn.gameObject.SetActive(true);
        chatPanel.gameObject.SetActive(false);
    }

    public void SendMessageToChat()
    {
        if(chatInputField.text.IsNullOrEmpty())
            return;

        string messageString = string.Concat(Helper.GetPascalCaseString(PhotonNetwork.LocalPlayer.NickName), ": ", chatInputField.text);
        SendMessageRPC(messageString, PhotonNetwork.LocalPlayer.ActorNumber);
        chatInputField.text = string.Empty;
        photonView.RPC(nameof(SendMessageRPC), RpcTarget.Others, messageString, PhotonNetwork.LocalPlayer.ActorNumber);
    }
    
    
    [PunRPC]
    private void SendMessageRPC(string message, int senderActorNum)
    {
        if(message.IsNullOrEmpty())
            return;
        
        ChatItem instantiatedItem;
        
        if (senderActorNum == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            //instantiatedItem = Instantiate(DataManager.Instance.OwnTeamColor == TeamColor.White ? blueChatItem : redChatItem, chatItemParent);
            instantiatedItem = Instantiate(redChatItem, chatItemParent);
        }
        else
        {
            //instantiatedItem = Instantiate(DataManager.Instance.OpponentTeamColor == TeamColor.White ? blueChatItem : redChatItem, chatItemParent);
            instantiatedItem = Instantiate(blueChatItem, chatItemParent);
            unreadMessageCount++;
            
            messageCountTxt.text = unreadMessageCount > 9 ? string.Concat(9, "+") : unreadMessageCount.ToString();
            messageCountGobj.gameObject.SetActive(true);
        }

        instantiatedItem.SetChatText(message);
    }
}