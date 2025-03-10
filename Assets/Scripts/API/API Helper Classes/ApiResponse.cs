using System;
using System.Collections.Generic;

[Serializable]
public class ApiResponse
{
    public bool status;
    public string token;
    public User user;
    public string message;
}

[Serializable]
public class LogOutResponse
{
    public bool status;
    public string message;
}

[Serializable]
public class StatsResponse
{
    public bool status;
    public string total_game;
    public string total_win_game;
    public string total_win_sum;
    public string win_rate;
    public string current_streak;
    public string best_win_streak;
}

[Serializable]
public class MessageResponse
{
    public bool status;
    public string message;
}

[Serializable]
public class WishCoinResponse
{
    public bool status;
    public string message;
    public int debuct_amount;
}


[Serializable]
public class GameHistoryData
{
    public string game_session_id;
    public string win_count;
    public string win_coin;
    public string fee_coin;
}

[Serializable]
public class GameHistoryResponse
{
    public bool status;
    public List<GameHistoryData> data;
}

[Serializable]
public class CoinResponse
{
    public string status;
    public string coin;
}

[Serializable]
public class SessionResponse
{
    public bool status;
    public string message;
    public string total_win;
    public string game_fee;
    public string grand_total;
}


[Serializable]
public class SessionInitResponse
{
    public bool status;
    public string game_session;
    public string game_name;
}