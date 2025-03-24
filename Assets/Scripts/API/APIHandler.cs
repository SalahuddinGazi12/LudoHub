using System;
using System.Text;
using System.Threading.Tasks;
using API.API_Helper_Classes;
using UnityEngine;
using UnityEngine.Networking;

public class APIHandler : MonoBehaviour
{
    public static APIHandler Instance { get; private set; }
    private const string appLogInUri = "https://bdgamersclub.com/api/app-user/login";
    private const string gameLogInUri = "https://bdgamersclub.com/api/game/login";

    private const string appRegUri = "https://bdgamersclub.com/api/app-user/registration";
    private const string gameRegUri = "https://bdgamersclub.com/api/game/registration";

    private const string getCoinUri = "https://bdgamersclub.com/api/app-user/total-coin";
    private const string sessionInitUri = "https://bdgamersclub.com/api/game/session-initiate";
    private const string sessionUpdateUri = "https://bdgamersclub.com/api/game/session-update";
    private const string giveCoinToUserUri = "https://bdgamersclub.com/api/app-user/give-coin";
    private const string logOutUri = "https://bdgamersclub.com/api/logout";
    private const string userDetailUri = "https://bdgamersclub.com/api/game/game-profile?game_id=";
    private const string configUri = "https://bdgamersclub.com/api/get-configuration?key=game_win_coin_deduct_percentage";
    private const string gameHistoryUri = "https://bdgamersclub.com/api/game/game-history?game_id=";
    private const string wishCoinStoreUrl = "https://bdgamersclub.com/api/game/wish-coin-store";

    #region Monobehaviour Methods and Initialization

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion Monobehaviour Methods and Initialization


    #region Web Requests

    #region Get Web Requests

    public async void SendGetWebRequestAsync(string targetUrl, Action<UnityWebRequest> webReqCallback)
    {
        using UnityWebRequest webRequest = UnityWebRequest.Get(targetUrl);

        UnityWebRequestAsyncOperation reqOperation = webRequest.SendWebRequest();

        while (!reqOperation.isDone)
        {
            await Task.Yield();
        }

        webReqCallback(webRequest);
    }

