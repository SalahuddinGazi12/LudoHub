using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections;

public class ChatUI : MonoBehaviour, IMessageObserver
{
    public event Action<string> OnMessageSent;

    [SerializeField] private TMP_Text _chatLog;
    [SerializeField] private TMP_InputField _messageInput;
    [SerializeField] private ScrollRect _scrollRect;

    public void OnMessageReceived(ChatMessage message)
    {
        _chatLog.text += message.FormatMessage() + "\n";
        StartCoroutine(ScrollToBottom());
    }

    public void SendMessage()
    {
        if (!string.IsNullOrEmpty(_messageInput.text))
        {
            OnMessageSent?.Invoke(_messageInput.text);
            _messageInput.text = "";
        }
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        _scrollRect.normalizedPosition = Vector2.zero;
    }
}