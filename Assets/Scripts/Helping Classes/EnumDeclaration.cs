
public enum TeamColor
{
    Red = 0, Blue = 1, Yellow = 2, Green = 3
}

public enum DiceColor
{
    Red = 0, Blue = 1, Yellow = 2, Green = 3, Unknown = 4
}

public enum UserType { Null, APP, NORMAL }

public enum GameName
{
    LUDO = 1,
    POOL = 2,
    CARROM = 3,
    DOTANDBLOCK = 4,
    CHESS = 5
}

public enum LocalRotation
{
    Null,
    Z90,
    Z180,
    Y180
}

public enum RoomType { Null, Random, Private, AI, Free }
public enum RoomMode { Null, Create, Join }

public enum JoinedPlayerType { AddUser, DeleteUser, HostUser }

public enum GameType { Null, Single, Multiplayer, LocalMultiplayer }

public enum CoinType { WIN, LOSS, BONUS, DRAW }

public enum AnchorPos
{
    LeftBottom,
    LeftTop,
    RightBottom,
    RightTop
}

public enum GameState
{
    Init, Play, Finished
}