    public async void SendGetWebRequestAsync(string targetUrl, Action<string> onSuccess, Action onFail = null)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onFail?.Invoke();
            return;
        }

        try
        {
            using UnityWebRequest unityWebRequest = UnityWebRequest.Get(targetUrl);

            UnityWebRequestAsyncOperation reqOperation = unityWebRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                await Task.Yield();
            }

            if (unityWebRequest.error == null && unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(unityWebRequest.downloadHandler.text);
            }
            else
            {
                Debug.LogError(unityWebRequest.error);
                onFail?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            onFail?.Invoke();
        }
    }

    public async void SendGetWebRequestAsyncForTotalCoins(string targetUrl, string bearerToken, Action<string> callback) // call_number
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            callback?.Invoke("NetworkReachability.NotReachable");
            return;
        }

        try
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(targetUrl);

            webRequest.SetRequestHeader("Authorization", "Bearer " + bearerToken);
            UnityWebRequestAsyncOperation reqOperation = webRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                await Task.Yield();
            }

            callback?.Invoke(webRequest.downloadHandler.text);

            // if (webRequest.error == null && webRequest.result == UnityWebRequest.Result.Success)
            // {
            //     onSuccess?.Invoke(webRequest.downloadHandler.text);
            // }
            // else
            // {
            //     onFail?.Invoke(webRequest.error);
            // }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            callback?.Invoke(ex.Message);
        }
    }

    private async void SendGetRequestAsyncWithBearerToken(string targetUrl, string bearerToken, Action<string> callback) // call_number
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            callback?.Invoke("NetworkReachability.NotReachable");
            return;
        }

        try
        {
            using UnityWebRequest webRequest = UnityWebRequest.Get(targetUrl);

            webRequest.SetRequestHeader("Authorization", "Bearer " + bearerToken);

            UnityWebRequestAsyncOperation reqOperation = webRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                await Task.Yield();
            }

            callback?.Invoke(webRequest.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            callback?.Invoke(string.Empty);
        }
    }


    #endregion Get Web Requests


    #region Post Web Requests
    public async void SendPostWebRequestAsync(string jsonData, string url, Action<UnityWebRequest> callback)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            callback(null);
        }

        try
        {
            WWWForm wwwForm = new WWWForm();
            wwwForm.AddField("a", ConvertDataIntoBase64(jsonData));

            using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, wwwForm);

            UnityWebRequestAsyncOperation reqOperation = unityWebRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                await Task.Yield();
            }

            callback(unityWebRequest);
        }
        catch (Exception ex)
        {
            Debug.Log($"Get error while posting new request, Error: {ex.Message}");
            callback(null);
        }
    }

    public async void SendPostWebRequestAsync(string jsonData, string url, Action onSuccess, Action onFail = null)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onFail?.Invoke();
            return;
        }

        try
        {
            WWWForm wwwForm = new WWWForm();
            wwwForm.AddField("a", jsonData);

            using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, wwwForm);

            UnityWebRequestAsyncOperation reqOperation = unityWebRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                await Task.Yield();
            }

            if (unityWebRequest.error == null && unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(); //Need to check the api response and after that this should be fire
            }
            else
            {
                Debug.LogError($"Got Error Else: {unityWebRequest.error}");
                onFail?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Got Error while posting data, Error: {ex.Message}");
            onFail?.Invoke();
        }
    }


    public async void SendPostWebRequestAsync(string url, WWWForm wwwForm, Action onSuccess, Action<string> onFail = null)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onFail?.Invoke("NetworkReachability.NotReachable");
            return;
        }

        try
        {
            using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, wwwForm);

            UnityWebRequestAsyncOperation reqOperation = unityWebRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                await Task.Yield();
            }

            if (unityWebRequest.error == null && unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                //print("Successfully data sent");
                //print(Base64ToJsonString(unityWebRequest.downloadHandler.text));
                //print("Result: " + unityWebRequest.downloadHandler.text);

                if (unityWebRequest.downloadHandler.text.StartsWith("-"))
                {
                    Debug.LogError($"Got Error Else: {unityWebRequest.downloadHandler.text}");
                    onFail?.Invoke($"Got Error: {unityWebRequest.downloadHandler.text}");
                    return;
                }

                onSuccess?.Invoke(); //Need to check the api response and after that this should be fire
            }
            else
            {
                Debug.LogError($"Got Error Else: {unityWebRequest.error}");
                onFail?.Invoke($"Got Error: {unityWebRequest.error}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            onFail?.Invoke(ex.Message);
        }
    }

    private async void SendPostWebRequestAsync(string url, WWWForm wwwForm, Action<string> onSuccess, Action<string> onFail = null)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            onFail?.Invoke("No Internet!\nPlease check you internet connection.");
            return;
        }

        try
        {
            using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, wwwForm);
            UnityWebRequestAsyncOperation reqOperation = unityWebRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                //Debug.LogWarning("ok");
                await Task.Yield();
            }

            Debug.Log($"Response: {unityWebRequest.downloadHandler.text}, www: {JsonUtility.ToJson(wwwForm)}");
            

            if (unityWebRequest.error == null && unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.LogError(unityWebRequest.downloadHandler.text);
                onSuccess?.Invoke(unityWebRequest.downloadHandler.text); //Need to check the api response and after that this should be fire
            }
            else
            {
                if (!string.IsNullOrEmpty(unityWebRequest.downloadHandler.text))
                {
                    onFail?.Invoke(unityWebRequest.downloadHandler.text);
                    return;
                }

                Debug.LogError($"Error: {unityWebRequest.error}, Result: {unityWebRequest.result}, Code: {unityWebRequest.responseCode}, {unityWebRequest.downloadHandler.text}");

                onFail?.Invoke(unityWebRequest.error);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            onFail?.Invoke(ex.Message);
        }
    }

    private async void SendPostWebRequestAsyncWithBearerToken(string url, WWWForm wwwForm, string bearerToken, Action<string> callbacks)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            callbacks?.Invoke("No Internet!\nPlease check you internet connection.");
            return;
        }

        try
        {
            using UnityWebRequest unityWebRequest = UnityWebRequest.Post(url, wwwForm);
            unityWebRequest.SetRequestHeader("Authorization", "Bearer " + bearerToken);
            UnityWebRequestAsyncOperation reqOperation = unityWebRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                //Debug.LogWarning("ok");
                await Task.Yield();
            }

            Debug.Log($"Response: {unityWebRequest.downloadHandler.text}");

            callbacks?.Invoke(unityWebRequest.downloadHandler.text);

            // if (unityWebRequest.error == null && unityWebRequest.result == UnityWebRequest.Result.Success)
            // {
            //     Debug.LogError(unityWebRequest.downloadHandler.text);
            //     callbacks?.Invoke(unityWebRequest.downloadHandler.text); //Need to check the api response and after that this should be fire
            // }
            // else
            // {
            //     if (!string.IsNullOrEmpty(unityWebRequest.downloadHandler.text))
            //     {
            //         onFail?.Invoke(unityWebRequest.downloadHandler.text);
            //         return;
            //     }
            //     
            //     Debug.LogError($"Error: {unityWebRequest.error}, Result: {unityWebRequest.result}, Code: {unityWebRequest.responseCode}, {unityWebRequest.downloadHandler.text}");
            //     
            //     onFail?.Invoke(unityWebRequest.error);
            // }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            callbacks?.Invoke(ex.Message);
        }
    }

    public async void DownloadImageAsync(string url, Action<Texture2D> callBack)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            callBack?.Invoke(null);
            return;
        }

        try
        {
            using UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(url);
            UnityWebRequestAsyncOperation reqOperation = unityWebRequest.SendWebRequest();

            while (!reqOperation.isDone)
            {
                //Debug.LogWarning("ok");
                await Task.Yield();
            }

            Debug.Log($"Response: {unityWebRequest.downloadHandler.text}");



            if (unityWebRequest.error == null && unityWebRequest.result == UnityWebRequest.Result.Success)
            {
                callBack?.Invoke(DownloadHandlerTexture.GetContent(unityWebRequest)); //Need to check the api response and after that this should be fire
                return;
            }

            callBack?.Invoke(null);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            callBack?.Invoke(null);
        }
    }


    #endregion Post Web Requests

    #endregion Web Request

    #region Encryption/Decryption
    private string ConvertDataIntoBase64(string jsonString)
    {
        string returnData = string.Empty;
        try
        {
            byte[] bytesToEncode = Encoding.UTF8.GetBytes(jsonString);
            returnData = Convert.ToBase64String(bytesToEncode);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Got error while converting from byte array to base64 and the error is {ex.Message}");
        }

        return returnData;
    }

    private string Base64ToJsonString(string base64Str)
    {
        string resultStr = string.Empty;
        try
        {
            resultStr = Encoding.UTF8.GetString(Convert.FromBase64String(base64Str));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error From Base64ToJsonString, Error: {ex.Message}");
        }

        return resultStr;

    }
    #endregion Encryption/Decryption

    #region Web Request Helping Method

    public void AppLogInPostRequest(WWWForm wwwForm, Action<string> successAction, Action<string> failedAction)
    {
        SendPostWebRequestAsync(appLogInUri, wwwForm, successAction, failedAction);
    }

    public void GameLogInPostRequest(WWWForm wwwForm, Action<string> successAction, Action<string> failedAction)
    {
        SendPostWebRequestAsync(gameLogInUri, wwwForm, successAction, failedAction);
    }

    public void AppMemberRegistrationPostRequest(WWWForm wwwForm, Action<string> successAction, Action<string> failedAction)
    {
        SendPostWebRequestAsync(appRegUri, wwwForm, successAction, failedAction);
    }

    public void GeneralMemberRegistrationPostRequest(WWWForm wwwForm, Action<string> successAction, Action<string> failedAction)
    {
        SendPostWebRequestAsync(gameRegUri, wwwForm, successAction, failedAction);
    }

    public void FixDiceCoinDeduction(Action<string> successAction)
    {
        WWWForm form = new WWWForm();
        form.AddField("game_session", DataManager.Instance.SessionId);
        SendPostWebRequestAsyncWithBearerToken(wishCoinStoreUrl, form, DataManager.Instance.Token, successAction);
    }

    
    public void UpdateSession(WWWForm wwwForm, string bearerToken, Action<string> callbacks)
    {
        SendPostWebRequestAsyncWithBearerToken(sessionUpdateUri, wwwForm, bearerToken, callbacks);
    }

    public void GetUserTotalCoin(string bearerToken, Action<string> callback)
    {
        SendGetWebRequestAsyncForTotalCoins(getCoinUri, bearerToken, callback);
    }

    public void InitiateSession(WWWForm wwwForm, string bearerToken, Action<string> callbacks)
    {
        SendPostWebRequestAsyncWithBearerToken(sessionInitUri, wwwForm, bearerToken, callbacks);
    }

    public void GiveCoinToUser(string userId, int coin, Action<string> callback = null)
    {
        WWWForm wwwForm = new WWWForm();

        wwwForm.AddField("coin", coin);
        wwwForm.AddField("user_id", userId);

        SendPostWebRequestAsyncWithBearerToken(giveCoinToUserUri, wwwForm, DataManager.Instance.Token, res =>
        {
            callback?.Invoke(res);
        });
    }

    public void LogOut(string bearerToken, Action<string> callbacks)
    {
        SendPostWebRequestAsyncWithBearerToken(logOutUri, new WWWForm(), bearerToken, callbacks);
    }

    public void GetUserDetails(string bearerToken, Action<string> callbacks)
    {
        SendGetRequestAsyncWithBearerToken(string.Concat(userDetailUri, DataManager.Instance.GameId), bearerToken, callbacks);
    }

    public void GetConfig(string bearerToken, Action<string> callback)
    {
        SendGetRequestAsyncWithBearerToken(configUri, bearerToken, callback);
    }

    public void GetGameHistoryData(string bearerToken, Action<string> callback)
    {
        SendGetRequestAsyncWithBearerToken(string.Concat(gameHistoryUri, DataManager.Instance.GameId), bearerToken, callback);
    }

    // public void DownloadImage(string url)
    // {
    //     DownloadImageAsync(url);
    // }

    #endregion Web Request Helping Method
}