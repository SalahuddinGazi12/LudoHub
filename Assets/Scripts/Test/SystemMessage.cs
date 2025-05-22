using System;

public class SystemMessage : ChatMessage
{
    public string Event { get; }

    public SystemMessage(string eventDescription)
    {
        Sender = "System";
        Event = eventDescription;
    }

    public override string FormatMessage()
        => $"<color=orange>{Sender}: {Event}</color>";
}