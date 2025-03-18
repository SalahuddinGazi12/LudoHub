using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Network
{
    public class InternetConnectivityManager : MonoBehaviour
    {
        public static InternetConnectivityManager Instance { get; private set; }
        
        public event Action onNetDisconnected;
        public event Action onNetConnected;

        [SerializeField] private float pingInterval = 0.5f; // Interval between pings
        [SerializeField] private float serverSwitchInterval = 3f; // Time to switch ping servers

        private bool isConnectedToInternet;
        public bool IsConnectedToInternet => isConnectedToInternet;

        private readonly List<string> serverUrls = new List<string>
        {
            "https://www.google.com",
            "https://www.bing.com",
            "https://www.yahoo.com",
            "8.8.8.8",        // Google DNS
            "1.1.1.1",        // CloudFlare DNS
            "9.9.9.9",        // Quad9 DNS
            "64.6.64.6",      // Verisign DNS
            "8.8.4.4"         // Google DNS Alternative
        };

        private string currentPingUrl;
        private float nextPingTime;
        private float nextServerSwitchTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (serverUrls.Count == 0)
            {
                Debug.LogError("No server URLs provided for connectivity check.");
                enabled = false;
                return;
            }

            ChangePingServer();
        }

        // private void Update()
        // {
        //     if (Time.time >= nextPingTime)
        //     {
        //         nextPingTime = Time.time + pingInterval;
        //         CheckInternetConnectivity();
        //     }
        //
        //     if (Time.time >= nextServerSwitchTime)
        //     {
        //         nextServerSwitchTime = Time.time + serverSwitchInterval;
        //         ChangePingServer();
        //     }
        // }

        private void CheckInternetConnectivity()
        {
            StartCoroutine(PingServer(currentPingUrl, isConnected =>
            {
                if (isConnected != isConnectedToInternet)
                {
                    isConnectedToInternet = isConnected;

                    if (isConnectedToInternet)
                    {
                        Debug.Log("Connected to the internet.");
                        onNetConnected?.Invoke();
                    }
                    else
                    {
                        Debug.Log("Disconnected from the internet.");
                        onNetDisconnected?.Invoke();
                    }
                }
            }));
        }

        private IEnumerator<UnityWebRequestAsyncOperation> PingServer(string url, Action<bool> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Head(url))
            {
                request.timeout = 2; // Timeout for the ping request
                yield return request.SendWebRequest();

                callback(request.result == UnityWebRequest.Result.Success);
            }
        }

        private void ChangePingServer()
        {
            if (serverUrls.Count == 0) return;

            currentPingUrl = serverUrls[UnityEngine.Random.Range(0, serverUrls.Count)];
            Debug.Log($"Switched ping server to: {currentPingUrl}");
        }
    }
}