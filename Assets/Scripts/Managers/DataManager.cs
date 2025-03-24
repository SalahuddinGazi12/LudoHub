using System;
using Photon.Realtime;
using SecureDataSaver;
using UnityEngine;
using UnityEngine.Serialization;

public class DataManager : MonoBehaviour
{
    private static DataManager instance;
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<DataManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("DataManager");
                    instance = obj.AddComponent<DataManager>();
                }
            }

            return instance;
        }
    }

    public BoardGraphics[] boardGraphics;
    

    [field: SerializeField] public RoomType CurrentRoomType { get; private set; }
    [field: SerializeField] public RoomMode CurrentRoomMode { get; private set; }
    [field: SerializeField] public string Token { get; private set; }

    [field: SerializeField] public readonly string GameId = "1";

    [field: SerializeField] public string SessionId { get; private set; }
    [field: SerializeField] public User CurrentUser { get; private set; }

    [field: SerializeField] public UserType CurrentUserType { get; private set; }
    [field: SerializeField] public DiceColor OwnDiceColor { get; private set; }
    [field: SerializeField] public TeamColor OpponentTeamColor { get; private set; }
    [field: SerializeField] public byte MaxPlayerNumberForCurrentBoard { get; private set; }

    [field: SerializeField] public int Coins { get; private set; }

    [field: SerializeField] public int CurrentEntryFee { get; private set; }

    [field: SerializeField] public GameType GameType { get; private set; }

    [field: SerializeField] public bool IsLoggedIn { get; private set; }

    public int defaultCoin = 15000;
    [field: SerializeField] public int FeePercentage { get; private set; }

    [field: SerializeField] private Sprite AvatarSprite { get; set; }
    [field: SerializeField] public GameState CurrentGameState { get; private set; }

    [field: SerializeField] public DiceColor ActiveDiceColor { get; private set; }
    [field: SerializeField]public bool IsMyTurn => ActiveDiceColor == OwnDiceColor;
    [SerializeField] private int maxTurnTime = 30;
    [SerializeField] private int maxTurnCanIgnore = 3;
    [SerializeField] private int maxNumberOfFixTurns = 10;
    public int MaxTurnTime => maxTurnTime;
    public int MaxTurnCanIgnore => maxTurnCanIgnore;
    public int MaxNumberOfFixTurns => maxNumberOfFixTurns;
    
    public LocalRotation LocalRotation { get; private set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetCoins(0);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ReInitialize()
    {
        instance = null;
        Instance.Awake();
    }
    
    public LocalRotation SetLocalRotation(LocalRotation localRotation) => LocalRotation = localRotation;

    public void SetCurrentUser(ApiResponse response)
    {
        CurrentUser = new User(response.user);
        Token = response.token;
        IsLoggedIn = true;
        SetOwnDiceColor(DiceColor.Unknown);
    }

    public void SetPlayerAvatar(Sprite sprite)
    {
        AvatarSprite = sprite;
    }

    public Sprite GetDefaultAvatarSprite()
    {
        return AvatarSprite;
    }

    public void SetCoins(int coin)
    {
        Coins = coin;
        CurrentUser.coins = Coins;
    }

    public void SetFeePercentage(int value)
    {
        FeePercentage = value;
    }

    public int GetPercentage(int boardFees)
    {
        if (FeePercentage <= 0)
            return 0;

        return (FeePercentage * boardFees) / 100;
    }

    public void SetOwnDiceColor(DiceColor diceColor)
    {
        OwnDiceColor = diceColor;
        //OpponentTeamColor = (diceColor == TeamColor.Blue) ? TeamColor.Red : TeamColor.Blue;
    }

    public void SetActiveDiceColor(DiceColor diceColor) => ActiveDiceColor = diceColor;

    public void SetMaxPlayerNumberForCurrentBoard(byte maxPlayerNumber) => MaxPlayerNumberForCurrentBoard = maxPlayerNumber;

    public void SetCurrentEntryFees(int entryFees) => CurrentEntryFee = entryFees;

    public void SetGameType(GameType gameType) => GameType = gameType;

    public void SetCurrentGameState(GameState gameState) => CurrentGameState = gameState;

    public void SetSessionId(string session) => SessionId = session;

    public void SetAccessToken(string token) => Token = token;

    public void SetCurrentRoomType(RoomType type) => CurrentRoomType = type;

    public void SetCurrentUserType(UserType type) => CurrentUserType = type;

    public void SetCurrentRoomMode(RoomMode mode) => CurrentRoomMode = mode;
    
    public void ReduceNormalUserCoins(int coin, bool saveCoin = true)
    {
        UpdateNormalUserCoins(coin * -1, saveCoin);
    }

    public void UpdateNormalUserCoins(int coins, bool saveCoin = true)
    {
        int prevCoins = Coins;

        Coins = Mathf.Max(0, Coins += coins);
        CurrentUser.coins = Coins;

        string key = CurrentUser.email + "_coins";

        PlayerPrefs.SetInt(key, Coins);
        Debug.Log($"Param: {coins}, TotalCon: {Coins}, PrevCoins: {prevCoins}, UserCoin: {CurrentUser.coins}, Key: {key}");

        if (saveCoin)
        {
            ApiResponse response = new ApiResponse();
            response.token = Token;
            response.user = CurrentUser;
            
            DataSaver.WriteData(JsonUtility.ToJson(response), PlayerPrefs.GetString("userData"));
        }
    }

    public void UpdateAppUserCoin(int coins)
    {
        int prevCoin = Coins;
        Coins = Mathf.Max(0, Coins += coins);
        CurrentUser.coins = Coins;
        Debug.Log($"Param: {coins}, TotalCon: {Coins}, PrevCoins: {prevCoin}, UserCoin: {CurrentUser.coins}, AppUserCoin");

        // ApiResponse response = JsonUtility.FromJson<ApiResponse>(DataSaver.ReadData(PlayerPrefs.GetString("userData")));
        //
        // if (response != null)
        // {
        //     response.user.coins = Coins;
        //     DataSaver.WriteData(response, PlayerPrefs.GetString("userData"));
        // }
    }

    public void SetCurrentEntryFeeFromFormattedValue(string value)
    {
        CurrentEntryFee = GetIntValueFromFormattedPrizeValue(value);
    }
    public int GetIntValueFromFormattedPrizeValue(string value)
    {
        return value switch
        {
            "5K" => 5000,
            "7K" => 7000,
            "10K" => 10000,
            "20K" => 20000,
            "50K" => 50000,
            "100K" => 100000,
            "200K" => 200000,
            "5M" => 5000000,
            "10M" => 10000000,
            "20M" => 20000000,
            "50M" => 50000000,
            _ => 5000
        };
    }

    public void ResetCurrentMatchData()
    {
        CurrentRoomType = RoomType.Null;
        CurrentRoomMode = RoomMode.Null;
        CurrentEntryFee = MaxPlayerNumberForCurrentBoard = 0;
        GameType = GameType.Null;
        CurrentGameState = GameState.Init;
        ActiveDiceColor = DiceColor.Unknown;
        LocalRotation = LocalRotation.Null;
        SessionId = string.Empty;
    }
}

[Serializable]
public struct BoardGraphics
{
    public Sprite boardSprite;
    public Sprite redPieceSprite;
    public Sprite bluePieceSprite;
    public Sprite yellowPieceSprite;
    public Sprite greenPieceSprite;
    public Sprite redBlinkSprite;
    public Sprite blueBlinkSprite;
    public Sprite yellowBlinkSprite;
    public Sprite greenBlinkSprite;

}