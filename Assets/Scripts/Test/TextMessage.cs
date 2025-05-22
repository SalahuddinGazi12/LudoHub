public class TextMessage : ChatMessage
{
    public string Content { get; }

    public TextMessage(string sender, string content)
    {
        Sender = sender;
        Content = content;
    }

    public override string FormatMessage()
        => $"<color=green>{Sender}:</color> {Content}";
}