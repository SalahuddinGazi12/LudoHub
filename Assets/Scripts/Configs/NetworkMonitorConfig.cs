using System.Collections.Generic;
using UnityEngine;

namespace Configs
{
    [CreateAssetMenu(fileName = "NetworkMonitorConfig", menuName = "Network/Monitor Configuration")]
    public class NetworkMonitorConfig : ScriptableObject
    {
        [Header("Server Settings")]
        public float pingInterval = 0.5f;
        public float serverChangeInterval = 30f;
        public int timeoutMilliseconds = 2000;
        public bool enableServerRotation = true;
    
        [Header("Server List")]
        public List<string> serverUrls = new List<string>
        {
            "8.8.8.8",        // Google DNS
            "1.1.1.1",        // Cloudflare DNS
            "208.67.222.222", // OpenDNS
            "9.9.9.9",        // Quad9 DNS
            "64.6.64.6",      // Verisign DNS
            "8.8.4.4"         // Google DNS Alternative
        };
    }
}
