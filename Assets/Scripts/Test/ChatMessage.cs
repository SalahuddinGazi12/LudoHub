using System;

public abstract class ChatMessage
{
    public string Sender { get; protected set; }
    public DateTime Timestamp { get; } = DateTime.Now;
    public abstract string FormatMessage();
}
