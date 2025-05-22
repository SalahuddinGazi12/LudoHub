using System.Collections.Generic;

public class MessageSubject
{
    private readonly List<IMessageObserver> _observers = new();

    public void AddObserver(IMessageObserver observer) => _observers.Add(observer);
    public void RemoveObserver(IMessageObserver observer) => _observers.Remove(observer);

    public void NotifyObservers(ChatMessage message)
    {
        foreach (var observer in _observers)
        {
            observer.OnMessageReceived(message);
        }
    }
}