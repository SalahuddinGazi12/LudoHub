using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayer : MonoBehaviour
{
    public string Name { get; }
    public Sprite Icon { get; }
    public Color Color { get; }

    public int Score { get; set; } = 0;

    public int ActorNum { get; set; }
    public string ID;
    public GamePlayer(string name, string id, Sprite icon, Color color)
    {
        Name = name;
        Icon = icon;
        Color = color;
        ID = id;
    }

    public GamePlayer(string name, Sprite icon, Color color)
    {
        Name = name;
        Icon = icon;
        Color = color;
    }
}
