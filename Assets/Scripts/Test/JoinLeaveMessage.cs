using System;

public class JoinLeaveMessage : ChatMessage
{
    public bool IsJoining { get; }

    public JoinLeaveMessage(string playerName, bool isJoining)
    {
        Sender = playerName;
        IsJoining = isJoining;
    }

    public override string FormatMessage()
        => IsJoining
            ? $"<color=yellow>{Sender} joined the chat</color>"
            : $"<color=red>{Sender} left the chat</color>";
}